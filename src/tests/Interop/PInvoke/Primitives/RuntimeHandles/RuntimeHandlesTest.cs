// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.InteropServices;
using System;
using System.Reflection;
using Xunit;


[ActiveIssue("https://github.com/dotnet/runtime/issues/91388", typeof(TestLibrary.PlatformDetection), nameof(TestLibrary.PlatformDetection.PlatformDoesNotSupportNativeTestAssets))]
public class RuntimeHandlesTest
{
    class TestClass
    {
        public int field;

        public void Method()
        {
        }
    }

    [DllImport("RuntimeHandlesNative")]
    private static extern bool Marshal_In(RuntimeMethodHandle expected, IntPtr handle);
    [DllImport("RuntimeHandlesNative")]
    private static extern bool Marshal_In(RuntimeFieldHandle expected, IntPtr handle);
    [DllImport("RuntimeHandlesNative")]
    private static extern bool Marshal_In(RuntimeTypeHandle expected, IntPtr handle);

    private static void TestRuntimeMethodHandle()
    {
        RuntimeMethodHandle handle = typeof(TestClass).GetMethod(nameof(TestClass.Method)).MethodHandle;
        Assert.True(Marshal_In(handle, handle.Value));
    }

    private static void TestRuntimeFieldHandle()
    {
        RuntimeFieldHandle handle = typeof(TestClass).GetField(nameof(TestClass.field)).FieldHandle;
        Assert.True(Marshal_In(handle, handle.Value));
    }

    private static void TestRuntimeTypeHandle()
    {
        RuntimeTypeHandle handle = typeof(TestClass).TypeHandle;
        Assert.True(Marshal_In(handle, handle.Value));
    }

    [Fact]
    public static int TestEntryPoint()
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
