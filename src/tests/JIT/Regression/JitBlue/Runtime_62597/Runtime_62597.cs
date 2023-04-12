// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//
// Note: In below test case, we were not honoring the fact that the explicit struct size
//       of struct is 32 bytes while the only 2 fields it has is just 2 bytes. In such case,
//       we would pass partial struct value.
using System;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using Xunit;

[StructLayout(LayoutKind.Explicit, Size = 32)]
public readonly unsafe struct SmallString
{
    [FieldOffset(0)] private readonly byte _length;
    [FieldOffset(1)] private readonly byte _firstByte;

    public SmallString(string value)
    {
        fixed (char* srcPtr = value)
        fixed (byte* destPtr = &_firstByte)
        {
            Encoding.ASCII.GetBytes(srcPtr, value.Length, destPtr, value.Length);
        }

        _length = (byte)value.Length;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public byte Dump()
    {
        fixed (byte* ptr = &_firstByte)
        {
            byte* next = ptr + 1;
            return *next;
        }
    }
}

public static class Program
{
    static int result = 0;
    [Fact]
    public static int TestEntryPoint()
    {
        var value = new SmallString("foobar");

        TheTest(value);

        return result;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    internal static void TheTest(SmallString foo)
    {
        Execute(foo);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static object Execute(SmallString foo)
    {
        byte value = foo.Dump();
        // 111 corresponds to the ASCII code of 2nd characted of string "foobar" i.e. ASCII value of 'o'.
        if (value == 111)
        {
            result = 100;
        }
        return new StringBuilder();
    }
}
