// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;
using Xunit.Sdk;

#nullable enable

namespace Wasm.Build.Tests;

public class WasmTemplateTestBase : BuildTestBase
{
    private readonly WasmSdkBasedProjectProvider _provider;
    protected WasmTemplateTestBase(ITestOutputHelper output, SharedBuildPerTestClassFixture buildContext, WasmSdkBasedProjectProvider? projectProvider = null)
                : base(projectProvider ?? new WasmSdkBasedProjectProvider(output), output, buildContext)
    {
        _provider = GetProvider<WasmSdkBasedProjectProvider>();
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

    public (string projectDir, string buildOutput) BuildTemplateProject(BuildArgs buildArgs,
                              string id,
                              BuildProjectOptions buildProjectOptions,
                              AssertTestMainJsAppBundleOptions? assertAppBundleOptions = null)
    {
        StringBuilder buildCmdLine = new();
        buildCmdLine.Append(buildProjectOptions.Publish ? "publish" : "build");

        string logFilePath = Path.Combine(s_buildEnv.LogRootPath, $"{id}.binlog");
        _testOutput.WriteLine($"-------- Building ---------");
        _testOutput.WriteLine($"Binlog path: {logFilePath}");
        buildCmdLine.Append($" -c {buildArgs.Config} -bl:{logFilePath} {buildArgs.ExtraBuildArgs}");

        if (buildProjectOptions.Publish && buildProjectOptions.BuildOnlyAfterPublish)
            buildCmdLine.Append(" -p:WasmBuildOnlyAfterPublish=true");

        CommandResult res = new DotNetCommand(s_buildEnv, _testOutput)
                                .WithWorkingDirectory(_projectDir!)
                                .WithEnvironmentVariables(buildProjectOptions.ExtraBuildEnvironmentVariables)
                                .ExecuteWithCapturedOutput(buildCmdLine.ToString());
        if (buildProjectOptions.ExpectSuccess)
            res.EnsureSuccessful();
        else
            Assert.NotEqual(0, res.ExitCode);

        if (buildProjectOptions.UseCache)
            _buildContext.CacheBuild(buildArgs, new BuildProduct(_projectDir!, logFilePath, true, res.Output));

        ProjectProviderBase.AssertRuntimePackPath(res.Output, buildProjectOptions.TargetFramework ?? DefaultTargetFramework);
        string bundleDir = Path.Combine(GetBinDir(config: buildArgs.Config, targetFramework: buildProjectOptions.TargetFramework ?? DefaultTargetFramework), "AppBundle");

        assertAppBundleOptions ??= new AssertTestMainJsAppBundleOptions(
                                        BundleDir: bundleDir,
                                        ProjectName: buildArgs.ProjectName,
                                        Config: buildArgs.Config,
                                        MainJS: buildProjectOptions.MainJS ?? "test-main.js",
                                        HasV8Script: buildProjectOptions.HasV8Script,
                                        GlobalizationMode: buildProjectOptions.GlobalizationMode,
                                        PredefinedIcudt: buildProjectOptions.PredefinedIcudt ?? "",
                                        UseWebcil: UseWebcil,
                                        IsBrowserProject: buildProjectOptions.IsBrowserProject,
                                        IsPublish: buildProjectOptions.Publish);
        new TestMainJsProjectProvider(_testOutput, _projectDir)
                .AssertBasicAppBundle(assertAppBundleOptions);

        return (_projectDir!, res.Output);
    }

}
