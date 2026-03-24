// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.DirectoryServices.Protocols;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

internal static partial class Interop
{
    internal static partial class Ldap
    {
        [RequiresUnsafe]
        [LibraryImport(Libraries.Wldap32, EntryPoint = "ber_free", StringMarshalling = StringMarshalling.Utf16)]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        public static partial IntPtr ber_free(IntPtr berelement, int option);

        [RequiresUnsafe]
        [LibraryImport(Libraries.Wldap32, EntryPoint = "ber_alloc_t", StringMarshalling = StringMarshalling.Utf16)]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        public static partial IntPtr ber_alloc(int option);

        [RequiresUnsafe]
        [LibraryImport(Libraries.Wldap32, EntryPoint = "ber_printf", StringMarshalling = StringMarshalling.Utf16)]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        public static partial int ber_printf(SafeBerHandle berElement, string format, IntPtr value);

        [RequiresUnsafe]
        [LibraryImport(Libraries.Wldap32, EntryPoint = "ber_printf", StringMarshalling = StringMarshalling.Utf16)]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        public static partial int ber_printf(SafeBerHandle berElement, string format, HGlobalMemHandle value, uint length);

        [RequiresUnsafe]
        [LibraryImport(Libraries.Wldap32, EntryPoint = "ber_printf", StringMarshalling = StringMarshalling.Utf16)]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        public static partial int ber_printf(SafeBerHandle berElement, string format);

        [RequiresUnsafe]
        [LibraryImport(Libraries.Wldap32, EntryPoint = "ber_printf", StringMarshalling = StringMarshalling.Utf16)]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        public static partial int ber_printf(SafeBerHandle berElement, string format, int value);

        [RequiresUnsafe]
        [LibraryImport(Libraries.Wldap32, EntryPoint = "ber_printf", StringMarshalling = StringMarshalling.Utf16)]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        public static partial int ber_printf(SafeBerHandle berElement, string format, uint tag);

        [RequiresUnsafe]
        [LibraryImport(Libraries.Wldap32, EntryPoint = "ber_flatten", StringMarshalling = StringMarshalling.Utf16)]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        public static partial int ber_flatten(SafeBerHandle berElement, ref IntPtr value);

        [RequiresUnsafe]
        [LibraryImport(Libraries.Wldap32, EntryPoint = "ber_init", StringMarshalling = StringMarshalling.Utf16)]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        public static partial IntPtr ber_init(BerVal value);

        [RequiresUnsafe]
        [LibraryImport(Libraries.Wldap32, EntryPoint = "ber_scanf", StringMarshalling = StringMarshalling.Utf16)]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        public static partial int ber_scanf(SafeBerHandle berElement, string format);

        [RequiresUnsafe]
        [LibraryImport(Libraries.Wldap32, EntryPoint = "ber_scanf", StringMarshalling = StringMarshalling.Utf16)]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        public static partial int ber_scanf(SafeBerHandle berElement, string format, ref IntPtr ptrResult, ref uint bitLength);

        [RequiresUnsafe]
        [LibraryImport(Libraries.Wldap32, EntryPoint = "ber_scanf", StringMarshalling = StringMarshalling.Utf16)]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        public static partial int ber_scanf(SafeBerHandle berElement, string format, ref int result);

        [RequiresUnsafe]
        [LibraryImport(Libraries.Wldap32, EntryPoint = "ber_scanf", StringMarshalling = StringMarshalling.Utf16)]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        public static partial int ber_scanf(SafeBerHandle berElement, string format, ref IntPtr value);

        [RequiresUnsafe]
        [LibraryImport(Libraries.Wldap32, EntryPoint = "ber_bvfree", StringMarshalling = StringMarshalling.Utf16)]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        public static partial int ber_bvfree(IntPtr value);

        [RequiresUnsafe]
        [LibraryImport(Libraries.Wldap32, EntryPoint = "ber_bvecfree", StringMarshalling = StringMarshalling.Utf16)]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        public static partial int ber_bvecfree(IntPtr value);
    }
}
