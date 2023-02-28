namespace StirlingLabs.Tests;

public class StirlingLabsTestDiscoverySink : ITestCaseDiscoverySink
{

    private readonly List<TestCase> _testCases = new();

    public ReadOnlyCollection<TestCase> TestCases
        => _testCases.AsReadOnly();

    public void SendTestCase(TestCase discoveredTest)
        => _testCases.Add(discoveredTest);

}
