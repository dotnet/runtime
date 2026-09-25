// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Runtime_122237;

using Xunit;

// Self-reassignment through a constructor whose `in` (byref) parameters alias the
// destination: `a = new GEJ(a.x, a.y, a.z, a.infinity)`. Per the ECMA-335 semantics of
// `newobj`, the object must be constructed into a temporary and only then copied to `a`,
// so the constructor observes the *old* value of `a` through the `in` pointers. A copy
// elimination that forwards the constructed value directly into the address-taken `a`
// would make the constructor read the storage it is simultaneously writing, zeroing the
// fields. The loop runs long enough to reach the Mono interpreter's optimized tier.
public readonly struct FE
{
    public readonly uint n0, n1, n2, n3, n4, n5, n6, n7, n8, n9;
    public readonly int magnitude;
    public readonly bool normalized;

    public FE(uint a0, uint a1, uint a2, uint a3, uint a4, uint a5, uint a6, uint a7, uint a8, uint a9)
    {
        n0 = a0; n1 = a1; n2 = a2; n3 = a3; n4 = a4;
        n5 = a5; n6 = a6; n7 = a7; n8 = a8; n9 = a9;
        magnitude = 1;
        normalized = true;
    }
}

public readonly struct GEJ
{
    public readonly FE x, y, z;
    public readonly bool infinity;

    public GEJ(in FE x, in FE y, in FE z, bool infinity)
    {
        this.x = x;
        this.y = y;
        this.z = z;
        this.infinity = infinity;
    }
}

public class Runtime_122237
{
    [Fact]
    public static void TestEntryPoint()
    {
        var a = new GEJ(
            new FE(1, 2, 3, 4, 5, 6, 7, 8, 9, 10),
            new FE(11, 22, 33, 44, 55, 66, 77, 88, 99, 11),
            new FE(21, 22, 23, 24, 25, 26, 27, 28, 29, 210),
            false);

        uint expected = a.x.n0;

        int firstBad = -1;
        for (int i = 0; i < 5000; i++)
        {
            a = new GEJ(a.x, a.y, a.z, a.infinity);
            if (a.x.n0 != expected)
            {
                firstBad = i;
                break;
            }
        }

        Assert.Equal(-1, firstBad);
    }
}
