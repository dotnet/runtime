// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Security;
using Xunit;
using Xunit.NetCore.Extensions;

namespace System.Diagnostics.Tests
{
    public partial class ProcessTests : ProcessTestBase
    {
        [Theory, InlineData("/usr/bin/open"), InlineData("/usr/bin/nano")]
        // [OuterLoop("Opens program")]
        public void ProcessStart_OpenFile_UsesSpecifiedProgram(string programToOpenWith)
        {
            string fileToOpen = GetTestFilePath() + ".txt";
            File.WriteAllText(fileToOpen, $"{nameof(ProcessStart_OpenFile_UsesSpecifiedProgram)}");
            using (var px = Process.Start(programToOpenWith, fileToOpen))
            {
                Assert.False(px.HasExited);
                px.WaitForExit();
                Assert.True(px.HasExited);
                Assert.Equal(0, px.ExitCode); // Exit Code 0 from open means success
            }
        }
        
        [Theory, InlineData("Safari"), InlineData("\"Google Chrome\"")]
        // [OuterLoop("Opens browser")]
        public void ProcessStart_OpenUrl_UsesSpecifiedApplication(string applicationToOpenWith)
        {
            using (var px = Process.Start("/usr/bin/open", "https://github.com/dotnet/corefx -a " + applicationToOpenWith))
            {
                Assert.False(px.HasExited);
                px.WaitForExit();
                Assert.True(px.HasExited);
                Assert.Equal(0, px.ExitCode); // Exit Code 0 from open means success
            }
        }

        [Theory, InlineData("-a Safari"), InlineData("-a \"Google Chrome\"")]
        // [OuterLoop("Opens browser")]
        public void ProcessStart_UseShellExecuteTrue_OpenUrl_SuccessfullyReadsArgument(string arguments)
        {
            var startInfo = new ProcessStartInfo { UseShellExecute = true, FileName = "https://github.com/dotnet/corefx", Arguments = arguments };
            using (var px = Process.Start(startInfo))
            {
                if (px != null)
                {
                    // px.Kill(); // uncommenting this changes exit code to 137, meaning process was killed
                    px.WaitForExit();
                    Assert.True(px.HasExited);
                    Assert.Equal(0, px.ExitCode);
                }
            }
        }

        [Fact]
        // [OuterLoop("Shows that /usr/bin/open fails to open missing file")]
        public void ProcessStart_UseShellExecuteTrue_TryOpenFileThatDoesntExist_ReturnsExitCode1()
        {
            string file = Path.Combine(Environment.CurrentDirectory, "_no_such_file.TXT");
            using (var p = Process.Start(new ProcessStartInfo { UseShellExecute = true, FileName = file }))
            {
                Assert.True(p.WaitForExit(WaitInMS));
                Assert.Equal(1, p.ExitCode); // Exit Code 1 from open means something went wrong
            }
        }
    }
}
