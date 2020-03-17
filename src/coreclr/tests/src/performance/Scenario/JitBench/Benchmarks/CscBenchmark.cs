using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace JitBench
{
    public abstract class CscBenchmark : Benchmark
    {
        public CscBenchmark(string name) : base(name) { }

        public override async Task Setup(DotNetInstallation dotNetInstall, string intermediateOutputDir, bool useExistingSetup, ITestOutputHelper output)
        {
            using (var setupSection = new IndentedTestOutputHelper("Setup " + Name, output))
            {
                SetupCscBinDir(dotNetInstall.SdkDir, dotNetInstall.FrameworkVersion, intermediateOutputDir, useExistingSetup, setupSection);
                await SetupSourceToCompile(intermediateOutputDir, dotNetInstall.FrameworkDir, useExistingSetup, setupSection);
            }
        }

        protected void SetupCscBinDir(string sdkDirPath, string runtimeVersion, string intermediateOutputDir, bool useExistingSetup, ITestOutputHelper output)
        {
            // copy the SDK version of csc into a private directory so we can safely retarget it
            string cscBinaryDirPath = Path.Combine(sdkDirPath, "Roslyn", "bincore");
            string localCscDir = Path.Combine(intermediateOutputDir, "csc");
            ExePath = Path.Combine(localCscDir, "csc.dll");

            if(useExistingSetup)
            {
                return;
            }

            FileTasks.DirectoryCopy(cscBinaryDirPath, localCscDir, output);
            //overwrite csc.runtimeconfig.json to point at the runtime version we want to use
            string runtimeConfigPath = Path.Combine(localCscDir, "csc.runtimeconfig.json");
            File.Delete(runtimeConfigPath);
            File.WriteAllLines(runtimeConfigPath, new string[] {
                "{",
                "  \"runtimeOptions\": {",
                "    \"tfm\": \"netcoreapp2.0\",",
                "    \"framework\": {",
                "        \"name\": \"Microsoft.NETCore.App\",",
                "        \"version\": \"" + runtimeVersion + "\"",
                "    }",
                "  }",
                "}"
            });
        }

        protected abstract Task SetupSourceToCompile(string intermediateOutputDir, string runtimeDirPath, bool useExistingSetup, ITestOutputHelper output);
    }
}
