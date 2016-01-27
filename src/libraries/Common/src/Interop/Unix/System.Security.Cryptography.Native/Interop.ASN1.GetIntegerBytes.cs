// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;

using Microsoft.Win32.SafeHandles;

internal static partial class Interop
{
    internal static partial class Crypto
    {
        [DllImport(Libraries.CryptoNative, EntryPoint = "CryptoNative_GetAsn1IntegerDerSize")]
        private static extern int GetAsn1IntegerDerSize(SafeSharedAsn1IntegerHandle i);

        [DllImport(Libraries.CryptoNative, EntryPoint = "CryptoNative_EncodeAsn1Integer")]
        private static extern int EncodeAsn1Integer(SafeSharedAsn1IntegerHandle i, byte[] buf);

        internal static byte[] GetAsn1IntegerBytes(SafeSharedAsn1IntegerHandle asn1Integer)
        {
            CheckValidOpenSslHandle(asn1Integer);

            // OpenSSL stores negative numbers in their two's complement (positive) form, but
            // sets an internal negative bit.
            //
            // If the number was positive, but could sign-test as negative, DER puts in a leading
            // 0x00 byte, which reading OpenSSL's data directly won't have.
            //
            // So to ensure we're getting a set of bytes compatible with BigInteger (though with the
            // wrong endianness here), DER encode it, then use the DER reader to skip past the tag
            // and length.
            byte[] derEncoded = OpenSslEncode(
                handle => GetAsn1IntegerDerSize(handle),
                (handle, buf) => EncodeAsn1Integer(handle, buf),
                asn1Integer);

            DerSequenceReader reader = DerSequenceReader.CreateForPayload(derEncoded);
            return reader.ReadIntegerBytes();
        }
    }
}
