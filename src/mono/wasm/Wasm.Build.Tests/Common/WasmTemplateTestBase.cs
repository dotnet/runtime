// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable enable

using System.IO;
using Xunit.Abstractions;

namespace Wasm.Build.Tests;

public abstract class WasmTemplateTestBase : BuildTestBase
{
    protected WasmTemplateTestBase(ITestOutputHelper output, SharedBuildPerTestClassFixture buildContext)
                : base(output, buildContext)
    {
    }

    public string CreateWasmTemplateProject(string id, string template = "wasmbrowser", string extraArgs = "", bool runAnalyzers = true)
    {
        InitPaths(id);
        InitProjectDir(_projectDir, addNuGetSourceForLocalPackages: true);

        File.WriteAllText(Path.Combine(_projectDir, "Directory.Build.props"), "<Project />");
        File.WriteAllText(Path.Combine(_projectDir, "Directory.Build.targets"),
            """
            <Project>
              <Target Name="PrintRuntimePackPath" BeforeTargets="Build">
                  <Message Text="** MicrosoftNetCoreAppRuntimePackDir : '@(ResolvedRuntimePack -> '%(PackageDirectory)')'" Importance="High" Condition="@(ResolvedRuntimePack->Count()) > 0" />
              </Target>
            </Project>
            """);

        new DotNetCommand(s_buildEnv, _testOutput, useDefaultArgs: false)
                .WithWorkingDirectory(_projectDir!)
                .ExecuteWithCapturedOutput($"new {template} {extraArgs}")
                .EnsureSuccessful();

        string projectfile = Path.Combine(_projectDir!, $"{id}.csproj");
        string extraProperties = string.Empty;
        extraProperties += "<TreatWarningsAsErrors>true</TreatWarningsAsErrors>";
        if (runAnalyzers)
            extraProperties += "<RunAnalyzers>true</RunAnalyzers>";
        if (!UseWebcil)
            extraProperties += "<WasmEnableWebcil>false</WasmEnableWebcil>";

        // TODO: Can be removed after updated templates propagate in.
        string extraItems = string.Empty;
        if (template == "wasmbrowser")
            extraItems += "<WasmExtraFilesToDeploy Include=\"main.js\" />";
        else
            extraItems += "<WasmExtraFilesToDeploy Include=\"main.mjs\" />";

        AddItemsPropertiesToProject(projectfile, extraProperties, extraItems);

        return projectfile;
    }
}
