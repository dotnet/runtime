// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace System.Diagnostics.Tests
{
    public partial class ProcessStreamReadTests : ProcessTestBase
    {
        [Fact]
        [SkipOnTargetFramework(TargetFrameworkMonikers.Uap, "RemotelyInvokable.ReadLineWithCustomEncodingWriteLineWithUtf8 is not supported on uap")]
        public void TestCustomStandardInputEncoding()
        {
            var process = CreateProcessPortable(RemotelyInvokable.ReadLineWithCustomEncodingWriteLineWithUtf8, Encoding.UTF32.WebName);
            process.StartInfo.RedirectStandardInput = true;
            process.StartInfo.StandardInputEncoding = Encoding.UTF32;
            process.StartInfo.RedirectStandardOutput = true;
            process.StartInfo.StandardOutputEncoding = Encoding.UTF8;
            process.Start();

            const string TestLine = "\U0001f627\U0001f62e\U0001f62f";
            process.StandardInput.WriteLine(TestLine);
            process.StandardInput.Close();

            var output = process.StandardOutput.ReadLine();
            Assert.Equal(TestLine, output);

            Assert.True(process.WaitForExit(WaitInMS));
            Assert.Equal(RemotelyInvokable.SuccessExitCode, process.ExitCode);
        }

        [Fact]
        [SkipOnTargetFramework(TargetFrameworkMonikers.Uap, "RemotelyInvokable.ReadLineWithCustomEncodingWriteLineWithUtf8 is not supported on uap")]
        public void TestMismatchedStandardInputEncoding()
        {
            var process = CreateProcessPortable(RemotelyInvokable.ReadLineWithCustomEncodingWriteLineWithUtf8, Encoding.UTF32.WebName);
            process.StartInfo.RedirectStandardInput = true;
            // incorrect: the process will be writing in UTF-32
            process.StartInfo.StandardInputEncoding = Encoding.ASCII;
            process.StartInfo.RedirectStandardOutput = true;
            process.StartInfo.StandardOutputEncoding = Encoding.UTF8;
            process.Start();

            const string TestLine = "\U0001f627\U0001f62e\U0001f62f";
            process.StandardInput.WriteLine(TestLine);
            process.StandardInput.Close();

            var output = process.StandardOutput.ReadLine();
            Assert.NotEqual(TestLine, output);

            Assert.True(process.WaitForExit(WaitInMS));
            Assert.Equal(RemotelyInvokable.SuccessExitCode, process.ExitCode);
        }
    }
}
