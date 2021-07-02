// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using Xunit;
using Xunit.Abstractions;
using Xunit.Sdk;

#nullable enable

namespace Wasm.Build.Tests
{
    public abstract class BuildTestBase : IClassFixture<SharedBuildPerTestClassFixture>, IDisposable
    {
        protected const string TestLogPathEnvVar = "TEST_LOG_PATH";
        protected const string SkipProjectCleanupEnvVar = "SKIP_PROJECT_CLEANUP";
        protected const string XHarnessRunnerCommandEnvVar = "XHARNESS_CLI_PATH";
        protected const string s_targetFramework = "net5.0";
        protected static string s_runtimeConfig = "Release";
        protected static string s_runtimePackDir;
        protected static string s_defaultBuildArgs;
        protected static readonly string s_logRoot;
        protected static readonly string s_emsdkPath;
        protected static readonly bool s_skipProjectCleanup;
        protected static readonly string s_xharnessRunnerCommand;
        protected string? _projectDir;
        protected readonly ITestOutputHelper _testOutput;
        protected string _logPath;
        protected bool _enablePerTestCleanup = false;
        protected SharedBuildPerTestClassFixture _buildContext;

        static BuildTestBase()
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
                s_xharnessRunnerCommand = "xharness";
            }
            else
            {
                s_xharnessRunnerCommand = $"exec {harnessVar}";
            }
        }

        public BuildTestBase(ITestOutputHelper output, SharedBuildPerTestClassFixture buildContext)
        {
            _buildContext = buildContext;
            _testOutput = output;
            _logPath = s_logRoot; // FIXME:
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

        public static IEnumerable<IEnumerable<object?>> ConfigWithAOTData(bool aot)
            => new IEnumerable<object?>[]
                {
#if TEST_DEBUG_CONFIG_ALSO
                    // list of each member data - for Debug+@aot
                    new object?[] { new BuildArgs("placeholder", "Debug", aot, "placeholder", string.Empty) }.AsEnumerable(),
#endif

                    // list of each member data - for Release+@aot
                    new object?[] { new BuildArgs("placeholder", "Release", aot, "placeholder", string.Empty) }.AsEnumerable()
                }.AsEnumerable();

        public static IEnumerable<object?[]> BuildAndRunData(bool aot = false,
                                                                        RunHost host = RunHost.All,
                                                                        params object[] parameters)
            => ConfigWithAOTData(aot)
                    .Multiply(parameters)
                    .WithRunHosts(host)
                    .UnwrapItemsAsArrays();

        protected string RunAndTestWasmApp(BuildArgs buildArgs,
                                           RunHost host,
                                           string id,
                                           Action<string>? test=null,
                                           string? buildDir = null,
                                           int expectedExitCode = 0,
                                           string? args = null,
                                           Dictionary<string, string>? envVars = null)
        {
            buildDir ??= _projectDir;
            envVars ??= new();
            envVars["XHARNESS_DISABLE_COLORED_OUTPUT"] = "true";
            if (buildArgs.AOT)
            {
                envVars["EMSDK_PATH"] = s_emsdkPath;
                envVars["MONO_LOG_LEVEL"] = "debug";
                envVars["MONO_LOG_MASK"] = "aot";
            }

            string bundleDir = Path.Combine(GetBinDir(baseDir: buildDir, config: buildArgs.Config), "AppBundle");
            (string testCommand, string extraXHarnessArgs) = host switch
            {
                RunHost.V8 => ("wasm test", "--js-file=runtime.js --engine=V8 -v trace"),
                _          => ("wasm test-browser", $"-v trace -b {host}")
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
                var setenv = string.Join(' ', envVars.Select(kvp => $"--setenv={kvp.Key}={kvp.Value}").ToArray());
                args.Append($" {setenv}");
            }

            args.Append($" --run {projectName}.dll");
            args.Append($" {appArgs ?? string.Empty}");

            _testOutput.WriteLine(string.Empty);
            _testOutput.WriteLine($"---------- Running with {testCommand} ---------");
            var (exitCode, output) = RunProcess("dotnet", _testOutput,
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

        [MemberNotNull(nameof(_projectDir), nameof(_logPath))]
        protected void InitPaths(string id)
        {
            _projectDir = Path.Combine(AppContext.BaseDirectory, id);
            _logPath = Path.Combine(s_logRoot, id);

            Directory.CreateDirectory(_logPath);
        }

        protected static void InitProjectDir(string dir)
        {
            Directory.CreateDirectory(dir);
            File.WriteAllText(Path.Combine(dir, "Directory.Build.props"), s_directoryBuildProps);
            File.WriteAllText(Path.Combine(dir, "Directory.Build.targets"), s_directoryBuildTargets);
        }

        protected const string SimpleProjectTemplate =
            @$"<Project Sdk=""Microsoft.NET.Sdk"">
              <PropertyGroup>
                <TargetFramework>{s_targetFramework}</TargetFramework>
                <OutputType>Exe</OutputType>
                <WasmGenerateRunV8Script>true</WasmGenerateRunV8Script>
                <WasmMainJSPath>runtime-test.js</WasmMainJSPath>
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
                extraProperties = $"{extraProperties}\n<RunAOTCompilation>true</RunAOTCompilation>\n<EmccVerbose>false</EmccVerbose>\n";

            string projectContents = projectTemplate
                                        .Replace("##EXTRA_PROPERTIES##", extraProperties)
                                        .Replace("##EXTRA_ITEMS##", extraItems)
                                        .Replace("##INSERT_AT_END##", insertAtEnd);
            return buildArgs with { ProjectFileContents = projectContents };
        }

        public (string projectDir, string buildOutput) BuildProject(BuildArgs buildArgs,
                                  Action initProject,
                                  string id,
                                  bool? dotnetWasmFromRuntimePack = null,
                                  bool hasIcudt = true,
                                  bool useCache = true,
                                  bool expectSuccess = true,
                                  bool createProject = true)
        {
            if (useCache && _buildContext.TryGetBuildFor(buildArgs, out BuildProduct? product))
            {
                Console.WriteLine ($"Using existing build found at {product.ProjectDir}, with build log at {product.LogFile}");

                Assert.True(product.Result, $"Found existing build at {product.ProjectDir}, but it had failed. Check build log at {product.LogFile}");
                _projectDir = product.ProjectDir;

                // use this test's id for the run logs
                _logPath = Path.Combine(s_logRoot, id);
                return (_projectDir, "FIXME");
            }

            if (createProject)
            {
                InitPaths(id);
                InitProjectDir(_projectDir);
                initProject?.Invoke();

                File.WriteAllText(Path.Combine(_projectDir, $"{buildArgs.ProjectName}.csproj"), buildArgs.ProjectFileContents);
                File.Copy(Path.Combine(AppContext.BaseDirectory, "runtime-test.js"), Path.Combine(_projectDir, "runtime-test.js"));
            }
            else if (_projectDir is null)
            {
                throw new Exception("_projectDir should be set, to use createProject=false");
            }

            StringBuilder sb = new();
            sb.Append("publish");
            sb.Append(s_defaultBuildArgs);

            sb.Append($" /p:Configuration={buildArgs.Config}");

            string logFilePath = Path.Combine(_logPath, $"{buildArgs.ProjectName}.binlog");
            _testOutput.WriteLine($"-------- Building ---------");
            _testOutput.WriteLine($"Binlog path: {logFilePath}");
            sb.Append($" /bl:\"{logFilePath}\" /v:minimal /nologo");
            if (buildArgs.ExtraBuildArgs != null)
                sb.Append($" {buildArgs.ExtraBuildArgs} ");

            Console.WriteLine($"Building {buildArgs.ProjectName} in {_projectDir}");

            (int exitCode, string buildOutput) result;
            try
            {
                result = AssertBuild(sb.ToString(), id, expectSuccess: expectSuccess);
                if (expectSuccess)
                {
                    string bundleDir = Path.Combine(GetBinDir(config: buildArgs.Config), "AppBundle");
                    dotnetWasmFromRuntimePack ??= !buildArgs.AOT;
                    AssertBasicAppBundle(bundleDir, buildArgs.ProjectName, buildArgs.Config, hasIcudt, dotnetWasmFromRuntimePack.Value);
                }

                if (useCache)
                {
                    _buildContext.CacheBuild(buildArgs, new BuildProduct(_projectDir, logFilePath, true));
                    Console.WriteLine($"caching build for {buildArgs}");
                }

                return (_projectDir, result.buildOutput);
            }
            catch
            {
                if (useCache)
                    _buildContext.CacheBuild(buildArgs, new BuildProduct(_projectDir, logFilePath, false));
                throw;
            }
        }

        protected static void AssertBasicAppBundle(string bundleDir, string projectName, string config, bool hasIcudt=true, bool dotnetWasmFromRuntimePack=true)
        {
            AssertFilesExist(bundleDir, new []
            {
                "index.html",
                "runtime.js",
                "dotnet.timezones.blat",
                "dotnet.wasm",
                "mono-config.json",
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

            AssertDotNetWasmJs(bundleDir, fromRuntimePack: dotnetWasmFromRuntimePack);
        }

        protected static void AssertDotNetWasmJs(string bundleDir, bool fromRuntimePack)
        {
            string nativeDir = GetRuntimeNativeDir();

            AssertNativeFile("dotnet.wasm");
            AssertNativeFile("dotnet.js");

            void AssertNativeFile(string file)
                => AssertFile(Path.Combine(nativeDir, file),
                              Path.Combine(bundleDir, file),
                              $"Expected {file} to be {(fromRuntimePack ? "the same as" : "different from")} the runtime pack",
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

        protected static void AssertSameFile(string file0, string file1, string? label=null) => AssertFile(file0, file1, label, same: true);
        protected static void AssertNotSameFile(string file0, string file1, string? label=null) => AssertFile(file0, file1, label, same: false);

        protected static void AssertFile(string file0, string file1, string? label=null, bool same=true)
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

        protected (int exitCode, string buildOutput) AssertBuild(string args, string label="build", bool expectSuccess=true)
        {
            var result = RunProcess("dotnet", _testOutput, args, workingDir: _projectDir, label: label);
            if (expectSuccess)
                Assert.True(0 == result.exitCode, $"Build process exited with non-zero exit code: {result.exitCode}");
            else
                Assert.True(0 != result.exitCode, $"Build should have failed, but it didn't. Process exited with exitCode : {result.exitCode}");

            return result;
        }

        // protected string GetObjDir(string targetFramework=s_targetFramework, string? baseDir=null, string config="Debug")
            // => Path.Combine(baseDir ?? _projectDir, "obj", config, targetFramework, "browser-wasm", "wasm");

        protected string GetBinDir(string config, string targetFramework=s_targetFramework, string? baseDir=null)
        {
            var dir = baseDir ?? _projectDir;
            Assert.NotNull(dir);
            return Path.Combine(dir!, "bin", config, targetFramework, "browser-wasm");
        }

        protected static string GetRuntimePackDir() => s_runtimePackDir;

        protected static string GetRuntimeNativeDir()
            => Path.Combine(GetRuntimePackDir(), "runtimes", "browser-wasm", "native");


        public static (int exitCode, string buildOutput) RunProcess(string path,
                                         ITestOutputHelper _testOutput,
                                         string args = "",
                                         IDictionary<string, string>? envVars = null,
                                         string? workingDir = null,
                                         string? label = null,
                                         bool logToXUnit = true)
        {
            _testOutput.WriteLine($"Running {path} {args}");
            Console.WriteLine($"Running: {path}: {args}");
            Console.WriteLine($"WorkingDirectory: {workingDir}");
            _testOutput.WriteLine($"WorkingDirectory: {workingDir}");
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

            process.ErrorDataReceived += (sender, e) => LogData($"[{label}-stderr]", e.Data);
            process.OutputDataReceived += (sender, e) => LogData($"[{label}]", e.Data);
            // AutoResetEvent resetEvent = new (false);
            // process.Exited += (_, _) => { Console.WriteLine ($"- exited called"); resetEvent.Set(); };

            if (!process.Start())
                throw new ArgumentException("No process was started: process.Start() return false.");

            try
            {
                process.BeginOutputReadLine();
                process.BeginErrorReadLine();

                // process.WaitForExit doesn't work if the process exits too quickly?
                // resetEvent.WaitOne();
                process.WaitForExit();
                return (process.ExitCode, outputBuilder.ToString().Trim('\r', '\n'));
            }
            catch (Exception ex)
            {
                Console.WriteLine($"-- exception -- {ex}");
                throw;
            }

            void LogData(string label, string? message)
            {
                if (logToXUnit && message != null)
                {
                    _testOutput.WriteLine($"{label} {message}");
                    Console.WriteLine($"{label} {message}");
                }
                outputBuilder.AppendLine($"{label} {message}");
            }
        }

        public void Dispose()
        {
            if (s_skipProjectCleanup || !_enablePerTestCleanup)
                return;

            if (_projectDir != null)
                _buildContext.RemoveFromCache(_projectDir);
        }

        protected static string s_mainReturns42 = @"
            public class TestClass {
                public static int Main()
                {
                    return 42;
                }
            }";

        protected static string s_directoryBuildProps = @"<Project>
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

        protected static string s_directoryBuildTargets = @"<Project>
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
      <WasmAssembliesToBundle Include=""$(TargetDir)publish\**\*.dll"" />
    </ItemGroup>
  </Target>

  <Import Project=""$(_WasmTargetsDir)WasmApp.LocalBuild.targets"" Condition=""Exists('$(_WasmTargetsDir)WasmApp.LocalBuild.targets')"" />
</Project>";

    }

    public record BuildArgs(string ProjectName, string Config, bool AOT, string ProjectFileContents, string? ExtraBuildArgs);
    public record BuildProduct(string ProjectDir, string LogFile, bool Result);
 }
