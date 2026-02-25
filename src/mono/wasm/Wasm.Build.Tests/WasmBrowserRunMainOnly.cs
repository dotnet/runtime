using System.Collections.Specialized;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace Wasm.Build.Tests;

public class WasmBrowserRunMainOnly(ITestOutputHelper output, SharedBuildPerTestClassFixture buildContext) : WasmTemplateTestsBase(output, buildContext)
{
    [TestCategory("coreclr")]
    [Theory]
    [BuildAndRun(config: Configuration.Release)]
    [BuildAndRun(config: Configuration.Debug)]
    public async Task RunMainOnly(Configuration config, bool aot)
    {
        ProjectInfo info = CopyTestAsset(config, aot, TestAsset.WasmBrowserRunMainOnly, $"WasmBrowserRunMainOnly");
        var (_, buildOutput) = PublishProject(info, config, new PublishOptions(AssertAppBundle: false, EnableDiagnostics: true));

        // ** MicrosoftNetCoreAppRuntimePackDir : '....microsoft.netcore.app.runtime.browser-wasm\11.0.0-dev'
        Assert.Contains("microsoft.netcore.app.runtime.browser-wasm", buildOutput);

        var result = await RunForPublishWithWebServer(new BrowserRunOptions(
            Configuration: config
        ));

        Assert.Collection(
            result.TestOutput,
            m => Assert.Equal("Hello from WasmBrowserRunMainOnly!", m)
        );
    }
}
