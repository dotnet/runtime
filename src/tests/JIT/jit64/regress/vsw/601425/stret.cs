// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.InteropServices;
using Xunit;

//force struct size=22 otherwise with padding struct will be 24 and never hit bug
[StructLayout(LayoutKind.Sequential, Size = 22)]
public struct VType
{
    public long v1;
    public long v2;
    public int v3;
    public short v4;
}


public class StructReturn
{
    public static VType VTypeInc(VType vt)
    {
        //add 1 to each struct member
        VType vtx;
        vtx.v1 = vt.v1 + 1;
        vtx.v2 = vt.v2 + 1;
        vtx.v3 = vt.v3 + 1;
        vtx.v4 = (short)(vt.v4 + 1);
        //do some nonsense to exercise reg assignment
        long loc1;
        long loc2;
        long loc3;
        long loc4;
        long loc5;
        long loc6;
        long loc7;
        long loc8;
        loc1 = vt.v1 + vt.v2;
        loc2 = loc1 + vt.v2;
        loc3 = loc2 + vt.v2;
        loc4 = loc3 + vt.v2;
        loc5 = loc4 + vt.v2;
        loc6 = loc5 + vt.v2;
        loc7 = loc6 + vt.v2;
        loc8 = loc7 + loc1 + vt.v2;
        //nonsense complete
        Console.WriteLine("should return v2={0:D}", vtx.v2);
        return vtx;
    }

    public static VType InitVType(long v1, long v2, int v3, short v4)
    {
        VType vt;
        vt.v1 = v1;
        vt.v2 = v2;
        vt.v3 = v3;
        vt.v4 = v4;
        return vt;
    }

    [Fact]
    public static int TestEntryPoint()
    {
        long v1 = 4444;
        long v2 = 2222;
        int v3 = 1111;
        short v4 = 999;

        VType vt = InitVType(v1, v2, v3, v4);
        Console.WriteLine("init returned v1={0:D} v2={1:D} v3={2:D} 4={3:D}", vt.v1, vt.v2, vt.v3, vt.v4);
        VType vtinc = VTypeInc(vt);
        Console.WriteLine("inc returned v1={0:D} v2={1:D} v3={2:D} v4={3:D}", vtinc.v1, vtinc.v2, vtinc.v3, vtinc.v4);
        if (vt.v2 + 1 != vtinc.v2)
        {
            Console.WriteLine("Fail");
            return 666;
        }
        Console.WriteLine("Pass");
        return 100;
    }
}
