// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading;
using Xunit;

public unsafe static class PInvokesCS
{
    private const string UnmanagedCallConvNative = nameof(UnmanagedCallConvNative);

    private const string Double_Default = nameof(Double_Default);
    private const string Double_Cdecl = nameof(Double_Cdecl);
    private const string Double_Stdcall = nameof(Double_Stdcall);

    private const string Invert_Stdcall = nameof(Invert_Stdcall);

    [DllImport(nameof(UnmanagedCallConvNative))]
    public static extern unsafe void SetIsInCooperativeModeFunction(delegate* unmanaged<int> fn);

    public static class DefaultDllImport
    {
        public static class Default
        {
            [UnmanagedCallConv]
            [DllImport(nameof(UnmanagedCallConvNative), EntryPoint = Double_Default)]
            public static extern int Blittable_Double_DefaultUnmanagedCallConv(int a, int* b);

            [UnmanagedCallConv]
            [DllImport(nameof(UnmanagedCallConvNative), EntryPoint = Double_Default)]
            public static extern bool NotBlittable_Double_DefaultUnmanagedCallConv(int a, int* b);
        }

        public static class Cdecl
        {
            [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
            [DllImport(nameof(UnmanagedCallConvNative), EntryPoint = Double_Cdecl)]
            public static extern int Blittable_Double_CdeclUnmanagedCallConv(int a, int* b);

            [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
            [DllImport(nameof(UnmanagedCallConvNative), EntryPoint = Double_Cdecl)]
            public static extern bool NotBlittable_Double_CdeclUnmanagedCallConv(int a, int* b);
        }

        public static class Stdcall
        {
            // Mismatch
            [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
            [DllImport(nameof(UnmanagedCallConvNative), EntryPoint = Double_Stdcall)]
            public static extern int Blittable_Double_CdeclUnmanagedCallConv(int a, int* b);

            [UnmanagedCallConv(CallConvs = [typeof(CallConvStdcall)])]
            [DllImport(nameof(UnmanagedCallConvNative), EntryPoint = Double_Stdcall)]
            public static extern int Blittable_Double_StdcallUnmanagedCallConv(int a, int* b);

            // Mismatch
            [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
            [DllImport(nameof(UnmanagedCallConvNative), EntryPoint = Double_Stdcall)]
            public static extern bool NotBlittable_Double_CdeclUnmanagedCallConv(int a, int* b);

            [UnmanagedCallConv(CallConvs = [typeof(CallConvStdcall)])]
            [DllImport(nameof(UnmanagedCallConvNative), EntryPoint = Double_Stdcall)]
            public static extern bool NotBlittable_Double_StdcallUnmanagedCallConv(int a, int* b);
        }
    }

    public static class WinapiDllImport
    {
        public static class Default
        {
            [UnmanagedCallConv]
            [DllImport(nameof(UnmanagedCallConvNative), EntryPoint = Double_Default, CallingConvention = CallingConvention.Winapi)]
            public static extern int Blittable_Double_DefaultUnmanagedCallConv(int a, int* b);

            [UnmanagedCallConv]
            [DllImport(nameof(UnmanagedCallConvNative), EntryPoint = Double_Default, CallingConvention = CallingConvention.Winapi)]
            public static extern bool NotBlittable_Double_DefaultUnmanagedCallConv(int a, int* b);
        }

        public static class Cdecl
        {
            [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
            [DllImport(nameof(UnmanagedCallConvNative), EntryPoint = Double_Cdecl, CallingConvention = CallingConvention.Winapi)]
            public static extern int Blittable_Double_CdeclUnmanagedCallConv(int a, int* b);

            [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
            [DllImport(nameof(UnmanagedCallConvNative), EntryPoint = Double_Cdecl, CallingConvention = CallingConvention.Winapi)]
            public static extern bool NotBlittable_Double_CdeclUnmanagedCallConv(int a, int* b);
        }

        public static class Stdcall
        {
            // Mismatch
            [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
            [DllImport(nameof(UnmanagedCallConvNative), EntryPoint = Double_Stdcall, CallingConvention = CallingConvention.Winapi)]
            public static extern int Blittable_Double_CdeclUnmanagedCallConv(int a, int* b);

            [UnmanagedCallConv(CallConvs = [typeof(CallConvStdcall)])]
            [DllImport(nameof(UnmanagedCallConvNative), EntryPoint = Double_Stdcall, CallingConvention = CallingConvention.Winapi)]
            public static extern int Blittable_Double_StdcallUnmanagedCallConv(int a, int* b);

            // Mismatch
            [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
            [DllImport(nameof(UnmanagedCallConvNative), EntryPoint = Double_Stdcall, CallingConvention = CallingConvention.Winapi)]
            public static extern bool NotBlittable_Double_CdeclUnmanagedCallConv(int a, int* b);

            [UnmanagedCallConv(CallConvs = [typeof(CallConvStdcall)])]
            [DllImport(nameof(UnmanagedCallConvNative), EntryPoint = Double_Stdcall, CallingConvention = CallingConvention.Winapi)]
            public static extern bool NotBlittable_Double_StdcallUnmanagedCallConv(int a, int* b);
        }
    }

    public static class SuppressGCTransition
    {
        public static class Default
        {
            [SuppressGCTransition]
            [UnmanagedCallConv]
            [DllImport(nameof(UnmanagedCallConvNative), EntryPoint = Double_Default)]
            public static extern int Blittable_Double_DefaultUnmanagedCallConv_SuppressGCAttr(int a, int* b);

            [UnmanagedCallConv(CallConvs = [typeof(CallConvSuppressGCTransition)])]
            [DllImport(nameof(UnmanagedCallConvNative), EntryPoint = Double_Default)]
            public static extern int Blittable_Double_DefaultUnmanagedCallConv_SuppressGC(int a, int* b);

            [SuppressGCTransition]
            [UnmanagedCallConv]
            [DllImport(nameof(UnmanagedCallConvNative), EntryPoint = Double_Default)]
            public static extern bool NotBlittable_Double_DefaultUnmanagedCallConv_SuppressGCAttr(int a, int* b);

            [UnmanagedCallConv(CallConvs = [typeof(CallConvSuppressGCTransition)])]
            [DllImport(nameof(UnmanagedCallConvNative), EntryPoint = Double_Default)]
            public static extern bool NotBlittable_Double_DefaultUnmanagedCallConv_SuppressGC(int a, int* b);
        }

        public static class Cdecl
        {
            [SuppressGCTransition]
            [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
            [DllImport(nameof(UnmanagedCallConvNative), EntryPoint = Double_Cdecl)]
            public static extern int Blittable_Double_CdeclUnmanagedCallConv_SuppressGCAttr(int a, int* b);

            [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl), typeof(CallConvSuppressGCTransition)])]
            [DllImport(nameof(UnmanagedCallConvNative), EntryPoint = Double_Cdecl)]
            public static extern int Blittable_Double_CdeclUnmanagedCallConv_SuppressGC(int a, int* b);

            [SuppressGCTransition]
            [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
            [DllImport(nameof(UnmanagedCallConvNative), EntryPoint = Double_Cdecl)]
            public static extern bool NotBlittable_Double_CdeclUnmanagedCallConv_SuppressGCAttr(int a, int* b);

            [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl), typeof(CallConvSuppressGCTransition)])]
            [DllImport(nameof(UnmanagedCallConvNative), EntryPoint = Double_Cdecl)]
            public static extern bool NotBlittable_Double_CdeclUnmanagedCallConv_SuppressGC(int a, int* b);
        }

        public static class Stdcall
        {
            [SuppressGCTransition]
            [UnmanagedCallConv(CallConvs = [typeof(CallConvStdcall)])]
            [DllImport(nameof(UnmanagedCallConvNative), EntryPoint = Double_Stdcall)]
            public static extern int Blittable_Double_StdcallUnmanagedCallConv_SuppressGCAttr(int a, int* b);

            [UnmanagedCallConv(CallConvs = [typeof(CallConvStdcall), typeof(CallConvSuppressGCTransition)])]
            [DllImport(nameof(UnmanagedCallConvNative), EntryPoint = Double_Stdcall)]
            public static extern int Blittable_Double_StdcallUnmanagedCallConv_SuppressGC(int a, int* b);

            [SuppressGCTransition]
            [UnmanagedCallConv(CallConvs = [typeof(CallConvStdcall)])]
            [DllImport(nameof(UnmanagedCallConvNative), EntryPoint = Double_Stdcall)]
            public static extern bool NotBlittable_Double_StdcallUnmanagedCallConv_SuppressGCAttr(int a, int* b);

            [UnmanagedCallConv(CallConvs = [typeof(CallConvStdcall), typeof(CallConvSuppressGCTransition)])]
            [DllImport(nameof(UnmanagedCallConvNative), EntryPoint = Double_Stdcall)]
            public static extern bool NotBlittable_Double_StdcallUnmanagedCallConv_SuppressGC(int a, int* b);
        }
    }

    public static class MatchingDllImport
    {
        public static class Cdecl
        {
            // UnmanagedCallConv should not be used
            [UnmanagedCallConv(CallConvs = [typeof(CallConvStdcall)])]
            [DllImport(nameof(UnmanagedCallConvNative), EntryPoint = Double_Cdecl, CallingConvention = CallingConvention.Cdecl)]
            public static extern int Blittable_Double_StdcallUnmanagedCallConv(int a, int* b);

            // UnmanagedCallConv should not be used
            [UnmanagedCallConv(CallConvs = [typeof(CallConvSuppressGCTransition)])]
            [DllImport(nameof(UnmanagedCallConvNative), EntryPoint = Double_Cdecl, CallingConvention = CallingConvention.Cdecl)]
            public static extern int Blittable_Double_SuppressGCUnmanagedCallConv(int a, int* b);

            // UnmanagedCallConv should not be used
            [UnmanagedCallConv(CallConvs = [typeof(CallConvStdcall)])]
            [DllImport(nameof(UnmanagedCallConvNative), EntryPoint = Double_Cdecl, CallingConvention = CallingConvention.Cdecl)]
            public static extern bool NotBlittable_Double_StdcallUnmanagedCallConv(int a, int* b);

            // UnmanagedCallConv should not be used
            [UnmanagedCallConv(CallConvs = [typeof(CallConvSuppressGCTransition)])]
            [DllImport(nameof(UnmanagedCallConvNative), EntryPoint = Double_Cdecl, CallingConvention = CallingConvention.Cdecl)]
            public static extern bool NotBlittable_Double_SuppressGCUnmanagedCallConv(int a, int* b);
        }

        public static class Stdcall
        {
            // UnmanagedCallConv should not be used
            [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
            [DllImport(nameof(UnmanagedCallConvNative), EntryPoint = Double_Stdcall, CallingConvention = CallingConvention.StdCall)]
            public static extern int Blittable_Double_CdeclUnmanagedCallConv(int a, int* b);

            // UnmanagedCallConv should not be used
            [UnmanagedCallConv(CallConvs = [typeof(CallConvSuppressGCTransition)])]
            [DllImport(nameof(UnmanagedCallConvNative), EntryPoint = Double_Stdcall, CallingConvention = CallingConvention.StdCall)]
            public static extern int Blittable_Double_SuppressGCUnmanagedCallConv(int a, int* b);

            // UnmanagedCallConv should not be used
            [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
            [DllImport(nameof(UnmanagedCallConvNative), EntryPoint = Double_Stdcall, CallingConvention = CallingConvention.StdCall)]
            public static extern bool NotBlittable_Double_CdeclUnmanagedCallConv(int a, int* b);

            // UnmanagedCallConv should not be used
            [UnmanagedCallConv(CallConvs = [typeof(CallConvSuppressGCTransition)])]
            [DllImport(nameof(UnmanagedCallConvNative), EntryPoint = Double_Stdcall, CallingConvention = CallingConvention.StdCall)]
            public static extern bool NotBlittable_Double_SuppressGCUnmanagedCallConv(int a, int* b);
        }
    }
}
