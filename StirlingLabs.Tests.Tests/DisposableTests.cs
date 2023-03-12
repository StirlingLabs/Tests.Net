using System.Diagnostics.CodeAnalysis;

namespace StirlingLabs.Tests.Tests;

public sealed class DisposableTests : IDisposable
{
    private static int _constructed;
    private static int _disposed;

    public DisposableTests()
        => Interlocked.Increment(ref _constructed);

    void IDisposable.Dispose()
    {
        Interlocked.Increment(ref _disposed);
        
        _constructed.Should().Be(_disposed);
        
        StirlingLabsTestRunner.ExecutingFromTestFrameworkThread.Should().BeTrue();
    }


    [SuppressMessage("ReSharper", "JoinDeclarationAndInitializer")]
    public void One_test()
    {
        // arrange
        bool hasShutdownStarted;

        // act
        hasShutdownStarted = Environment.HasShutdownStarted;

        // assert
        hasShutdownStarted.Should().BeFalse();
        _constructed.Should().BeGreaterThan(0);
        _disposed.Should().BeLessThanOrEqualTo(0);
        StirlingLabsTestRunner.ExecutingFromTestFrameworkThread.Should().BeTrue();
    }
}
