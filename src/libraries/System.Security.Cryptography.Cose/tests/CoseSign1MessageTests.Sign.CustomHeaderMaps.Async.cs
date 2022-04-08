// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.IO;
using System.Threading.Tasks;
using Xunit;
using static System.Security.Cryptography.Cose.Tests.CoseTestHelpers;

namespace System.Security.Cryptography.Cose.Tests
{
    public class CoseSign1MessageTests_SignAsync : CoseSign1MessageTests_Sign_CustomHeaderMaps
    {
        internal override bool OnlySupportsDetachedContent => true;
        internal override byte[] Sign(byte[] content, AsymmetricAlgorithm key, HashAlgorithmName hashAlgorithm, CoseHeaderMap? protectedHeaders = null, CoseHeaderMap? unprotectedHeaders = null, bool isDetached = false)
        {
            Assert.False(isDetached);

            if (content == null)
            {
                return CoseSign1Message.SignAsync(null!, key, hashAlgorithm, protectedHeaders, unprotectedHeaders).GetAwaiter().GetResult();
            }

            using Stream stream = GetTestStream(content);
            return CoseSign1Message.SignAsync(stream, key, hashAlgorithm, protectedHeaders, unprotectedHeaders).GetAwaiter().GetResult();
        }

        [Fact]
        public async Task SignAsyncWithUnseekableStream()
        {
            using Stream stream = GetTestStream(s_sampleContent, StreamKind.Unseekable);
            await Assert.ThrowsAsync<ArgumentException>("detachedContent", () => CoseSign1Message.SignAsync(stream, DefaultKey, DefaultHash));
        }

        [Fact]
        public async Task SignAsyncWithUnreadableStream()
        {
            using Stream stream = GetTestStream(s_sampleContent, StreamKind.Unreadable);
            await Assert.ThrowsAsync<ArgumentException>("detachedContent", () => CoseSign1Message.SignAsync(stream, DefaultKey, DefaultHash));
        }
    }
}
