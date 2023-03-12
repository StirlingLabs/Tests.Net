using Microsoft.VisualStudio.TestPlatform.CrossPlatEngine.Discovery;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Logging;
using Moq;

namespace StirlingLabs.Tests.Tests;

public sealed class DiscoverySinkTests
{
    public void It_should_discover_tests(TextWriter logger)
    {
        // arrange
        var runner = new StirlingLabsTestRunner();
        var asm = typeof(DiscoverySinkTests).Assembly;
        var discoveryContext = new DiscoveryContext();
        var sink = new StirlingLabsTestDiscoverySink();
        var mockLogger = new Mock<IMessageLogger>();

        mockLogger.Setup(l => l.SendMessage(It.IsAny<TestMessageLevel>(), It.IsAny<string>()));

        // act
        runner.DiscoverTests(new[] { asm.Location }, discoveryContext, mockLogger.Object, sink);

        // assert
        sink.TestCases.Should().NotBeEmpty();

    }
}
