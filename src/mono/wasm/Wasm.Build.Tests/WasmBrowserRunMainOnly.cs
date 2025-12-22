using System.Collections.Specialized;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace Wasm.Build.Tests;

public class WasmBrowserRunMainOnly(ITestOutputHelper output, SharedBuildPerTestClassFixture buildContext) : WasmTemplateTestsBase(output, buildContext)
{
    [Fact, TestCategory("coreclr")]
    public async Task RunMainOnly()
    {
        Configuration config = Configuration.Debug;

        ProjectInfo info = CopyTestAsset(config, false, TestAsset.WasmBrowserRunMainOnly, $"WasmBrowserRunMainOnly");
        var (_, buildOutput) = PublishProject(info, config);

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
