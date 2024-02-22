// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
using System;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using Xunit;

public partial class FunctionPtr
{
    public static bool CanRunGenericFunctionPointerTest => !TestLibrary.Utilities.IsMonoRuntime;
    public static bool CanRunInvalidGenericFunctionPointerTest => !TestLibrary.Utilities.IsNativeAot && !TestLibrary.Utilities.IsMonoRuntime;

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

    [ConditionalTheory(nameof(CanRunGenericFunctionPointerTest))]
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

    [UnmanagedCallersOnly]
    static unsafe nint* ReturnParameter(nint* p)
    {
        return p;
    }
}
