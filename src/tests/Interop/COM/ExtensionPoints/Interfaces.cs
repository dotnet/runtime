// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.InteropServices;

namespace COM
{
    static class Ole32
    {
        [DllImport(nameof(Ole32), ExactSpelling = true)]
        public static extern int CoRegisterMallocSpy(IMallocSpy mallocSpy);

        [DllImport(nameof(Ole32), ExactSpelling = true)]
        public static extern int CoRevokeMallocSpy();
    }

    [ComImport]
    [Guid("0000001d-0000-0000-C000-000000000046")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    public unsafe interface IMallocSpy
    {
        [PreserveSig]
        nuint PreAlloc(
            nuint cbRequest);

        [PreserveSig]
        void* PostAlloc(
            void* pActual);

        [PreserveSig]
        void* PreFree(
            void* pRequest,
            [MarshalAs(UnmanagedType.Bool)] bool fSpyed);

        [PreserveSig]
        void PostFree(
            [MarshalAs(UnmanagedType.Bool)] bool fSpyed);

        [PreserveSig]
        nuint PreRealloc(
            void* pRequest,
            nuint cbRequest,
            void** ppNewRequest,
            [MarshalAs(UnmanagedType.Bool)] bool fSpyed);

        [PreserveSig]
        void* PostRealloc(
            void* pActual,
            [MarshalAs(UnmanagedType.Bool)] bool fSpyed);

        [PreserveSig]
        void* PreGetSize(
            void* pRequest,
            [MarshalAs(UnmanagedType.Bool)] bool fSpyed);

        [PreserveSig]
        nuint PostGetSize(
            nuint cbActual,
            [MarshalAs(UnmanagedType.Bool)] bool fSpyed);

        [PreserveSig]
        void* PreDidAlloc(
            void* pRequest,
            [MarshalAs(UnmanagedType.Bool)] bool fSpyed);

        [PreserveSig]
        int PostDidAlloc(
            void* pRequest,
            [MarshalAs(UnmanagedType.Bool)] bool fSpyed,
            int fActual);

        [PreserveSig]
        void PreHeapMinimize();

        [PreserveSig]
        void PostHeapMinimize();
    }
}