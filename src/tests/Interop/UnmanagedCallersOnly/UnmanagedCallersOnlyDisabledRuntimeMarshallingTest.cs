// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Xunit;
using TestLibrary;

[assembly: DisableRuntimeMarshalling]

[ActiveIssue("https://github.com/dotnet/runtime/issues/91388", typeof(PlatformDetection), nameof(PlatformDetection.PlatformDoesNotSupportNativeTestAssets))]
public unsafe class Program
{
    private static class UnmanagedCallersOnlyDll
    {
        [DllImport(nameof(UnmanagedCallersOnlyDll))]
        public static extern int CallManagedProcReturningBool(IntPtr callbackProc, nuint firstArg);

        [DllImport(nameof(UnmanagedCallersOnlyDll))]
        public static extern int CallManagedProcReturningChar(IntPtr callbackProc, nuint firstArg);
    }

    [UnmanagedCallersOnly]
    private static bool ReturnFalse(nuint firstArg, IntPtr context) => false;

    [UnmanagedCallersOnly]
    private static char ReturnA(nuint firstArg, IntPtr context) => 'A';

    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvSwift)])]
    private static bool ReturnFalseSwift(nuint firstArg, IntPtr context) => false;

    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvSwift)])]
    private static char ReturnASwift(nuint firstArg, IntPtr context) => 'A';

    [Fact]
    public static void SmallIntegerReturnsAreZeroExtended()
    {
        nuint firstArg = unchecked((nuint)0x123456789ABCDEF0UL);

        Assert.Equal(0, UnmanagedCallersOnlyDll.CallManagedProcReturningBool((IntPtr)(delegate* unmanaged<nuint, IntPtr, bool>)&ReturnFalse, firstArg));
        Assert.Equal(1, UnmanagedCallersOnlyDll.CallManagedProcReturningChar((IntPtr)(delegate* unmanaged<nuint, IntPtr, char>)&ReturnA, firstArg));
    }

    [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.Is64BitProcess))]
    [PlatformSpecific(TestPlatforms.AnyApple)]
    public static void SmallIntegerSwiftReturnsAreZeroExtended()
    {
        nuint firstArg = unchecked((nuint)0x123456789ABCDEF0UL);

        Assert.Equal(0, UnmanagedCallersOnlyDll.CallManagedProcReturningBool((IntPtr)(delegate* unmanaged[Swift]<nuint, IntPtr, bool>)&ReturnFalseSwift, firstArg));
        Assert.Equal(1, UnmanagedCallersOnlyDll.CallManagedProcReturningChar((IntPtr)(delegate* unmanaged[Swift]<nuint, IntPtr, char>)&ReturnASwift, firstArg));
    }
}
