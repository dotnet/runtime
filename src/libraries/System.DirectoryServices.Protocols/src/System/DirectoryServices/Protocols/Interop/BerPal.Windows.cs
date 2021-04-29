// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.InteropServices;

namespace System.DirectoryServices.Protocols
{
    internal static class BerPal
    {
        internal static void FreeBervalArray(IntPtr ptrResult) => Interop.Ldap.ber_bvecfree(ptrResult);

        internal static void FreeBerval(IntPtr flattenptr) => Interop.Ldap.ber_bvfree(flattenptr);

        internal static void FreeBerElement(IntPtr berelement, int option) => Interop.Ldap.ber_free(berelement, option);

        internal static int FlattenBerElement(SafeBerHandle berElement, ref IntPtr flattenptr) => Interop.Ldap.ber_flatten(berElement, ref flattenptr);

        internal static int PrintBerArray(SafeBerHandle berElement, string format, IntPtr value, int tag) => Interop.Ldap.ber_printf(berElement, format, __arglist(value));

        internal static int PrintByteArray(SafeBerHandle berElement, string format, HGlobalMemHandle value, int length, int tag) => Interop.Ldap.ber_printf(berElement, format, __arglist(value, length));

        internal static int PrintEmptyArgument(SafeBerHandle berElement, string format, int tag) => Interop.Ldap.ber_printf(berElement, format, __arglist());

        internal static int PrintInt(SafeBerHandle berElement, string format, int value, int tag) => Interop.Ldap.ber_printf(berElement, format, __arglist(value));

        internal static int PrintTag(SafeBerHandle berElement, string format, int tag) => Interop.Ldap.ber_printf(berElement, format, __arglist(tag));

        internal static int ScanNext(SafeBerHandle berElement, string format) => Interop.Ldap.ber_scanf(berElement, format, __arglist());

        internal static int ScanNextBitString(SafeBerHandle berElement, string format, ref IntPtr ptrResult, ref int bitLength) => Interop.Ldap.ber_scanf(berElement, format, __arglist(ref ptrResult, ref bitLength));

        internal static int ScanNextInt(SafeBerHandle berElement, string format, ref int result) => Interop.Ldap.ber_scanf(berElement, format, __arglist(ref result));

        internal static int ScanNextPtr(SafeBerHandle berElement, string format, ref IntPtr value) => Interop.Ldap.ber_scanf(berElement, format, __arglist(ref value));

        internal static int ScanNextMultiByteArray(SafeBerHandle berElement, string format, ref IntPtr value) => Interop.Ldap.ber_scanf(berElement, format, __arglist(ref value));

        internal static bool IsBerDecodeError(int errorCode) => errorCode != 0;
    }
}
