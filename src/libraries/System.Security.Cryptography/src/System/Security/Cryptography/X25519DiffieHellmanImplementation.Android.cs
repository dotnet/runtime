// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Formats.Asn1;

namespace System.Security.Cryptography
{
    internal sealed class X25519DiffieHellmanImplementation : X25519DiffieHellman
    {
        private static readonly Lazy<SafeX25519PublicKeyHandle> s_basePointHandle = new(static () =>
        {
            ReadOnlySpan<byte> basePoint =
            [
                9, 0, 0, 0, 0, 0, 0, 0,
                0, 0, 0, 0, 0, 0, 0, 0,
                0, 0, 0, 0, 0, 0, 0, 0,
                0, 0, 0, 0, 0, 0, 0, 0,
            ];

            return ImportPublicKeyAsHandle(basePoint);
        });

        private readonly SafeX25519PublicKeyHandle _publicKey;
        private readonly SafeX25519PrivateKeyHandle? _privateKey;

        internal static new bool IsSupported { get; } = Interop.AndroidCrypto.X25519IsSupported();

        private X25519DiffieHellmanImplementation(
            SafeX25519PublicKeyHandle publicKey,
            SafeX25519PrivateKeyHandle? privateKey)
        {
            _publicKey = publicKey;
            _privateKey = privateKey;
        }

        protected override void DeriveRawSecretAgreementCore(X25519DiffieHellman otherParty, Span<byte> destination)
        {
            Debug.Assert(destination.Length == SecretAgreementSizeInBytes);
            ThrowIfPrivateNeeded();

            if (otherParty is X25519DiffieHellmanImplementation otherImpl)
            {
                DeriveRawSecretAgreementCore(_privateKey, otherImpl._publicKey, destination);
            }
            else
            {
                unsafe
                {
                    Span<byte> otherPublicKey = stackalloc byte[PublicKeySizeInBytes];
                    otherParty.ExportPublicKey(otherPublicKey);
                    DeriveRawSecretAgreementCore(otherPublicKey, destination);
                }
            }
        }

        protected override void DeriveRawSecretAgreementCore(ReadOnlySpan<byte> otherPartyPublicKey, Span<byte> destination)
        {
            Debug.Assert(otherPartyPublicKey.Length == PublicKeySizeInBytes);
            Debug.Assert(destination.Length == SecretAgreementSizeInBytes);
            ThrowIfPrivateNeeded();

            unsafe
            {
                Span<byte> spki = stackalloc byte[SpkiSizeInBytes];

                bool encoded = TryWriteSubjectPublicKeyInfo(
                    spki,
                    otherPartyPublicKey,
                    static (source, buffer) => source.CopyTo(buffer),
                    out int written);

                // SPKI encoding is either right or wrong, there aren't "optional" things that can be written down. So it
                // should be precisely sized.
                if (!encoded || written != SpkiSizeInBytes)
                {
                    throw new CryptographicException();
                }

                Interop.AndroidCrypto.X25519DeriveSecretWithSubjectPublicKeyInfo(_privateKey, spki, destination);
            }
        }

        protected override void ExportPrivateKeyCore(Span<byte> destination)
        {
            Debug.Assert(destination.Length == PrivateKeySizeInBytes);
            ThrowIfPrivateNeeded();

            // PKCS#8 keys are not strictly deterministic in size because they could have attributes as "metadata"
            // attached. A minimally encoded PKCS#8 private key is going to be 48 bytes. 512 bytes is 10x more space
            // than needed, but anything larger than that we won't attempt to process.
            scoped Span<byte> pkcs8Buffer;

            unsafe
            {
                pkcs8Buffer = stackalloc byte[512];
            }

            if (!Interop.AndroidCrypto.X25519TryExportPkcs8PrivateKey(_privateKey, pkcs8Buffer, out int written))
            {
                Debug.Fail($"X25519 PKCS#8 PrivateKeyInfo did not fit in {pkcs8Buffer.Length} bytes.");
                throw new CryptographicException(SR.Argument_DestinationTooShort);
            }

            try
            {
                ReadOnlySpan<byte> privateKeyContents = KeyFormatHelper.ReadPkcs8(
                    s_knownOids,
                    pkcs8Buffer.Slice(0, written),
                    out int bytesRead,
                    permitParameters: false);

                Debug.Assert(bytesRead == written);

                ValueAsnReader reader = new(privateKeyContents, AsnEncodingRules.BER);

                if (reader.TryReadPrimitiveOctetString(out ReadOnlySpan<byte> privateKey))
                {
                    if (privateKey.Length != PrivateKeySizeInBytes)
                    {
                        throw new CryptographicException(SR.Argument_PrivateKeyWrongSizeForAlgorithm);
                    }

                    privateKey.CopyTo(destination);
                }
                else
                {
                    byte[] allocatedPrivateKey = reader.ReadOctetString();

                    try
                    {
                        if (allocatedPrivateKey.Length != PrivateKeySizeInBytes)
                        {
                            throw new CryptographicException(SR.Argument_PrivateKeyWrongSizeForAlgorithm);
                        }

                        allocatedPrivateKey.CopyTo(destination);
                    }
                    finally
                    {
                        CryptographicOperations.ZeroMemory(allocatedPrivateKey);
                    }
                }

                reader.ThrowIfNotEmpty();
            }
            catch (AsnContentException e)
            {
                throw new CryptographicException(SR.Cryptography_Der_Invalid_Encoding, e);
            }
            finally
            {
                CryptographicOperations.ZeroMemory(pkcs8Buffer.Slice(0, written));
            }
        }

        protected override void ExportPublicKeyCore(Span<byte> destination)
        {
            Debug.Assert(destination.Length == PublicKeySizeInBytes);


            scoped Span<byte> spkiBuffer;

            unsafe
            {
                spkiBuffer = stackalloc byte[SpkiSizeInBytes];
            }

            // A SPKI has no wiggle room - they are DER, we expect no algorithm parameters, etc. Either it is exactly
            // the right size, or it's wrong.
            if (!Interop.AndroidCrypto.X25519TryExportSubjectPublicKeyInfo(_publicKey, spkiBuffer, out int written)
                || written != SpkiSizeInBytes)
            {
                Debug.Fail($"X25519 SubjectPublicKeyInfo did not fit in {spkiBuffer.Length} bytes or wrote the incorrect amount.");
                throw new CryptographicException();
            }

            ReadOnlySpan<byte> key = KeyFormatHelper.ReadSubjectPublicKeyInfo(
                s_knownOids,
                spkiBuffer,
                out int read,
                permitParameters: false);

            Debug.Assert(read == SpkiSizeInBytes);
            key.CopyTo(destination);
        }

        protected override bool TryExportPkcs8PrivateKeyCore(Span<byte> destination, out int bytesWritten)
        {
            ThrowIfPrivateNeeded();
            return TryExportPkcs8PrivateKeyImpl(destination, out bytesWritten);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _publicKey.Dispose();
                _privateKey?.Dispose();
            }

            base.Dispose(disposing);
        }

        internal static X25519DiffieHellmanImplementation GenerateKeyImpl()
        {
            Interop.AndroidCrypto.X25519GenerateKey(
                out SafeX25519PublicKeyHandle publicKey,
                out SafeX25519PrivateKeyHandle privateKey);

            return new X25519DiffieHellmanImplementation(publicKey, privateKey);
        }

        internal static X25519DiffieHellmanImplementation ImportPrivateKeyImpl(ReadOnlySpan<byte> source)
        {
            Debug.Assert(source.Length == PrivateKeySizeInBytes);

            unsafe
            {
                Span<byte> pkcs8 = stackalloc byte[Pkcs8SizeInBytes];

                bool encoded = TryWritePkcs8PrivateKey(
                    pkcs8,
                    source,
                    static (source, buffer) => source.CopyTo(buffer),
                    out int written);

                Debug.Assert(encoded);
                Debug.Assert(written == Pkcs8SizeInBytes);

                SafeX25519PrivateKeyHandle privateKey = Interop.AndroidCrypto.X25519ImportPkcs8PrivateKey(pkcs8.Slice(0, written));

                try
                {
                    // Android's native implementation gives us two handles, one for the private key and one for the
                    // public key when generating a key pair. When importing a private key, it only gives us a handle
                    // representing the private key back. This makes it difficult to export the public key out of a private
                    // key handle since the export on the handle doesn't specify whether you want the public or private key.
                    // To recover the public key from the private key, we do X25519(9, key). This is how the public key
                    // is computed per RFC7748, section 6.1.
                    Span<byte> publicKeyBytes = stackalloc byte[PublicKeySizeInBytes];
                    DeriveRawSecretAgreementCore(privateKey, s_basePointHandle.Value, publicKeyBytes);
                    SafeX25519PublicKeyHandle publicKey = ImportPublicKeyAsHandle(publicKeyBytes);
                    return new X25519DiffieHellmanImplementation(publicKey, privateKey);
                }
                catch
                {
                    privateKey.Dispose();
                    throw;
                }
                finally
                {
                    CryptographicOperations.ZeroMemory(pkcs8);
                }
            }
        }

        internal static X25519DiffieHellmanImplementation ImportPublicKeyImpl(ReadOnlySpan<byte> source)
        {
            Debug.Assert(source.Length == PublicKeySizeInBytes);
            return new X25519DiffieHellmanImplementation(ImportPublicKeyAsHandle(source), privateKey: null);
        }

        [MemberNotNull(nameof(_privateKey))]
        private void ThrowIfPrivateNeeded()
        {
            if (_privateKey is null)
            {
                throw new CryptographicException(SR.Cryptography_CSP_NoPrivateKey);
            }
        }

        private static void DeriveRawSecretAgreementCore(
            SafeX25519PrivateKeyHandle currentParty,
            SafeX25519PublicKeyHandle otherParty,
            Span<byte> destination)
        {
            Debug.Assert(destination.Length == SecretAgreementSizeInBytes);
            Interop.AndroidCrypto.X25519DeriveSecret(currentParty, otherParty, destination);
        }

        private static SafeX25519PublicKeyHandle ImportPublicKeyAsHandle(ReadOnlySpan<byte> source)
        {
            scoped Span<byte> spki;

            unsafe
            {
                 spki = stackalloc byte[SpkiSizeInBytes];
            }

            bool encoded = TryWriteSubjectPublicKeyInfo(
                spki,
                source,
                static (source, buffer) => source.CopyTo(buffer),
                out int written);

            // SPKI encoding is either right or wrong, there aren't "optional" things that can be written down. So it
            // should be precisely sized.
            if (!encoded || written != SpkiSizeInBytes)
            {
                throw new CryptographicException();
            }

            return Interop.AndroidCrypto.X25519ImportSubjectPublicKeyInfo(spki);
        }
    }
}
