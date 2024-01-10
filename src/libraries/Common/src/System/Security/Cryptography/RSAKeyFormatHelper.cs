// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using System.Diagnostics;
using System.Formats.Asn1;
using System.Security.Cryptography.Asn1;

namespace System.Security.Cryptography
{
    internal static partial class RSAKeyFormatHelper
    {
        private static readonly string[] s_validOids =
        {
            Oids.Rsa,
        };

        internal static void FromPkcs1PrivateKey(
            ReadOnlyMemory<byte> keyData,
            in AlgorithmIdentifierAsn algId,
            out RSAParameters ret)
        {
            RSAPrivateKeyAsn key = RSAPrivateKeyAsn.Decode(keyData, AsnEncodingRules.BER);

            if (!algId.HasNullEquivalentParameters())
            {
                throw new CryptographicException(SR.Cryptography_Der_Invalid_Encoding);
            }

            const int MaxSupportedVersion = 0;

            if (key.Version > MaxSupportedVersion)
            {
                throw new CryptographicException(
                    SR.Format(
                        SR.Cryptography_RSAPrivateKey_VersionTooNew,
                        key.Version,
                        MaxSupportedVersion));
            }

            // The modulus size determines the encoded output size of the CRT parameters.
            byte[] n = key.Modulus.ToByteArray(isUnsigned: true, isBigEndian: true);
            int halfModulusLength = (n.Length + 1) / 2;

            ret = new RSAParameters
            {
                Modulus = n,
                Exponent = key.PublicExponent.ToByteArray(isUnsigned: true, isBigEndian: true),
                D = key.PrivateExponent.ExportKeyParameter(n.Length),
                P = key.Prime1.ExportKeyParameter(halfModulusLength),
                Q = key.Prime2.ExportKeyParameter(halfModulusLength),
                DP = key.Exponent1.ExportKeyParameter(halfModulusLength),
                DQ = key.Exponent2.ExportKeyParameter(halfModulusLength),
                InverseQ = key.Coefficient.ExportKeyParameter(halfModulusLength),
            };
        }

        internal static void ReadRsaPublicKey(
            ReadOnlyMemory<byte> keyData,
            in AlgorithmIdentifierAsn algId,
            out RSAParameters ret)
        {
            RSAPublicKeyAsn key = RSAPublicKeyAsn.Decode(keyData, AsnEncodingRules.BER);

            ret = new RSAParameters
            {
                Modulus = key.Modulus.ToByteArray(isUnsigned: true, isBigEndian: true),
                Exponent = key.PublicExponent.ToByteArray(isUnsigned: true, isBigEndian: true),
            };
        }

        internal static void ReadRsaPublicKey(
            ReadOnlyMemory<byte> keyData,
            out int bytesRead)
        {
            int read;

            try
            {
                AsnValueReader reader = new AsnValueReader(keyData.Span, AsnEncodingRules.DER);
                read = reader.PeekEncodedValue().Length;
                RSAPublicKeyAsn.Decode(keyData, AsnEncodingRules.BER);
            }
            catch (AsnContentException e)
            {
                throw new CryptographicException(SR.Cryptography_Der_Invalid_Encoding, e);
            }

            bytesRead = read;
        }

        internal static void ReadSubjectPublicKeyInfo(
            ReadOnlySpan<byte> source,
            out int bytesRead,
            out RSAParameters key)
        {
            KeyFormatHelper.ReadSubjectPublicKeyInfo<RSAParameters>(
                s_validOids,
                source,
                ReadRsaPublicKey,
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

        /// <summary>
        ///   Checks that a SubjectPublicKeyInfo represents an RSA key.
        /// </summary>
        /// <returns>The number of bytes read from <paramref name="source"/>.</returns>
        internal static unsafe int CheckSubjectPublicKeyInfo(ReadOnlySpan<byte> source)
        {
            int bytesRead;

            fixed (byte* ptr = source)
            {
                using (MemoryManager<byte> manager = new PointerMemoryManager<byte>(ptr, source.Length))
                {
                    _ = ReadSubjectPublicKeyInfo(manager.Memory, out bytesRead);
                }
            }

            return bytesRead;
        }

        public static void ReadPkcs8(
            ReadOnlySpan<byte> source,
            out int bytesRead,
            out RSAParameters key)
        {
            KeyFormatHelper.ReadPkcs8<RSAParameters>(
                s_validOids,
                source,
                FromPkcs1PrivateKey,
                out bytesRead,
                out key);
        }

        internal static ReadOnlyMemory<byte> ReadPkcs8(
            ReadOnlyMemory<byte> source,
            out int bytesRead)
        {
            return KeyFormatHelper.ReadPkcs8(
                s_validOids,
                source,
                out bytesRead);
        }

        /// <summary>
        ///   Checks that a Pkcs8PrivateKeyInfo represents an RSA key.
        /// </summary>
        /// <returns>The number of bytes read from <paramref name="source"/>.</returns>
        internal static unsafe int CheckPkcs8(ReadOnlySpan<byte> source)
        {
            int bytesRead;

            fixed (byte* ptr = source)
            {
                using (MemoryManager<byte> manager = new PointerMemoryManager<byte>(ptr, source.Length))
                {
                    _ = ReadPkcs8(manager.Memory, out bytesRead);
                }
            }

            return bytesRead;
        }

        internal static AsnWriter WriteSubjectPublicKeyInfo(ReadOnlySpan<byte> pkcs1PublicKey)
        {
            AsnWriter writer = new AsnWriter(AsnEncodingRules.DER);

            writer.PushSequence();
            WriteAlgorithmIdentifier(writer);
            writer.WriteBitString(pkcs1PublicKey);
            writer.PopSequence();

            return writer;
        }

        internal static AsnWriter WriteSubjectPublicKeyInfo(in RSAParameters rsaParameters)
        {
            AsnWriter pkcs1PublicKey = WritePkcs1PublicKey(rsaParameters);
            byte[] rented = CryptoPool.Rent(pkcs1PublicKey.GetEncodedLength());

            if (!pkcs1PublicKey.TryEncode(rented, out int written))
            {
                Debug.Fail("TryEncode failed with a presized buffer");
                throw new CryptographicException();
            }

            AsnWriter ret = WriteSubjectPublicKeyInfo(rented.AsSpan(0, written));

            // Only public key data data
            CryptoPool.Return(rented, clearSize: 0);
            return ret;
        }

        internal static AsnWriter WritePkcs8PrivateKey(
            ReadOnlySpan<byte> pkcs1PrivateKey,
            AsnWriter? copyFrom = null)
        {
            Debug.Assert(copyFrom == null || pkcs1PrivateKey.IsEmpty);

            AsnWriter writer = new AsnWriter(AsnEncodingRules.BER);

            using (writer.PushSequence())
            {
                // Version 0 format (no attributes)
                writer.WriteInteger(0);
                WriteAlgorithmIdentifier(writer);

                if (copyFrom != null)
                {
                    using (writer.PushOctetString())
                    {
                        copyFrom.CopyTo(writer);
                    }
                }
                else
                {
                    writer.WriteOctetString(pkcs1PrivateKey);
                }
            }

            return writer;
        }

        internal static AsnWriter WritePkcs8PrivateKey(in RSAParameters rsaParameters)
        {
            AsnWriter pkcs1PrivateKey = WritePkcs1PrivateKey(rsaParameters);
            return WritePkcs8PrivateKey(ReadOnlySpan<byte>.Empty, pkcs1PrivateKey);
        }

        private static void WriteAlgorithmIdentifier(AsnWriter writer)
        {
            writer.PushSequence();

            // https://tools.ietf.org/html/rfc3447#appendix-C
            //
            // --
            // -- When rsaEncryption is used in an AlgorithmIdentifier the
            // -- parameters MUST be present and MUST be NULL.
            // --
            writer.WriteObjectIdentifier(Oids.Rsa);
            writer.WriteNull();

            writer.PopSequence();
        }

        internal static AsnWriter WritePkcs1PublicKey(in RSAParameters rsaParameters)
        {
            if (rsaParameters.Modulus == null || rsaParameters.Exponent == null)
            {
                throw new CryptographicException(SR.Cryptography_InvalidRsaParameters);
            }

            AsnWriter writer = new AsnWriter(AsnEncodingRules.DER);
            writer.PushSequence();
            writer.WriteKeyParameterInteger(rsaParameters.Modulus);
            writer.WriteKeyParameterInteger(rsaParameters.Exponent);
            writer.PopSequence();

            return writer;
        }

        internal static AsnWriter WritePkcs1PrivateKey(in RSAParameters rsaParameters)
        {
            if (rsaParameters.Modulus == null || rsaParameters.Exponent == null)
            {
                throw new CryptographicException(SR.Cryptography_InvalidRsaParameters);
            }

            if (rsaParameters.D == null ||
                rsaParameters.P == null ||
                rsaParameters.Q == null ||
                rsaParameters.DP == null ||
                rsaParameters.DQ == null ||
                rsaParameters.InverseQ == null)
            {
                throw new CryptographicException(SR.Cryptography_NotValidPrivateKey);
            }

            AsnWriter writer = new AsnWriter(AsnEncodingRules.DER);

            writer.PushSequence();

            // Format version 0
            writer.WriteInteger(0);
            writer.WriteKeyParameterInteger(rsaParameters.Modulus);
            writer.WriteKeyParameterInteger(rsaParameters.Exponent);
            writer.WriteKeyParameterInteger(rsaParameters.D);
            writer.WriteKeyParameterInteger(rsaParameters.P);
            writer.WriteKeyParameterInteger(rsaParameters.Q);
            writer.WriteKeyParameterInteger(rsaParameters.DP);
            writer.WriteKeyParameterInteger(rsaParameters.DQ);
            writer.WriteKeyParameterInteger(rsaParameters.InverseQ);

            writer.PopSequence();
            return writer;
        }
    }
}
