// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading;
using Xunit;

public unsafe class Program
{
    // We test for mismatched callling convention by checking for a failure to
    // find the native entry point when the exported function is stdcall, but
    // the defined p/invoke is cdecl. This is only relevant on Windows x86.
    private static bool ValidateMismatch = OperatingSystem.IsWindows() && TestLibrary.Utilities.IsX86;

    private static void DefaultDllImport_Blittable()
    {
        Console.WriteLine($"Running {nameof(DefaultDllImport_Blittable)}...");

        const int a = 11;
        const int expected = a * 2;
        {
            Console.WriteLine($" -- default: UnmanagedCallConv()");
            int b;
            PInvokesCS.DefaultDllImport.Default.Blittable_Double_DefaultUnmanagedCallConv(a, &b);
            Assert.Equal(expected, b);
        }
        {
            Console.WriteLine($" -- cdecl: UnmanagedCallConv(cdecl)");
            int b;
            PInvokesCS.DefaultDllImport.Cdecl.Blittable_Double_CdeclUnmanagedCallConv(a, &b);
            Assert.Equal(expected, b);
        }
        {
            Console.WriteLine($" -- stdcall: UnmanagedCallConv(stdcall)");
            int b;
            PInvokesCS.DefaultDllImport.Stdcall.Blittable_Double_StdcallUnmanagedCallConv(a, &b);
            Assert.Equal(expected, b);
        }

        if (ValidateMismatch)
        {
            Console.WriteLine($" -- stdcall: UnmanagedCallConv(cdecl)");
            Assert.Throws<EntryPointNotFoundException>(() => PInvokesCS.DefaultDllImport.Stdcall.Blittable_Double_CdeclUnmanagedCallConv(a, null));
        }
    }

    private static void DefaultDllImport_NotBlittable()
    {
        Console.WriteLine($"Running {nameof(DefaultDllImport_NotBlittable)}...");

        const int a = 11;
        const int expected = a * 2;
        {
            Console.WriteLine($" -- default: UnmanagedCallConv()");
            int b;
            PInvokesCS.DefaultDllImport.Default.NotBlittable_Double_DefaultUnmanagedCallConv(a, &b);
            Assert.Equal(expected, b);
        }
        {
            Console.WriteLine($" -- cdecl: UnmanagedCallConv(cdecl)");
            int b;
            PInvokesCS.DefaultDllImport.Cdecl.NotBlittable_Double_CdeclUnmanagedCallConv(a, &b);
            Assert.Equal(expected, b);
        }
        {
            Console.WriteLine($" -- stdcall: UnmanagedCallConv(stdcall)");
            int b;
            PInvokesCS.DefaultDllImport.Stdcall.NotBlittable_Double_StdcallUnmanagedCallConv(a, &b);
            Assert.Equal(expected, b);
        }

        if (ValidateMismatch)
        {
            Console.WriteLine($" -- stdcall: UnmanagedCallConv(cdecl)");
            Assert.Throws<EntryPointNotFoundException>(() => PInvokesCS.DefaultDllImport.Stdcall.NotBlittable_Double_CdeclUnmanagedCallConv(a, null));
        }
    }

    private static void WinapiDllImport_Blittable()
    {
        Console.WriteLine($"Running {nameof(WinapiDllImport_Blittable)}...");

        const int a = 11;
        const int expected = a * 2;
        {
            Console.WriteLine($" -- default: UnmanagedCallConv()");
            int b;
            PInvokesCS.WinapiDllImport.Default.Blittable_Double_DefaultUnmanagedCallConv(a, &b);
            Assert.Equal(expected, b);
        }
        {
            Console.WriteLine($" -- cdecl: UnmanagedCallConv(cdecl)");
            int b;
            PInvokesCS.WinapiDllImport.Cdecl.Blittable_Double_CdeclUnmanagedCallConv(a, &b);
            Assert.Equal(expected, b);
        }
        {
            Console.WriteLine($" -- stdcall: UnmanagedCallConv(stdcall)");
            int b;
            PInvokesCS.WinapiDllImport.Stdcall.Blittable_Double_StdcallUnmanagedCallConv(a, &b);
            Assert.Equal(expected, b);
        }

        if (ValidateMismatch)
        {
            Console.WriteLine($" -- stdcall: UnmanagedCallConv(cdecl)");
            Assert.Throws<EntryPointNotFoundException>(() => PInvokesCS.WinapiDllImport.Stdcall.Blittable_Double_CdeclUnmanagedCallConv(a, null));
        }
    }

    private static void WinapiDllImport_NotBlittable()
    {
        Console.WriteLine($"Running {nameof(WinapiDllImport_NotBlittable)}...");

        const int a = 11;
        const int expected = a * 2;
        {
            Console.WriteLine($" -- default: UnmanagedCallConv()");
            int b;
            PInvokesCS.WinapiDllImport.Default.NotBlittable_Double_DefaultUnmanagedCallConv(a, &b);
            Assert.Equal(expected, b);
        }
        {
            Console.WriteLine($" -- cdecl: UnmanagedCallConv(cdecl)");
            int b;
            PInvokesCS.WinapiDllImport.Cdecl.NotBlittable_Double_CdeclUnmanagedCallConv(a, &b);
            Assert.Equal(expected, b);
        }
        {
            Console.WriteLine($" -- stdcall: UnmanagedCallConv(stdcall)");
            int b;
            PInvokesCS.WinapiDllImport.Stdcall.NotBlittable_Double_StdcallUnmanagedCallConv(a, &b);
            Assert.Equal(expected, b);
        }

        if (ValidateMismatch)
        {
            Console.WriteLine($" -- stdcall: UnmanagedCallConv(cdecl)");
            Assert.Throws<EntryPointNotFoundException>(() => PInvokesCS.WinapiDllImport.Stdcall.NotBlittable_Double_CdeclUnmanagedCallConv(a, null));
        }
    }

    private static void UnsetPInvokeImpl_Blittable()
    {
        Console.WriteLine($"Running {nameof(UnsetPInvokeImpl_Blittable)}...");

        const int a = 11;
        const int expected = a * 2;
        {
            Console.WriteLine($" -- default: UnmanagedCallConv()");
            int b;
            PInvokesIL.UnsetPInvokeImpl.Default.Blittable_Double_DefaultUnmanagedCallConv(a, &b);
            Assert.Equal(expected, b);
        }
        {
            Console.WriteLine($" -- cdecl: UnmanagedCallConv(cdecl)");
            int b;
            PInvokesIL.UnsetPInvokeImpl.Cdecl.Blittable_Double_CdeclUnmanagedCallConv(a, &b);
            Assert.Equal(expected, b);
        }
        {
            Console.WriteLine($" -- stdcall: UnmanagedCallConv(stdcall)");
            int b;
            PInvokesIL.UnsetPInvokeImpl.Stdcall.Blittable_Double_StdcallUnmanagedCallConv(a, &b);
            Assert.Equal(expected, b);
        }

        if (ValidateMismatch)
        {
            Console.WriteLine($" -- stdcall: UnmanagedCallConv(cdecl)");
            Assert.Throws<EntryPointNotFoundException>(() => PInvokesIL.UnsetPInvokeImpl.Stdcall.Blittable_Double_CdeclUnmanagedCallConv(a, null));
        }
    }

    private static void UnsetPInvokeImpl_NotBlittable()
    {
        Console.WriteLine($"Running {nameof(UnsetPInvokeImpl_NotBlittable)}...");

        const int a = 11;
        const int expected = a * 2;
        {
            Console.WriteLine($" -- default: UnmanagedCallConv()");
            int b;
            PInvokesIL.UnsetPInvokeImpl.Default.NotBlittable_Double_DefaultUnmanagedCallConv(a, &b);
            Assert.Equal(expected, b);
        }
        {
            Console.WriteLine($" -- cdecl: UnmanagedCallConv(cdecl)");
            int b;
            PInvokesIL.UnsetPInvokeImpl.Cdecl.NotBlittable_Double_CdeclUnmanagedCallConv(a, &b);
            Assert.Equal(expected, b);
        }
        {
            Console.WriteLine($" -- stdcall: UnmanagedCallConv(stdcall)");
            int b;
            PInvokesIL.UnsetPInvokeImpl.Stdcall.NotBlittable_Double_StdcallUnmanagedCallConv(a, &b);
            Assert.Equal(expected, b);
        }

        if (ValidateMismatch)
        {
            Console.WriteLine($" -- stdcall: UnmanagedCallConv(cdecl)");
            Assert.Throws<EntryPointNotFoundException>(() => PInvokesIL.UnsetPInvokeImpl.Stdcall.NotBlittable_Double_CdeclUnmanagedCallConv(a, null));
        }
    }

    private static void SuppressGCTransition_Blittable()
    {
        Console.WriteLine($"Running {nameof(SuppressGCTransition_Blittable)}...");

        const int a = 11;
        const int expected = a * 2;
        {
            Console.WriteLine($" -- default: SuppressGCTransition, UnmanagedCallConv()");
            int b;
            int ret = PInvokesCS.SuppressGCTransition.Default.Blittable_Double_DefaultUnmanagedCallConv_SuppressGCAttr(a, &b);
            Assert.Equal(expected, b);
            CheckGCMode.Validate(transitionSuppressed: true, ret);
        }
        {
            Console.WriteLine($" -- default: UnmanagedCallConv(suppressgctransition)");
            int b;
            int ret = PInvokesCS.SuppressGCTransition.Default.Blittable_Double_DefaultUnmanagedCallConv_SuppressGC(a, &b);
            Assert.Equal(expected, b);
            CheckGCMode.Validate(transitionSuppressed: true, ret);
        }
        {
            Console.WriteLine($" -- cdecl: SuppressGCTransition, UnmanagedCallConv(cdecl)");
            int b;
            int ret = PInvokesCS.SuppressGCTransition.Cdecl.Blittable_Double_CdeclUnmanagedCallConv_SuppressGCAttr(a, &b);
            Assert.Equal(expected, b);
            CheckGCMode.Validate(transitionSuppressed: true, ret);
        }
        {
            Console.WriteLine($" -- cdecl: UnmanagedCallConv(cdecl, suppressgctransition)");
            int b;
            int ret = PInvokesCS.SuppressGCTransition.Cdecl.Blittable_Double_CdeclUnmanagedCallConv_SuppressGC(a, &b);
            Assert.Equal(expected, b);
            CheckGCMode.Validate(transitionSuppressed: true, ret);
        }
        {
            Console.WriteLine($" -- stdcall: SuppressGCTransition, UnmanagedCallConv(stdcall)");
            int b;
            int ret = PInvokesCS.SuppressGCTransition.Stdcall.Blittable_Double_StdcallUnmanagedCallConv_SuppressGCAttr(a, &b);
            Assert.Equal(expected, b);
            CheckGCMode.Validate(transitionSuppressed: true, ret);
        }
        {
            Console.WriteLine($" -- stdcall: UnmanagedCallConv(stdcall, suppressgctransition)");
            int b;
            int ret = PInvokesCS.SuppressGCTransition.Stdcall.Blittable_Double_StdcallUnmanagedCallConv_SuppressGC(a, &b);
            Assert.Equal(expected, b);
            CheckGCMode.Validate(transitionSuppressed: true, ret);
        }
    }

    private static void SuppressGCTransition_NotBlittable()
    {
        Console.WriteLine($"Running {nameof(SuppressGCTransition_NotBlittable)}...");

        const int a = 11;
        const int expected = a * 2;
        {
            Console.WriteLine($" -- default: SuppressGCTransition, UnmanagedCallConv()");
            int b;
            bool ret = PInvokesCS.SuppressGCTransition.Default.NotBlittable_Double_DefaultUnmanagedCallConv_SuppressGCAttr(a, &b);
            Assert.Equal(expected, b);
            CheckGCMode.Validate(transitionSuppressed: true, ret);
        }
        {
            Console.WriteLine($" -- default: UnmanagedCallConv(suppressgctransition)");
            int b;
            bool ret = PInvokesCS.SuppressGCTransition.Default.NotBlittable_Double_DefaultUnmanagedCallConv_SuppressGC(a, &b);
            Assert.Equal(expected, b);
            CheckGCMode.Validate(transitionSuppressed: true, ret);
        }
        {
            Console.WriteLine($" -- cdecl: SuppressGCTransition, UnmanagedCallConv(cdecl)");
            int b;
            bool ret = PInvokesCS.SuppressGCTransition.Cdecl.NotBlittable_Double_CdeclUnmanagedCallConv_SuppressGCAttr(a, &b);
            Assert.Equal(expected, b);
            CheckGCMode.Validate(transitionSuppressed: true, ret);
        }
        {
            Console.WriteLine($" -- cdecl: UnmanagedCallConv(cdecl, suppressgctransition)");
            int b;
            bool ret = PInvokesCS.SuppressGCTransition.Cdecl.NotBlittable_Double_CdeclUnmanagedCallConv_SuppressGC(a, &b);
            Assert.Equal(expected, b);
            CheckGCMode.Validate(transitionSuppressed: true, ret);
        }
        {
            Console.WriteLine($" -- stdcall: SuppressGCTransition, UnmanagedCallConv(stdcall)");
            int b;
            bool ret = PInvokesCS.SuppressGCTransition.Stdcall.NotBlittable_Double_StdcallUnmanagedCallConv_SuppressGCAttr(a, &b);
            Assert.Equal(expected, b);
            CheckGCMode.Validate(transitionSuppressed: true, ret);
        }
        {
            Console.WriteLine($" -- stdcall: UnmanagedCallConv(stdcall, suppressgctransition)");
            int b;
            bool ret = PInvokesCS.SuppressGCTransition.Stdcall.NotBlittable_Double_StdcallUnmanagedCallConv_SuppressGC(a, &b);
            Assert.Equal(expected, b);
            CheckGCMode.Validate(transitionSuppressed: true, ret);
        }
    }

    private static void MatchingDllImport_Blittable()
    {
        Console.WriteLine($"Running {nameof(MatchingDllImport_Blittable)}...");

        // Calling convention is set in DllImport. UnmanagedCallConv should be ignored,
        const int a = 11;
        const int expected = a * 2;
        {
            // Should work despite the mismatched value in UnmanagedCallConv
            Console.WriteLine($" -- cdecl: UnmanagedCallConv(stdcall)");
            int b;
            PInvokesCS.MatchingDllImport.Cdecl.Blittable_Double_StdcallUnmanagedCallConv(a, &b);
            Assert.Equal(expected, b);
        }
        {
            // Should not suppress GC transition
            Console.WriteLine($" -- cdecl: UnmanagedCallConv(suppressgctransition)");
            int b;
            int ret = PInvokesCS.MatchingDllImport.Cdecl.Blittable_Double_SuppressGCUnmanagedCallConv(a, &b);
            Assert.Equal(expected, b);
            CheckGCMode.Validate(transitionSuppressed: false, ret);
        }
        {
            // Should work despite the mismatched value in UnmanagedCallConv
            Console.WriteLine($" -- stdcall: UnmanagedCallConv(cdecl)");
            int b;
            PInvokesCS.MatchingDllImport.Stdcall.Blittable_Double_CdeclUnmanagedCallConv(a, &b);
            Assert.Equal(expected, b);
        }
        {
            // Should not suppress GC transition
            Console.WriteLine($" -- stdcall: UnmanagedCallConv(suppressgctransition)");
            int b;
            int ret = PInvokesCS.MatchingDllImport.Stdcall.Blittable_Double_SuppressGCUnmanagedCallConv(a, &b);
            Assert.Equal(expected, b);
            CheckGCMode.Validate(transitionSuppressed: false, ret);
        }
    }

    private static void MatchingDllImport_NotBlittable()
    {
        Console.WriteLine($"Running {nameof(MatchingDllImport_NotBlittable)}...");

        // Calling convention is set in DllImport. UnmanagedCallConv should be ignored,
        const int a = 11;
        const int expected = a * 2;
        {
            // Should work despite the mismatched value in UnmanagedCallConv
            Console.WriteLine($" -- cdecl: UnmanagedCallConv(stdcall)");
            int b;
            PInvokesCS.MatchingDllImport.Cdecl.NotBlittable_Double_StdcallUnmanagedCallConv(a, &b);
            Assert.Equal(expected, b);
        }
        {
            // Should not suppress GC transition
            Console.WriteLine($" -- cdecl: UnmanagedCallConv(suppressgctransition)");
            int b;
            bool ret = PInvokesCS.MatchingDllImport.Cdecl.NotBlittable_Double_SuppressGCUnmanagedCallConv(a, &b);
            Assert.Equal(expected, b);
            CheckGCMode.Validate(transitionSuppressed: false, ret);
        }
        {
            // Should work despite the mismatched value in UnmanagedCallConv
            Console.WriteLine($" -- stdcall: UnmanagedCallConv(cdecl)");
            int b;
            PInvokesCS.MatchingDllImport.Stdcall.NotBlittable_Double_CdeclUnmanagedCallConv(a, &b);
            Assert.Equal(expected, b);
        }
        {
            // Should not suppress GC transition
            Console.WriteLine($" -- stdcall: UnmanagedCallConv(suppressgctransition)");
            int b;
            bool ret = PInvokesCS.MatchingDllImport.Stdcall.NotBlittable_Double_SuppressGCUnmanagedCallConv(a, &b);
            Assert.Equal(expected, b);
            CheckGCMode.Validate(transitionSuppressed: false, ret);
        }
    }

    [Fact]
    [ActiveIssue("https://github.com/dotnet/runtime/issues/91388", typeof(TestLibrary.PlatformDetection), nameof(TestLibrary.PlatformDetection.PlatformDoesNotSupportNativeTestAssets))]
    public static int TestEntryPoint()
    {
        try
        {
            DefaultDllImport_Blittable();
            DefaultDllImport_NotBlittable();
            WinapiDllImport_Blittable();
            WinapiDllImport_NotBlittable();
            UnsetPInvokeImpl_Blittable();
            UnsetPInvokeImpl_NotBlittable();

            // Following tests explicitly check GC mode when possible
            CheckGCMode.Initialize(&PInvokesCS.SetIsInCooperativeModeFunction);
            SuppressGCTransition_Blittable();
            SuppressGCTransition_NotBlittable();
            MatchingDllImport_Blittable();
            MatchingDllImport_NotBlittable();

        }
        catch (Exception e)
        {
            Console.WriteLine($"Test Failure: {e}");
            return 101;
        }

        return 100;
    }
}
