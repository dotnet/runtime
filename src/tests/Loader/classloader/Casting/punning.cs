// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.InteropServices;

using Xunit;

public class punning
{
    [Fact]
    public static void Via_GetFunctionPointer()
    {
        Console.WriteLine($"Running {nameof(Via_GetFunctionPointer)}...");

        IntPtr fptr = typeof(A.Class).GetMethod("GetField").MethodHandle.GetFunctionPointer();
        Assert.NotEqual(IntPtr.Zero, fptr);
        var b = new Caller.Struct()
        {
            Field = 0x55
        };
        int fieldValue = Caller.Class.CallGetField(b, fptr, null);
        Assert.Equal(b.Field, fieldValue);
    }

    [Fact]
    public static void Via_GetFunctionPointer_Generics()
    {
        Console.WriteLine($"Running {nameof(Via_GetFunctionPointer_Generics)}...");

        IntPtr fptr = typeof(A.Class).GetMethod("GetFieldGeneric").MakeGenericMethod(typeof(object)).MethodHandle.GetFunctionPointer();
        Assert.NotEqual(IntPtr.Zero, fptr);
        var b = new Caller.Struct<object>()
        {
            Field = 0x55
        };
        int fieldValue = Caller.Class.CallGetField(b, fptr, null);
        Assert.Equal(b.Field, fieldValue);
    }

    [Fact]
    public static void Via_Ldftn()
    {
        Console.WriteLine($"Running {nameof(Via_Ldftn)}...");

        IntPtr fptr = B.Class.GetFunctionPointer();
        Assert.NotEqual(IntPtr.Zero, fptr);
        var b = new Caller.Struct()
        {
            Field = 0x55
        };
        int fieldValue = Caller.Class.CallGetField(b, fptr, null);
        Assert.Equal(b.Field, fieldValue);
    }

    [Fact]
    public static void Via_Ldftn_Generics()
    {
        Console.WriteLine($"Running {nameof(Via_Ldftn_Generics)}...");

        IntPtr fptr = B.Class.GetFunctionPointerGeneric();
        Assert.NotEqual(IntPtr.Zero, fptr);
        var b = new Caller.Struct<object>()
        {
            Field = 0x55
        };
        int fieldValue = Caller.Class.CallGetField(b, fptr, null);
        Assert.Equal(b.Field, fieldValue);
    }

    [Fact]
    public static void Via_Ldftn_Generics_Virtual()
    {
        Console.WriteLine($"Running {nameof(Via_Ldftn_Generics_Virtual)}...");

        object inst = new B.Derived();
        IntPtr fptr = B.Class.GetFunctionPointerGeneric(inst);
        Assert.NotEqual(IntPtr.Zero, fptr);
        var b = new Caller.Struct<object>()
        {
            Field = 0x55
        };
        int fieldValue = Caller.Class.CallGetField(b, fptr, inst);
        Assert.Equal(b.Field, fieldValue);
    }

    [Fact]
    public static void Via_Ldftn_Generics_EarlyLoad()
    {
        Console.WriteLine($"Running {nameof(Via_Ldftn_Generics_EarlyLoad)}...");

        IntPtr fptr = B.Class.GetFunctionPointer<object>();
        Assert.NotEqual(IntPtr.Zero, fptr);
        var b = new Caller.Struct<object>()
        {
            Field = 0x55
        };
        int fieldValue = Caller.Class.CallGetField(b, fptr, null);
        Assert.Equal(b.Field, fieldValue);
    }

    [Fact]
    public static void Via_Ldftn_Generics_Virtual_EarlyLoad()
    {
        Console.WriteLine($"Running {nameof(Via_Ldftn_Generics_Virtual_EarlyLoad)}...");

        object inst = new B.Derived();
        IntPtr fptr = B.Class.GetFunctionPointer<object>(inst);
        Assert.NotEqual(IntPtr.Zero, fptr);
        var b = new Caller.Struct<object>()
        {
            Field = 0x55
        };
        int fieldValue = Caller.Class.CallGetField(b, fptr, inst);
        Assert.Equal(b.Field, fieldValue);
    }

    [Fact]
    public static void Via_Ldvirtftn()
    {
        Console.WriteLine($"Running {nameof(Via_Ldvirtftn)}...");

        object inst = new C.Derived();
        IntPtr fptr = C.Class.GetFunctionPointer(inst);
        Assert.NotEqual(IntPtr.Zero, fptr);
        var b = new Caller.Struct()
        {
            Field = 0x55
        };
        int fieldValue = Caller.Class.CallGetField(b, fptr, inst);
        Assert.Equal(b.Field, fieldValue);
    }

    [Fact]
    public static void Via_Ldvirtftn_Generics()
    {
        Console.WriteLine($"Running {nameof(Via_Ldvirtftn_Generics)}...");

        object inst = new C.Derived();
        IntPtr fptr = C.Class.GetFunctionPointerGeneric(inst);
        Assert.NotEqual(IntPtr.Zero, fptr);
        var b = new Caller.Struct<object>()
        {
            Field = 0x55
        };
        int fieldValue = Caller.Class.CallGetField(b, fptr, inst);
        Assert.Equal(b.Field, fieldValue);
    }

    [Fact]
    public static void Via_Ldvirtftn_Generics_EarlyLoad()
    {
        Console.WriteLine($"Running {nameof(Via_Ldvirtftn_Generics_EarlyLoad)}...");

        object inst = new C.Derived();
        IntPtr fptr = C.Class.GetFunctionPointer<object>(inst);
        Assert.NotEqual(IntPtr.Zero, fptr);
        var b = new Caller.Struct<object>()
        {
            Field = 0x55
        };
        int fieldValue = Caller.Class.CallGetField(b, fptr, inst);
        Assert.Equal(b.Field, fieldValue);
    }
}
