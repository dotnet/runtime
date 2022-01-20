// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.Intrinsics.X86;
using Xunit;

namespace System.Runtime.Intrinsics.Tests.X86;

public sealed partial class ShaTests
{
    [ConditionalFact(typeof(X86Base), nameof(X86Base.IsSupported))]
    public void TestIsSupported()
    {
        bool intrinsicSupported = Sha.IsSupported;

        (_, int b, _, _) = X86Base.CpuId(7, 0);

        // Intel SHA Extensions feature bit is EBX[29]
        bool isShaCpuIdSupported = (b & (1 << 29)) != 0;

        // Verify that `Sha.IsSupported` and the SHA CPUID bit
        // produces the same value
        Assert.Equal(isShaCpuIdSupported, intrinsicSupported);
    }
}
