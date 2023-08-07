// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable enable

using System.IO;
using Xunit.Abstractions;

namespace Wasm.Build.Tests;

public abstract class WasmTemplateTestBase : BuildTestBase
{
    private readonly WasmSdkBasedProjectProvider _provider;
    protected WasmTemplateTestBase(ITestOutputHelper output, SharedBuildPerTestClassFixture buildContext, WasmSdkBasedProjectProvider? projectProvider = null)
                : base(projectProvider ?? new WasmSdkBasedProjectProvider(output), output, buildContext)
    {
        _provider = GetProvider<WasmSdkBasedProjectProvider>();
        // Wasm templates are not using wasm sdk yet
        _provider.BundleDirName = "AppBundle";
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

        // TODO: Can be removed after updated templates propagate in.
        string extraItems = string.Empty;
        if (template == "wasmbrowser")
            extraItems += "<WasmExtraFilesToDeploy Include=\"main.js\" />";
        else
            extraItems += "<WasmExtraFilesToDeploy Include=\"main.mjs\" />";

        AddItemsPropertiesToProject(projectfile, extraProperties, extraItems);

        return projectfile;
    }

    public (string projectDir, string buildOutput) BuildTemplateProject(BuildArgs buildArgs,
        string id,
        BuildProjectOptions buildProjectOptions,
        AssertTestMainJsAppBundleOptions? assertAppBundleOptions = null)
    {
        (CommandResult res, string logFilePath) = BuildProjectWithoutAssert(id, buildArgs.Config, buildProjectOptions);
        if (buildProjectOptions.UseCache)
            _buildContext.CacheBuild(buildArgs, new BuildProduct(_projectDir!, logFilePath, true, res.Output));

        if (buildProjectOptions.AssertAppBundle)
            AssertBundle(buildArgs, buildProjectOptions, res.Output, assertAppBundleOptions);
        return (_projectDir!, res.Output);
    }

    public void AssertBundle(BuildArgs buildArgs,
                              BuildProjectOptions buildProjectOptions,
                              string? buildOutput = null,
                              AssertTestMainJsAppBundleOptions? assertAppBundleOptions = null)
    {
        if (buildOutput is not null)
            ProjectProviderBase.AssertRuntimePackPath(buildOutput, buildProjectOptions.TargetFramework ?? DefaultTargetFramework);

        // TODO: templates don't use wasm sdk yet
        var testMainJsProvider = new TestMainJsProjectProvider(_testOutput, _projectDir!);
        if (assertAppBundleOptions is not null)
            testMainJsProvider.AssertBundle(assertAppBundleOptions);
        else
            testMainJsProvider.AssertBundle(buildArgs, buildProjectOptions);
    }
}
