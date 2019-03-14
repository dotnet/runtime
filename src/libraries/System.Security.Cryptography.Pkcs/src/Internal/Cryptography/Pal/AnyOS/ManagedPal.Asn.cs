// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Buffers;
using System.Diagnostics;
using System.Security.Cryptography;
using System.Security.Cryptography.Asn1;
using System.Security.Cryptography.Pkcs.Asn1;

namespace Internal.Cryptography.Pal.AnyOS
{
    internal sealed partial class ManagedPkcsPal : PkcsPal
    {
        private static readonly byte[] s_invalidEmptyOid = { 0x06, 0x00 };

        public override byte[] EncodeOctetString(byte[] octets)
        {
            // Write using DER to support the most readers.
            using (AsnWriter writer = new AsnWriter(AsnEncodingRules.DER))
            {
                writer.WriteOctetString(octets);
                return writer.Encode();
            }
        }

        public override byte[] DecodeOctetString(byte[] encodedOctets)
        {
            // Read using BER because the CMS specification says the encoding is BER.
            AsnReader reader = new AsnReader(encodedOctets, AsnEncodingRules.BER);

            const int ArbitraryStackLimit = 256;
            Span<byte> tmp = stackalloc byte[ArbitraryStackLimit];
            // Use stackalloc 0 so data can later hold a slice of tmp.
            ReadOnlySpan<byte> data = stackalloc byte[0];
            byte[] poolBytes = null;

            try
            {
                if (!reader.TryReadPrimitiveOctetStringBytes(out var contents))
                {
                    if (reader.TryCopyOctetStringBytes(tmp, out int bytesWritten))
                    {
                        data = tmp.Slice(0, bytesWritten);
                    }
                    else
                    {
                        poolBytes = ArrayPool<byte>.Shared.Rent(reader.PeekContentBytes().Length);

                        if (!reader.TryCopyOctetStringBytes(poolBytes, out bytesWritten))
                        {
                            Debug.Fail("TryCopyOctetStringBytes failed with a provably-large-enough buffer");
                            throw new CryptographicException();
                        }

                        data = new ReadOnlySpan<byte>(poolBytes, 0, bytesWritten);
                    }
                }
                else
                {
                    data = contents.Span;
                }

                reader.ThrowIfNotEmpty();

                return data.ToArray();
            }
            finally
            {
                if (poolBytes != null)
                {
                    Array.Clear(poolBytes, 0, data.Length);
                    ArrayPool<byte>.Shared.Return(poolBytes);
                }
            }
        }

        public override byte[] EncodeUtcTime(DateTime utcTime)
        {
            const int maxLegalYear = 2049;
            // Write using DER to support the most readers.
            using (AsnWriter writer = new AsnWriter(AsnEncodingRules.DER))
            {
                try 
                {
                    // Sending the DateTime through ToLocalTime here will cause the right normalization
                    // of DateTimeKind.Unknown.
                    //
                    // Unknown => Local (adjust) => UTC (adjust "back", add Z marker; matches Windows)
                    if (utcTime.Kind == DateTimeKind.Unspecified)
                    {
                        writer.WriteUtcTime(utcTime.ToLocalTime(), maxLegalYear);
                    }
                    else
                    {
                        writer.WriteUtcTime(utcTime, maxLegalYear);
                    }

                    return writer.Encode();
                }
                catch (ArgumentException ex)
                {
                    throw new CryptographicException(ex.Message, ex);
                }
            }
        }

        public override DateTime DecodeUtcTime(byte[] encodedUtcTime)
        {
            // Read using BER because the CMS specification says the encoding is BER.
            AsnReader reader = new AsnReader(encodedUtcTime, AsnEncodingRules.BER);
            DateTimeOffset value = reader.ReadUtcTime();
            reader.ThrowIfNotEmpty();
            return value.UtcDateTime;
        }

        public override string DecodeOid(byte[] encodedOid)
        {
            // Windows compat.
            if (s_invalidEmptyOid.AsSpan().SequenceEqual(encodedOid))
            {
                return string.Empty;
            }

            // Read using BER because the CMS specification says the encoding is BER.
            AsnReader reader = new AsnReader(encodedOid, AsnEncodingRules.BER);
            string value = reader.ReadObjectIdentifierAsString();
            reader.ThrowIfNotEmpty();
            return value;
        }

        public override Oid GetEncodedMessageType(byte[] encodedMessage)
        {
            AsnReader reader = new AsnReader(encodedMessage, AsnEncodingRules.BER);

            ContentInfoAsn.Decode(reader, out ContentInfoAsn contentInfo);

            switch (contentInfo.ContentType)
            {
                case Oids.Pkcs7Data:
                case Oids.Pkcs7Signed:
                case Oids.Pkcs7Enveloped:
                case Oids.Pkcs7SignedEnveloped:
                case Oids.Pkcs7Hashed:
                case Oids.Pkcs7Encrypted:
                    return new Oid(contentInfo.ContentType);
            }

            throw new CryptographicException(SR.Cryptography_Cms_InvalidMessageType);
        }
    }
}
