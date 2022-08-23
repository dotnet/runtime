// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.IO;
using System.Threading.Tasks;
using Xunit;
using static System.Security.Cryptography.Cose.Tests.CoseTestHelpers;

namespace System.Security.Cryptography.Cose.Tests
{
    public abstract class CoseMessageTests_SignStream : CoseMessageTests_Sign_CustomHeaderMaps
    {
        internal override bool OnlySupportsDetachedContent => true;
    }

    public abstract class CoseMessageTests_SignStream_Async : CoseMessageTests_SignStream
    {
        internal abstract Task<byte[]> SignDetachedAsync(Stream detachedContent, CoseSigner signer, CoseHeaderMap? protectedHeaders = null, CoseHeaderMap? unprotectedHeaders = null, byte[]? associatedData = null);

        internal override byte[] Sign(byte[] content, CoseSigner signer, CoseHeaderMap? protectedHeaders = null, CoseHeaderMap? unprotectedHeaders = null, byte[]? associatedData = null, bool isDetached = false)
        {
            Assert.False(isDetached);

            if (content == null)
            {
                return SignDetachedAsync(null!, signer, protectedHeaders, unprotectedHeaders, associatedData).GetAwaiter().GetResult();
            }

            using Stream stream = GetTestStream(content);
            return SignDetachedAsync(stream, signer, protectedHeaders, unprotectedHeaders, associatedData).GetAwaiter().GetResult();
        }

        [Fact]
        public async Task SignAsyncWithUnseekableStream()
        {
            using Stream stream = GetTestStream(s_sampleContent, StreamKind.Unseekable);
            await Assert.ThrowsAsync<ArgumentException>("detachedContent", () => SignDetachedAsync(stream, GetCoseSigner(DefaultKey, DefaultHash)));
        }

        [Fact]
        public async Task SignAsyncWithUnreadableStream()
        {
            using Stream stream = GetTestStream(s_sampleContent, StreamKind.Unreadable);
            await Assert.ThrowsAsync<ArgumentException>("detachedContent", () => SignDetachedAsync(stream, GetCoseSigner(DefaultKey, DefaultHash)));
        }
    }

    public abstract class CoseMessageTests_SignStream_Sync : CoseMessageTests_SignStream
    {
        internal abstract byte[] SignDetached(Stream detachedContent, CoseSigner signer, CoseHeaderMap? protectedHeaders = null, CoseHeaderMap? unprotectedHeaders = null, byte[]? associatedData = null);

        internal override byte[] Sign(byte[] content, CoseSigner signer, CoseHeaderMap? protectedHeaders = null, CoseHeaderMap? unprotectedHeaders = null, byte[]? associatedData = null, bool isDetached = false)
        {
            Assert.False(isDetached);

            if (content == null)
            {
                return SignDetached(null!, signer, protectedHeaders, unprotectedHeaders, associatedData);
            }

            using Stream stream = GetTestStream(content);
            return SignDetached(stream, signer, protectedHeaders, unprotectedHeaders, associatedData);
        }

        [Fact]
        public void SignWithUnseekableStream()
        {
            using Stream stream = GetTestStream(s_sampleContent, StreamKind.Unseekable);
            Assert.Throws<ArgumentException>("detachedContent", () => SignDetached(stream, GetCoseSigner(DefaultKey, DefaultHash)));
        }

        [Fact]
        public void SignWithUnreadableStream()
        {
            using Stream stream = GetTestStream(s_sampleContent, StreamKind.Unreadable);
            Assert.Throws<ArgumentException>("detachedContent", () => SignDetached(stream, GetCoseSigner(DefaultKey, DefaultHash)));
        }
    }
}
