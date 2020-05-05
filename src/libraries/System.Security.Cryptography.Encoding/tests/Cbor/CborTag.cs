// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Formats.Cbor
{
    // https://tools.ietf.org/html/rfc7049#section-2.4
    public enum CborTag : ulong
    {
        DateTimeString = 0,
        UnixTimeSeconds = 1,
        UnsignedBigNum = 2,
        NegativeBigNum = 3,
        DecimalFraction = 4,
        BigFloat = 5,

        Base64UrlLaterEncoding = 21,
        Base64StringLaterEncoding = 22,
        Base16StringLaterEncoding = 23,
        EncodedCborDataItem = 24,

        Uri = 32,
        Base64Url = 33,
        Base64 = 34,
        Regex = 35,
        MimeMessage = 36,

        SelfDescribingCbor = 55799,
    }

    // https://tools.ietf.org/html/rfc7049#section-2.3
    public enum CborSimpleValue : byte
    {
        False = 20,
        True = 21,
        Null = 22,
        Undefined = 23,
    }
}
