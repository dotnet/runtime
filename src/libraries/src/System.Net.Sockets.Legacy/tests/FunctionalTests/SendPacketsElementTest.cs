// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace System.Net.Sockets.Tests
{
    public class SendPacketsElementTest
    {
        #region Buffer

        [Fact]
        public void NullBufferCtor_Throws()
        {
            Assert.Throws<ArgumentNullException>(() =>
            {
                SendPacketsElement element = new SendPacketsElement((byte[])null);
            });
        }

        [Fact]
        public void NullBufferCtorWithOffset_Throws()
        {
            Assert.Throws<ArgumentNullException>(() =>
            {
                SendPacketsElement element = new SendPacketsElement((byte[])null, 0, 0);
            });
        }

        [Fact]
        public void NullBufferCtorWithEndOfPacket_Throws()
        {
            Assert.Throws<ArgumentNullException>(() =>
            {
                // Elements with null Buffers are ignored on Send
                SendPacketsElement element = new SendPacketsElement((byte[])null, 0, 0, true);
            });
        }

        [Fact]
        public void EmptyBufferCtor_Success()
        {
            // Elements with empty Buffers are ignored on Send
            SendPacketsElement element = new SendPacketsElement(new byte[0]);
            Assert.NotNull(element.Buffer);
            Assert.Equal(0, element.Buffer.Length);
            Assert.Equal(0, element.Offset);
            Assert.Equal(0, element.Count);
            Assert.False(element.EndOfPacket);
            Assert.Null(element.FilePath);
        }

        [Fact]
        public void BufferCtorNormal_Success()
        {
            SendPacketsElement element = new SendPacketsElement(new byte[10]);
            Assert.NotNull(element.Buffer);
            Assert.Equal(10, element.Buffer.Length);
            Assert.Equal(0, element.Offset);
            Assert.Equal(10, element.Count);
            Assert.False(element.EndOfPacket);
            Assert.Null(element.FilePath);
        }

        [Fact]
        public void BufferCtorNegOffset_ArgumentOutOfRangeException()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() =>
            {
                SendPacketsElement element = new SendPacketsElement(new byte[10], -1, 11);
            });
        }

        [Fact]
        public void BufferCtorNegCount_ArgumentOutOfRangeException()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() =>
            {
                SendPacketsElement element = new SendPacketsElement(new byte[10], 0, -1);
            });
        }

        [Fact]
        public void BufferCtorLargeOffset_ArgumentOutOfRangeException()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() =>
            {
                SendPacketsElement element = new SendPacketsElement(new byte[10], 11, 1);
            });
        }

        [Fact]
        public void BufferCtorLargeCount_ArgumentOutOfRangeException()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() =>
            {
                SendPacketsElement element = new SendPacketsElement(new byte[10], 5, 10);
            });
        }

        [Fact]
        public void BufferCtorEndOfBufferTrue_Success()
        {
            SendPacketsElement element = new SendPacketsElement(new byte[10], 2, 8, true);
            Assert.NotNull(element.Buffer);
            Assert.Equal(10, element.Buffer.Length);
            Assert.Equal(2, element.Offset);
            Assert.Equal(8, element.Count);
            Assert.True(element.EndOfPacket);
            Assert.Null(element.FilePath);
        }

        [Fact]
        public void BufferCtorEndOfBufferFalse_Success()
        {
            SendPacketsElement element = new SendPacketsElement(new byte[10], 6, 4, false);
            Assert.NotNull(element.Buffer);
            Assert.Equal(10, element.Buffer.Length);
            Assert.Equal(6, element.Offset);
            Assert.Equal(4, element.Count);
            Assert.False(element.EndOfPacket);
            Assert.Null(element.FilePath);
        }

        [Fact]
        public void BufferCtorZeroCount_Success()
        {
            // Elements with empty Buffers are ignored on Send
            SendPacketsElement element = new SendPacketsElement(new byte[0], 0, 0);
            Assert.NotNull(element.Buffer);
            Assert.Equal(0, element.Buffer.Length);
            Assert.Equal(0, element.Offset);
            Assert.Equal(0, element.Count);
            Assert.False(element.EndOfPacket);
            Assert.Null(element.FilePath);
        }

        #endregion Buffer

        #region File

        [Fact]
        public void FileCtorNull_Throws()
        {
            Assert.Throws<ArgumentNullException>(() =>
            {
                SendPacketsElement element = new SendPacketsElement((string)null);
            });
        }

        [Fact]
        public void FileCtorEmpty_Success()
        {
            // An exception will happen on send if this file doesn't exist
            SendPacketsElement element = new SendPacketsElement(String.Empty);
            Assert.Null(element.Buffer);
            Assert.Equal(0, element.Offset);
            Assert.Equal(0, element.Count);
            Assert.False(element.EndOfPacket);
            Assert.Equal(String.Empty, element.FilePath);
        }

        [Fact]
        public void FileCtorWhiteSpace_Success()
        {
            // An exception will happen on send if this file doesn't exist
            SendPacketsElement element = new SendPacketsElement("   \t ");
            Assert.Null(element.Buffer);
            Assert.Equal(0, element.Offset);
            Assert.Equal(0, element.Count);
            Assert.False(element.EndOfPacket);
            Assert.Equal("   \t ", element.FilePath);
        }

        [Fact]
        public void FileCtorNormal_Success()
        {
            // An exception will happen on send if this file doesn't exist
            SendPacketsElement element = new SendPacketsElement("SomeFileName"); // Send whole file
            Assert.Null(element.Buffer);
            Assert.Equal(0, element.Offset);
            Assert.Equal(0, element.Count);
            Assert.False(element.EndOfPacket);
            Assert.Equal("SomeFileName", element.FilePath);
        }

        [Fact]
        public void FileCtorZeroCountLength_Success()
        {
            // An exception will happen on send if this file doesn't exist
            SendPacketsElement element = new SendPacketsElement("SomeFileName", 0, 0); // Send whole file
            Assert.Null(element.Buffer);
            Assert.Equal(0, element.Offset);
            Assert.Equal(0, element.Count);
            Assert.False(element.EndOfPacket);
            Assert.Equal("SomeFileName", element.FilePath);
        }

        [Fact]
        public void FileCtorNegOffset_ArgumentOutOfRangeException()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() =>
            {
                SendPacketsElement element = new SendPacketsElement("SomeFileName", -1, 11);
            });
        }

        [Fact]
        public void FileCtorNegCount_ArgumentOutOfRangeException()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() =>
            {
                SendPacketsElement element = new SendPacketsElement("SomeFileName", 0, -1);
            });
        }

        // File lengths are validated on send

        [Fact]
        public void FileCtorEndOfBufferTrue_Success()
        {
            SendPacketsElement element = new SendPacketsElement("SomeFileName", 2, 8, true);
            Assert.Null(element.Buffer);
            Assert.Equal(2, element.Offset);
            Assert.Equal(8, element.Count);
            Assert.True(element.EndOfPacket);
            Assert.Equal("SomeFileName", element.FilePath);
        }

        [Fact]
        public void FileCtorEndOfBufferFalse_Success()
        {
            SendPacketsElement element = new SendPacketsElement("SomeFileName", 6, 4, false);
            Assert.Null(element.Buffer);
            Assert.Equal(6, element.Offset);
            Assert.Equal(4, element.Count);
            Assert.False(element.EndOfPacket);
            Assert.Equal("SomeFileName", element.FilePath);
        }

        #endregion File
    }
}
