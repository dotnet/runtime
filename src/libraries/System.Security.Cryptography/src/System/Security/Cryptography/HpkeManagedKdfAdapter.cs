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
            try
            {
                // Callers slice these buffers to the suite's exact output sizes.
                Debug.Assert(key.Length == Suite.AeadMetadata.Nk);
                Debug.Assert(baseNonce.Length == Suite.AeadMetadata.Nn);
                Debug.Assert(exporterSecret.Length == Suite.KdfMetadata.Nh);

                DeriveSecretsCore(mode, sharedSecret, info, psk, pskId, key, baseNonce, exporterSecret);
            }
            catch
            {
                CryptographicOperations.ZeroMemory(key);
                CryptographicOperations.ZeroMemory(exporterSecret);
                throw;
            }
        }

        // https://datatracker.ietf.org/doc/html/draft-ietf-hpke-hpke-04#section-5.3
        internal void ExportSecret(
            ReadOnlySpan<byte> exporterSecret,
            ReadOnlySpan<byte> exporterContext,
            Span<byte> destination)
        {
            Debug.Assert(exporterSecret.Length == Suite.KdfMetadata.Nh);
            Debug.Assert(destination.Length <= Suite.KdfMetadata.MaximumExportLength);

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
            // One mode byte plus psk_id_hash and info_hash; SHA-512 has the largest supported hash size.
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
            // This is HPKE-Extract, but using HMAC directly so we don't need a contiguous buffer of all of
            // these components. HKDF-Extract is defined as `PRK = HMAC-Hash(salt, IKM)`.
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

            Span<byte> buffer = length <= MaxStackInfoLength
                ? stackalloc byte[MaxStackInfoLength]
                : new byte[length];
            buffer = buffer.Slice(0, length);
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

    internal abstract class HpkeManagedShakeKdfAdapter<TShake> : HpkeManagedKdfAdapter
        where TShake : class, IDisposable
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
            int outputLength = checked(key.Length + baseNonce.Length + exporterSecret.Length);
            const int MaxStackOutputLength = 128;

            // Current suites require at most 32 + 12 + 64 bytes for these outputs.
            Debug.Assert(outputLength <= MaxStackOutputLength);

            using (TShake shake = CreateShake())
            {
                Span<byte> outputBuffer = stackalloc byte[MaxStackOutputLength];

                try
                {
                    Span<byte> output = outputBuffer.Slice(0, outputLength);

                    AppendLengthPrefixed(shake, psk);
                    AppendLengthPrefixed(shake, sharedSecret);
                    AppendLabeledDerivePrefix(shake, "secret"u8, outputLength);
                    Append(shake, new ReadOnlySpan<byte>(in mode));
                    AppendLengthPrefixed(shake, pskId);
                    AppendLengthPrefixed(shake, info);

                    GetHashAndReset(shake, output);
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
            Span<byte> destination)
        {
            using (TShake shake = CreateShake())
            {
                Append(shake, exporterSecret);
                AppendLabeledDerivePrefix(shake, "sec"u8, destination.Length);
                Append(shake, exporterContext);
                GetHashAndReset(shake, destination);
            }
        }

        // "HPKE-v1" || suite_id || lengthPrefixed(label) || I2OSP(L, 2)
        // https://datatracker.ietf.org/doc/html/draft-ietf-hpke-hpke-04#section-4.4
        private void AppendLabeledDerivePrefix(TShake shake, ReadOnlySpan<byte> label, int outputLength)
        {
            Append(shake, VersionLabel);
            Append(shake, SuiteId);
            AppendLengthPrefixed(shake, label);
            Span<byte> lengthBytes = stackalloc byte[sizeof(ushort)];
            BinaryPrimitives.WriteUInt16BigEndian(lengthBytes, checked((ushort)outputLength));
            Append(shake, lengthBytes);
        }

        private void AppendLengthPrefixed(TShake shake, ReadOnlySpan<byte> value)
        {
            Span<byte> lengthBytes = stackalloc byte[sizeof(ushort)];
            BinaryPrimitives.WriteUInt16BigEndian(lengthBytes, checked((ushort)value.Length));
            Append(shake, lengthBytes);
            Append(shake, value);
        }

        protected abstract TShake CreateShake();
        protected abstract void Append(TShake shake, ReadOnlySpan<byte> data);
        protected abstract void GetHashAndReset(TShake shake, Span<byte> destination);
    }

    internal sealed class HpkeManagedShake128KdfAdapter : HpkeManagedShakeKdfAdapter<Shake128>
    {
        internal HpkeManagedShake128KdfAdapter(HpkeSuite suite) : base(suite)
        {
        }

        protected override Shake128 CreateShake() => new Shake128();

        protected override void Append(Shake128 shake, ReadOnlySpan<byte> data) =>
            shake.AppendData(data);

        protected override void GetHashAndReset(Shake128 shake, Span<byte> destination) =>
            shake.GetHashAndReset(destination);
    }

    internal sealed class HpkeManagedShake256KdfAdapter : HpkeManagedShakeKdfAdapter<Shake256>
    {
        internal HpkeManagedShake256KdfAdapter(HpkeSuite suite) : base(suite)
        {
        }

        protected override Shake256 CreateShake() => new Shake256();

        protected override void Append(Shake256 shake, ReadOnlySpan<byte> data) =>
            shake.AppendData(data);

        protected override void GetHashAndReset(Shake256 shake, Span<byte> destination) =>
            shake.GetHashAndReset(destination);
    }
}
