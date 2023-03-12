using InlineIL;

namespace StirlingLabs.Tests.Tests;

public sealed class BadClassTests
{
    public BadClassTests()
    {
        throw new Exception("This test class is intentionally broken.");
    }
    
    public void Test_should_not_run()
    {
        // intentionally bad IL
        IL.Emit.Rethrow();
        
        throw new Exception("This test is intentionally broken.");
    }
}
