// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Text;
using Xunit;

namespace System.Diagnostics.Tests
{
    public partial class ProcessTests : ProcessTestBase
    {
        [Fact]
        public void Start_HasStandardInputEncodingNonRedirected_ThrowsInvalidOperationException()
        {
            var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "FileName",
                    RedirectStandardInput = false,
                    StandardInputEncoding = Encoding.UTF8
                }
            };

            Assert.Throws<InvalidOperationException>(() => process.Start());
        }

        [Fact]
        public void Start_StandardInputEncodingPropagatesToStreamWriter()
        {
            var process = CreateProcessPortable(RemotelyInvokable.Dummy);
            process.StartInfo.RedirectStandardInput = true;
            var encoding = new UTF32Encoding(bigEndian: false, byteOrderMark: true);
            process.StartInfo.StandardInputEncoding = encoding;
            process.Start();

            Assert.Same(encoding, process.StandardInput.Encoding);
        }

        [Fact]
        [PlatformSpecific(TestPlatforms.Windows)]
        [SkipOnTargetFramework(TargetFrameworkMonikers.Uap, "Retrieving information about local processes is not supported on uap")]
        public void StartProcessWithArgumentList()
        {
            ProcessStartInfo psi = new ProcessStartInfo(GetCurrentProcessName());
            psi.ArgumentList.Add("arg1");
            psi.ArgumentList.Add("arg2");

            Process testProcess = CreateProcess();
            testProcess.StartInfo = psi;

            try
            {
                testProcess.Start();
                Assert.Equal(string.Empty, testProcess.StartInfo.Arguments);
            }
            finally
            {
                if (!testProcess.HasExited)
                    testProcess.Kill();

                Assert.True(testProcess.WaitForExit(WaitInMS));
            }
        }

        [Fact]
        [PlatformSpecific(TestPlatforms.Windows)]
        [SkipOnTargetFramework(TargetFrameworkMonikers.Uap, "Retrieving information about local processes is not supported on uap")]
        public void StartProcessWithSameArgumentList()
        {
            ProcessStartInfo psi = new ProcessStartInfo(GetCurrentProcessName());
            psi.ArgumentList.Add("arg1");
            psi.ArgumentList.Add("arg2");

            Process testProcess = CreateProcess();
            Process secondTestProcess = CreateProcess();
            testProcess.StartInfo = psi;
            try
            {
                testProcess.Start();
                Assert.Equal(string.Empty, testProcess.StartInfo.Arguments);
                secondTestProcess.StartInfo = psi;
                secondTestProcess.Start();
                Assert.Equal(string.Empty, secondTestProcess.StartInfo.Arguments);
            }
            finally
            {
                if (!testProcess.HasExited)
                    testProcess.Kill();

                Assert.True(testProcess.WaitForExit(WaitInMS));

                if (!secondTestProcess.HasExited)
                    secondTestProcess.Kill();

                Assert.True(testProcess.WaitForExit(WaitInMS));
            }
        }

        [Fact]
        [SkipOnTargetFramework(TargetFrameworkMonikers.Uap, "Retrieving information about local processes is not supported on uap")]
        public void BothArgumentCtorAndArgumentListSet()
        {
            ProcessStartInfo psi = new ProcessStartInfo(GetCurrentProcessName(), "arg3");
            psi.ArgumentList.Add("arg1");
            psi.ArgumentList.Add("arg2");

            Process testProcess = CreateProcess();
            testProcess.StartInfo = psi;
            Assert.Throws<InvalidOperationException>(() => testProcess.Start());
        }

        [Fact]
        [SkipOnTargetFramework(TargetFrameworkMonikers.Uap, "Retrieving information about local processes is not supported on uap")]
        public void BothArgumentSetAndArgumentListSet()
        {
            ProcessStartInfo psi = new ProcessStartInfo(GetCurrentProcessName());
            psi.Arguments = "arg3";
            psi.ArgumentList.Add("arg1");
            psi.ArgumentList.Add("arg2");

            Process testProcess = CreateProcess();
            testProcess.StartInfo = psi;
            Assert.Throws<InvalidOperationException>(() => testProcess.Start());
        }
    }
}
