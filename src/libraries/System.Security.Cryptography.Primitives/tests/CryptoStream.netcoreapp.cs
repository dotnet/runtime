// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.IO;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace System.Security.Cryptography.Encryption.Tests.Asymmetric
{
    public static partial class CryptoStreamTests
    {
        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public static async Task DisposeAsync_DataFlushedCorrectly(bool explicitFlushFinalBeforeDispose)
        {
            const string Text = "hello";

            var stream = new MemoryStream();
            using (CryptoStream encryptStream = new CryptoStream(stream, new IdentityTransform(64, 64, true), CryptoStreamMode.Write))
            {
                Assert.Equal(0, stream.Position);

                byte[] toWrite = Encoding.UTF8.GetBytes(Text);
                encryptStream.Write(toWrite, 0, toWrite.Length);
                Assert.False(encryptStream.HasFlushedFinalBlock);
                Assert.Equal(0, stream.Position);

                if (explicitFlushFinalBeforeDispose)
                {
                    encryptStream.FlushFinalBlock();
                }

                await encryptStream.DisposeAsync();
                Assert.True(encryptStream.HasFlushedFinalBlock);
                Assert.Equal(5, stream.ToArray().Length);

                Assert.True(encryptStream.DisposeAsync().IsCompletedSuccessfully);
            }

            stream = new MemoryStream(stream.ToArray()); // CryptoStream.Dispose disposes the stream
            using (CryptoStream decryptStream = new CryptoStream(stream, new IdentityTransform(64, 64, true), CryptoStreamMode.Read))
            {
                using (StreamReader reader = new StreamReader(decryptStream))
                {
                    Assert.Equal(Text, reader.ReadToEnd());
                }

                Assert.True(decryptStream.DisposeAsync().IsCompletedSuccessfully);
            }
        }

        [Fact]
        public static void DisposeAsync_DerivedStream_InvokesDispose()
        {
            var stream = new MemoryStream();
            using (var encryptStream = new DerivedCryptoStream(stream, new IdentityTransform(64, 64, true), CryptoStreamMode.Write))
            {
                Assert.False(encryptStream.DisposeInvoked);
                Assert.True(encryptStream.DisposeAsync().IsCompletedSuccessfully);
                Assert.True(encryptStream.DisposeInvoked);
            }
        }

        private sealed class DerivedCryptoStream : CryptoStream
        {
            public bool DisposeInvoked;
            public DerivedCryptoStream(Stream stream, ICryptoTransform transform, CryptoStreamMode mode) : base(stream, transform, mode) { }
            protected override void Dispose(bool disposing)
            {
                DisposeInvoked = true;
                base.Dispose(disposing);
            }
        }
    }
}
