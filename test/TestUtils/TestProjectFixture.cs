using Microsoft.DotNet.Cli.Build;
using System;
using System.Collections.Generic;
using System.IO;

namespace Microsoft.DotNet.CoreSetup.Test
{
    public class TestProjectFixture
    {
        private static readonly string s_testArtifactDirectoryEnvironmentVariable = "TEST_ARTIFACTS";

        private string _testProjectName;
        private string _exeExtension;
        private string _sharedLibraryExtension;
        private string _sharedLibraryPrefix;
        private string _testProjectSourceDirectory;
        private string _testArtifactDirectory;
        private string _currentRid;

        private RepoDirectoriesProvider _repoDirectoriesProvider;

        private DotNetCli _sdkDotnet;
        private DotNetCli _builtDotnet;

        private TestProject _sourceTestProject;
        private TestProject _testProject;

        public DotNetCli SdkDotnet => _sdkDotnet;
        public DotNetCli BuiltDotnet => _builtDotnet;
        public TestProject TestProject => _testProject;
        public string CurrentRid => _currentRid;


        public TestProjectFixture(
            string testProjectName,
            RepoDirectoriesProvider repoDirectoriesProvider,
            string exeExtension = null,
            string sharedLibraryExtension = null,
            string sharedLibraryPrefix= null,
            string testProjectSourceDirectory = null,
            string testArtifactDirectory = null,
            string dotnetInstallPath = null,
            string currentRid = null,
            string builtDotnetOutputPath = null)
        {
            ValidateRequiredDirectories(repoDirectoriesProvider);

            _testProjectName = testProjectName;

            _exeExtension = exeExtension ?? RuntimeInformationExtensions.GetExeExtensionForCurrentOSPlatform();
            _sharedLibraryExtension = sharedLibraryExtension 
                ?? RuntimeInformationExtensions.GetSharedLibraryExtensionForCurrentPlatform();
            _sharedLibraryPrefix = sharedLibraryPrefix 
                ?? RuntimeInformationExtensions.GetSharedLibraryPrefixForCurrentPlatform();

            _repoDirectoriesProvider = repoDirectoriesProvider;

            _testProjectSourceDirectory = testProjectSourceDirectory
                ?? Path.Combine(repoDirectoriesProvider.RepoRoot, "TestAssets", "TestProjects");
            _testArtifactDirectory = _testArtifactDirectory
                ?? Environment.GetEnvironmentVariable(s_testArtifactDirectoryEnvironmentVariable)
                ?? Path.Combine(AppContext.BaseDirectory, s_testArtifactDirectoryEnvironmentVariable);

            _sdkDotnet = new DotNetCli(dotnetInstallPath ?? DotNetCli.GetStage0Path(repoDirectoriesProvider.RepoRoot));
            _currentRid = currentRid ?? _sdkDotnet.GetRuntimeId();
            
            _builtDotnet = new DotNetCli(repoDirectoriesProvider.BuiltDotnet);

            InitializeTestProject(
                _testProjectName,
                _testProjectSourceDirectory, 
                _testArtifactDirectory,
                _exeExtension,
                _sharedLibraryExtension,
                _sharedLibraryPrefix);
        }

        public TestProjectFixture(TestProjectFixture fixtureToCopy)
        {
            _testProjectName = fixtureToCopy._testProjectName;
            _exeExtension = fixtureToCopy._exeExtension;
            _sharedLibraryExtension = fixtureToCopy._sharedLibraryExtension;
            _sharedLibraryPrefix = fixtureToCopy._sharedLibraryPrefix;
            _repoDirectoriesProvider = fixtureToCopy._repoDirectoriesProvider;
            _testProjectSourceDirectory = fixtureToCopy._testProjectSourceDirectory;
            _testArtifactDirectory = fixtureToCopy._testArtifactDirectory;
            _sdkDotnet = fixtureToCopy._sdkDotnet;
            _currentRid = fixtureToCopy._currentRid;
            _builtDotnet = fixtureToCopy._builtDotnet;
            _sourceTestProject = fixtureToCopy._sourceTestProject;

            _testProject = CopyTestProject(
                fixtureToCopy.TestProject, 
                _testArtifactDirectory, 
                _exeExtension,
                _sharedLibraryExtension,
                _sharedLibraryPrefix);
        }

        private void InitializeTestProject(
            string testProjectName,
            string testProjectSourceDirectory,
            string testArtifactDirectory,
            string exeExtension,
            string sharedLibraryExtension,
            string sharedLibraryPrefix)
        {
            var sourceTestProjectPath = Path.Combine(testProjectSourceDirectory, testProjectName);
            _sourceTestProject = new TestProject(
                sourceTestProjectPath,
                exeExtension,
                sharedLibraryExtension,
                sharedLibraryPrefix);

            _testProject = CopyTestProject(
                _sourceTestProject, 
                testArtifactDirectory, 
                exeExtension,
                sharedLibraryExtension,
                sharedLibraryPrefix);
        }

        private TestProject CopyTestProject(
            TestProject sourceTestProject, 
            string testArtifactDirectory, 
            string exeExtension,
            string sharedLibraryExtension,
            string sharedLibraryPrefix)
        {
            string copiedTestProjectDirectory = CalculateTestProjectDirectory(
                sourceTestProject.ProjectName, 
                testArtifactDirectory);

            sourceTestProject.CopyProjectFiles(copiedTestProjectDirectory);
            return new TestProject(
                copiedTestProjectDirectory, 
                exeExtension,
                sharedLibraryExtension,
                sharedLibraryPrefix);
        }

        private string CalculateTestProjectDirectory(string testProjectName, string testArtifactDirectory)
        {
            int projectCount = 0;
            string projectDirectory = Path.Combine(testArtifactDirectory, projectCount.ToString(), testProjectName);

            while (Directory.Exists(projectDirectory))
            {
                projectDirectory = Path.Combine(testArtifactDirectory, (++projectCount).ToString(), testProjectName);
            }

            return projectDirectory;
        }
        
        private void ValidateRequiredDirectories(RepoDirectoriesProvider repoDirectoriesProvider)
        {
            if ( ! Directory.Exists(repoDirectoriesProvider.BuiltDotnet))
            {
                throw new Exception($"Unable to find built host and sharedfx, please ensure the build has been run: {repoDirectoriesProvider.BuiltDotnet}");
            }

            if ( ! Directory.Exists(repoDirectoriesProvider.CorehostPackages))
            {
                throw new Exception($"Unable to find host packages directory, please ensure the build has been run: {repoDirectoriesProvider.CorehostPackages}");
            }

            if (!Directory.Exists(repoDirectoriesProvider.CorehostDummyPackages))
            {
                throw new Exception($"Unable to find host dummy packages directory, please ensure the build has been run: {repoDirectoriesProvider.CorehostDummyPackages}");
            }
        }

        public TestProjectFixture BuildProject(
            DotNetCli dotnet = null, 
            string runtime = null, 
            string framework = "netcoreapp1.0",
            string outputDirectory = null)
        {
            dotnet = dotnet ?? _sdkDotnet;
            outputDirectory = outputDirectory ?? _testProject.OutputDirectory;
            _testProject.OutputDirectory = outputDirectory;

            var buildArgs = new List<string>();
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

            if (outputDirectory != null)
            {
                buildArgs.Add("-o");
                buildArgs.Add(outputDirectory);
            }

            dotnet.Build(buildArgs.ToArray())
                .WorkingDirectory(_testProject.ProjectDirectory)
                .Environment("NUGET_PACKAGES", _repoDirectoriesProvider.NugetPackages)
                .CaptureStdErr()
                .CaptureStdOut()
                .Execute()
                .EnsureSuccessful();

            _testProject.LoadOutputFiles();

            return this;
        }

        public TestProjectFixture PublishProject(
            DotNetCli dotnet = null,
            string runtime = null,
            string framework = "netcoreapp1.0",
            string outputDirectory = null)
        {
            dotnet = dotnet ?? _sdkDotnet;
            outputDirectory = outputDirectory ?? _testProject.OutputDirectory;
            _testProject.OutputDirectory = outputDirectory;

            var publishArgs = new List<string>();
            if (runtime != null)
            {
                publishArgs.Add("--runtime");
                publishArgs.Add(runtime);
            }

            if (framework != null)
            {
                publishArgs.Add("--framework");
                publishArgs.Add(framework);
            }

            if (outputDirectory != null)
            {
                publishArgs.Add("-o");
                publishArgs.Add(outputDirectory);
            }

            dotnet.Publish(publishArgs.ToArray())
                .WorkingDirectory(_testProject.ProjectDirectory)
                .Environment("NUGET_PACKAGES", _repoDirectoriesProvider.NugetPackages)
                .CaptureStdErr()
                .CaptureStdOut()
                .Execute()
                .EnsureSuccessful();

            _testProject.LoadOutputFiles();

            return this;
        }

        public TestProjectFixture RestoreProject(string[] fallbackSources)
        {
            var restoreArgs = new List<string>();
            foreach (var fallbackSource in fallbackSources)
            {
                restoreArgs.Add("-f");
                restoreArgs.Add(fallbackSource);
            }

            _sdkDotnet.Restore(restoreArgs.ToArray())
                .WorkingDirectory(_testProject.ProjectDirectory)
                .CaptureStdErr()
                .CaptureStdOut()
                .Environment("NUGET_PACKAGES", _repoDirectoriesProvider.NugetPackages)
                .Execute()
                .EnsureSuccessful();

            return this;
        }

        public TestProjectFixture EnsureRestored(params string[] fallbackSources)
        {
            if ( ! _testProject.IsRestored())
            {
                RestoreProject(fallbackSources);
            }

            return this;
        }

        public TestProjectFixture Copy()
        {
            return new TestProjectFixture(this);
        }

        public TestProjectFixture MoveDepsJsonToSubdirectory()
        {
            var subdirectory = Path.Combine(_testProject.ProjectDirectory, "d");
            if (!Directory.Exists(subdirectory))
            {
                Directory.CreateDirectory(subdirectory);
            }

            var destDepsJson = Path.Combine(subdirectory, Path.GetFileName(_testProject.DepsJson));

            if (File.Exists(destDepsJson))
            {
                File.Delete(destDepsJson);
            }
            File.Move(_testProject.DepsJson, destDepsJson);

            _testProject.DepsJson = destDepsJson;

            return this;
        }

        public TestProjectFixture MoveRuntimeConfigToSubdirectory()
        {
            var subdirectory = Path.Combine(_testProject.ProjectDirectory, "r");
            if (!Directory.Exists(subdirectory))
            {
                Directory.CreateDirectory(subdirectory);
            }

            var destRuntimeConfig = Path.Combine(subdirectory, Path.GetFileName(_testProject.RuntimeConfigJson));

            if (File.Exists(destRuntimeConfig))
            {
                File.Delete(destRuntimeConfig);
            }
            File.Move(_testProject.RuntimeConfigJson, destRuntimeConfig);

            _testProject.RuntimeConfigJson = destRuntimeConfig;

            return this;
        }

        public TestProjectFixture ReplaceTestProjectOutputHostFromDotnet(DotNetCli dotnet = null)
        {
            dotnet = dotnet ?? _builtDotnet;

            var testProjectHost = _testProject.AppExe;
            var testProjectHostPolicy = _testProject.HostPolicyDll;
            var testProjectHostFxr = _testProject.HostFxrDll;

            if ( ! File.Exists(testProjectHost) || ! File.Exists(testProjectHostPolicy))
            {
                throw new Exception("host or hostpolicy does not exist in test project output. Is this a standalone app?");
            }

            var dotnetHost = Path.Combine(dotnet.GreatestVersionSharedFxPath, $"dotnet{_exeExtension}");
            var dotnetHostPolicy = Path.Combine(dotnet.GreatestVersionSharedFxPath, $"{_sharedLibraryPrefix}hostpolicy{_sharedLibraryExtension}");
            var dotnetHostFxr = Path.Combine(dotnet.GreatestVersionSharedFxPath, $"{_sharedLibraryPrefix}hostfxr{_sharedLibraryExtension}");

            File.Copy(dotnetHost, testProjectHost, true);
            File.Copy(dotnetHostPolicy, testProjectHostPolicy, true);

            if (File.Exists(testProjectHostFxr))
            {
                File.Copy(dotnetHostFxr, testProjectHostFxr, true);
            }

            return this;
        }
    }
}
