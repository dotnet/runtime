// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Xunit;
public class Runtime_81356
{
    public static byte[] s_130;
    [Fact]
    public static int TestEntryPoint()
    {
        try
        {
            ulong vr5 = default(ulong);
            byte vr4 = (byte)(((byte)vr5 & 0) * s_130[0]);
        }
        catch { }
        return 100;
    }
}
