// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.DirectoryServices.Protocols
{
    internal static class BerPal
    {
        internal static void FreeBervalArray(IntPtr ptrResult) => Interop.ber_bvecfree(ptrResult);

        internal static void FreeBerval(IntPtr flattenptr) => Interop.ber_bvfree(flattenptr);

        internal static void FreeBerElement(IntPtr berelement, int option) => Interop.ber_free(berelement, option);

        internal static int FlattenBerElement(SafeBerHandle berElement, ref IntPtr flattenptr) => Interop.ber_flatten(berElement, ref flattenptr);

        internal static int PrintBerArray(SafeBerHandle berElement, string format, IntPtr value) => Interop.ber_printf_berarray(berElement, format, value);

        internal static int PrintByteArray(SafeBerHandle berElement, string format, HGlobalMemHandle value, int length) => Interop.ber_printf_bytearray(berElement, format, value, length);

        internal static int PrintEmptyArgument(SafeBerHandle berElement, string format) => Interop.ber_printf_emptyarg(berElement, format);

        internal static int PrintInt(SafeBerHandle berElement, string format, int value) => Interop.ber_printf_int(berElement, format, value);

        internal static int ScanNext(SafeBerHandle berElement, string format) => Interop.ber_scanf(berElement, format);

        internal static int ScanNextBitString(SafeBerHandle berElement, string format, ref IntPtr ptrResult, ref int bitLength) => Interop.ber_scanf_bitstring(berElement, format, ref ptrResult, ref bitLength);

        internal static int ScanNextInt(SafeBerHandle berElement, string format, ref int result) => Interop.ber_scanf_int(berElement, format, ref result);

        internal static int ScanNextPtr(SafeBerHandle berElement, string format, ref IntPtr value) => Interop.ber_scanf_ptr(berElement, format, ref value);

        internal static bool IsBerDecodeError(int errorCode) => errorCode == -1;
    }
}
