// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//

using System;
using System.Runtime.CompilerServices;


struct S2
{
    public uint F0;
    public ulong F1, F2;
    public S2(uint f0) : this() { F0 = f0; }
}

public class GitHub_19256
{
    static S2 s_one = new S2(1);
    static S2 s_two = new S2(2);
    static uint sum = 0;
    public static int Main()
    {
        M28(s_two, M28(s_one, s_one));
        return sum == 3 ? 100 : -1;
    }

    static ref S2 M28(S2 arg0, S2 arg1)
    {
        sum += arg0.F0;
        System.Console.WriteLine(arg0.F0);
        arg0.F0 = 1234; // this is printed in next invocation
        System.GC.KeepAlive(arg0); // ensure that assignment isn't removed
        return ref s_one;
    }
}


