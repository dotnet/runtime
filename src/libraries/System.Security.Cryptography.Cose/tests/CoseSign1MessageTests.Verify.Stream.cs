// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.IO;
using System.Threading.Tasks;
using Xunit;
using static System.Security.Cryptography.Cose.Tests.CoseTestHelpers;

namespace System.Security.Cryptography.Cose.Tests
{
    public abstract class CoseSign1MessageTests_VerifyStream : CoseSign1MessageTests_Verify
    {
        internal override bool UseDetachedContent => true;
    }

    public class CoseSign1MessageTests_VerifyStream_Async : CoseSign1MessageTests_VerifyStream
    {
        internal override bool Verify(CoseMessage msg, AsymmetricAlgorithm key, byte[] content, byte[]? associatedData = null)
        {
            CoseSign1Message sign1Msg = Assert.IsType<CoseSign1Message>(msg);
            if (content == null)
            {
                return sign1Msg.VerifyDetachedAsync(key, null!, associatedData).GetAwaiter().GetResult();
            }

            using Stream stream = GetTestStream(content);
            return sign1Msg.VerifyDetachedAsync(key, stream, associatedData).GetAwaiter().GetResult();
        }

        internal override byte[] Sign(byte[] content, CoseSigner signer)
        {
            if (content == null)
            {
                return CoseSign1Message.SignDetachedAsync(null!, signer).GetAwaiter().GetResult();
            }

            using Stream stream = GetTestStream(content);
            return CoseSign1Message.SignDetachedAsync(stream, signer).GetAwaiter().GetResult();
        }

        [Fact]
        public async Task VerifyAsyncWithUnseekableStream()
        {
            using Stream stream = GetTestStream(s_sampleContent);
            byte[] encodedMsg = await CoseSign1Message.SignDetachedAsync(stream, GetCoseSigner(DefaultKey, DefaultHash));

            CoseSign1Message msg = CoseMessage.DecodeSign1(encodedMsg);
            using Stream unseekableStream = GetTestStream(s_sampleContent, StreamKind.Unseekable);
            await Assert.ThrowsAsync<ArgumentException>("detachedContent", () => msg.VerifyDetachedAsync(DefaultKey, unseekableStream));
        }

        [Fact]
        public async Task VerifyAsyncWithUnreadableStream()
        {
            using Stream stream = GetTestStream(s_sampleContent);
            byte[] encodedMsg = await CoseSign1Message.SignDetachedAsync(stream, GetCoseSigner(DefaultKey, DefaultHash));

            CoseSign1Message msg = CoseMessage.DecodeSign1(encodedMsg);
            using Stream unseekableStream = GetTestStream(s_sampleContent, StreamKind.Unreadable);
            await Assert.ThrowsAsync<ArgumentException>("detachedContent", () => msg.VerifyDetachedAsync(DefaultKey, unseekableStream));
        }
    }

    public class CoseSign1MessageTests_VerifyStream_Sync : CoseSign1MessageTests_VerifyStream
    {
        internal override bool Verify(CoseMessage msg, AsymmetricAlgorithm key, byte[] content, byte[]? associatedData = null)
        {
            CoseSign1Message sign1Msg = Assert.IsType<CoseSign1Message>(msg);
            if (content == null)
            {
                return sign1Msg.VerifyDetached(key, (Stream)null!, associatedData);
            }

            using Stream stream = GetTestStream(content);
            return sign1Msg.VerifyDetached(key, stream, associatedData);
        }

        internal override byte[] Sign(byte[] content, CoseSigner signer)
        {
            if (content == null)
            {
                return CoseSign1Message.SignDetached((Stream)null!, signer);
            }

            using Stream stream = GetTestStream(content);
            return CoseSign1Message.SignDetached(stream, signer);
        }

        [Fact]
        public void VerifyWithUnseekableStream()
        {
            using Stream stream = GetTestStream(s_sampleContent);
            byte[] encodedMsg = CoseSign1Message.SignDetached(stream, GetCoseSigner(DefaultKey, DefaultHash));

            CoseSign1Message msg = CoseMessage.DecodeSign1(encodedMsg);
            using Stream unseekableStream = GetTestStream(s_sampleContent, StreamKind.Unseekable);
            Assert.Throws<ArgumentException>("detachedContent", () => msg.VerifyDetached(DefaultKey, unseekableStream));
        }

        [Fact]
        public void VerifyWithUnreadableStream()
        {
            using Stream stream = GetTestStream(s_sampleContent);
            byte[] encodedMsg = CoseSign1Message.SignDetached(stream, GetCoseSigner(DefaultKey, DefaultHash));

            CoseSign1Message msg = CoseMessage.DecodeSign1(encodedMsg);
            using Stream unseekableStream = GetTestStream(s_sampleContent, StreamKind.Unreadable);
            Assert.Throws<ArgumentException>("detachedContent", () => msg.VerifyDetached(DefaultKey, unseekableStream));
        }
    }
}
