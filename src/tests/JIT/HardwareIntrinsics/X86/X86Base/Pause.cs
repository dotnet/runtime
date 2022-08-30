// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//

using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;
using Xunit;

namespace IntelHardwareIntrinsicTest.Pause
{
    public class Program
    {
        const int Pass = 100;
        const int Fail = 0;

        [Fact]
        public static unsafe void Pause()
        {
            if (X86Base.IsSupported)
            {
                X86Base.Pause();
            }
            else
            {
                Assert.Throws<PlatformNotSupportedException>(X86Base.Pause);
            }
        }
    }
}
