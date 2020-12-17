// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.InteropServices;
using System;
using System.Reflection;
using System.Text;
using TestLibrary;
using System.Runtime.CompilerServices;

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
    public delegate SizeF GetSizeFn(C* c);
    [UnmanagedFunctionPointer(CallingConvention.ThisCall)]
    public delegate Width GetWidthFn(C* c);
    [UnmanagedFunctionPointer(CallingConvention.ThisCall)]
    public delegate IntWrapper GetHeightAsIntFn(C* c);

    [UnmanagedFunctionPointer(CallingConvention.ThisCall)]
    public delegate E GetEFn(C* c);

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
}

unsafe class ThisCallTest
{
    public static int Main(string[] args)
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
            Test8ByteHFAReverse();
            Test4ByteHFAReverse();
            Test4ByteNonHFAReverse();
            TestEnumReverse();
            Test8ByteHFAUnmanagedCallersOnly();
            Test4ByteHFAUnmanagedCallersOnly();
            Test4ByteNonHFAUnmanagedCallersOnly();
            TestEnumUnmanagedCallersOnly();
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

        ThisCallNative.SizeF result = callback(instance);

        Assert.AreEqual(instance->width, result.width);
        Assert.AreEqual(instance->height, result.height);
    }

    private static void Test4ByteHFA(ThisCallNative.C* instance)
    {
        ThisCallNative.GetWidthFn callback = Marshal.GetDelegateForFunctionPointer<ThisCallNative.GetWidthFn>(instance->vtable->getWidth);

        ThisCallNative.Width result = callback(instance);

        Assert.AreEqual(instance->width, result.width);
    }

    private static void Test4ByteNonHFA(ThisCallNative.C* instance)
    {
        ThisCallNative.GetHeightAsIntFn callback = Marshal.GetDelegateForFunctionPointer<ThisCallNative.GetHeightAsIntFn>(instance->vtable->getHeightAsInt);

        ThisCallNative.IntWrapper result = callback(instance);

        Assert.AreEqual((int)instance->height, result.i);
    }

    private static void TestEnum(ThisCallNative.C* instance)
    {
        ThisCallNative.GetEFn callback = Marshal.GetDelegateForFunctionPointer<ThisCallNative.GetEFn>(instance->vtable->getE);

        ThisCallNative.E result = callback(instance);

        Assert.AreEqual(instance->dummy, result);
    }

    private static void Test8ByteHFAReverse()
    {
        ThisCallNative.C c = CreateCWithManagedVTable(2.0f, 3.0f);
        ThisCallNative.SizeF result = ThisCallNative.GetSizeFromManaged(&c);
        
        Assert.AreEqual(c.width, result.width);
        Assert.AreEqual(c.height, result.height);
    }
    
    private static void Test4ByteHFAReverse()
    {
        ThisCallNative.C c = CreateCWithManagedVTable(2.0f, 3.0f);
        ThisCallNative.Width result = ThisCallNative.GetWidthFromManaged(&c);

        Assert.AreEqual(c.width, result.width);
    }

    private static void Test4ByteNonHFAReverse()
    {
        ThisCallNative.C c = CreateCWithManagedVTable(2.0f, 3.0f);
        ThisCallNative.IntWrapper result = ThisCallNative.GetHeightAsIntFromManaged(&c);

        Assert.AreEqual((int)c.height, result.i);
    }

    private static void TestEnumReverse()
    {
        ThisCallNative.C c = CreateCWithManagedVTable(2.0f, 3.0f);
        ThisCallNative.E result = ThisCallNative.GetEFromManaged(&c);

        Assert.AreEqual(c.dummy, result);
    }
    private static void Test8ByteHFAUnmanagedCallersOnly()
    {
        ThisCallNative.C c = CreateCWithUnmanagedCallersOnlyVTable(2.0f, 3.0f);
        ThisCallNative.SizeF result = ThisCallNative.GetSizeFromManaged(&c);
        
        Assert.AreEqual(c.width, result.width);
        Assert.AreEqual(c.height, result.height);
    }
    
    private static void Test4ByteHFAUnmanagedCallersOnly()
    {
        ThisCallNative.C c = CreateCWithUnmanagedCallersOnlyVTable(2.0f, 3.0f);
        ThisCallNative.Width result = ThisCallNative.GetWidthFromManaged(&c);

        Assert.AreEqual(c.width, result.width);
    }

    private static void Test4ByteNonHFAUnmanagedCallersOnly()
    {
        ThisCallNative.C c = CreateCWithUnmanagedCallersOnlyVTable(2.0f, 3.0f);
        ThisCallNative.IntWrapper result = ThisCallNative.GetHeightAsIntFromManaged(&c);

        Assert.AreEqual((int)c.height, result.i);
    }

    private static void TestEnumUnmanagedCallersOnly()
    {
        ThisCallNative.C c = CreateCWithUnmanagedCallersOnlyVTable(2.0f, 3.0f);
        ThisCallNative.E result = ThisCallNative.GetEFromManaged(&c);

        Assert.AreEqual(c.dummy, result);
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
                    (ThisCallNative.GetSizeFn)((ThisCallNative.C* c) => new ThisCallNative.SizeF { width = c->width, height = c->height} ));
                managedVtable->getWidth = Marshal.GetFunctionPointerForDelegate(
                    (ThisCallNative.GetWidthFn)((ThisCallNative.C* c) => new ThisCallNative.Width { width = c->width} ));
                managedVtable->getHeightAsInt = Marshal.GetFunctionPointerForDelegate(
                    (ThisCallNative.GetHeightAsIntFn)((ThisCallNative.C* c) => new ThisCallNative.IntWrapper { i = (int)c->height} ));
                managedVtable->getE = Marshal.GetFunctionPointerForDelegate(
                    (ThisCallNative.GetEFn)((ThisCallNative.C* c) => c->dummy ));
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
                unmanagedCallersOnlyVtable->getSize = (IntPtr)(delegate* unmanaged[Thiscall]<ThisCallNative.C*, ThisCallNative.SizeF>)&GetSize;
                unmanagedCallersOnlyVtable->getWidth = (IntPtr)(delegate* unmanaged[Thiscall]<ThisCallNative.C*, ThisCallNative.Width>)&GetWidth;
                unmanagedCallersOnlyVtable->getHeightAsInt = (IntPtr)(delegate* unmanaged[Thiscall]<ThisCallNative.C*, ThisCallNative.IntWrapper>)&GetHeightAsInt;
                unmanagedCallersOnlyVtable->getE = (IntPtr)(delegate* unmanaged[Thiscall]<ThisCallNative.C*, ThisCallNative.E>)&GetE;
            }
            return unmanagedCallersOnlyVtable;
        }
    }

    [UnmanagedCallersOnly(CallConvs = new [] {typeof(CallConvThiscall)})]
    private static ThisCallNative.SizeF GetSize(ThisCallNative.C* c)
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
}
