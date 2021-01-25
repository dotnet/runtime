using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace JitBench
{
    class CscHelloWorldBenchmark : CscBenchmark
    {
        public CscHelloWorldBenchmark() : base("Csc_Hello_World")
        {
        }

#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously
        protected override async Task SetupSourceToCompile(string intermediateOutputDir, string runtimeDirPath, bool useExistingSetup, ITestOutputHelper output)
#pragma warning restore CS1998
        {
            string helloWorldDir = Path.Combine(intermediateOutputDir, "helloWorldSource");
            string helloWorldPath = Path.Combine(helloWorldDir, "hello.cs");
            string systemPrivateCoreLibPath = Path.Combine(runtimeDirPath, "System.Private.CoreLib.dll");
            string systemRuntimePath = Path.Combine(runtimeDirPath, "System.Runtime.dll");
            string systemConsolePath = Path.Combine(runtimeDirPath, "System.Console.dll");
            CommandLineArguments = "hello.cs /nostdlib /r:" + systemPrivateCoreLibPath + " /r:" + systemRuntimePath + " /r:" + systemConsolePath;
            WorkingDirPath = helloWorldDir;
            if(useExistingSetup)
            {
                return;
            }

            FileTasks.DeleteDirectory(helloWorldDir, output);
            FileTasks.CreateDirectory(helloWorldDir, output);
            File.WriteAllLines(helloWorldPath, new string[]
            {
                "using System;",
                "public static class Program",
                "{",
                "    public static void Main(string[] args)",
                "    {",
                "        Console.WriteLine(\"Hello World!\");",
                "    }",
                "}"
            });
        }
    }
}
