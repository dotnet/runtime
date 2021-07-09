using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using Xunit;

namespace R2RDumpTests
{
    public class R2RDumpTester : XunitBase
    {
        private const string CoreRoot = "CORE_ROOT";
        private const string R2RDumpRelativePath = "R2RDump";
        private const string R2RDumpFile = "R2RDump.dll";
        private const string CoreRunFileName = "corerun";
        
        public static string FindExePath(string exe)
        {
            if (OperatingSystem.IsWindows())
            {
                exe = exe + ".exe";
            }
            exe = Environment.ExpandEnvironmentVariables(exe);
            if (!File.Exists(exe))
            {
                if (Path.GetDirectoryName(exe) == String.Empty)
                {
                    foreach (string test in (Environment.GetEnvironmentVariable("PATH") ?? "").Split(Path.PathSeparator))
                    {
                        string path = test.Trim();
                        if (!String.IsNullOrEmpty(path) && File.Exists(path = Path.Combine(path, exe)))
                            return Path.GetFullPath(path);
                    }
                }
                throw new FileNotFoundException(new FileNotFoundException().Message, exe);
            }
            return Path.GetFullPath(exe);
        }

        [Fact]
        public void DumpCoreLib()
        {
            string CoreRootVar = Environment.GetEnvironmentVariable(CoreRoot);
            bool IsUnix = !OperatingSystem.IsWindows();
            string R2RDumpAbsolutePath = Path.Combine(CoreRootVar, R2RDumpRelativePath, R2RDumpFile);
            string CoreLibFile = "System.Private.CoreLib.dll";
            string CoreLibAbsolutePath = Path.Combine(CoreRootVar, CoreLibFile);
            string OutputFile = Path.GetTempFileName();
            string TestDotNetCmdVar = Environment.GetEnvironmentVariable("__TestDotNetCmd");
            string DotNetAbsolutePath = string.IsNullOrEmpty(TestDotNetCmdVar) ? FindExePath("dotnet") : TestDotNetCmdVar;

            ProcessStartInfo processStartInfo = new ProcessStartInfo
            {
                UseShellExecute = false,
                FileName = DotNetAbsolutePath,
                // TODO, what flags do we like to test?
                Arguments = string.Join(" ", new string[]{"exec", R2RDumpAbsolutePath, "--in", CoreLibAbsolutePath, "--out", OutputFile})
            };

            Process process = Process.Start(processStartInfo);
            process.WaitForExit();
            int exitCode = process.ExitCode;
            string outputContent = File.ReadAllText(OutputFile);
            File.Delete(OutputFile);
            // TODO, here is a point where we can add more validation to outputs
            // An uncaught exception (such as signature decoding error, would be caught by the error code)
            bool failed = exitCode != 0;
            if (failed)
            {
                Console.WriteLine("The process terminated with exit code {0}", exitCode);
                Console.WriteLine(outputContent);
                Assert.True(!failed);
            }
        }

        public static int Main(string[] args)
        {
            return new R2RDumpTester().RunTests();
        }
    }
}