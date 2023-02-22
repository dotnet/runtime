// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Reflection;
using System.Text;
using Xunit;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

unsafe class StdCallMemberFunctionNative
{
    public struct C
    {
        public struct VtableLayout
        {
            public delegate* unmanaged[Stdcall, MemberFunction]<C*, int, SizeF> getSize;
            public delegate* unmanaged[Stdcall, MemberFunction]<C*, Width> getWidth;
            public delegate* unmanaged[Stdcall, MemberFunction]<C*, IntWrapper> getHeightAsInt;
            public delegate* unmanaged[Stdcall, MemberFunction]<C*, E> getE;
            public delegate* unmanaged[Stdcall, MemberFunction]<C*, CLong> getWidthAsLong;
        }

        public VtableLayout* vtable;
        public E dummy;
        public float width;
        public float height;
    }

    public struct SizeF
    {
        public float width;
        public float height;
    }

    public struct Width
    {
        public float width;
    }

    public struct IntWrapper
    {
        public int i;
    }

    public enum E : uint
    {
        Value = 42
    }

    [DllImport(nameof(StdCallMemberFunctionNative))]
    public static extern C* CreateInstanceOfC(float width, float height);

    [DllImport(nameof(StdCallMemberFunctionNative))]
    public static extern SizeF GetSizeFromManaged(C* c);
    [DllImport(nameof(StdCallMemberFunctionNative))]
    public static extern Width GetWidthFromManaged(C* c);
    [DllImport(nameof(StdCallMemberFunctionNative))]
    public static extern IntWrapper GetHeightAsIntFromManaged(C* c);
    [DllImport(nameof(StdCallMemberFunctionNative))]
    public static extern E GetEFromManaged(C* c);
    [DllImport(nameof(StdCallMemberFunctionNative))]
    public static extern CLong GetWidthAsLongFromManaged(C* c);
}

public unsafe class StdCallMemberFunctionTest
{
    [Fact]
    public static int TestEntryPoint()
    {
        try
        {
            float width = 1.0f;
            float height = 2.0f;
            StdCallMemberFunctionNative.C* instance = StdCallMemberFunctionNative.CreateInstanceOfC(width, height);
            Test8ByteHFA(instance);
            Test4ByteHFA(instance);
            Test4ByteNonHFA(instance);
            TestEnum(instance);
            TestCLong(instance);
            Test8ByteHFAUnmanagedCallersOnly();
            Test4ByteHFAUnmanagedCallersOnly();
            Test4ByteNonHFAUnmanagedCallersOnly();
            TestEnumUnmanagedCallersOnly();
            TestCLongUnmanagedCallersOnly();
        }
        catch (System.Exception ex)
        {
            Console.WriteLine(ex);
            return 101;
        }
        return 100;
    }

    private static void Test8ByteHFA(StdCallMemberFunctionNative.C* instance)
    {
        StdCallMemberFunctionNative.SizeF result = instance->vtable->getSize(instance, 1234);

        Assert.Equal(instance->width, result.width);
        Assert.Equal(instance->height, result.height);
    }

    private static void Test4ByteHFA(StdCallMemberFunctionNative.C* instance)
    {
        StdCallMemberFunctionNative.Width result = instance->vtable->getWidth(instance);

        Assert.Equal(instance->width, result.width);
    }

    private static void Test4ByteNonHFA(StdCallMemberFunctionNative.C* instance)
    {
        StdCallMemberFunctionNative.IntWrapper result = instance->vtable->getHeightAsInt(instance);

        Assert.Equal((int)instance->height, result.i);
    }

    private static void TestEnum(StdCallMemberFunctionNative.C* instance)
    {
        StdCallMemberFunctionNative.E result = instance->vtable->getE(instance);

        Assert.Equal(instance->dummy, result);
    }

    private static void TestCLong(StdCallMemberFunctionNative.C* instance)
    {
        CLong result = instance->vtable->getWidthAsLong(instance);

        Assert.Equal((nint)instance->width, result.Value);
    }
    private static void Test8ByteHFAUnmanagedCallersOnly()
    {
        StdCallMemberFunctionNative.C c = CreateCWithUnmanagedCallersOnlyVTable(2.0f, 3.0f);
        StdCallMemberFunctionNative.SizeF result = StdCallMemberFunctionNative.GetSizeFromManaged(&c);

        Assert.Equal(c.width, result.width);
        Assert.Equal(c.height, result.height);
    }

    private static void Test4ByteHFAUnmanagedCallersOnly()
    {
        StdCallMemberFunctionNative.C c = CreateCWithUnmanagedCallersOnlyVTable(2.0f, 3.0f);
        StdCallMemberFunctionNative.Width result = StdCallMemberFunctionNative.GetWidthFromManaged(&c);

        Assert.Equal(c.width, result.width);
    }

    private static void Test4ByteNonHFAUnmanagedCallersOnly()
    {
        StdCallMemberFunctionNative.C c = CreateCWithUnmanagedCallersOnlyVTable(2.0f, 3.0f);
        StdCallMemberFunctionNative.IntWrapper result = StdCallMemberFunctionNative.GetHeightAsIntFromManaged(&c);

        Assert.Equal((int)c.height, result.i);
    }

    private static void TestEnumUnmanagedCallersOnly()
    {
        StdCallMemberFunctionNative.C c = CreateCWithUnmanagedCallersOnlyVTable(2.0f, 3.0f);
        StdCallMemberFunctionNative.E result = StdCallMemberFunctionNative.GetEFromManaged(&c);

        Assert.Equal(c.dummy, result);
    }

    private static void TestCLongUnmanagedCallersOnly()
    {
        StdCallMemberFunctionNative.C c = CreateCWithUnmanagedCallersOnlyVTable(2.0f, 3.0f);
        CLong result = StdCallMemberFunctionNative.GetWidthAsLongFromManaged(&c);

        Assert.Equal((nint)c.width, result.Value);
    }

    private static StdCallMemberFunctionNative.C CreateCWithUnmanagedCallersOnlyVTable(float width, float height)
    {
        return new StdCallMemberFunctionNative.C
        {
            vtable = UnmanagedCallersOnlyVtable,
            dummy = StdCallMemberFunctionNative.E.Value,
            width = width,
            height = height
        };
    }

    private static StdCallMemberFunctionNative.C.VtableLayout* unmanagedCallersOnlyVtable;

    private static StdCallMemberFunctionNative.C.VtableLayout* UnmanagedCallersOnlyVtable
    {
        get
        {
            if (unmanagedCallersOnlyVtable == null)
            {
                unmanagedCallersOnlyVtable = (StdCallMemberFunctionNative.C.VtableLayout*)Marshal.AllocHGlobal(sizeof(StdCallMemberFunctionNative.C.VtableLayout));
                unmanagedCallersOnlyVtable->getSize = &GetSize;
                unmanagedCallersOnlyVtable->getWidth = &GetWidth;
                unmanagedCallersOnlyVtable->getHeightAsInt = &GetHeightAsInt;
                unmanagedCallersOnlyVtable->getE = &GetE;
                unmanagedCallersOnlyVtable->getWidthAsLong = &GetWidthAsLong;
            }
            return unmanagedCallersOnlyVtable;
        }
    }

    [UnmanagedCallersOnly(CallConvs = new [] {typeof(CallConvStdcall), typeof(CallConvMemberFunction)})]
    private static StdCallMemberFunctionNative.SizeF GetSize(StdCallMemberFunctionNative.C* c, int unused)
    {
        return new StdCallMemberFunctionNative.SizeF
        {
            width = c->width,
            height = c->height
        };
    }

    [UnmanagedCallersOnly(CallConvs = new [] {typeof(CallConvStdcall), typeof(CallConvMemberFunction)})]
    private static StdCallMemberFunctionNative.Width GetWidth(StdCallMemberFunctionNative.C* c)
    {
        return new StdCallMemberFunctionNative.Width
        {
            width = c->width
        };
    }

    [UnmanagedCallersOnly(CallConvs = new [] {typeof(CallConvStdcall), typeof(CallConvMemberFunction)})]
    private static StdCallMemberFunctionNative.IntWrapper GetHeightAsInt(StdCallMemberFunctionNative.C* c)
    {
        return new StdCallMemberFunctionNative.IntWrapper
        {
            i = (int)c->height
        };
    }

    [UnmanagedCallersOnly(CallConvs = new [] {typeof(CallConvStdcall), typeof(CallConvMemberFunction)})]
    private static StdCallMemberFunctionNative.E GetE(StdCallMemberFunctionNative.C* c)
    {
        return c->dummy;
    }

    [UnmanagedCallersOnly(CallConvs = new [] {typeof(CallConvStdcall), typeof(CallConvMemberFunction)})]
    private static CLong GetWidthAsLong(StdCallMemberFunctionNative.C* c)
    {
        return new CLong((nint)c->width);
    }
}
