// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Xunit;
public class Program
{
    public static ulong[,] s_1;
    [Fact]
    public static int TestEntryPoint()
    {
        // This should not assert.
        try
        {
            ushort vr10 = default(ushort);
            bool vr11 = 0 < ((s_1[0, 0] * (uint)(0 / vr10)) % 1);
        }
        catch {}

        return 100;
    }
}