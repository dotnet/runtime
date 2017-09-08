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
        [Fact]
        public void ProcessStart_UseShellExecuteTrue_OpenFile_ThrowsIfNoDefaultProgramInstalledSucceedsOtherwise()
        {
            string fileToOpen = GetTestFilePath() + ".txt";
            File.WriteAllText(fileToOpen, $"{nameof(ProcessStart_UseShellExecuteTrue_OpenFile_ThrowsIfNoDefaultProgramInstalledSucceedsOtherwise)}");

            string[] allowedProgramsToRun = { "xdg-open", "gnome-open", "kfmclient" };
            foreach (var program in allowedProgramsToRun)
            {
                if (IsProgramInstalled(program))
                {
                    var startInfo = new ProcessStartInfo { UseShellExecute = true, FileName = fileToOpen };
                    using (var px = Process.Start(startInfo))
                    {
                        if (px != null)
                        {
                            Assert.Equal(program, px.ProcessName);
                            px.Kill();
                            px.WaitForExit();
                            Assert.True(px.HasExited);
                            Assert.Equal(137, px.ExitCode); // 137 means the process was killed
                        }
                    }
                    return;
                }
            }

            Win32Exception e = Assert.Throws<Win32Exception>(() => Process.Start(new ProcessStartInfo { UseShellExecute = true, FileName = fileToOpen }));
        }

        [Fact]
        [OuterLoop("Returns failure exit code when default program, xdg-open, is installed")]
        public void ProcessStart_UseShellExecuteTrue_OpenMissingFile_DefaultProgramInstalled_ReturnsFailureExitCode()
        {
            string fileToOpen = Path.Combine(Environment.CurrentDirectory, "_no_such_file.TXT");
            using (var p = Process.Start(new ProcessStartInfo { UseShellExecute = true, FileName = fileToOpen }))
            {
                if (p != null)
                {
                    Assert.Equal("xdg-open", p.ProcessName);
                    p.WaitForExit();
                    Assert.True(p.HasExited);
                    Assert.Equal(2, p.ExitCode);
                }
            }
        }

        /// <summary>
        /// Gets the path to the program
        /// </summary>
        /// <param name="program"></param>
        /// <returns></returns>
        private bool IsProgramInstalled(string program)
        {
            string path = Environment.GetEnvironmentVariable("PATH");
            string[] dirs = path.Split(':');
            foreach (var dir in dirs)
            {
                string[] files = Directory.GetFiles(dir, program);
                if (files.Length != 0)
                {
                    return true;
                }
            }
            return false;
        }
    }
}
