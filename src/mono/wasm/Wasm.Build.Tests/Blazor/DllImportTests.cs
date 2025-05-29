// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;
using Xunit.Sdk;
using Microsoft.Playwright;

#nullable enable

namespace Wasm.Build.Tests.Blazor;

public class DllImportTests : BlazorWasmTestBase
{
    public DllImportTests(ITestOutputHelper output, SharedBuildPerTestClassFixture buildContext)
        : base(output, buildContext)
    {
        _enablePerTestCleanup = true;
    }

    public static TheoryData<Configuration, bool, bool> DllImportTheoryData()
    {
        var data = new TheoryData<Configuration, bool, bool>();
        data.Add(Configuration.Debug, /*build*/true, /*publish*/false);
        data.Add(Configuration.Release, /*build*/true, /*publish*/false);
        data.Add(Configuration.Release, /*build*/false, /*publish*/true);
        data.Add(Configuration.Release, /*build*/true, /*publish*/true);
        return data;
    }

    [Theory]
    [MemberData(nameof(DllImportTheoryData))]
    public async Task WithDllImportInMainAssembly(Configuration config, bool build, bool publish)
    {
        // Based on https://github.com/dotnet/runtime/issues/59255
        string prefix = $"blz_dllimp_{config}_{s_unicodeChars}";
        if (build && publish)
            prefix += "build_then_publish";
        else if (build)
            prefix += "build";
        else
            prefix += "publish";
        string extraItems = @"<NativeFileReference Include=""mylib.cpp"" />";
        ProjectInfo info = CopyTestAsset(config, aot: false, TestAsset.BlazorBasicTestApp, prefix, extraItems: extraItems);
        File.Copy(Path.Combine(BuildEnvironment.TestAssetsPath, "EntryPoints", "MyDllImport.cs"), Path.Combine(_projectDir, "Pages", "MyDllImport.cs"));
        File.Copy(Path.Combine(BuildEnvironment.TestAssetsPath, "native-libs", "mylib.cpp"), Path.Combine(_projectDir, "mylib.cpp"));
        UpdateFile(Path.Combine("Pages", "MyDllImport.cs"), new Dictionary<string, string> { { "##NAMESPACE##", info.ProjectName } });

        BlazorAddRazorButton("cpp_add", """
            var result = MyDllImports.cpp_add(10, 12);
            outputText = $"{result}";
        """);

        if (build)
            BlazorBuild(info, config, isNativeBuild: true);

        if (publish)
            BlazorPublish(info, config, new PublishOptions(UseCache: false), isNativeBuild: true);

        BlazorRunOptions runOptions = new(config, Test: TestDllImport);
        if (publish)
            await RunForPublishWithWebServer(runOptions);
        else
            await RunForBuildWithDotnetRun(runOptions);

        async Task TestDllImport(IPage page)
        {
            await page.Locator("text=\"cpp_add\"").ClickAsync();
            var txt = await page.Locator("p[role='test']").InnerHTMLAsync();
            Assert.Equal("Output: 22", txt);
        }
    }

    private void BlazorAddRazorButton(string buttonText, string customCode, string methodName = "test") =>
        UpdateFile(Path.Combine("Pages", "Counter.razor"), new Dictionary<string, string> {
                {
                    @"<button class=""btn btn-primary"" @onclick=""IncrementCount"">Click me</button>",
                    $@"
                        <button class=""btn btn-primary"" @onclick=""IncrementCount"">Click me</button>
                        <p role=""{methodName}"">Output: @outputText</p>
                        <button class=""btn btn-primary"" @onclick=""{methodName}"">{buttonText}</button>
                    "
                },
                {
                    "private int currentCount = 0;",
                    $@"
                        private int currentCount = 0;
                        private string outputText = string.Empty;
                        public void {methodName}()
                        {{
                            {customCode}
                        }}
                    "
                }
            });
}
