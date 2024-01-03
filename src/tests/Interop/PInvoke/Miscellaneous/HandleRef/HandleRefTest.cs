// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using Xunit;

public unsafe class HandleRefTest
{
    [DllImport(@"HandleRefNative")]
    private static extern int MarshalPointer_In(HandleRef pintValue, int stackGuard);

    [DllImport(@"HandleRefNative")]
    private static extern int MarshalPointer_InOut(HandleRef pintValue, int stackGuard);

    [DllImport(@"HandleRefNative")]
    private static extern int MarshalPointer_Out(HandleRef pintValue, int stackGuard);

    [DllImport(@"HandleRefNative")]
    private static extern int TestNoGC(HandleRef pintValue, void* gcCallback);

    [DllImport(@"HandleRefNative")]
    private static extern HandleRef InvalidMarshalPointer_Return();

    // See matching values in HandleRefNative.cpp
    const int intManaged = 1000;
    const int intNative = 2000;
    const int intReturn = 3000;
    const int stackGuard = 5000;

    [Fact]
    [ActiveIssue("https://github.com/dotnet/runtime/issues/91388", typeof(TestLibrary.PlatformDetection), nameof(TestLibrary.PlatformDetection.PlatformDoesNotSupportNativeTestAssets))]
    public static void Validate_In()
    {
        int value = intManaged;
        int* pInt = &value;
        HandleRef hr = new HandleRef(new object(), (IntPtr)pInt);
        Assert.Equal(intReturn, MarshalPointer_In(hr, stackGuard));
        Assert.Equal(intManaged, value);
    }

    [Fact]
    [ActiveIssue("https://github.com/dotnet/runtime/issues/91388", typeof(TestLibrary.PlatformDetection), nameof(TestLibrary.PlatformDetection.PlatformDoesNotSupportNativeTestAssets))]
    public static void Validate_InOut()
    {
        int value = intManaged;
        int* pInt = &value;
        HandleRef hr = new HandleRef(new object(), (IntPtr)pInt);
        Assert.Equal(intReturn, MarshalPointer_InOut(hr, stackGuard));
        Assert.Equal(intNative, value);
    }

    [Fact]
    [ActiveIssue("https://github.com/dotnet/runtime/issues/91388", typeof(TestLibrary.PlatformDetection), nameof(TestLibrary.PlatformDetection.PlatformDoesNotSupportNativeTestAssets))]
    public static void Validate_Out()
    {
        int value = intManaged;
        int* pInt = &value;
        HandleRef hr = new HandleRef(new object(), (IntPtr)pInt);
        Assert.Equal(intReturn, MarshalPointer_Out(hr, stackGuard));
        Assert.Equal(intNative, value);
    }

    [Fact]
    [ActiveIssue("https://github.com/dotnet/runtime/issues/91388", typeof(TestLibrary.PlatformDetection), nameof(TestLibrary.PlatformDetection.PlatformDoesNotSupportNativeTestAssets))]
    public static void Validate_InvalidReturn()
    {
        Assert.Throws<MarshalDirectiveException>(() => InvalidMarshalPointer_Return());
    }

    [Fact]
    [ActiveIssue("https://github.com/dotnet/runtime/issues/91388", typeof(TestLibrary.PlatformDetection), nameof(TestLibrary.PlatformDetection.PlatformDoesNotSupportNativeTestAssets))]
    public static void Validate_NoGC()
    {
        int* pInt = (int*)RuntimeHelpers.AllocateTypeAssociatedMemory(typeof(CollectableClass), sizeof(int));
        *pInt = intManaged;
        HandleRef hr = CreateHandleRef(pInt);
        Assert.Equal(intReturn, TestNoGC(hr, (delegate* unmanaged<void>)&Callback));
        Assert.Equal(intManaged, *pInt);

        [UnmanagedCallersOnly]
        static void Callback()
        {
            Console.WriteLine("GC Callback 0");
            GC.Collect(2, GCCollectionMode.Forced);
            Console.WriteLine("GC Callback 1");
            GC.WaitForPendingFinalizers();
            Console.WriteLine("GC Callback 2");
            GC.Collect(2, GCCollectionMode.Forced);
            Console.WriteLine("GC Callback 3");
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static HandleRef CreateHandleRef(int* addr)
        {
            CollectableClass collectableClass = new(addr);
            return new HandleRef(collectableClass, (IntPtr)addr);
        }
    }

    /// <summary>
    /// Class that will change a pointer passed to native code when this class gets finalized.
    /// Native code can check whether the pointer changed during a P/Invoke
    /// </summary>
    class CollectableClass
    {
        int* PtrToChange;
        public CollectableClass(int* ptrToChange)
        {
            PtrToChange = ptrToChange;
        }

        ~CollectableClass()
        {
            Console.WriteLine("CollectableClass finalizer");
            int* ptr = PtrToChange;
            *ptr = Int32.MaxValue;
        }
    }
}
