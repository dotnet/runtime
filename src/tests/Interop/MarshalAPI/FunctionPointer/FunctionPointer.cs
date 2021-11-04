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

    public static int Main()
    {
        try
        {
            RunGetDelForFcnPtrTest();
            RunGetFcnPtrSingleMulticastTest();
        }
        catch (Exception e)
        {
            Console.WriteLine($"Test Failure: {e}");
            return 101;
        }

        return 100;
    }
}
