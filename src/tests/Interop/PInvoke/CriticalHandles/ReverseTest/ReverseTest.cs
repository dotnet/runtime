// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Xunit;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Runtime.CompilerServices;

internal class MyCriticalHandle : CriticalHandle
{
    static int s_uniqueHandleValue;
    static HashSet<int> s_closedHandles = new HashSet<int>();

    public MyCriticalHandle() : base(new IntPtr(-1))
    {

    }

    public override bool IsInvalid
    {
        get { return false; }
    }

    protected override bool ReleaseHandle()
    {
        if (!s_closedHandles.Contains(handle.ToInt32()))
        {
            s_closedHandles.Add(handle.ToInt32());
            return true;
        }

        return false;
    }

    internal IntPtr Handle
    {
        get
        {
            return handle;
        }
        set
        {
            handle = value;
        }
    }

    internal static IntPtr GetUniqueHandle()
    {
        return new IntPtr(s_uniqueHandleValue++);
    }

    internal static bool IsHandleClosed(IntPtr handle)
    {
        return s_closedHandles.Contains(handle.ToInt32());
    }
}

[ActiveIssue("https://github.com/dotnet/runtime/issues/91388", typeof(TestLibrary.PlatformDetection), nameof(TestLibrary.PlatformDetection.PlatformDoesNotSupportNativeTestAssets))]
public class Reverse
{
    public static void In()
    {
        IntPtr handleValue = new IntPtr(1);
        Native.InCallback callback = (handle) => { };
        Assert.Throws<MarshalDirectiveException>(() => Native.InvokeInCallback(callback, handleValue));
        GC.KeepAlive(callback);
    }

    public static void Ret()
    {
        IntPtr handleValue = new IntPtr(2);
        Native.RetCallback callback = () => new MyCriticalHandle();
        Assert.Throws<MarshalDirectiveException>(() => Native.InvokeRetCallback(callback));
        GC.KeepAlive(callback);
    }

    public static void Out()
    {
        IntPtr handleValue = new IntPtr(3);
        Native.OutCallback callback = (out MyCriticalHandle handle) => handle = null;
        Assert.Throws<MarshalDirectiveException>(() => Native.InvokeOutCallback(callback, ref handleValue));
        GC.KeepAlive(callback);
    }

    public static void InRef()
    {
        IntPtr handleValue = new IntPtr(4);
        Native.InRefCallback callback = (ref MyCriticalHandle handle) => { };
        Assert.Throws<MarshalDirectiveException>(() => Native.InvokeInRefCallback(callback, ref handleValue));
        GC.KeepAlive(callback);
    }

    public static void Ref()
    {
        IntPtr handleValue = new IntPtr(5);
        Native.RefCallback callback = (ref MyCriticalHandle handle) => { };
        Assert.Throws<MarshalDirectiveException>(() => Native.InvokeRefCallback(callback, ref handleValue));
        GC.KeepAlive(callback);
    }

    internal class Native
    {
        [UnmanagedFunctionPointer(System.Runtime.InteropServices.CallingConvention.StdCall)]
        internal delegate void InCallback(MyCriticalHandle handle);

        [UnmanagedFunctionPointer(System.Runtime.InteropServices.CallingConvention.StdCall)]
        internal delegate void OutCallback(out MyCriticalHandle handle);

        [UnmanagedFunctionPointer(System.Runtime.InteropServices.CallingConvention.StdCall)]
        internal delegate void InRefCallback([In]ref MyCriticalHandle handle);

        [UnmanagedFunctionPointer(System.Runtime.InteropServices.CallingConvention.StdCall)]
        internal delegate void RefCallback(ref MyCriticalHandle handle);

        [UnmanagedFunctionPointer(System.Runtime.InteropServices.CallingConvention.StdCall)]
        internal delegate MyCriticalHandle RetCallback();

        [DllImport("CriticalHandlesNative", CallingConvention = CallingConvention.StdCall)]
        internal static extern void InvokeInCallback(InCallback callback, IntPtr handle);

        [DllImport("CriticalHandlesNative", EntryPoint = "InvokeRefCallback", CallingConvention = CallingConvention.StdCall)]
        internal static extern void InvokeOutCallback(OutCallback callback, ref IntPtr handle);

        [DllImport("CriticalHandlesNative", EntryPoint = "InvokeRefCallback", CallingConvention = CallingConvention.StdCall)]
        internal static extern void InvokeInRefCallback(InRefCallback callback, ref IntPtr handle);

        [DllImport("CriticalHandlesNative", CallingConvention = CallingConvention.StdCall)]
        internal static extern void InvokeRefCallback(RefCallback callback, ref IntPtr handle);

        [DllImport("CriticalHandlesNative", CallingConvention = CallingConvention.StdCall)]
        internal static extern IntPtr InvokeRetCallback(RetCallback callback);
    }

    [Fact]
    public static int TestEntryPoint()
    {
        try
        {
            In();
            Ret();
            Out();
            InRef();
            Ref();

            return 100;
        }
        catch (Exception e)
        {
            Console.WriteLine($"Test Failure: {e}");
            return 101;
        }
    }
}
