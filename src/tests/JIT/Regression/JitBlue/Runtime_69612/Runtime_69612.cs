// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.CompilerServices;
using System.Threading;
using Xunit;

ref struct NewReference
{
    public IntPtr pointer;
}

static unsafe class Delegates
{
    static Delegates()
    {
        PyLong_FromLongLong = (delegate* unmanaged[Cdecl]<long, NewReference>)0xbadcab;
        z = 100;
    }

    public static delegate* unmanaged[Cdecl]<long, NewReference> PyLong_FromLongLong { get; }
    public static long z;
}

public class Runtime_69612
{
    static NewReference ToPython(object value, Type type)
    {
        TypeCode tc = Type.GetTypeCode(type);
        
        switch (tc)
        {
            case TypeCode.Byte:
                return PyInt_FromInt32((byte)value);
            case TypeCode.Int16:
                return PyInt_FromInt32((short)value);
            case TypeCode.UInt16:
                return PyInt_FromInt32((ushort)value);
            case TypeCode.Int32:
                return PyInt_FromInt32((int)value);
            default:
                return new NewReference();
        }
    }

    static NewReference PyInt_FromInt32(int value) => PyLong_FromLongLong(value);

    unsafe static NewReference PyLong_FromLongLong(long value) => Delegates.PyLong_FromLongLong(value);

    [MethodImpl(MethodImplOptions.NoOptimization)]
    [Fact]
    public static int TestEntryPoint()
    {
        for (int i = 0; i < 100; i++)
        {
            _ =  ToPython(Delegates.z, typeof(long));
            Thread.Sleep(15);
        }

        Thread.Sleep(50);
        _ = ToPython(Delegates.z, typeof(long));
        return 100;

    }
}
