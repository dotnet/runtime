// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// Regression test for https://github.com/dotnet/runtime/issues/131716
//
// A switch with exactly 64 cases and two distinct targets is lowered to a bit test.
// On xarch the JIT inverts the bit table when its upper 32 bits are all set, so that the
// table still fits in a 32 bit immediate. The inversion has to swap the two successor
// edges as well; only swapping the target blocks left the bit test branching to the
// wrong target (and corrupted the block ref counts).

using System.Runtime.CompilerServices;
using Xunit;

namespace Runtime_131716;

public class Runtime_131716
{
    // 64 cases, two targets. Case 0 and cases 4..63 share a target, so the bit table is
    // 0xFFFFFFFFFFFFFFF1 and gets inverted to 0xE.
    [MethodImpl(MethodImplOptions.NoInlining)]
    private static int Inverted(uint x)
    {
        switch (x)
        {
            case 1:
            case 2:
            case 3:
                return 111;
            case 0:
            case 4:
            case 5:
            case 6:
            case 7:
            case 8:
            case 9:
            case 10:
            case 11:
            case 12:
            case 13:
            case 14:
            case 15:
            case 16:
            case 17:
            case 18:
            case 19:
            case 20:
            case 21:
            case 22:
            case 23:
            case 24:
            case 25:
            case 26:
            case 27:
            case 28:
            case 29:
            case 30:
            case 31:
            case 32:
            case 33:
            case 34:
            case 35:
            case 36:
            case 37:
            case 38:
            case 39:
            case 40:
            case 41:
            case 42:
            case 43:
            case 44:
            case 45:
            case 46:
            case 47:
            case 48:
            case 49:
            case 50:
            case 51:
            case 52:
            case 53:
            case 54:
            case 55:
            case 56:
            case 57:
            case 58:
            case 59:
            case 60:
            case 61:
            case 62:
            case 63:
                return 222;
            default:
                return 333;
        }
    }

    // Same shape, but cases 0..3 share a target so the bit table is 0xF and is not inverted.
    [MethodImpl(MethodImplOptions.NoInlining)]
    private static int NotInverted(uint x)
    {
        switch (x)
        {
            case 0:
            case 1:
            case 2:
            case 3:
                return 111;
            case 4:
            case 5:
            case 6:
            case 7:
            case 8:
            case 9:
            case 10:
            case 11:
            case 12:
            case 13:
            case 14:
            case 15:
            case 16:
            case 17:
            case 18:
            case 19:
            case 20:
            case 21:
            case 22:
            case 23:
            case 24:
            case 25:
            case 26:
            case 27:
            case 28:
            case 29:
            case 30:
            case 31:
            case 32:
            case 33:
            case 34:
            case 35:
            case 36:
            case 37:
            case 38:
            case 39:
            case 40:
            case 41:
            case 42:
            case 43:
            case 44:
            case 45:
            case 46:
            case 47:
            case 48:
            case 49:
            case 50:
            case 51:
            case 52:
            case 53:
            case 54:
            case 55:
            case 56:
            case 57:
            case 58:
            case 59:
            case 60:
            case 61:
            case 62:
            case 63:
                return 222;
            default:
                return 333;
        }
    }

    [Fact]
    public static void TestEntryPoint()
    {
        for (uint i = 0; i < 128; i++)
        {
            Assert.Equal(i > 63 ? 333 : (i is >= 1 and <= 3) ? 111 : 222, Inverted(i));
            Assert.Equal(i > 63 ? 333 : (i <= 3) ? 111 : 222, NotInverted(i));
        }

        Assert.Equal(333, Inverted(uint.MaxValue));
        Assert.Equal(333, NotInverted(uint.MaxValue));
    }
}
