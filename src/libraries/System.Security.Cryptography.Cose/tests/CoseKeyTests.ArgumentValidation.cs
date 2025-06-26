// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Formats.Cbor;
using System.IO;
using System.Linq;
using System.Text;
using Test.Cryptography;
using Xunit;

namespace System.Security.Cryptography.Cose.Tests
{
    public partial class CoseKeyTests
    {
        [Fact]
        public void CreateCoseKeyWithNullArg_Throws()
        {
            AssertExtensions.Throws<ArgumentNullException>("key", static () => CoseKey.FromKey((MLDsa)null!));
            AssertExtensions.Throws<ArgumentNullException>("key", static () => CoseKey.FromKey((ECDsa)null!, HashAlgorithmName.SHA256));
            AssertExtensions.Throws<ArgumentNullException>("key", static () => CoseKey.FromKey((RSA)null!, RSASignaturePadding.Pkcs1, HashAlgorithmName.SHA256));

            AssertExtensions.Throws<ArgumentNullException>("signaturePadding", () => CoseKey.FromKey(CoseTestHelpers.RSAKey, null!, HashAlgorithmName.SHA256));
            AssertExtensions.Throws<ArgumentNullException>("hashAlgorithm.Name", () => CoseKey.FromKey(CoseTestHelpers.RSAKey, RSASignaturePadding.Pkcs1, default));

            AssertExtensions.Throws<ArgumentNullException>("hashAlgorithm.Name", () => CoseKey.FromKey(CoseTestHelpers.ES256, default));
        }

        [Fact]
        public void CreateCoseSignerWithNullCoseKey_Throws()
        {
            AssertExtensions.Throws<ArgumentNullException>("key", static () => new CoseSigner((CoseKey)null!));
        }

        [Fact]
        public void VerifySingleSignerWithNullCoseKey_Throws()
        {
            CoseKey key = CoseKey.FromKey(CoseTestHelpers.ES256, HashAlgorithmName.SHA256);
            CoseSigner signer = new CoseSigner(key);
            byte[] payload = Encoding.UTF8.GetBytes(nameof(VerifySingleSignerWithNullCoseKey_Throws));
            MemoryStream payloadStream = new(payload);
            byte[] embeddedMessageBytes = CoseSign1Message.SignEmbedded(payload, signer, Array.Empty<byte>());
            byte[] detatchedMessageBytes = CoseSign1Message.SignDetached(payload, signer, Array.Empty<byte>());

            CoseSign1Message embeddedMessage = CoseSign1Message.DecodeSign1(embeddedMessageBytes);
            CoseSign1Message detachedMessage = CoseSign1Message.DecodeSign1(detatchedMessageBytes);

            AssertExtensions.Throws<ArgumentNullException>("key", () => embeddedMessage.VerifyEmbedded((CoseKey)null!, ReadOnlySpan<byte>.Empty));
            AssertExtensions.Throws<ArgumentNullException>("key", () => detachedMessage.VerifyDetached((CoseKey)null!, payload, Array.Empty<byte>()));
            AssertExtensions.Throws<ArgumentNullException>("key", () => detachedMessage.VerifyDetached((CoseKey)null!, payloadStream));
            AssertExtensions.Throws<ArgumentNullException>("key", () => detachedMessage.VerifyDetachedAsync((CoseKey)null!, payloadStream)); // null check synchronously
        }

        [Fact]
        public void VerifyMultiSignerWithNullCoseKey_Throws()
        {
            CoseKey key = CoseKey.FromKey(CoseTestHelpers.ES256, HashAlgorithmName.SHA256);
            CoseSigner signer = new CoseSigner(key);
            byte[] payload = Encoding.UTF8.GetBytes(nameof(VerifySingleSignerWithNullCoseKey_Throws));
            MemoryStream payloadStream = new(payload);
            byte[] embeddedMessageBytes = CoseMultiSignMessage.SignEmbedded(payload, signer);
            byte[] detatchedMessageBytes = CoseMultiSignMessage.SignDetached(payload, signer);

            CoseMultiSignMessage embeddedMessage = CoseMultiSignMessage.DecodeMultiSign(embeddedMessageBytes);
            CoseMultiSignMessage detachedMessage = CoseMultiSignMessage.DecodeMultiSign(detatchedMessageBytes);

            AssertExtensions.Throws<ArgumentNullException>("key", () => embeddedMessage.Signatures[0].VerifyEmbedded((CoseKey)null!, ReadOnlySpan<byte>.Empty));
            AssertExtensions.Throws<ArgumentNullException>("key", () => detachedMessage.Signatures[0].VerifyDetached((CoseKey)null!, payload, Array.Empty<byte>()));
            AssertExtensions.Throws<ArgumentNullException>("key", () => detachedMessage.Signatures[0].VerifyDetached((CoseKey)null!, payloadStream));
            AssertExtensions.Throws<ArgumentNullException>("key", () => detachedMessage.Signatures[0].VerifyDetachedAsync((CoseKey)null!, payloadStream)); // null check synchronously
        }
    }
}
