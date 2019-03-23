// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.DotNet.Cli.Build;
using Microsoft.DotNet.Cli.Build.Framework;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Microsoft.DotNet.CoreSetup.Test.HostActivation.FrameworkResolution
{
    public abstract partial class FrameworkResolutionBase
    {
        protected const string MicrosoftNETCoreApp = "Microsoft.NETCore.App";

        protected void RunTest(
            DotNetCli dotnet,
            TestApp app,
            Func<RuntimeConfig, RuntimeConfig> runtimeConfig,
            Action<CommandResult> resultAction,
            IDictionary<string, string> environment = null,
            string[] commandLine = null,
            bool multiLevelLookup = false)
        {
            RunTest(
                dotnet,
                app,
                new TestSettings()
                {
                    RuntimeConfigCustomizer = runtimeConfig,
                    Environment = environment,
                    CommandLine = commandLine
                },
                resultAction,
                multiLevelLookup);
        }

        public class TestSettings
        {
            public Func<RuntimeConfig, RuntimeConfig> RuntimeConfigCustomizer { get; set; }
            public IDictionary<string, string> Environment { get; set; }
            public IEnumerable<string> CommandLine { get; set; }

            public TestSettings WithRuntimeConfigCustomizer(Func<RuntimeConfig, RuntimeConfig> customizer)
            {
                Func<RuntimeConfig, RuntimeConfig> previousCustomizer = RuntimeConfigCustomizer;
                if (previousCustomizer == null)
                {
                    RuntimeConfigCustomizer = customizer;
                }
                else
                {
                    RuntimeConfigCustomizer = runtimeConfig => customizer(previousCustomizer(runtimeConfig));
                }

                return this;
            }

            public TestSettings WithEnvironment(string key, string value)
            {
                Environment = Environment == null ? 
                    new Dictionary<string, string>() { { key, value } } : 
                    new Dictionary<string, string>(Environment.Append(new KeyValuePair<string, string>(key, value)));
                return this;
            }

            public TestSettings WithCommandLine(params string[] args)
            {
                CommandLine = CommandLine == null ? args : CommandLine.Concat(args);
                return this;
            }

            public TestSettings With(Func<TestSettings, TestSettings> modifier)
            {
                return modifier(this);
            }
        }

        protected void RunTest(
            DotNetCli dotnet,
            TestApp app,
            TestSettings settings,
            Action<CommandResult> resultAction,
            bool multiLevelLookup = false)
        {
            if (settings.RuntimeConfigCustomizer != null)
            {
                settings.RuntimeConfigCustomizer(RuntimeConfig.Path(app.RuntimeConfigJson)).Save();
            }

            settings.WithCommandLine(app.AppDll);

            CommandResult result = dotnet.Exec(settings.CommandLine.First(), settings.CommandLine.Skip(1).ToArray())
                .EnvironmentVariable("COREHOST_TRACE", "1")
                .EnvironmentVariable("DOTNET_MULTILEVEL_LOOKUP", multiLevelLookup ? "1" : "0")
                .Environment(settings.Environment)
                .CaptureStdOut()
                .CaptureStdErr()
                .Execute();
            resultAction(result);
        }

        public class SharedTestStateBase : IDisposable
        {
            private readonly string _builtDotnet;
            private readonly string _baseDir;

            public SharedTestStateBase()
            {
                _builtDotnet = Path.Combine(TestArtifact.TestArtifactsPath, "sharedFrameworkPublish");

                string baseDir = Path.Combine(TestArtifact.TestArtifactsPath, "frameworkResolution");
                _baseDir = SharedFramework.CalculateUniqueTestDirectory(baseDir);
            }

            public DotNetBuilder DotNet(string name)
            {
                return new DotNetBuilder(_baseDir, _builtDotnet, name);
            }

            public TestApp CreateFrameworkReferenceApp()
            {
                // Prepare the app mock - we're not going to run anything really, so we just need the basic files
                string testAppDir = Path.Combine(_baseDir, "FrameworkReferenceApp");
                Directory.CreateDirectory(testAppDir);

                // ./FrameworkReferenceApp.dll
                File.WriteAllText(Path.Combine(testAppDir, "FrameworkReferenceApp.dll"), string.Empty);

                // ./FrameworkReferenceApp.runtimeconfig.json
                File.WriteAllText(Path.Combine(testAppDir, "FrameworkReferenceApp.runtimeconfig.json"), "{}");

                return new TestApp(testAppDir);
            }

            public void Dispose()
            {
                if (!TestArtifact.PreserveTestRuns() && Directory.Exists(_baseDir))
                {
                    Directory.Delete(_baseDir, true);
                }
            }

            public class DotNetBuilder
            {
                private readonly string _path;
                private readonly RepoDirectoriesProvider _repoDirectories;

                public DotNetBuilder(string basePath, string builtDotnet, string name)
                {
                    _path = Path.Combine(basePath, name);
                    Directory.CreateDirectory(_path);

                    _repoDirectories = new RepoDirectoriesProvider(builtDotnet: _path);

                    // Prepare the dotnet installation mock

                    // ./dotnet.exe - used as a convenient way to load and invoke hostfxr. May change in the future to use test-specific executable
                    var builtDotNetCli = new DotNetCli(builtDotnet);
                    File.Copy(
                        builtDotNetCli.DotnetExecutablePath,
                        Path.Combine(_path, RuntimeInformationExtensions.GetExeFileNameForCurrentPlatform("dotnet")),
                        true);

                    // ./host/fxr/<version>/hostfxr.dll - this is the component being tested
                    SharedFramework.CopyDirectory(
                        builtDotNetCli.GreatestVersionHostFxrPath,
                        Path.Combine(_path, "host", "fxr", Path.GetFileName(builtDotNetCli.GreatestVersionHostFxrPath)));
                }

                public DotNetBuilder AddMicrosoftNETCoreAppFramework(string version)
                {
                    // ./shared/Microsoft.NETCore.App/<version> - create a mock of the root framework
                    string netCoreAppPath = Path.Combine(_path, "shared", MicrosoftNETCoreApp, version);
                    Directory.CreateDirectory(netCoreAppPath);

                    // ./shared/Microsoft.NETCore.App/<version>/hostpolicy.dll - this is a mock, will not actually load CoreCLR
                    string mockHostPolicyFileName = RuntimeInformationExtensions.GetSharedLibraryFileNameForCurrentPlatform("mockhostpolicy");
                    File.Copy(
                        Path.Combine(_repoDirectories.Artifacts, "corehost_test", mockHostPolicyFileName),
                        Path.Combine(netCoreAppPath, RuntimeInformationExtensions.GetSharedLibraryFileNameForCurrentPlatform("hostpolicy")),
                        true);

                    return this;
                }

                public DotNetBuilder AddFramework(
                    string name,
                    string version,
                    Action<RuntimeConfig> runtimeConfigCustomizer)
                {
                    // ./shared/<name>/<version> - create a mock of effectively empty non-root framework
                    string path = Path.Combine(_path, "shared", name, version);
                    Directory.CreateDirectory(path);

                    // ./shared/<name>/<version>/<name>.runtimeconfig.json - runtime config which can be customized
                    RuntimeConfig runtimeConfig = new RuntimeConfig(Path.Combine(path, name + ".runtimeconfig.json"));
                    runtimeConfigCustomizer(runtimeConfig);
                    runtimeConfig.Save();

                    return this;
                }

                public DotNetCli Build()
                {
                    return new DotNetCli(_path);
                }
            }
        }
    }
}
