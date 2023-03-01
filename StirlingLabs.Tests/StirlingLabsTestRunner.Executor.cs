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

            case 1: {
                var firstParamType = methodParams[0].ParameterType;
                if (firstParamType == typeof(TextWriter))
                    return TestRunnerWithLogger;

                if (firstParamType == typeof(CancellationToken))
                    return TestRunnerWithCancellation;

                throw new NotImplementedException();
            }
            case 2: {
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
    public void RunTests(IEnumerable<TestCase>? tests, IRunContext? runContext, IFrameworkHandle? frameworkHandle)
    {
        if (tests is null) return;

        var alc = new AssemblyLoadContext("StirlingLabsTestExecutor" + Interlocked.Increment(ref _alcCounter), true);

        var hierarchy = BuildTestCaseHierarchy(tests);

        // <class, <instance, <thread, funcPtr, fw, testCase>>>
        var jobsByClass = BuildJobsByClass();

        ProcessAssemblies(frameworkHandle, alc, hierarchy, jobsByClass);

        frameworkHandle?.SendMessage(TestMessageLevel.Informational, "Starting tests.");

        var ct = CancellationTokenSource.Token;

        var initThreads = new Dictionary<string, Thread>();
        if (InitializeAssemblyModules(frameworkHandle, alc, jobsByClass, initThreads, ct))
            return;

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

        if (InitializeClasses(frameworkHandle, jobsByClass, initThreads, ct))
            return;

        foreach (var (_, thread) in initThreads)
        {
            if (ct.IsCancellationRequested)
                return;

            thread.Join();
        }

        if (ct.IsCancellationRequested)
            return;

        if (!StartAllTests(jobsByClass, ct))
            return;

        frameworkHandle?.SendMessage(TestMessageLevel.Informational, "All tests started.");

        if (!WaitForAllTests(jobsByClass, ct))
            return;

        frameworkHandle?.SendMessage(TestMessageLevel.Informational, "All tests finished.");

        alc.Unload();

        frameworkHandle?.SendMessage(TestMessageLevel.Informational, "Test host finished.");
    }
    private static bool WaitForAllTests(Dictionary<string, (Type Class, List<(Thread Thread, MethodInfo Method, IFrameworkHandle? FrameworkHandle, TestCase TestCase)> Jobs)> jobsByClass, CancellationToken ct) {
        foreach (var jobs in jobsByClass.Values)
        foreach (var (thread, _, _, _) in jobs.Jobs) {
            if (ct.IsCancellationRequested)
                return false;

            thread.Join();
        }

        return true;
    }
    private static bool StartAllTests(Dictionary<string, (Type Class, List<(Thread Thread, MethodInfo Method, IFrameworkHandle? FrameworkHandle, TestCase TestCase)> Jobs)> jobsByClass, CancellationToken ct) {
        foreach (var (jobClass, jobs) in jobsByClass.Values) {
            foreach (var (thread, funcPtr, fw, tc) in jobs) {
                thread.UnsafeStart((funcPtr, fw, tc, jobClass, ct));

                if (ct.IsCancellationRequested)
                    return false;
            }
        }

        return true;
    }
    private static bool InitializeClasses(IFrameworkHandle? frameworkHandle, Dictionary<string, (Type Class, List<(Thread Thread, MethodInfo Method, IFrameworkHandle? FrameworkHandle, TestCase TestCase)> Jobs)> jobsByClass, Dictionary<string, Thread> initThreads, CancellationToken ct) {
        foreach (var (jobClass, jobs) in jobsByClass.Values) {
            InitializeClass(frameworkHandle, jobClass, initThreads);

            if (ct.IsCancellationRequested)
                return true;
        }

        return false;
    }
    private static bool InitializeAssemblyModules(IFrameworkHandle? frameworkHandle, AssemblyLoadContext alc, Dictionary<string, (Type Class, List<(Thread Thread, MethodInfo Method, IFrameworkHandle? FrameworkHandle, TestCase TestCase)> Jobs)> jobsByClass, Dictionary<string, Thread> initThreads, CancellationToken ct) {
        foreach (var asmName in jobsByClass.Keys) {
            var asm = alc.Assemblies.FirstOrDefault(asm => asm.FullName == asmName);
            if (asm is null) continue;

            InitializeAssemblyModule(frameworkHandle, asm, asmName, initThreads);

            if (ct.IsCancellationRequested)
                return true;
        }

        return false;
    }
    private static void ProcessAssemblies(IFrameworkHandle? frameworkHandle, AssemblyLoadContext alc, Dictionary<string, Dictionary<string, List<TestCase>>> hierarchy, Dictionary<string, (Type Class, List<(Thread Thread, MethodInfo Method, IFrameworkHandle? FrameworkHandle, TestCase TestCase)> Jobs)> jobsByClass) {
        foreach (var (asmPath, classGroup) in hierarchy) {
            if (classGroup.Count == 0) continue;

            ProcessAssembly(frameworkHandle, alc, asmPath, jobsByClass, classGroup);
        }
    }
    private static void InitializeClass(IFrameworkHandle? frameworkHandle, Type jobClass, Dictionary<string, Thread> initThreads) {
        var thread = new Thread(static o => {
            var (type, frameworkHandle) = (((Type, IFrameworkHandle))o!);
            try {
                var thread = Thread.CurrentThread;
                thread.Name = $"init static {type.FullName}";
                RuntimeHelpers.RunClassConstructor(type.TypeHandle);
            }
            catch (Exception ex) {
                var errMsg = "<unset>";
                var errStackTrace = "<unset>";
                var exTypeFullName = "<unset>";
                try {
                    // ReSharper disable once NullCoalescingConditionIsAlwaysNotNullAccordingToAPIContract
                    errMsg = ex.Message ?? "<missing>";
                    errStackTrace = ex.StackTrace ?? "<missing>";
                    var exType = ex.GetType();
                    exTypeFullName = exType.FullName ?? exType.Name ?? "<missing>";
                }
#pragma warning disable CA1031
                catch {
                    // oof
                }
#pragma warning restore CA1031

                var currentThread = Thread.CurrentThread;
                frameworkHandle?.SendMessage(TestMessageLevel.Error,
                    $"Error in {currentThread.Name ?? $"Thread{currentThread.ManagedThreadId}"}\n{exTypeFullName}: {errMsg}\n{errStackTrace}");
                throw;
            }
        });
        thread.UnsafeStart((jobClass, frameworkHandle));
        initThreads.Add(jobClass.FullName!, thread);
    }
    private static void InitializeAssemblyModule(IFrameworkHandle? frameworkHandle, Assembly asm, string asmName, Dictionary<string, Thread> initThreads) {
        var thread = new Thread(static o => {
            var (module, frameworkHandle) = ((Module, IFrameworkHandle?))o!;
            try {
                var thread = Thread.CurrentThread;
                thread.Name = $"init module {module.Name}";
                RuntimeHelpers.RunModuleConstructor(module.ModuleHandle);
            }
            catch (Exception ex) {
                var errMsg = "<unset>";
                var errStackTrace = "<unset>";
                var exTypeFullName = "<unset>";
                try {
                    // ReSharper disable once NullCoalescingConditionIsAlwaysNotNullAccordingToAPIContract
                    errMsg = ex.Message ?? "<missing>";
                    errStackTrace = ex.StackTrace ?? "<missing>";
                    var exType = ex.GetType();
                    exTypeFullName = exType.FullName ?? exType.Name ?? "<missing>";
                }
#pragma warning disable CA1031
                catch {
                    // oof
                }
#pragma warning restore CA1031

                var currentThread = Thread.CurrentThread;
                frameworkHandle?.SendMessage(TestMessageLevel.Error,
                    $"Error in {currentThread.Name ?? $"Thread{currentThread.ManagedThreadId}"}\n{exTypeFullName}: {errMsg}\n{errStackTrace}");
                throw;
            }
        });
        thread.UnsafeStart((asm.ManifestModule, frameworkHandle));
        initThreads.Add(asmName, thread);
    }
    private static void ProcessAssembly(IFrameworkHandle? frameworkHandle, AssemblyLoadContext alc, string asmPath, Dictionary<string, (Type Class, List<(Thread Thread, MethodInfo Method, IFrameworkHandle? FrameworkHandle, TestCase TestCase)> Jobs)> jobsByClass, Dictionary<string, List<TestCase>> classGroup) {
        frameworkHandle?.SendMessage(TestMessageLevel.Informational, $"Loading {asmPath}");
        try {
            if (!File.Exists(asmPath))
                throw new FileNotFoundException($"Could not find {asmPath}", asmPath);

            var asm = alc.LoadFromAssemblyPath(asmPath);
            var classNames = new HashSet<string>(classGroup.Keys);
            var testClasses = asm.ExportedTypes
                .Where(t => {
                    var tfn = t.FullName;
                    return tfn is not null && classNames.Contains(tfn);
                })
                .ToDictionary(k => k.FullName!);
            frameworkHandle?.SendMessage(TestMessageLevel.Informational, $"Loaded {testClasses.Count} classes from {asmPath}");
            foreach (var (className, testCases) in classGroup) {
                if (testCases.Count == 0) continue;

                var classType = testClasses[className];
                try {
                    ref var classJobs = ref CollectionsMarshal.GetValueRefOrAddDefault(jobsByClass, className, out var exists);
                    if (!exists) classJobs = (classType, new());

                    frameworkHandle?.SendMessage(TestMessageLevel.Informational, $"Running {testCases.Count} tests in {className}.");

                    foreach (var testCase in testCases) {
                        try {
                            var methodName = GetMethodName(testCase);
                            var method = classType.GetMethod(methodName);
                            if (method is null) continue;

                            var runner = GetTestRunnerMethod(method);
                            classJobs.Jobs.Add((new(runner), method, frameworkHandle, testCase));
                        }
#pragma warning disable CA1031
                        catch (Exception ex) {
                            if (frameworkHandle is null) continue;

                            frameworkHandle.RecordResult(new(testCase) {
                                Outcome = TestOutcome.Skipped,
                                ErrorMessage = ex.Message,
                                ErrorStackTrace = ex.StackTrace,
                                Messages = {
                                    new TestResultMessage(
                                        TestResultMessage.StandardErrorCategory,
                                        "Test host failed to set up this test."
                                    )
                                }
                            });
                        }
#pragma warning restore CA1031
                    }
                }
#pragma warning disable CA1031
                catch (Exception ex) {
                    if (frameworkHandle is null) continue;

                    foreach (var testCase in testCases) {
                        frameworkHandle.RecordResult(new(testCase) {
                            Outcome = TestOutcome.Skipped,
                            ErrorMessage = ex.Message,
                            ErrorStackTrace = ex.StackTrace,
                            Messages = {
                                new TestResultMessage(
                                    TestResultMessage.StandardErrorCategory,
                                    "Test host failed to set up the test class."
                                )
                            }
                        });
                    }
                }
#pragma warning restore CA1031
            }
        }
#pragma warning disable CA1031
        catch (Exception ex) {
            if (frameworkHandle is null) return;

            foreach (var testCase in classGroup.Values.SelectMany(v => v)) {
                frameworkHandle.RecordResult(new(testCase) {
                    Outcome = TestOutcome.Skipped,
                    ErrorMessage = ex.Message,
                    ErrorStackTrace = ex.StackTrace,
                    Messages = {
                        new TestResultMessage(
                            TestResultMessage.StandardErrorCategory,
                            "Test host failed to load the test assembly."
                        )
                    }
                });
            }
        }
#pragma warning restore CA1031
    }
    private static Dictionary<string, (Type Class, List<(Thread Thread, MethodInfo Method, IFrameworkHandle? FrameworkHandle, TestCase TestCase)> Jobs)> BuildJobsByClass()
        => new Dictionary<string,
            (Type Class, List<(
                Thread Thread,
                MethodInfo Method,
                IFrameworkHandle? FrameworkHandle,
                TestCase TestCase
                )> Jobs)>();

    private static Dictionary<string, Dictionary<string, List<TestCase>>> BuildTestCaseHierarchy(IEnumerable<TestCase> tests) {
        // <assembly, <class, test>>
        var hierarchy = new Dictionary<string, Dictionary<string, List<TestCase>>>();
        foreach (var test in tests) {
            ref var byAsm = ref CollectionsMarshal.GetValueRefOrAddDefault(hierarchy, test.Source, out var exists);
            if (!exists) byAsm = new();
            var className = GetClassName(test);
            ref var byClass = ref CollectionsMarshal.GetValueRefOrAddDefault(byAsm!, className, out exists);
            if (!exists) byClass = new();
            byClass!.Add(test);
        }

        return hierarchy;
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
#pragma warning disable CA1031
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
#pragma warning restore CA1031

        return inst;
    }

    private static nint PrepareMethodAndGetPointer(MethodInfo mi, IFrameworkHandle? fw, TestCase tc)
    {
        var mh = mi.MethodHandle;
        try
        {
            RuntimeHelpers.PrepareMethod(mh);
        }
#pragma warning disable CA1031
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
#pragma warning restore CA1031

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
        => CancellationTokenSource.Cancel();

    public bool ShouldAttachToTestHost(IEnumerable<string>? containers, IRunContext? runContext)
        => containers is not null && containers.Any();

    public bool ShouldAttachToTestHost(IEnumerable<TestCase>? tests, IRunContext? runContext)
        => tests is not null && tests.Any();
}
