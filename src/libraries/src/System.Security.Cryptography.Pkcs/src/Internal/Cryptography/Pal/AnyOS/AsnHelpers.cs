// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Diagnostics;
using System.Security.Cryptography;
using System.Security.Cryptography.Asn1;
using System.Security.Cryptography.Pkcs;
using System.Security.Cryptography.Pkcs.Asn1;
using System.Security.Cryptography.X509Certificates;
using System.Security.Cryptography.Xml;

namespace Internal.Cryptography.Pal.AnyOS
{
    internal static class AsnHelpers
    {
        internal static SubjectIdentifierOrKey ToSubjectIdentifierOrKey(
            this OriginatorIdentifierOrKeyAsn originator)
        {
            if (originator.IssuerAndSerialNumber != null)
            {
                var name = new X500DistinguishedName(originator.IssuerAndSerialNumber.Value.Issuer.ToArray());

                return new SubjectIdentifierOrKey(
                    SubjectIdentifierOrKeyType.IssuerAndSerialNumber,
                    new X509IssuerSerial(
                        name.Name,
                        originator.IssuerAndSerialNumber.Value.SerialNumber.Span.ToBigEndianHex()));
            }

            if (originator.SubjectKeyIdentifier != null)
            {
                return new SubjectIdentifierOrKey(
                    SubjectIdentifierOrKeyType.SubjectKeyIdentifier,
                    originator.SubjectKeyIdentifier.Value.Span.ToBigEndianHex());
            }

            if (originator.OriginatorKey != null)
            {
                OriginatorPublicKeyAsn originatorKey = originator.OriginatorKey;

                return new SubjectIdentifierOrKey(
                    SubjectIdentifierOrKeyType.PublicKeyInfo,
                    new PublicKeyInfo(
                        originatorKey.Algorithm.ToPresentationObject(),
                        originatorKey.PublicKey.ToArray()));
            }

            Debug.Fail("Unknown SubjectIdentifierOrKey state");
            return new SubjectIdentifierOrKey(SubjectIdentifierOrKeyType.Unknown, String.Empty);
        }

        internal static AlgorithmIdentifier ToPresentationObject(this AlgorithmIdentifierAsn asn)
        {
            int keyLength;

            switch (asn.Algorithm.Value)
            {
                case Oids.Rc2Cbc:
                    {
                        if (asn.Parameters == null)
                        {
                            keyLength = 0;
                            break;
                        }

                        Rc2CbcParameters rc2Params = AsnSerializer.Deserialize<Rc2CbcParameters>(
                            asn.Parameters.Value,
                            AsnEncodingRules.BER);

                        int keySize = rc2Params.GetEffectiveKeyBits();

                        // These are the only values .NET Framework would set.
                        switch (keySize)
                        {
                            case 40:
                            case 56:
                            case 64:
                            case 128:
                                keyLength = keySize;
                                break;
                            default:
                                keyLength = 0;
                                break;
                        }

                        break;
                    }
                case Oids.Rc4:
                    {
                        if (asn.Parameters == null)
                        {
                            keyLength = 0;
                            break;
                        }

                        int saltLen = 0;
                        AsnReader reader = new AsnReader(asn.Parameters.Value, AsnEncodingRules.BER);

                        // DER NULL is considered the same as not present.
                        // No call to ReadNull() is necessary because the serializer already verified that
                        // there's no data after the [AnyValue] value.
                        if (reader.PeekTag() != Asn1Tag.Null)
                        {
                            if (reader.TryGetPrimitiveOctetStringBytes(out ReadOnlyMemory<byte> contents))
                            {
                                saltLen = contents.Length;
                            }
                            else
                            {
                                Span<byte> salt = stackalloc byte[KeyLengths.Rc4Max_128Bit / 8];

                                if (!reader.TryCopyOctetStringBytes(salt, out saltLen))
                                {
                                    throw new CryptographicException();
                                }
                            }
                        }

                        keyLength = KeyLengths.Rc4Max_128Bit - 8 * saltLen;
                        break;
                    }
                case Oids.DesCbc:
                    keyLength = KeyLengths.Des_64Bit;
                    break;
                case Oids.TripleDesCbc:
                    keyLength = KeyLengths.TripleDes_192Bit;
                    break;
                default:
                    // .NET Framework doesn't set a keylength for AES, or any other algorithm than the ones
                    // listed here.
                    keyLength = 0;
                    break;
            }

            return new AlgorithmIdentifier(new Oid(asn.Algorithm), keyLength);
        }
    }
}
