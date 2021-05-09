// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Linq;
using Microsoft.DotNet.RemoteExecutor;
using Xunit;

namespace System.Tests
{
    public partial class NonStandardConfigurationTests
    {
        [PlatformSpecific(TestPlatforms.AnyUnix)] // Uses P/Invokes
        [ConditionalFact(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/49568", typeof(PlatformDetection), nameof(PlatformDetection.IsMacOsAppleSilicon))]
        public void NonBlockingStdout_AllDataReceived()
        {
            RemoteInvokeHandle remote = RemoteExecutor.Invoke(() =>
            {
                char[] data = Enumerable.Repeat('a', 1024).ToArray();

                const int StdoutFd = 1;
                Assert.Equal(0, Interop.Sys.Fcntl.DangerousSetIsNonBlocking((IntPtr)StdoutFd, 1));

                for (int i = 0; i < 10_000; i++)
                {
                    Console.Write(data);
                }
            }, new RemoteInvokeOptions { StartInfo = new ProcessStartInfo() { RedirectStandardOutput = true } });

            using (remote)
            {
                Assert.Equal(
                    new string('a', 1024 * 10_000),
                    remote.Process.StandardOutput.ReadToEnd());
            }
        }
    }
}
