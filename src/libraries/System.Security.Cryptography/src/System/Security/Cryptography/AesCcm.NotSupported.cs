// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;

namespace System.Security.Cryptography
{
    public partial class AesCcm
    {
        public static bool IsSupported => false;

#pragma warning disable CA1822
        private void ImportKey(ReadOnlySpan<byte> key)
        {
            Debug.Fail("Instance ctor should fail before we reach this point.");
            throw new NotImplementedException();
        }

        private void EncryptCore(
            ReadOnlySpan<byte> nonce,
            ReadOnlySpan<byte> plaintext,
            Span<byte> ciphertext,
            Span<byte> tag,
            ReadOnlySpan<byte> associatedData = default)
        {
            Debug.Fail("Instance ctor should fail before we reach this point.");
            throw new NotImplementedException();
        }

        private void DecryptCore(
            ReadOnlySpan<byte> nonce,
            ReadOnlySpan<byte> ciphertext,
            ReadOnlySpan<byte> tag,
            Span<byte> plaintext,
            ReadOnlySpan<byte> associatedData = default)
        {
            Debug.Fail("Instance ctor should fail before we reach this point.");
            throw new NotImplementedException();
        }

        public void Dispose()
        {
            Debug.Fail("Instance ctor should fail before we reach this point.");
            // no-op
        }
#pragma warning restore CA1822
    }
}
