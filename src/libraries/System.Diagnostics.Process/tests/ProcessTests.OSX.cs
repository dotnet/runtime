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
        [OuterLoop("Launches default application")]
        public void TestWithFilename_ShouldUseOpenWithDefaultApp()
        {
            string file = Path.Combine(Environment.CurrentDirectory, "..", "..", "..", "..", "PATENTS.TXT");
            using (var px = Process.Start("/usr/bin/open", file))
            {
                Assert.False(px.HasExited);
                px.WaitForExit();
                Assert.True(px.HasExited);
                Assert.Equal(0, px.ExitCode); // Exit Code 0 from open means success
            }
        }

        [Fact]
        [OuterLoop("Launches default browser")]
        public void TestWithUrl_ShouldUseOpenWithDefaultApp()
        {
            using (var px = Process.Start("/usr/bin/open", "http://www.google.com"))
            {
                Assert.False(px.HasExited);
                px.WaitForExit();
                Assert.True(px.HasExited);
                Assert.Equal(0, px.ExitCode); // Exit Code 0 from open means success
            }
        }

        [Fact]
        // TODO fix behavior to ThrowWin32Exception instead?
        public void ProcessStart_TryOpenFileThatDoesntExist_UseShellExecuteIsTrue_ThrowsWin32Exception()
        {
            string file = Path.Combine(Environment.CurrentDirectory, "..", "..", "..", "..", "_no_such_file.TXT");
            using (var p = Process.Start(new ProcessStartInfo { UseShellExecute = true, FileName = file }))
            {
                Assert.True(p.WaitForExit(WaitInMS));
                Assert.Equal(1, p.ExitCode); // Exit Code 1 from open means something went wrong
            }
        }
    }
}
