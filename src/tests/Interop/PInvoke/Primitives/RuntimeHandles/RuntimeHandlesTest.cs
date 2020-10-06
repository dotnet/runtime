// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.InteropServices;
using System;
using System.Reflection;
using TestLibrary;

class TestClass
{
    public int field;

    public void Method()
    {
    }
}

class RuntimeHandlesTest
{
    [DllImport("RuntimeHandlesNative")]
    private static extern bool Marshal_In(RuntimeMethodHandle expected, IntPtr handle);
    [DllImport("RuntimeHandlesNative")]
    private static extern bool Marshal_In(RuntimeFieldHandle expected, IntPtr handle);
    [DllImport("RuntimeHandlesNative")]
    private static extern bool Marshal_In(RuntimeTypeHandle expected, IntPtr handle);

    private static void TestRuntimeMethodHandle()
    {
        RuntimeMethodHandle handle = typeof(TestClass).GetMethod(nameof(TestClass.Method)).MethodHandle;
        Assert.IsTrue(Marshal_In(handle, handle.Value));
    }

    private static void TestRuntimeFieldHandle()
    {
        RuntimeFieldHandle handle = typeof(TestClass).GetField(nameof(TestClass.field)).FieldHandle;
        Assert.IsTrue(Marshal_In(handle, handle.Value));
    }

    private static void TestRuntimeTypeHandle()
    {
        RuntimeTypeHandle handle = typeof(TestClass).TypeHandle;
        Assert.IsTrue(Marshal_In(handle, handle.Value));
    }

    public static int Main()
    {
        try
        {
            TestRuntimeTypeHandle();
            TestRuntimeFieldHandle();
            TestRuntimeMethodHandle();
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            return 101;
        }
        return 100;
    }
}
