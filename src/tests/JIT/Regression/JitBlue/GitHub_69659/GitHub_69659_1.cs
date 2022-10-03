// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//
// In this issue, although we were not removing an unreachable block, we were removing all the code
// inside it and as such should update the liveness information. Since we were not updating the liveness
// information for such scenarios, we were hitting an assert during register allocation.
public class Program
{
    public static ulong[] s_14;
    public static uint s_34;
    public static int Main()
    {
        var vr2 = new ulong[][]{new ulong[]{0}};
        M27(s_34, vr2);
        return 100;
    }

    public static void M27(uint arg4, ulong[][] arg5)
    {
        arg5[0][0] = arg5[0][0];
        for (int var7 = 0; var7 < 1; var7++)
        {
            return;
        }

        try
        {
            s_14 = arg5[0];
        }
        finally
        {
            arg4 = arg4;
        }
    }
}