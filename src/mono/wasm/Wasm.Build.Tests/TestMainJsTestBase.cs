// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable enable

using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Xunit.Abstractions;
using Xunit.Sdk;

namespace Wasm.Build.Tests;

public abstract class TestMainJsTestBase : BuildTestBase
{
    protected TestMainJsProjectProvider _provider;
    protected TestMainJsTestBase(ITestOutputHelper output, SharedBuildPerTestClassFixture buildContext)
                : base(new TestMainJsProjectProvider(output), output, buildContext)
    {
        _provider = GetProvider<TestMainJsProjectProvider>();
    }

    public (string projectDir, string buildOutput) BuildProject(BuildArgs buildArgs,
                              string id,
                              BuildProjectOptions options)
    {
        string msgPrefix = options.Label != null ? $"[{options.Label}] " : string.Empty;
        if (options.UseCache && _buildContext.TryGetBuildFor(buildArgs, out BuildProduct? product))
        {
            _testOutput.WriteLine($"Using existing build found at {product.ProjectDir}, with build log at {product.LogFile}");

            if (!product.Result)
                throw new XunitException($"Found existing build at {product.ProjectDir}, but it had failed. Check build log at {product.LogFile}");
            _projectDir = product.ProjectDir;

            // use this test's id for the run logs
            _logPath = Path.Combine(s_buildEnv.LogRootPath, id);
            return (_projectDir, product.BuildOutput);
        }

        if (options.CreateProject)
        {
            InitPaths(id);
            InitProjectDir(_projectDir);
            options.InitProject?.Invoke();

            File.WriteAllText(Path.Combine(_projectDir, $"{buildArgs.ProjectName}.csproj"), buildArgs.ProjectFileContents);
            File.Copy(
                Path.Combine(
                    AppContext.BaseDirectory,
                    string.IsNullOrEmpty(options.TargetFramework) || options.TargetFramework == "net8.0"
                        ? "test-main.js"
                        : "data/test-main-7.0.js"
                ),
                Path.Combine(_projectDir, "test-main.js")
            );

            File.WriteAllText(Path.Combine(_projectDir!, "index.html"), @"<html><body><script type=""module"" src=""test-main.js""></script></body></html>");
        }
        else if (_projectDir is null)
        {
            throw new Exception("_projectDir should be set, to use options.createProject=false");
        }

        StringBuilder sb = new();
        sb.Append(options.Publish ? "publish" : "build");
        if (options.Publish && options.BuildOnlyAfterPublish)
            sb.Append(" -p:WasmBuildOnlyAfterPublish=true");
        sb.Append($" {s_buildEnv.DefaultBuildArgs}");

        sb.Append($" /p:Configuration={buildArgs.Config}");

        string logFileSuffix = options.Label == null ? string.Empty : options.Label.Replace(' ', '_');
        string logFilePath = Path.Combine(_logPath, $"{buildArgs.ProjectName}{logFileSuffix}.binlog");
        _testOutput.WriteLine($"-------- Building ---------");
        _testOutput.WriteLine($"Binlog path: {logFilePath}");
        sb.Append($" /bl:\"{logFilePath}\" /nologo");
        sb.Append($" /v:{options.Verbosity ?? "minimal"}");
        if (buildArgs.ExtraBuildArgs != null)
            sb.Append($" {buildArgs.ExtraBuildArgs} ");

        _testOutput.WriteLine($"Building {buildArgs.ProjectName} in {_projectDir}");

        (int exitCode, string buildOutput) result;
        try
        {
            var envVars = s_buildEnv.EnvVars;
            if (options.ExtraBuildEnvironmentVariables is not null)
            {
                envVars = new Dictionary<string, string>(s_buildEnv.EnvVars);
                foreach (var kvp in options.ExtraBuildEnvironmentVariables!)
                    envVars[kvp.Key] = kvp.Value;
            }
            envVars["NUGET_PACKAGES"] = _nugetPackagesDir;
            result = AssertBuild(sb.ToString(), id, expectSuccess: options.ExpectSuccess, envVars: envVars);

            // check that we are using the correct runtime pack!

            if (options.ExpectSuccess && options.AssertAppBundle)
            {
                ProjectProviderBase.AssertRuntimePackPath(result.buildOutput, options.TargetFramework ?? DefaultTargetFramework);

                string bundleDir = Path.Combine(GetBinDir(config: buildArgs.Config, targetFramework: options.TargetFramework ?? DefaultTargetFramework), "AppBundle");
                _provider.AssertBasicAppBundle(new AssertTestMainJsAppBundleOptions(
                                        BundleDir: bundleDir,
                                        ProjectName: buildArgs.ProjectName,
                                        Config: buildArgs.Config,
                                        MainJS: options.MainJS ?? "test-main.js",
                                        HasV8Script: options.HasV8Script,
                                        GlobalizationMode: options.GlobalizationMode,
                                        PredefinedIcudt: options.PredefinedIcudt ?? "",
                                        UseWebcil: UseWebcil,
                                        IsBrowserProject: options.IsBrowserProject,
                                        IsPublish: options.Publish));
            }

            if (options.UseCache)
                _buildContext.CacheBuild(buildArgs, new BuildProduct(_projectDir, logFilePath, true, result.buildOutput));

            return (_projectDir, result.buildOutput);
        }
        catch (Exception ex)
        {
            if (options.UseCache)
                _buildContext.CacheBuild(buildArgs, new BuildProduct(_projectDir, logFilePath, false, $"The build attempt resulted in exception: {ex}."));
            throw;
        }
    }

    protected (int exitCode, string buildOutput) AssertBuild(string args, string label = "build", bool expectSuccess = true, IDictionary<string, string>? envVars = null, int? timeoutMs = null)
    {
        var result = RunProcess(s_buildEnv.DotNet, _testOutput, args, workingDir: _projectDir, label: label, envVars: envVars, timeoutMs: timeoutMs ?? s_defaultPerTestTimeoutMs);
        if (expectSuccess && result.exitCode != 0)
            throw new XunitException($"Build process exited with non-zero exit code: {result.exitCode}");
        if (!expectSuccess && result.exitCode == 0)
            throw new XunitException($"Build should have failed, but it didn't. Process exited with exitCode : {result.exitCode}");

        return result;
    }
}
