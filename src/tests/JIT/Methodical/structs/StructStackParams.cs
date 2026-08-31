// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//

// This tests passing structs that are less than 64-bits in size, but that
// don't match the size of a primitive type, and passes them as the 6th
// parameter so that they are likely to wind up on the stack for ABIs that
// pass structs by value.

using System;
using System.Runtime.CompilerServices;
using Xunit;

// Struct that's greater than 32-bits, but not a multiple of 32-bits.
public struct MyStruct1
{
    public byte f1;
    public byte f2;
    public short f3;
    public short f4;
}

// Struct that's less than 32-bits, but not the same size as any primitive type.
public struct MyStruct2
{
    public byte f1;
    public byte f2;
    public byte f3;
}

// Struct that's less than 64-bits, but not the same size as any primitive type.
public struct MyStruct3
{
    public short f1;
    public short f2;
    public short f3;
}

// Struct that's greater than 64-bits, but not a multiple of 64-bits.
public struct MyStruct4
{
    public int f1;
    public int f2;
    public short f3;
}

public class MyProgram
{
    const int Pass = 100;
    const int Fail = -1;

    [MethodImplAttribute(MethodImplOptions.NoInlining)]
    public static byte GetByte(byte i)
    {
        return i;
    }

    [MethodImplAttribute(MethodImplOptions.NoInlining)]
    public static short GetShort(short i)
    {
        return i;
    }

    [MethodImplAttribute(MethodImplOptions.NoInlining)]
    public static int GetInt(int i)
    {
        return i;
    }

    [MethodImplAttribute(MethodImplOptions.NoInlining)]
    public static int Check1(int w, int i1, int i2, int i3, int i4, int i5, int i6, int i7, MyStruct1 s1)
    {
        if ((w != 1) || (s1.f1 != i1) || (s1.f2 != i2) || (s1.f3 != i3) || (s1.f4 != i4))
        {
            Console.WriteLine("Check1: FAIL");
            return Fail;
        }
        Console.WriteLine("Check1: PASS");
        return Pass;
    }

    public static int TestStruct1()
    {
        MyStruct1 s1;
        s1.f1 = GetByte(1); s1.f2 = GetByte(2); s1.f3 = GetShort(3); s1.f4 = GetShort(4);
        int x = (s1.f1 * s1.f2 * s1.f3 * s1.f4);
        int y = (s1.f1 - s1.f2) * (s1.f3 - s1.f4);
        int z = (s1.f1 + s1.f2) * (s1.f3 + s1.f4);
        int w = (x + y) / z;

        return Check1(w, 1, 2, 3, 4, 5, 6, 7, s1);
    }

    [MethodImplAttribute(MethodImplOptions.NoInlining)]
    public static int Check2(int w, int i1, int i2, int i3, int i4, int i5, int i6, int i7, MyStruct2 s2)
    {
        if ((w != 2) || (s2.f1 != i1) || (s2.f2 != i2) || (s2.f3 != i3) || (i4 != 4))
        {
            Console.WriteLine("Check2: FAIL");
            return Fail;
        }
        Console.WriteLine("Check2: PASS");
        return Pass;
    }

    public static int TestStruct2()
    {
        MyStruct2 s2;
        s2.f1 = GetByte(1); s2.f2 = GetByte(2); s2.f3 = GetByte(3);
        int x = s2.f1 * s2.f2 * s2.f3;
        int y = (s2.f1 + s2.f2) * s2.f3;
        int z = s2.f1 + s2.f2 + s2.f3;
        int w = (x + y) / z;

        return Check2(w, 1, 2, 3, 4, 5, 6, 7, s2);
    }

    [MethodImplAttribute(MethodImplOptions.NoInlining)]
    public static int Check3(int w, int i1, int i2, int i3, int i4, int i5, int i6, int i7, MyStruct3 s3)
    {
        if ((w != 2) || (s3.f1 != i1) || (s3.f2 != i2) || (s3.f3 != i3) || (i4 != 4))
        {
            Console.WriteLine("Check3: FAIL");
            return Fail;
        }
        Console.WriteLine("Check3: PASS");
        return Pass;
    }

    public static int TestStruct3()
    {
        MyStruct3 s3;
        s3.f1 = GetByte(1); s3.f2 = GetByte(2); s3.f3 = GetByte(3);
        int x = s3.f1 * s3.f2 * s3.f3;
        int y = (s3.f1 + s3.f2) * s3.f3;
        int z = s3.f1 + s3.f2 + s3.f3;
        int w = (x + y) / z;

        return Check3(w, 1, 2, 3, 4, 5, 6, 7, s3);
    }

    [MethodImplAttribute(MethodImplOptions.NoInlining)]
    public static int Check4(int w, int i1, int i2, int i3, int i4, int i5, int i6, int i7, MyStruct4 s4)
    {
        if ((w != 2) || (s4.f1 != i1) || (s4.f2 != i2) || (s4.f3 != i3) || (i4 != 4))
        {
            Console.WriteLine("Check4: FAIL");
            return Fail;
        }
        Console.WriteLine("Check4: PASS");
        return Pass;
    }

    public static int TestStruct4()
    {
        MyStruct4 s4;
        s4.f1 = GetInt(1); s4.f2 = GetInt(2); s4.f3 = GetShort(3);
        int x = s4.f1 * s4.f2 * s4.f3;
        int y = (s4.f1 + s4.f2) * s4.f3;
        int z = s4.f1 + s4.f2 + s4.f3;
        int w = (x + y) / z;

        return Check4(w, 1, 2, 3, 4, 5, 6, 7, s4);
    }

    [Fact]
    public static int TestEntryPoint()
    {
        int retVal = Pass;
        if (TestStruct1() != Pass)
        {
            retVal = Fail;
        }
        if (TestStruct2() != Pass)
        {
            retVal = Fail;
        }
        if (TestStruct3() != Pass)
        {
            retVal = Fail;
        }
        if (TestStruct4() != Pass)
        {
            retVal = Fail;
        }
        return retVal;
    }
}
