// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

#nullable enable

namespace Wasm.Build.Tests.Blazor;

public class AppsettingsTests : BlazorWasmTestBase
{
    public AppsettingsTests(ITestOutputHelper output, SharedBuildPerTestClassFixture buildContext)
        : base(output, buildContext, DefaultTargetFrameworkForBlazorTemplate)
    {
        _enablePerTestCleanup = true;
    }

    [Fact]
    public async Task FileInVfs()
    {
        Configuration config = Configuration.Debug;
        ProjectInfo info = CreateWasmTemplateProject(Template.BlazorWasm, config, aot: false, "blazor");
        UpdateHomePage();
        string projectDirectory = Path.GetDirectoryName(info.ProjectFilePath)!;
        string settings = $"{{ \"Id\": \"{info.ProjectName}\" }}";
        File.WriteAllText(Path.Combine(projectDirectory, "wwwroot", "appsettings.json"), settings);
        File.WriteAllText(Path.Combine(projectDirectory, "wwwroot", "appsettings.Development.json"), "{}");
        File.WriteAllText(Path.Combine(projectDirectory, "wwwroot", "appsettings.Production.json"), "{}");
        UpdateFile("Program.cs", new Dictionary<string, string>
        {
            {
                "var builder",
                """
                    System.Console.WriteLine($"appSettings Exists '{File.Exists("/appsettings.json")}'");
                    System.Console.WriteLine($"appSettings Content '{File.ReadAllText("/appsettings.json")}'");
                    var builder
                """
            }
        });

        (string _, string buildOutput) = BlazorBuild(info, config);
        bool existsChecked = false;
        bool contentChecked = false;
        await RunForBuildWithDotnetRun(new BlazorRunOptions(
            config,
            OnConsoleMessage: (_, msg) => {
                if (msg.Contains("appSettings Exists 'True'"))
                    existsChecked = true;
                else if (msg.Contains($"appSettings Content '{{ \"Id\": \"{info.ProjectName}\" }}'"))
                    contentChecked = true;
            },
            Test: async page => {
                string[] files = await page.EvaluateAsync<string[]>("() => getDotnetRuntime(0).INTERNAL.getAppSettingsFiles()");
                Assert.Equal(new[] { "appsettings.json", "appsettings.Development.json" }, files);
                string content = await page.EvaluateAsync<string>("() => new TextDecoder().decode(getDotnetRuntime(0).INTERNAL.getAppSettingsBytes('appsettings.json'))");
                Assert.Equal(settings, content);
            }));

        Assert.True(existsChecked, "File '/appsettings.json' wasn't found");
        Assert.True(contentChecked, "Content of '/appsettings.json' is not matched");
    }
}
