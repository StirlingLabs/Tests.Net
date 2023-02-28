namespace StirlingLabs.Tests;

[FileExtension(".dll"), FileExtension(".exe")]
[DefaultExecutorUri(UriString)]
public sealed partial class StirlingLabsTestRunner : ITestDiscoverer
{

    [GeneratedRegex(
        @"_|\\u[0-9A-Fa-f]{4,4}",
        RegexOptions.CultureInvariant
        | RegexOptions.ExplicitCapture
    )]
    private static partial Regex TestMethodDisplayNameRegex();

    private static string GetTestMethodDisplayName(MethodInfo method)
        => TestMethodDisplayNameRegex().Replace(method.Name, static v =>
        {
            if (v.ValueSpan[0] == '_')
            {
                return " ";
            }

            // "\u0000" - "\uFFFF"
            var hexDigits = v.ValueSpan.Slice(2);
            Debug.Assert(hexDigits.Length == 4);
            var bytes = Convert.FromHexString(hexDigits);
            Debug.Assert(bytes.Length == 2);
            return ((char)MemoryMarshal.Read<ushort>(bytes)).ToString();
        });

    /// <summary>
    /// Discovers the tests available from the provided container.
    /// </summary>
    /// <param name="containers">Collection of test containers.</param>
    /// <param name="discoveryContext">Context in which discovery is being performed.</param>
    /// <param name="logger">Logger used to log messages.</param>
    /// <param name="discoverySink">Used to send testcases and discovery related events back to Discoverer manager.</param>
    public void DiscoverTests(IEnumerable<string>? containers, IDiscoveryContext? discoveryContext, IMessageLogger? logger, ITestCaseDiscoverySink discoverySink)
    {
        if (containers is null) return;

        var alc = new AssemblyLoadContext("StirlingLabsTestDiscoverer", true);
        foreach (string container in containers)
        {
            var asm = alc.LoadFromAssemblyPath(container);
            foreach (var export in asm.ExportedTypes)
            {
                // test cases should be in sealed classes, but not abstract or static
                if (export is not { IsClass: true, IsAbstract: false, IsSealed: true }
                    || !export.Name.EndsWith("Tests")
                    || export.Namespace?.EndsWith(".Tests") != true)
                    continue;

                logger?.SendMessage(TestMessageLevel.Informational, $"class {container}!{export.FullName}");
                foreach (var method in export.GetMethods(BindingFlags.Public | BindingFlags.Instance))
                {
                    if (!ValidateTestMethod(method))
                        continue;

                    logger?.SendMessage(TestMessageLevel.Informational, $"method {method.Name}");
                    var dnAttrib = method.GetCustomAttribute<DisplayNameAttribute>();
                    var tc = new TestCase($"{export.FullName}.{method.Name}", StirlingLabsTestRunner.Uri, container)
                    {
                        DisplayName = dnAttrib?.DisplayName ?? GetTestMethodDisplayName(method)
                        // TODO: locate source project? use roslyn to locate source location?
                    };
                    discoverySink.SendTestCase(tc);
                }
            }
        }
    }

    private static bool ValidateTestMethod(MethodInfo method)
    {
        if (method.ReturnParameter.ParameterType != typeof(void))
            return false;

        var methodParams = method.GetParameters();

        switch (methodParams.Length)
        {
            case 0:
                // this works just fine
                break;
            case 1:
            {
                var firstParamType = methodParams[0].ParameterType;
                if (firstParamType != typeof(TextWriter)
                    && firstParamType != typeof(CancellationToken))
                    return false;

                break;
            }
            case 2:
            {
                if (methodParams[0].ParameterType != typeof(TextWriter)
                    || methodParams[1].ParameterType != typeof(CancellationToken))
                    return false;

                break;
            }
            default: return false;
        }

        return true;
    }

}
