// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
using System;
using System.Runtime.InteropServices;
using Xunit;

public partial class FunctionPtr
{
    static class FunctionPointerNative
    {
        [DllImport(nameof(FunctionPointerNative))]
        public static extern IntPtr GetVoidVoidFcnPtr();

        [DllImport(nameof(FunctionPointerNative))]
        public static extern bool CheckFcnPtr(IntPtr fcnptr);

        [DllImport(nameof(FunctionPointerNative))]
        static unsafe extern void FillOutPtr(IntPtr* p);

	[DllImport(nameof(FunctionPointerNative))]
	static unsafe extern void FillOutIntParameter(out IntPtr p);
    }

    delegate void VoidDelegate();

    public static void RunGetDelForFcnPtrTest()
    {
        Console.WriteLine($"Running {nameof(RunGetDelForFcnPtrTest)}...");

        // Delegate -> FcnPtr -> Delegate
        {
            VoidDelegate md = new VoidDelegate(VoidVoidMethod);
            IntPtr fcnptr = Marshal.GetFunctionPointerForDelegate<VoidDelegate>(md);

            VoidDelegate del = (VoidDelegate)Marshal.GetDelegateForFunctionPointer(fcnptr, typeof(VoidDelegate));
            Assert.Equal(md.Target, del.Target);
            Assert.Equal(md.Method, del.Method);
        }

        // Native FcnPtr -> Delegate
        {
            IntPtr fcnptr = FunctionPointerNative.GetVoidVoidFcnPtr();
            VoidDelegate del = (VoidDelegate)Marshal.GetDelegateForFunctionPointer(fcnptr, typeof(VoidDelegate));
            Assert.Equal(null, del.Target);
            Assert.Equal("Invoke", del.Method.Name);

            // Round trip of a native function pointer is never legal for a non-concrete Delegate type
            Assert.Throws<ArgumentException>(() =>
            {
                Marshal.GetDelegateForFunctionPointer(fcnptr, typeof(Delegate));
            });

            Assert.Throws<ArgumentException>(() =>
            {
                Marshal.GetDelegateForFunctionPointer(fcnptr, typeof(MulticastDelegate));
            });
        }

        void VoidVoidMethod()
        {
            Console.WriteLine("Simple method to get a delegate for");
        }
    }


    [DllImport(nameof(FunctionPointerNative))]
    static unsafe extern void FillOutPtr(IntPtr* p);

    private unsafe delegate void DelegateToFillOutPtr([Out] IntPtr* p);

    public static void RunGetDelForOutPtrTest()
    {
        Console.WriteLine($"Running {nameof(RunGetDelForOutPtrTest)}...");
        IntPtr outVar = 0;
        int expectedValue = 60;
        unsafe
        {
            DelegateToFillOutPtr d = new DelegateToFillOutPtr(FillOutPtr);
            IntPtr ptr = Marshal.GetFunctionPointerForDelegate(d);
            DelegateToFillOutPtr OutPtrDelegate = Marshal.GetDelegateForFunctionPointer<DelegateToFillOutPtr>(ptr);
            OutPtrDelegate(&outVar);
            GC.KeepAlive(d);
        }
        Assert.Equal(expectedValue, outVar);
    }

    [DllImport(nameof(FunctionPointerNative))]
    static unsafe extern void FillOutIntParameter(out IntPtr p);

    private unsafe delegate void DelegateToFillOutIntParameter(out IntPtr p);

    public static void RunGetDelForOutIntTest()
    {
        Console.WriteLine($"Running {nameof(RunGetDelForOutIntTest)}...");
        IntPtr outVar = 0;
        int expectedValue = 50;
        unsafe
        {
            DelegateToFillOutIntParameter d = new DelegateToFillOutIntParameter(FillOutIntParameter);
            IntPtr ptr = Marshal.GetFunctionPointerForDelegate(d);
            DelegateToFillOutIntParameter OutPtrDelegate = Marshal.GetDelegateForFunctionPointer<DelegateToFillOutIntParameter>(ptr);
            OutPtrDelegate(out outVar);
            GC.KeepAlive(d);
        }
        Assert.Equal(expectedValue, outVar);
    }

    public static int Main()
    {
        try
        {
            RunGetDelForFcnPtrTest();
            RunGetFcnPtrSingleMulticastTest();
            RunGetDelForOutPtrTest();
            RunGetDelForOutIntTest();
        }
        catch (Exception e)
        {
            Console.WriteLine($"Test Failure: {e}");
            return 101;
        }

        return 100;
    }
}
