// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//

/*Incorrect code generated for assignment in multidimmensional arrays of large (>255 bytes) valuetypes by the x86 JIT.
(The size of the array element is truncated to 1 byte while being stored into internal JIT datastructures.)
The fix is: Disable array get/set optimizations for multidimmensional arrays of large (>255 bytes) valuetypes.*/


using System;
using System.Runtime.CompilerServices;
using Xunit;

struct BigStruct
{
    public int x1;
    public int x2;
    public int x3;
    public int x4;
    public int x5;
    public int x6;
    public int x7;
    public int x8;
    public int x9;
    public int x10;
    public int x11;
    public int x12;
    public int x13;
    public int x14;
    public int x15;
    public int x16;
    public int x17;
    public int x18;
    public int x19;
    public int x20;
    public int x21;
    public int x22;
    public int x23;
    public int x24;
    public int x25;
    public int x26;
    public int x27;
    public int x28;
    public int x29;
    public int x30;
    public int x31;
    public int x32;
    public int x33;
    public int x34;
    public int x35;
    public int x36;
    public int x37;
    public int x38;
    public int x39;
    public int x40;
    public int x41;
    public int x42;
    public int x43;
    public int x44;
    public int x45;
    public int x46;
    public int x47;
    public int x48;
    public int x49;
    public int x50;
    public int x51;
    public int x52;
    public int x53;
    public int x54;
    public int x55;
    public int x56;
    public int x57;
    public int x58;
    public int x59;
    public int x60;
    public int x61;
    public int x62;
    public int x63;
    public int x64;
    public int x65;
};

public class My
{
    [Fact]
    public static int TestEntryPoint()
    {
        BigStruct[,] a = new BigStruct[1, 3];

        BigStruct v = new BigStruct();
        v.x65 = 5;

        // Use reflection to set the array element. This will guarantee that we are not
        // hitting the JIT bug while setting the array element.
        a.SetValue(v, 0, 2);

        int x = a[0, 2].x65;
        if (x == 5)
        {
            Console.WriteLine("PASSED");
            return 100;
        }
        else
        {
            Console.WriteLine("FAILED");
            return 101;
        }


    }
}
