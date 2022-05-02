// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using System.Xml;
using Xunit;
using Xunit.Abstractions;
using Xunit.Sdk;

#nullable enable

// [assembly: CollectionBehavior(CollectionBehavior.CollectionPerAssembly)]

namespace Wasm.Build.Tests
{
    public abstract class BuildTestBase : IClassFixture<SharedBuildPerTestClassFixture>, IDisposable
    {
        public const string DefaultTargetFramework = "net7.0";
        public static readonly string NuGetConfigFileNameForDefaultFramework = $"nuget7.config";
        protected static readonly bool s_skipProjectCleanup;
        protected static readonly string s_xharnessRunnerCommand;
        protected string? _projectDir;
        protected readonly ITestOutputHelper _testOutput;
        protected string _logPath;
        protected bool _enablePerTestCleanup = false;
        protected SharedBuildPerTestClassFixture _buildContext;

        // FIXME: use an envvar to override this
        protected static int s_defaultPerTestTimeoutMs = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? 30*60*1000 : 15*60*1000;
        protected static BuildEnvironment s_buildEnv;
        private const string s_runtimePackPathPattern = "\\*\\* MicrosoftNetCoreAppRuntimePackDir : ([^ ]*)";
        private static Regex s_runtimePackPathRegex;

        public static bool IsUsingWorkloads => s_buildEnv.IsWorkload;
        public static bool IsNotUsingWorkloads => !s_buildEnv.IsWorkload;

        static BuildTestBase()
        {
            try
            {
                s_buildEnv = new BuildEnvironment();
                s_runtimePackPathRegex = new Regex(s_runtimePackPathPattern);

                s_skipProjectCleanup = !string.IsNullOrEmpty(EnvironmentVariables.SkipProjectCleanup) && EnvironmentVariables.SkipProjectCleanup == "1";

                if (string.IsNullOrEmpty(EnvironmentVariables.XHarnessCliPath))
                    s_xharnessRunnerCommand = "xharness";
                else
                    s_xharnessRunnerCommand = EnvironmentVariables.XHarnessCliPath;

                string? nugetPackagesPath = Environment.GetEnvironmentVariable("NUGET_PACKAGES");
                if (!string.IsNullOrEmpty(nugetPackagesPath))
                {
                    if (!Directory.Exists(nugetPackagesPath))
                        Directory.CreateDirectory(nugetPackagesPath);
                }

                Console.WriteLine ("");
                Console.WriteLine ($"==============================================================================================");
                Console.WriteLine ($"=============== Running with {(s_buildEnv.IsWorkload ? "Workloads" : "EMSDK")} ===============");
                Console.WriteLine ($"==============================================================================================");
                Console.WriteLine ("");
            }
            catch (Exception ex)
            {
                Console.WriteLine ($"Exception: {ex}");
                throw;
            }
        }

        public BuildTestBase(ITestOutputHelper output, SharedBuildPerTestClassFixture buildContext)
        {
            Console.WriteLine($"{Environment.NewLine}-------- New test --------{Environment.NewLine}");
            _buildContext = buildContext;
            _testOutput = output;
            _logPath = s_buildEnv.LogRootPath; // FIXME:
        }

        /*
         * TODO:
            - AOT modes
                - llvmonly
                - aotinterp
                    - skipped assemblies should get have their pinvoke/icall stuff scanned

            - only buildNative
            - aot but no wrapper - check that AppBundle wasn't generated
        */

        public static IEnumerable<IEnumerable<object?>> ConfigWithAOTData(bool aot, string? config=null)
        {
            if (config == null)
            {
                return new IEnumerable<object?>[]
                    {
    #if TEST_DEBUG_CONFIG_ALSO
                        // list of each member data - for Debug+@aot
                        new object?[] { new BuildArgs("placeholder", "Debug", aot, "placeholder", string.Empty) }.AsEnumerable(),
    #endif
                        // list of each member data - for Release+@aot
                        new object?[] { new BuildArgs("placeholder", "Release", aot, "placeholder", string.Empty) }.AsEnumerable()
                    }.AsEnumerable();
            }
            else
            {
                return new IEnumerable<object?>[]
                {
                    new object?[] { new BuildArgs("placeholder", config, aot, "placeholder", string.Empty) }.AsEnumerable()
                };
            }
        }


        protected string RunAndTestWasmApp(BuildArgs buildArgs,
                                           RunHost host,
                                           string id,
                                           Action<string>? test=null,
                                           string? buildDir = null,
                                           int expectedExitCode = 0,
                                           string? args = null,
                                           Dictionary<string, string>? envVars = null,
                                           string targetFramework = DefaultTargetFramework)
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

            string bundleDir = Path.Combine(GetBinDir(baseDir: buildDir, config: buildArgs.Config, targetFramework: targetFramework), "AppBundle");
            (string testCommand, string extraXHarnessArgs) = host switch
            {
                RunHost.V8     => ("wasm test", "--js-file=test-main.js --engine=V8 -v trace"),
                RunHost.NodeJS => ("wasm test", "--js-file=test-main.js --engine=NodeJS -v trace"),
                _              => ("wasm test-browser", $"-v trace -b {host}")
            };

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
                                appArgs: args);

            if (buildArgs.AOT)
            {
                Assert.Contains("AOT: image 'System.Private.CoreLib' found.", output);
                Assert.Contains($"AOT: image '{buildArgs.ProjectName}' found.", output);
            }
            else
            {
                Assert.DoesNotContain("AOT: image 'System.Private.CoreLib' found.", output);
                Assert.DoesNotContain($"AOT: image '{buildArgs.ProjectName}' found.", output);
            }

            if (test != null)
                test(output);

            return output;
        }

        protected static string RunWithXHarness(string testCommand, string testLogPath, string projectName, string bundleDir,
                                        ITestOutputHelper _testOutput, IDictionary<string, string>? envVars=null,
                                        int expectedAppExitCode=0, int xharnessExitCode=0, string? extraXHarnessArgs=null, string? appArgs=null)
        {
            Console.WriteLine($"============== {testCommand} =============");
            Directory.CreateDirectory(testLogPath);

            StringBuilder args = new();
            args.Append(s_xharnessRunnerCommand);
            args.Append($" {testCommand}");
            args.Append($" --app=.");
            args.Append($" --output-directory={testLogPath}");
            args.Append($" --expected-exit-code={expectedAppExitCode}");
            args.Append($" {extraXHarnessArgs ?? string.Empty}");

            args.Append(" -- ");

            // App arguments
            if (envVars != null)
            {
                var setenv = string.Join(' ', envVars.Select(kvp => $"\"--setenv={kvp.Key}={kvp.Value}\"").ToArray());
                args.Append($" {setenv}");
            }

            args.Append($" --run {projectName}.dll");
            args.Append($" {appArgs ?? string.Empty}");

            _testOutput.WriteLine(string.Empty);
            _testOutput.WriteLine($"---------- Running with {testCommand} ---------");
            var (exitCode, output) = RunProcess(s_buildEnv.DotNet, _testOutput,
                                        args: args.ToString(),
                                        workingDir: bundleDir,
                                        envVars: envVars,
                                        label: testCommand,
                                        timeoutMs: s_defaultPerTestTimeoutMs);

            File.WriteAllText(Path.Combine(testLogPath, $"xharness.log"), output);

            if (exitCode != xharnessExitCode)
            {
                _testOutput.WriteLine($"Exit code: {exitCode}");
                if (exitCode != expectedAppExitCode)
                    throw new XunitException($"[{testCommand}] Exit code, expected {expectedAppExitCode} but got {exitCode} for command: {testCommand} {args}");
            }

            return output;
        }

        [MemberNotNull(nameof(_projectDir), nameof(_logPath))]
        protected void InitPaths(string id)
        {
            if (_projectDir == null)
                _projectDir = Path.Combine(AppContext.BaseDirectory, id);
            _logPath = Path.Combine(s_buildEnv.LogRootPath, id);

            Directory.CreateDirectory(_logPath);
        }

        protected static void InitProjectDir(string dir)
        {
            Directory.CreateDirectory(dir);
            File.WriteAllText(Path.Combine(dir, "Directory.Build.props"), s_buildEnv.DirectoryBuildPropsContents);
            File.WriteAllText(Path.Combine(dir, "Directory.Build.targets"), s_buildEnv.DirectoryBuildTargetsContents);

            File.Copy(Path.Combine(BuildEnvironment.TestDataPath, NuGetConfigFileNameForDefaultFramework), Path.Combine(dir, "nuget.config"));
            Directory.CreateDirectory(Path.Combine(dir, ".nuget"));
        }

        protected const string SimpleProjectTemplate =
            @$"<Project Sdk=""Microsoft.NET.Sdk"">
              <PropertyGroup>
                <TargetFramework>{DefaultTargetFramework}</TargetFramework>
                <OutputType>Exe</OutputType>
                <WasmGenerateRunV8Script>true</WasmGenerateRunV8Script>
                <WasmMainJSPath>test-main.js</WasmMainJSPath>
                ##EXTRA_PROPERTIES##
              </PropertyGroup>
              <ItemGroup>
                ##EXTRA_ITEMS##
              </ItemGroup>
              ##INSERT_AT_END##
            </Project>";

        protected static BuildArgs ExpandBuildArgs(BuildArgs buildArgs, string extraProperties="", string extraItems="", string insertAtEnd="", string projectTemplate=SimpleProjectTemplate)
        {
            if (buildArgs.AOT)
            {
                extraProperties = $"{extraProperties}\n<RunAOTCompilation>true</RunAOTCompilation>";
                extraProperties += $"\n<EmccVerbose>{RuntimeInformation.IsOSPlatform(OSPlatform.Windows)}</EmccVerbose>\n";
            }

            string projectContents = projectTemplate
                                        .Replace("##EXTRA_PROPERTIES##", extraProperties)
                                        .Replace("##EXTRA_ITEMS##", extraItems)
                                        .Replace("##INSERT_AT_END##", insertAtEnd);
            return buildArgs with { ProjectFileContents = projectContents };
        }

        public (string projectDir, string buildOutput) BuildProject(BuildArgs buildArgs,
                                  string id,
                                  BuildProjectOptions options)
        {
            string msgPrefix = options.Label != null ? $"[{options.Label}] " : string.Empty;
            if (options.UseCache && _buildContext.TryGetBuildFor(buildArgs, out BuildProduct? product))
            {
                Console.WriteLine ($"Using existing build found at {product.ProjectDir}, with build log at {product.LogFile}");

                Assert.True(product.Result, $"Found existing build at {product.ProjectDir}, but it had failed. Check build log at {product.LogFile}");
                _projectDir = product.ProjectDir;

                // use this test's id for the run logs
                _logPath = Path.Combine(s_buildEnv.LogRootPath, id);
                return (_projectDir, "FIXME");
            }

            if (options.CreateProject)
            {
                InitPaths(id);
                InitProjectDir(_projectDir);
                options.InitProject?.Invoke();

                File.WriteAllText(Path.Combine(_projectDir, $"{buildArgs.ProjectName}.csproj"), buildArgs.ProjectFileContents);
                File.Copy(Path.Combine(AppContext.BaseDirectory, "test-main.js"), Path.Combine(_projectDir, "test-main.js"));
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
            Console.WriteLine($"Binlog path: {logFilePath}");
            sb.Append($" /bl:\"{logFilePath}\" /nologo");
            sb.Append($" /fl /flp:\"v:diag,LogFile={logFilePath}.log\" /v:{options.Verbosity ?? "minimal"}");
            if (buildArgs.ExtraBuildArgs != null)
                sb.Append($" {buildArgs.ExtraBuildArgs} ");

            Console.WriteLine($"Building {buildArgs.ProjectName} in {_projectDir}");

            (int exitCode, string buildOutput) result;
            try
            {
                result = AssertBuild(sb.ToString(), id, expectSuccess: options.ExpectSuccess, envVars: s_buildEnv.EnvVars);

                //AssertRuntimePackPath(result.buildOutput);

                // check that we are using the correct runtime pack!

                if (options.ExpectSuccess)
                {
                    string bundleDir = Path.Combine(GetBinDir(config: buildArgs.Config, targetFramework: options.TargetFramework ?? DefaultTargetFramework), "AppBundle");
                    AssertBasicAppBundle(bundleDir, buildArgs.ProjectName, buildArgs.Config, options.MainJS ?? "test-main.js", options.HasV8Script, options.HasIcudt, options.DotnetWasmFromRuntimePack ?? !buildArgs.AOT);
                }

                if (options.UseCache)
                    _buildContext.CacheBuild(buildArgs, new BuildProduct(_projectDir, logFilePath, true));

                return (_projectDir, result.buildOutput);
            }
            catch
            {
                if (options.UseCache)
                    _buildContext.CacheBuild(buildArgs, new BuildProduct(_projectDir, logFilePath, false));
                throw;
            }
        }

        public void InitBlazorWasmProjectDir(string id)
        {
            InitPaths(id);
            if (Directory.Exists(_projectDir))
                Directory.Delete(_projectDir, recursive: true);
            Directory.CreateDirectory(_projectDir);
            Directory.CreateDirectory(Path.Combine(_projectDir, ".nuget"));

            File.Copy(Path.Combine(BuildEnvironment.TestDataPath, NuGetConfigFileNameForDefaultFramework), Path.Combine(_projectDir, "nuget.config"));
            File.Copy(Path.Combine(BuildEnvironment.TestDataPath, "Blazor.Directory.Build.props"), Path.Combine(_projectDir, "Directory.Build.props"));
            File.Copy(Path.Combine(BuildEnvironment.TestDataPath, "Blazor.Directory.Build.targets"), Path.Combine(_projectDir, "Directory.Build.targets"));
        }

        public string CreateWasmTemplateProject(string id, string template = "wasmbrowser")
        {
            InitPaths(id);
            InitProjectDir(id);
            new DotNetCommand(s_buildEnv, useDefaultArgs: false)
                    .WithWorkingDirectory(_projectDir!)
                    .ExecuteWithCapturedOutput($"new {template}")
                    .EnsureSuccessful();

            return Path.Combine(_projectDir!, $"{id}.csproj");
        }

        public string CreateBlazorWasmTemplateProject(string id)
        {
            InitBlazorWasmProjectDir(id);
            new DotNetCommand(s_buildEnv, useDefaultArgs: false)
                    .WithWorkingDirectory(_projectDir!)
                    .ExecuteWithCapturedOutput("new blazorwasm")
                    .EnsureSuccessful();

            return Path.Combine(_projectDir!, $"{id}.csproj");
        }

        protected (CommandResult, string) BlazorBuild(BlazorBuildOptions options, params string[] extraArgs)
        {
            var res = BuildInternal(options.Id, options.Config, publish: false, setWasmDevel: false, extraArgs);
            AssertDotNetNativeFiles(options.ExpectedFileType, options.Config, forPublish: false, targetFramework: options.TargetFramework);
            AssertBlazorBundle(options.Config, isPublish: false, dotnetWasmFromRuntimePack: options.ExpectedFileType == NativeFilesType.FromRuntimePack);

            return res;
        }

        protected (CommandResult, string) BlazorPublish(BlazorBuildOptions options, params string[] extraArgs)
        {
            var res = BuildInternal(options.Id, options.Config, publish: true, setWasmDevel: false, extraArgs);
            AssertDotNetNativeFiles(options.ExpectedFileType, options.Config, forPublish: true, targetFramework: options.TargetFramework);
            AssertBlazorBundle(options.Config, isPublish: true, dotnetWasmFromRuntimePack: options.ExpectedFileType == NativeFilesType.FromRuntimePack);

            if (options.ExpectedFileType == NativeFilesType.AOT)
            {
                // check for this too, so we know the format is correct for the negative
                // test for jsinterop.webassembly.dll
                Assert.Contains("Microsoft.JSInterop.dll -> Microsoft.JSInterop.dll.bc", res.Item1.Output);

                // make sure this assembly gets skipped
                Assert.DoesNotContain("Microsoft.JSInterop.WebAssembly.dll -> Microsoft.JSInterop.WebAssembly.dll.bc", res.Item1.Output);
            }
            return res;
        }

        protected (CommandResult, string) BuildInternal(string id, string config, bool publish=false, bool setWasmDevel=true, params string[] extraArgs)
        {
            string label = publish ? "publish" : "build";
            Console.WriteLine($"{Environment.NewLine}** {label} **{Environment.NewLine}");

            string logPath = Path.Combine(s_buildEnv.LogRootPath, id, $"{id}-{label}.binlog");
            string[] combinedArgs = new[]
            {
                label, // same as the command name
                $"-bl:{logPath}",
                $"-p:Configuration={config}",
                "-p:BlazorEnableCompression=false",
                setWasmDevel ? "-p:_WasmDevel=true" : string.Empty
            }.Concat(extraArgs).ToArray();

            CommandResult res = new DotNetCommand(s_buildEnv)
                                        .WithWorkingDirectory(_projectDir!)
                                        .ExecuteWithCapturedOutput(combinedArgs)
                                        .EnsureSuccessful();

            return (res, logPath);
        }

        protected void AssertDotNetNativeFiles(NativeFilesType type, string config, bool forPublish, string targetFramework = DefaultTargetFramework)
        {
            string label = forPublish ? "publish" : "build";
            string objBuildDir = Path.Combine(_projectDir!, "obj", config, targetFramework, "wasm", forPublish ? "for-publish" : "for-build");
            string binFrameworkDir = FindBlazorBinFrameworkDir(config, forPublish);

            string srcDir = type switch
            {
                NativeFilesType.FromRuntimePack => s_buildEnv.RuntimeNativeDir,
                NativeFilesType.Relinked => objBuildDir,
                NativeFilesType.AOT => objBuildDir,
                _ => throw new ArgumentOutOfRangeException(nameof(type))
            };

            AssertSameFile(Path.Combine(srcDir, "dotnet.wasm"), Path.Combine(binFrameworkDir, "dotnet.wasm"), label);

            // find dotnet*js
            string? dotnetJsPath = Directory.EnumerateFiles(binFrameworkDir)
                                    .Where(p => Path.GetFileName(p).StartsWith("dotnet.", StringComparison.OrdinalIgnoreCase) &&
                                                    Path.GetFileName(p).EndsWith(".js", StringComparison.OrdinalIgnoreCase))
                                    .SingleOrDefault();

            Assert.True(!string.IsNullOrEmpty(dotnetJsPath), $"[{label}] Expected to find dotnet*js in {binFrameworkDir}");
            AssertSameFile(Path.Combine(srcDir, "dotnet.js"), dotnetJsPath!, label);

            if (type != NativeFilesType.FromRuntimePack)
            {
                // check that the files are *not* from runtime pack
                AssertNotSameFile(Path.Combine(s_buildEnv.RuntimeNativeDir, "dotnet.wasm"), Path.Combine(binFrameworkDir, "dotnet.wasm"), label);
                AssertNotSameFile(Path.Combine(s_buildEnv.RuntimeNativeDir, "dotnet.js"), dotnetJsPath!, label);
            }
        }

        static void AssertRuntimePackPath(string buildOutput)
        {
            var match = s_runtimePackPathRegex.Match(buildOutput);
            if (!match.Success || match.Groups.Count != 2)
                throw new XunitException($"Could not find the pattern in the build output: '{s_runtimePackPathPattern}'.{Environment.NewLine}Build output: {buildOutput}");

            string actualPath = match.Groups[1].Value;
            if (string.Compare(actualPath, s_buildEnv.RuntimePackDir) != 0)
                throw new XunitException($"Runtime pack path doesn't match.{Environment.NewLine}Expected: {s_buildEnv.RuntimePackDir}{Environment.NewLine}Actual:   {actualPath}");
        }

        protected static void AssertBasicAppBundle(string bundleDir, string projectName, string config, string mainJS, bool hasV8Script, bool hasIcudt=true, bool dotnetWasmFromRuntimePack=true)
        {
            AssertFilesExist(bundleDir, new []
            {
                "index.html",
                mainJS,
                "dotnet.timezones.blat",
                "dotnet.wasm",
                "mono-config.json",
                "dotnet.js"
            });

            AssertFilesExist(bundleDir, new[] { "run-v8.sh" }, expectToExist: hasV8Script);
            AssertFilesExist(bundleDir, new[] { "icudt.dat" }, expectToExist: hasIcudt);

            string managedDir = Path.Combine(bundleDir, "managed");
            AssertFilesExist(managedDir, new[] { $"{projectName}.dll" });

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

            AssertDotNetWasmJs(bundleDir, fromRuntimePack: dotnetWasmFromRuntimePack);
        }

        protected static void AssertDotNetWasmJs(string bundleDir, bool fromRuntimePack)
        {
            AssertFile(Path.Combine(s_buildEnv.RuntimeNativeDir, "dotnet.wasm"),
                       Path.Combine(bundleDir, "dotnet.wasm"),
                       "Expected dotnet.wasm to be same as the runtime pack",
                       same: fromRuntimePack);

            AssertFile(Path.Combine(s_buildEnv.RuntimeNativeDir, "dotnet.js"),
                       Path.Combine(bundleDir, "dotnet.js"),
                       "Expected dotnet.js to be same as the runtime pack",
                       same: fromRuntimePack);
        }

        protected static void AssertFilesDontExist(string dir, string[] filenames, string? label = null)
            => AssertFilesExist(dir, filenames, label, expectToExist: false);

        protected static void AssertFilesExist(string dir, string[] filenames, string? label = null, bool expectToExist=true)
        {
            Assert.True(Directory.Exists(dir), $"[{label}] {dir} not found");
            foreach (string filename in filenames)
            {
                string path = Path.Combine(dir, filename);

                if (expectToExist)
                {
                    Assert.True(File.Exists(path),
                            label != null
                                ? $"{label}: File exists: {path}"
                                : $"File exists: {path}");
                }
                else
                {
                    Assert.False(File.Exists(path),
                            label != null
                                ? $"{label}: {path} should not exist"
                                : $"{path} should not exist");
                }
            }
        }

        protected static void AssertSameFile(string file0, string file1, string? label=null) => AssertFile(file0, file1, label, same: true);
        protected static void AssertNotSameFile(string file0, string file1, string? label=null) => AssertFile(file0, file1, label, same: false);

        protected static void AssertFile(string file0, string file1, string? label=null, bool same=true)
        {
            Assert.True(File.Exists(file0), $"{label}: Expected to find {file0}");
            Assert.True(File.Exists(file1), $"{label}: Expected to find {file1}");

            FileInfo finfo0 = new(file0);
            FileInfo finfo1 = new(file1);

            if (same)
                Assert.True(finfo0.Length == finfo1.Length, $"{label}:{Environment.NewLine}  File sizes don't match for {file0} ({finfo0.Length}), and {file1} ({finfo1.Length})");
            else
                Assert.True(finfo0.Length != finfo1.Length, $"{label}:{Environment.NewLine}  File sizes should not match for {file0} ({finfo0.Length}), and {file1} ({finfo1.Length})");
        }

        protected (int exitCode, string buildOutput) AssertBuild(string args, string label="build", bool expectSuccess=true, IDictionary<string, string>? envVars=null, int? timeoutMs=null)
        {
            var result = RunProcess(s_buildEnv.DotNet, _testOutput, args, workingDir: _projectDir, label: label, envVars: envVars, timeoutMs: timeoutMs ?? s_defaultPerTestTimeoutMs);
            if (expectSuccess)
                Assert.True(0 == result.exitCode, $"Build process exited with non-zero exit code: {result.exitCode}");
            else
                Assert.True(0 != result.exitCode, $"Build should have failed, but it didn't. Process exited with exitCode : {result.exitCode}");

            return result;
        }

        protected void AssertBlazorBundle(string config, bool isPublish, bool dotnetWasmFromRuntimePack, string? binFrameworkDir=null)
        {
            binFrameworkDir ??= FindBlazorBinFrameworkDir(config, isPublish);

            AssertBlazorBootJson(config, isPublish, binFrameworkDir: binFrameworkDir);
            AssertFile(Path.Combine(s_buildEnv.RuntimeNativeDir, "dotnet.wasm"),
                       Path.Combine(binFrameworkDir, "dotnet.wasm"),
                       "Expected dotnet.wasm to be same as the runtime pack",
                       same: dotnetWasmFromRuntimePack);

            string? dotnetJsPath = Directory.EnumerateFiles(binFrameworkDir, "dotnet.*.js").FirstOrDefault();
            Assert.True(dotnetJsPath != null, $"Could not find blazor's dotnet*js in {binFrameworkDir}");

            AssertFile(Path.Combine(s_buildEnv.RuntimeNativeDir, "dotnet.js"),
                        dotnetJsPath!,
                        "Expected dotnet.js to be same as the runtime pack",
                        same: dotnetWasmFromRuntimePack);
        }

        protected void AssertBlazorBootJson(string config, bool isPublish, string? binFrameworkDir=null)
        {
            binFrameworkDir ??= FindBlazorBinFrameworkDir(config, isPublish);

            string bootJsonPath = Path.Combine(binFrameworkDir, "blazor.boot.json");
            Assert.True(File.Exists(bootJsonPath), $"Expected to find {bootJsonPath}");

            string bootJson = File.ReadAllText(bootJsonPath);
            var bootJsonNode = JsonNode.Parse(bootJson);
            var runtimeObj = bootJsonNode?["resources"]?["runtime"]?.AsObject();
            Assert.NotNull(runtimeObj);

            string msgPrefix=$"[{( isPublish ? "publish" : "build" )}]";
            Assert.True(runtimeObj!.Where(kvp => kvp.Key == "dotnet.wasm").Any(), $"{msgPrefix} Could not find dotnet.wasm entry in blazor.boot.json");
            Assert.True(runtimeObj!.Where(kvp => kvp.Key.StartsWith("dotnet.", StringComparison.OrdinalIgnoreCase) &&
                                                    kvp.Key.EndsWith(".js", StringComparison.OrdinalIgnoreCase)).Any(),
                                            $"{msgPrefix} Could not find dotnet.*js in {bootJson}");
        }

        protected string FindBlazorBinFrameworkDir(string config, bool forPublish, string framework = DefaultTargetFramework)
        {
            string basePath = Path.Combine(_projectDir!, "bin", config, framework);
            if (forPublish)
                basePath = FindSubDirIgnoringCase(basePath, "publish");

            return Path.Combine(basePath, "wwwroot", "_framework");
        }

        private string FindSubDirIgnoringCase(string parentDir, string dirName)
        {
            IEnumerable<string> matchingDirs = Directory.EnumerateDirectories(parentDir,
                                                            dirName,
                                                            new EnumerationOptions { MatchCasing = MatchCasing.CaseInsensitive });

            string? first = matchingDirs.FirstOrDefault();
            if (matchingDirs.Count() > 1)
                throw new Exception($"Found multiple directories with names that differ only in case. {string.Join(", ", matchingDirs.ToArray())}");

            return first ?? Path.Combine(parentDir, dirName);
        }

        protected string GetBinDir(string config, string targetFramework=DefaultTargetFramework, string? baseDir=null)
        {
            var dir = baseDir ?? _projectDir;
            Assert.NotNull(dir);
            return Path.Combine(dir!, "bin", config, targetFramework, "browser-wasm");
        }

        protected string GetObjDir(string config, string targetFramework=DefaultTargetFramework, string? baseDir=null)
        {
            var dir = baseDir ?? _projectDir;
            Assert.NotNull(dir);
            return Path.Combine(dir!, "obj", config, targetFramework, "browser-wasm");
        }

        public static (int exitCode, string buildOutput) RunProcess(string path,
                                         ITestOutputHelper _testOutput,
                                         string args = "",
                                         IDictionary<string, string>? envVars = null,
                                         string? workingDir = null,
                                         string? label = null,
                                         bool logToXUnit = true,
                                         int? timeoutMs = null)
        {
            _testOutput.WriteLine($"Running {path} {args}");
            Console.WriteLine($"Running: {path}: {args}");
            Console.WriteLine($"WorkingDirectory: {workingDir}");
            _testOutput.WriteLine($"WorkingDirectory: {workingDir}");
            StringBuilder outputBuilder = new ();
            object syncObj = new();

            var processStartInfo = new ProcessStartInfo
            {
                FileName = path,
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardError = true,
                RedirectStandardOutput = true,
                Arguments = args,
            };

            if (workingDir == null || !Directory.Exists(workingDir))
                throw new Exception($"Working directory {workingDir} not found");

            if (workingDir != null)
                processStartInfo.WorkingDirectory = workingDir;

            if (envVars != null)
            {
                if (envVars.Count > 0)
                    _testOutput.WriteLine("Setting environment variables for execution:");

                foreach (KeyValuePair<string, string> envVar in envVars)
                {
                    processStartInfo.EnvironmentVariables[envVar.Key] = envVar.Value;
                    _testOutput.WriteLine($"\t{envVar.Key} = {envVar.Value}");
                }
            }

            Process process = new ();
            process.StartInfo = processStartInfo;
            process.EnableRaisingEvents = true;

            // AutoResetEvent resetEvent = new (false);
            // process.Exited += (_, _) => { Console.WriteLine ($"- exited called"); resetEvent.Set(); };

            if (!process.Start())
                throw new ArgumentException("No process was started: process.Start() return false.");

            try
            {
                DataReceivedEventHandler logStdErr = (sender, e) => LogData($"[{label}-stderr]", e.Data);
                DataReceivedEventHandler logStdOut = (sender, e) => LogData($"[{label}]", e.Data);

                process.ErrorDataReceived += logStdErr;
                process.OutputDataReceived += logStdOut;
                process.BeginOutputReadLine();
                process.BeginErrorReadLine();

                // process.WaitForExit doesn't work if the process exits too quickly?
                // resetEvent.WaitOne();
                if (!process.WaitForExit(timeoutMs ?? s_defaultPerTestTimeoutMs))
                {
                    // process didn't exit
                    process.Kill(entireProcessTree: true);
                    lock (syncObj)
                    {
                        var lastLines = outputBuilder.ToString().Split('\r', '\n').TakeLast(20);
                        throw new XunitException($"Process timed out. Last 20 lines of output:{Environment.NewLine}{string.Join(Environment.NewLine, lastLines)}");
                    }
                }
                else
                {
                    // this will ensure that all the async event handling
                    // has completed
                    // https://docs.microsoft.com/en-us/dotnet/api/system.diagnostics.process.waitforexit?view=net-5.0#System_Diagnostics_Process_WaitForExit_System_Int32_
                    process.WaitForExit();
                }

                process.ErrorDataReceived -= logStdErr;
                process.OutputDataReceived -= logStdOut;
                process.CancelErrorRead();
                process.CancelOutputRead();

                lock (syncObj)
                {
                    var exitCode = process.ExitCode;
                    return (process.ExitCode, outputBuilder.ToString().Trim('\r', '\n'));
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"-- exception -- {ex}");
                throw;
            }

            void LogData(string label, string? message)
            {
                lock (syncObj)
                {
                    if (logToXUnit && message != null)
                    {
                        _testOutput.WriteLine($"{label} {message}");
                        Console.WriteLine($"{label} {message}");
                    }
                    outputBuilder.AppendLine($"{label} {message}");
                }
            }
        }

        public static string AddItemsPropertiesToProject(string projectFile, string? extraProperties=null, string? extraItems=null, string? atTheEnd=null)
        {
            if (extraProperties == null && extraItems == null && atTheEnd == null)
                return projectFile;

            XmlDocument doc = new();
            doc.Load(projectFile);

            XmlNode root = doc.DocumentElement ?? throw new Exception();
            if (extraItems != null)
            {
                XmlNode node = doc.CreateNode(XmlNodeType.Element, "ItemGroup", null);
                node.InnerXml = extraItems;
                root.AppendChild(node);
            }

            if (extraProperties != null)
            {
                XmlNode node = doc.CreateNode(XmlNodeType.Element, "PropertyGroup", null);
                node.InnerXml = extraProperties;
                root.AppendChild(node);
            }

            if (atTheEnd != null)
            {
                XmlNode node = doc.CreateNode(XmlNodeType.DocumentFragment, "foo", null);
                node.InnerXml = atTheEnd;
                root.InsertAfter(node, root.LastChild);
            }

            doc.Save(projectFile);

            return projectFile;
        }

        public void Dispose()
        {
            if (_projectDir != null && _enablePerTestCleanup)
                _buildContext.RemoveFromCache(_projectDir, keepDir: s_skipProjectCleanup);
        }

        private static string GetEnvironmentVariableOrDefault(string envVarName, string defaultValue)
        {
            string? value = Environment.GetEnvironmentVariable(envVarName);
            return string.IsNullOrEmpty(value) ? defaultValue : value;
        }

        internal BuildPaths GetBuildPaths(BuildArgs buildArgs, bool forPublish=true)
        {
            string objDir = GetObjDir(buildArgs.Config);
            string bundleDir = Path.Combine(GetBinDir(baseDir: _projectDir, config: buildArgs.Config), "AppBundle");
            string wasmDir = Path.Combine(objDir, "wasm", forPublish ? "for-publish" : "for-build");

            return new BuildPaths(wasmDir, objDir, GetBinDir(buildArgs.Config), bundleDir);
        }

        internal IDictionary<string, FileStat> StatFiles(IEnumerable<string> fullpaths)
        {
            Dictionary<string, FileStat> table = new();
            foreach (string file in fullpaths)
            {
                if (File.Exists(file))
                    table.Add(Path.GetFileName(file), new FileStat(FullPath: file, Exists: true, LastWriteTimeUtc: File.GetLastWriteTimeUtc(file), Length: new FileInfo(file).Length));
                else
                    table.Add(Path.GetFileName(file), new FileStat(FullPath: file, Exists: false, LastWriteTimeUtc: DateTime.MinValue, Length: 0));
            }

            return table;
        }

        protected static string s_mainReturns42 = @"
            public class TestClass {
                public static int Main()
                {
                    return 42;
                }
            }";
    }

    public record BuildArgs(string ProjectName,
                            string Config,
                            bool AOT,
                            string ProjectFileContents,
                            string? ExtraBuildArgs);
    public record BuildProduct(string ProjectDir, string LogFile, bool Result);
    internal record FileStat (bool Exists, DateTime LastWriteTimeUtc, long Length, string FullPath);
    internal record BuildPaths(string ObjWasmDir, string ObjDir, string BinDir, string BundleDir);

    public record BuildProjectOptions
    (
        Action? InitProject               = null,
        bool?   DotnetWasmFromRuntimePack = null,
        bool    HasIcudt                  = true,
        bool    UseCache                  = true,
        bool    ExpectSuccess             = true,
        bool    CreateProject             = true,
        bool    Publish                   = true,
        bool    BuildOnlyAfterPublish     = true,
        bool    HasV8Script               = true,
        string? Verbosity                 = null,
        string? Label                     = null,
        string? TargetFramework           = null,
        string? MainJS                    = null
    );

    public record BlazorBuildOptions
    (
        string Id,
        string Config,
        NativeFilesType ExpectedFileType,
        string TargetFramework = BuildTestBase.DefaultTargetFramework
    );
}
