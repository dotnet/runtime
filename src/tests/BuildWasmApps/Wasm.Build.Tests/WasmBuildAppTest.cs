// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using Xunit;
using Xunit.Abstractions;

#nullable enable

namespace Wasm.Build.Tests
{
    public class WasmBuildAppTest : IDisposable
    {
        private const string TestLogPathEnvVar = "TEST_LOG_PATH";
        private const string SkipProjectCleanupEnvVar = "SKIP_PROJECT_CLEANUP";
        private const string XHarnessRunnerCommandEnvVar = "XHARNESS_CLI_PATH";

        private readonly string _tempDir;
        private readonly ITestOutputHelper _testOutput;
        private readonly string _id;
        private readonly string _logPath;

        private const string s_targetFramework = "net5.0";
        private static string s_runtimeConfig = "Release";
        private static string s_runtimePackDir;
        private static string s_defaultBuildArgs;
        private static readonly string s_logRoot;
        private static readonly string s_emsdkPath;
        private static readonly bool s_skipProjectCleanup;
        private static readonly string s_xharnessRunnerCommand;

        static WasmBuildAppTest()
        {
            DirectoryInfo? solutionRoot = new (AppContext.BaseDirectory);
            while (solutionRoot != null)
            {
                if (File.Exists(Path.Combine(solutionRoot.FullName, "NuGet.config")))
                {
                    break;
                }

                solutionRoot = solutionRoot.Parent;
            }

            if (solutionRoot == null)
            {
                string? buildDir = Environment.GetEnvironmentVariable("WasmBuildSupportDir");

                if (buildDir == null || !Directory.Exists(buildDir))
                    throw new Exception($"Could not find the solution root, or a build dir: {buildDir}");

                s_emsdkPath = Path.Combine(buildDir, "emsdk");
                s_runtimePackDir = Path.Combine(buildDir, "microsoft.netcore.app.runtime.browser-wasm");
                s_defaultBuildArgs = $" /p:WasmBuildSupportDir={buildDir} /p:EMSDK_PATH={s_emsdkPath} ";
            }
            else
            {
                string artifactsBinDir = Path.Combine(solutionRoot.FullName, "artifacts", "bin");
                s_runtimePackDir = Path.Combine(artifactsBinDir, "microsoft.netcore.app.runtime.browser-wasm", s_runtimeConfig);

                string? emsdk = Environment.GetEnvironmentVariable("EMSDK_PATH");
                if (string.IsNullOrEmpty(emsdk))
                    emsdk = Path.Combine(solutionRoot.FullName, "src", "mono", "wasm", "emsdk");
                s_emsdkPath = emsdk;

                s_defaultBuildArgs = $" /p:RuntimeSrcDir={solutionRoot.FullName} /p:RuntimeConfig={s_runtimeConfig} /p:EMSDK_PATH={s_emsdkPath} ";
            }

            string? logPathEnvVar = Environment.GetEnvironmentVariable(TestLogPathEnvVar);
            if (!string.IsNullOrEmpty(logPathEnvVar))
            {
                s_logRoot = logPathEnvVar;
                if (!Directory.Exists(s_logRoot))
                {
                    Directory.CreateDirectory(s_logRoot);
                }
            }
            else
            {
                s_logRoot = Environment.CurrentDirectory;
            }

            string? cleanupVar = Environment.GetEnvironmentVariable(SkipProjectCleanupEnvVar);
            s_skipProjectCleanup = !string.IsNullOrEmpty(cleanupVar) && cleanupVar == "1";

            string? harnessVar = Environment.GetEnvironmentVariable(XHarnessRunnerCommandEnvVar);
            if (string.IsNullOrEmpty(harnessVar))
            {
                throw new Exception($"{XHarnessRunnerCommandEnvVar} not set");
            }

            s_xharnessRunnerCommand = harnessVar;
        }

        public WasmBuildAppTest(ITestOutputHelper output)
        {
            _testOutput = output;
            _id = Path.GetRandomFileName();
            _tempDir = Path.Combine(AppContext.BaseDirectory, _id);
            Directory.CreateDirectory(_tempDir);

            _logPath = Path.Combine(s_logRoot, _id);
            Directory.CreateDirectory(_logPath);

            _testOutput.WriteLine($"Test Id: {_id}");
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


        public static TheoryData<string, bool> ConfigWithAOTData(bool include_aot=true)
        {
            TheoryData<string, bool> data = new()
            {
                { "Debug", false },
                { "Release", false }
            };

            if (include_aot)
            {
                data.Add("Debug", true);
                data.Add("Release", true);
            }

            return data;
        }

        public static TheoryData<string, bool, bool?> InvariantGlobalizationTestData()
        {
            var data = new TheoryData<string, bool, bool?>();
            foreach (var configData in ConfigWithAOTData())
            {
                data.Add((string)configData[0], (bool)configData[1], null);
                data.Add((string)configData[0], (bool)configData[1], true);
                data.Add((string)configData[0], (bool)configData[1], false);
            }
            return data;
        }

        // TODO: check that icu bits have been linked out
        [Theory]
        [MemberData(nameof(InvariantGlobalizationTestData))]
        public void InvariantGlobalization(string config, bool aot, bool? invariantGlobalization)
        {
            File.WriteAllText(Path.Combine(_tempDir, "Program.cs"), @"
            using System;
            using System.Threading.Tasks;

            public class TestClass {
                public static int Main()
                {
                    Console.WriteLine(""Hello, World!"");
                    return 42;
                }
            }
            ");

            string? extraProperties = null;
            if (invariantGlobalization != null)
                extraProperties = $"<InvariantGlobalization>{invariantGlobalization}</InvariantGlobalization>";

            string projectName = $"invariant_{invariantGlobalization?.ToString() ?? "unset"}";
            BuildProject(projectName, config, aot: aot, extraProperties: extraProperties,
                        hasIcudt: invariantGlobalization == null || invariantGlobalization.Value == false,
                        dotnetWasmFromRuntimePack: !(aot || config == "Release"));

            RunAndTestWasmApp(projectName, config, isAOT: aot, expectedExitCode: 42,
                                test: output => Assert.Contains("Hello, World!", output));
        }

        [Theory]
        [MemberData(nameof(ConfigWithAOTData), parameters: /*aot*/ true)]
        public void TopLevelMain(string config, bool aot)
            => TestMain("top_level",
                    @"System.Console.WriteLine(""Hello, World!""); return await System.Threading.Tasks.Task.FromResult(42);",
                    config, aot);

        [Theory]
        [MemberData(nameof(ConfigWithAOTData), parameters: /*aot*/ true)]
        public void AsyncMain(string config, bool aot)
            => TestMain("async_main", @"
            using System;
            using System.Threading.Tasks;

            public class TestClass {
                public static async Task<int> Main()
                {
                    Console.WriteLine(""Hello, World!"");
                    return await Task.FromResult(42);
                }
            }", config, aot);

        [Theory]
        [MemberData(nameof(ConfigWithAOTData), parameters: /*aot*/ true)]
        public void NonAsyncMain(string config, bool aot)
            => TestMain("non_async_main", @"
                using System;
                using System.Threading.Tasks;

                public class TestClass {
                    public static int Main()
                    {
                        Console.WriteLine(""Hello, World!"");
                        return 42;
                    }
                }", config, aot);

        public static TheoryData<string, bool, string[]> MainWithArgsTestData()
        {
            var data = new TheoryData<string, bool, string[]>();
            foreach (var configData in ConfigWithAOTData())
            {
                data.Add((string)configData[0], (bool)configData[1], new string[] { "abc", "foobar" });
                data.Add((string)configData[0], (bool)configData[1], new string[0]);
            }

            return data;
        }

        [Theory]
        [MemberData(nameof(MainWithArgsTestData))]
        public void NonAsyncMainWithArgs(string config, bool aot, string[] args)
            => TestMainWithArgs("non_async_main_args", @"
                public class TestClass {
                    public static int Main(string[] args)
                    {
                        ##CODE##
                        return 42 + count;
                    }
                }", config, aot, args);

        [Theory]
        [MemberData(nameof(MainWithArgsTestData))]
        public void AsyncMainWithArgs(string config, bool aot, string[] args)
            => TestMainWithArgs("async_main_args", @"
                public class TestClass {
                    public static async System.Threading.Tasks.Task<int> Main(string[] args)
                    {
                        ##CODE##
                        return await System.Threading.Tasks.Task.FromResult(42 + count);
                    }
                }", config, aot, args);

        [Theory]
        [MemberData(nameof(MainWithArgsTestData))]
        public void TopLevelWithArgs(string config, bool aot, string[] args)
            => TestMainWithArgs("top_level_args",
                                @"##CODE## return await System.Threading.Tasks.Task.FromResult(42 + count);",
                                config, aot, args);

        void TestMain(string projectName, string programText, string config, bool aot)
        {
            File.WriteAllText(Path.Combine(_tempDir, "Program.cs"), programText);
            BuildProject(projectName, config, aot: aot, dotnetWasmFromRuntimePack: !(aot || config == "Release"));
            RunAndTestWasmApp(projectName, config, isAOT: aot, expectedExitCode: 42,
                                test: output => Assert.Contains("Hello, World!", output));
        }

        void TestMainWithArgs(string projectName, string programFormatString, string config, bool aot, string[] args)
        {
            string code = @"
                    int count = args == null ? 0 : args.Length;
                    System.Console.WriteLine($""args#: {args?.Length}"");
                    foreach (var arg in args ?? System.Array.Empty<string>())
                        System.Console.WriteLine($""arg: {arg}"");
                    ";
            string programText = programFormatString.Replace("##CODE##", code);

            File.WriteAllText(Path.Combine(_tempDir, "Program.cs"), programText);
            BuildProject(projectName, config, aot: aot, dotnetWasmFromRuntimePack: !(aot || config == "Release"));
            RunAndTestWasmApp(projectName, config, isAOT: aot, expectedExitCode: 42 + args.Length, args: string.Join(' ', args),
                test: output =>
                {
                    Assert.Contains($"args#: {args.Length}", output);
                    foreach (var arg in args)
                        Assert.Contains($"arg: {arg}", output);
                });
        }

        private void RunAndTestWasmApp(string projectName, string config, bool isAOT, Action<string> test, int expectedExitCode=0, string? args=null)
        {
            Dictionary<string, string>? envVars = new();
            envVars["XHARNESS_DISABLE_COLORED_OUTPUT"] = "true";
            if (isAOT)
            {
                envVars["EMSDK_PATH"] = s_emsdkPath;
                envVars["MONO_LOG_LEVEL"] = "debug";
                envVars["MONO_LOG_MASK"] = "aot";
            }

            string bundleDir = Path.Combine(GetBinDir(config: config), "AppBundle");
            string v8output = RunWasmTest(projectName, bundleDir, envVars, expectedExitCode, appArgs: args);
            Test(v8output);

            string browserOutput = RunWasmTestBrowser(projectName, bundleDir, envVars, expectedExitCode, appArgs: args);
            Test(browserOutput);

            void Test(string output)
            {
                if (isAOT)
                {
                    Assert.Contains("AOT: image 'System.Private.CoreLib' found.", output);
                    Assert.Contains($"AOT: image '{projectName}' found.", output);
                }
                else
                {
                    Assert.DoesNotContain("AOT: image 'System.Private.CoreLib' found.", output);
                    Assert.DoesNotContain($"AOT: image '{projectName}' found.", output);
                }
            }
        }

        private string RunWithXHarness(string testCommand, string relativeLogPath, string projectName, string bundleDir, IDictionary<string, string>? envVars=null,
                                        int expectedAppExitCode=0, int xharnessExitCode=0, string? extraXHarnessArgs=null, string? appArgs=null)
        {
            _testOutput.WriteLine($"============== {testCommand} =============");
            Console.WriteLine($"============== {testCommand} =============");
            string testLogPath = Path.Combine(_logPath, relativeLogPath);

            StringBuilder args = new();
            args.Append($"exec {s_xharnessRunnerCommand}");
            args.Append($" {testCommand}");
            args.Append($" --app=.");
            args.Append($" --output-directory={testLogPath}");
            args.Append($" --expected-exit-code={expectedAppExitCode}");
            args.Append($" {extraXHarnessArgs ?? string.Empty}");

            args.Append(" -- ");
            // App arguments

            if (envVars != null)
            {
                var setenv = string.Join(' ', envVars.Select(kvp => $"--setenv={kvp.Key}={kvp.Value}").ToArray());
                args.Append($" {setenv}");
            }

            args.Append($" --run {projectName}.dll");
            args.Append($" {appArgs ?? string.Empty}");

            var (exitCode, output) = RunProcess("dotnet",
                                        args: args.ToString(),
                                        workingDir: bundleDir,
                                        envVars: envVars,
                                        label: testCommand);

            File.WriteAllText(Path.Combine(testLogPath, $"xharness.log"), output);

            if (exitCode != xharnessExitCode)
            {
                _testOutput.WriteLine($"Exit code: {exitCode}");
                Assert.True(exitCode == expectedAppExitCode, $"[{testCommand}] Exit code, expected {expectedAppExitCode} but got {exitCode}");
            }

            return output;
        }
        private string RunWasmTest(string projectName, string bundleDir, IDictionary<string, string>? envVars=null, int expectedAppExitCode=0, int xharnessExitCode=0, string? appArgs=null)
            => RunWithXHarness("wasm test", "wasm-test", projectName, bundleDir,
                                    envVars: envVars,
                                    expectedAppExitCode: expectedAppExitCode,
                                    extraXHarnessArgs: "--js-file=runtime.js --engine=V8 -v trace",
                                    appArgs: appArgs);

        private string RunWasmTestBrowser(string projectName, string bundleDir, IDictionary<string, string>? envVars=null, int expectedAppExitCode=0, int xharnessExitCode=0, string? appArgs=null)
            => RunWithXHarness("wasm test-browser", "wasm-test-browser", projectName, bundleDir,
                                    envVars: envVars,
                                    expectedAppExitCode: expectedAppExitCode,
                                    extraXHarnessArgs: "-v trace", // needed to get messages like those for AOT loading
                                    appArgs: appArgs);

        private static void InitProjectDir(string dir)
        {
            File.WriteAllText(Path.Combine(dir, "Directory.Build.props"), s_directoryBuildProps);
            File.WriteAllText(Path.Combine(dir, "Directory.Build.targets"), s_directoryBuildTargets);
        }

        private void BuildProject(string projectName,
                                  string config,
                                  string? extraBuildArgs = null,
                                  string? extraProperties = null,
                                  bool aot = false,
                                  bool? dotnetWasmFromRuntimePack = null,
                                  bool hasIcudt = true)
        {
            if (aot)
                extraProperties = $"{extraProperties}\n<RunAOTCompilation>true</RunAOTCompilation>\n";

            InitProjectDir(_tempDir);

            File.WriteAllText(Path.Combine(_tempDir, $"{projectName}.csproj"),
@$"<Project Sdk=""Microsoft.NET.Sdk"">
  <PropertyGroup>
    <TargetFramework>{s_targetFramework}</TargetFramework>
    <OutputType>Exe</OutputType>
    <WasmGenerateRunV8Script>true</WasmGenerateRunV8Script>
    <WasmMainJSPath>runtime-test.js</WasmMainJSPath>
    {extraProperties ?? string.Empty}
  </PropertyGroup>
</Project>");

            File.Copy(Path.Combine(AppContext.BaseDirectory, "runtime-test.js"), Path.Combine(_tempDir, "runtime-test.js"));

            StringBuilder sb = new();
            sb.Append("publish");
            sb.Append(s_defaultBuildArgs);

            sb.Append($" /p:Configuration={config}");

            string logFilePath = Path.Combine(_logPath, $"{projectName}.binlog");
            _testOutput.WriteLine($"Binlog path: {logFilePath}");
            sb.Append($" /bl:\"{logFilePath}\" /v:minimal /nologo");
            if (extraBuildArgs != null)
                sb.Append($" {extraBuildArgs} ");

            AssertBuild(sb.ToString());

            string bundleDir = Path.Combine(GetBinDir(config: config), "AppBundle");
            AssertBasicAppBundle(bundleDir, projectName, config, hasIcudt);

            dotnetWasmFromRuntimePack ??= !aot;
            AssertDotNetWasmJs(bundleDir, fromRuntimePack: dotnetWasmFromRuntimePack.Value);
        }

        private static void AssertBasicAppBundle(string bundleDir, string projectName, string config, bool hasIcudt=true)
        {
            AssertFilesExist(bundleDir, new []
            {
                "index.html",
                "runtime.js",
                "dotnet.timezones.blat",
                "dotnet.wasm",
                "mono-config.js",
                "dotnet.js",
                "run-v8.sh"
            });

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
        }

        private void AssertDotNetWasmJs(string bundleDir, bool fromRuntimePack)
        {
            string nativeDir = GetRuntimeNativeDir();

            AssertFile(Path.Combine(nativeDir, "dotnet.wasm"), Path.Combine(bundleDir, "dotnet.wasm"), "Expected dotnet.wasm to be same as the runtime pack", same: fromRuntimePack);
            AssertFile(Path.Combine(nativeDir, "dotnet.js"), Path.Combine(bundleDir, "dotnet.js"), "Expected dotnet.js to be same as the runtime pack", same: fromRuntimePack);
        }

        private static void AssertFilesDontExist(string dir, string[] filenames, string? label = null)
            => AssertFilesExist(dir, filenames, label, expectToExist: false);

        private static void AssertFilesExist(string dir, string[] filenames, string? label = null, bool expectToExist=true)
        {
            Assert.True(Directory.Exists(dir), $"[{label}] {dir} not found");
            foreach (string filename in filenames)
            {
                string path = Path.Combine(dir, filename);

                if (expectToExist)
                {
                    Assert.True(File.Exists(path),
                            label != null
                                ? $"{label}: {path} doesn't exist"
                                : $"{path} doesn't exist");
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

        private static void AssertSameFile(string file0, string file1, string? label=null) => AssertFile(file0, file1, label, same: true);
        private static void AssertNotSameFile(string file0, string file1, string? label=null) => AssertFile(file0, file1, label, same: false);

        private static void AssertFile(string file0, string file1, string? label=null, bool same=true)
        {
            Assert.True(File.Exists(file0), $"{label}: Expected to find {file0}");
            Assert.True(File.Exists(file1), $"{label}: Expected to find {file1}");

            FileInfo finfo0 = new(file0);
            FileInfo finfo1 = new(file1);

            if (same)
                Assert.True(finfo0.Length == finfo1.Length, $"{label}: File sizes don't match for {file0} ({finfo0.Length}), and {file1} ({finfo1.Length})");
            else
                Assert.True(finfo0.Length != finfo1.Length, $"{label}: File sizes should not match for {file0} ({finfo0.Length}), and {file1} ({finfo1.Length})");
        }

        private void AssertBuild(string args)
        {
            (int exitCode, _) = RunProcess("dotnet", args, workingDir: _tempDir, label: "build");
            Assert.True(0 == exitCode, $"Build process exited with non-zero exit code: {exitCode}");
        }

        private string GetObjDir(string targetFramework=s_targetFramework, string? baseDir=null, string config="Debug")
            => Path.Combine(baseDir ?? _tempDir, "obj", config, targetFramework, "browser-wasm", "wasm");

        private string GetBinDir(string targetFramework=s_targetFramework, string? baseDir=null, string config="Debug")
            => Path.Combine(baseDir ?? _tempDir, "bin", config, targetFramework, "browser-wasm");

        private string GetRuntimePackDir() => s_runtimePackDir;

        private string GetRuntimeNativeDir()
            => Path.Combine(GetRuntimePackDir(), "runtimes", "browser-wasm", "native");

        public void Dispose()
        {
            if (s_skipProjectCleanup)
                return;

            try
            {
                Directory.Delete(_tempDir, recursive: true);
            }
            catch
            {
                Console.Error.WriteLine($"Failed to delete '{_tempDir}' during test cleanup");
            }
        }

        private (int, string) RunProcess(string path,
                                         string args = "",
                                         IDictionary<string, string>? envVars = null,
                                         string? workingDir = null,
                                         string? label = null,
                                         bool logToXUnit = true)
        {
            _testOutput.WriteLine($"Running: {path} {args}");
            StringBuilder outputBuilder = new ();
            var processStartInfo = new ProcessStartInfo
            {
                FileName = path,
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardError = true,
                RedirectStandardOutput = true,
                Arguments = args,
            };

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

            Process? process = Process.Start(processStartInfo);
            if (process == null)
                throw new ArgumentException($"Process.Start({path} {args}) returned null process");

            process.ErrorDataReceived += (sender, e) => LogData("[stderr]", e.Data);
            process.OutputDataReceived += (sender, e) => LogData("[stdout]", e.Data);

            try
            {
                process.BeginOutputReadLine();
                process.BeginErrorReadLine();
                process.WaitForExit();

                return (process.ExitCode, outputBuilder.ToString().Trim('\r', '\n'));
            }
            catch
            {
                Console.WriteLine(outputBuilder.ToString());
                throw;
            }

            void LogData(string label, string? message)
            {
                if (logToXUnit && message != null)
                {
                    _testOutput.WriteLine($"{label} {message}");
                }
                outputBuilder.AppendLine($"{label} {message}");
            }
        }

        private static string s_directoryBuildProps = @"<Project>
  <PropertyGroup>
    <_WasmTargetsDir Condition=""'$(RuntimeSrcDir)' != ''"">$(RuntimeSrcDir)\src\mono\wasm\build\</_WasmTargetsDir>
    <_WasmTargetsDir Condition=""'$(WasmBuildSupportDir)' != ''"">$(WasmBuildSupportDir)\wasm\</_WasmTargetsDir>
    <EMSDK_PATH Condition=""'$(WasmBuildSupportDir)' != ''"">$(WasmBuildSupportDir)\emsdk\</EMSDK_PATH>

  </PropertyGroup>

  <Import Project=""$(_WasmTargetsDir)WasmApp.LocalBuild.props"" Condition=""Exists('$(_WasmTargetsDir)WasmApp.LocalBuild.props')"" />

  <PropertyGroup>
    <WasmBuildAppDependsOn>PrepareForWasmBuild;$(WasmBuildAppDependsOn)</WasmBuildAppDependsOn>
  </PropertyGroup>
</Project>";

        private static string s_directoryBuildTargets = @"<Project>
  <Target Name=""CheckWasmLocalBuildInputs"" BeforeTargets=""Build"">
    <Error Condition=""'$(RuntimeSrcDir)' == '' and '$(WasmBuildSupportDir)' == ''""
           Text=""Both %24(RuntimeSrcDir) and %24(WasmBuildSupportDir) are not set. Either one of them needs to be set to use local runtime builds"" />

    <Error Condition=""'$(RuntimeSrcDir)' != '' and '$(WasmBuildSupportDir)' != ''""
           Text=""Both %24(RuntimeSrcDir) and %24(WasmBuildSupportDir) are set. "" />

    <Error Condition=""!Exists('$(_WasmTargetsDir)WasmApp.LocalBuild.props')""
           Text=""Could not find WasmApp.LocalBuild.props in $(_WasmTargetsDir)"" />
    <Error Condition=""!Exists('$(_WasmTargetsDir)WasmApp.LocalBuild.targets')""
           Text=""Could not find WasmApp.LocalBuild.targets in $(_WasmTargetsDir)"" />

    <Warning
      Condition=""'$(WasmMainJS)' != '' and '$(WasmGenerateAppBundle)' != 'true'""
      Text=""%24(WasmMainJS) is set when %24(WasmGenerateAppBundle) is not true: it won't be used because an app bundle is not being generated. Possible build authoring error"" />
  </Target>

  <Target Name=""PrepareForWasmBuild"">
    <ItemGroup>
      <WasmAssembliesToBundle Include=""$(TargetDir)publish\*.dll"" />
    </ItemGroup>
  </Target>

  <Import Project=""$(_WasmTargetsDir)WasmApp.LocalBuild.targets"" Condition=""Exists('$(_WasmTargetsDir)WasmApp.LocalBuild.targets')"" />
</Project>";

    }

 }
