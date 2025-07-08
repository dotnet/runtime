// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//

using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;
using Xunit;

namespace IntelHardwareIntrinsicTest
{
    public class Program
    {
        [Fact]
        public static unsafe void TestUserLevelMonitor()
        {
            if (WaitPkg.IsSupported)
            {
                RunBasicScenario();
            }
            else
            {
                Assert.Throws<PlatformNotSupportedException>(RunBasicScenario);
            }

            static void RunBasicScenario()
            {
                // We're really just testing this doesn't throw. So
                // indicate a fast wakeup and the lowest counter possible

                byte* data = stackalloc byte[1];

                WaitPkg.SetUpUserLevelMonitor(data);
                WaitPkg.WaitForUserLevelMonitor(control: 1, counter: 1);
            }
        }

        [Fact]
        public static unsafe void TestTimedPause()
        {
            if (WaitPkg.IsSupported)
            {
                RunBasicScenario();
            }
            else
            {
                Assert.Throws<PlatformNotSupportedException>(RunBasicScenario);
            }

            static void RunBasicScenario()
            {
                // We're really just testing this doesn't throw. So
                // indicate a fast wakeup and the lowest counter possible

                byte* data = stackalloc byte[1];
                WaitPkg.TimedPause(control: 1, counter: 1);
            }
        }
    }
}
