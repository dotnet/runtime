// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;

namespace System.Security.Cryptography
{
    internal sealed class HpkeImplementation : Hpke
    {
        internal HpkeImplementation(HpkeSuite suite) : base(suite)
        {
        }

        internal static bool IsSupportedImpl(HpkeSuite suite)
        {
            _ = suite;
            return false;
        }

        internal static HpkeImplementation DeriveKeyImpl(HpkeSuite suite, ReadOnlySpan<byte> ikm)
        {
            _ = suite;
            _ = ikm;
            Debug.Fail("Platform validation should not permit this call.");
            throw new CryptographicException();
        }

        internal static HpkeImplementation GenerateKeyImpl(HpkeSuite suite)
        {
            _ = suite;
            Debug.Fail("Platform validation should not permit this call.");
            throw new CryptographicException();
        }

        protected override void ExportDecapsulationKeyCore(Span<byte> destination)
        {
            _ = destination;
            Debug.Fail("Platform validation should not permit this call.");
            throw new CryptographicException();
        }

        protected override void ExportEncapsulationKeyCore(Span<byte> destination)
        {
            _ = destination;
            Debug.Fail("Platform validation should not permit this call.");
            throw new CryptographicException();
        }

        protected override void SealCore(
            ReadOnlySpan<byte> plaintext,
            Span<byte> encapsulatedSecret,
            Span<byte> ciphertext,
            ReadOnlySpan<byte> associatedData,
            ReadOnlySpan<byte> info)
        {
            _ = plaintext;
            _ = encapsulatedSecret;
            _ = ciphertext;
            _ = associatedData;
            _ = info;
            Debug.Fail("Platform validation should not permit this call.");
            throw new CryptographicException();
        }

        protected override void OpenCore(
            ReadOnlySpan<byte> encapsulatedSecret,
            ReadOnlySpan<byte> ciphertext,
            Span<byte> plaintext,
            ReadOnlySpan<byte> associatedData,
            ReadOnlySpan<byte> info)
        {
            _ = encapsulatedSecret;
            _ = ciphertext;
            _ = plaintext;
            _ = associatedData;
            _ = info;
            Debug.Fail("Platform validation should not permit this call.");
            throw new CryptographicException();
        }

        protected override HpkeSender CreateSenderCore(Span<byte> encapsulatedSecret, ReadOnlySpan<byte> info) =>
            throw new PlatformNotSupportedException();

        protected override HpkeRecipient CreateRecipientCore(
            ReadOnlySpan<byte> encapsulatedSecret,
            ReadOnlySpan<byte> info) =>
            throw new PlatformNotSupportedException();

        protected override void Dispose(bool disposing)
        {
            Debug.Fail("Platform validation should not permit this call.");
            throw new CryptographicException();
        }
    }
}
