// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.IO;
using Xunit;

namespace System.Diagnostics.Tests
{
    public partial class ProcessStartInfoTests
    {
        [Fact]
        public void UnintializedArgumentList()
        {
            ProcessStartInfo psi = new ProcessStartInfo();
            Assert.Equal(0, psi.ArgumentList.Count);

            psi = new ProcessStartInfo("filename", "-arg1 -arg2");
            Assert.Equal(0, psi.ArgumentList.Count);
        }

        [Fact]
        public void InitializeWithArgumentList()
        {
            ProcessStartInfo psi = new ProcessStartInfo("filename");
            psi.ArgumentList.Add("arg1");
            psi.ArgumentList.Add("arg2");

            Assert.Equal(2, psi.ArgumentList.Count);
            Assert.Equal("arg1", psi.ArgumentList[0]);
            Assert.Equal("arg2", psi.ArgumentList[1]);
        }

        [ConditionalTheory(typeof(PlatformDetection), nameof(PlatformDetection.IsNotWindowsNanoServer))] // No Notepad on Nano
        [MemberData(nameof(UseShellExecute))]
        [OuterLoop("Launches notepad")]
        [PlatformSpecific(TestPlatforms.Windows)]
        [SkipOnTargetFramework(TargetFrameworkMonikers.Uap, "WaitForInputIdle, ProcessName, and MainWindowTitle are not supported on UAP")]
        public void StartInfo_NotepadWithContent_withArgumentList(bool useShellExecute)
        {
            string tempFile = GetTestFilePath() + ".txt";
            File.WriteAllText(tempFile, $"StartInfo_NotepadWithContent({useShellExecute})");

            ProcessStartInfo info = new ProcessStartInfo
            {
                UseShellExecute = useShellExecute,
                FileName = @"notepad.exe",
                Arguments = null,
                WindowStyle = ProcessWindowStyle.Minimized
            };

            info.ArgumentList.Add(tempFile);

            using (var process = Process.Start(info))
            {
                Assert.True(process != null, $"Could not start {info.FileName} {info.Arguments} UseShellExecute={info.UseShellExecute}");

                try
                {
                    process.WaitForInputIdle(); // Give the file a chance to load
                    Assert.Equal("notepad", process.ProcessName);

                    // On some Windows versions, the file extension is not included in the title
                    Assert.StartsWith(Path.GetFileNameWithoutExtension(tempFile), process.MainWindowTitle);
                }
                finally
                {
                    process?.Kill();
                }
            }
        }
    }
}
