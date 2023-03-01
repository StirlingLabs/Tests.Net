using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace StirlingLabs.Tests.Tests;

public sealed class ActualTests
{
    public void We_have_a_processor()
    {
        // arrange
        int processorCount;

        // act
        processorCount = Environment.ProcessorCount;

        // assert
        processorCount.Should().BeGreaterThan(0);
    }

    public void Logging_test(TextWriter logger)
    {
        logger.Should().NotBeNull();

        logger.WriteLine($"Hello from {nameof(Logging_test)}");
    }

    public void Cancellation_test(CancellationToken cancellationToken)
    {
        cancellationToken.Should().NotBe(default);
        if (cancellationToken.IsCancellationRequested)
            return;
        if (!Thread.Yield())
            Thread.Sleep(1);
        cancellationToken.ThrowIfCancellationRequested();
        throw new OperationCanceledException("This test is intentionally cancelled.", cancellationToken);
    }

    public void Test_with_logger_and_cancellation_token(TextWriter logger, CancellationToken cancellationToken)
    {
        logger.Should().NotBeNull();
        cancellationToken.Should().NotBe(default);
        throw new OperationCanceledException("This test is intentionally cancelled.", cancellationToken);
    }

    public void Inconclusive_test1()
    {
        throw new AssertInconclusiveException("This test is intentionally inconclusive.");
    }

    public void Inconclusive_test2(TextWriter logger)
    {
        throw new AssertInconclusiveException("This test is intentionally inconclusive.");
    }

    public void Inconclusive_test3(CancellationToken cancellationToken)
    {
        throw new AssertInconclusiveException("This test is intentionally inconclusive.");
    }

    public void Inconclusive_test3(TextWriter logger, CancellationToken cancellationToken)
    {
        throw new AssertInconclusiveException("This test is intentionally inconclusive.");
    }
}
