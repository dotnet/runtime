// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Runtime.InteropServices;
using System.DirectoryServices.Protocols;

internal static partial class Interop
{
    [DllImport(Libraries.OpenLdap, EntryPoint = "ber_alloc_t", CharSet = CharSet.Ansi)]
    public static extern IntPtr ber_alloc(int option);

    [DllImport(Libraries.OpenLdap, EntryPoint = "ber_init", CharSet = CharSet.Ansi)]
    public static extern IntPtr ber_init(berval value);

    [DllImport(Libraries.OpenLdap, EntryPoint = "ber_free", CharSet = CharSet.Ansi)]
    public static extern IntPtr ber_free([In] IntPtr berelement, int option);

    [DllImport(Libraries.OpenLdap, EntryPoint = "ber_printf", CharSet = CharSet.Ansi)]
    public static extern int ber_printf_emptyarg(SafeBerHandle berElement, string format);

    [DllImport(Libraries.OpenLdap, EntryPoint = "ber_printf", CharSet = CharSet.Ansi)]
    public static extern int ber_printf_int(SafeBerHandle berElement, string format, int value);

    [DllImport(Libraries.OpenLdap, EntryPoint = "ber_printf", CharSet = CharSet.Ansi)]
    public static extern int ber_printf_bytearray(SafeBerHandle berElement, string format, HGlobalMemHandle value, int length);

    [DllImport(Libraries.OpenLdap, EntryPoint = "ber_printf", CharSet = CharSet.Ansi)]
    public static extern int ber_printf_berarray(SafeBerHandle berElement, string format, IntPtr value);

    [DllImport(Libraries.OpenLdap, EntryPoint = "ber_flatten", CharSet = CharSet.Ansi)]
    public static extern int ber_flatten(SafeBerHandle berElement, ref IntPtr value);

    [DllImport(Libraries.OpenLdap, EntryPoint = "ber_bvfree", CharSet = CharSet.Ansi)]
    public static extern int ber_bvfree(IntPtr value);

    [DllImport(Libraries.OpenLdap, EntryPoint = "ber_bvecfree", CharSet = CharSet.Ansi)]
    public static extern int ber_bvecfree(IntPtr value);

    [DllImport(Libraries.OpenLdap, EntryPoint = "ber_scanf", CharSet = CharSet.Ansi)]
    public static extern int ber_scanf(SafeBerHandle berElement, string format);

    [DllImport(Libraries.OpenLdap, EntryPoint = "ber_scanf", CharSet = CharSet.Ansi)]
    public static extern int ber_scanf_int(SafeBerHandle berElement, string format, ref int value);

    [DllImport(Libraries.OpenLdap, EntryPoint = "ber_scanf", CharSet = CharSet.Ansi)]
    public static extern int ber_scanf_bitstring(SafeBerHandle berElement, string format, ref IntPtr value, ref int bitLength);

    [DllImport(Libraries.OpenLdap, EntryPoint = "ber_scanf", CharSet = CharSet.Ansi)]
    public static extern int ber_scanf_ptr(SafeBerHandle berElement, string format, ref IntPtr value);
}
