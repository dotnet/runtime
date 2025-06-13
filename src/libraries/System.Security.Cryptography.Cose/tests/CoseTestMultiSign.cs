// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Xunit;

namespace System.Security.Cryptography.Cose.Tests
{
    public sealed class CoseTestMultiSign
    {
        public bool IsEmbedded { get; }
        private string _label;
        private Func<CoseTestKey, byte[], byte[]> _signFirstImpl;
        private Action<CoseMultiSignMessage, CoseTestKey, byte[]> _addSignatureImpl;
        private Func<CoseTestKey, byte[], CoseSignature, bool> _verifyImpl;

        private CoseTestMultiSign(
            bool isEmbedded,
            string label,
            Func<CoseTestKey, byte[], byte[]> signFirstImpl,
            Action<CoseMultiSignMessage, CoseTestKey, byte[]> addSignatureImpl,
            Func<CoseTestKey, byte[], CoseSignature, bool> verifyImpl)
        {
            IsEmbedded = isEmbedded;
            _label = label;
            _signFirstImpl = signFirstImpl;
            _addSignatureImpl = addSignatureImpl;
            _verifyImpl = verifyImpl;
        }

        public byte[] Sign(CoseTestKeyManager keyManager, string[] keys, byte[] payload)
        {
            Assert.NotEmpty(keys);

            CoseTestKey firstKey = keyManager.GetKey(keys[0]);
            byte[] multiSignDocWithSingleSignature = _signFirstImpl(firstKey, payload);
            CoseMultiSignMessage multiSignDoc = CoseMessage.DecodeMultiSign(multiSignDocWithSingleSignature);

            for (int i = 1; i < keys.Length; i++)
            {
                CoseTestKey key = keyManager.GetKey(keys[i]);
                _addSignatureImpl(multiSignDoc, key, payload);
            }

            return multiSignDoc.Encode();
        }

        public bool Verify(CoseTestKeyManager keyManager, string[] expectedKeys, byte[] payload, byte[] signature)
        {
            Assert.NotEmpty(expectedKeys);

            HashSet<string> unusedExpectedKeys = new(expectedKeys);
            CoseMultiSignMessage multiSignDoc = CoseMessage.DecodeMultiSign(signature);
            foreach (CoseSignature singleSignature in multiSignDoc.Signatures)
            {
                Assert.True(singleSignature.ProtectedHeaders.TryGetValue(CoseHeaderLabel.KeyIdentifier, out CoseHeaderValue keyIdValue));
                byte[] keyIdBytes = keyIdValue.GetValueAsBytes();
                string keyId = Encoding.UTF8.GetString(keyIdBytes);

                if (!unusedExpectedKeys.Remove(keyId))
                {
                    // one of the signatures was used more than once or unexpected key
                    return false;
                }

                CoseTestKey key = keyManager.GetKey(keyId);
                if (!_verifyImpl(key, payload, singleSignature))
                {
                    // one of signatures doesn't verify
                    return false;
                }
            }

            // not signed with all signatures
            return unusedExpectedKeys.Count == 0;
        }

        public static IEnumerable<CoseTestMultiSign> GetImplementations()
        {
            const int SufficientSignatureSize = 16 * 1024;

            yield return new(true, "Sign/VerifyEmbedded(byte[])/AddSignatureForEmbedded(byte[])",
                (key, payload) => CoseMultiSignMessage.SignEmbedded(payload, key.Signer, associatedData: Array.Empty<byte>()),
                (message, key, _) => message.AddSignatureForEmbedded(key.Signer, Array.Empty<byte>()),
                (key, _, signature) => signature.VerifyEmbedded(key.CoseKey, Array.Empty<byte>()));

            yield return new(true, "Sign/VerifyEmbedded(ROS<byte>)/AddSignatureForEmbedded(ROS<byte>)",
                (key, payload) => CoseMultiSignMessage.SignEmbedded(payload.AsSpan(), key.Signer, associatedData: Span<byte>.Empty),
                (message, key, _) => message.AddSignatureForEmbedded(key.Signer, Span<byte>.Empty),
                (key, _, signature) => signature.VerifyEmbedded(key.CoseKey, Span<byte>.Empty));

            yield return new(true, "TrySign(ROS<byte>)/VerifyEmbedded(ROS<byte>)/AddSignatureForEmbedded(ROS<byte>)",
                (key, payload) =>
                {
                    byte[] destination = new byte[SufficientSignatureSize];
                    Assert.True(CoseMultiSignMessage.TrySignEmbedded(payload.AsSpan(), destination, key.Signer, out int bytesWritten, associatedData: ReadOnlySpan<byte>.Empty));

                    byte[] ret = new byte[bytesWritten];
                    destination.AsSpan(0, bytesWritten).CopyTo(ret);
                    return ret;
                },
                (message, key, _) => message.AddSignatureForEmbedded(key.Signer, Span<byte>.Empty),
                (key, _, signature) => signature.VerifyEmbedded(key.CoseKey, Span<byte>.Empty));

            yield return new(false, "Sign/VerifyDetached(byte[])/AddSignatureForDetached(byte[])",
                (key, payload) => CoseMultiSignMessage.SignDetached(payload, key.Signer, associatedData: Array.Empty<byte>()),
                (message, key, payload) => message.AddSignatureForDetached(payload, key.Signer, Array.Empty<byte>()),
                (key, payload, signature) => signature.VerifyDetached(key.CoseKey, payload, Array.Empty<byte>()));

            yield return new(false, "Sign/VerifyDetached(ROS<byte>)/AddSignatureForDetached(ROS<byte>)",
                (key, payload) => CoseMultiSignMessage.SignDetached(payload.AsSpan(), key.Signer, associatedData: Span<byte>.Empty),
                (message, key, payload) => message.AddSignatureForDetached(payload.AsSpan(), key.Signer, Span<byte>.Empty),
                (key, payload, signature) => signature.VerifyDetached(key.CoseKey, payload.AsSpan(), Span<byte>.Empty));

            yield return new(false, "TrySignDetached/VerifyDetached(ROS<byte>)/AddSignatureForDetached(ROS<byte>)",
                (key, payload) =>
                {
                    byte[] destination = new byte[SufficientSignatureSize];
                    Assert.True(CoseMultiSignMessage.TrySignDetached(payload.AsSpan(), destination, key.Signer, out int bytesWritten, associatedData: ReadOnlySpan<byte>.Empty));

                    byte[] ret = new byte[bytesWritten];
                    destination.AsSpan(0, bytesWritten).CopyTo(ret);
                    return ret;
                },
                (message, key, payload) => message.AddSignatureForDetached(payload.AsSpan(), key.Signer, Span<byte>.Empty),
                (key, payload, signature) => signature.VerifyDetached(key.CoseKey, payload.AsSpan(), Span<byte>.Empty));

            yield return new(false, "Sign/VerifyDetached(Stream)/AddSignatureForDetached(Stream)",
                (key, payload) =>
                {
                    using MemoryStream stream = new MemoryStream(payload);
                    return CoseMultiSignMessage.SignDetached(stream, key.Signer, associatedData: Span<byte>.Empty);
                },
                (message, key, payload) =>
                {
                    using MemoryStream stream = new MemoryStream(payload);
                    message.AddSignatureForDetached(stream, key.Signer, Span<byte>.Empty);
                },
                (key, payload, signature) =>
                {
                    using MemoryStream stream = new MemoryStream(payload);
                    return signature.VerifyDetached(key.CoseKey, stream, Span<byte>.Empty);
                });

            yield return new(false, "Sign/VerifyDetachedAsync(Stream)/AddSignatureForDetachedAsync(Stream)",
                (key, payload) =>
                {
                    using MemoryStream stream = new MemoryStream(payload);
                    return CoseMultiSignMessage.SignDetachedAsync(stream, key.Signer, associatedData: ReadOnlyMemory<byte>.Empty).GetAwaiter().GetResult();
                },
                (message, key, payload) =>
                {
                    using MemoryStream stream = new MemoryStream(payload);
                    message.AddSignatureForDetachedAsync(stream, key.Signer, ReadOnlyMemory<byte>.Empty).GetAwaiter().GetResult();
                },
                (key, payload, signature) =>
                {
                    using MemoryStream stream = new MemoryStream(payload);
                    return signature.VerifyDetachedAsync(key.CoseKey, stream, ReadOnlyMemory<byte>.Empty).GetAwaiter().GetResult();
                });
        }

        public override string ToString() => _label;
    }
}
