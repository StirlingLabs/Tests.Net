using System.Diagnostics;

namespace StirlingLabs.Tests;

public partial class StirlingLabsTestRunner
{

    private static void ReportTestOperationCancelledException(DateTimeOffset started, DateTimeOffset ended, long startedTs, long endedTs, IFrameworkHandle fw, TestCase tc, OperationCanceledException ex, StringWriter sw)
    {
        var errMsg = "<unset>";
        var errStackTrace = "<unset>";
        try
        {
            // ReSharper disable once NullCoalescingConditionIsAlwaysNotNullAccordingToAPIContract
            errMsg = ex.Message ?? "<missing>";
            errStackTrace = ex.StackTrace ?? "<missing>";
        }
        catch
        {
            // oof
        }

        var elapsed = Stopwatch.GetElapsedTime(startedTs, endedTs);
        fw.RecordResult(new(tc)
        {
            Outcome = TestOutcome.Skipped,
            StartTime = started,
            EndTime = ended,
            Duration = elapsed,
            ErrorMessage = errMsg,
            ErrorStackTrace = errStackTrace,
            Messages =
            {
                new TestResultMessage(TestResultMessage.StandardOutCategory, sw.ToString()),
                new TestResultMessage(TestResultMessage.AdditionalInfoCategory, $"{elapsed.Ticks} ticks")
            }
        });
    }

    private static void ReportTestException(DateTimeOffset started, DateTimeOffset ended, long startedTs, long endedTs, IFrameworkHandle fw, TestCase tc, Exception ex, StringWriter sw)
    {
        var errMsg = "<unset>";
        var errStackTrace = "<unset>";
        try
        {
            // ReSharper disable once NullCoalescingConditionIsAlwaysNotNullAccordingToAPIContract
            errMsg = ex.Message ?? "<missing>";
            errStackTrace = ex.StackTrace ?? "<missing>";
        }
        catch
        {
            // oof
        }

        var elapsed = Stopwatch.GetElapsedTime(startedTs, endedTs);
        fw.RecordResult(new(tc)
        {
            Outcome = TestOutcome.Failed,
            StartTime = started,
            EndTime = ended,
            Duration = elapsed,
            ErrorMessage = errMsg,
            ErrorStackTrace = errStackTrace,
            Messages =
            {
                new TestResultMessage(TestResultMessage.StandardOutCategory, sw.ToString()),
                new TestResultMessage(TestResultMessage.AdditionalInfoCategory, $"{elapsed.Ticks} ticks")
            }
        });
    }

    private static void ReportSuccess(DateTimeOffset started, DateTimeOffset ended, long startedTs, long endedTs, IFrameworkHandle fw, TestCase tc, StringWriter sw)
    {
        var elapsed = Stopwatch.GetElapsedTime(startedTs, endedTs);
        fw.RecordResult(new(tc)
        {
            Outcome = TestOutcome.Passed,
            StartTime = started,
            EndTime = ended,
            Duration = elapsed,
            Messages =
            {
                new TestResultMessage(TestResultMessage.StandardOutCategory, sw.ToString()),
                new TestResultMessage(TestResultMessage.AdditionalInfoCategory, $"{elapsed.Ticks} ticks")
            }
        });
    }

}
