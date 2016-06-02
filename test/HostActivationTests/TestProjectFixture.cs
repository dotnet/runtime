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
        private string _repoRoot;
        private string _testProjectSourceDirectory;
        private string _testArtifactDirectory;
        private string _currentRid;
        private string _builtDotnetOutputPath;

        private DotNetCli _sdkDotnet;
        private DotNetCli _builtDotnet;

        private TestProject _sourceTestProject;
        private TestProject _testProject;

        public DotNetCli SdkDotnet => _sdkDotnet;
        public DotNetCli BuiltDotnet => _builtDotnet;
        public TestProject TestProject => _testProject;

        
        public TestProjectFixture(
            string testProjectName,
            string exeExtension,
            string repoRoot = null,
            string testProjectSourceDirectory = null,
            string testArtifactDirectory = null,
            string dotnetInstallPath = null,
            string currentRid = null,
            string builtDotnetOutputPath = null)
        {
            _testProjectName = testProjectName;
            _exeExtension = exeExtension;

            _repoRoot = repoRoot ?? Path.Combine(Directory.GetCurrentDirectory(), "..", "..");
            _testProjectSourceDirectory = testProjectSourceDirectory 
                ?? Path.Combine(_repoRoot, "TestAssets", "TestProjects");
            _testArtifactDirectory = _testArtifactDirectory 
                ?? Environment.GetEnvironmentVariable(s_testArtifactDirectoryEnvironmentVariable)
                ?? Path.Combine(AppContext.BaseDirectory, s_testArtifactDirectoryEnvironmentVariable);

            _sdkDotnet = new DotNetCli(dotnetInstallPath ?? DotNetCli.GetStage0Path(_repoRoot));
            _currentRid = currentRid ?? _sdkDotnet.GetRuntimeId();

            _builtDotnetOutputPath = builtDotnetOutputPath 
                ?? Path.Combine(_repoRoot, "artifacts", _currentRid, "intermediate", "sharedFrameworkPublish");
            _builtDotnet = new DotNetCli(_builtDotnetOutputPath);

            InitializeTestProject(_testProjectName, _testProjectSourceDirectory, _testArtifactDirectory, _exeExtension);
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
            if ( !Directory.Exists(subdirectory))
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

        public TestProjectFixture(TestProjectFixture fixtureToCopy)
        {
            _testProjectName = fixtureToCopy._testProjectName;
            _exeExtension = fixtureToCopy._exeExtension;
            _repoRoot = fixtureToCopy._repoRoot;
            _testProjectSourceDirectory = fixtureToCopy._testProjectSourceDirectory;
            _testArtifactDirectory = fixtureToCopy._testArtifactDirectory;
            _sdkDotnet = fixtureToCopy._sdkDotnet;
            _currentRid = fixtureToCopy._currentRid;
            _builtDotnetOutputPath = fixtureToCopy._builtDotnetOutputPath;
            _builtDotnet = fixtureToCopy._builtDotnet;
            _sourceTestProject = fixtureToCopy._sourceTestProject;

            _testProject = CopyTestProject(fixtureToCopy.TestProject, _testArtifactDirectory, _exeExtension);
        }

        private void InitializeTestProject(
            string testProjectName,
            string testProjectSourceDirectory,
            string testArtifactDirectory,
            string exeExtension)
        {
            var sourceTestProjectPath = Path.Combine(testProjectSourceDirectory, testProjectName);
            _sourceTestProject = new TestProject(sourceTestProjectPath, _exeExtension);

            _testProject = CopyTestProject(_sourceTestProject, testArtifactDirectory, exeExtension);
        }

        private TestProject CopyTestProject(
            TestProject sourceTestProject, 
            string testArtifactDirectory, 
            string exeExtension)
        {
            string copiedTestProjectDirectory = CalculateTestProjectDirectory(
                sourceTestProject.ProjectName, 
                testArtifactDirectory);

            sourceTestProject.CopyProjectFiles(copiedTestProjectDirectory);
            return new TestProject(copiedTestProjectDirectory, exeExtension);
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
                .Execute()
                .EnsureSuccessful();

            _testProject.LoadOutputFiles();

            return this;
        }

        public TestProjectFixture RestoreProject(DotNetCli dotnet = null)
        {
            dotnet = dotnet ?? _sdkDotnet;

            dotnet.Restore()
                .WorkingDirectory(_testProject.ProjectDirectory)
                .Execute()
                .EnsureSuccessful();

            return this;
        }

        public TestProjectFixture EnsureRestored(DotNetCli dotnet = null)
        {
            dotnet = dotnet ?? _sdkDotnet;

            if ( ! _testProject.IsRestored())
            {
                RestoreProject(dotnet: dotnet);
            }

            return this;
        }

        public TestProjectFixture Copy()
        {
            return new TestProjectFixture(this);
        }
    }
}
