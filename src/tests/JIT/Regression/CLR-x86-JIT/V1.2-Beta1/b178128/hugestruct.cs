// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//

using System;
using Xunit;


struct Big10DW
{
#pragma warning disable 0414
    public Int64 i1;
    public Int64 i2;
    public Int64 i3;
    public Int64 i4;
    public Int64 i5;
    public void Zero()
    {
        i1 = 0;
        i2 = 0;
        i3 = 0;
        i4 = 0;
        i5 = 0;
    }
#pragma warning restore 0414
}

struct Big100DW
{
    public Big10DW b1;
    public Big10DW b2;
    public Big10DW b3;
    public Big10DW b4;
    public Big10DW b5;
    public Big10DW b6;
    public Big10DW b7;
    public Big10DW b8;
    public Big10DW b9;
    public Big10DW b10;
    public void Zero()
    {
        b1.Zero();
        b2.Zero();
        b3.Zero();
        b4.Zero();
        b5.Zero();
        b6.Zero();
        b7.Zero();
        b8.Zero();
        b9.Zero();
        b10.Zero();
    }
}

struct Big1000DW
{
    public Big100DW b1;
    public Big100DW b2;
    public Big100DW b3;
    public Big100DW b4;
    public Big100DW b5;
    public Big100DW b6;
    public Big100DW b7;
    public Big100DW b8;
    public Big100DW b9;
    public Big100DW b10;
    public void Zero()
    {
        b1.Zero();
        b2.Zero();
        b3.Zero();
        b4.Zero();
        b5.Zero();
        b6.Zero();
        b7.Zero();
        b8.Zero();
        b9.Zero();
        b10.Zero();
    }
}

struct Big10000DW
{
    public Big1000DW b1;
    public Big1000DW b2;
    public Big1000DW b3;
    public Big1000DW b4;
    public Big1000DW b5;
    public Big1000DW b6;
    public Big1000DW b7;
    public Big1000DW b8;
    public Big1000DW b9;
    public Big1000DW b10;
    public void Zero()
    {
        b1.Zero();
        b2.Zero();
        b3.Zero();
        b4.Zero();
        b5.Zero();
        b6.Zero();
        b7.Zero();
        b8.Zero();
        b9.Zero();
        b10.Zero();
    }
}

struct Big100000DW
{
    public Big10000DW b1;
    public Big10000DW b2;
    public Big10000DW b3;
    public Big10000DW b4;
    public Big10000DW b5;
    public Big10000DW b6;
    public Big10000DW b7;
    public Big10000DW b8;
    public Big10000DW b9;
    public Big10000DW b10;
    public void Zero()
    {
        b1.Zero();
        b2.Zero();
        b3.Zero();
        b4.Zero();
        b5.Zero();
        b6.Zero();
        b7.Zero();
        b8.Zero();
        b9.Zero();
        b10.Zero();
    }
}


public class Test_hugestruct
{
    [Fact]
    public static int TestEntryPoint()
    {
        Big100000DW b = new Big100000DW();
        b.b10.b10.b10.b10.i5 = 0;
        GC.Collect();
        return 100;
    }
}
