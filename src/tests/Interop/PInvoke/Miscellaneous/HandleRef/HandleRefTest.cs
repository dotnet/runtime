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
    [SkipOnCoreClr("WaitForPendingFinalizers() not supported with GCStress", RuntimeTestModes.AnyGCStress)]
    [ActiveIssue("https://github.com/dotnet/runtime/issues/91388", typeof(TestLibrary.PlatformDetection), nameof(TestLibrary.PlatformDetection.PlatformDoesNotSupportNativeTestAssets))]
    public static void Validate_NoGC()
    {
        HandleRef hr = CreateHandleRef();
        Assert.Equal(intReturn, TestNoGC(hr, (delegate* unmanaged<void>)&Callback));

        [UnmanagedCallersOnly]
        static void Callback()
        {
            Console.WriteLine("GC Callback Begin");
            GC.Collect(2, GCCollectionMode.Forced);
            Console.WriteLine("GC Callback before WaitForPendingFinalizers()");
            GC.WaitForPendingFinalizers();
            Console.WriteLine("GC Callback after WaitForPendingFinalizers()");
            GC.Collect(2, GCCollectionMode.Forced);
            Console.WriteLine("GC Callback End");
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static HandleRef CreateHandleRef()
        {
            // We don't free this memory so we don't have to worry
            // about a race with CollectableClass's finalizer.
            int* pInt = (int*)NativeMemory.Alloc(sizeof(int));
            *pInt = intManaged;
            CollectableClass collectableClass = new(pInt);
            return new HandleRef(collectableClass, (IntPtr)pInt);
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
            int* ptr = PtrToChange;
            *ptr = Int32.MaxValue;
        }
    }
}
