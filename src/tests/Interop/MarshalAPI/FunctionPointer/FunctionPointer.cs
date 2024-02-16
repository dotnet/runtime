// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
using System;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using Xunit;

[SkipOnMono("needs triage")]
[ActiveIssue("https://github.com/dotnet/runtime/issues/91388", typeof(TestLibrary.PlatformDetection), nameof(TestLibrary.PlatformDetection.PlatformDoesNotSupportNativeTestAssets))]
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

    [Fact]

    [ActiveIssue("https://github.com/dotnet/runtimelab/issues/164", typeof(TestLibrary.Utilities), nameof(TestLibrary.Utilities.IsNativeAot))]
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
            Assert.Null(del.Target);
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

    [Fact]
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

    [Fact]
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

    [UnmanagedCallersOnly]
    static int UnmanagedExportedFunction(float arg)
    {
        return Convert.ToInt32(arg);
    }
    
    [UnmanagedCallersOnly]
    static BlittableGeneric<int> UnmanagedExportedFunctionBlittableGenericInt(float arg)
    {
        return new() { X = Convert.ToInt32(arg) };
    }

    [UnmanagedCallersOnly]
    static BlittableGeneric<string> UnmanagedExportedFunctionBlittableGenericString(float arg)
    {
        return new() { X = Convert.ToInt32(arg) };
    }

    class GenericCaller<T>
    {
        internal static unsafe T GenericCalli<U>(void* fnptr, U arg)
        {
            return ((delegate* unmanaged<U, T>)fnptr)(arg);
        }

        internal static unsafe BlittableGeneric<T> WrappedGenericCalli<U>(void* fnptr, U arg)
        {
            return ((delegate* unmanaged<U, BlittableGeneric<T>>)fnptr)(arg);
        }
    }

    struct BlittableGeneric<T>
    {
        public int X;
    }

    [Theory]
    [InlineData(0f)]
    [InlineData(1f)]
    [InlineData(-1f)]
    [InlineData(42f)]
    [InlineData(60f)]
    public static void RunGenericFunctionPointerTest(float inVal)
    {
        Console.WriteLine($"Running {nameof(RunGenericFunctionPointerTest)}...");
        int outVar = 0;
        int expectedValue = Convert.ToInt32(inVal);

        Console.WriteLine("Testing GenericCalli with int as the return type");
        unsafe
        {
            outVar = GenericCaller<int>.GenericCalli((delegate* unmanaged<float, int>)&UnmanagedExportedFunction, inVal);
        }
        Assert.Equal(expectedValue, outVar);
        
        outVar = 0;
        Console.WriteLine("Testing GenericCalli with BlittableGeneric<int> as the return type");
        unsafe
        {
            outVar = GenericCaller<int>.WrappedGenericCalli((delegate* unmanaged<float, BlittableGeneric<int>>)&UnmanagedExportedFunctionBlittableGenericInt, inVal).X;
        }
        Assert.Equal(expectedValue, outVar);

        outVar = 0;
        Console.WriteLine("Testing GenericCalli with BlittableGeneric<string> as the return type");
        unsafe
        {
            outVar = GenericCaller<string>.WrappedGenericCalli((delegate* unmanaged<float, BlittableGeneric<string>>)&UnmanagedExportedFunctionBlittableGenericString, inVal).X;
        }
        Assert.Equal(expectedValue, outVar);
    }
/*
    public static bool CanRunInvalidGenericFunctionPointerTest => !TestLibrary.Utilities.IsNativeAot;

    [ConditionalFact(nameof(CanRunInvalidGenericFunctionPointerTest))]
    public static void RunInvalidGenericFunctionPointerTest()
    {
        Console.WriteLine($"Running {nameof(RunInvalidGenericFunctionPointerTest)}...");
        unsafe
        {
            nint fnptr = (nint)(delegate* unmanaged<nint*, nint*>)&ReturnParameter;
            Console.WriteLine("Testing GenericCalli with string as the parameter type");
            Assert.Throws<MarshalDirectiveException>(() => GenericCaller<int>.GenericCalli((delegate* unmanaged<string, int>)fnptr, "test"));
            Console.WriteLine("Testing GenericCalli with string as the return type");
            Assert.Throws<MarshalDirectiveException>(() => GenericCaller<string>.GenericCalli((delegate* unmanaged<int, string>)fnptr, "test"));
            Console.WriteLine("Testing GenericCalli with string as both the parameter and return type");
            Assert.Throws<MarshalDirectiveException>(() => GenericCaller<string>.GenericCalli((delegate* unmanaged<string, string>)fnptr, "test"));
        }
    }
*/
    [UnmanagedCallersOnly]
    static unsafe nint* ReturnParameter(nint* p)
    {
        return p;
    }
}
