// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//

using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.Arm;
using Xunit;

namespace JIT.HardwareIntrinsics.Arm
{
    public class Program
    {
        [Fact]
        public static unsafe void Yield()
        {
            if (ArmBase.IsSupported)
            {
                ArmBase.Yield();
            }
            else
            {
                Assert.Throws<PlatformNotSupportedException>(ArmBase.Yield);
            }
        }
    }
}
