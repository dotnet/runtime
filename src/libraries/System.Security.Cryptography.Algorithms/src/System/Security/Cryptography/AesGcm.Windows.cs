// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Internal.Cryptography;
using Internal.NativeCrypto;
using static Interop.BCrypt;

namespace System.Security.Cryptography
{
    partial class AesGcm
    {
        private static readonly SafeAlgorithmHandle s_aesGcm = AesBCryptModes.OpenAesAlgorithm(Cng.BCRYPT_CHAIN_MODE_GCM);
        private SafeKeyHandle _keyHandle;

        private void ImportKey(ReadOnlySpan<byte> key)
        {
            _keyHandle = s_aesGcm.BCryptImportKey(key);
        }

        private void EncryptInternal(
            ReadOnlySpan<byte> nonce,
            ReadOnlySpan<byte> plaintext,
            Span<byte> ciphertext,
            Span<byte> tag,
            ReadOnlySpan<byte> associatedData = default)
        {
            AesAEAD.Encrypt(s_aesGcm, _keyHandle, nonce, associatedData, plaintext, ciphertext, tag);
        }

        private void DecryptInternal(
            ReadOnlySpan<byte> nonce,
            ReadOnlySpan<byte> ciphertext,
            ReadOnlySpan<byte> tag,
            Span<byte> plaintext,
            ReadOnlySpan<byte> associatedData = default)
        {
            AesAEAD.Decrypt(s_aesGcm, _keyHandle, nonce, associatedData, ciphertext, tag, plaintext, clearPlaintextOnFailure: true);
        }

        public void Dispose()
        {
            _keyHandle.Dispose();
        }
    }
}
