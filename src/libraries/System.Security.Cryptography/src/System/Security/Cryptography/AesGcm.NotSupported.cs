// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;

namespace System.Security.Cryptography
{
    public partial class AesGcm
    {
        public static partial bool IsSupported => false;
        public static partial KeySizes TagByteSizes => new KeySizes(12, 16, 1);

#pragma warning disable CA1822, IDE0060
        private partial void ImportKey(ReadOnlySpan<byte> key)
        {
            Debug.Fail("Instance ctor should fail before we reach this point.");
            throw new NotImplementedException();
        }

        private partial void EncryptCore(
            ReadOnlySpan<byte> nonce,
            ReadOnlySpan<byte> plaintext,
            Span<byte> ciphertext,
            Span<byte> tag,
            ReadOnlySpan<byte> associatedData)
        {
            Debug.Fail("Instance ctor should fail before we reach this point.");
            throw new NotImplementedException();
        }

        private partial void DecryptCore(
            ReadOnlySpan<byte> nonce,
            ReadOnlySpan<byte> ciphertext,
            ReadOnlySpan<byte> tag,
            Span<byte> plaintext,
            ReadOnlySpan<byte> associatedData)
        {
            Debug.Fail("Instance ctor should fail before we reach this point.");
            throw new NotImplementedException();
        }

        public partial void Dispose()
        {
            Debug.Fail("Instance ctor should fail before we reach this point.");
            // no-op
        }
#pragma warning restore CA1822, IDE0060
    }
}
