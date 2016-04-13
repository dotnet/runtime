using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.DotNet.Cli.Utils;
using Microsoft.DotNet.TestFramework;
using Microsoft.DotNet.Tools.Test.Utilities;
using FluentAssertions;
using Xunit;

namespace Microsoft.Extensions.DependencyModel
{
    public class FunctionalTests : TestBase
    {
        private readonly string _testProjectsRoot;

        public FunctionalTests()
        {
            _testProjectsRoot = Path.Combine(AppContext.BaseDirectory, "TestAssets", "TestProjects");
        }

        [Theory]
        [InlineData("TestApp", true)]
        [InlineData("TestAppPortable", true)]
        [InlineData("TestAppDeps", false)]
        [InlineData("TestAppPortableDeps", false)]
        public void RunTest(string appname, bool checkCompilation)
        {
            var testProjectPath = Path.Combine(RepoRoot, "TestAssets", "TestProjects", "DependencyContextValidator", appname);
            var testProject = Path.Combine(testProjectPath, "project.json");

            var runCommand = new RunCommand(testProject);
            var result = runCommand.ExecuteWithCapturedOutput();
            result.Should().Pass();
            ValidateRuntimeLibraries(result, appname);
            if (checkCompilation)
            {
                ValidateCompilationLibraries(result, appname);
            }
        }

        [Theory]
        [InlineData("TestApp", false, true)]
        [InlineData("TestAppPortable", true, true)]
        [InlineData("TestAppDeps", false, false)]
        [InlineData("TestAppPortableDeps", true, false)]
        public void PublishTest(string appname, bool portable, bool checkCompilation)
        {
            var testProjectPath = Path.Combine(RepoRoot, "TestAssets", "TestProjects", "DependencyContextValidator", appname);
            var testProject = Path.Combine(testProjectPath, "project.json");

            var publishCommand = new PublishCommand(testProject);
            publishCommand.Execute().Should().Pass();

            var exeName = portable ? publishCommand.GetPortableOutputName() : publishCommand.GetOutputExecutable();

            var result = TestExecutable(publishCommand.GetOutputDirectory(portable).FullName, exeName, string.Empty);
            ValidateRuntimeLibraries(result, appname);
            if (checkCompilation)
            {
                ValidateCompilationLibraries(result, appname);
            }
        }

        [WindowsOnlyFact]
        public void RunTestFullClr()
        {
            var testProjectPath = Path.Combine(RepoRoot, "TestAssets", "TestProjects", "DependencyContextValidator", "TestAppFullClr");
            var testProject = Path.Combine(testProjectPath, "project.json");

            var runCommand = new RunCommand(testProject);
            var result = runCommand.ExecuteWithCapturedOutput();
            result.Should().Pass();
            ValidateRuntimeLibrariesFullClr(result, "TestAppFullClr");
            ValidateCompilationLibrariesFullClr(result, "TestAppFullClr");
        }

        [WindowsOnlyFact]
        public void PublishTestFullClr()
        {
            var testProjectPath = Path.Combine(RepoRoot, "TestAssets", "TestProjects", "DependencyContextValidator", "TestAppFullClr");
            var testProject = Path.Combine(testProjectPath, "project.json");

            var publishCommand = new PublishCommand(testProject);
            publishCommand.Execute().Should().Pass();

            var result = TestExecutable(publishCommand.GetOutputDirectory().FullName, publishCommand.GetOutputExecutable(), string.Empty);
            ValidateRuntimeLibrariesFullClr(result, "TestAppFullClr");
            ValidateCompilationLibrariesFullClr(result, "TestAppFullClr");
        }

        private void ValidateRuntimeLibrariesFullClr(CommandResult result, string appname)
        {
            // entry assembly
            result.Should().HaveStdOutContaining($"Runtime {appname}:{appname}");
            // project dependency
            result.Should().HaveStdOutContaining("Runtime DependencyContextValidator:DependencyContextValidator");
        }

        private void ValidateCompilationLibrariesFullClr(CommandResult result, string appname)
        {
            // entry assembly
            result.Should().HaveStdOutContaining($"Compilation {appname}:{appname}.exe");
            // project dependency
            result.Should().HaveStdOutContaining("Compilation DependencyContextValidator:DependencyContextValidator.dll");
            // system assembly
            result.Should().HaveStdOutContaining("Compilation mscorlib:mscorlib.dll");
        }


        private void ValidateRuntimeLibraries(CommandResult result, string appname)
        {
            // entry assembly
            result.Should().HaveStdOutContaining($"Runtime {appname}:{appname}");
            // project dependency
            result.Should().HaveStdOutContaining("Runtime DependencyContextValidator:DependencyContextValidator");
            // system assembly
            result.Should().HaveStdOutContaining("Runtime System.Linq:System.Linq");
        }

        private void ValidateCompilationLibraries(CommandResult result, string appname)
        {
            // entry assembly
            result.Should().HaveStdOutContaining($"Compilation {appname}:{appname}.dll");
            // project dependency
            result.Should().HaveStdOutContaining("Compilation DependencyContextValidator:DependencyContextValidator.dll");
            // system assembly
            result.Should().HaveStdOutContaining("Compilation System.Linq:System.Linq.dll");
        }

    }
}
