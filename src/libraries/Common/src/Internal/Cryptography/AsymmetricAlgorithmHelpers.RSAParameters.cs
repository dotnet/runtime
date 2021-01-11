// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics;
using System.Formats.Asn1;
using System.Security.Cryptography;

namespace Internal.Cryptography
{
    internal static partial class AsymmetricAlgorithmHelpers
    {
        internal static void FromRSAPublicKey(
            ReadOnlySpan<byte> source,
            ref RSAParameters rsaParameters)
        {
            Debug.Assert(rsaParameters.Modulus == null);
            Debug.Assert(rsaParameters.Exponent == null);
            Debug.Assert(rsaParameters.D == null);
            Debug.Assert(rsaParameters.P == null);
            Debug.Assert(rsaParameters.Q == null);
            Debug.Assert(rsaParameters.DP == null);
            Debug.Assert(rsaParameters.DQ == null);
            Debug.Assert(rsaParameters.InverseQ == null);

            try
            {
                // https://tools.ietf.org/html/rfc3447#appendix-A.1.1
                //
                // RSAPublicKey ::= SEQUENCE {
                //   modulus           INTEGER,  -- n
                //   publicExponent    INTEGER   -- e
                // }

                AsnValueReader reader = new AsnValueReader(source, AsnEncodingRules.DER);
                AsnValueReader pubKey = reader.ReadSequence();
                reader.ThrowIfNotEmpty();

                ReadOnlySpan<byte> modulus = pubKey.ReadIntegerBytes();
                ReadOnlySpan<byte> exponent = pubKey.ReadIntegerBytes();
                pubKey.ThrowIfNotEmpty();

                rsaParameters.Modulus = ExportInteger(modulus, pinned: false);
                rsaParameters.Exponent = ExportInteger(exponent, pinned: false);
            }
            catch (AsnContentException e)
            {
                throw new CryptographicException(SR.Arg_CryptographyException, e);
            }
        }

        internal static void FromRSAPrivateKey(
            ReadOnlySpan<byte> source,
            ref RSAParameters rsaParameters)
        {
            Debug.Assert(rsaParameters.Modulus == null);
            Debug.Assert(rsaParameters.Exponent == null);
            Debug.Assert(rsaParameters.D == null);
            Debug.Assert(rsaParameters.P == null);
            Debug.Assert(rsaParameters.Q == null);
            Debug.Assert(rsaParameters.DP == null);
            Debug.Assert(rsaParameters.DQ == null);
            Debug.Assert(rsaParameters.InverseQ == null);

            try
            {
                // https://tools.ietf.org/html/rfc3447#appendix-A.1.2
                //
                // RSAPrivateKey ::= SEQUENCE {
                //   version           Version,
                //   modulus           INTEGER,  -- n
                //   publicExponent    INTEGER,  -- e
                //   privateExponent   INTEGER,  -- d
                //   prime1            INTEGER,  -- p
                //   prime2            INTEGER,  -- q
                //   exponent1         INTEGER,  -- d mod (p-1)
                //   exponent2         INTEGER,  -- d mod (q-1)
                //   coefficient       INTEGER,  -- (inverse of q) mod p
                //   otherPrimeInfos   OtherPrimeInfos OPTIONAL
                // }

                AsnValueReader reader = new AsnValueReader(source, AsnEncodingRules.DER);
                AsnValueReader privKey = reader.ReadSequence();
                reader.ThrowIfNotEmpty();

                if (!privKey.TryReadInt32(out int version))
                {
                    throw new CryptographicException(SR.Arg_CryptographyException);
                }

                // version 0 is two-prime RSA.
                // version 1 is multi-prime RSA.
                // version 2+ is not defined (as of Jan 2021)
                if (version != 0)
                {
                    // This isn't expected, since multi-prime RSA isn't popular.
                    // If it starts getting hit then it would be worth a dedicated string,
                    // but until then just use the generic message.
                    throw new CryptographicException(SR.Arg_CryptographyException);
                }

                ReadOnlySpan<byte> modulus = privKey.ReadIntegerBytes();
                ReadOnlySpan<byte> exponent = privKey.ReadIntegerBytes();
                ReadOnlySpan<byte> d = privKey.ReadIntegerBytes();
                ReadOnlySpan<byte> p = privKey.ReadIntegerBytes();
                ReadOnlySpan<byte> q = privKey.ReadIntegerBytes();
                ReadOnlySpan<byte> dp = privKey.ReadIntegerBytes();
                ReadOnlySpan<byte> dq = privKey.ReadIntegerBytes();
                ReadOnlySpan<byte> qInv = privKey.ReadIntegerBytes();
                privKey.ThrowIfNotEmpty();

                rsaParameters.Modulus = ExportInteger(modulus, pinned: false);
                rsaParameters.Exponent = ExportInteger(exponent, pinned: false);

                int halfModulus = (rsaParameters.Modulus.Length + 1) / 2;

                rsaParameters.D = ExportInteger(d, pinned: true, rsaParameters.Modulus.Length);
                rsaParameters.P = ExportInteger(p, pinned: true, halfModulus);
                rsaParameters.Q = ExportInteger(q, pinned: true, halfModulus);
                rsaParameters.DP = ExportInteger(dp, pinned: true, halfModulus);
                rsaParameters.DQ = ExportInteger(dq, pinned: true, halfModulus);
                rsaParameters.InverseQ = ExportInteger(qInv, pinned: true, halfModulus);
            }
            catch (AsnContentException e)
            {
                throw new CryptographicException(SR.Arg_CryptographyException, e);
            }
        }

        internal static ArraySegment<byte> ToSubjectPublicKeyInfo(in this RSAParameters rsaParameters)
        {
            AsnWriter writer = ToRSAPublicKey(rsaParameters);

            // The SPKI overhead is
            // SPKI SEQUENCE, at most 6 bytes (1 for tag, 5 for length)
            // RSA AlgorithmIdentifier, fixed 15 bytes.
            // SPKI.publicKey's BIT STRING, at most 7 bytes (1 for tag, 5 for length, 1 for unused bits=0)
            // 13+15 is 18.  Let's go ahead and round up to 32.
            const int Overhead = 32;

            byte[] rented = CryptoPool.Rent(writer.GetEncodedLength() + Overhead);
            int written = writer.Encode(rented);

            writer.Reset();

            // SubjectPublicKeyInfo
            using (writer.PushSequence())
            {
                // algorithm
                using (writer.PushSequence())
                {
                    writer.WriteObjectIdentifier(Oids.Rsa);
                    writer.WriteNull();
                }

                writer.WriteBitString(rented.AsSpan(0, written));
            }

            Debug.Assert(rented.Length > writer.GetEncodedLength());
            written = writer.Encode(rented);
            return new ArraySegment<byte>(rented, 0, written);
        }

        internal static ArraySegment<byte> RSAPublicKeyToSPKI(ReadOnlySpan<byte> rsaPublicKey)
        {
            AsnWriter writer = new AsnWriter(AsnEncodingRules.DER);

            // SubjectPublicKeyInfo
            using (writer.PushSequence())
            {
                // algorithm
                using (writer.PushSequence())
                {
                    writer.WriteObjectIdentifier(Oids.Rsa);
                    writer.WriteNull();
                }

                writer.WriteBitString(rsaPublicKey);
            }

            byte[] rented = CryptoPool.Rent(writer.GetEncodedLength());
            int written = writer.Encode(rented);
            return new ArraySegment<byte>(rented, 0, written);
        }

        private static AsnWriter ToRSAPublicKey(in RSAParameters rsaParameters)
        {
            Debug.Assert(rsaParameters.Modulus != null);
            Debug.Assert(rsaParameters.Exponent != null);

            AsnWriter writer = new AsnWriter(AsnEncodingRules.DER);

            // RSAPublicKey
            using (writer.PushSequence())
            {
                // n, e
                writer.WriteKeyParameterInteger(rsaParameters.Modulus);
                writer.WriteKeyParameterInteger(rsaParameters.Exponent);
            }

            return writer;
        }

        private static void ToRSAPrivateKey(in RSAParameters rsaParameters, AsnWriter writer)
        {
            Debug.Assert(rsaParameters.Modulus != null);
            Debug.Assert(rsaParameters.Exponent != null);
            Debug.Assert(rsaParameters.D != null);
            Debug.Assert(rsaParameters.P != null);
            Debug.Assert(rsaParameters.Q != null);
            Debug.Assert(rsaParameters.DP != null);
            Debug.Assert(rsaParameters.DQ != null);
            Debug.Assert(rsaParameters.InverseQ != null);
            Debug.Assert(writer != null);

            // RSAPrivateKey
            using (writer.PushSequence())
            {
                // Version 0 (two-prime RSA)
                writer.WriteInteger(0);

                // All the parameters (n, e, d, p, q, dp, dq, qInv)
                writer.WriteKeyParameterInteger(rsaParameters.Modulus);
                writer.WriteKeyParameterInteger(rsaParameters.Exponent);
                writer.WriteKeyParameterInteger(rsaParameters.D);
                writer.WriteKeyParameterInteger(rsaParameters.P);
                writer.WriteKeyParameterInteger(rsaParameters.Q);
                writer.WriteKeyParameterInteger(rsaParameters.DP);
                writer.WriteKeyParameterInteger(rsaParameters.DQ);
                writer.WriteKeyParameterInteger(rsaParameters.InverseQ);
            }
        }

        internal static ArraySegment<byte> ToPkcs8(in this RSAParameters rsaParameters)
        {
            AsnWriter writer = new AsnWriter(AsnEncodingRules.DER);

            // PrivateKeyInfo
            using (writer.PushSequence())
            {
                // Version 0 (no attributes)
                writer.WriteInteger(0);

                // privateKeyAlgorithm
                using (writer.PushSequence())
                {
                    writer.WriteObjectIdentifier(Oids.Rsa);
                    writer.WriteNull();
                }

                // privateKey
                using (writer.PushOctetString())
                {
                    // RSAPrivateKey
                    ToRSAPrivateKey(rsaParameters, writer);
                }
            }

            byte[] rented = CryptoPool.Rent(writer.GetEncodedLength());
            int written = writer.Encode(rented);
            writer.Reset();
            return new ArraySegment<byte>(rented, 0, written);
        }

        internal static ArraySegment<byte> RSAPrivateKeyToPkcs8(ReadOnlySpan<byte> rsaPrivateKey)
        {
            AsnWriter writer = new AsnWriter(AsnEncodingRules.DER);

            // PrivateKeyInfo
            using (writer.PushSequence())
            {
                // Version 0 (no attributes)
                writer.WriteInteger(0);

                // privateKeyAlgorithm
                using (writer.PushSequence())
                {
                    writer.WriteObjectIdentifier(Oids.Rsa);
                    writer.WriteNull();
                }

                // privateKey
                writer.WriteOctetString(rsaPrivateKey);
            }

            byte[] rented = CryptoPool.Rent(writer.GetEncodedLength());
            int written = writer.Encode(rented);
            writer.Reset();
            return new ArraySegment<byte>(rented, 0, written);
        }

        private static byte[] ExportInteger(ReadOnlySpan<byte> integerValue, bool pinned, int targetSize = -1)
        {
            // The values are signed, so a leading 0 byte may be present to make the value positive.
            // Our export is unsigned, so strip it off if it's found.
            if (integerValue.Length > 1 && integerValue[0] == 0)
            {
                integerValue = integerValue.Slice(1);
            }

            if (targetSize > integerValue.Length)
            {
                byte[] ret = CryptoPool.AllocateArray(targetSize, pinned);
                int shift = targetSize - integerValue.Length;

                ret.AsSpan(0, shift).Clear();
                integerValue.CopyTo(ret.AsSpan(shift));
                return ret;
            }

            return integerValue.ToArray();
        }
    }
}
