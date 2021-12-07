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

#pragma warning disable DLLIMPORTGENANALYZER015 // Use 'GeneratedDllImportAttribute' instead of 'DllImportAttribute' to generate P/Invoke marshalling code at compile time
        // TODO: [DllImportGenerator] Requires some varargs support mechanism in generated interop
        [DllImport(Libraries.Wldap32, EntryPoint = "ber_printf", CharSet = CharSet.Unicode, CallingConvention = CallingConvention.Cdecl)]
        public static extern int ber_printf(SafeBerHandle berElement, string format, __arglist);
#pragma warning restore DLLIMPORTGENANALYZER015 // Use 'GeneratedDllImportAttribute' instead of 'DllImportAttribute' to generate P/Invoke marshalling code at compile time

        [GeneratedDllImport(Libraries.Wldap32, EntryPoint = "ber_flatten", CharSet = CharSet.Unicode)]
        [UnmanagedCallConv(CallConvs = new Type[] { typeof(System.Runtime.CompilerServices.CallConvCdecl) })]
        public static partial int ber_flatten(SafeBerHandle berElement, ref IntPtr value);

#pragma warning disable DLLIMPORTGENANALYZER015 // Use 'GeneratedDllImportAttribute' instead of 'DllImportAttribute' to generate P/Invoke marshalling code at compile time
        // TODO: [DllImportGenerator] Switch to use GeneratedDllImport once we support non-blittable structs.
        [DllImport(Libraries.Wldap32, EntryPoint = "ber_init", CharSet = CharSet.Unicode)]
        [UnmanagedCallConv(CallConvs = new Type[] { typeof(System.Runtime.CompilerServices.CallConvCdecl) })]
        public static extern IntPtr ber_init(berval value);
#pragma warning restore DLLIMPORTGENANALYZER015 // Use 'GeneratedDllImportAttribute' instead of 'DllImportAttribute' to generate P/Invoke marshalling code at compile time

#pragma warning disable DLLIMPORTGENANALYZER015 // Use 'GeneratedDllImportAttribute' instead of 'DllImportAttribute' to generate P/Invoke marshalling code at compile time
        // TODO: [DllImportGenerator] Requires some varargs support mechanism in generated interop
        [DllImport(Libraries.Wldap32, EntryPoint = "ber_scanf", CharSet = CharSet.Unicode, CallingConvention = CallingConvention.Cdecl)]
        public static extern int ber_scanf(SafeBerHandle berElement, string format, __arglist);
#pragma warning restore DLLIMPORTGENANALYZER015 // Use 'GeneratedDllImportAttribute' instead of 'DllImportAttribute' to generate P/Invoke marshalling code at compile time

        [GeneratedDllImport(Libraries.Wldap32, EntryPoint = "ber_bvfree", CharSet = CharSet.Unicode)]
        [UnmanagedCallConv(CallConvs = new Type[] { typeof(System.Runtime.CompilerServices.CallConvCdecl) })]
        public static partial int ber_bvfree(IntPtr value);

        [GeneratedDllImport(Libraries.Wldap32, EntryPoint = "ber_bvecfree", CharSet = CharSet.Unicode)]
        [UnmanagedCallConv(CallConvs = new Type[] { typeof(System.Runtime.CompilerServices.CallConvCdecl) })]
        public static partial int ber_bvecfree(IntPtr value);
    }
}
