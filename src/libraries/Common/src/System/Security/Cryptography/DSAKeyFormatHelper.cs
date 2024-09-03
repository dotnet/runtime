// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Formats.Asn1;
using System.Numerics;
using System.Security.Cryptography.Asn1;

namespace System.Security.Cryptography
{
    internal static class DSAKeyFormatHelper
    {
        private static readonly string[] s_validOids =
        {
            Oids.Dsa,
        };

        internal static void ReadDsaPrivateKey(
            ReadOnlyMemory<byte> xBytes,
            in AlgorithmIdentifierAsn algId,
            out DSAParameters ret)
        {
            if (!algId.Parameters.HasValue)
            {
                throw new CryptographicException(SR.Cryptography_Der_Invalid_Encoding);
            }

            BigInteger x;

            try
            {
                ReadOnlySpan<byte> xSpan = AsnDecoder.ReadIntegerBytes(
                    xBytes.Span,
                    AsnEncodingRules.DER,
                    out int consumed);

                if (consumed != xBytes.Length)
                {
                    throw new CryptographicException(SR.Cryptography_Der_Invalid_Encoding);
                }

                // Force a positive interpretation because Windows sometimes writes negative numbers.
                x = new BigInteger(xSpan, isUnsigned: true, isBigEndian: true);
            }
            catch (AsnContentException e)
            {
                throw new CryptographicException(SR.Cryptography_Der_Invalid_Encoding, e);
            }

            DssParms parms = DssParms.Decode(algId.Parameters.Value, AsnEncodingRules.BER);

            // Sanity checks from FIPS 186-4 4.1/4.2.  Since FIPS 186-5 withdrew DSA/DSS
            // these will never change again.
            //
            // This technically allows a non-standard combination of 1024-bit P and 256-bit Q,
            // but that will get filtered out by the underlying provider.
            // These checks just prevent obviously bad data from wasting work on reinterpretation.
            if (parms.P.Sign < 0 ||
                parms.Q.Sign < 0 ||
                !IsValidPLength(parms.P.GetBitLength()) ||
                !IsValidQLength(parms.Q.GetBitLength()) ||
                parms.G <= 1 ||
                parms.G >= parms.P ||
                x <= 1 ||
                x >= parms.Q)
            {
                throw new CryptographicException(SR.Cryptography_Der_Invalid_Encoding);
            }

            ret = new DSAParameters
            {
                P = parms.P.ToByteArray(isUnsigned: true, isBigEndian: true),
                Q = parms.Q.ToByteArray(isUnsigned: true, isBigEndian: true),
            };

            ret.G = parms.G.ExportKeyParameter(ret.P.Length);
            ret.X = x.ExportKeyParameter(ret.Q.Length);

            // The public key is not contained within the format, calculate it.
            BigInteger y = BigInteger.ModPow(parms.G, x, parms.P);
            ret.Y = y.ExportKeyParameter(ret.P.Length);
        }

        internal static void ReadDsaPublicKey(
            ReadOnlyMemory<byte> yBytes,
            in AlgorithmIdentifierAsn algId,
            out DSAParameters ret)
        {
            if (!algId.Parameters.HasValue)
            {
                throw new CryptographicException(SR.Cryptography_Der_Invalid_Encoding);
            }

            BigInteger y;

            try
            {
                y = AsnDecoder.ReadInteger(
                    yBytes.Span,
                    AsnEncodingRules.DER,
                    out int consumed);

                if (consumed != yBytes.Length)
                {
                    throw new CryptographicException(SR.Cryptography_Der_Invalid_Encoding);
                }
            }
            catch (AsnContentException e)
            {
                throw new CryptographicException(SR.Cryptography_Der_Invalid_Encoding, e);
            }

            DssParms parms = DssParms.Decode(algId.Parameters.Value, AsnEncodingRules.BER);

            // Sanity checks from FIPS 186-4 4.1/4.2.  Since FIPS 186-5 withdrew DSA/DSS
            // these will never change again.
            //
            // This technically allows a non-standard combination of 1024-bit P and 256-bit Q,
            // but that will get filtered out by the underlying provider.
            // These checks just prevent obviously bad data from wasting work on reinterpretation.
            if (parms.P.Sign < 0 ||
                parms.Q.Sign < 0 ||
                !IsValidPLength(parms.P.GetBitLength()) ||
                !IsValidQLength(parms.Q.GetBitLength()) ||
                parms.G <= 1 ||
                parms.G >= parms.P ||
                y <= 1 ||
                y >= parms.P)
            {
                throw new CryptographicException(SR.Cryptography_Der_Invalid_Encoding);
            }

            ret = new DSAParameters
            {
                P = parms.P.ToByteArray(isUnsigned: true, isBigEndian: true),
                Q = parms.Q.ToByteArray(isUnsigned: true, isBigEndian: true),
            };

            ret.G = parms.G.ExportKeyParameter(ret.P.Length);
            ret.Y = y.ExportKeyParameter(ret.P.Length);
        }

        private static bool IsValidPLength(long pBitLength)
        {
            return pBitLength switch
            {
                // FIPS 186-3/186-4
                1024 or 2048 or 3072 => true,
                // FIPS 186-1/186-2
                >= 512 and < 1024 => pBitLength % 64 == 0,
                _ => false,
            };
        }

        private static bool IsValidQLength(long qBitLength)
        {
            // FIPS 186-1/186-2 only allows 160
            // FIPS 186-3/186-4 allow 160/224/256
            return qBitLength is 160 or 224 or 256;
        }

        internal static void ReadSubjectPublicKeyInfo(
            ReadOnlySpan<byte> source,
            out int bytesRead,
            out DSAParameters key)
        {
            KeyFormatHelper.ReadSubjectPublicKeyInfo<DSAParameters>(
                s_validOids,
                source,
                ReadDsaPublicKey,
                out bytesRead,
                out key);
        }

        internal static ReadOnlyMemory<byte> ReadSubjectPublicKeyInfo(
             ReadOnlyMemory<byte> source,
             out int bytesRead)
        {
            return KeyFormatHelper.ReadSubjectPublicKeyInfo(
                s_validOids,
                source,
                out bytesRead);
        }

        internal static void ReadPkcs8(
            ReadOnlySpan<byte> source,
            out int bytesRead,
            out DSAParameters key)
        {
            KeyFormatHelper.ReadPkcs8<DSAParameters>(
                s_validOids,
                source,
                ReadDsaPrivateKey,
                out bytesRead,
                out key);
        }

        internal static void ReadEncryptedPkcs8(
            ReadOnlySpan<byte> source,
            ReadOnlySpan<char> password,
            out int bytesRead,
            out DSAParameters key)
        {
            KeyFormatHelper.ReadEncryptedPkcs8<DSAParameters>(
                s_validOids,
                source,
                password,
                ReadDsaPrivateKey,
                out bytesRead,
                out key);
        }

        internal static void ReadEncryptedPkcs8(
            ReadOnlySpan<byte> source,
            ReadOnlySpan<byte> passwordBytes,
            out int bytesRead,
            out DSAParameters key)
        {
            KeyFormatHelper.ReadEncryptedPkcs8<DSAParameters>(
                s_validOids,
                source,
                passwordBytes,
                ReadDsaPrivateKey,
                out bytesRead,
                out key);
        }

        internal static AsnWriter WriteSubjectPublicKeyInfo(in DSAParameters dsaParameters)
        {
            AsnWriter writer = new AsnWriter(AsnEncodingRules.DER);

            writer.PushSequence();
            WriteAlgorithmId(writer, dsaParameters);
            WriteKeyComponent(writer, dsaParameters.Y, bitString: true);
            writer.PopSequence();

            return writer;
        }

        internal static AsnWriter WritePkcs8(in DSAParameters dsaParameters)
        {
            AsnWriter writer = new AsnWriter(AsnEncodingRules.DER);

            writer.PushSequence();
            writer.WriteInteger(0);
            WriteAlgorithmId(writer, dsaParameters);
            WriteKeyComponent(writer, dsaParameters.X, bitString: false);
            writer.PopSequence();

            return writer;
        }

        private static void WriteAlgorithmId(AsnWriter writer, in DSAParameters dsaParameters)
        {
            writer.PushSequence();
            writer.WriteObjectIdentifier(Oids.Dsa);

            // Dss-Parms ::= SEQUENCE {
            //   p INTEGER,
            //   q INTEGER,
            //   g INTEGER  }
            writer.PushSequence();
            writer.WriteKeyParameterInteger(dsaParameters.P);
            writer.WriteKeyParameterInteger(dsaParameters.Q);
            writer.WriteKeyParameterInteger(dsaParameters.G);
            writer.PopSequence();
            writer.PopSequence();
        }

        private static void WriteKeyComponent(AsnWriter writer, byte[]? component, bool bitString)
        {
            if (bitString)
            {
                AsnWriter inner = new AsnWriter(AsnEncodingRules.DER);
                inner.WriteKeyParameterInteger(component);

                byte[] tmp = CryptoPool.Rent(inner.GetEncodedLength());

                if (!inner.TryEncode(tmp, out int written))
                {
                    Debug.Fail("TryEncode failed with a pre-allocated buffer");
                    throw new CryptographicException();
                }

                writer.WriteBitString(tmp.AsSpan(0, written));
                CryptoPool.Return(tmp, written);
            }
            else
            {
                using (writer.PushOctetString())
                {
                    writer.WriteKeyParameterInteger(component);
                }
            }
        }
    }
}
