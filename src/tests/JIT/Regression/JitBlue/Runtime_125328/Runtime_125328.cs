// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics.X86;
using Xunit;

public class Runtime_125328
{
    public static bool s_7;
    public static uint s_23;

    [Fact]
    public static void TestEntryPoint()
    {
        if (Bmi2.IsSupported)
        {
            var vr6 = M9();
            s_7 = Bmi2.MultiplyNoFlags(vr6, s_23) != 0;
            var vr7 = Bmi2.MultiplyNoFlags(s_23, 0);
            Assert.Equal(0u, vr7);
        }
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static uint M9()
    {
        uint var3 = default(uint);
        for (ulong lvar0 = 18446744073709551515UL; lvar0 < 18446744073709551517UL; lvar0++)
        {
        }

        return var3;
    }
}
