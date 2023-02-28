using System.Diagnostics;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics.Arm;
using System.Runtime.Intrinsics.X86;
using System.Runtime.Loader;

namespace StirlingLabs.Tests;

[PublicAPI]
[ExtensionUri(UriString)]
public partial class StirlingLabsTestRunner : ITestExecutor2
{

    public const string UriString = "executor://dwtest/";

    public static readonly Uri Uri = new(UriString);

    public CancellationTokenSource CancellationTokenSource = new CancellationTokenSource();

    private static ParameterizedThreadStart GetTestRunnerMethod(MethodInfo method)
    {
        var methodParams = method.GetParameters();

        switch (methodParams.Length)
        {
            case 0:
                return TestRunner;

            case 1:
            {
                var firstParamType = methodParams[0].ParameterType;
                if (firstParamType == typeof(TextWriter))
                    return TestRunnerWithLogger;

                if (firstParamType == typeof(CancellationToken))
                    return TestRunnerWithCancellation;

                throw new NotImplementedException();
            }
            case 2:
            {
                if (methodParams[0].ParameterType != typeof(TextWriter)
                    || methodParams[1].ParameterType != typeof(CancellationToken))
                    throw new NotImplementedException();

                return TestRunnerWithLoggerAndCancellation;
            }
            default:
                throw new NotImplementedException();
        }

        throw new NotImplementedException();
    }

    private static long _alcCounter;

    /// <summary>
    /// Runs only the tests specified by parameter 'tests'. 
    /// </summary>
    /// <param name="tests">Tests to be run.</param>
    /// <param name="runContext">Context to use when executing the tests.</param>
    /// <param name="frameworkHandle">Handle to the framework to record results and to do framework operations.</param>
    public unsafe void RunTests(IEnumerable<TestCase>? tests, IRunContext? runContext, IFrameworkHandle? frameworkHandle)
    {
        if (tests is null) return;

        var alc = new AssemblyLoadContext("StirlingLabsTestExecutor" + Interlocked.Increment(ref _alcCounter), true);

        // <assembly, <class, test>>
        var hierarchy = new Dictionary<string, Dictionary<string, List<TestCase>>>();
        foreach (var test in tests)
        {
            ref var byAsm = ref CollectionsMarshal.GetValueRefOrAddDefault(hierarchy, test.Source, out var exists);
            if (!exists) byAsm = new();
            var className = GetClassName(test);
            ref var byClass = ref CollectionsMarshal.GetValueRefOrAddDefault(byAsm!, className, out exists);
            if (!exists) byClass = new();
            byClass!.Add(test);
        }

        // <class, <instance, <thread, funcPtr, fw, testCase>>>
        var jobsByClass = new Dictionary<string,
            (Type Class, List<(
                Thread Thread,
                MethodInfo Method,
                IFrameworkHandle? FrameworkHandle,
                TestCase TestCase
                )> Jobs)>();

        foreach (var (asmPath, classGroup) in hierarchy)
        {
            if (classGroup.Count == 0) continue;

            frameworkHandle?.SendMessage(TestMessageLevel.Informational, $"Loading {asmPath}");
            try
            {
                if (!File.Exists(asmPath))
                    throw new FileNotFoundException($"Could not find {asmPath}", asmPath);

                var asm = alc.LoadFromAssemblyPath(asmPath);
                var classNames = new HashSet<string>(classGroup.Keys);
                var testClasses = asm.ExportedTypes
                    .Where(t =>
                    {
                        var tfn = t.FullName;
                        return tfn is not null && classNames.Contains(tfn);
                    })
                    .ToDictionary(k => k.FullName!);
                frameworkHandle?.SendMessage(TestMessageLevel.Informational, $"Loaded {testClasses.Count} classes from {asmPath}");
                foreach (var (className, testCases) in classGroup)
                {
                    if (testCases.Count == 0) continue;

                    var classType = testClasses[className];
                    try
                    {
                        ref var classJobs = ref CollectionsMarshal.GetValueRefOrAddDefault(jobsByClass, className, out var exists);
                        if (!exists) classJobs = (classType, new());

                        frameworkHandle?.SendMessage(TestMessageLevel.Informational, $"Running {testCases.Count} tests in {className}.");

                        foreach (var testCase in testCases)
                        {
                            try
                            {
                                var methodName = GetMethodName(testCase);
                                var method = classType.GetMethod(methodName);
                                if (method is null) continue;

                                var runner = GetTestRunnerMethod(method);
                                classJobs.Jobs.Add((new(runner), method, frameworkHandle, testCase));
                            }
                            catch (Exception ex)
                            {
                                if (frameworkHandle is null) continue;

                                frameworkHandle.RecordResult(new(testCase)
                                {
                                    Outcome = TestOutcome.Skipped,
                                    ErrorMessage = ex.Message,
                                    ErrorStackTrace = ex.StackTrace,
                                    Messages =
                                    {
                                        new TestResultMessage(
                                            TestResultMessage.StandardErrorCategory,
                                            "Test host failed to set up this test."
                                        )
                                    }
                                });
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        if (frameworkHandle is null) continue;

                        foreach (var testCase in testCases)
                        {
                            frameworkHandle?.RecordResult(new(testCase)
                            {
                                Outcome = TestOutcome.Skipped,
                                ErrorMessage = ex.Message,
                                ErrorStackTrace = ex.StackTrace,
                                Messages =
                                {
                                    new TestResultMessage(
                                        TestResultMessage.StandardErrorCategory,
                                        "Test host failed to set up the test class."
                                    )
                                }
                            });
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                if (frameworkHandle is null) continue;

                foreach (var testCase in classGroup.Values.SelectMany(v => v))
                {
                    frameworkHandle.RecordResult(new(testCase)
                    {
                        Outcome = TestOutcome.Skipped,
                        ErrorMessage = ex.Message,
                        ErrorStackTrace = ex.StackTrace,
                        Messages =
                        {
                            new TestResultMessage(
                                TestResultMessage.StandardErrorCategory,
                                "Test host failed to load the test assembly."
                            )
                        }
                    });
                }
            }
        }

        frameworkHandle?.SendMessage(TestMessageLevel.Informational, "Starting tests.");

        var ct = CancellationTokenSource.Token;

        var initThreads = new Dictionary<string, Thread>();
        foreach (var asmName in jobsByClass.Keys)
        {
            var asm = alc.Assemblies.FirstOrDefault(asm => asm.FullName == asmName);
            if (asm is null) continue;

            var thread = new Thread(static o =>
            {
                try
                {
                    var module = (Module)o!;
                    var thread = Thread.CurrentThread;
                    thread.Name = $"init module {module.Name}";
                    RuntimeHelpers.RunModuleConstructor(module.ModuleHandle);
                }
                catch
                {
                }
            });
            thread.UnsafeStart(asm?.ManifestModule);
            initThreads.Add(asmName, thread);

            if (ct.IsCancellationRequested)
                return;
        }

        if (ct.IsCancellationRequested)
            return;

        foreach (var (asmName, thread) in initThreads)
        {
            thread.Join();

            if (ct.IsCancellationRequested)
                return;
        }

        initThreads.Clear();

        if (ct.IsCancellationRequested)
            return;

        foreach (var (jobClass, jobs) in jobsByClass.Values)
        {
            var thread = new Thread(static o =>
            {
                try
                {
                    var type = ((Type)o!);
                    var thread = Thread.CurrentThread;
                    thread.Name = $"init static {type.FullName}";
                    RuntimeHelpers.RunClassConstructor(type.TypeHandle);
                }
                catch
                {
                }
            });
            thread.UnsafeStart(jobClass);
            initThreads.Add(jobClass.FullName!, thread);

            if (ct.IsCancellationRequested)
                return;
        }

        foreach (var (className, thread) in initThreads)
        {
            if (ct.IsCancellationRequested)
                return;

            thread.Join();
        }

        if (ct.IsCancellationRequested)
            return;

        foreach (var (jobClass, jobs) in jobsByClass.Values)
        {
            foreach (var (thread, funcPtr, fw, tc) in jobs)
            {
                thread.UnsafeStart((funcPtr, fw, tc, jobClass, ct));

                if (ct.IsCancellationRequested)
                    return;
            }
        }

        frameworkHandle?.SendMessage(TestMessageLevel.Informational, "All tests started.");

        foreach (var jobs in jobsByClass.Values)
        foreach (var (thread, _, _, _) in jobs.Jobs)
        {
            if (ct.IsCancellationRequested)
                return;

            thread.Join();
        }

        frameworkHandle?.SendMessage(TestMessageLevel.Informational, "All tests finished.");

        alc.Unload();

        frameworkHandle?.SendMessage(TestMessageLevel.Informational, "Test host finished.");
    }

    [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.AggressiveOptimization)]
    private static void SyncTimestampBoundary()
    {
        var ts = Stopwatch.GetTimestamp();
        while (Stopwatch.GetTimestamp() == ts)
        {
            if (X86Base.IsSupported)
                X86Base.Pause();
            else if (ArmBase.IsSupported)
                ArmBase.Yield();
            else throw new NotImplementedException();
        }
    }

    private static object? ActivateTestClassInstance(Type jobClass, IFrameworkHandle? fw, TestCase tc)
    {
        object inst;
        try
        {
            inst = Activator.CreateInstance(jobClass)!;
            if (inst is null)
                throw new NullReferenceException($"Activator failed to create an instance of {jobClass.AssemblyQualifiedName}");
        }
        catch (Exception ex)
        {
            fw?.RecordResult(new(tc)
            {
                Outcome = TestOutcome.Skipped,
                ErrorMessage = ex.Message,
                ErrorStackTrace = ex.StackTrace,
                Messages =
                {
                    new TestResultMessage(
                        TestResultMessage.StandardErrorCategory,
                        "Test cancelled because test class instance was not constructed successfully."
                    )
                }
            });
            return null;
        }

        return inst;
    }

    private static nint PrepareMethodAndGetPointer(MethodInfo mi, IFrameworkHandle? fw, TestCase tc)
    {
        var mh = mi.MethodHandle;
        try
        {
            RuntimeHelpers.PrepareMethod(mh);
        }
        catch (Exception ex)
        {
            fw?.RecordResult(new(tc)
            {
                Outcome = TestOutcome.Skipped,
                ErrorMessage = ex.Message,
                ErrorStackTrace = ex.StackTrace,
                Messages =
                {
                    new TestResultMessage(
                        TestResultMessage.StandardErrorCategory,
                        "Test cancelled because test method was not able to be prepared."
                    )
                }
            });
            return 0;
        }

        var fp = mh.GetFunctionPointer();
        return fp;
    }

    /// <summary>
    /// Runs 'all' the tests present in the specified 'containers'. 
    /// </summary>
    /// <param name="containers">Path to test container files to look for tests in.</param>
    /// <param name="runContext">Context to use when executing the tests.</param>
    /// <param name="frameworkHandle">Handle to the framework to record results and to do framework operations.</param>
    public void RunTests(IEnumerable<string>? containers, IRunContext? runContext, IFrameworkHandle? frameworkHandle)
    {
        var sink = new StirlingLabsTestDiscoverySink();
        DiscoverTests(containers, null, null, sink);
        RunTests(sink.TestCases, runContext, frameworkHandle);
    }

    /// <summary>
    /// Cancel the execution of the tests.
    /// </summary>
    public void Cancel()
    {
        CancellationTokenSource.Cancel();
    }

    public bool ShouldAttachToTestHost(IEnumerable<string>? containers, IRunContext? runContext)
    {
        return containers is not null && containers.Any();
    }

    public bool ShouldAttachToTestHost(IEnumerable<TestCase>? tests, IRunContext? runContext)
    {
        return tests is not null && tests.Any();
    }

}
