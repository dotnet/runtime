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
using System.Threading.Tasks;
using System.Threading;
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
        public const string DefaultTargetFramework = "net9.0";
        public const string DefaultTargetFrameworkForBlazor = "net9.0";
        public const string TargetFrameworkForTasks = "net9.0";
        private const string DefaultEnvironmentLocale = "en-US";
        protected static readonly char s_unicodeChar = '\u7149';
        protected static readonly bool s_skipProjectCleanup;
        protected static readonly string s_xharnessRunnerCommand;
        protected readonly ITestOutputHelper _testOutput;
        protected string _logPath;
        protected bool _enablePerTestCleanup = false;
        protected SharedBuildPerTestClassFixture _buildContext;
        protected string _nugetPackagesDir = string.Empty;
        private ProjectProviderBase _providerOfBaseType;

        /* This will trigger importing WasmOverridePacks.targets for the tests,
         * which will override the runtime pack with with the locally built one.
         * But note that this only partially helps with "switching workloads" because
         * the tasks/targets, aot compiler, etc would still be from the old version
         */
        public bool UseWBTOverridePackTargets = false;

        private static readonly char[] s_charsToReplace = new[] { '.', '-', '+' };
        private static bool s_isWindows = RuntimeInformation.IsOSPlatform(OSPlatform.Windows);
        // changing Windows's language programistically is complicated and Node is using OS's language to determine
        // what is client's preferred locale and then to load corresponding ICU => skip automatic icu testing with Node
        // on Linux sharding does not work because we rely on LANG env var to check locale and emcc is overwriting it
        protected static RunHost s_hostsForOSLocaleSensitiveTests = RunHost.Chrome;
        // FIXME: use an envvar to override this
        protected static int s_defaultPerTestTimeoutMs = s_isWindows ? 30 * 60 * 1000 : 15 * 60 * 1000;
        public static BuildEnvironment s_buildEnv;
        private const string s_nugetInsertionTag = "<!-- TEST_RESTORE_SOURCES_INSERTION_LINE -->";

        public static bool IsUsingWorkloads => s_buildEnv.IsWorkload;
        public static bool IsNotUsingWorkloads => !s_buildEnv.IsWorkload;
        public static bool IsWorkloadWithMultiThreadingForDefaultFramework => s_buildEnv.IsWorkloadWithMultiThreadingForDefaultFramework;
        public static bool UseWebcil => s_buildEnv.UseWebcil;
        public static string GetNuGetConfigPathFor(string targetFramework)
            => Path.Combine(BuildEnvironment.TestDataPath, targetFramework == "net9.0" ? "nuget9.config" : "nuget8.config");

        public TProvider GetProvider<TProvider>() where TProvider : ProjectProviderBase
            => (TProvider)_providerOfBaseType;

        protected string? _projectDir
        {
            get => _providerOfBaseType.ProjectDir;
            set => _providerOfBaseType.ProjectDir = value;
        }

        static BuildTestBase()
        {
            try
            {
                s_buildEnv = new BuildEnvironment();

                s_skipProjectCleanup = !string.IsNullOrEmpty(EnvironmentVariables.SkipProjectCleanup) && EnvironmentVariables.SkipProjectCleanup == "1";

                if (string.IsNullOrEmpty(EnvironmentVariables.XHarnessCliPath))
                    s_xharnessRunnerCommand = "xharness";
                else
                    s_xharnessRunnerCommand = EnvironmentVariables.XHarnessCliPath;

                Console.WriteLine("");
                Console.WriteLine($"==============================================================================================");
                Console.WriteLine($"=============== Running with {(s_buildEnv.IsWorkload ? "Workloads" : "No workloads")} ===============");
                if (UseWebcil)
                    Console.WriteLine($"=============== Using webcil-in-wasm ===============");
                else
                    Console.WriteLine($"=============== Webcil disabled ===============");
                Console.WriteLine ($"============== Multi-threading runtime pack for {DefaultTargetFramework} is {(IsWorkloadWithMultiThreadingForDefaultFramework ? "available" : "not available")} ==============");
                Console.WriteLine($"==============================================================================================");
                Console.WriteLine("");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Exception: {ex}");
                throw;
            }
        }

        public BuildTestBase(ProjectProviderBase providerBase, ITestOutputHelper output, SharedBuildPerTestClassFixture buildContext)
        {
            _buildContext = buildContext;
            _testOutput = new TestOutputWrapper(output);
            _logPath = s_buildEnv.LogRootPath; // FIXME:
            _providerOfBaseType = providerBase;
        }

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

        public (CommandResult res, string logPath) BuildProjectWithoutAssert(
            string id,
            string config,
            BuildProjectOptions buildProjectOptions,
            params string[] extraArgs)
        {
            string buildType = buildProjectOptions.Publish ? "publish" : "build";
            string logFileSuffix = buildProjectOptions.Label == null ? string.Empty : buildProjectOptions.Label.Replace(' ', '_') + "-";
            string logFilePath = Path.Combine(s_buildEnv.LogRootPath, id, $"{id}-{logFileSuffix}{buildType}.binlog");

            _testOutput.WriteLine($"{Environment.NewLine}** -------- {buildType} -------- **{Environment.NewLine}");
            _testOutput.WriteLine($"Binlog path: {logFilePath}");

            List<string> commandLineArgs = new()
            {
                buildType,
                $"-bl:{logFilePath}",
                $"-p:Configuration={config}",
                "-nr:false"
            };
            commandLineArgs.AddRange(extraArgs);

            if (buildProjectOptions.Publish && buildProjectOptions.BuildOnlyAfterPublish)
                commandLineArgs.Append("-p:WasmBuildOnlyAfterPublish=true");

            var cmd = new DotNetCommand(s_buildEnv, _testOutput)
                                    .WithWorkingDirectory(_projectDir!)
                                    .WithEnvironmentVariable("NUGET_PACKAGES", _nugetPackagesDir)
                                    .WithEnvironmentVariables(buildProjectOptions.ExtraBuildEnvironmentVariables);
            if (UseWBTOverridePackTargets && s_buildEnv.IsWorkload)
                cmd.WithEnvironmentVariable("WBTOverrideRuntimePack", "true");

            CommandResult res = cmd.ExecuteWithCapturedOutput(commandLineArgs.ToArray());
            if (buildProjectOptions.ExpectSuccess)
                res.EnsureSuccessful();
            else if (res.ExitCode == 0)
                throw new XunitException($"Build should have failed, but it didn't. Process exited with exitCode : {res.ExitCode}");

            return (res, logFilePath);
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

            TestUtils.AssertSubstring("AOT: image 'System.Private.CoreLib' found.", output, contains: buildArgs.AOT);

            if (s_isWindows && buildArgs.ProjectName.Contains(s_unicodeChar))
            {
                // unicode chars in output on Windows are decoded in unknown way, so finding utf8 string is more complicated
                string projectNameCore = buildArgs.ProjectName.Trim(new char[] {s_unicodeChar});
                TestUtils.AssertMatches(@$"AOT: image '{projectNameCore}\S+' found.", output, contains: buildArgs.AOT);
            }
            else
            {
                TestUtils.AssertSubstring($"AOT: image '{buildArgs.ProjectName}' found.", output, contains: buildArgs.AOT);
            }

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

            // `/.dockerenv` - is to check if this is running in a codespace
            if (File.Exists("/.dockerenv"))
                args.Append(" --browser-arg=--no-sandbox");

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
                    throw new XunitException($"[{testCommand}] Exit code, expected {expectedAppExitCode} but got {exitCode} for command: {args}");
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

        protected void InitProjectDir(string dir, bool addNuGetSourceForLocalPackages = true, string targetFramework = DefaultTargetFramework)
        {
            Directory.CreateDirectory(dir);
            File.WriteAllText(Path.Combine(dir, "Directory.Build.props"), s_buildEnv.DirectoryBuildPropsContents);
            File.WriteAllText(Path.Combine(dir, "Directory.Build.targets"), s_buildEnv.DirectoryBuildTargetsContents);
            if (UseWBTOverridePackTargets)
                File.Copy(BuildEnvironment.WasmOverridePacksTargetsPath, Path.Combine(dir, Path.GetFileName(BuildEnvironment.WasmOverridePacksTargetsPath)), overwrite: true);

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
                <RuntimeIdentifier>browser-wasm</RuntimeIdentifier>
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

            extraItems += "<WasmExtraFilesToDeploy Include='index.html' />";

            string projectContents = projectTemplate
                                        .Replace("##EXTRA_PROPERTIES##", extraProperties)
                                        .Replace("##EXTRA_ITEMS##", extraItems)
                                        .Replace("##INSERT_AT_END##", insertAtEnd);
            return buildArgs with { ProjectFileContents = projectContents };
        }

        protected static string GetNuGetConfigWithLocalPackagesPath(string templatePath, string localNuGetsPath)
        {
            string contents = File.ReadAllText(templatePath);
            if (contents.IndexOf(s_nugetInsertionTag, StringComparison.InvariantCultureIgnoreCase) < 0)
                throw new Exception($"Could not find {s_nugetInsertionTag} in {templatePath}");

            return contents.Replace(s_nugetInsertionTag, $@"<add key=""nuget-local"" value=""{localNuGetsPath}"" />");
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

        public static (int exitCode, string buildOutput) RunProcess(string path,
                                         ITestOutputHelper _testOutput,
                                         string args = "",
                                         IDictionary<string, string>? envVars = null,
                                         string? workingDir = null,
                                         string? label = null,
                                         int? timeoutMs = null)
        {
            var t = RunProcessAsync(path, _testOutput, args, envVars, workingDir, label, timeoutMs);
            t.Wait();
            return t.Result;
        }

        public static async Task<(int exitCode, string buildOutput)> RunProcessAsync(string path,
                                         ITestOutputHelper _testOutput,
                                         string args = "",
                                         IDictionary<string, string>? envVars = null,
                                         string? workingDir = null,
                                         string? label = null,
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
                    if (message != null)
                    {
                        _testOutput.WriteLine($"{label} {message}");
                    }
                    outputBuilder.AppendLine($"{label} {message}");
                }
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

        public static string GetRandomId() => TestUtils.FixupSymbolName(Path.GetRandomFileName());

        internal BuildPaths GetBuildPaths(BuildArgs buildArgs, bool forPublish = true)
        {
            string objDir = GetObjDir(buildArgs.Config);
            string bundleDir = Path.Combine(GetBinDir(baseDir: _projectDir, config: buildArgs.Config), "AppBundle");
            string wasmDir = Path.Combine(objDir, "wasm", forPublish ? "for-publish" : "for-build");

            return new BuildPaths(wasmDir, objDir, GetBinDir(buildArgs.Config), bundleDir);
        }

        protected static string GetSkiaSharpReferenceItems()
            => @"<PackageReference Include=""SkiaSharp"" Version=""2.88.6"" />
                <PackageReference Include=""SkiaSharp.NativeAssets.WebAssembly"" Version=""2.88.6"" />
                <NativeFileReference Include=""$(SkiaSharpStaticLibraryPath)\3.1.34\st\*.a"" />";

        protected static string s_mainReturns42 = @"
            public class TestClass {
                public static int Main()
                {
                    return 42;
                }
            }";

        private static IHostRunner GetHostRunnerFromRunHost(RunHost host) => host switch
        {
            RunHost.V8 => new V8HostRunner(),
            RunHost.NodeJS => new NodeJSHostRunner(),
            _ => new BrowserHostRunner(),
        };
    }

    public record BuildArgs(string ProjectName,
                            string Config,
                            bool AOT,
                            string ProjectFileContents,
                            string? ExtraBuildArgs);
    public record BuildProduct(string ProjectDir, string LogFile, bool Result, string BuildOutput);

    public enum NativeFilesType { FromRuntimePack, Relinked, AOT };
}
