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
using System.Threading.Tasks;
using System.Threading;
using System.Xml;
using Xunit;
using Xunit.Abstractions;
using Xunit.Sdk;
using Microsoft.Playwright;

#nullable enable

// [assembly: CollectionBehavior(CollectionBehavior.CollectionPerAssembly)]

namespace Wasm.Build.Tests
{
    public abstract class BuildTestBase : IClassFixture<SharedBuildPerTestClassFixture>, IDisposable
    {
        public const string DefaultTargetFramework = "net8.0";
        public const string DefaultTargetFrameworkForBlazor = "net8.0";
        private const string DefaultEnvironmentLocale = "en-US";
        protected static readonly char s_unicodeChar = '\u7149';
        protected static readonly bool s_skipProjectCleanup;
        protected static readonly string s_xharnessRunnerCommand;
        protected string? _projectDir;
        protected readonly ITestOutputHelper _testOutput;
        protected string _logPath;
        protected bool _enablePerTestCleanup = false;
        protected SharedBuildPerTestClassFixture _buildContext;
        protected string _nugetPackagesDir = string.Empty;

        private static bool s_isWindows = RuntimeInformation.IsOSPlatform(OSPlatform.Windows);
        // changing Windows's language programistically is complicated and Node is using OS's language to determine
        // what is client's preferred locale and then to load corresponding ICU => skip automatic icu testing with Node
        // on Linux sharding does not work because we rely on LANG env var to check locale and emcc is overwriting it
        protected static RunHost s_hostsForOSLocaleSensitiveTests = RunHost.Chrome;
        // FIXME: use an envvar to override this
        protected static int s_defaultPerTestTimeoutMs = s_isWindows ? 30 * 60 * 1000 : 15 * 60 * 1000;
        protected static BuildEnvironment s_buildEnv;
        private const string s_runtimePackPathPattern = "\\*\\* MicrosoftNetCoreAppRuntimePackDir : '([^ ']*)'";
        private const string s_nugetInsertionTag = "<!-- TEST_RESTORE_SOURCES_INSERTION_LINE -->";
        private static Regex s_runtimePackPathRegex;
        private static int s_testCounter;
        private readonly int _testIdx;

        public static bool IsUsingWorkloads => s_buildEnv.IsWorkload;
        public static bool IsNotUsingWorkloads => !s_buildEnv.IsWorkload;
        public static bool UseWebcil => s_buildEnv.UseWebcil;
        public static string GetNuGetConfigPathFor(string targetFramework) =>
            Path.Combine(BuildEnvironment.TestDataPath, "nuget8.config"); // for now - we are still using net7, but with
                                                                          // targetFramework == "net7.0" ? "nuget7.config" : "nuget8.config");

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

                Console.WriteLine("");
                Console.WriteLine($"==============================================================================================");
                Console.WriteLine($"=============== Running with {(s_buildEnv.IsWorkload ? "Workloads" : "No workloads")} ===============");
                if (UseWebcil)
                    Console.WriteLine($"=============== Using .webcil ===============");
                Console.WriteLine($"==============================================================================================");
                Console.WriteLine("");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Exception: {ex}");
                throw;
            }
        }

        public BuildTestBase(ITestOutputHelper output, SharedBuildPerTestClassFixture buildContext)
        {
            _testIdx = Interlocked.Increment(ref s_testCounter);
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

        public static IEnumerable<IEnumerable<object?>> ConfigWithAOTData(bool aot, string? config = null, string? extraArgs = null)
        {
            if (extraArgs == null)
                extraArgs = string.Empty;

            if (config == null)
            {
                return new IEnumerable<object?>[]
                    {
    #if TEST_DEBUG_CONFIG_ALSO
                        // list of each member data - for Debug+@aot
                        new object?[] { new BuildArgs("placeholder", "Debug", aot, "placeholder", extraArgs) }.AsEnumerable(),
    #endif
                        // list of each member data - for Release+@aot
                        new object?[] { new BuildArgs("placeholder", "Release", aot, "placeholder", extraArgs) }.AsEnumerable()
                    }.AsEnumerable();
            }
            else
            {
                return new IEnumerable<object?>[]
                {
                    new object?[] { new BuildArgs("placeholder", config, aot, "placeholder", extraArgs) }.AsEnumerable()
                };
            }
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

        protected static string RunWithXHarness(string testCommand, string testLogPath, string projectName, string bundleDir,
                                        ITestOutputHelper _testOutput, IDictionary<string, string>? envVars = null,
                                        int expectedAppExitCode = 0, int xharnessExitCode = 0, string? extraXHarnessArgs = null,
                                        string? appArgs = null, string? extraXHarnessMonoArgs = null, bool useWasmConsoleOutput = false)
        {
            _testOutput.WriteLine($"============== {testCommand} =============");
            Directory.CreateDirectory(testLogPath);

            StringBuilder args = new();
            args.Append(s_xharnessRunnerCommand);
            args.Append($" {testCommand}");
            args.Append($" --app=.");
            args.Append($" --output-directory={testLogPath}");
            args.Append($" --expected-exit-code={expectedAppExitCode}");
            args.Append($" {extraXHarnessArgs ?? string.Empty}");

            if (File.Exists("/.dockerenv"))
                args.Append(" --browser-arg=--no-sandbox");

            if (!string.IsNullOrEmpty(EnvironmentVariables.BrowserPathForTests))
            {
                if (!File.Exists(EnvironmentVariables.BrowserPathForTests))
                    throw new Exception($"Cannot find BROWSER_PATH_FOR_TESTS={EnvironmentVariables.BrowserPathForTests}");
                args.Append($" --browser-path=\"{EnvironmentVariables.BrowserPathForTests}\"");
            }

            args.Append(" -- ");
            if (extraXHarnessMonoArgs != null)
            {
                args.Append($" {extraXHarnessMonoArgs}");
            }
            // App arguments
            if (envVars != null)
            {
                var setenv = string.Join(' ', envVars
                                                .Where(ev => ev.Key != "PATH")
                                                .Select(kvp => $"\"--setenv={kvp.Key}={kvp.Value}\"").ToArray());
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
            if (useWasmConsoleOutput)
            {
                string wasmConsolePath = Path.Combine(testLogPath, "wasm-console.log");
                try
                {
                    if (File.Exists(wasmConsolePath))
                        output = File.ReadAllText(wasmConsolePath);
                    else
                        _testOutput.WriteLine($"Warning: Could not find {wasmConsolePath}. Ignoring.");
                }
                catch (IOException ioex)
                {
                    _testOutput.WriteLine($"Warning: Could not read {wasmConsolePath}: {ioex}");
                }
            }

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
                _projectDir = Path.Combine(BuildEnvironment.TmpPath, id);
            _logPath = Path.Combine(s_buildEnv.LogRootPath, id);
            _nugetPackagesDir = Path.Combine(BuildEnvironment.TmpPath, "nuget", id);

            if (Directory.Exists(_nugetPackagesDir))
                Directory.Delete(_nugetPackagesDir, recursive: true);

            Directory.CreateDirectory(_nugetPackagesDir!);
            Directory.CreateDirectory(_logPath);
        }

        protected void InitProjectDir(string dir, bool addNuGetSourceForLocalPackages = false, string targetFramework = DefaultTargetFramework)
        {
            Directory.CreateDirectory(dir);
            File.WriteAllText(Path.Combine(dir, "Directory.Build.props"), s_buildEnv.DirectoryBuildPropsContents);
            File.WriteAllText(Path.Combine(dir, "Directory.Build.targets"), s_buildEnv.DirectoryBuildTargetsContents);

            string targetNuGetConfigPath = Path.Combine(dir, "nuget.config");
            if (addNuGetSourceForLocalPackages)
            {
                File.WriteAllText(targetNuGetConfigPath,
                                    GetNuGetConfigWithLocalPackagesPath(
                                                GetNuGetConfigPathFor(targetFramework),
                                                s_buildEnv.BuiltNuGetsPath));
            }
            else
            {
                File.Copy(GetNuGetConfigPathFor(targetFramework), targetNuGetConfigPath);
            }
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

        protected static BuildArgs ExpandBuildArgs(BuildArgs buildArgs, string extraProperties = "", string extraItems = "", string insertAtEnd = "", string projectTemplate = SimpleProjectTemplate)
        {
            if (buildArgs.AOT)
            {
                extraProperties = $"{extraProperties}\n<RunAOTCompilation>true</RunAOTCompilation>";
                extraProperties += $"\n<EmccVerbose>{s_isWindows}</EmccVerbose>\n";
            }

            if (UseWebcil)
            {
                extraProperties += "<WasmEnableWebcil>true</WasmEnableWebcil>\n";
            }

            extraItems += "<WasmExtraFilesToDeploy Include='index.html' />";

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

        public void InitBlazorWasmProjectDir(string id, string targetFramework = DefaultTargetFrameworkForBlazor)
        {
            InitPaths(id);
            if (Directory.Exists(_projectDir))
                Directory.Delete(_projectDir, recursive: true);
            Directory.CreateDirectory(_projectDir);

            File.WriteAllText(Path.Combine(_projectDir, "nuget.config"),
                                GetNuGetConfigWithLocalPackagesPath(
                                            GetNuGetConfigPathFor(targetFramework),
                                            s_buildEnv.BuiltNuGetsPath));

            File.Copy(Path.Combine(BuildEnvironment.TestDataPath, "Blazor.Directory.Build.props"), Path.Combine(_projectDir, "Directory.Build.props"));
            File.Copy(Path.Combine(BuildEnvironment.TestDataPath, "Blazor.Directory.Build.targets"), Path.Combine(_projectDir, "Directory.Build.targets"));
        }

        private static string GetNuGetConfigWithLocalPackagesPath(string templatePath, string localNuGetsPath)
        {
            string contents = File.ReadAllText(templatePath);
            if (contents.IndexOf(s_nugetInsertionTag, StringComparison.InvariantCultureIgnoreCase) < 0)
                throw new Exception($"Could not find {s_nugetInsertionTag} in {templatePath}");

            return contents.Replace(s_nugetInsertionTag, $@"<add key=""nuget-local"" value=""{localNuGetsPath}"" />");
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
            if (UseWebcil)
                extraProperties += "<WasmEnableWebcil>true</WasmEnableWebcil>";

            // TODO: Can be removed after updated templates propagate in.
            string extraItems = string.Empty;
            if (template == "wasmbrowser")
                extraItems += "<WasmExtraFilesToDeploy Include=\"main.js\" />";
            else
                extraItems += "<WasmExtraFilesToDeploy Include=\"main.mjs\" />";

            AddItemsPropertiesToProject(projectfile, extraProperties, extraItems);

            return projectfile;
        }

        public string CreateBlazorWasmTemplateProject(string id)
        {
            InitBlazorWasmProjectDir(id);
            new DotNetCommand(s_buildEnv, _testOutput, useDefaultArgs: false)
                    .WithWorkingDirectory(_projectDir!)
                    .WithEnvironmentVariable("NUGET_PACKAGES", _nugetPackagesDir)
                    .ExecuteWithCapturedOutput("new blazorwasm")
                    .EnsureSuccessful();

            string projectFile = Path.Combine(_projectDir!, $"{id}.csproj");
            if (UseWebcil)
                AddItemsPropertiesToProject(projectFile, "<WasmEnableWebcil>true</WasmEnableWebcil>");
            return projectFile;
        }

        protected (CommandResult, string) BlazorBuild(BlazorBuildOptions options, params string[] extraArgs)
        {
            if (options.WarnAsError)
                extraArgs = extraArgs.Append("/warnaserror").ToArray();

            var res = BlazorBuildInternal(options.Id, options.Config, publish: false, setWasmDevel: false, extraArgs);
            _testOutput.WriteLine($"BlazorBuild, options.tfm: {options.TargetFramework}");
            AssertDotNetNativeFiles(options.ExpectedFileType, options.Config, forPublish: false, targetFramework: options.TargetFramework);
            AssertBlazorBundle(options.Config,
                               isPublish: false,
                               dotnetWasmFromRuntimePack: options.ExpectedFileType == NativeFilesType.FromRuntimePack,
                               targetFramework: options.TargetFramework);

            return res;
        }

        protected (CommandResult, string) BlazorPublish(BlazorBuildOptions options, params string[] extraArgs)
        {
            var res = BlazorBuildInternal(options.Id, options.Config, publish: true, setWasmDevel: false, extraArgs);
            AssertDotNetNativeFiles(options.ExpectedFileType, options.Config, forPublish: true, targetFramework: options.TargetFramework);
            AssertBlazorBundle(options.Config,
                               isPublish: true,
                               dotnetWasmFromRuntimePack: options.ExpectedFileType == NativeFilesType.FromRuntimePack,
                               targetFramework: options.TargetFramework);

            if (options.ExpectedFileType == NativeFilesType.AOT)
            {
                // check for this too, so we know the format is correct for the negative
                // test for jsinterop.webassembly.dll
                Assert.Contains("Microsoft.JSInterop.dll -> Microsoft.JSInterop.dll.bc", res.Item1.Output);

                // make sure this assembly gets skipped
                Assert.DoesNotContain("Microsoft.JSInterop.WebAssembly.dll -> Microsoft.JSInterop.WebAssembly.dll.bc", res.Item1.Output);

            }

            string objBuildDir = Path.Combine(_projectDir!, "obj", options.Config, options.TargetFramework, "wasm", "for-build");
            // Check that we linked only for publish
            if (options.ExpectRelinkDirWhenPublishing)
                Assert.True(Directory.Exists(objBuildDir), $"Could not find expected {objBuildDir}, which gets created when relinking during Build. This is liokely a test authoring error");
            else
                Assert.False(Directory.Exists(objBuildDir), $"Found unexpected {objBuildDir}, which gets created when relinking during Build");

            return res;
        }

        protected (CommandResult, string) BlazorBuildInternal(string id, string config, bool publish = false, bool setWasmDevel = true, params string[] extraArgs)
        {
            string label = publish ? "publish" : "build";
            _testOutput.WriteLine($"{Environment.NewLine}** {label} **{Environment.NewLine}");

            string logPath = Path.Combine(s_buildEnv.LogRootPath, id, $"{id}-{label}.binlog");
            string[] combinedArgs = new[]
            {
                label, // same as the command name
                $"-bl:{logPath}",
                $"-p:Configuration={config}",
                UseWebcil ? "-p:WasmEnableWebcil=true" : string.Empty,
                "-p:BlazorEnableCompression=false",
                "-nr:false",
                setWasmDevel ? "-p:_WasmDevel=true" : string.Empty
            }.Concat(extraArgs).ToArray();

            CommandResult res = new DotNetCommand(s_buildEnv, _testOutput)
                                        .WithWorkingDirectory(_projectDir!)
                                        .WithEnvironmentVariable("NUGET_PACKAGES", _nugetPackagesDir)
                                        .ExecuteWithCapturedOutput(combinedArgs)
                                        .EnsureSuccessful();

            return (res, logPath);
        }

        protected void AssertDotNetNativeFiles(NativeFilesType type, string config, bool forPublish, string targetFramework)
        {
            string label = forPublish ? "publish" : "build";
            string objBuildDir = Path.Combine(_projectDir!, "obj", config, targetFramework, "wasm", forPublish ? "for-publish" : "for-build");
            string binFrameworkDir = FindBlazorBinFrameworkDir(config, forPublish, framework: targetFramework);

            string srcDir = type switch
            {
                NativeFilesType.FromRuntimePack => s_buildEnv.GetRuntimeNativeDir(targetFramework),
                NativeFilesType.Relinked => objBuildDir,
                NativeFilesType.AOT => objBuildDir,
                _ => throw new ArgumentOutOfRangeException(nameof(type))
            };

            AssertSameFile(Path.Combine(srcDir, "dotnet.native.wasm"), Path.Combine(binFrameworkDir, "dotnet.native.wasm"), label);

            // find dotnet*js
            string? dotnetJsPath = Directory.EnumerateFiles(binFrameworkDir)
                                    .Where(p => Path.GetFileName(p).StartsWith("dotnet.native", StringComparison.OrdinalIgnoreCase) &&
                                                    Path.GetFileName(p).EndsWith(".js", StringComparison.OrdinalIgnoreCase))
                                    .SingleOrDefault();

            Assert.True(!string.IsNullOrEmpty(dotnetJsPath), $"[{label}] Expected to find dotnet.native*js in {binFrameworkDir}");
            AssertSameFile(Path.Combine(srcDir, "dotnet.native.js"), dotnetJsPath!, label);

            if (type != NativeFilesType.FromRuntimePack)
            {
                // check that the files are *not* from runtime pack
                AssertNotSameFile(Path.Combine(s_buildEnv.GetRuntimeNativeDir(targetFramework), "dotnet.native.wasm"), Path.Combine(binFrameworkDir, "dotnet.native.wasm"), label);
                AssertNotSameFile(Path.Combine(s_buildEnv.GetRuntimeNativeDir(targetFramework), "dotnet.native.js"), dotnetJsPath!, label);
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

        protected static void AssertBasicAppBundle(string bundleDir,
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
                useWebcil ? $"{projectName}.webcil" : $"{projectName}.dll";
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
                switch (globalizationMode)
                {
                    case GlobalizationMode.Invariant:
                        break;
                    case GlobalizationMode.FullIcu:
                        expectFULL = true;
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
            }
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

        protected static void AssertDotNetJsSymbols(string bundleDir, bool fromRuntimePack, string targetFramework)
            => AssertFile(Path.Combine(s_buildEnv.GetRuntimeNativeDir(targetFramework), "dotnet.native.js.symbols"),
                            Path.Combine(bundleDir, "dotnet.native.js.symbols"),
                            same: fromRuntimePack);

        protected static void AssertFilesDontExist(string dir, string[] filenames, string? label = null)
            => AssertFilesExist(dir, filenames, label, expectToExist: false);

        protected static void AssertFilesExist(string dir, IEnumerable<string> filenames, string? label = null, bool expectToExist = true)
        {
            string prefix = label != null ? $"{label}: " : string.Empty;
            if (!Directory.Exists(dir))
                throw new XunitException($"[{label}] {dir} not found");
            foreach (string filename in filenames)
            {
                string path = Path.Combine(dir, filename);
                if (expectToExist && !File.Exists(path))
                    throw new XunitException($"{prefix}Expected the file to exist: {path}");

                if (!expectToExist && File.Exists(path))
                    throw new XunitException($"{prefix}Expected the file to *not* exist: {path}");
            }
        }

        protected static void AssertSameFile(string file0, string file1, string? label = null) => AssertFile(file0, file1, label, same: true);
        protected static void AssertNotSameFile(string file0, string file1, string? label = null) => AssertFile(file0, file1, label, same: false);

        protected static void AssertFile(string file0, string file1, string? label = null, bool same = true)
        {
            Assert.True(File.Exists(file0), $"{label}: Expected to find {file0}");
            Assert.True(File.Exists(file1), $"{label}: Expected to find {file1}");

            FileInfo finfo0 = new(file0);
            FileInfo finfo1 = new(file1);

            if (same && finfo0.Length != finfo1.Length)
                throw new XunitException($"{label}:{Environment.NewLine}  File sizes don't match for {file0} ({finfo0.Length}), and {file1} ({finfo1.Length})");

            if (!same && finfo0.Length == finfo1.Length)
                throw new XunitException($"{label}:{Environment.NewLine}  File sizes should not match for {file0} ({finfo0.Length}), and {file1} ({finfo1.Length})");
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

        protected void AssertBlazorBundle(string config, bool isPublish, bool dotnetWasmFromRuntimePack, string targetFramework = DefaultTargetFrameworkForBlazor, string? binFrameworkDir = null)
        {
            binFrameworkDir ??= FindBlazorBinFrameworkDir(config, isPublish, targetFramework);

            AssertBlazorBootJson(config, isPublish, targetFramework != DefaultTargetFrameworkForBlazor, targetFramework, binFrameworkDir: binFrameworkDir);
            AssertFile(Path.Combine(s_buildEnv.GetRuntimeNativeDir(targetFramework), "dotnet.native.wasm"),
                       Path.Combine(binFrameworkDir, "dotnet.native.wasm"),
                       "Expected dotnet.native.wasm to be same as the runtime pack",
                       same: dotnetWasmFromRuntimePack);

            string? dotnetJsPath = Directory.EnumerateFiles(binFrameworkDir, "dotnet.native.*.js").FirstOrDefault();
            Assert.True(dotnetJsPath != null, $"Could not find blazor's dotnet*js in {binFrameworkDir}");

            AssertFile(Path.Combine(s_buildEnv.GetRuntimeNativeDir(targetFramework), "dotnet.native.js"),
                        dotnetJsPath!,
                        "Expected dotnet.native.js to be same as the runtime pack",
                        same: dotnetWasmFromRuntimePack);
        }

        protected void AssertBlazorBootJson(string config, bool isPublish, bool isNet7AndBelow, string targetFramework = DefaultTargetFrameworkForBlazor, string? binFrameworkDir = null)
        {
            binFrameworkDir ??= FindBlazorBinFrameworkDir(config, isPublish, targetFramework);

            string bootJsonPath = Path.Combine(binFrameworkDir, "blazor.boot.json");
            Assert.True(File.Exists(bootJsonPath), $"Expected to find {bootJsonPath}");

            string bootJson = File.ReadAllText(bootJsonPath);
            var bootJsonNode = JsonNode.Parse(bootJson);
            var runtimeObj = bootJsonNode?["resources"]?["runtime"]?.AsObject();
            Assert.NotNull(runtimeObj);

            string msgPrefix = $"[{(isPublish ? "publish" : "build")}]";
            Assert.True(runtimeObj!.Where(kvp => kvp.Key == (isNet7AndBelow ? "dotnet.wasm" : "dotnet.native.wasm")).Any(), $"{msgPrefix} Could not find dotnet.native.wasm entry in blazor.boot.json");
            Assert.True(runtimeObj!.Where(kvp => kvp.Key.StartsWith("dotnet.", StringComparison.OrdinalIgnoreCase) &&
                                                    kvp.Key.EndsWith(".js", StringComparison.OrdinalIgnoreCase)).Any(),
                                            $"{msgPrefix} Could not find dotnet.*js in {bootJson}");
        }

        protected string FindBlazorBinFrameworkDir(string config, bool forPublish, string framework = DefaultTargetFrameworkForBlazor)
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

        protected string GetBinDir(string config, string targetFramework = DefaultTargetFramework, string? baseDir = null)
        {
            var dir = baseDir ?? _projectDir;
            Assert.NotNull(dir);
            return Path.Combine(dir!, "bin", config, targetFramework, "browser-wasm");
        }

        protected string GetObjDir(string config, string targetFramework = DefaultTargetFramework, string? baseDir = null)
        {
            var dir = baseDir ?? _projectDir;
            Assert.NotNull(dir);
            return Path.Combine(dir!, "obj", config, targetFramework, "browser-wasm");
        }

        protected string CreateProjectWithNativeReference(string id)
        {
            CreateBlazorWasmTemplateProject(id);

            string extraItems = @$"
                {GetSkiaSharpReferenceItems()}
                <WasmFilesToIncludeInFileSystem Include=""{Path.Combine(BuildEnvironment.TestAssetsPath, "mono.png")}"" />
            ";
            string projectFile = Path.Combine(_projectDir!, $"{id}.csproj");
            AddItemsPropertiesToProject(projectFile, extraItems: extraItems);

            return projectFile;
        }

        public void BlazorAddRazorButton(string buttonText, string customCode, string methodName = "test", string razorPage = "Pages/Counter.razor")
        {
            string additionalCode = $$"""
                <p role="{{methodName}}">Output: @outputText</p>
                <button class="btn btn-primary" @onclick="{{methodName}}">{{buttonText}}</button>

                @code {
                    private string outputText = string.Empty;
                    public void {{methodName}}()
                    {
                        {{customCode}}
                    }
                }
            """;

            // find blazor's Counter.razor
            string counterRazorPath = Path.Combine(_projectDir!, razorPage);
            if (!File.Exists(counterRazorPath))
                throw new FileNotFoundException($"Could not find {counterRazorPath}");

            string oldContent = File.ReadAllText(counterRazorPath);
            File.WriteAllText(counterRazorPath, oldContent + additionalCode);
        }

        // Keeping these methods with explicit Build/Publish in the name
        // so in the test code it is evident which is being run!
        public Task BlazorRunForBuildWithDotnetRun(string config, Func<IPage, Task>? test = null, string extraArgs = "--no-build", Action<IConsoleMessage>? onConsoleMessage = null)
            => BlazorRunTest($"run -c {config} {extraArgs}", _projectDir!, test, onConsoleMessage);

        public Task BlazorRunForPublishWithWebServer(string config, Func<IPage, Task>? test = null, string extraArgs = "", Action<IConsoleMessage>? onConsoleMessage = null)
            => BlazorRunTest($"{s_xharnessRunnerCommand} wasm webserver --app=. --web-server-use-default-files {extraArgs}",
                             Path.GetFullPath(Path.Combine(FindBlazorBinFrameworkDir(config, forPublish: true), "..")),
                             test, onConsoleMessage);

        public async Task BlazorRunTest(string runArgs,
                                        string workingDirectory,
                                        Func<IPage, Task>? test = null,
                                        Action<IConsoleMessage>? onConsoleMessage = null,
                                        bool detectRuntimeFailures = true)
        {
            using var runCommand = new RunCommand(s_buildEnv, _testOutput)
                                        .WithWorkingDirectory(workingDirectory);

            await using var runner = new BrowserRunner(_testOutput);
            var page = await runner.RunAsync(runCommand, runArgs, onConsoleMessage: OnConsoleMessage);

            await page.Locator("text=Counter").ClickAsync();
            var txt = await page.Locator("p[role='status']").InnerHTMLAsync();
            Assert.Equal("Current count: 0", txt);

            await page.Locator("text=\"Click me\"").ClickAsync();
            txt = await page.Locator("p[role='status']").InnerHTMLAsync();
            Assert.Equal("Current count: 1", txt);

            if (test is not null)
                await test(page);

            void OnConsoleMessage(IConsoleMessage msg)
            {
                if (EnvironmentVariables.ShowBuildOutput)
                    Console.WriteLine($"[{msg.Type}] {msg.Text}");
                _testOutput.WriteLine($"[{msg.Type}] {msg.Text}");

                onConsoleMessage?.Invoke(msg);

                if (detectRuntimeFailures)
                {
                    if (msg.Text.Contains("[MONO] * Assertion") || msg.Text.Contains("Error: [MONO] "))
                        throw new XunitException($"Detected a runtime failure at line: {msg.Text}");
                }
            }
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
            var t = RunProcessAsync(path, _testOutput, args, envVars, workingDir, label, logToXUnit, timeoutMs);
            t.Wait();
            return t.Result;
        }

        public static async Task<(int exitCode, string buildOutput)> RunProcessAsync(string path,
                                         ITestOutputHelper _testOutput,
                                         string args = "",
                                         IDictionary<string, string>? envVars = null,
                                         string? workingDir = null,
                                         string? label = null,
                                         bool logToXUnit = true,
                                         int? timeoutMs = null)
        {
            _testOutput.WriteLine($"Running {path} {args}");
            _testOutput.WriteLine($"WorkingDirectory: {workingDir}");
            StringBuilder outputBuilder = new();
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

                // runtime repo sets this, which interferes with the tests
                processStartInfo.RemoveEnvironmentVariables("MSBuildSDKsPath");
            }

            Process process = new();
            process.StartInfo = processStartInfo;
            process.EnableRaisingEvents = true;

            // AutoResetEvent resetEvent = new (false);
            // process.Exited += (_, _) => { _testOutput.WriteLine ($"- exited called"); resetEvent.Set(); };

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

                using CancellationTokenSource cts = new();
                cts.CancelAfter(timeoutMs ?? s_defaultPerTestTimeoutMs);

                await process.WaitForExitAsync(cts.Token);

                if (cts.IsCancellationRequested)
                {
                    // process didn't exit
                    process.Kill(entireProcessTree: true);
                    lock (syncObj)
                    {
                        var lastLines = outputBuilder.ToString().Split('\r', '\n').TakeLast(20);
                        throw new XunitException($"Process timed out. Last 20 lines of output:{Environment.NewLine}{string.Join(Environment.NewLine, lastLines)}");
                    }
                }

                // this will ensure that all the async event handling has completed
                // and should be called after process.WaitForExit(int)
                // https://docs.microsoft.com/en-us/dotnet/api/system.diagnostics.process.waitforexit?view=net-5.0#System_Diagnostics_Process_WaitForExit_System_Int32_
                process.WaitForExit();

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
                _testOutput.WriteLine($"-- exception -- {ex}");
                throw;
            }

            void LogData(string label, string? message)
            {
                lock (syncObj)
                {
                    if (logToXUnit && message != null)
                    {
                        _testOutput.WriteLine($"{label} {message}");
                    }
                    outputBuilder.AppendLine($"{label} {message}");
                }
                if (EnvironmentVariables.ShowBuildOutput)
                    Console.WriteLine($"{label} {message}");
            }
        }

        public static string AddItemsPropertiesToProject(string projectFile, string? extraProperties = null, string? extraItems = null, string? atTheEnd = null)
        {
            if (!File.Exists(projectFile))
                throw new Exception($"{projectFile} does not exist");
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

        internal BuildPaths GetBuildPaths(BuildArgs buildArgs, bool forPublish = true)
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

        protected static string GetSkiaSharpReferenceItems()
            => @"<PackageReference Include=""SkiaSharp"" Version=""2.88.3"" />
                <PackageReference Include=""SkiaSharp.NativeAssets.WebAssembly"" Version=""2.88.3"" />
                <NativeFileReference Include=""$(SkiaSharpStaticLibraryPath)\3.1.12\st\*.a"" />";

        protected static string s_mainReturns42 = @"
            public class TestClass {
                public static int Main()
                {
                    return 42;
                }
            }";

        private IHostRunner GetHostRunnerFromRunHost(RunHost host) => host switch
        {
            RunHost.V8 => new V8HostRunner(),
            RunHost.NodeJS => new NodeJSHostRunner(),
            _ => new BrowserHostRunner(),
        };

        protected void AssertSubstring(string substring, string full, bool contains)
        {
            if (contains)
                Assert.Contains(substring, full);
            else
                Assert.DoesNotContain(substring, full);
        }
    }

    public record BuildArgs(string ProjectName,
                            string Config,
                            bool AOT,
                            string ProjectFileContents,
                            string? ExtraBuildArgs);
    public record BuildProduct(string ProjectDir, string LogFile, bool Result, string BuildOutput);
    internal record FileStat(bool Exists, DateTime LastWriteTimeUtc, long Length, string FullPath);
    internal record BuildPaths(string ObjWasmDir, string ObjDir, string BinDir, string BundleDir);

    public record BuildProjectOptions
    (
        Action? InitProject = null,
        bool? DotnetWasmFromRuntimePack = null,
        GlobalizationMode? GlobalizationMode = null,
        string? PredefinedIcudt = null,
        bool UseCache = true,
        bool ExpectSuccess = true,
        bool AssertAppBundle = true,
        bool CreateProject = true,
        bool Publish = true,
        bool BuildOnlyAfterPublish = true,
        bool HasV8Script = true,
        string? Verbosity = null,
        string? Label = null,
        string? TargetFramework = null,
        string? MainJS = null,
        bool IsBrowserProject = true,
        IDictionary<string, string>? ExtraBuildEnvironmentVariables = null
    );

    public record BlazorBuildOptions
    (
        string Id,
        string Config,
        NativeFilesType ExpectedFileType,
        string TargetFramework = BuildTestBase.DefaultTargetFrameworkForBlazor,
        bool WarnAsError = true,
        bool ExpectRelinkDirWhenPublishing = false
    );

    public enum GlobalizationMode
    {
        Invariant,       // no icu
        FullIcu,         // full icu data: icudt.dat is loaded
        PredefinedIcu   // user set WasmIcuDataFileName value and we are loading that file
    };

    public enum NativeFilesType { FromRuntimePack, Relinked, AOT };
}
