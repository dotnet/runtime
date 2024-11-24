// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics;
using System.Formats.Asn1;
using System.Security.Cryptography;

namespace Internal.Cryptography
{
    internal static partial class Helpers
    {
        internal static ReadOnlyMemory<byte> DecodeOctetStringAsMemory(ReadOnlyMemory<byte> encodedOctetString)
        {
            try
            {
                ReadOnlySpan<byte> input = encodedOctetString.Span;

                if (AsnDecoder.TryReadPrimitiveOctetString(
                    input,
                    AsnEncodingRules.BER,
                    out ReadOnlySpan<byte> primitive,
                    out int consumed))
                {
                    if (consumed != input.Length)
                    {
                        throw new CryptographicException(SR.Cryptography_Der_Invalid_Encoding);
                    }

                    if (input.Overlaps(primitive, out int offset))
                    {
                        return encodedOctetString.Slice(offset, primitive.Length);
                    }

                    Debug.Fail("input.Overlaps(primitive) failed after TryReadPrimitiveOctetString succeeded");
                }

                byte[] ret = AsnDecoder.ReadOctetString(input, AsnEncodingRules.BER, out consumed);

                if (consumed != input.Length)
                {
                    throw new CryptographicException(SR.Cryptography_Der_Invalid_Encoding);
                }

                return ret;
            }
            catch (AsnContentException e)
            {
                throw new CryptographicException(SR.Cryptography_Der_Invalid_Encoding, e);
            }
        }
    }
}
