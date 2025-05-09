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
using Microsoft.Build.Logging.StructuredLogger;

#nullable enable

// [assembly: CollectionBehavior(CollectionBehavior.CollectionPerAssembly)]

namespace Wasm.Build.Tests
{
    public abstract class BuildTestBase : IClassFixture<SharedBuildPerTestClassFixture>, IDisposable
    {
        public static readonly string DefaultTargetFramework = $"net{Environment.Version.Major}.0";
        public static readonly string PreviousTargetFramework = $"net{Environment.Version.Major - 1}.0";
        public static readonly string Previous2TargetFramework = $"net{Environment.Version.Major - 2}.0";
        public static readonly string DefaultTargetFrameworkForBlazor = $"net{Environment.Version.Major}.0";
        public static readonly string TargetFrameworkForTasks = $"net{Environment.Version.Major}.0";
        private const string DefaultEnvironmentLocale = "en-US";
        protected static readonly string s_unicodeChars = "\u9FC0\u8712\u679B\u906B\u486B\u7149";
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
        protected static bool s_isWindows = RuntimeInformation.IsOSPlatform(OSPlatform.Windows);
        // changing Windows's language programistically is complicated and Node is using OS's language to determine
        // what is client's preferred locale and then to load corresponding ICU => skip automatic icu testing with Node
        // on Linux sharding does not work because we rely on LANG env var to check locale and emcc is overwriting it
        // FIXME: use an envvar to override this
        protected static int s_defaultPerTestTimeoutMs = s_isWindows ? 30 * 60 * 1000 : 15 * 60 * 1000;
        public static BuildEnvironment s_buildEnv;
        private const string s_nugetInsertionTag = "<!-- TEST_RESTORE_SOURCES_INSERTION_LINE -->";

        public static bool IsUsingWorkloads => s_buildEnv.IsWorkload;
        public static bool IsNotUsingWorkloads => !s_buildEnv.IsWorkload;
        public static bool IsWorkloadWithMultiThreadingForDefaultFramework => s_buildEnv.IsWorkloadWithMultiThreadingForDefaultFramework;
        public static bool UseWebcil => s_buildEnv.UseWebcil;
        public static string GetNuGetConfigPathFor(string targetFramework)
            => Path.Combine(BuildEnvironment.TestDataPath, "nuget.config");

        public TProvider GetProvider<TProvider>() where TProvider : ProjectProviderBase
            => (TProvider)_providerOfBaseType;

        protected string _projectDir
        {
            get => _providerOfBaseType.ProjectDir!;
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

        public static IEnumerable<IEnumerable<object?>> ConfigWithAOTData(bool aot, Configuration config = Configuration.Undefined)
        {
            if (config == Configuration.Undefined)
            {
                return new IEnumerable<object?>[]
                    {
    #if TEST_DEBUG_CONFIG_ALSO
                        // list of each member data - for Debug+@aot
                        new object?[] { Configuration.Debug, aot }.AsEnumerable(),
    #endif
                        // list of each member data - for Release+@aot
                        new object?[] { Configuration.Release, aot }.AsEnumerable()
                    }.AsEnumerable();
            }
            else
            {
                return new IEnumerable<object?>[]
                {
                    new object?[] { config, aot }.AsEnumerable()
                };
            }
        }

        public (CommandResult res, string logPath) BuildProjectWithoutAssert(
            Configuration configuration,
            string projectName,
            MSBuildOptions buildOptions)
        {
            string buildType = buildOptions.IsPublish ? "publish" : "build";
            string logFileSuffix = string.IsNullOrEmpty(buildOptions.Label) ? string.Empty : buildOptions.Label.Replace(' ', '_') + "-";
            string logFilePath = Path.Combine(_logPath, $"{projectName}-{logFileSuffix}{buildType}.binlog");

            _testOutput.WriteLine($"{Environment.NewLine}** -------- {buildType} -------- **{Environment.NewLine}");
            _testOutput.WriteLine($"Binlog path: {logFilePath}");

            List<string> commandLineArgs = new()
            {
                buildType,
                $"-bl:{logFilePath}",
                $"-p:Configuration={configuration}",
                "-nr:false"
            };
            commandLineArgs.AddRange(buildOptions.ExtraMSBuildArgs);

            if (buildOptions.IsPublish && buildOptions is PublishOptions po && po.BuildOnlyAfterPublish)
                commandLineArgs.Append("-p:WasmBuildOnlyAfterPublish=true");

            using ToolCommand cmd = new DotNetCommand(s_buildEnv, _testOutput)
                                        .WithWorkingDirectory(_projectDir);
            cmd.WithEnvironmentVariable("NUGET_PACKAGES", _nugetPackagesDir)
                .WithEnvironmentVariables(buildOptions.ExtraBuildEnvironmentVariables);
            if (UseWBTOverridePackTargets && s_buildEnv.IsWorkload)
                cmd.WithEnvironmentVariable("WBTOverrideRuntimePack", "true");

            CommandResult res = cmd.ExecuteWithCapturedOutput(commandLineArgs.ToArray());
            if (buildOptions.ExpectSuccess)
                res.EnsureSuccessful();
            else if (res.ExitCode == 0)
                throw new XunitException($"Build should have failed, but it didn't. Process exited with exitCode : {res.ExitCode}");

            // Ensure we got all output.
            string[] successMessages = ["Build succeeded"];
            string[] errorMessages = ["Build failed", "Build FAILED", "Restore failed", "Stopping the build"];
            if ((res.ExitCode == 0 && successMessages.All(m => !res.Output.Contains(m))) || (res.ExitCode != 0 && errorMessages.All(m => !res.Output.Contains(m))))
            {
                _testOutput.WriteLine("Replacing dotnet process output with messages from binlog");

                var outputBuilder = new StringBuilder();
                var buildRoot = BinaryLog.ReadBuild(logFilePath);
                buildRoot.VisitAllChildren<TextNode>(m =>
                {
                    if (m is Message || m is Error || m is Warning)
                    {
                        var context = GetBinlogMessageContext(m);
                        outputBuilder.AppendLine($"{context}{m.Title}");
                    }
                });

                res = new CommandResult(res.StartInfo, res.ExitCode, outputBuilder.ToString());
            }

            return (res, logFilePath);
        }

        private string GetBinlogMessageContext(TextNode node)
        {
            var currentNode = node;
            while (currentNode != null)
            {
                if (currentNode is Error error)
                {
                    return $"{error.File}({error.LineNumber},{error.ColumnNumber}): error {error.Code}: ";
                }
                else if (currentNode is Warning warning)
                {
                    return $"{warning.File}({warning.LineNumber},{warning.ColumnNumber}): warning {warning.Code}: ";
                }
                currentNode = currentNode.Parent as TextNode;
            }
            return string.Empty;
        }

        [MemberNotNull(nameof(_projectDir), nameof(_logPath))]
        protected (string, string) InitPaths(string id)
        {
            if (_projectDir == null)
                _projectDir = Path.Combine(BuildEnvironment.TmpPath, id);
            _logPath = Path.Combine(s_buildEnv.LogRootPath, id);
            _nugetPackagesDir = Path.Combine(BuildEnvironment.TmpPath, "nuget", id);

            if (Directory.Exists(_nugetPackagesDir))
                Directory.Delete(_nugetPackagesDir, recursive: true);

            Directory.CreateDirectory(_nugetPackagesDir!);
            Directory.CreateDirectory(_logPath);
            return (_logPath, _nugetPackagesDir);
        }

        protected void InitProjectDir(string dir, bool addNuGetSourceForLocalPackages = true, string? targetFramework = null)
        {
            targetFramework ??= DefaultTargetFramework;
            if (Directory.Exists(dir))
                Directory.Delete(dir, recursive: true);
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


        protected static string GetNuGetConfigWithLocalPackagesPath(string templatePath, string localNuGetsPath)
        {
            string contents = File.ReadAllText(templatePath);
            if (contents.IndexOf(s_nugetInsertionTag, StringComparison.InvariantCultureIgnoreCase) < 0)
                throw new Exception($"Could not find {s_nugetInsertionTag} in {templatePath}");

            return contents.Replace(s_nugetInsertionTag, $@"<add key=""nuget-local"" value=""{localNuGetsPath}"" />");
        }

        public static string AddItemsPropertiesToProject(string projectFile, string? extraProperties = null, string? extraItems = null, string? insertAtEnd = null)
        {
            if (!File.Exists(projectFile))
                throw new Exception($"{projectFile} does not exist");
            if (extraProperties == null && extraItems == null && insertAtEnd == null)
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

            if (insertAtEnd != null)
            {
                XmlNode node = doc.CreateNode(XmlNodeType.DocumentFragment, "foo", null);
                node.InnerXml = insertAtEnd;
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

        protected static string GetSkiaSharpReferenceItems()
            => @"<PackageReference Include=""SkiaSharp"" Version=""2.88.9-preview.2.2"" />
                <PackageReference Include=""SkiaSharp.NativeAssets.WebAssembly"" Version=""2.88.9-preview.2.2"" />
                <NativeFileReference Include=""$(SkiaSharpStaticLibraryPath)\3.1.56\st\*.a"" />";

        protected static string s_mainReturns42 = @"
            public class TestClass {
                public static int Main()
                {
                    return 42;
                }
            }";
    }
}
