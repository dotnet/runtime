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
    public static int Main()
    {
        var value = new SmallString("foobar");
        Execute(value);

        var method = new DynamicMethod("test", typeof(void), new[] { typeof(SmallString) }, typeof(Program), true);
        var il = method.GetILGenerator();
        il.Emit(OpCodes.Ldarg_0);
        il.EmitCall(OpCodes.Call, typeof(Program).GetMethod("Execute")!, null);
        il.Emit(OpCodes.Pop);
        il.Emit(OpCodes.Ret);

        var action = (Action<SmallString>)method.CreateDelegate(typeof(Action<SmallString>));
        action.Invoke(value);

        return result;
    }

    public static object Execute(SmallString foo)
    {
        byte value = foo.Dump();
        if (value == 111)
        {
            result += 50;
        }
        return new StringBuilder();
    }
}
