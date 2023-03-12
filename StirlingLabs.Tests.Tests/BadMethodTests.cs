using InlineIL;

namespace StirlingLabs.Tests.Tests;

public sealed class BadMethodTests
{
    public void Test_should_not_run1()
    {
        // intentionally bad IL
        IL.Emit.Break();
        try
        {
            IL.Emit.Ret();
        }
        finally
        {
            IL.Emit.Ret();
        }
        IL.Emit.Ret();
    }
    
    public void Test_should_not_run2(TextWriter logger)
    {
        // intentionally bad IL
        IL.Emit.Break();
        try
        {
            IL.Emit.Ret();
        }
        finally
        {
            IL.Emit.Ret();
        }
        IL.Emit.Ret();
    }

    public void Test_should_not_run3(CancellationToken cancellationToken)
    {
        // intentionally bad IL
        IL.Emit.Break();
        try
        {
            IL.Emit.Ret();
        }
        finally
        {
            IL.Emit.Ret();
        }
        IL.Emit.Ret();
    }

    public void Test_should_not_run4(TextWriter logger, CancellationToken cancellationToken)
    {
        // intentionally bad IL
        IL.Emit.Break();
        try
        {
            IL.Emit.Ret();
        }
        finally
        {
            IL.Emit.Ret();
        }
        IL.Emit.Ret();
    }
}
