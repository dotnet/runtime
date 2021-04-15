// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.DirectoryServices.Protocols
{
    internal static class BerPal
    {
        internal static void FreeBervalArray(IntPtr ptrResult) => Interop.Ldap.ber_bvecfree(ptrResult);

        internal static void FreeBerval(IntPtr flattenptr) => Interop.Ldap.ber_bvfree(flattenptr);

        internal static void FreeBerElement(IntPtr berelement, int option) => Interop.Ldap.ber_free(berelement, option);

        internal static int FlattenBerElement(SafeBerHandle berElement, ref IntPtr flattenptr) => Interop.Ldap.ber_flatten(berElement, ref flattenptr);

        internal static int PrintBerArray(SafeBerHandle berElement, string format, IntPtr value, int tag) => Interop.Ldap.ber_printf_berarray(berElement, format, value, tag);

        internal static int PrintByteArray(SafeBerHandle berElement, string format, HGlobalMemHandle value, int length, int tag) => Interop.Ldap.ber_printf_bytearray(berElement, format, value, length, tag);

        internal static int PrintEmptyArgument(SafeBerHandle berElement, string format, int tag) => Interop.Ldap.ber_printf_emptyarg(berElement, format, tag);

        internal static int PrintInt(SafeBerHandle berElement, string format, int value, int tag) => Interop.Ldap.ber_printf_int(berElement, format, value, tag);

        internal static int PrintTag(SafeBerHandle berElement, string format, int tag) => Interop.Ldap.ber_printf_tag(berElement, format, tag);

        internal static int ScanNext(SafeBerHandle berElement, string format) => Interop.Ldap.ber_scanf_emptyarg(berElement, format);

        internal static int ScanNextBitString(SafeBerHandle berElement, string format, ref IntPtr ptrResult, ref int bitLength) => Interop.Ldap.ber_scanf_bitstring(berElement, format, ref ptrResult, ref bitLength);

        internal static int ScanNextInt(SafeBerHandle berElement, string format, ref int result) => Interop.Ldap.ber_scanf_int(berElement, format, ref result);

        internal static int ScanNextPtr(SafeBerHandle berElement, string format, ref IntPtr value) => Interop.Ldap.ber_scanf_ptr(berElement, format, ref value);

        internal static int ScanNextMultiByteArray(SafeBerHandle berElement, string format, ref IntPtr value) => Interop.Ldap.ber_scanf_multibytearray(berElement, format, ref value);

        internal static bool IsBerDecodeError(int errorCode) => errorCode == -1;
    }
}
