namespace StirlingLabs.Tests;

public partial class StirlingLabsTestRunner
{

    private static string GetClassName(TestCase tc)
    {
        var fqn = tc.FullyQualifiedName;
        var lastDot = fqn.LastIndexOf('.');
        return fqn.Substring(0, lastDot);
    }

    private static string GetMethodName(TestCase tc)
    {
        var fqn = tc.FullyQualifiedName;
        var lastDot = fqn.LastIndexOf('.');
        return fqn.Substring(lastDot + 1);
    }

}
