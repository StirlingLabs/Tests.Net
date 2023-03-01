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
        var (mi, fw, tc, jobClass, ct)
            = ((MethodInfo, IFrameworkHandle?, TestCase, Type, CancellationToken))o!;

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
    }

    [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.AggressiveOptimization)]
    [StackTraceHidden, DebuggerStepThrough, DebuggerHidden, DebuggerNonUserCode]
    private static unsafe void TestRunnerWithLogger(object? o)
    {
        var (mi, fw, tc, jobClass, ct)
            = ((MethodInfo, IFrameworkHandle?, TestCase, Type, CancellationToken))o!;

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
    }

    [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.AggressiveOptimization)]
    [StackTraceHidden, DebuggerStepThrough, DebuggerHidden, DebuggerNonUserCode]
    private static unsafe void TestRunnerWithCancellation(object? o)
    {
        var (mi, fw, tc, jobClass, ct)
            = ((MethodInfo, IFrameworkHandle?, TestCase, Type, CancellationToken))o!;

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
    }

    [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.AggressiveOptimization)]
    [StackTraceHidden, DebuggerStepThrough, DebuggerHidden, DebuggerNonUserCode]
    private static unsafe void TestRunnerWithLoggerAndCancellation(object? o)
    {
        var (mi, fw, tc, jobClass, ct)
            = ((MethodInfo, IFrameworkHandle?, TestCase, Type, CancellationToken))o!;

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
    }

}
