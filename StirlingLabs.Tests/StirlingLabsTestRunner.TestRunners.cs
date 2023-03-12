using System.Collections.Concurrent;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace StirlingLabs.Tests;

public partial class StirlingLabsTestRunner
{
    [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.AggressiveOptimization)]
    [StackTraceHidden, DebuggerStepThrough, DebuggerHidden, DebuggerNonUserCode]
    private static unsafe void TestRunner(object? o)
    {
        NotifyExecutingFromTestFrameworkThread();
        var (mi, fw, tc, jobClass, ct)
            = Unsafe.Unbox<(MethodInfo, IFrameworkHandle?, TestCase, Type, CancellationToken)>(o!);
        

        var thread = Thread.CurrentThread;
        thread.Name = $"Test: {tc.FullyQualifiedName}";

        var inst = ActivateTestClassInstance(jobClass, fw, tc);
        if (inst is null) return;

        var fp = PrepareMethodAndGetPointer(mi, fw, tc);

        if (fp == default)
            return;

        var failed = false;
        using var sw = new StringWriter();

        SyncTimestampBoundary();

        fw?.RecordStart(tc);
        DateTimeOffset started = default;
        long startedTs = default;
        try
        {
            ct.ThrowIfCancellationRequested();
            started = DateTimeOffset.Now;
            startedTs = Stopwatch.GetTimestamp();
            ((delegate * <object, void>)fp)(inst);
        }
        catch (OperationCanceledException ex) when (ex.CancellationToken == ct)
        {
            if (fw is null)
                return;

            var endedTs = Stopwatch.GetTimestamp();
            var ended = DateTimeOffset.Now;
            fw.RecordEnd(tc, TestOutcome.Failed);
            failed = true;
            ReportTestOperationCancelledException(started, ended, startedTs, endedTs, fw, tc, ex, sw);
        }
#pragma warning disable CA1031
        catch (Exception ex)
        {
            if (fw is null)
                return;

            var endedTs = Stopwatch.GetTimestamp();
            var ended = DateTimeOffset.Now;
            fw.RecordEnd(tc, TestOutcome.Failed);
            failed = true;
            ReportTestException(started, ended, startedTs, endedTs, fw, tc, ex, sw);
        }
#pragma warning restore CA1031

        // ReSharper disable once InvertIf // naming collisions
        if (!failed)
        {
            if (fw is null)
                return;

            var endedTs = Stopwatch.GetTimestamp();
            var ended = DateTimeOffset.Now;
            fw.RecordEnd(tc, TestOutcome.Passed);

            ReportSuccess(started, ended, startedTs, endedTs, fw, tc, sw);
        }

        if (inst is not IDisposable)
            return;

        try
        {
            var disposeMethod = GetDisposeFunctionPointer(inst);
            ((delegate * <object, void>)disposeMethod)(inst);
        }
#pragma warning disable CA1031
        catch (Exception ex)
        {
            ReportDisposalFailure(tc, fw, ex);
        }
#pragma warning restore CA1031
    }

    [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.AggressiveOptimization)]
    [StackTraceHidden, DebuggerStepThrough, DebuggerHidden, DebuggerNonUserCode]
    private static unsafe void TestRunnerWithLogger(object? o)
    {
        NotifyExecutingFromTestFrameworkThread();
        var (mi, fw, tc, jobClass, ct)
            = Unsafe.Unbox<(MethodInfo, IFrameworkHandle?, TestCase, Type, CancellationToken)>(o!);

        var thread = Thread.CurrentThread;
        thread.Name = $"{tc.FullyQualifiedName}";

        var inst = ActivateTestClassInstance(jobClass, fw, tc);
        if (inst is null) return;

        var fp = PrepareMethodAndGetPointer(mi, fw, tc);

        if (fp == default)
            return;

        var failed = false;
        using var sw = new StringWriter();

        SyncTimestampBoundary();

        fw?.RecordStart(tc);
        DateTimeOffset started = default;
        long startedTs = default;
        try
        {
            ct.ThrowIfCancellationRequested();
            started = DateTimeOffset.Now;
            startedTs = Stopwatch.GetTimestamp();
            ((delegate * <object, TextWriter, void>)fp)(inst, sw);
        }
        catch (OperationCanceledException ex) when (ex.CancellationToken == ct)
        {
            if (fw is null)
                return;

            var endedTs = Stopwatch.GetTimestamp();
            var ended = DateTimeOffset.Now;
            fw.RecordEnd(tc, TestOutcome.Failed);
            failed = true;
            ReportTestOperationCancelledException(started, ended, startedTs, endedTs, fw, tc, ex, sw);
        }
#pragma warning disable CA1031
        catch (Exception ex)
        {
            if (fw is null)
                return;

            var endedTs = Stopwatch.GetTimestamp();
            var ended = DateTimeOffset.Now;
            fw.RecordEnd(tc, TestOutcome.Failed);
            failed = true;
            ReportTestException(started, ended, startedTs, endedTs, fw, tc, ex, sw);
        }
#pragma warning restore CA1031

        // ReSharper disable once InvertIf // naming collisions
        if (!failed)
        {
            if (fw is null)
                return;

            var endedTs = Stopwatch.GetTimestamp();
            var ended = DateTimeOffset.Now;
            fw.RecordEnd(tc, TestOutcome.Passed);

            ReportSuccess(started, ended, startedTs, endedTs, fw, tc, sw);
        }

        if (inst is not IDisposable)
            return;

        try
        {
            var disposeMethod = GetDisposeFunctionPointer(inst);
            ((delegate * <object, void>)disposeMethod)(inst);
        }
#pragma warning disable CA1031
        catch (Exception ex)
        {
            ReportDisposalFailure(tc, fw, ex);
        }
#pragma warning restore CA1031
    }

    [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.AggressiveOptimization)]
    [StackTraceHidden, DebuggerStepThrough, DebuggerHidden, DebuggerNonUserCode]
    private static unsafe void TestRunnerWithCancellation(object? o)
    {
        NotifyExecutingFromTestFrameworkThread();
        var (mi, fw, tc, jobClass, ct)
            = Unsafe.Unbox<(MethodInfo, IFrameworkHandle?, TestCase, Type, CancellationToken)>(o!);

        var thread = Thread.CurrentThread;
        thread.Name = $"Test: {tc.FullyQualifiedName}";

        var inst = ActivateTestClassInstance(jobClass, fw, tc);
        if (inst is null) return;

        var fp = PrepareMethodAndGetPointer(mi, fw, tc);

        if (fp == default)
            return;

        var failed = false;
        using var sw = new StringWriter();

        SyncTimestampBoundary();

        fw?.RecordStart(tc);
        DateTimeOffset started = default;
        long startedTs = default;
        try
        {
            ct.ThrowIfCancellationRequested();
            started = DateTimeOffset.Now;
            startedTs = Stopwatch.GetTimestamp();
            ((delegate * <object, CancellationToken, void>)fp)(inst, ct);
        }
        catch (OperationCanceledException ex) when (ex.CancellationToken == ct)
        {
            if (fw is null)
                return;

            var endedTs = Stopwatch.GetTimestamp();
            var ended = DateTimeOffset.Now;
            fw.RecordEnd(tc, TestOutcome.Failed);
            failed = true;
            ReportTestOperationCancelledException(started, ended, startedTs, endedTs, fw, tc, ex, sw);
        }
#pragma warning disable CA1031
        catch (Exception ex)
        {
            if (fw is null)
                return;

            var endedTs = Stopwatch.GetTimestamp();
            var ended = DateTimeOffset.Now;
            fw.RecordEnd(tc, TestOutcome.Failed);
            failed = true;
            ReportTestException(started, ended, startedTs, endedTs, fw, tc, ex, sw);
        }
#pragma warning restore CA1031

        // ReSharper disable once InvertIf // naming collisions
        if (!failed)
        {
            if (fw is null)
                return;

            var endedTs = Stopwatch.GetTimestamp();
            var ended = DateTimeOffset.Now;
            fw.RecordEnd(tc, TestOutcome.Passed);

            ReportSuccess(started, ended, startedTs, endedTs, fw, tc, sw);
        }

        if (inst is not IDisposable)
            return;

        try
        {
            var disposeMethod = GetDisposeFunctionPointer(inst);
            ((delegate * <object, void>)disposeMethod)(inst);
        }
#pragma warning disable CA1031
        catch (Exception ex)
        {
            ReportDisposalFailure(tc, fw, ex);
        }
#pragma warning restore CA1031
    }

    [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.AggressiveOptimization)]
    [StackTraceHidden, DebuggerStepThrough, DebuggerHidden, DebuggerNonUserCode]
    private static unsafe void TestRunnerWithLoggerAndCancellation(object? o)
    {
        NotifyExecutingFromTestFrameworkThread();
        var (mi, fw, tc, jobClass, ct)
            = Unsafe.Unbox<(MethodInfo, IFrameworkHandle?, TestCase, Type, CancellationToken)>(o!);

        var thread = Thread.CurrentThread;
        thread.Name = $"Test: {tc.FullyQualifiedName}";

        var inst = ActivateTestClassInstance(jobClass, fw, tc);
        if (inst is null) return;

        var fp = PrepareMethodAndGetPointer(mi, fw, tc);

        if (fp == default)
            return;

        var failed = false;
        using var sw = new StringWriter();

        SyncTimestampBoundary();

        fw?.RecordStart(tc);
        DateTimeOffset started = default;
        long startedTs = default;
        try
        {
            ct.ThrowIfCancellationRequested();
            started = DateTimeOffset.Now;
            startedTs = Stopwatch.GetTimestamp();
            ((delegate * <object, TextWriter, CancellationToken, void>)fp)(inst, sw, ct);
        }
        catch (OperationCanceledException ex) when (ex.CancellationToken == ct)
        {
            if (fw is null)
                return;

            var endedTs = Stopwatch.GetTimestamp();
            var ended = DateTimeOffset.Now;
            fw.RecordEnd(tc, TestOutcome.Failed);
            failed = true;
            ReportTestOperationCancelledException(started, ended, startedTs, endedTs, fw, tc, ex, sw);
        }
#pragma warning disable CA1031
        catch (Exception ex)
        {
            if (fw is null)
                return;

            var endedTs = Stopwatch.GetTimestamp();
            var ended = DateTimeOffset.Now;
            fw.RecordEnd(tc, TestOutcome.Failed);
            failed = true;
            ReportTestException(started, ended, startedTs, endedTs, fw, tc, ex, sw);
        }
#pragma warning restore CA1031

        // ReSharper disable once InvertIf // naming collisions
        if (!failed)
        {
            if (fw is null)
                return;

            var endedTs = Stopwatch.GetTimestamp();
            var ended = DateTimeOffset.Now;
            fw.RecordEnd(tc, TestOutcome.Passed);

            ReportSuccess(started, ended, startedTs, endedTs, fw, tc, sw);
        }

        if (inst is not IDisposable)
            return;

        try
        {
            var disposeMethod = GetDisposeFunctionPointer(inst);
            ((delegate * <object, void>)disposeMethod)(inst);
        }
#pragma warning disable CA1031
        catch (Exception ex)
        {
            ReportDisposalFailure(tc, fw, ex);
        }
#pragma warning restore CA1031
    }


    private static readonly ConcurrentDictionary<Type, nint> DisposeMethodPointers = new();
    private static nint GetDisposeFunctionPointer(object inst)
        => GetDisposeFunctionPointer(inst.GetType());

    private static nint GetDisposeFunctionPointer(Type disposableType)
        => DisposeMethodPointers.GetOrAdd(disposableType, static disposableType => {
            var map = disposableType.GetInterfaceMap(typeof(IDisposable));
            var m = Array.FindIndex(map.InterfaceMethods, x => x.Name == "Dispose");
            var disposeMethod = map.TargetMethods[m].MethodHandle.GetFunctionPointer();
            return disposeMethod;
        });
}
