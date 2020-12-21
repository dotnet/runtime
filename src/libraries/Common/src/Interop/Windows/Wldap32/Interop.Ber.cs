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

        [DllImport(Libraries.Wldap32, CallingConvention = CallingConvention.Cdecl, EntryPoint = "ber_printf", CharSet = CharSet.Unicode)]
        public static extern int ber_printf_emptyarg(SafeBerHandle berElement, string format);

        [DllImport(Libraries.Wldap32, CallingConvention = CallingConvention.Cdecl, EntryPoint = "ber_printf", CharSet = CharSet.Unicode)]
        public static extern int ber_printf_int(SafeBerHandle berElement, string format, int value);

        [DllImport(Libraries.Wldap32, CallingConvention = CallingConvention.Cdecl, EntryPoint = "ber_printf", CharSet = CharSet.Unicode)]
        public static extern int ber_printf_bytearray(SafeBerHandle berElement, string format, HGlobalMemHandle value, int length);

        [DllImport(Libraries.Wldap32, CallingConvention = CallingConvention.Cdecl, EntryPoint = "ber_printf", CharSet = CharSet.Unicode)]
        public static extern int ber_printf_berarray(SafeBerHandle berElement, string format, IntPtr value);

        [DllImport(Libraries.Wldap32, CallingConvention = CallingConvention.Cdecl, EntryPoint = "ber_flatten", CharSet = CharSet.Unicode)]
        public static extern int ber_flatten(SafeBerHandle berElement, ref IntPtr value);

        [DllImport(Libraries.Wldap32, CallingConvention = CallingConvention.Cdecl, EntryPoint = "ber_init", CharSet = CharSet.Unicode)]
        public static extern IntPtr ber_init(berval value);

        [DllImport(Libraries.Wldap32, CallingConvention = CallingConvention.Cdecl, EntryPoint = "ber_scanf", CharSet = CharSet.Unicode)]
        public static extern int ber_scanf(SafeBerHandle berElement, string format);

        [DllImport(Libraries.Wldap32, CallingConvention = CallingConvention.Cdecl, EntryPoint = "ber_scanf", CharSet = CharSet.Unicode)]
        public static extern int ber_scanf_int(SafeBerHandle berElement, string format, ref int value);

        [DllImport(Libraries.Wldap32, CallingConvention = CallingConvention.Cdecl, EntryPoint = "ber_scanf", CharSet = CharSet.Unicode)]
        public static extern int ber_scanf_ptr(SafeBerHandle berElement, string format, ref IntPtr value);

        [DllImport(Libraries.Wldap32, CallingConvention = CallingConvention.Cdecl, EntryPoint = "ber_scanf", CharSet = CharSet.Unicode)]
        public static extern int ber_scanf_bitstring(SafeBerHandle berElement, string format, ref IntPtr value, ref int bitLength);

        [DllImport(Libraries.Wldap32, CallingConvention = CallingConvention.Cdecl, EntryPoint = "ber_bvfree", CharSet = CharSet.Unicode)]
        public static extern int ber_bvfree(IntPtr value);

        [DllImport(Libraries.Wldap32, CallingConvention = CallingConvention.Cdecl, EntryPoint = "ber_bvecfree", CharSet = CharSet.Unicode)]
        public static extern int ber_bvecfree(IntPtr value);
    }
}
