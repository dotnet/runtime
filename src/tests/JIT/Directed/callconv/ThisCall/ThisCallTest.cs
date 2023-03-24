// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Reflection;
using System.Text;
using Xunit;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

unsafe class ThisCallNative
{
    public struct C
    {
        public struct VtableLayout
        {
            public IntPtr getSize;
            public IntPtr getWidth;
            public IntPtr getHeightAsInt;
            public IntPtr getE;
            public IntPtr getWidthAsLong;
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

    [UnmanagedFunctionPointer(CallingConvention.ThisCall)]
    public delegate SizeF GetSizeFn(C* c, int unused);
    [UnmanagedFunctionPointer(CallingConvention.ThisCall)]
    public delegate Width GetWidthFn(C* c);
    [UnmanagedFunctionPointer(CallingConvention.ThisCall)]
    public delegate IntWrapper GetHeightAsIntFn(C* c);

    [UnmanagedFunctionPointer(CallingConvention.ThisCall)]
    public delegate E GetEFn(C* c);

    [UnmanagedFunctionPointer(CallingConvention.ThisCall)]
    public delegate CLong GetWidthAsLongFn(C* c);

    [DllImport(nameof(ThisCallNative))]
    public static extern C* CreateInstanceOfC(float width, float height);

    [DllImport(nameof(ThisCallNative))]
    public static extern SizeF GetSizeFromManaged(C* c);
    [DllImport(nameof(ThisCallNative))]
    public static extern Width GetWidthFromManaged(C* c);
    [DllImport(nameof(ThisCallNative))]
    public static extern IntWrapper GetHeightAsIntFromManaged(C* c);
    [DllImport(nameof(ThisCallNative))]
    public static extern E GetEFromManaged(C* c);
    [DllImport(nameof(ThisCallNative))]
    public static extern CLong GetWidthAsLongFromManaged(C* c);
}

public unsafe class ThisCallTest
{
    [Fact]
    public static int TestEntryPoint()
    {
        try
        {
            float width = 1.0f;
            float height = 2.0f;
            ThisCallNative.C* instance = ThisCallNative.CreateInstanceOfC(width, height);
            Test8ByteHFA(instance);
            Test4ByteHFA(instance);
            Test4ByteNonHFA(instance);
            TestEnum(instance);
            TestCLong(instance);
            Test8ByteHFAReverse();
            Test4ByteHFAReverse();
            Test4ByteNonHFAReverse();
            TestEnumReverse();
            TestCLongReverse();
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

    private static void Test8ByteHFA(ThisCallNative.C* instance)
    {
        ThisCallNative.GetSizeFn callback = Marshal.GetDelegateForFunctionPointer<ThisCallNative.GetSizeFn>(instance->vtable->getSize);

        ThisCallNative.SizeF result = callback(instance, 1234);

        Assert.Equal(instance->width, result.width);
        Assert.Equal(instance->height, result.height);
    }

    private static void Test4ByteHFA(ThisCallNative.C* instance)
    {
        ThisCallNative.GetWidthFn callback = Marshal.GetDelegateForFunctionPointer<ThisCallNative.GetWidthFn>(instance->vtable->getWidth);

        ThisCallNative.Width result = callback(instance);

        Assert.Equal(instance->width, result.width);
    }

    private static void Test4ByteNonHFA(ThisCallNative.C* instance)
    {
        ThisCallNative.GetHeightAsIntFn callback = Marshal.GetDelegateForFunctionPointer<ThisCallNative.GetHeightAsIntFn>(instance->vtable->getHeightAsInt);

        ThisCallNative.IntWrapper result = callback(instance);

        Assert.Equal((int)instance->height, result.i);
    }

    private static void TestEnum(ThisCallNative.C* instance)
    {
        ThisCallNative.GetEFn callback = Marshal.GetDelegateForFunctionPointer<ThisCallNative.GetEFn>(instance->vtable->getE);

        ThisCallNative.E result = callback(instance);

        Assert.Equal(instance->dummy, result);
    }

    private static void TestCLong(ThisCallNative.C* instance)
    {
        ThisCallNative.GetWidthAsLongFn callback = Marshal.GetDelegateForFunctionPointer<ThisCallNative.GetWidthAsLongFn>(instance->vtable->getWidthAsLong);

        CLong result = callback(instance);

        Assert.Equal((nint)instance->width, result.Value);
    }

    private static void Test8ByteHFAReverse()
    {
        ThisCallNative.C c = CreateCWithManagedVTable(2.0f, 3.0f);
        ThisCallNative.SizeF result = ThisCallNative.GetSizeFromManaged(&c);

        Assert.Equal(c.width, result.width);
        Assert.Equal(c.height, result.height);
    }

    private static void Test4ByteHFAReverse()
    {
        ThisCallNative.C c = CreateCWithManagedVTable(2.0f, 3.0f);
        ThisCallNative.Width result = ThisCallNative.GetWidthFromManaged(&c);

        Assert.Equal(c.width, result.width);
    }

    private static void Test4ByteNonHFAReverse()
    {
        ThisCallNative.C c = CreateCWithManagedVTable(2.0f, 3.0f);
        ThisCallNative.IntWrapper result = ThisCallNative.GetHeightAsIntFromManaged(&c);

        Assert.Equal((int)c.height, result.i);
    }

    private static void TestEnumReverse()
    {
        ThisCallNative.C c = CreateCWithManagedVTable(2.0f, 3.0f);
        ThisCallNative.E result = ThisCallNative.GetEFromManaged(&c);

        Assert.Equal(c.dummy, result);
    }

    private static void TestCLongReverse()
    {
        ThisCallNative.C c = CreateCWithManagedVTable(2.0f, 3.0f);
        CLong result = ThisCallNative.GetWidthAsLongFromManaged(&c);

        Assert.Equal((nint)c.width, result.Value);
    }

    private static void Test8ByteHFAUnmanagedCallersOnly()
    {
        ThisCallNative.C c = CreateCWithUnmanagedCallersOnlyVTable(2.0f, 3.0f);
        ThisCallNative.SizeF result = ThisCallNative.GetSizeFromManaged(&c);

        Assert.Equal(c.width, result.width);
        Assert.Equal(c.height, result.height);
    }

    private static void Test4ByteHFAUnmanagedCallersOnly()
    {
        ThisCallNative.C c = CreateCWithUnmanagedCallersOnlyVTable(2.0f, 3.0f);
        ThisCallNative.Width result = ThisCallNative.GetWidthFromManaged(&c);

        Assert.Equal(c.width, result.width);
    }

    private static void Test4ByteNonHFAUnmanagedCallersOnly()
    {
        ThisCallNative.C c = CreateCWithUnmanagedCallersOnlyVTable(2.0f, 3.0f);
        ThisCallNative.IntWrapper result = ThisCallNative.GetHeightAsIntFromManaged(&c);

        Assert.Equal((int)c.height, result.i);
    }

    private static void TestEnumUnmanagedCallersOnly()
    {
        ThisCallNative.C c = CreateCWithUnmanagedCallersOnlyVTable(2.0f, 3.0f);
        ThisCallNative.E result = ThisCallNative.GetEFromManaged(&c);

        Assert.Equal(c.dummy, result);
    }

    private static void TestCLongUnmanagedCallersOnly()
    {
        ThisCallNative.C c = CreateCWithUnmanagedCallersOnlyVTable(2.0f, 3.0f);
        CLong result = ThisCallNative.GetWidthAsLongFromManaged(&c);

        Assert.Equal((nint)c.width, result.Value);
    }

    private static ThisCallNative.C CreateCWithManagedVTable(float width, float height)
    {
        return new ThisCallNative.C
        {
            vtable = ManagedVtable,
            dummy = ThisCallNative.E.Value,
            width = width,
            height = height
        };
    }

    private static ThisCallNative.C CreateCWithUnmanagedCallersOnlyVTable(float width, float height)
    {
        return new ThisCallNative.C
        {
            vtable = UnmanagedCallersOnlyVtable,
            dummy = ThisCallNative.E.Value,
            width = width,
            height = height
        };
    }

    private static ThisCallNative.C.VtableLayout* managedVtable;

    private static ThisCallNative.C.VtableLayout* ManagedVtable
    {
        get
        {
            if (managedVtable == null)
            {
                managedVtable = (ThisCallNative.C.VtableLayout*)Marshal.AllocHGlobal(sizeof(ThisCallNative.C.VtableLayout));
                managedVtable->getSize = Marshal.GetFunctionPointerForDelegate(
                    (ThisCallNative.GetSizeFn)((ThisCallNative.C* c, int unused) => new ThisCallNative.SizeF { width = c->width, height = c->height} ));
                managedVtable->getWidth = Marshal.GetFunctionPointerForDelegate(
                    (ThisCallNative.GetWidthFn)((ThisCallNative.C* c) => new ThisCallNative.Width { width = c->width} ));
                managedVtable->getHeightAsInt = Marshal.GetFunctionPointerForDelegate(
                    (ThisCallNative.GetHeightAsIntFn)((ThisCallNative.C* c) => new ThisCallNative.IntWrapper { i = (int)c->height} ));
                managedVtable->getE = Marshal.GetFunctionPointerForDelegate(
                    (ThisCallNative.GetEFn)((ThisCallNative.C* c) => c->dummy ));
                managedVtable->getWidthAsLong = Marshal.GetFunctionPointerForDelegate(
                    (ThisCallNative.GetWidthAsLongFn)((ThisCallNative.C* c) => new CLong((nint)c->width)));
            }
            return managedVtable;
        }
    }

    private static ThisCallNative.C.VtableLayout* unmanagedCallersOnlyVtable;

    private static ThisCallNative.C.VtableLayout* UnmanagedCallersOnlyVtable
    {
        get
        {
            if (unmanagedCallersOnlyVtable == null)
            {
                unmanagedCallersOnlyVtable = (ThisCallNative.C.VtableLayout*)Marshal.AllocHGlobal(sizeof(ThisCallNative.C.VtableLayout));
                unmanagedCallersOnlyVtable->getSize = (IntPtr)(delegate* unmanaged[Thiscall]<ThisCallNative.C*, int, ThisCallNative.SizeF>)&GetSize;
                unmanagedCallersOnlyVtable->getWidth = (IntPtr)(delegate* unmanaged[Thiscall]<ThisCallNative.C*, ThisCallNative.Width>)&GetWidth;
                unmanagedCallersOnlyVtable->getHeightAsInt = (IntPtr)(delegate* unmanaged[Thiscall]<ThisCallNative.C*, ThisCallNative.IntWrapper>)&GetHeightAsInt;
                unmanagedCallersOnlyVtable->getE = (IntPtr)(delegate* unmanaged[Thiscall]<ThisCallNative.C*, ThisCallNative.E>)&GetE;
                unmanagedCallersOnlyVtable->getWidthAsLong = (IntPtr)(delegate* unmanaged[Thiscall]<ThisCallNative.C*, CLong>)&GetWidthAsLong;
            }
            return unmanagedCallersOnlyVtable;
        }
    }

    [UnmanagedCallersOnly(CallConvs = new [] {typeof(CallConvThiscall)})]
    private static ThisCallNative.SizeF GetSize(ThisCallNative.C* c, int unused)
    {
        return new ThisCallNative.SizeF
        {
            width = c->width,
            height = c->height
        };
    }

    [UnmanagedCallersOnly(CallConvs = new [] {typeof(CallConvThiscall)})]
    private static ThisCallNative.Width GetWidth(ThisCallNative.C* c)
    {
        return new ThisCallNative.Width
        {
            width = c->width
        };
    }

    [UnmanagedCallersOnly(CallConvs = new [] {typeof(CallConvThiscall)})]
    private static ThisCallNative.IntWrapper GetHeightAsInt(ThisCallNative.C* c)
    {
        return new ThisCallNative.IntWrapper
        {
            i = (int)c->height
        };
    }

    [UnmanagedCallersOnly(CallConvs = new [] {typeof(CallConvThiscall)})]
    private static ThisCallNative.E GetE(ThisCallNative.C* c)
    {
        return c->dummy;
    }

    [UnmanagedCallersOnly(CallConvs = new [] {typeof(CallConvThiscall)})]
    private static CLong GetWidthAsLong(ThisCallNative.C* c)
    {
        return new CLong((nint)c->width);
    }
}
