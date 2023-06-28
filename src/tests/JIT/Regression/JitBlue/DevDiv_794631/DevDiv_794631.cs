// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//

using System;
using Xunit;

public class Repro
{
    [Fact]
    public static int TestEntryPoint()
    {
        //This testcase ensures that we correctly generate one ReadUInt16() call
        //instead of two due to a bug in fgmorph which transformed a call result 
        //as an index of an array incorrectly resulting in an unexpected index

        U1[] u1 = new U1[1];
        U2[] u2 = new U2[2];
        u2[1] = new U2();
        U1 obj = u1[ReadUInt16()];

        if (obj == null)
        {
            Console.WriteLine("PASS!");
            return 100;
        }
        else
        {
            Console.WriteLine("FAIL!");
            Console.WriteLine("obj is not null.");
            return 101;
        }
    }

    static byte[] buf = new byte[] { 0, 0, 0, 6 };
    static int pos;

    static ushort ReadUInt16()
    {
        ushort s = (ushort)((buf[pos] << 8) + buf[pos + 1]);
        pos += 2;
        return s;
    }

    class U1 { }
    class U2 { }
}
