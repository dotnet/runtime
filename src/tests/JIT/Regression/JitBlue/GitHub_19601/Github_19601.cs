// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Xunit;
public class GitHub_19601
{
    static ushort s_2;
    static short[] s_5 = new short[]{1};
    static ulong s_8;
    [Fact]
    public static int TestEntryPoint()
    {
        var vr2 = s_5[0];
        M9();
        return 100;
    }

	// The JIT was emitting a 4-byte immediate for the shift in M9, but this form is invalid.
	// This lead to NullReferenceException or AccessViolationException when the last part of
	// the immediate was treated as instructions.
    static void M9()
    {
        s_8 <<= (0 & s_2) + 186;
    }
}

