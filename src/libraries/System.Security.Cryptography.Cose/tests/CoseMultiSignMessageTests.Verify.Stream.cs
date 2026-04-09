// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.ObjectModel;
using System.IO;
using System.Threading.Tasks;
using Xunit;
using static System.Security.Cryptography.Cose.Tests.CoseTestHelpers;

namespace System.Security.Cryptography.Cose.Tests
{
    public abstract class CoseMultiSignMessageTests_VerifyStream : CoseMultiSignMessageTests_Verify
    {
        internal override bool UseDetachedContent => true;
    }

    public class CoseMultiSignMessageTests_VerifyStream_Async : CoseMultiSignMessageTests_VerifyStream
    {
        internal override bool Verify(CoseMessage msg, AsymmetricAlgorithm key, byte[] content, byte[]? associatedData = null)
        {
            CoseMultiSignMessage multiSignMsg = Assert.IsType<CoseMultiSignMessage>(msg);
            ReadOnlyCollection<CoseSignature> signatures = multiSignMsg.Signatures;
            Assert.Equal(1, signatures.Count);

            if (content == null)
            {
                return signatures[0].VerifyDetachedAsync(key, null!, associatedData).GetAwaiter().GetResult();
            }

            using Stream stream = GetTestStream(content);
            return signatures[0].VerifyDetachedAsync(key, stream, associatedData).GetAwaiter().GetResult();
        }

        internal override byte[] Sign(byte[] content, CoseSigner signer)
        {
            if (content == null)
            {
                return CoseMultiSignMessage.SignDetachedAsync(null!, signer).GetAwaiter().GetResult();
            }

            using Stream stream = GetTestStream(content);
            return CoseMultiSignMessage.SignDetachedAsync(stream, signer).GetAwaiter().GetResult();
        }

        [Fact]
        public async Task VerifyAsyncWithUnseekableStream()
        {
            using Stream stream = GetTestStream(s_sampleContent);
            byte[] encodedMsg = await CoseMultiSignMessage.SignDetachedAsync(stream, GetCoseSigner(DefaultKey, DefaultHash));

            CoseMultiSignMessage msg = CoseMessage.DecodeMultiSign(encodedMsg);
            using Stream unseekableStream = GetTestStream(s_sampleContent, StreamKind.Unseekable);
            await Assert.ThrowsAsync<ArgumentException>("detachedContent", () => msg.Signatures[0].VerifyDetachedAsync(DefaultKey, unseekableStream));
        }

        [Fact]
        public async Task VerifyAsyncWithUnreadableStream()
        {
            using Stream stream = GetTestStream(s_sampleContent);
            byte[] encodedMsg = await CoseMultiSignMessage.SignDetachedAsync(stream, GetCoseSigner(DefaultKey, DefaultHash));

            CoseMultiSignMessage msg = CoseMessage.DecodeMultiSign(encodedMsg);
            using Stream unseekableStream = GetTestStream(s_sampleContent, StreamKind.Unreadable);
            await Assert.ThrowsAsync<ArgumentException>("detachedContent", () => msg.Signatures[0].VerifyDetachedAsync(DefaultKey, unseekableStream));
        }
    }

    public class CoseMultiSignMessageTests_VerifyStream_Sync : CoseMultiSignMessageTests_VerifyStream
    {
        internal override bool Verify(CoseMessage msg, AsymmetricAlgorithm key, byte[] content, byte[]? associatedData = null)
        {
            CoseMultiSignMessage multiSignMsg = Assert.IsType<CoseMultiSignMessage>(msg);
            ReadOnlyCollection<CoseSignature> signatures = multiSignMsg.Signatures;
            Assert.Equal(1, signatures.Count);

            if (content == null)
            {
                return signatures[0].VerifyDetached(key, (Stream)null!, associatedData);
            }

            using Stream stream = GetTestStream(content);
            return signatures[0].VerifyDetached(key, stream, associatedData);
        }

        internal override byte[] Sign(byte[] content, CoseSigner signer)
        {
            if (content == null)
            {
                return CoseMultiSignMessage.SignDetached((Stream)null!, signer);
            }

            using Stream stream = GetTestStream(content);
            return CoseMultiSignMessage.SignDetached(stream, signer);
        }

        [Fact]
        public void VerifyWithUnseekableStream()
        {
            using Stream stream = GetTestStream(s_sampleContent);
            byte[] encodedMsg = CoseMultiSignMessage.SignDetached(stream, GetCoseSigner(DefaultKey, DefaultHash));

            CoseMultiSignMessage msg = CoseMessage.DecodeMultiSign(encodedMsg);
            using Stream unseekableStream = GetTestStream(s_sampleContent, StreamKind.Unseekable);
            Assert.Throws<ArgumentException>("detachedContent", () => msg.Signatures[0].VerifyDetached(DefaultKey, unseekableStream));
        }

        [Fact]
        public void VerifyWithUnreadableStream()
        {
            using Stream stream = GetTestStream(s_sampleContent);
            byte[] encodedMsg = CoseMultiSignMessage.SignDetached(stream, GetCoseSigner(DefaultKey, DefaultHash));

            CoseMultiSignMessage msg = CoseMessage.DecodeMultiSign(encodedMsg);
            using Stream unseekableStream = GetTestStream(s_sampleContent, StreamKind.Unreadable);
            Assert.Throws<ArgumentException>("detachedContent", () => msg.Signatures[0].VerifyDetached(DefaultKey, unseekableStream));
        }
    }
}
