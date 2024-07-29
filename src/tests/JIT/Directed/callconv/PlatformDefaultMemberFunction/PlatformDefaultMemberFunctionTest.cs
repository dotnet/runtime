// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Reflection;
using System.Text;
using Xunit;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

unsafe class PlatformDefaultMemberFunctionNative
{
    public struct C
    {
        public struct VtableLayout
        {
            public delegate* unmanaged[MemberFunction]<C*, int, SizeF> getSize;
            public delegate* unmanaged[MemberFunction]<C*, Width> getWidth;
            public delegate* unmanaged[MemberFunction]<C*, IntWrapper> getHeightAsInt;
            public delegate* unmanaged[MemberFunction]<C*, E> getE;
            public delegate* unmanaged[MemberFunction]<C*, CLong> getWidthAsLong;
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

    [DllImport(nameof(PlatformDefaultMemberFunctionNative))]
    public static extern C* CreateInstanceOfC(float width, float height);

    [DllImport(nameof(PlatformDefaultMemberFunctionNative))]
    public static extern SizeF GetSizeFromManaged(C* c);
    [DllImport(nameof(PlatformDefaultMemberFunctionNative))]
    public static extern Width GetWidthFromManaged(C* c);
    [DllImport(nameof(PlatformDefaultMemberFunctionNative))]
    public static extern IntWrapper GetHeightAsIntFromManaged(C* c);
    [DllImport(nameof(PlatformDefaultMemberFunctionNative))]
    public static extern E GetEFromManaged(C* c);
    [DllImport(nameof(PlatformDefaultMemberFunctionNative))]
    public static extern CLong GetWidthAsLongFromManaged(C* c);
}

public unsafe class PlatformDefaultMemberFunctionTest
{
    [Fact]
    public static int TestEntryPoint()
    {
        try
        {
            float width = 1.0f;
            float height = 2.0f;
            PlatformDefaultMemberFunctionNative.C* instance = PlatformDefaultMemberFunctionNative.CreateInstanceOfC(width, height);
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

    private static void Test8ByteHFA(PlatformDefaultMemberFunctionNative.C* instance)
    {
        PlatformDefaultMemberFunctionNative.SizeF result = instance->vtable->getSize(instance, 1234);

        Assert.Equal(instance->width, result.width);
        Assert.Equal(instance->height, result.height);
    }

    private static void Test4ByteHFA(PlatformDefaultMemberFunctionNative.C* instance)
    {
        PlatformDefaultMemberFunctionNative.Width result = instance->vtable->getWidth(instance);

        Assert.Equal(instance->width, result.width);
    }

    private static void Test4ByteNonHFA(PlatformDefaultMemberFunctionNative.C* instance)
    {
        PlatformDefaultMemberFunctionNative.IntWrapper result = instance->vtable->getHeightAsInt(instance);

        Assert.Equal((int)instance->height, result.i);
    }

    private static void TestEnum(PlatformDefaultMemberFunctionNative.C* instance)
    {
        PlatformDefaultMemberFunctionNative.E result = instance->vtable->getE(instance);

        Assert.Equal(instance->dummy, result);
    }

    private static void TestCLong(PlatformDefaultMemberFunctionNative.C* instance)
    {
        CLong result = instance->vtable->getWidthAsLong(instance);

        Assert.Equal((nint)instance->width, result.Value);
    }

    private static void Test8ByteHFAUnmanagedCallersOnly()
    {
        PlatformDefaultMemberFunctionNative.C c = CreateCWithUnmanagedCallersOnlyVTable(2.0f, 3.0f);
        PlatformDefaultMemberFunctionNative.SizeF result = PlatformDefaultMemberFunctionNative.GetSizeFromManaged(&c);

        Assert.Equal(c.width, result.width);
        Assert.Equal(c.height, result.height);
    }

    private static void Test4ByteHFAUnmanagedCallersOnly()
    {
        PlatformDefaultMemberFunctionNative.C c = CreateCWithUnmanagedCallersOnlyVTable(2.0f, 3.0f);
        PlatformDefaultMemberFunctionNative.Width result = PlatformDefaultMemberFunctionNative.GetWidthFromManaged(&c);

        Assert.Equal(c.width, result.width);
    }

    private static void Test4ByteNonHFAUnmanagedCallersOnly()
    {
        PlatformDefaultMemberFunctionNative.C c = CreateCWithUnmanagedCallersOnlyVTable(2.0f, 3.0f);
        PlatformDefaultMemberFunctionNative.IntWrapper result = PlatformDefaultMemberFunctionNative.GetHeightAsIntFromManaged(&c);

        Assert.Equal((int)c.height, result.i);
    }

    private static void TestEnumUnmanagedCallersOnly()
    {
        PlatformDefaultMemberFunctionNative.C c = CreateCWithUnmanagedCallersOnlyVTable(2.0f, 3.0f);
        PlatformDefaultMemberFunctionNative.E result = PlatformDefaultMemberFunctionNative.GetEFromManaged(&c);

        Assert.Equal(c.dummy, result);
    }

    private static void TestCLongUnmanagedCallersOnly()
    {
        PlatformDefaultMemberFunctionNative.C c = CreateCWithUnmanagedCallersOnlyVTable(2.0f, 3.0f);
        CLong result = PlatformDefaultMemberFunctionNative.GetWidthAsLongFromManaged(&c);

        Assert.Equal((nint)c.width, result.Value);
    }

    private static PlatformDefaultMemberFunctionNative.C CreateCWithUnmanagedCallersOnlyVTable(float width, float height)
    {
        return new PlatformDefaultMemberFunctionNative.C
        {
            vtable = UnmanagedCallersOnlyVtable,
            dummy = PlatformDefaultMemberFunctionNative.E.Value,
            width = width,
            height = height
        };
    }

    private static PlatformDefaultMemberFunctionNative.C.VtableLayout* unmanagedCallersOnlyVtable;

    private static PlatformDefaultMemberFunctionNative.C.VtableLayout* UnmanagedCallersOnlyVtable
    {
        get
        {
            if (unmanagedCallersOnlyVtable == null)
            {
                unmanagedCallersOnlyVtable = (PlatformDefaultMemberFunctionNative.C.VtableLayout*)Marshal.AllocHGlobal(sizeof(PlatformDefaultMemberFunctionNative.C.VtableLayout));
                unmanagedCallersOnlyVtable->getSize = &GetSize;
                unmanagedCallersOnlyVtable->getWidth = &GetWidth;
                unmanagedCallersOnlyVtable->getHeightAsInt = &GetHeightAsInt;
                unmanagedCallersOnlyVtable->getE = &GetE;
                unmanagedCallersOnlyVtable->getWidthAsLong = &GetWidthAsLong;
            }
            return unmanagedCallersOnlyVtable;
        }
    }

    [UnmanagedCallersOnly(CallConvs = new [] {typeof(CallConvMemberFunction)})]
    private static PlatformDefaultMemberFunctionNative.SizeF GetSize(PlatformDefaultMemberFunctionNative.C* c, int unused)
    {
        return new PlatformDefaultMemberFunctionNative.SizeF
        {
            width = c->width,
            height = c->height
        };
    }

    [UnmanagedCallersOnly(CallConvs = new [] {typeof(CallConvMemberFunction)})]
    private static PlatformDefaultMemberFunctionNative.Width GetWidth(PlatformDefaultMemberFunctionNative.C* c)
    {
        return new PlatformDefaultMemberFunctionNative.Width
        {
            width = c->width
        };
    }

    [UnmanagedCallersOnly(CallConvs = new [] {typeof(CallConvMemberFunction)})]
    private static PlatformDefaultMemberFunctionNative.IntWrapper GetHeightAsInt(PlatformDefaultMemberFunctionNative.C* c)
    {
        return new PlatformDefaultMemberFunctionNative.IntWrapper
        {
            i = (int)c->height
        };
    }

    [UnmanagedCallersOnly(CallConvs = new [] {typeof(CallConvMemberFunction)})]
    private static PlatformDefaultMemberFunctionNative.E GetE(PlatformDefaultMemberFunctionNative.C* c)
    {
        return c->dummy;
    }

    [UnmanagedCallersOnly(CallConvs = new [] {typeof(CallConvMemberFunction)})]
    private static CLong GetWidthAsLong(PlatformDefaultMemberFunctionNative.C* c)
    {
        return new CLong((nint)c->width);
    }
}
