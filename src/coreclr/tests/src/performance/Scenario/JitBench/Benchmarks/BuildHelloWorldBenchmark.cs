using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace JitBench
{
    public class BuildHelloWorldBenchmark : Benchmark
    {
        public BuildHelloWorldBenchmark() : base("Dotnet_Build_HelloWorld") { }

        public override async Task Setup(DotNetInstallation dotNetInstall, string intermediateOutputDir, bool useExistingSetup, ITestOutputHelper output)
        {
            using (var setupSection = new IndentedTestOutputHelper("Setup " + Name, output))
            {
                await SetupHelloWorldProject(dotNetInstall, intermediateOutputDir, useExistingSetup, setupSection);
            }
        }

        protected async Task SetupHelloWorldProject(DotNetInstallation dotNetInstall, string intermediateOutputDir, bool useExistingSetup, ITestOutputHelper output)
        {
            string helloWorldProjectDir = Path.Combine(intermediateOutputDir, "helloworld");
            //the 'exePath' gets passed as an argument to dotnet.exe
            //in this case it isn't an executable at all, its a CLI command
            //a little cheap, but it works
            ExePath = "build";
            WorkingDirPath = helloWorldProjectDir;

            // This disables using the shared build server. I was told using it interferes with the ability to delete folders after the
            // test is complete though I haven't encountered that particular issue myself. I imagine this meaningfully changes the
            // performance of this benchmark, so if we ever want to do real perf testing on the shared scenario we have to resolve this
            // issue another way.
            EnvironmentVariables["UseSharedCompilation"] = "false";

            if(!useExistingSetup)
            {
                FileTasks.DeleteDirectory(helloWorldProjectDir, output);
                FileTasks.CreateDirectory(helloWorldProjectDir, output);
                await new ProcessRunner(dotNetInstall.DotNetExe, "new console")
                    .WithWorkingDirectory(helloWorldProjectDir)
                    .WithLog(output)
                    .Run();

                RetargetProjects(dotNetInstall, helloWorldProjectDir, new string[] { "helloworld.csproj" });
            }
        }
    }
}
