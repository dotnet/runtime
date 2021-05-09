// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Reflection;
using System.Text;
using TestLibrary;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

unsafe class CdeclMemberFunctionNative
{
    public struct C
    {
        public struct VtableLayout
        {
            public delegate* unmanaged[Cdecl, MemberFunction]<C*, int, SizeF> getSize;
            public delegate* unmanaged[Cdecl, MemberFunction]<C*, Width> getWidth;
            public delegate* unmanaged[Cdecl, MemberFunction]<C*, IntWrapper> getHeightAsInt;
            public delegate* unmanaged[Cdecl, MemberFunction]<C*, E> getE;
            public delegate* unmanaged[Cdecl, MemberFunction]<C*, CLong> getWidthAsLong;
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

    [DllImport(nameof(CdeclMemberFunctionNative))]
    public static extern C* CreateInstanceOfC(float width, float height);

    [DllImport(nameof(CdeclMemberFunctionNative))]
    public static extern SizeF GetSizeFromManaged(C* c);
    [DllImport(nameof(CdeclMemberFunctionNative))]
    public static extern Width GetWidthFromManaged(C* c);
    [DllImport(nameof(CdeclMemberFunctionNative))]
    public static extern IntWrapper GetHeightAsIntFromManaged(C* c);
    [DllImport(nameof(CdeclMemberFunctionNative))]
    public static extern E GetEFromManaged(C* c);
    [DllImport(nameof(CdeclMemberFunctionNative))]
    public static extern CLong GetWidthAsLongFromManaged(C* c);
}

unsafe class CdeclMemberFunctionTest
{
    public static int Main(string[] args)
    {
        try
        {
            float width = 1.0f;
            float height = 2.0f;
            CdeclMemberFunctionNative.C* instance = CdeclMemberFunctionNative.CreateInstanceOfC(width, height);
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

    private static void Test8ByteHFA(CdeclMemberFunctionNative.C* instance)
    {
        CdeclMemberFunctionNative.SizeF result = instance->vtable->getSize(instance, 1234);

        Assert.AreEqual(instance->width, result.width);
        Assert.AreEqual(instance->height, result.height);
    }

    private static void Test4ByteHFA(CdeclMemberFunctionNative.C* instance)
    {
        CdeclMemberFunctionNative.Width result = instance->vtable->getWidth(instance);

        Assert.AreEqual(instance->width, result.width);
    }

    private static void Test4ByteNonHFA(CdeclMemberFunctionNative.C* instance)
    {
        CdeclMemberFunctionNative.IntWrapper result = instance->vtable->getHeightAsInt(instance);

        Assert.AreEqual((int)instance->height, result.i);
    }

    private static void TestEnum(CdeclMemberFunctionNative.C* instance)
    {
        CdeclMemberFunctionNative.E result = instance->vtable->getE(instance);

        Assert.AreEqual(instance->dummy, result);
    }

    private static void TestCLong(CdeclMemberFunctionNative.C* instance)
    {
        CLong result = instance->vtable->getWidthAsLong(instance);

        Assert.AreEqual((nint)instance->width, result.Value);
    }

    private static void Test8ByteHFAUnmanagedCallersOnly()
    {
        CdeclMemberFunctionNative.C c = CreateCWithUnmanagedCallersOnlyVTable(2.0f, 3.0f);
        CdeclMemberFunctionNative.SizeF result = CdeclMemberFunctionNative.GetSizeFromManaged(&c);

        Assert.AreEqual(c.width, result.width);
        Assert.AreEqual(c.height, result.height);
    }

    private static void Test4ByteHFAUnmanagedCallersOnly()
    {
        CdeclMemberFunctionNative.C c = CreateCWithUnmanagedCallersOnlyVTable(2.0f, 3.0f);
        CdeclMemberFunctionNative.Width result = CdeclMemberFunctionNative.GetWidthFromManaged(&c);

        Assert.AreEqual(c.width, result.width);
    }

    private static void Test4ByteNonHFAUnmanagedCallersOnly()
    {
        CdeclMemberFunctionNative.C c = CreateCWithUnmanagedCallersOnlyVTable(2.0f, 3.0f);
        CdeclMemberFunctionNative.IntWrapper result = CdeclMemberFunctionNative.GetHeightAsIntFromManaged(&c);

        Assert.AreEqual((int)c.height, result.i);
    }

    private static void TestEnumUnmanagedCallersOnly()
    {
        CdeclMemberFunctionNative.C c = CreateCWithUnmanagedCallersOnlyVTable(2.0f, 3.0f);
        CdeclMemberFunctionNative.E result = CdeclMemberFunctionNative.GetEFromManaged(&c);

        Assert.AreEqual(c.dummy, result);
    }

    private static void TestCLongUnmanagedCallersOnly()
    {
        CdeclMemberFunctionNative.C c = CreateCWithUnmanagedCallersOnlyVTable(2.0f, 3.0f);
        CLong result = CdeclMemberFunctionNative.GetWidthAsLongFromManaged(&c);

        Assert.AreEqual((nint)c.width, result.Value);
    }

    private static CdeclMemberFunctionNative.C CreateCWithUnmanagedCallersOnlyVTable(float width, float height)
    {
        return new CdeclMemberFunctionNative.C
        {
            vtable = UnmanagedCallersOnlyVtable,
            dummy = CdeclMemberFunctionNative.E.Value,
            width = width,
            height = height
        };
    }

    private static CdeclMemberFunctionNative.C.VtableLayout* unmanagedCallersOnlyVtable;

    private static CdeclMemberFunctionNative.C.VtableLayout* UnmanagedCallersOnlyVtable
    {
        get
        {
            if (unmanagedCallersOnlyVtable == null)
            {
                unmanagedCallersOnlyVtable = (CdeclMemberFunctionNative.C.VtableLayout*)Marshal.AllocHGlobal(sizeof(CdeclMemberFunctionNative.C.VtableLayout));
                unmanagedCallersOnlyVtable->getSize = &GetSize;
                unmanagedCallersOnlyVtable->getWidth = &GetWidth;
                unmanagedCallersOnlyVtable->getHeightAsInt = &GetHeightAsInt;
                unmanagedCallersOnlyVtable->getE = &GetE;
                unmanagedCallersOnlyVtable->getWidthAsLong = &GetWidthAsLong;
            }
            return unmanagedCallersOnlyVtable;
        }
    }

    [UnmanagedCallersOnly(CallConvs = new [] {typeof(CallConvCdecl), typeof(CallConvMemberFunction)})]
    private static CdeclMemberFunctionNative.SizeF GetSize(CdeclMemberFunctionNative.C* c, int unused)
    {
        return new CdeclMemberFunctionNative.SizeF
        {
            width = c->width,
            height = c->height
        };
    }

    [UnmanagedCallersOnly(CallConvs = new [] {typeof(CallConvCdecl), typeof(CallConvMemberFunction)})]
    private static CdeclMemberFunctionNative.Width GetWidth(CdeclMemberFunctionNative.C* c)
    {
        return new CdeclMemberFunctionNative.Width
        {
            width = c->width
        };
    }

    [UnmanagedCallersOnly(CallConvs = new [] {typeof(CallConvCdecl), typeof(CallConvMemberFunction)})]
    private static CdeclMemberFunctionNative.IntWrapper GetHeightAsInt(CdeclMemberFunctionNative.C* c)
    {
        return new CdeclMemberFunctionNative.IntWrapper
        {
            i = (int)c->height
        };
    }

    [UnmanagedCallersOnly(CallConvs = new [] {typeof(CallConvCdecl), typeof(CallConvMemberFunction)})]
    private static CdeclMemberFunctionNative.E GetE(CdeclMemberFunctionNative.C* c)
    {
        return c->dummy;
    }

    [UnmanagedCallersOnly(CallConvs = new [] {typeof(CallConvCdecl), typeof(CallConvMemberFunction)})]
    private static CLong GetWidthAsLong(CdeclMemberFunctionNative.C* c)
    {
        return new CLong((nint)c->width);
    }
}
