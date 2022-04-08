// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.IO;
using System.Threading.Tasks;
using Xunit;
using static System.Security.Cryptography.Cose.Tests.CoseTestHelpers;

namespace System.Security.Cryptography.Cose.Tests
{
    public class CoseSign1MessageTests_VerifyAsync : CoseSign1MessageTests_Verify
    {
        internal override bool UseDetachedContent => true;
        internal override bool Verify(CoseSign1Message msg, AsymmetricAlgorithm key, byte[] content)
        {
            if (content == null)
            {
                return msg.VerifyAsync(key, null!).GetAwaiter().GetResult();
            }

            using Stream stream = GetTestStream(content);
            return msg.VerifyAsync(key, stream).GetAwaiter().GetResult();
        }

        [Fact]
        public async Task VerifyAsyncWithUnseekableStream()
        {
            using Stream stream = GetTestStream(s_sampleContent);
            byte[] encodedMsg = await CoseSign1Message.SignAsync(stream, DefaultKey, DefaultHash);

            CoseSign1Message msg = CoseMessage.DecodeSign1(encodedMsg);
            using Stream unseekableStream = GetTestStream(s_sampleContent, StreamKind.Unseekable);
            await Assert.ThrowsAsync<ArgumentException>("detachedContent", () => msg.VerifyAsync(DefaultKey, unseekableStream));
        }

        [Fact]
        public async Task VerifyAsyncWithUnreadableStream()
        {
            using Stream stream = GetTestStream(s_sampleContent);
            byte[] encodedMsg = await CoseSign1Message.SignAsync(stream, DefaultKey, DefaultHash);

            CoseSign1Message msg = CoseMessage.DecodeSign1(encodedMsg);
            using Stream unseekableStream = GetTestStream(s_sampleContent, StreamKind.Unreadable);
            await Assert.ThrowsAsync<ArgumentException>("detachedContent", () => msg.VerifyAsync(DefaultKey, unseekableStream));
        }
    }
}
