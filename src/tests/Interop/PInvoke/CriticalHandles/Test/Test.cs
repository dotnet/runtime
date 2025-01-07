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

public abstract class AbstractCriticalHandle : CriticalHandle
{
    public AbstractCriticalHandle() : base(new IntPtr(-1))
    {

    }

    internal IntPtr Handle
    {
        get
        {
            return handle;
        }
    }
}

public class CriticalHandleWithNoDefaultCtor : AbstractCriticalHandle
{
    public CriticalHandleWithNoDefaultCtor(IntPtr handle)
    {
        this.handle = handle;
    }

    public override bool IsInvalid
    {
        get { return false; }
    }

    protected override bool ReleaseHandle()
    {
        return true;
    }
}

public class CriticalHandleTest
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
        MyCriticalHandle handle = new MyCriticalHandle() { Handle = handleValue };
        IntPtr value;
        value = Native.In(handle, s_handleCallback);
        Assert.Equal(handleValue.ToInt32(), value.ToInt32());
    }

    public static void Ret()
    {
        IntPtr handleValue = MyCriticalHandle.GetUniqueHandle();
        RetWorker(handleValue);
        GC.Collect();
        GC.WaitForPendingFinalizers();
        Assert.True(MyCriticalHandle.IsHandleClosed(handleValue));
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void RetWorker(IntPtr handleValue)
    {
        MyCriticalHandle handle = Native.Ret(handleValue);
        Assert.Equal(handleValue.ToInt32(), handle.Handle.ToInt32());
    }

    public static void Out()
    {
        IntPtr handleValue = MyCriticalHandle.GetUniqueHandle();
        OutWorker(handleValue);
        GC.Collect();
        GC.WaitForPendingFinalizers();
        Assert.True(MyCriticalHandle.IsHandleClosed(handleValue));
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void OutWorker(IntPtr handleValue)
    {
        MyCriticalHandle handle;
        Native.Out(handleValue, out handle);
        Assert.Equal(handleValue.ToInt32(), handle.Handle.ToInt32());
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
        MyCriticalHandle handle = new MyCriticalHandle() { Handle = handleValue };
        Native.InRef(ref handle, s_handleCallback);
        Assert.Equal(handleValue.ToInt32(), handle.Handle.ToInt32());
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
        MyCriticalHandle handle = new MyCriticalHandle() { Handle = handleValue };
        Native.Ref(ref handle, s_handleCallback);
        Assert.Equal(handleValue.ToInt32(), handle.Handle.ToInt32());
    }

    public static void RefModify()
    {
        IntPtr handleValue1 = MyCriticalHandle.GetUniqueHandle();
        IntPtr handleValue2 = MyCriticalHandle.GetUniqueHandle();
        RefModifyWorker(handleValue1, handleValue2);
        GC.Collect();
        GC.WaitForPendingFinalizers();
        Assert.True(MyCriticalHandle.IsHandleClosed(handleValue1));
        Assert.True(MyCriticalHandle.IsHandleClosed(handleValue2));
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void RefModifyWorker(IntPtr handleValue1, IntPtr handleValue2)
    {
        MyCriticalHandle handle = new MyCriticalHandle() { Handle = handleValue1 };
        Native.RefModify(handleValue2, ref handle, s_handleCallback);
        Assert.Equal(handleValue2.ToInt32(), handle.Handle.ToInt32());
    }

    internal class Native
    {
        [UnmanagedFunctionPointer(System.Runtime.InteropServices.CallingConvention.StdCall)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal delegate bool HandleCallback(IntPtr handle);

        [DllImport("CriticalHandlesNative", CallingConvention = CallingConvention.StdCall)]
        internal static extern IntPtr In(MyCriticalHandle handle, HandleCallback handleCallback);

        [DllImport("CriticalHandlesNative", CallingConvention = CallingConvention.StdCall)]
        internal static extern void Out(IntPtr handleValue, out MyCriticalHandle handle);

        [DllImport("CriticalHandlesNative", EntryPoint = "Ref", CallingConvention = CallingConvention.StdCall)]
        internal static extern IntPtr InRef([In]ref MyCriticalHandle handle, HandleCallback handleCallback);

        [DllImport("CriticalHandlesNative", CallingConvention = CallingConvention.StdCall)]
        internal static extern IntPtr Ref(ref MyCriticalHandle handle, HandleCallback handleCallback);

        [DllImport("CriticalHandlesNative", CallingConvention = CallingConvention.StdCall)]
        internal static extern IntPtr RefModify(IntPtr handleValue, ref MyCriticalHandle handle, HandleCallback handleCallback);

        [DllImport("CriticalHandlesNative", CallingConvention = CallingConvention.StdCall)]
        internal static extern MyCriticalHandle Ret(IntPtr handleValue);
    }
}

public class AbstractCriticalHandleTest
{
    public static void In()
    {
        IntPtr handleValue = new IntPtr(1);
        AbstractCriticalHandle handle = new CriticalHandleWithNoDefaultCtor(handleValue);
        IntPtr value;
        value = Native.In(handle, null);
        Assert.Equal(handleValue.ToInt32(), value.ToInt32());
    }

    public static void Ret()
    {
        IntPtr handleValue = new IntPtr(2);
        Assert.Throws<MarshalDirectiveException>(() => Native.Ret(handleValue));
    }

    public static void Out()
    {
        IntPtr handleValue = new IntPtr(3);
        AbstractCriticalHandle handle;
        Assert.Throws<MarshalDirectiveException>(() => Native.Out(handleValue, out handle));
    }

    public static void InRef()
    {
        IntPtr handleValue = new IntPtr(4);
        AbstractCriticalHandle handle = new CriticalHandleWithNoDefaultCtor(handleValue);
        Native.InRef(ref handle, null);
        Assert.Equal(handleValue.ToInt32(), handle.Handle.ToInt32());
    }

    public static void Ref()
    {
        IntPtr handleValue = new IntPtr(5);
        AbstractCriticalHandle handle = new CriticalHandleWithNoDefaultCtor(handleValue);
        Assert.Throws<MarshalDirectiveException>(() => Native.Ref(ref handle, null));
    }

    internal class Native
    {
        [UnmanagedFunctionPointer(System.Runtime.InteropServices.CallingConvention.StdCall)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal delegate bool HandleCallback(IntPtr handle);

        [DllImport("CriticalHandlesNative", CallingConvention = CallingConvention.StdCall)]
        internal static extern IntPtr In(AbstractCriticalHandle handle, HandleCallback handleCallback);

        [DllImport("CriticalHandlesNative", CallingConvention = CallingConvention.StdCall)]
        internal static extern void Out(IntPtr handleValue, out AbstractCriticalHandle handle);

        [DllImport("CriticalHandlesNative", EntryPoint = "Ref", CallingConvention = CallingConvention.StdCall)]
        internal static extern IntPtr InRef([In]ref AbstractCriticalHandle handle, HandleCallback handleCallback);

        [DllImport("CriticalHandlesNative", CallingConvention = CallingConvention.StdCall)]
        internal static extern IntPtr Ref(ref AbstractCriticalHandle handle, HandleCallback handleCallback);

        [DllImport("CriticalHandlesNative", CallingConvention = CallingConvention.StdCall)]
        internal static extern AbstractCriticalHandle Ret(IntPtr handleValue);
    }
}

public class NoDefaultCtorCriticalHandleTest
{
    public static void In()
    {
        IntPtr handleValue = new IntPtr(1);
        CriticalHandleWithNoDefaultCtor handle = new CriticalHandleWithNoDefaultCtor(handleValue);
        IntPtr value;
        value = Native.In(handle, null);
        Assert.Equal(handleValue.ToInt32(), value.ToInt32());
    }

    public static void Ret()
    {
        IntPtr handleValue = new IntPtr(2);
        //TODO: Expected MissingMemberException but throws MissingMethodException
        Assert.Throws<MissingMethodException>(() => Native.Ret(handleValue));
    }

    public static void Out()
    {
        IntPtr handleValue = new IntPtr(3);
        CriticalHandleWithNoDefaultCtor handle;
        //TODO: Expected MissingMemberException but throws MissingMethodException
        Assert.Throws<MissingMethodException>(() => Native.Out(handleValue, out handle));
    }

    public static void InRef()
    {
        IntPtr handleValue = new IntPtr(4);
        CriticalHandleWithNoDefaultCtor handle = new CriticalHandleWithNoDefaultCtor(handleValue);
        //TODO: Expected MissingMemberException but throws MissingMethodException
        Assert.Throws<MissingMethodException>(() => Native.InRef(ref handle, null));
    }

    public static void Ref()
    {
        IntPtr handleValue = new IntPtr(5);
        CriticalHandleWithNoDefaultCtor handle = new CriticalHandleWithNoDefaultCtor(handleValue);
        //TODO: Expected MissingMemberException but throws MissingMethodException
        Assert.Throws<MissingMethodException>(() => Native.Ref(ref handle, null));
    }

    internal class Native
    {
        [UnmanagedFunctionPointer(System.Runtime.InteropServices.CallingConvention.StdCall)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal delegate bool HandleCallback(IntPtr handle);

        [DllImport("CriticalHandlesNative", CallingConvention = CallingConvention.StdCall)]
        internal static extern IntPtr In(CriticalHandleWithNoDefaultCtor handle, HandleCallback handleCallback);

        [DllImport("CriticalHandlesNative", CallingConvention = CallingConvention.StdCall)]
        internal static extern void Out(IntPtr handleValue, out CriticalHandleWithNoDefaultCtor handle);

        [DllImport("CriticalHandlesNative", EntryPoint = "Ref", CallingConvention = CallingConvention.StdCall)]
        internal static extern IntPtr InRef([In]ref CriticalHandleWithNoDefaultCtor handle, HandleCallback handleCallback);

        [DllImport("CriticalHandlesNative", CallingConvention = CallingConvention.StdCall)]
        internal static extern IntPtr Ref(ref CriticalHandleWithNoDefaultCtor handle, HandleCallback handleCallback);

        [DllImport("CriticalHandlesNative", CallingConvention = CallingConvention.StdCall)]
        internal static extern CriticalHandleWithNoDefaultCtor Ret(IntPtr handleValue);
    }
}

public class Test
{
    [Fact]
    public static int TestEntryPoint()
    {
        try
        {
            CriticalHandleTest.In();
            CriticalHandleTest.Ret();
            CriticalHandleTest.Out();
            CriticalHandleTest.InRef();
            CriticalHandleTest.Ref();
            CriticalHandleTest.RefModify();

            AbstractCriticalHandleTest.In();
            AbstractCriticalHandleTest.Ret();
            AbstractCriticalHandleTest.Out();
            AbstractCriticalHandleTest.InRef();
            AbstractCriticalHandleTest.Ref();

            NoDefaultCtorCriticalHandleTest.In();
            NoDefaultCtorCriticalHandleTest.Ret();
            NoDefaultCtorCriticalHandleTest.Out();
            NoDefaultCtorCriticalHandleTest.InRef();
            NoDefaultCtorCriticalHandleTest.Ref();

            return 100;
        }
        catch (Exception e)
        {
            Console.WriteLine($"Test Failure: {e}");
            return 101;
        }
    }
}
