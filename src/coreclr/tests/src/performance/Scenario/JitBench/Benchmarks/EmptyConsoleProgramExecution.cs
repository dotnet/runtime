using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace JitBench
{
    sealed class EmptyConsoleProgramExecution : Benchmark
    {
        private const string ExecutableName = "console.dll";

        public EmptyConsoleProgramExecution() : base("Empty Console Program")
        {
            ExePath = ExecutableName;
        }

        public override async Task Setup(DotNetInstallation dotNetInstall, string outputDir, bool useExistingSetup, ITestOutputHelper output)
        {
            if (!useExistingSetup)
            {
                using (var setupSection = new IndentedTestOutputHelper("Setup " + Name, output))
                {
                    await SetupSourceToCompile(outputDir, dotNetInstall.FrameworkDir, useExistingSetup, setupSection);
                    RetargetProjects(dotNetInstall, GetRootDir(outputDir), new string[] { "console.csproj" });
                    await Publish(dotNetInstall, outputDir, setupSection);
                }
            }

            string tfm = DotNetSetup.GetTargetFrameworkMonikerForFrameworkVersion(dotNetInstall.FrameworkVersion);
            WorkingDirPath = GetAppPublishDirectory(dotNetInstall, outputDir, tfm);
            EnvironmentVariables.Add("DOTNET_MULTILEVEL_LOOKUP", "0");
            EnvironmentVariables.Add("UseSharedCompilation", "false");
        }

#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously
        private async Task SetupSourceToCompile(string intermediateOutputDir, string runtimeDirPath, bool useExistingSetup, ITestOutputHelper output)
#pragma warning restore CS1998
        {
            const string sourceFile = "Program.cs";
            const string csprojFile = "console.csproj";

            string consoleProjectMainDir = GetRootDir(intermediateOutputDir);
            FileTasks.DeleteDirectory(consoleProjectMainDir, output);
            FileTasks.CreateDirectory(consoleProjectMainDir, output);

            File.WriteAllLines(Path.Combine(consoleProjectMainDir, sourceFile), new[] {
                "using System;",
                "public static class Program",
                "{",
                "    public static int Main(string[] args) => 0;",
                "}"
            });

            File.WriteAllLines(Path.Combine(consoleProjectMainDir, csprojFile), new[] {
                @"<Project Sdk=""Microsoft.NET.Sdk"">",
                @"  <PropertyGroup>",
                @"    <OutputType>Exe</OutputType>",
                @"    <TargetFramework>netcoreapp2.1</TargetFramework>",
                @"  </PropertyGroup>",
                @"</Project>",
            });
        }

        private async Task<string> Publish(DotNetInstallation dotNetInstall, string outputDir, ITestOutputHelper output)
        {
            string tfm = DotNetSetup.GetTargetFrameworkMonikerForFrameworkVersion(dotNetInstall.FrameworkVersion);
            string publishDir = GetAppPublishDirectory(dotNetInstall, outputDir, tfm);
            if (publishDir != null)
                FileTasks.DeleteDirectory(publishDir, output);

            string dotNetExePath = dotNetInstall.DotNetExe;
            await new ProcessRunner(dotNetExePath, $"publish -c Release -f {tfm}")
                .WithWorkingDirectory(GetAppSrcDirectory(outputDir))
                .WithEnvironmentVariable("DOTNET_MULTILEVEL_LOOKUP", "0")
                .WithEnvironmentVariable("UseSharedCompilation", "false")
                .WithLog(output)
                .Run();

            publishDir = GetAppPublishDirectory(dotNetInstall, outputDir, tfm);
            if (publishDir == null)
                throw new DirectoryNotFoundException("Could not find 'publish' directory");
            return publishDir;
        }

        private string GetAppPublishDirectory(DotNetInstallation dotNetInstall, string outputDir, string tfm)
        {
            string dir = Path.Combine(GetAppSrcDirectory(outputDir), "bin", dotNetInstall.Architecture, "Release", tfm, "publish");
            if (Directory.Exists(dir))
                return dir;

            dir = Path.Combine(GetAppSrcDirectory(outputDir), "bin", "Release", tfm, "publish");
            if (Directory.Exists(dir))
                return dir;

            return null;
        }

        private static string GetAppSrcDirectory(string outputDir) =>
            Path.Combine(GetRootDir(outputDir));

        private static string GetRootDir(string outputDir) =>
            Path.Combine(outputDir, "EmptyDotNetConsoleProject");
    }
}
