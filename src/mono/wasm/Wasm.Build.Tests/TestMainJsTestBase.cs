// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable enable

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json.Nodes;
using System.Threading.Tasks;
using Microsoft.Playwright;
using Xunit;
using Xunit.Abstractions;
using Xunit.Sdk;

namespace Wasm.Build.Tests;

public abstract class TestMainJsTestBase : BuildTestBase
{
    protected TestMainJsTestBase(ITestOutputHelper output, SharedBuildPerTestClassFixture buildContext)
                : base(output, buildContext)
    {
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
            File.Copy(Path.Combine(AppContext.BaseDirectory,
                                    options.TargetFramework == "net8.0" ? "test-main.js" : "data/test-main-7.0.js"),
                        Path.Combine(_projectDir, "test-main.js"));

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
                AssertRuntimePackPath(result.buildOutput, options.TargetFramework ?? DefaultTargetFramework);

                string bundleDir = Path.Combine(GetBinDir(config: buildArgs.Config, targetFramework: options.TargetFramework ?? DefaultTargetFramework), "AppBundle");
                AssertBasicAppBundle(bundleDir,
                                     buildArgs.ProjectName,
                                     buildArgs.Config,
                                     options.MainJS ?? "test-main.js",
                                     options.HasV8Script,
                                     options.TargetFramework ?? DefaultTargetFramework,
                                     options.GlobalizationMode,
                                     options.PredefinedIcudt ?? "",
                                     options.DotnetWasmFromRuntimePack ?? !buildArgs.AOT,
                                     UseWebcil,
                                     options.IsBrowserProject);
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

    static void AssertRuntimePackPath(string buildOutput, string targetFramework)
    {
        var match = s_runtimePackPathRegex.Match(buildOutput);
        if (!match.Success || match.Groups.Count != 2)
            throw new XunitException($"Could not find the pattern in the build output: '{s_runtimePackPathPattern}'.{Environment.NewLine}Build output: {buildOutput}");

        string expectedRuntimePackDir = s_buildEnv.GetRuntimePackDir(targetFramework);
        string actualPath = match.Groups[1].Value;
        if (string.Compare(actualPath, expectedRuntimePackDir) != 0)
            throw new XunitException($"Runtime pack path doesn't match.{Environment.NewLine}Expected: '{expectedRuntimePackDir}'{Environment.NewLine}Actual:   '{actualPath}'");
    }

    private static void AssertBasicAppBundle(string bundleDir,
                                               string projectName,
                                               string config,
                                               string mainJS,
                                               bool hasV8Script,
                                               string targetFramework,
                                               GlobalizationMode? globalizationMode,
                                               string predefinedIcudt = "",
                                               bool dotnetWasmFromRuntimePack = true,
                                               bool useWebcil = true,
                                               bool isBrowserProject = true)
    {
        var filesToExist = new List<string>()
        {
            mainJS,
            "dotnet.native.wasm",
            "_framework/blazor.boot.json",
            "dotnet.js",
            "dotnet.native.js",
            "dotnet.runtime.js"
        };

        if (isBrowserProject)
            filesToExist.Add("index.html");

        AssertFilesExist(bundleDir, filesToExist);

        AssertFilesExist(bundleDir, new[] { "run-v8.sh" }, expectToExist: hasV8Script);
        AssertIcuAssets();

        string managedDir = Path.Combine(bundleDir, "managed");
        string bundledMainAppAssembly =
            useWebcil ? $"{projectName}{WebcilInWasmExtension}" : $"{projectName}.dll";
        AssertFilesExist(managedDir, new[] { bundledMainAppAssembly });

        bool is_debug = config == "Debug";
        if (is_debug)
        {
            // Use cecil to check embedded pdb?
            // AssertFilesExist(managedDir, new[] { $"{projectName}.pdb" });

            //FIXME: um.. what about these? embedded? why is linker omitting them?
            //foreach (string file in Directory.EnumerateFiles(managedDir, "*.dll"))
            //{
            //string pdb = Path.ChangeExtension(file, ".pdb");
            //Assert.True(File.Exists(pdb), $"Could not find {pdb} for {file}");
            //}
        }

        AssertDotNetWasmJs(bundleDir, fromRuntimePack: dotnetWasmFromRuntimePack, targetFramework);

        void AssertIcuAssets()
        {
            bool expectEFIGS = false;
            bool expectCJK = false;
            bool expectNOCJK = false;
            bool expectFULL = false;
            bool expectHYBRID = false;
            switch (globalizationMode)
            {
                case GlobalizationMode.Invariant:
                    break;
                case GlobalizationMode.FullIcu:
                    expectFULL = true;
                    break;
                case GlobalizationMode.Hybrid:
                    expectHYBRID = true;
                    break;
                case GlobalizationMode.PredefinedIcu:
                    if (string.IsNullOrEmpty(predefinedIcudt))
                        throw new ArgumentException("WasmBuildTest is invalid, value for predefinedIcudt is required when GlobalizationMode=PredefinedIcu.");
                    AssertFilesExist(bundleDir, new[] { predefinedIcudt }, expectToExist: true);
                    // predefined ICU name can be identical with the icu files from runtime pack
                    switch (predefinedIcudt)
                    {
                        case "icudt.dat":
                            expectFULL = true;
                            break;
                        case "icudt_EFIGS.dat":
                            expectEFIGS = true;
                            break;
                        case "icudt_CJK.dat":
                            expectCJK = true;
                            break;
                        case "icudt_no_CJK.dat":
                            expectNOCJK = true;
                            break;
                    }
                    break;
                default:
                    // icu shard chosen based on the locale
                    expectCJK = true;
                    expectEFIGS = true;
                    expectNOCJK = true;
                    break;
            }
            AssertFilesExist(bundleDir, new[] { "icudt.dat" }, expectToExist: expectFULL);
            AssertFilesExist(bundleDir, new[] { "icudt_EFIGS.dat" }, expectToExist: expectEFIGS);
            AssertFilesExist(bundleDir, new[] { "icudt_CJK.dat" }, expectToExist: expectCJK);
            AssertFilesExist(bundleDir, new[] { "icudt_no_CJK.dat" }, expectToExist: expectNOCJK);
            AssertFilesExist(bundleDir, new[] { "icudt_hybrid.dat" }, expectToExist: expectHYBRID);
        }
    }

    private (int exitCode, string buildOutput) AssertBuild(string args, string label = "build", bool expectSuccess = true, IDictionary<string, string>? envVars = null, int? timeoutMs = null)
    {
        var result = RunProcess(s_buildEnv.DotNet, _testOutput, args, workingDir: _projectDir, label: label, envVars: envVars, timeoutMs: timeoutMs ?? s_defaultPerTestTimeoutMs);
        if (expectSuccess && result.exitCode != 0)
            throw new XunitException($"Build process exited with non-zero exit code: {result.exitCode}");
        if (!expectSuccess && result.exitCode == 0)
            throw new XunitException($"Build should have failed, but it didn't. Process exited with exitCode : {result.exitCode}");

        return result;
    }

    protected static void AssertDotNetWasmJs(string bundleDir, bool fromRuntimePack, string targetFramework)
    {
        AssertFile(Path.Combine(s_buildEnv.GetRuntimeNativeDir(targetFramework), "dotnet.native.wasm"),
                   Path.Combine(bundleDir, "dotnet.native.wasm"),
                   "Expected dotnet.native.wasm to be same as the runtime pack",
                   same: fromRuntimePack);

        AssertFile(Path.Combine(s_buildEnv.GetRuntimeNativeDir(targetFramework), "dotnet.native.js"),
                   Path.Combine(bundleDir, "dotnet.native.js"),
                   "Expected dotnet.native.js to be same as the runtime pack",
                   same: fromRuntimePack);
    }

    protected string RunAndTestWasmApp(BuildArgs buildArgs,
                                       RunHost host,
                                       string id,
                                       Action<string>? test = null,
                                       string? buildDir = null,
                                       string? bundleDir = null,
                                       int expectedExitCode = 0,
                                       string? args = null,
                                       Dictionary<string, string>? envVars = null,
                                       string targetFramework = DefaultTargetFramework,
                                       string? extraXHarnessMonoArgs = null,
                                       string? extraXHarnessArgs = null,
                                       string jsRelativePath = "test-main.js",
                                       string environmentLocale = DefaultEnvironmentLocale)
    {
        buildDir ??= _projectDir;
        envVars ??= new();
        envVars["XHARNESS_DISABLE_COLORED_OUTPUT"] = "true";
        if (buildArgs.AOT)
        {
            envVars["MONO_LOG_LEVEL"] = "debug";
            envVars["MONO_LOG_MASK"] = "aot";
        }

        if (s_buildEnv.EnvVars != null)
        {
            foreach (var kvp in s_buildEnv.EnvVars)
                envVars[kvp.Key] = kvp.Value;
        }

        bundleDir ??= Path.Combine(GetBinDir(baseDir: buildDir, config: buildArgs.Config, targetFramework: targetFramework), "AppBundle");
        IHostRunner hostRunner = GetHostRunnerFromRunHost(host);
        if (!hostRunner.CanRunWBT())
            throw new InvalidOperationException("Running tests with V8 on windows isn't supported");

        // Use wasm-console.log to get the xharness output for non-browser cases
        string testCommand = hostRunner.GetTestCommand();
        XHarnessArgsOptions options = new XHarnessArgsOptions(jsRelativePath, environmentLocale, host);
        string xharnessArgs = s_isWindows ? hostRunner.GetXharnessArgsWindowsOS(options) : hostRunner.GetXharnessArgsOtherOS(options);
        bool useWasmConsoleOutput = hostRunner.UseWasmConsoleOutput();

        extraXHarnessArgs += " " + xharnessArgs;

        string testLogPath = Path.Combine(_logPath, host.ToString());
        string output = RunWithXHarness(
                            testCommand,
                            testLogPath,
                            buildArgs.ProjectName,
                            bundleDir,
                            _testOutput,
                            envVars: envVars,
                            expectedAppExitCode: expectedExitCode,
                            extraXHarnessArgs: extraXHarnessArgs,
                            appArgs: args,
                            extraXHarnessMonoArgs: extraXHarnessMonoArgs,
                            useWasmConsoleOutput: useWasmConsoleOutput
                            );

        AssertSubstring("AOT: image 'System.Private.CoreLib' found.", output, contains: buildArgs.AOT);
        AssertSubstring($"AOT: image '{buildArgs.ProjectName}' found.", output, contains: buildArgs.AOT);

        if (test != null)
            test(output);

        return output;
    }

}
