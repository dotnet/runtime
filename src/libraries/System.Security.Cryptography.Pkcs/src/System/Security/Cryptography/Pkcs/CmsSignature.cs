// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Formats.Asn1;
using System.Numerics;
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

        internal abstract RSASignaturePadding? SignaturePadding { get; }
        protected abstract bool VerifyKeyType(AsymmetricAlgorithm key);

        internal abstract bool VerifySignature(
#if NETCOREAPP || NETSTANDARD2_1
            ReadOnlySpan<byte> valueHash,
            ReadOnlyMemory<byte> signature,
#else
            byte[] valueHash,
            byte[] signature,
#endif
            string? digestAlgorithmOid,
            HashAlgorithmName digestAlgorithmName,
            ReadOnlyMemory<byte>? signatureParameters,
            X509Certificate2 certificate);

        protected abstract bool Sign(
#if NETCOREAPP || NETSTANDARD2_1
            ReadOnlySpan<byte> dataHash,
#else
            byte[] dataHash,
#endif
            HashAlgorithmName hashAlgorithmName,
            X509Certificate2 certificate,
            AsymmetricAlgorithm? key,
            bool silent,
            [NotNullWhen(true)] out string? signatureAlgorithm,
            [NotNullWhen(true)] out byte[]? signatureValue,
            out byte[]? signatureParameters);

        internal static CmsSignature? ResolveAndVerifyKeyType(
            string signatureAlgorithmOid,
            AsymmetricAlgorithm? key,
            RSASignaturePadding? rsaSignaturePadding)
        {
            // Rules:
            // RSASignaturePadding 'wins' if specified if the signatureAlgorithmOid is any RSA OID.
            // if there is no rsaSignaturePadding, the OID is used.
            // If the rsaSignaturePadding is specified and the signatureAlgorithm OID is not any
            // RSA OID, this is invalid, so null.
            if (s_lookup.TryGetValue(signatureAlgorithmOid, out CmsSignature? processor))
            {
                // We have a padding that might override the OID.
                if (rsaSignaturePadding is not null)
                {
                    // The processor does not support RSA signature padding
                    if (processor.SignaturePadding is null)
                    {
                        // We were given an RSA signature padding, but the processor is not any known RSA.
                        // We won't override a non-RSA OID (like ECDSA) to RSA based on the padding, so return null.
                        return null;
                    }

                    // The processor is RSA, but does not agree with the specified signature padding, so override.
                    if (processor.SignaturePadding != rsaSignaturePadding)
                    {
                        if (rsaSignaturePadding == RSASignaturePadding.Pkcs1)
                        {
                            processor = s_lookup[Oids.Rsa];
                            Debug.Assert(processor is not null);
                        }
                        else if (rsaSignaturePadding == RSASignaturePadding.Pss)
                        {
                            processor = s_lookup[Oids.RsaPss];
                            Debug.Assert(processor is not null);
                        }
                        else
                        {
                            Debug.Fail("Unhandled RSA signature padding.");
                            return null;
                        }
                    }
                }

                if (key != null && !processor.VerifyKeyType(key))
                {
                    return null;
                }

                return processor;
            }

            return null;
        }

        internal static bool Sign(
#if NETCOREAPP || NETSTANDARD2_1
            ReadOnlySpan<byte> dataHash,
#else
            byte[] dataHash,
#endif
            HashAlgorithmName hashAlgorithmName,
            X509Certificate2 certificate,
            AsymmetricAlgorithm? key,
            bool silent,
            RSASignaturePadding? rsaSignaturePadding,
            out string? oid,
            out ReadOnlyMemory<byte> signatureValue,
            out ReadOnlyMemory<byte> signatureParameters)
        {
            CmsSignature? processor = ResolveAndVerifyKeyType(certificate.GetKeyAlgorithm(), key, rsaSignaturePadding);

            if (processor == null)
            {
                oid = null;
                signatureValue = default;
                signatureParameters = default;
                return false;
            }

            bool signed = processor.Sign(
                dataHash,
                hashAlgorithmName,
                certificate,
                key,
                silent,
                out oid,
                out byte[]? signature,
                out byte[]? parameters);

            signatureValue = signature;
            signatureParameters = parameters;
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

                ReadOnlySpan<byte> val = sequence.ReadIntegerBytes().Span;

                if (val.Length > fieldSize && val[0] == 0)
                {
                    val = val.Slice(1);
                }

                if (val.Length <= fieldSize)
                {
                    val.CopyTo(ieeeSignature.Slice(fieldSize - val.Length, val.Length));
                }

                val = sequence.ReadIntegerBytes().Span;

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
            catch (AsnContentException)
            {
                return false;
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

            AsnWriter writer = new AsnWriter(AsnEncodingRules.DER);
            {
                writer.PushSequence();

#if NETCOREAPP || NETSTANDARD2_1
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
