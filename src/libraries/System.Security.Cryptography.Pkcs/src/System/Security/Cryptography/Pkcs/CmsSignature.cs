// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Diagnostics;
using System.Numerics;
using System.Security.Cryptography.Asn1;
using System.Security.Cryptography.X509Certificates;

namespace System.Security.Cryptography.Pkcs
{
    internal abstract partial class CmsSignature
    {
        private static readonly Dictionary<string, CmsSignature> s_lookup =
            new Dictionary<string, CmsSignature>();

        static CmsSignature()
        {
            PrepareRegistrationRsa(s_lookup);
            PrepareRegistrationDsa(s_lookup);
            PrepareRegistrationECDsa(s_lookup);
        }

        static partial void PrepareRegistrationRsa(Dictionary<string, CmsSignature> lookup);
        static partial void PrepareRegistrationDsa(Dictionary<string, CmsSignature> lookup);
        static partial void PrepareRegistrationECDsa(Dictionary<string, CmsSignature> lookup);

        internal abstract bool VerifySignature(
#if netcoreapp
            ReadOnlySpan<byte> valueHash,
            ReadOnlyMemory<byte> signature,
#else
            byte[] valueHash,
            byte[] signature,
#endif
            string digestAlgorithmOid,
            HashAlgorithmName digestAlgorithmName,
            ReadOnlyMemory<byte>? signatureParameters,
            X509Certificate2 certificate);

        protected abstract bool Sign(
#if netcoreapp
            ReadOnlySpan<byte> dataHash,
#else
            byte[] dataHash,
#endif
            HashAlgorithmName hashAlgorithmName,
            X509Certificate2 certificate,
            bool silent,
            out Oid signatureAlgorithm,
            out byte[] signatureValue);

        internal static CmsSignature Resolve(string signatureAlgorithmOid)
        {
            if (s_lookup.TryGetValue(signatureAlgorithmOid, out CmsSignature processor))
            {
                return processor;
            }

            return null;
        }

        internal static bool Sign(
#if netcoreapp
            ReadOnlySpan<byte> dataHash,
#else
            byte[] dataHash,
#endif
            HashAlgorithmName hashAlgorithmName,
            X509Certificate2 certificate,
            bool silent,
            out Oid oid,
            out ReadOnlyMemory<byte> signatureValue)
        {
            CmsSignature processor = Resolve(certificate.GetKeyAlgorithm());

            if (processor == null)
            {
                oid = null;
                signatureValue = default;
                return false;
            }

            byte[] signature;
            bool signed = processor.Sign(dataHash, hashAlgorithmName, certificate, silent, out oid, out signature);
            signatureValue = signature;
            return signed;
        }

        private static bool DsaDerToIeee(
            ReadOnlyMemory<byte> derSignature,
            Span<byte> ieeeSignature)
        {
            int fieldSize = ieeeSignature.Length / 2;

            Debug.Assert(
                fieldSize * 2 == ieeeSignature.Length,
                $"ieeeSignature.Length ({ieeeSignature.Length}) must be even");

            try
            {
                AsnReader reader = new AsnReader(derSignature, AsnEncodingRules.DER);
                AsnReader sequence = reader.ReadSequence();

                if (reader.HasData)
                {
                    return false;
                }

                // Fill it with zeros so that small data is correctly zero-prefixed.
                // this buffer isn't very large, so there's not really a reason to the
                // partial-fill gymnastics.
                ieeeSignature.Clear();

                ReadOnlySpan<byte> val = sequence.GetIntegerBytes().Span;

                if (val.Length > fieldSize && val[0] == 0)
                {
                    val = val.Slice(1);
                }

                if (val.Length <= fieldSize)
                {
                    val.CopyTo(ieeeSignature.Slice(fieldSize - val.Length, val.Length));
                }

                val = sequence.GetIntegerBytes().Span;

                if (val.Length > fieldSize && val[0] == 0)
                {
                    val = val.Slice(1);
                }

                if (val.Length <= fieldSize)
                {
                    val.CopyTo(ieeeSignature.Slice(fieldSize + fieldSize - val.Length, val.Length));
                }

                return !sequence.HasData;
            }
            catch (CryptographicException)
            {
                return false;
            }
        }

        private static byte[] DsaIeeeToDer(ReadOnlySpan<byte> ieeeSignature)
        {
            int fieldSize = ieeeSignature.Length / 2;

            Debug.Assert(
                fieldSize * 2 == ieeeSignature.Length,
                $"ieeeSignature.Length ({ieeeSignature.Length}) must be even");

            using (AsnWriter writer = new AsnWriter(AsnEncodingRules.DER))
            {
                writer.PushSequence();

#if netcoreapp
                // r
                BigInteger val = new BigInteger(
                    ieeeSignature.Slice(0, fieldSize),
                    isUnsigned: true,
                    isBigEndian: true);

                writer.WriteInteger(val);

                // s
                val = new BigInteger(
                    ieeeSignature.Slice(fieldSize, fieldSize),
                    isUnsigned: true,
                    isBigEndian: true);

                writer.WriteInteger(val);
#else
                byte[] buf = new byte[fieldSize + 1];
                Span<byte> bufWriter = new Span<byte>(buf, 1, fieldSize);

                ieeeSignature.Slice(0, fieldSize).CopyTo(bufWriter);
                Array.Reverse(buf);
                BigInteger val = new BigInteger(buf);
                writer.WriteInteger(val);

                buf[0] = 0;
                ieeeSignature.Slice(fieldSize, fieldSize).CopyTo(bufWriter);
                Array.Reverse(buf);
                val = new BigInteger(buf);
                writer.WriteInteger(val);
#endif

                writer.PopSequence();

                return writer.Encode();
            }
        }
    }
}
