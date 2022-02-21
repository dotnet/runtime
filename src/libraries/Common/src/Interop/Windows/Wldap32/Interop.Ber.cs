// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.DirectoryServices.Protocols;
using System.Runtime.InteropServices;

internal static partial class Interop
{
    internal static partial class Ldap
    {
        [GeneratedDllImport(Libraries.Wldap32, EntryPoint = "ber_free", CharSet = CharSet.Unicode)]
        [UnmanagedCallConv(CallConvs = new Type[] { typeof(System.Runtime.CompilerServices.CallConvCdecl) })]
        public static partial IntPtr ber_free(IntPtr berelement, int option);

        [GeneratedDllImport(Libraries.Wldap32, EntryPoint = "ber_alloc_t", CharSet = CharSet.Unicode)]
        [UnmanagedCallConv(CallConvs = new Type[] { typeof(System.Runtime.CompilerServices.CallConvCdecl) })]
        public static partial IntPtr ber_alloc(int option);

        [GeneratedDllImport(Libraries.Wldap32, EntryPoint = "ber_printf", CharSet = CharSet.Unicode)]
        [UnmanagedCallConv(CallConvs = new Type[] { typeof(System.Runtime.CompilerServices.CallConvCdecl) })]
        public static partial int ber_printf(SafeBerHandle berElement, string format, IntPtr value);

        [GeneratedDllImport(Libraries.Wldap32, EntryPoint = "ber_printf", CharSet = CharSet.Unicode)]
        [UnmanagedCallConv(CallConvs = new Type[] { typeof(System.Runtime.CompilerServices.CallConvCdecl) })]
        public static partial int ber_printf(SafeBerHandle berElement, string format, HGlobalMemHandle value, uint length);

        [GeneratedDllImport(Libraries.Wldap32, EntryPoint = "ber_printf", CharSet = CharSet.Unicode)]
        [UnmanagedCallConv(CallConvs = new Type[] { typeof(System.Runtime.CompilerServices.CallConvCdecl) })]
        public static partial int ber_printf(SafeBerHandle berElement, string format);

        [GeneratedDllImport(Libraries.Wldap32, EntryPoint = "ber_printf", CharSet = CharSet.Unicode)]
        [UnmanagedCallConv(CallConvs = new Type[] { typeof(System.Runtime.CompilerServices.CallConvCdecl) })]
        public static partial int ber_printf(SafeBerHandle berElement, string format, int value);

        [GeneratedDllImport(Libraries.Wldap32, EntryPoint = "ber_printf", CharSet = CharSet.Unicode)]
        [UnmanagedCallConv(CallConvs = new Type[] { typeof(System.Runtime.CompilerServices.CallConvCdecl) })]
        public static partial int ber_printf(SafeBerHandle berElement, string format, uint tag);

        [GeneratedDllImport(Libraries.Wldap32, EntryPoint = "ber_flatten", CharSet = CharSet.Unicode)]
        [UnmanagedCallConv(CallConvs = new Type[] { typeof(System.Runtime.CompilerServices.CallConvCdecl) })]
        public static partial int ber_flatten(SafeBerHandle berElement, ref IntPtr value);

        [GeneratedDllImport(Libraries.Wldap32, EntryPoint = "ber_init", CharSet = CharSet.Unicode)]
        [UnmanagedCallConv(CallConvs = new Type[] { typeof(System.Runtime.CompilerServices.CallConvCdecl) })]
        public static partial IntPtr ber_init(BerVal value);

        [GeneratedDllImport(Libraries.Wldap32, EntryPoint = "ber_scanf", CharSet = CharSet.Unicode)]
        [UnmanagedCallConv(CallConvs = new Type[] { typeof(System.Runtime.CompilerServices.CallConvCdecl) })]
        public static partial int ber_scanf(SafeBerHandle berElement, string format);

        [GeneratedDllImport(Libraries.Wldap32, EntryPoint = "ber_scanf", CharSet = CharSet.Unicode)]
        [UnmanagedCallConv(CallConvs = new Type[] { typeof(System.Runtime.CompilerServices.CallConvCdecl) })]
        public static partial int ber_scanf(SafeBerHandle berElement, string format, ref IntPtr ptrResult, ref uint bitLength);

        [GeneratedDllImport(Libraries.Wldap32, EntryPoint = "ber_scanf", CharSet = CharSet.Unicode)]
        [UnmanagedCallConv(CallConvs = new Type[] { typeof(System.Runtime.CompilerServices.CallConvCdecl) })]
        public static partial int ber_scanf(SafeBerHandle berElement, string format, ref int result);

        [GeneratedDllImport(Libraries.Wldap32, EntryPoint = "ber_scanf", CharSet = CharSet.Unicode)]
        [UnmanagedCallConv(CallConvs = new Type[] { typeof(System.Runtime.CompilerServices.CallConvCdecl) })]
        public static partial int ber_scanf(SafeBerHandle berElement, string format, ref IntPtr value);

        [GeneratedDllImport(Libraries.Wldap32, EntryPoint = "ber_bvfree", CharSet = CharSet.Unicode)]
        [UnmanagedCallConv(CallConvs = new Type[] { typeof(System.Runtime.CompilerServices.CallConvCdecl) })]
        public static partial int ber_bvfree(IntPtr value);

        [GeneratedDllImport(Libraries.Wldap32, EntryPoint = "ber_bvecfree", CharSet = CharSet.Unicode)]
        [UnmanagedCallConv(CallConvs = new Type[] { typeof(System.Runtime.CompilerServices.CallConvCdecl) })]
        public static partial int ber_bvecfree(IntPtr value);
    }
}
