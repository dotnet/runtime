// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.InteropServices;

namespace Unity.CoreCLRHelpers;

static partial class CoreCLRHostNative
{
    // Most contents are source generated.  However, manually implemented methods can be added here

    public static void InitializeNative()
    {
        unsafe
        {
            mono_unity_initialize_host_apis(&InitCallback);
        }
    }

    [UnmanagedCallersOnly]
    static unsafe int InitCallback(IntPtr ptr, int size, IntPtr hostStructNativePtr, int hostStructNativeSize)
        => CoreCLRHost.InitMethod((HostStruct*)ptr, size, (HostStructNative*)hostStructNativePtr, hostStructNativeSize);

    [DllImport("coreclr", EntryPoint = nameof(mono_unity_initialize_host_apis), CharSet = CharSet.Unicode, ExactSpelling = true,
        CallingConvention = CallingConvention.Cdecl)]
    public unsafe static extern void mono_unity_initialize_host_apis(delegate* unmanaged<nint, int, nint, int, int> callback);
}
