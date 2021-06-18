// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.IO;
using System.IO.Tests;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace System.Security.Cryptography.Encryption.Tests.Asymmetric
{
    public class CryptoStreamTests : WrappingConnectedStreamConformanceTests
    {
        protected override Task<StreamPair> CreateConnectedStreamsAsync()
        {
            (Stream writeable, Stream readable) = ConnectedStreams.CreateBidirectional();
            return CreateWrappedConnectedStreamsAsync((writeable, readable));
        }

        protected override Task<StreamPair> CreateWrappedConnectedStreamsAsync(StreamPair wrapped, bool leaveOpen = false)
        {
            ICryptoTransform transform = new IdentityTransform(1, 1, true);
            (Stream writeable, Stream readable) = GetReadWritePair(wrapped);
            var encryptedWriteable = new CryptoStream(writeable, transform, CryptoStreamMode.Write, leaveOpen);
            var decryptedReadable = new CryptoStream(readable, transform, CryptoStreamMode.Read, leaveOpen);
            return Task.FromResult<StreamPair>((encryptedWriteable, decryptedReadable));
        }

        protected override Type UnsupportedConcurrentExceptionType => null;
        protected override bool BlocksOnZeroByteReads => true;

        [ActiveIssue("https://github.com/dotnet/runtime/issues/45080")]
        [Theory]
        [MemberData(nameof(ReadWrite_Success_Large_MemberData))]
        public override Task ReadWrite_Success_Large(ReadWriteMode mode, int writeSize, bool startWithFlush) => base.ReadWrite_Success_Large(mode, writeSize, startWithFlush);

        [Fact]
        public static void Ctor()
        {
            var transform = new IdentityTransform(1, 1, true);
            AssertExtensions.Throws<ArgumentException>("mode", () => new CryptoStream(new MemoryStream(), transform, (CryptoStreamMode)12345));
            AssertExtensions.Throws<ArgumentException>(null, "stream", () => new CryptoStream(new MemoryStream(new byte[0], writable: false), transform, CryptoStreamMode.Write));
            AssertExtensions.Throws<ArgumentException>(null, "stream", () => new CryptoStream(new CryptoStream(new MemoryStream(new byte[0]), transform, CryptoStreamMode.Write), transform, CryptoStreamMode.Read));
        }

        [Theory]
        [InlineData(64, 64, true)]
        [InlineData(64, 128, true)]
        [InlineData(128, 64, true)]
        [InlineData(1, 1, true)]
        [InlineData(37, 24, true)]
        [InlineData(128, 3, true)]
        [InlineData(8192, 64, true)]
        [InlineData(64, 64, false)]
        public static void Roundtrip(int inputBlockSize, int outputBlockSize, bool canTransformMultipleBlocks)
        {
            ICryptoTransform encryptor = new IdentityTransform(inputBlockSize, outputBlockSize, canTransformMultipleBlocks);
            ICryptoTransform decryptor = new IdentityTransform(inputBlockSize, outputBlockSize, canTransformMultipleBlocks);

            var stream = new MemoryStream();
            using (CryptoStream encryptStream = new CryptoStream(stream, encryptor, CryptoStreamMode.Write))
            {
                Assert.True(encryptStream.CanWrite);
                Assert.False(encryptStream.CanRead);
                Assert.False(encryptStream.CanSeek);
                Assert.False(encryptStream.HasFlushedFinalBlock);

                byte[] toWrite = Encoding.UTF8.GetBytes(LoremText);

                // Write it all at once
                encryptStream.Write(toWrite, 0, toWrite.Length);
                Assert.False(encryptStream.HasFlushedFinalBlock);

                // Write in chunks
                encryptStream.Write(toWrite, 0, toWrite.Length / 2);
                encryptStream.Write(toWrite, toWrite.Length / 2, toWrite.Length - (toWrite.Length / 2));
                Assert.False(encryptStream.HasFlushedFinalBlock);

                // Write one byte at a time
                for (int i = 0; i < toWrite.Length; i++)
                {
                    encryptStream.WriteByte(toWrite[i]);
                }
                Assert.False(encryptStream.HasFlushedFinalBlock);

                // Write async
                encryptStream.WriteAsync(toWrite, 0, toWrite.Length).GetAwaiter().GetResult();
                Assert.False(encryptStream.HasFlushedFinalBlock);

                // Flush (nops)
                encryptStream.Flush();
                encryptStream.FlushAsync().GetAwaiter().GetResult();

                encryptStream.FlushFinalBlock();
                Assert.Throws<NotSupportedException>(() => encryptStream.FlushFinalBlock());
                Assert.True(encryptStream.HasFlushedFinalBlock);

                Assert.True(stream.Length > 0);
            }

            // Read/decrypt using Read
            stream = new MemoryStream(stream.ToArray()); // CryptoStream.Dispose disposes the stream
            using (CryptoStream decryptStream = new CryptoStream(stream, decryptor, CryptoStreamMode.Read))
            {
                Assert.False(decryptStream.CanWrite);
                Assert.True(decryptStream.CanRead);
                Assert.False(decryptStream.CanSeek);
                Assert.False(decryptStream.HasFlushedFinalBlock);

                using (StreamReader reader = new StreamReader(decryptStream))
                {
                    Assert.Equal(
                        LoremText + LoremText + LoremText + LoremText,
                        reader.ReadToEnd());
                }
            }

            // Read/decrypt using ReadToEnd
            stream = new MemoryStream(stream.ToArray()); // CryptoStream.Dispose disposes the stream
            using (CryptoStream decryptStream = new CryptoStream(stream, decryptor, CryptoStreamMode.Read))
            using (StreamReader reader = new StreamReader(decryptStream))
            {
                Assert.Equal(
                    LoremText + LoremText + LoremText + LoremText,
                    reader.ReadToEndAsync().GetAwaiter().GetResult());
            }

            // Read/decrypt using a small buffer to force multiple calls to Read
            stream = new MemoryStream(stream.ToArray()); // CryptoStream.Dispose disposes the stream
            using (CryptoStream decryptStream = new CryptoStream(stream, decryptor, CryptoStreamMode.Read))
            using (StreamReader reader = new StreamReader(decryptStream, Encoding.UTF8, true, bufferSize: 10))
            {
                Assert.Equal(
                    LoremText + LoremText + LoremText + LoremText,
                    reader.ReadToEndAsync().GetAwaiter().GetResult());
            }

            // Read/decrypt one byte at a time with ReadByte
            stream = new MemoryStream(stream.ToArray()); // CryptoStream.Dispose disposes the stream
            using (CryptoStream decryptStream = new CryptoStream(stream, decryptor, CryptoStreamMode.Read))
            {
                string expectedStr = LoremText + LoremText + LoremText + LoremText;
                foreach (char c in expectedStr)
                {
                    Assert.Equal(c, decryptStream.ReadByte()); // relies on LoremText being ASCII
                }
                Assert.Equal(-1, decryptStream.ReadByte());
            }
        }

        [Fact]
        public static void Clear()
        {
            ICryptoTransform encryptor = new IdentityTransform(1, 1, true);
            using (MemoryStream output = new MemoryStream())
            using (CryptoStream encryptStream = new CryptoStream(output, encryptor, CryptoStreamMode.Write))
            {
                encryptStream.Clear();
                Assert.Throws<NotSupportedException>(() => encryptStream.Write(new byte[] { 1, 2, 3, 4, 5 }, 0, 5));
            }
        }

        [Fact]
        public static async Task FlushFinalBlockAsync()
        {
            ICryptoTransform encryptor = new IdentityTransform(1, 1, true);
            using (MemoryStream output = new MemoryStream())
            using (CryptoStream encryptStream = new CryptoStream(output, encryptor, CryptoStreamMode.Write))
            {
                await encryptStream.WriteAsync(new byte[] { 1, 2, 3, 4, 5 }, 0, 5);
                await encryptStream.FlushFinalBlockAsync();
                Assert.True(encryptStream.HasFlushedFinalBlock);
                Assert.Equal(5, output.ToArray().Length);
            }
        }

        [Fact]
        public static async Task FlushFinalBlockAsync_Canceled()
        {
            ICryptoTransform encryptor = new IdentityTransform(1, 1, true);
            using (MemoryStream output = new MemoryStream())
            using (CryptoStream encryptStream = new CryptoStream(output, encryptor, CryptoStreamMode.Write))
            {
                await encryptStream.WriteAsync(new byte[] { 1, 2, 3, 4, 5 }, 0, 5);
                ValueTask waitable = encryptStream.FlushFinalBlockAsync(new Threading.CancellationToken(canceled: true));
                Assert.True(waitable.IsCanceled);
                Assert.False(encryptStream.HasFlushedFinalBlock);
            }
        }

        [Fact]
        public static void FlushCalledOnFlushAsync_DerivedClass()
        {
            ICryptoTransform encryptor = new IdentityTransform(1, 1, true);
            using (MemoryStream output = new MemoryStream())
            using (MinimalCryptoStream encryptStream = new MinimalCryptoStream(output, encryptor, CryptoStreamMode.Write))
            {
                encryptStream.WriteAsync(new byte[] { 1, 2, 3, 4, 5 }, 0, 5);
                Task waitable = encryptStream.FlushAsync(new Threading.CancellationToken(false));
                Assert.False(waitable.IsCanceled);
                waitable.Wait();
                Assert.True(encryptStream.FlushCalled);
            }
        }

        [Fact]
        public static void MultipleDispose()
        {
            ICryptoTransform encryptor = new IdentityTransform(1, 1, true);

            using (MemoryStream output = new MemoryStream())
            {
                using (CryptoStream encryptStream = new CryptoStream(output, encryptor, CryptoStreamMode.Write))
                {
                    encryptStream.Dispose();
                }

                Assert.False(output.CanRead);
            }

            using (MemoryStream output = new MemoryStream())
            {
                using (CryptoStream encryptStream = new CryptoStream(output, encryptor, CryptoStreamMode.Write, leaveOpen: false))
                {
                    encryptStream.Dispose();
                }

                Assert.False(output.CanRead);
            }

            using (MemoryStream output = new MemoryStream())
            {
                using (CryptoStream encryptStream = new CryptoStream(output, encryptor, CryptoStreamMode.Write, leaveOpen: true))
                {
                    encryptStream.Dispose();
                }

                Assert.True(output.CanRead);
            }
        }

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

        [Fact]
        public static void PaddedAes_PartialRead_Success()
        {
            using (Aes aes = Aes.Create())
            {
                aes.Mode = CipherMode.CBC;
                aes.Key = aes.IV = new byte[] { 0x0, 0x1, 0x2, 0x3, 0x4, 0x5, 0x6, 0x7, 0x8, 0x9, 0xA, 0xB, 0xC, 0xD, 0xE, 0xF, };

                var memoryStream = new MemoryStream();
                using (var cryptoStream = new CryptoStream(memoryStream, aes.CreateEncryptor(), CryptoStreamMode.Write, leaveOpen: true))
                {
                    cryptoStream.Write(Encoding.ASCII.GetBytes("Sample string that's bigger than cryptoAlg.BlockSize"));
                    cryptoStream.FlushFinalBlock();
                }

                memoryStream.Position = 0;
                using (var cryptoStream = new CryptoStream(memoryStream, aes.CreateDecryptor(), CryptoStreamMode.Read))
                {
                    cryptoStream.ReadByte(); // Partially read the CryptoStream before disposing it.
                }

                // No exception should be thrown.
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

        private const string LoremText =
            @"Lorem ipsum dolor sit amet, consectetuer adipiscing elit. Maecenas porttitor congue massa.
              Fusce posuere, magna sed pulvinar ultricies, purus lectus malesuada libero, sit amet commodo magna eros quis urna.
              Nunc viverra imperdiet enim. Fusce est. Vivamus a tellus.
              Pellentesque habitant morbi tristique senectus et netus et malesuada fames ac turpis egestas.
              Proin pharetra nonummy pede. Mauris et orci.
              Aenean nec lorem. In porttitor. Donec laoreet nonummy augue.
              Suspendisse dui purus, scelerisque at, vulputate vitae, pretium mattis, nunc. Mauris eget neque at sem venenatis eleifend.
              Ut nonummy.";

        private sealed class IdentityTransform : ICryptoTransform
        {
            private readonly int _inputBlockSize, _outputBlockSize;
            private readonly bool _canTransformMultipleBlocks;
            private readonly object _lock = new object();

            private long _writePos, _readPos;
            private MemoryStream _stream;

            internal IdentityTransform(int inputBlockSize, int outputBlockSize, bool canTransformMultipleBlocks)
            {
                _inputBlockSize = inputBlockSize;
                _outputBlockSize = outputBlockSize;
                _canTransformMultipleBlocks = canTransformMultipleBlocks;
                _stream = new MemoryStream();
            }

            public bool CanReuseTransform { get { return true; } }

            public bool CanTransformMultipleBlocks { get { return _canTransformMultipleBlocks; } }

            public int InputBlockSize { get { return _inputBlockSize; } }

            public int OutputBlockSize { get { return _outputBlockSize; } }

            public void Dispose() { }

            public int TransformBlock(byte[] inputBuffer, int inputOffset, int inputCount, byte[] outputBuffer, int outputOffset)
            {
                lock (_lock)
                {
                    _stream.Position = _writePos;
                    _stream.Write(inputBuffer, inputOffset, inputCount);
                    _writePos = _stream.Position;

                    _stream.Position = _readPos;
                    int copied = _stream.Read(outputBuffer, outputOffset, outputBuffer.Length - outputOffset);
                    _readPos = _stream.Position;
                    return copied;
                }
            }

            public byte[] TransformFinalBlock(byte[] inputBuffer, int inputOffset, int inputCount)
            {
                lock (_lock)
                {
                    _stream.Position = _writePos;
                    _stream.Write(inputBuffer, inputOffset, inputCount);

                    _stream.Position = _readPos;
                    long len = _stream.Length - _stream.Position;
                    byte[] outputBuffer = new byte[len];
                    _stream.Read(outputBuffer, 0, outputBuffer.Length);

                    _stream = new MemoryStream();
                    _writePos = 0;
                    _readPos = 0;
                    return outputBuffer;
                }
            }
        }

        public class MinimalCryptoStream : CryptoStream
        {
            public bool FlushCalled;

            public MinimalCryptoStream(Stream stream, ICryptoTransform transform, CryptoStreamMode mode) : base(stream, transform, mode) { }

            public override void Flush()
            {
                FlushCalled = true;
                base.Flush();
            }
        }

    }
}
