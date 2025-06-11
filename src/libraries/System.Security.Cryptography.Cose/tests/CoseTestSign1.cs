// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Xunit;

namespace System.Security.Cryptography.Cose.Tests
{
    public sealed class CoseTestSign1
    {
        public bool IsEmbedded { get; }
        private string _label;
        private Func<CoseTestKey, byte[], byte[]> _signImpl;
        private Func<CoseTestKey, byte[], CoseSign1Message, bool> _verifyImpl;

        private CoseTestSign1(bool isEmbedded, string label, Func<CoseTestKey, byte[], byte[]> signImpl, Func<CoseTestKey, byte[], CoseSign1Message, bool> verifyImpl)
        {
            IsEmbedded = isEmbedded;
            _label = label;
            _signImpl = signImpl;
            _verifyImpl = verifyImpl;
        }

        public byte[] Sign(CoseTestKeyManager keyManager, string key, byte[] payload)
        {
            CoseTestKey signer = keyManager.GetKey(key);
            return _signImpl(signer, payload);
        }

        public bool Verify(CoseTestKeyManager keyManager, string expectedKey, byte[] payload, byte[] signature)
        {
            CoseSign1Message message = CoseSign1Message.DecodeSign1(signature);

            Assert.True(message.ProtectedHeaders.TryGetValue(CoseHeaderLabel.KeyIdentifier, out CoseHeaderValue keyIdValue));
            byte[] keyIdBytes = keyIdValue.GetValueAsBytes();
            string keyId = Encoding.UTF8.GetString(keyIdBytes);
            if (keyId != expectedKey)
            {
                // key identifier doesn't match
                return false;
            }

            CoseTestKey key = keyManager.GetKey(expectedKey);
            return _verifyImpl(key, payload, message);
        }

        public static IEnumerable<CoseTestSign1> GetImplementations()
        {
            const int SufficientSignatureSize = 16 * 1024;

            yield return new(true, "Sign/VerifyEmbedded(byte[])",
                (key, payload) => CoseSign1Message.SignEmbedded(payload, key.Signer, Array.Empty<byte>()),
                (key, _, message) => message.VerifyEmbedded(key.CoseKey, Array.Empty<byte>()));

            yield return new(true, "Sign/VerifyEmbedded(ROS<byte>)",
                (key, payload) => CoseSign1Message.SignEmbedded(payload.AsSpan(), key.Signer, ReadOnlySpan<byte>.Empty),
                (key, _, message) => message.VerifyEmbedded(key.CoseKey, ReadOnlySpan<byte>.Empty));

            yield return new(true, "TrySignEmbedded/VerifyEmbedded(ROS<byte>)",
                (key, payload) =>
                {
                    byte[] destination = new byte[SufficientSignatureSize];
                    Assert.True(CoseSign1Message.TrySignEmbedded(payload.AsSpan(), destination, key.Signer, out int bytesWritten, ReadOnlySpan<byte>.Empty));

                    byte[] ret = new byte[bytesWritten];
                    destination.AsSpan(0, bytesWritten).CopyTo(ret);
                    return ret;
                },
                (key, _, message) => message.VerifyEmbedded(key.CoseKey, ReadOnlySpan<byte>.Empty));

            yield return new(false, "Sign/VerifyDetached(byte[])",
                (key, payload) => CoseSign1Message.SignDetached(payload, key.Signer, Array.Empty<byte>()),
                (key, payload, message) => message.VerifyDetached(key.CoseKey, payload, Array.Empty<byte>()));

            yield return new(false, "Sign/VerifyDetached(ROS<byte>)",
                (key, payload) => CoseSign1Message.SignDetached(payload.AsSpan(), key.Signer, ReadOnlySpan<byte>.Empty),
                (key, payload, message) => message.VerifyDetached(key.CoseKey, payload.AsSpan(), ReadOnlySpan<byte>.Empty));

            yield return new(false, "TrySignDetached/VerifyDetached(ROS<byte>)",
                (key, payload) =>
                {
                    byte[] destination = new byte[SufficientSignatureSize];
                    Assert.True(CoseSign1Message.TrySignDetached(payload.AsSpan(), destination, key.Signer, out int bytesWritten, ReadOnlySpan<byte>.Empty));

                    byte[] ret = new byte[bytesWritten];
                    destination.AsSpan(0, bytesWritten).CopyTo(ret);
                    return ret;
                },
                (key, payload, message) => message.VerifyDetached(key.CoseKey, payload.AsSpan(), ReadOnlySpan<byte>.Empty));

            yield return new(false, "Sign/VerifyDetached(Stream)",
                (key, payload) =>
                {
                    using MemoryStream stream = new MemoryStream(payload);
                    return CoseSign1Message.SignDetached(stream, key.Signer, ReadOnlySpan<byte>.Empty);
                },
                (key, payload, message) =>
                {
                    using MemoryStream stream = new MemoryStream(payload);
                    return message.VerifyDetached(key.CoseKey, stream, ReadOnlySpan<byte>.Empty);
                });

            yield return new(false, "Sign/VerifyDetachedAsync(Stream)",
                (key, payload) =>
                {
                    using MemoryStream stream = new MemoryStream(payload);
                    return CoseSign1Message.SignDetachedAsync(stream, key.Signer, ReadOnlyMemory<byte>.Empty).GetAwaiter().GetResult();
                },
                (key, payload, message) =>
                {
                    using MemoryStream stream = new MemoryStream(payload);
                    return message.VerifyDetachedAsync(key.CoseKey, stream, ReadOnlyMemory<byte>.Empty).GetAwaiter().GetResult();
                });
        }

        public override string ToString() => _label;
    }
}
