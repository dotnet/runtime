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
        public static unsafe void Test()
        {
            if (X86Serialize.IsSupported)
            {
                // Should work without throwing
                X86Serialize.Serialize();
            }
            else
            {
                Assert.Throws<PlatformNotSupportedException>(X86Serialize.Serialize);
            }
        }
    }
}
