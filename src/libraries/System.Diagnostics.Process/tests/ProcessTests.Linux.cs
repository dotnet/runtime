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
        private const string s_xdg_open = "xdg-open";

        [Fact]
        // [OuterLoop("Opens program")]
        public void ProcessStart_UseShellExecuteTrue_OpenFile_ThrowsIfNoDefaultProgramInstalledSucceedsOtherwise()
        {
            string fileToOpen = GetTestFilePath() + ".txt";
            File.WriteAllText(fileToOpen, $"{nameof(ProcessStart_UseShellExecuteTrue_OpenFile_ThrowsIfNoDefaultProgramInstalledSucceedsOtherwise)}");

            string[] allowedProgramsToRun = { s_xdg_open, "gnome-open", "kfmclient" };
            foreach (var program in allowedProgramsToRun)
            {
                if (IsProgramInstalled(program))
                {
                    var startInfo = new ProcessStartInfo { UseShellExecute = true, FileName = fileToOpen };
                    using (var px = Process.Start(startInfo))
                    {
                        Assert.NotNull(px);
                        Console.WriteLine($"{nameof(ProcessStart_UseShellExecuteTrue_OpenFile_ThrowsIfNoDefaultProgramInstalledSucceedsOtherwise)}(): {program} was used to open file on this machine. ProcessName: {px.ProcessName}");
                        Assert.Equal(program, px.ProcessName);
                        px.Kill();
                        px.WaitForExit();
                        Assert.True(px.HasExited);
                        Assert.Equal(137, px.ExitCode); // 137 means the process was killed
                    }
                    return;
                }
            }

            Win32Exception e = Assert.Throws<Win32Exception>(() => Process.Start(new ProcessStartInfo { UseShellExecute = true, FileName = fileToOpen }));
        }

        [Theory, InlineData("nano")]
        // [OuterLoop("Opens program")]
        public void ProcessStart_OpenFile_UsesSpecifiedProgram(string programToOpenWith)
        {
            string fileToOpen = GetTestFilePath() + ".txt";
            File.WriteAllText(fileToOpen, $"{nameof(ProcessStart_OpenFile_UsesSpecifiedProgram)}");
            using (var px = Process.Start(programToOpenWith, fileToOpen))
            {
                Assert.Equal(programToOpenWith, px.ProcessName);
                px.Kill();
                px.WaitForExit();
                Assert.True(px.HasExited);
                Assert.Equal(137, px.ExitCode); // 137 means the process was killed
            }
        }

        [Fact]
        // [OuterLoop("test should succeed when xdg-open is installed. Otherwise we write to console")]
        public void ProcessStart_UseShellExecuteTrue_OpenMissingFile_XdgOpenReturnsExitCode2()
        {
            if (IsProgramInstalled(s_xdg_open))
            {
                string fileToOpen = Path.Combine(Environment.CurrentDirectory, "_no_such_file.TXT");
                using (var p = Process.Start(new ProcessStartInfo { UseShellExecute = true, FileName = fileToOpen }))
                {
                    if (p != null)
                    {
                        Assert.Equal(s_xdg_open, p.ProcessName);
                        p.WaitForExit();
                        Assert.True(p.HasExited);
                        Assert.Equal(2, p.ExitCode);
                    }
                }
            }
            else
            {
                Console.WriteLine($"{nameof(ProcessStart_UseShellExecuteTrue_OpenMissingFile_XdgOpenReturnsExitCode2)}(): {s_xdg_open} is not installed on this machine.");
            }
        }

        /// <summary>
        /// Checks if the program is installed
        /// </summary>
        /// <param name="program"></param>
        /// <returns></returns>
        private bool IsProgramInstalled(string program)
        {
            string path;
            string pathEnvVar = Environment.GetEnvironmentVariable("PATH");
            if (pathEnvVar != null)
            {
                var pathParser = new StringParser(pathEnvVar, ':', skipEmpty: true);
                while (pathParser.MoveNext())
                {
                    string subPath = pathParser.ExtractCurrent();
                    path = Path.Combine(subPath, program);
                    if (File.Exists(path))
                    {
                        return true;
                    }
                }
            }
            return false;
        }
    }
}
