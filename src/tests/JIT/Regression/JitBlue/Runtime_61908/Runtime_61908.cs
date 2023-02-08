// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Xunit;
public class Runtime_61908
{
    public static bool s_3;
    [Fact]
    public static int TestEntryPoint()
    {
        var vr6 = M3(s_3);
        if (M3(vr6))
        {
            return -1;
        }

        return 100;
    }

    public static bool M3(bool arg0)
    {
        arg0 = !arg0;
        return arg0;
    }
}
