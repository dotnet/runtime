// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;

using Xunit;

namespace System.Net.Http.Functional.Tests
{
    public class ByteArrayContentTest
    {
        [Fact]
        public void Ctor_NullSourceArray_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() => new ByteArrayContent(null));
        }

        [Fact]
        public void Ctor_NullSourceArrayWithRange_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() => new ByteArrayContent(null, 0, 1));
        }

        [Fact]
        public void Ctor_EmptySourceArrayWithRange_ThrowsArgumentOutOfRangeException()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new ByteArrayContent(new byte[0], 0, 1));
        }

        [Fact]
        public void Ctor_StartIndexTooBig_ThrowsArgumentOufOfRangeException()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new ByteArrayContent(new byte[5], 5, 1));
        }

        [Fact]
        public void Ctor_StartIndexNegative_ThrowsArgumentOutOfRangeException()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new ByteArrayContent(new byte[5], -1, 1));
        }

        [Fact]
        public void Ctor_LengthTooBig_ThrowsArgumentOutOfRangeException()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new ByteArrayContent(new byte[5], 1, 5));
        }

        [Fact]
        public void Ctor_LengthPlusOffsetCauseIntOverflow_ThrowsArgumentOutOfRangeException()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new ByteArrayContent(new byte[5], 1, int.MaxValue));
        }

        [Fact]
        public void Ctor_LengthNegative_ThrowsArgumentOutOfRangeException()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new ByteArrayContent(new byte[5], 0, -1));
        }

        [Fact]
        public void ContentLength_UseWholeSourceArray_LengthMatchesArrayLength()
        {
            var contentData = new byte[10];
            var content = new ByteArrayContent(contentData);

            Assert.Equal(contentData.Length, content.Headers.ContentLength);
        }

        [Fact]
        public void ContentLength_UsePartialSourceArray_LengthMatchesArrayLength()
        {
            var contentData = new byte[10];
            var content = new ByteArrayContent(contentData, 5, 3);

            Assert.Equal(3, content.Headers.ContentLength);
        }

        [Fact]
        public async Task ReadAsStreamAsync_EmptySourceArray_Succeed()
        {
            var content = new ByteArrayContent(new byte[0]);
            Stream stream = await content.ReadAsStreamAsync();
            Assert.Equal(0, stream.Length);
        }

        [Fact]
        public async Task ReadAsStreamAsync_Call_MemoryStreamWrappingByteArrayReturned()
        {
            var contentData = new byte[10];
            var content = new MockByteArrayContent(contentData, 5, 3);

            Stream stream = await content.ReadAsStreamAsync();
            Assert.False(stream.CanWrite);
            Assert.Equal(3, stream.Length);
            Assert.Equal(0, content.CopyToCount);
        }

        [Fact]
        public void CopyToAsync_NullDestination_ThrowsArgumentNullException()
        {
            byte[] contentData = CreateSourceArray();
            var content = new ByteArrayContent(contentData);

            Assert.Throws<ArgumentNullException>(() => { Task t = content.CopyToAsync(null); });
        }

        [Fact]
        public async Task CopyToAsync_UseWholeSourceArray_WholeContentCopied()
        {
            byte[] contentData = CreateSourceArray();
            var content = new ByteArrayContent(contentData);

            var destination = new MemoryStream();
            await content.CopyToAsync(destination);

            Assert.Equal(contentData.Length, destination.Length);
            CheckResult(destination, 0);
        }

        [Fact]
        public async Task CopyToAsync_UsePartialSourceArray_PartialContentCopied()
        {
            byte[] contentData = CreateSourceArray();
            var content = new ByteArrayContent(contentData, 3, 5);

            var destination = new MemoryStream();
            await content.CopyToAsync(destination);

            Assert.Equal(5, destination.Length);
            CheckResult(destination, 3);
        }

        [Fact]
        public async Task CopyToAsync_UseEmptySourceArray_NothingCopied()
        {
            var contentData = new byte[0];
            var content = new ByteArrayContent(contentData, 0, 0);

            var destination = new MemoryStream();
            await content.CopyToAsync(destination);

            Assert.Equal(0, destination.Length);
        }

        #region Helper methods

        private static byte[] CreateSourceArray()
        {
            var contentData = new byte[10];
            for (int i = 0; i < contentData.Length; i++)
            {
                contentData[i] = (byte)(i % 256);
            }
            return contentData;
        }

        private static void CheckResult(Stream destination, byte firstValue)
        {
            destination.Position = 0;
            var destinationData = new byte[destination.Length];
            int read = destination.Read(destinationData, 0, destinationData.Length);

            Assert.Equal(destinationData.Length, read);
            Assert.Equal(firstValue, destinationData[0]);

            for (int i = 1; i < read; i++)
            {
                Assert.True((destinationData[i] == (destinationData[i - 1] + 1)) ||
                    ((destinationData[i] == 0) && (destinationData[i - 1] != 0)));
            }
        }

        private class MockByteArrayContent : ByteArrayContent
        {
            public int CopyToCount { get; private set; }

            public MockByteArrayContent(byte[] content, int offset, int count)
                : base(content, offset, count)
            {
            }

            protected override Task SerializeToStreamAsync(Stream stream, TransportContext context)
            {
                CopyToCount++;
                return base.CopyToAsync(stream, context);
            }
        }

        #endregion
    }
}
