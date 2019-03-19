// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Buffers;
using System.Diagnostics;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
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

            DssParms parms = DssParms.Decode(algId.Parameters.Value, AsnEncodingRules.BER);

            ret = new DSAParameters
            {
                P = parms.P.ToByteArray(isUnsigned: true, isBigEndian: true),
                Q = parms.Q.ToByteArray(isUnsigned: true, isBigEndian: true),
            };

            ret.G = parms.G.ExportKeyParameter(ret.P.Length);

            AsnReader reader = new AsnReader(xBytes, AsnEncodingRules.DER);
            // Force a positive interpretation because Windows sometimes writes negative numbers.
            BigInteger x = new BigInteger(reader.ReadIntegerBytes().Span, isUnsigned: true, isBigEndian: true);
            ret.X = x.ExportKeyParameter(ret.Q.Length);
            reader.ThrowIfNotEmpty();

            // The public key is not contained within the format, calculate it.
            BigInteger y = BigInteger.ModPow(parms.G, x, parms.P);
            ret.Y = y.ExportKeyParameter(ret.P.Length);
        }

        internal static void ReadDsaPublicKey(
            ReadOnlyMemory<byte> yBytes,
            in AlgorithmIdentifierAsn algId,
            out DSAParameters ret)
        {
            AsnReader reader = new AsnReader(yBytes, AsnEncodingRules.DER);
            BigInteger y = reader.ReadInteger();
            reader.ThrowIfNotEmpty();

            if (!algId.Parameters.HasValue)
            {
                throw new CryptographicException(SR.Cryptography_Der_Invalid_Encoding);
            }

            DssParms parms = DssParms.Decode(algId.Parameters.Value, AsnEncodingRules.BER);

            ret = new DSAParameters
            {
                P = parms.P.ToByteArray(isUnsigned: true, isBigEndian: true),
                Q = parms.Q.ToByteArray(isUnsigned: true, isBigEndian: true),
            };

            ret.G = parms.G.ExportKeyParameter(ret.P.Length);
            ret.Y = y.ExportKeyParameter(ret.P.Length);
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

        private static void WriteKeyComponent(AsnWriter writer, byte[] component, bool bitString)
        {
            using (AsnWriter inner = new AsnWriter(AsnEncodingRules.DER))
            {
                inner.WriteKeyParameterInteger(component);

                if (bitString)
                {
                    writer.WriteBitString(inner.EncodeAsSpan());
                }
                else
                {
                    writer.WriteOctetString(inner.EncodeAsSpan());
                }
            }
        }
    }
}
