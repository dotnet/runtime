// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

public class Program
{
    public static ulong[,] s_1;
    public static int Main()
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