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

        [Fact]
        public void DumpCoreLib()
        {
            string CoreRootVar = Environment.GetEnvironmentVariable(CoreRoot);
            bool IsUnix = !RuntimeInformation.IsOSPlatform(OSPlatform.Windows);
            string CoreRunFile = CoreRunFileName + (IsUnix ? string.Empty : ".exe");
            string CoreRunAbsolutePath = Path.Combine(CoreRootVar, CoreRunFile);
            string R2RDumpAbsolutePath = Path.Combine(Path.Combine(CoreRootVar, R2RDumpRelativePath), R2RDumpFile);
            string CoreLibFile = "System.Private.CoreLib.dll";
            string CoreLibAbsolutePath = Path.Combine(CoreRootVar, CoreLibFile);
            string OutputFile = Path.GetTempFileName();

            ProcessStartInfo processStartInfo = new ProcessStartInfo
            {
                UseShellExecute = false,
                FileName = CoreRunAbsolutePath,
                // TODO, what flags do we like to test?
                Arguments = string.Join(" ", new string[]{R2RDumpAbsolutePath, "--in", CoreLibAbsolutePath, "--out", OutputFile})
            };

            Process process = Process.Start(processStartInfo);
            process.WaitForExit();
            string outputFileContent = File.ReadAllText(OutputFile);
            // TODO, validate content more carefully
            Assert.True(outputFileContent.Contains("ToString"));
            File.Delete(OutputFile);
        }

        public static int Main(string[] args)
        {
            return new R2RDumpTester().RunTests();
        }
    }
}