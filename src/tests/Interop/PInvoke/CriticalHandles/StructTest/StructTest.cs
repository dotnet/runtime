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
public class CriticalHandleStructTest
{
    private static Native.HandleCallback s_handleCallback = (handleValue) =>
    {
        GC.Collect();
        GC.WaitForPendingFinalizers();
        return !MyCriticalHandle.IsHandleClosed(handleValue);
    };

    public static void In()
    {
        IntPtr handleValue = MyCriticalHandle.GetUniqueHandle();
        InWorker(handleValue);
        GC.Collect();
        GC.WaitForPendingFinalizers();
        Assert.True(MyCriticalHandle.IsHandleClosed(handleValue));
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void InWorker(IntPtr handleValue)
    {
        Native.MyCriticalHandleStruct handleStruct = new Native.MyCriticalHandleStruct() { Handle = new MyCriticalHandle() { Handle = handleValue } };
        IntPtr value;
        value = Native.In(handleStruct, s_handleCallback);
        Assert.Equal(handleValue.ToInt32(), value.ToInt32());
    }

    public static void Ret()
    {
        IntPtr handleValue = MyCriticalHandle.GetUniqueHandle();
        Assert.Throws<NotSupportedException>(() => Native.Ret(handleValue));
    }

    public static void Out()
    {
        IntPtr handleValue = MyCriticalHandle.GetUniqueHandle();
        Native.MyCriticalHandleStruct handleStruct;
        Assert.Throws<NotSupportedException>(() => Native.Out(handleValue, out handleStruct));
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void OutWorker(IntPtr handleValue)
    {
        Native.MyCriticalHandleStruct handleStruct;
        Native.Out(handleValue, out handleStruct);
        Assert.Equal(handleValue.ToInt32(), handleStruct.Handle.Handle.ToInt32());
    }

    public static void InRef()
    {
        IntPtr handleValue = MyCriticalHandle.GetUniqueHandle();
        InRefWorker(handleValue);
        GC.Collect();
        GC.WaitForPendingFinalizers();
        Assert.True(MyCriticalHandle.IsHandleClosed(handleValue));
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void InRefWorker(IntPtr handleValue)
    {
        Native.MyCriticalHandleStruct handleStruct = new Native.MyCriticalHandleStruct() { Handle = new MyCriticalHandle() { Handle = handleValue } };
        Native.InRef(ref handleStruct, s_handleCallback);
        Assert.Equal(handleValue.ToInt32(), handleStruct.Handle.Handle.ToInt32());
    }

    public static void Ref()
    {
        IntPtr handleValue = MyCriticalHandle.GetUniqueHandle();
        RefWorker(handleValue);
        GC.Collect();
        GC.WaitForPendingFinalizers();
        Assert.True(MyCriticalHandle.IsHandleClosed(handleValue));
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void RefWorker(IntPtr handleValue)
    {
        Native.MyCriticalHandleStruct handleStruct = new Native.MyCriticalHandleStruct() { Handle = new MyCriticalHandle() { Handle = handleValue } };
        Native.Ref(ref handleStruct, s_handleCallback);
        Assert.Equal(handleValue.ToInt32(), handleStruct.Handle.Handle.ToInt32());
    }

    public static void RefModify()
    {
        IntPtr handleValue1 = MyCriticalHandle.GetUniqueHandle();
        IntPtr handleValue2 = MyCriticalHandle.GetUniqueHandle();
        Native.MyCriticalHandleStruct handleStruct = new Native.MyCriticalHandleStruct() { Handle = new MyCriticalHandle() { Handle = handleValue1 } };

        Assert.Throws<NotSupportedException>(() => Native.RefModify(handleValue2, ref handleStruct, null));
    }

    internal class Native
    {
        [StructLayout(LayoutKind.Sequential)]
        internal struct MyCriticalHandleStruct
        {
            internal MyCriticalHandle Handle;
        }

        [UnmanagedFunctionPointer(System.Runtime.InteropServices.CallingConvention.StdCall)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal delegate bool HandleCallback(IntPtr handle);

        [DllImport("CriticalHandlesNative", CallingConvention = CallingConvention.StdCall)]
        internal static extern IntPtr In(MyCriticalHandleStruct handle, HandleCallback handleCallback);

        [DllImport("CriticalHandlesNative", CallingConvention = CallingConvention.StdCall)]
        internal static extern void Out(IntPtr handleValue, out MyCriticalHandleStruct handle);

        [DllImport("CriticalHandlesNative", EntryPoint = "Ref", CallingConvention = CallingConvention.StdCall)]
        internal static extern IntPtr InRef([In]ref MyCriticalHandleStruct handle, HandleCallback handleCallback);

        [DllImport("CriticalHandlesNative", CallingConvention = CallingConvention.StdCall)]
        internal static extern IntPtr Ref(ref MyCriticalHandleStruct handle, HandleCallback handleCallback);

        [DllImport("CriticalHandlesNative", CallingConvention = CallingConvention.StdCall)]
        internal static extern IntPtr RefModify(IntPtr handleValue, ref MyCriticalHandleStruct handle, HandleCallback handleCallback);

        [DllImport("CriticalHandlesNative", CallingConvention = CallingConvention.StdCall)]
        internal static extern MyCriticalHandleStruct Ret(IntPtr handleValue);
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
            RefModify();

            return 100;
        }
        catch (Exception e)
        {
            Console.WriteLine($"Test Failure: {e}");
            return 101;
        }
    }
}
