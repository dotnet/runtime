// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.IO;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

#nullable enable

namespace Wasm.Build.Tests.Blazor;

public class AppsettingsTests : BlazorWasmTestBase
{
    public AppsettingsTests(ITestOutputHelper output, SharedBuildPerTestClassFixture buildContext)
        : base(output, buildContext)
    {
        _enablePerTestCleanup = true;
    }

    [Fact]
    public async Task FileInVfs()
    {
        string id = $"blazor_{GetRandomId()}";
        string projectFile = CreateWasmTemplateProject(id, "blazorwasm");

        string projectDirectory = Path.GetDirectoryName(projectFile)!;

        File.WriteAllText(Path.Combine(projectDirectory, "wwwroot", "appsettings.json"), $"{{ \"Id\": \"{id}\" }}");

        string programPath = Path.Combine(projectDirectory, "Program.cs");
        string programContent = File.ReadAllText(programPath);
        programContent = programContent.Replace("var builder",
        """
        System.Console.WriteLine($"appSettings Exists '{File.Exists("/appsettings.json")}'");
        System.Console.WriteLine($"appSettings Content '{File.ReadAllText("/appsettings.json")}'");
        var builder
        """);
        File.WriteAllText(programPath, programContent);

        BlazorBuild(new BlazorBuildOptions(id, "debug", NativeFilesType.FromRuntimePack));

        bool existsChecked = false;
        bool contentChecked = false;

        await BlazorRunForBuildWithDotnetRun(new BlazorRunOptions()
        {
            Config = "debug",
            OnConsoleMessage = (_, msg) =>
            {
                if (msg.Text.Contains("appSettings Exists 'True'"))
                    existsChecked = true;
                else if (msg.Text.Contains($"appSettings Content '{{ \"Id\": \"{id}\" }}'"))
                    contentChecked = true;
            }
        });

        Assert.True(existsChecked, "File '/appsettings.json' wasn't found");
        Assert.True(contentChecked, "Content of '/appsettings.json' is not matched");
    }
}
