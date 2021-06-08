// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Formats.Cbor
{
    /// <summary>Represents a CBOR semantic tag (major type 6).</summary>
    [CLSCompliant(false)]
    public enum CborTag : ulong
    {
        /// <summary>Tag value for RFC3339 date/time strings.</summary>
        DateTimeString = 0,

        /// <summary>Tag value for Epoch-based date/time strings.</summary>
        UnixTimeSeconds = 1,

        /// <summary>Tag value for unsigned bignum encodings.</summary>
        UnsignedBigNum = 2,

        /// <summary>Tag value for negative bignum encodings.</summary>
        NegativeBigNum = 3,

        /// <summary>Tag value for decimal fraction encodings.</summary>
        DecimalFraction = 4,

        /// <summary>Tag value for big float encodings.</summary>
        BigFloat = 5,

        /// <summary>Tag value for byte strings, meant for later encoding to a base64url string representation.</summary>
        Base64UrlLaterEncoding = 21,

        /// <summary>Tag value for byte strings, meant for later encoding to a base64 string representation.</summary>
        Base64StringLaterEncoding = 22,

        /// <summary>Tag value for byte strings, meant for later encoding to a base16 string representation.</summary>
        Base16StringLaterEncoding = 23,

        /// <summary>Tag value for byte strings containing embedded CBOR data item encodings.</summary>
        EncodedCborDataItem = 24,

        /// <summary>Tag value for Uri strings, as defined in RFC3986.</summary>
        Uri = 32,

        /// <summary>Tag value for base64url-encoded text strings, as defined in RFC4648.</summary>
        Base64Url = 33,

        /// <summary>Tag value for base64-encoded text strings, as defined in RFC4648.</summary>
        Base64 = 34,

        /// <summary>Tag value for regular expressions in Perl Compatible Regular Expressions / Javascript syntax.</summary>
        Regex = 35,

        /// <summary>Tag value for MIME messages (including all headers), as defined in RFC2045.</summary>
        MimeMessage = 36,

        /// <summary>Tag value for the Self-Describe CBOR header (0xd9d9f7).</summary>
        SelfDescribeCbor = 55799,
    }

    /// <summary>Represents a CBOR simple value (major type 7).</summary>
    public enum CborSimpleValue : byte
    {
        /// <summary>Represents the value 'false'.</summary>
        False = 20,

        /// <summary>Represents the value 'true'.</summary>
        True = 21,

        /// <summary>Represents the value 'null'.</summary>
        Null = 22,

        /// <summary>Represents an undefined value, to be used by an encoder as a substitute for a data item with an encoding problem.</summary>
        Undefined = 23,
    }
}
