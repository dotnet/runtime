// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.DirectoryServices.Protocols;
using System.Runtime.InteropServices;

internal static partial class Interop
{
    internal static partial class Ldap
    {
        [DllImport(Libraries.Wldap32, CallingConvention = CallingConvention.Cdecl, EntryPoint = "ber_free", CharSet = CharSet.Unicode)]
        public static extern IntPtr ber_free([In] IntPtr berelement, int option);

        [DllImport(Libraries.Wldap32, CallingConvention = CallingConvention.Cdecl, EntryPoint = "ber_alloc_t", CharSet = CharSet.Unicode)]
        public static extern IntPtr ber_alloc(int option);

        [DllImport(Libraries.DirectoryServicesNative, CallingConvention = CallingConvention.Cdecl, EntryPoint = "ber_printf_proxy_emptyarg", CharSet = CharSet.Unicode)]
        public static extern int ber_printf_proxy_emptyarg(SafeBerHandle berElement, string format);

        [DllImport(Libraries.DirectoryServicesNative, CallingConvention = CallingConvention.Cdecl, EntryPoint = "ber_printf_proxy_int", CharSet = CharSet.Unicode)]
        public static extern int ber_printf_proxy_int(SafeBerHandle berElement, string format, int value);

        [DllImport(Libraries.DirectoryServicesNative, CallingConvention = CallingConvention.Cdecl, EntryPoint = "ber_printf_proxy_bytearray", CharSet = CharSet.Unicode)]
        public static extern int ber_printf_proxy_bytearray(SafeBerHandle berElement, string format, HGlobalMemHandle value, int length);

        [DllImport(Libraries.DirectoryServicesNative, CallingConvention = CallingConvention.Cdecl, EntryPoint = "ber_printf_proxy_berarray", CharSet = CharSet.Unicode)]
        public static extern int ber_printf_proxy_berarray(SafeBerHandle berElement, string format, IntPtr value);

        [DllImport(Libraries.Wldap32, CallingConvention = CallingConvention.Cdecl, EntryPoint = "ber_flatten", CharSet = CharSet.Unicode)]
        public static extern int ber_flatten(SafeBerHandle berElement, ref IntPtr value);

        [DllImport(Libraries.Wldap32, CallingConvention = CallingConvention.Cdecl, EntryPoint = "ber_init", CharSet = CharSet.Unicode)]
        public static extern IntPtr ber_init(berval value);

        [DllImport(Libraries.DirectoryServicesNative, CallingConvention = CallingConvention.Cdecl, EntryPoint = "ber_scanf_proxy", CharSet = CharSet.Unicode)]
        public static extern int ber_scanf_proxy(SafeBerHandle berElement, string format);

        [DllImport(Libraries.DirectoryServicesNative, CallingConvention = CallingConvention.Cdecl, EntryPoint = "ber_scanf_proxy_int", CharSet = CharSet.Unicode)]
        public static extern int ber_scanf_proxy_int(SafeBerHandle berElement, string format, ref int value);

        [DllImport(Libraries.DirectoryServicesNative, CallingConvention = CallingConvention.Cdecl, EntryPoint = "ber_scanf_proxy_ptr", CharSet = CharSet.Unicode)]
        public static extern int ber_scanf_proxy_ptr(SafeBerHandle berElement, string format, ref IntPtr value);

        [DllImport(Libraries.DirectoryServicesNative, CallingConvention = CallingConvention.Cdecl, EntryPoint = "ber_scanf_proxy_bitstring", CharSet = CharSet.Unicode)]
        public static extern int ber_scanf_proxy_bitstring(SafeBerHandle berElement, string format, ref IntPtr value, ref int bitLength);

        [DllImport(Libraries.Wldap32, CallingConvention = CallingConvention.Cdecl, EntryPoint = "ber_bvfree", CharSet = CharSet.Unicode)]
        public static extern int ber_bvfree(IntPtr value);

        [DllImport(Libraries.Wldap32, CallingConvention = CallingConvention.Cdecl, EntryPoint = "ber_bvecfree", CharSet = CharSet.Unicode)]
        public static extern int ber_bvecfree(IntPtr value);
    }
}
