// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.InteropServices;
using System.Formats.Asn1;
using System.Security.Cryptography;
using Microsoft.Win32.SafeHandles;

internal static partial class Interop
{
    internal static partial class Crypto
    {
        [LibraryImport(Libraries.CryptoNative, EntryPoint = "CryptoNative_GetAsn1IntegerDerSize")]
        private static partial int GetAsn1IntegerDerSize(SafeSharedAsn1IntegerHandle i);

        [LibraryImport(Libraries.CryptoNative, EntryPoint = "CryptoNative_EncodeAsn1Integer")]
        private static partial int EncodeAsn1Integer(SafeSharedAsn1IntegerHandle i, byte[] buf);

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
                GetAsn1IntegerDerSize,
                EncodeAsn1Integer,
                asn1Integer);

            try
            {
                return AsnDecoder.ReadIntegerBytes(
                    derEncoded,
                    AsnEncodingRules.DER,
                    out _).ToArray();
            }
            catch (AsnContentException e)
            {
                throw new CryptographicException(SR.Cryptography_Der_Invalid_Encoding, e);
            }
        }
    }
}
