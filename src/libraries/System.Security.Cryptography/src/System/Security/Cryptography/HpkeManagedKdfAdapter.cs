// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers.Binary;
using System.Diagnostics;

namespace System.Security.Cryptography
{
    internal abstract class HpkeManagedKdfAdapter
    {
        protected static ReadOnlySpan<byte> VersionLabel => "HPKE-v1"u8;
        protected ReadOnlySpan<byte> SuiteId => Suite.SuiteId;
        protected HpkeSuite Suite { get; }

        protected HpkeManagedKdfAdapter(HpkeSuite suite)
        {
            Suite = suite;
        }

        internal static HpkeManagedKdfAdapter Create(HpkeSuite suite)
        {
            switch (suite.KdfAlgorithm)
            {
                case HpkeKdf.HKDF_SHA256:
                case HpkeKdf.HKDF_SHA384:
                case HpkeKdf.HKDF_SHA512:
                    return new HpkeManagedHkdfAdapter(suite, suite.KdfMetadata.HkdfHashAlgorithm);
                case HpkeKdf.SHAKE128:
                    return new HpkeManagedShake128KdfAdapter(suite);
                case HpkeKdf.SHAKE256:
                    return new HpkeManagedShake256KdfAdapter(suite);
                default:
                    Debug.Fail($"Unmapped KDF adapter algorithm {suite.KdfAlgorithm}.");
                    throw new CryptographicException();
            }
        }

        // https://datatracker.ietf.org/doc/html/draft-ietf-hpke-hpke-04#section-5.1
        // Mode (0 for Base, 1 for PSK) and input validation is the caller's responsibility.
        internal void DeriveSecrets(
            byte mode,
            ReadOnlySpan<byte> sharedSecret,
            ReadOnlySpan<byte> info,
            ReadOnlySpan<byte> psk,
            ReadOnlySpan<byte> pskId,
            Span<byte> key,
            Span<byte> baseNonce,
            Span<byte> exporterSecret)
        {
            int secretLength = checked(key.Length + baseNonce.Length + exporterSecret.Length);
            const int MaxStackSecretLength = 128;
            Span<byte> secretBuffer = stackalloc byte[MaxStackSecretLength];

            try
            {
                Span<byte> secret = secretBuffer.Slice(0, secretLength);
                Span<byte> derivedKey = secret.Slice(0, key.Length);
                Span<byte> derivedNonce = secret.Slice(key.Length, baseNonce.Length);
                Span<byte> derivedExporterSecret = secret.Slice(key.Length + baseNonce.Length);

                DeriveSecretsCore(mode, sharedSecret, info, psk, pskId, derivedKey, derivedNonce, derivedExporterSecret);

                derivedKey.CopyTo(key);
                derivedNonce.CopyTo(baseNonce);
                derivedExporterSecret.CopyTo(exporterSecret);
            }
            finally
            {
                CryptographicOperations.ZeroMemory(secretBuffer);
            }
        }

        // https://datatracker.ietf.org/doc/html/draft-ietf-hpke-hpke-04#section-5.3
        internal void ExportSecret(
            ReadOnlySpan<byte> exporterSecret,
            ReadOnlySpan<byte> exporterContext,
            Span<byte> destination)
        {
            Debug.Assert(exporterSecret.Length == Suite.KdfMetadata.Nh);

            int maximumLength = Suite.KdfMetadata.MaximumExportLength;

            if (destination.Length > maximumLength)
            {
                throw new ArgumentException(
                    SR.Format(SR.Argument_HpkeExportLengthTooLarge, maximumLength),
                    nameof(destination));
            }

            // HPKE allows a zero-length export; HKDF.Expand requires a nonempty output.
            if (!destination.IsEmpty)
            {
                ExportSecretCore(exporterSecret, exporterContext, destination);
            }
        }

        protected abstract void DeriveSecretsCore(
            byte mode,
            ReadOnlySpan<byte> sharedSecret,
            ReadOnlySpan<byte> info,
            ReadOnlySpan<byte> psk,
            ReadOnlySpan<byte> pskId,
            Span<byte> key,
            Span<byte> baseNonce,
            Span<byte> exporterSecret);

        protected abstract void ExportSecretCore(
            ReadOnlySpan<byte> exporterSecret,
            ReadOnlySpan<byte> exporterContext,
            Span<byte> destination);
    }

    internal sealed class HpkeManagedHkdfAdapter : HpkeManagedKdfAdapter
    {
        private readonly HashAlgorithmName _hashAlgorithm;

        internal HpkeManagedHkdfAdapter(HpkeSuite suite, HashAlgorithmName hashAlgorithm) : base(suite)
        {
            Debug.Assert(
                hashAlgorithm == HashAlgorithmName.SHA256 ||
                hashAlgorithm == HashAlgorithmName.SHA384 ||
                hashAlgorithm == HashAlgorithmName.SHA512);

            _hashAlgorithm = hashAlgorithm;
        }

        protected override void DeriveSecretsCore(
            byte mode,
            ReadOnlySpan<byte> sharedSecret,
            ReadOnlySpan<byte> info,
            ReadOnlySpan<byte> psk,
            ReadOnlySpan<byte> pskId,
            Span<byte> key,
            Span<byte> baseNonce,
            Span<byte> exporterSecret)
        {
            int hashLength = Suite.KdfMetadata.Nh;
            const int MaxStackContextLength = 1 + 2 * SHA512.HashSizeInBytes;
            Span<byte> contextBuffer = stackalloc byte[MaxStackContextLength];
            Span<byte> secretBuffer = stackalloc byte[SHA512.HashSizeInBytes];

            try
            {
                Span<byte> context = contextBuffer.Slice(0, 1 + 2 * hashLength);
                Span<byte> secret = secretBuffer.Slice(0, hashLength);
                context[0] = mode;
                LabeledExtract(ReadOnlySpan<byte>.Empty, "psk_id_hash"u8, pskId, context.Slice(1, hashLength));
                LabeledExtract(ReadOnlySpan<byte>.Empty, "info_hash"u8, info, context.Slice(1 + hashLength));
                LabeledExtract(sharedSecret, "secret"u8, psk, secret);

                LabeledExpand(secret, "key"u8, context, key);
                LabeledExpand(secret, "base_nonce"u8, context, baseNonce);
                LabeledExpand(secret, "exp"u8, context, exporterSecret);
            }
            finally
            {
                CryptographicOperations.ZeroMemory(contextBuffer);
                CryptographicOperations.ZeroMemory(secretBuffer);
            }
        }

        protected override void ExportSecretCore(
            ReadOnlySpan<byte> exporterSecret,
            ReadOnlySpan<byte> exporterContext,
            Span<byte> destination) =>
            LabeledExpand(exporterSecret, "sec"u8, exporterContext, destination);

        private void LabeledExtract(
            ReadOnlySpan<byte> salt,
            ReadOnlySpan<byte> label,
            ReadOnlySpan<byte> ikm,
            Span<byte> prk)
        {
            using (IncrementalHash hmac = IncrementalHash.CreateHMAC(_hashAlgorithm, salt))
            {
                hmac.AppendData(VersionLabel);
                hmac.AppendData(SuiteId);
                hmac.AppendData(label);
                hmac.AppendData(ikm);
                int written = hmac.GetHashAndReset(prk);
                Debug.Assert(written == prk.Length);
            }
        }

        private void LabeledExpand(
            ReadOnlySpan<byte> prk,
            ReadOnlySpan<byte> label,
            ReadOnlySpan<byte> info,
            Span<byte> output)
        {
            int length = checked(sizeof(ushort) + VersionLabel.Length + SuiteId.Length + label.Length + info.Length);
            const int MaxStackInfoLength = 256;

            using (CryptoPoolLease labeledInfo = CryptoPoolLease.RentConditionally(
                length, stackalloc byte[MaxStackInfoLength]))
            {
                Span<byte> buffer = labeledInfo.Span;
                BinaryPrimitives.WriteUInt16BigEndian(buffer, checked((ushort)output.Length));
                int offset = sizeof(ushort);
                VersionLabel.CopyTo(buffer.Slice(offset));
                offset += VersionLabel.Length;
                SuiteId.CopyTo(buffer.Slice(offset));
                offset += SuiteId.Length;
                label.CopyTo(buffer.Slice(offset));
                offset += label.Length;
                info.CopyTo(buffer.Slice(offset));

                HKDF.Expand(_hashAlgorithm, prk, output, buffer);
            }
        }
    }

    internal abstract class HpkeManagedShakeKdfAdapter : HpkeManagedKdfAdapter
    {
        protected HpkeManagedShakeKdfAdapter(HpkeSuite suite) : base(suite)
        {
        }

        protected override void DeriveSecretsCore(
            byte mode,
            ReadOnlySpan<byte> sharedSecret,
            ReadOnlySpan<byte> info,
            ReadOnlySpan<byte> psk,
            ReadOnlySpan<byte> pskId,
            Span<byte> key,
            Span<byte> baseNonce,
            Span<byte> exporterSecret)
        {
            // The single-stage schedule length-prefixes both secrets and application context.
            // https://datatracker.ietf.org/doc/html/draft-ietf-hpke-hpke-04#section-5.1
            int secretsLength = checked(2 * sizeof(ushort) + psk.Length + sharedSecret.Length);
            int contextLength = checked(1 + 2 * sizeof(ushort) + pskId.Length + info.Length);
            int outputLength = checked(key.Length + baseNonce.Length + exporterSecret.Length);
            const int MaxStackInputLength = 256;
            const int MaxStackOutputLength = 128;

            using (CryptoPoolLease secrets = CryptoPoolLease.RentConditionally(
                secretsLength, stackalloc byte[MaxStackInputLength]))
            using (CryptoPoolLease context = CryptoPoolLease.RentConditionally(
                contextLength, stackalloc byte[MaxStackInputLength]))
            {
                Span<byte> outputBuffer = stackalloc byte[MaxStackOutputLength];

                try
                {
                    Span<byte> output = outputBuffer.Slice(0, outputLength);
                    int offset = WriteLengthPrefixed(psk, secrets.Span);
                    WriteLengthPrefixed(sharedSecret, secrets.Span.Slice(offset));
                    context.Span[0] = mode;
                    offset = 1 + WriteLengthPrefixed(pskId, context.Span.Slice(1));
                    WriteLengthPrefixed(info, context.Span.Slice(offset));

                    LabeledDerive(secrets.Span, "secret"u8, context.Span, output);
                    output.Slice(0, key.Length).CopyTo(key);
                    output.Slice(key.Length, baseNonce.Length).CopyTo(baseNonce);
                    output.Slice(key.Length + baseNonce.Length).CopyTo(exporterSecret);
                }
                finally
                {
                    CryptographicOperations.ZeroMemory(outputBuffer);
                }
            }
        }

        protected override void ExportSecretCore(
            ReadOnlySpan<byte> exporterSecret,
            ReadOnlySpan<byte> exporterContext,
            Span<byte> destination) =>
            LabeledDerive(exporterSecret, "sec"u8, exporterContext, destination);

        private void LabeledDerive(
            ReadOnlySpan<byte> ikm,
            ReadOnlySpan<byte> label,
            ReadOnlySpan<byte> context,
            Span<byte> output)
        {
            // ikm || "HPKE-v1" || suite_id || lengthPrefixed(label) || I2OSP(L, 2) || context
            // https://datatracker.ietf.org/doc/html/draft-ietf-hpke-hpke-04#section-4.4
            int length = checked(VersionLabel.Length + SuiteId.Length + sizeof(ushort) + label.Length + sizeof(ushort));
            const int MaxStackPrefixLength = 64;
            Span<byte> prefixBuffer = stackalloc byte[MaxStackPrefixLength];

            try
            {
                Span<byte> prefix = prefixBuffer.Slice(0, length);
                VersionLabel.CopyTo(prefix);
                int offset = VersionLabel.Length;
                SuiteId.CopyTo(prefix.Slice(offset));
                offset += SuiteId.Length;
                offset += WriteLengthPrefixed(label, prefix.Slice(offset));
                BinaryPrimitives.WriteUInt16BigEndian(prefix.Slice(offset), checked((ushort)output.Length));

                Derive(ikm, prefix, context, output);
            }
            finally
            {
                CryptographicOperations.ZeroMemory(prefixBuffer);
            }
        }

        private static int WriteLengthPrefixed(ReadOnlySpan<byte> value, Span<byte> destination)
        {
            BinaryPrimitives.WriteUInt16BigEndian(destination, checked((ushort)value.Length));
            value.CopyTo(destination.Slice(sizeof(ushort)));
            return sizeof(ushort) + value.Length;
        }

        protected abstract void Derive(
            ReadOnlySpan<byte> ikm,
            ReadOnlySpan<byte> prefix,
            ReadOnlySpan<byte> context,
            Span<byte> output);
    }

    internal sealed class HpkeManagedShake128KdfAdapter : HpkeManagedShakeKdfAdapter
    {
        internal HpkeManagedShake128KdfAdapter(HpkeSuite suite) : base(suite)
        {
        }

        protected override void Derive(
            ReadOnlySpan<byte> ikm,
            ReadOnlySpan<byte> prefix,
            ReadOnlySpan<byte> context,
            Span<byte> output)
        {
            using (Shake128 shake = new Shake128())
            {
                shake.AppendData(ikm);
                shake.AppendData(prefix);
                shake.AppendData(context);
                shake.GetHashAndReset(output);
            }
        }
    }

    internal sealed class HpkeManagedShake256KdfAdapter : HpkeManagedShakeKdfAdapter
    {
        internal HpkeManagedShake256KdfAdapter(HpkeSuite suite) : base(suite)
        {
        }

        protected override void Derive(
            ReadOnlySpan<byte> ikm,
            ReadOnlySpan<byte> prefix,
            ReadOnlySpan<byte> context,
            Span<byte> output)
        {
            using (Shake256 shake = new Shake256())
            {
                shake.AppendData(ikm);
                shake.AppendData(prefix);
                shake.AppendData(context);
                shake.GetHashAndReset(output);
            }
        }
    }
}
