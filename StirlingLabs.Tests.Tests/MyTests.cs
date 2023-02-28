namespace StirlingLabs.Tests.Tests;

public sealed class MyTests
{
    public MyTests()
    {
        // OneTimeSetUp equivalent
    }

    public void MyTest1()
    {
        // test code
    }

    public void MyTest2(TextWriter logger)
    {
        // test code with logging
    }

    public void MyTest3(CancellationToken cancellationToken)
    {
        // test code with cancellation
    }

    public void MyTest4(TextWriter logger, CancellationToken cancellationToken)
    {
        // test code with logging and cancellation
    }
}
