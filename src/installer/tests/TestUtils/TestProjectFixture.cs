// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.DotNet.Cli.Build;
using System;
using System.Collections.Generic;
using System.IO;

namespace Microsoft.DotNet.CoreSetup.Test
{
    /*
     * TestProjectFixture is an abstraction around a TestProject which manages
     * setup of the TestProject, copying test projects for perf on build/restore,
     * and building/publishing/restoring test projects where necessary.
     */
    public class TestProjectFixture : IDisposable
    {
        private string _assemblyName;
        private TestProject _sourceTestProject;

        public DotNetCli SdkDotnet { get; }
        public DotNetCli BuiltDotnet { get; }
        public TestProject TestProject { get; private set; }

        public string CurrentRid { get; private set; }
        public string Framework { get; private set; }
        public RepoDirectoriesProvider RepoDirProvider { get; }

        public TestProjectFixture(
            string testProjectName,
            RepoDirectoriesProvider repoDirectoriesProvider,
            string framework = null,
            string assemblyName = null)
        {
            ValidateRequiredDirectories(repoDirectoriesProvider);

            RepoDirProvider = repoDirectoriesProvider;

            Framework = framework ?? RepoDirProvider.GetTestContextVariable("MNA_TFM");

            SdkDotnet = new DotNetCli(repoDirectoriesProvider.DotnetSDK);
            CurrentRid = repoDirectoriesProvider.TargetRID;

            BuiltDotnet = new DotNetCli(repoDirectoriesProvider.BuiltDotnet);

            _assemblyName = assemblyName;

            var sourceTestProjectPath = Path.Combine(repoDirectoriesProvider.TestAssetsFolder, "TestProjects", testProjectName);
            _sourceTestProject = new TestProject(
                sourceTestProjectPath,
                assemblyName: _assemblyName);

            TestProject = CopyTestProject(_sourceTestProject);
        }

        public TestProjectFixture(TestProjectFixture fixtureToCopy)
        {
            RepoDirProvider = fixtureToCopy.RepoDirProvider;
            SdkDotnet = fixtureToCopy.SdkDotnet;
            CurrentRid = fixtureToCopy.CurrentRid;
            BuiltDotnet = fixtureToCopy.BuiltDotnet;
            _sourceTestProject = fixtureToCopy._sourceTestProject;
            Framework = fixtureToCopy.Framework;
            _assemblyName = fixtureToCopy._assemblyName;

            TestProject = CopyTestProject(fixtureToCopy.TestProject);
        }

        public void Dispose()
        {
            if (TestProject != null)
            {
                TestProject.Dispose();
                TestProject = null;
            }
        }

        private TestProject CopyTestProject(TestProject sourceTestProject)
        {
            EnsureDirectoryBuildFiles(TestArtifact.TestArtifactsPath);
            return sourceTestProject.Copy();
        }

        private void EnsureDirectoryBuildFiles(string testArtifactDirectory)
        {
            Directory.CreateDirectory(testArtifactDirectory);

            // write an empty Directory.Build.* file to ensure that msbuild doesn't pick up
            // the repo's root Directory.Build.*.
            EnsureTestProjectsFileContent(testArtifactDirectory, "props");
            EnsureTestProjectsFileContent(testArtifactDirectory, "targets");
        }

        private void EnsureTestProjectsFileContent(string dir, string type) => EnsureFileWithContent(
            Path.Combine(dir, $"Directory.Build.{type}"),
            string.Join(
                Environment.NewLine,
                "<Project>",
                $"  <Import Project=\"{RepoDirProvider.TestAssetsFolder}/TestUtils/TestProjects.{type}\" />",
                "</Project>"));

        private void EnsureFileWithContent(string path, string content)
        {
            for(int i = 0; i < 3 && !File.Exists(path); i++)
            {
                try
                {
                    File.WriteAllText(path, content);
                }
                catch (IOException)
                {}
            }
        }

        private void ValidateRequiredDirectories(RepoDirectoriesProvider repoDirectoriesProvider)
        {
            if ( ! Directory.Exists(repoDirectoriesProvider.BuiltDotnet))
            {
                throw new Exception($"Unable to find built host and sharedfx, please ensure the build has been run: {repoDirectoriesProvider.BuiltDotnet}");
            }

            if ( ! Directory.Exists(repoDirectoriesProvider.HostArtifacts))
            {
                throw new Exception($"Unable to find host artifacts directory, please ensure the build has been run: {repoDirectoriesProvider.HostArtifacts}");
            }
        }

        public TestProjectFixture BuildProject(
            DotNetCli dotnet = null,
            string runtime = null,
            string framework = null,
            string outputDirectory = null,
            bool restore = false)
        {
            dotnet = dotnet ?? SdkDotnet;
            outputDirectory = outputDirectory ?? TestProject.OutputDirectory;
            TestProject.OutputDirectory = outputDirectory;
            framework = framework ?? Framework;
            Framework = framework;

            var buildArgs = new List<string>
            {
                "/bl:BuildProject.binlog"
            };

            if (restore != true)
            {
                buildArgs.Add("--no-restore");
            }

            if (runtime != null)
            {
                buildArgs.Add("--runtime");
                buildArgs.Add(runtime);
            }

            if (framework != null)
            {
                buildArgs.Add("--framework");
                buildArgs.Add(framework);
            }

            buildArgs.Add($"/p:TestTargetRid={RepoDirProvider.TargetRID}");
            buildArgs.Add($"/p:MNAVersion={RepoDirProvider.MicrosoftNETCoreAppVersion}");

            if (outputDirectory != null)
            {
                buildArgs.Add("-o");
                buildArgs.Add(outputDirectory);
            }

            dotnet.Build(buildArgs.ToArray())
                .WorkingDirectory(TestProject.ProjectDirectory)
                .Environment("NUGET_PACKAGES", RepoDirProvider.NugetPackages)
                .Environment("VERSION", "") // Generate with package version 1.0.0, not %VERSION%
                .CaptureStdErr()
                .CaptureStdOut()
                .Execute()
                .EnsureSuccessful();

            TestProject.LoadOutputFiles();

            return this;
        }

        public TestProjectFixture StoreProject(
            DotNetCli dotnet = null,
            string runtime = null,
            string framework = null,
            string manifest = null,
            string outputDirectory = null)
        {
            dotnet = dotnet ?? SdkDotnet;
            outputDirectory = outputDirectory ?? TestProject.OutputDirectory;
            framework = framework ?? Framework;
            Framework = framework;

            var storeArgs = new List<string>
            {
                "--runtime"
            };

            if (runtime != null)
            {
                storeArgs.Add(runtime);
            }
            else
            {
               storeArgs.Add(CurrentRid);
            }

            if (framework != null)
            {
                storeArgs.Add("--framework");
                storeArgs.Add(framework);
            }

                storeArgs.Add("--manifest");
            if (manifest != null)
            {
                storeArgs.Add(manifest);
            }
            else
            {
                storeArgs.Add(_sourceTestProject.ProjectFile);
            }

            if (outputDirectory != null)
            {
                storeArgs.Add("-o");
                storeArgs.Add(outputDirectory);
            }

            storeArgs.Add($"/p:MNAVersion={RepoDirProvider.MicrosoftNETCoreAppVersion}");
            storeArgs.Add($"/p:NetCoreAppCurrent={Framework}");

            // Ensure the project's OutputType isn't 'Exe', since that causes issues with 'dotnet store'
            storeArgs.Add("/p:OutputType=Library");

            dotnet.Store(storeArgs.ToArray())
                .WorkingDirectory(TestProject.ProjectDirectory)
                .Environment("NUGET_PACKAGES", RepoDirProvider.NugetPackages)
                .CaptureStdErr()
                .CaptureStdOut()
                .Execute()
                .EnsureSuccessful();

            TestProject.LoadOutputFiles();

            return this;
        }

        public TestProjectFixture PublishProject(
            DotNetCli dotnet = null,
            string runtime = null,
            string framework = null,
            bool? selfContained = null,
            string outputDirectory = null,
            bool singleFile = false,
            bool restore = false,
            params string[] extraArgs)
        {
            dotnet = dotnet ?? SdkDotnet;
            outputDirectory = outputDirectory ?? TestProject.OutputDirectory;
            TestProject.OutputDirectory = outputDirectory;
            framework = framework ?? Framework;
            Framework = framework;

            var publishArgs = new List<string>
            {
                "/bl:PublishProject.binlog"
            };

            if (restore != true)
            {
                publishArgs.Add("--no-restore");
            }

            if (runtime != null)
            {
                publishArgs.Add("--runtime");
                publishArgs.Add(runtime);
            }

            if (framework != null)
            {
                publishArgs.Add("--framework");
                publishArgs.Add(framework);
                publishArgs.Add($"/p:NetCoreAppCurrent={framework}");
            }

            if (selfContained != null)
            {
                publishArgs.Add("--self-contained");
                publishArgs.Add(selfContained.Value ? "true" : "false");
            }

            if (outputDirectory != null)
            {
                publishArgs.Add("-o");
                publishArgs.Add(outputDirectory);
            }

            if (singleFile)
            {
                publishArgs.Add("/p:PublishSingleFile=true");
            }

            publishArgs.Add($"/p:TestTargetRid={RepoDirProvider.TargetRID}");
            publishArgs.Add($"/p:MNAVersion={RepoDirProvider.MicrosoftNETCoreAppVersion}");

            foreach (var arg in extraArgs)
            {
                publishArgs.Add(arg);
            }

            dotnet.Publish(publishArgs.ToArray())
                .WorkingDirectory(TestProject.ProjectDirectory)
                .Environment("NUGET_PACKAGES", RepoDirProvider.NugetPackages)
                .CaptureStdErr()
                .CaptureStdOut()
                .Execute()
                .EnsureSuccessful();

            TestProject.LoadOutputFiles();

            return this;
        }

        public TestProjectFixture RestoreProject(string[] fallbackSources, string extraMSBuildProperties = null)
        {
            var restoreArgs = new List<string>();
            foreach (var fallbackSource in fallbackSources)
            {
                restoreArgs.Add("--source");
                restoreArgs.Add(fallbackSource);
            }
            restoreArgs.Add("--disable-parallel");

            restoreArgs.Add($"/p:MNAVersion={RepoDirProvider.MicrosoftNETCoreAppVersion}");
            restoreArgs.Add($"/p:NetCoreAppCurrent={Framework}");

            if (extraMSBuildProperties != null)
            {
                restoreArgs.Add(extraMSBuildProperties);
            }

            SdkDotnet.Restore(restoreArgs.ToArray())
                .WorkingDirectory(TestProject.ProjectDirectory)
                .CaptureStdErr()
                .CaptureStdOut()
                .Environment("NUGET_PACKAGES", RepoDirProvider.NugetPackages)
                .Execute()
                .EnsureSuccessful();

            return this;
        }

        public TestProjectFixture EnsureRestored(params string[] fallbackSources)
        {
            if (!TestProject.IsRestored())
            {
                RestoreProject(fallbackSources);
            }

            return this;
        }

        public TestProjectFixture EnsureRestoredForRid(string rid, params string[] fallbackSources)
        {
            if (!TestProject.IsRestored())
            {
                string extraMSBuildProperties = $"/p:TestTargetRid={rid}";
                RestoreProject(fallbackSources, extraMSBuildProperties);
            }

            return this;
        }

        public TestProjectFixture Copy()
        {
            return new TestProjectFixture(this);
        }
    }
}
