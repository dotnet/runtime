// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// This test was the repro case for issue #35144.
// Until interop is supported for vectors, it is difficult to validate
// that the ABI is correctly implemented, but this test is here to enable
// these cases to be manually verified (and diffed).
//
using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.Arm;
using Xunit;

public static class Runtime_35976
{
    [MethodImpl(MethodImplOptions.NoInlining)]
    public static uint fo1(uint value)
    {
        if (AdvSimd.IsSupported)
        {
            var input = Vector64.CreateScalar(value);
            return AdvSimd.Extract(input, 0);
        }
        return 0;
    }

    [Fact]
    public static int TestEntryPoint()
    {
        fo1(1);
        return 100;
    }
}
