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
using System.Text.RegularExpressions;
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
        public const string DefaultTargetFramework = "net8.0";
        protected static readonly bool s_skipProjectCleanup;
        protected static readonly string s_xharnessRunnerCommand;
        protected string? _projectDir;
        protected readonly ITestOutputHelper _testOutput;
        protected string _logPath;
        protected bool _enablePerTestCleanup = false;
        protected SharedBuildPerTestClassFixture _buildContext;
        protected string _nugetPackagesDir = string.Empty;

        // FIXME: use an envvar to override this
        protected static int s_defaultPerTestTimeoutMs = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? 30*60*1000 : 15*60*1000;
        protected static BuildEnvironment s_buildEnv;
        private const string s_runtimePackPathPattern = "\\*\\* MicrosoftNetCoreAppRuntimePackDir : '([^ ']*)'";
        private const string s_nugetInsertionTag = "<!-- TEST_RESTORE_SOURCES_INSERTION_LINE -->";
        private static Regex s_runtimePackPathRegex;
        private static int s_testCounter;
        private readonly int _testIdx;

        public static bool IsUsingWorkloads => s_buildEnv.IsWorkload;
        public static bool IsNotUsingWorkloads => !s_buildEnv.IsWorkload;
        public static string GetNuGetConfigPathFor(string targetFramework) =>
            Path.Combine(BuildEnvironment.TestDataPath, "nuget8.config"); // for now - we are still using net7, but with
                            // targetFramework == "net7.0" ? "nuget7.config" : "nuget8.config");

        static BuildTestBase()
        {
            try
            {
                s_buildEnv = new BuildEnvironment();
                if (EnvironmentVariables.WasiSdkPath is null)
                    throw new Exception($"Error: WASI_SDK_PATH is not set");

                s_buildEnv.EnvVars["WASI_SDK_PATH"] = EnvironmentVariables.WasiSdkPath;
                s_runtimePackPathRegex = new Regex(s_runtimePackPathPattern);

                s_skipProjectCleanup = !string.IsNullOrEmpty(EnvironmentVariables.SkipProjectCleanup) && EnvironmentVariables.SkipProjectCleanup == "1";

                if (string.IsNullOrEmpty(EnvironmentVariables.XHarnessCliPath))
                    s_xharnessRunnerCommand = "xharness";
                else
                    s_xharnessRunnerCommand = EnvironmentVariables.XHarnessCliPath;

                Console.WriteLine ("");
                Console.WriteLine ($"==============================================================================================");
                Console.WriteLine ($"=============== Running with {(s_buildEnv.IsWorkload ? "Workloads" : "No workloads")} ===============");
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
            _testIdx = Interlocked.Increment(ref s_testCounter);
            _buildContext = buildContext;
            _testOutput = output;
            _logPath = s_buildEnv.LogRootPath; // FIXME:
        }

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
                _testOutput.WriteLine ($"Using existing build found at {product.ProjectDir}, with build log at {product.LogFile}");

                if (!product.Result)
                    throw new XunitException($"Found existing build at {product.ProjectDir}, but it had failed. Check build log at {product.LogFile}");
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
                File.Copy(Path.Combine(AppContext.BaseDirectory,
                                        options.TargetFramework == "net8.0" ? "test-main.js" : "data/test-main-7.0.js"),
                            Path.Combine(_projectDir, "test-main.js"));
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
                                         options.HasIcudt,
                                         options.DotnetWasmFromRuntimePack ?? !buildArgs.AOT);
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
            if (runAnalyzers)
                AddItemsPropertiesToProject("<RunAnalyzers>true</RunAnalyzers>");
            return projectfile;
        }

        protected (CommandResult, string) BuildInternal(string id, string config, bool publish=false, bool setWasmDevel=true, params string[] extraArgs)
        {
            string label = publish ? "publish" : "build";
            _testOutput.WriteLine($"{Environment.NewLine}** {label} **{Environment.NewLine}");

            string logPath = Path.Combine(s_buildEnv.LogRootPath, id, $"{id}-{label}.binlog");
            string[] combinedArgs = new[]
            {
                label, // same as the command name
                $"-bl:{logPath}",
                $"-p:Configuration={config}",
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
                                                   bool hasIcudt = true,
                                                   bool dotnetWasmFromRuntimePack = true)
        {
#if false
            AssertFilesExist(bundleDir, new []
            {
                "index.html",
                mainJS,
                "dotnet.wasm",
                "_framework/blazor.boot.json",
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

            AssertDotNetWasmJs(bundleDir, fromRuntimePack: dotnetWasmFromRuntimePack, targetFramework);
#endif
        }

        protected static void AssertFilesDontExist(string dir, string[] filenames, string? label = null)
            => AssertFilesExist(dir, filenames, label, expectToExist: false);

        protected static void AssertFilesExist(string dir, string[] filenames, string? label = null, bool expectToExist=true)
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

        protected static void AssertSameFile(string file0, string file1, string? label=null) => AssertFile(file0, file1, label, same: true);
        protected static void AssertNotSameFile(string file0, string file1, string? label=null) => AssertFile(file0, file1, label, same: false);

        protected static void AssertFile(string file0, string file1, string? label=null, bool same=true)
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

        protected (int exitCode, string buildOutput) AssertBuild(string args, string label="build", bool expectSuccess=true, IDictionary<string, string>? envVars=null, int? timeoutMs=null)
        {
            var result = RunProcess(s_buildEnv.DotNet, _testOutput, args, workingDir: _projectDir, label: label, envVars: envVars, timeoutMs: timeoutMs ?? s_defaultPerTestTimeoutMs);
            if (expectSuccess && result.exitCode != 0)
                throw new XunitException($"Build process exited with non-zero exit code: {result.exitCode}");
            if (!expectSuccess && result.exitCode == 0)
                throw new XunitException($"Build should have failed, but it didn't. Process exited with exitCode : {result.exitCode}");

            return result;
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
            return Path.Combine(dir!, "bin", config, targetFramework, BuildEnvironment.DefaultRuntimeIdentifier);
        }

        protected string GetObjDir(string config, string targetFramework=DefaultTargetFramework, string? baseDir=null)
        {
            var dir = baseDir ?? _projectDir;
            Assert.NotNull(dir);
            return Path.Combine(dir!, "obj", config, targetFramework, BuildEnvironment.DefaultRuntimeIdentifier);
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

                // runtime repo sets this, which interferes with the tests
                processStartInfo.RemoveEnvironmentVariables("MSBuildSDKsPath");
            }

            Process process = new ();
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
        bool    AssertAppBundle           = true,
        bool    CreateProject             = true,
        bool    Publish                   = true,
        bool    BuildOnlyAfterPublish     = true,
        bool    HasV8Script               = true,
        string? Verbosity                 = null,
        string? Label                     = null,
        string? TargetFramework           = null,
        string? MainJS                    = null,
        IDictionary<string, string>? ExtraBuildEnvironmentVariables = null
    );

    public enum NativeFilesType { FromRuntimePack, Relinked, AOT };
}
