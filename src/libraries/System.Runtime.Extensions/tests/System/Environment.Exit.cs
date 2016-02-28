// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;
using System.Runtime.InteropServices;
using Xunit;

namespace System.Tests
{
    public class Environment_Exit : RemoteExecutorTestBase
    {
        [Theory]
        [InlineData(0)]
        [InlineData(1)]
        [InlineData(42)]
        [InlineData(-1)]
        [InlineData(-45)]
        [InlineData(255)]
        public static void CheckExitCode(int expectedExitCode)
        {
            using (Process p = RemoteInvoke(s => int.Parse(s), expectedExitCode.ToString()).Process)
            {
                Assert.True(p.WaitForExit(30 * 1000));
                if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                {
                    Assert.Equal(expectedExitCode, p.ExitCode);
                }
                else
                {
                    Assert.Equal((sbyte)expectedExitCode, (sbyte)p.ExitCode);
                }
            }
        }
    }
}
