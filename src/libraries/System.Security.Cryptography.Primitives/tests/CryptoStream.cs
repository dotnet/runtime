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
        [Fact]
        public static void Ctor()
        {
            var transform = new IdentityTransform(1, 1, true);
            AssertExtensions.Throws<ArgumentException>(null, () => new CryptoStream(new MemoryStream(), transform, (CryptoStreamMode)12345));
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
                Assert.Throws<NotSupportedException>(() => encryptStream.SetLength(1));
                Assert.Throws<NotSupportedException>(() => encryptStream.Length);
                Assert.Throws<NotSupportedException>(() => encryptStream.Position);
                Assert.Throws<NotSupportedException>(() => encryptStream.Position = 0);
                Assert.Throws<NotSupportedException>(() => encryptStream.Seek(0, SeekOrigin.Begin));
                Assert.Throws<NotSupportedException>(() => encryptStream.Read(new byte[0], 0, 0));
                Assert.Throws<NullReferenceException>(() => encryptStream.Write(null, 0, 0)); // No arg validation on buffer?
                Assert.Throws<ArgumentOutOfRangeException>(() => encryptStream.Write(new byte[0], -1, 0));
                Assert.Throws<ArgumentOutOfRangeException>(() => encryptStream.Write(new byte[0], 0, -1));
                Assert.Throws<ArgumentOutOfRangeException>(() => encryptStream.Write(new byte[0], 0, -1));
                AssertExtensions.Throws<ArgumentException>(null, () => encryptStream.Write(new byte[3], 1, 4));

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
                Assert.Throws<NotSupportedException>(() => decryptStream.SetLength(1));
                Assert.Throws<NotSupportedException>(() => decryptStream.Length);
                Assert.Throws<NotSupportedException>(() => decryptStream.Position);
                Assert.Throws<NotSupportedException>(() => decryptStream.Position = 0);
                Assert.Throws<NotSupportedException>(() => decryptStream.Seek(0, SeekOrigin.Begin));
                Assert.Throws<NotSupportedException>(() => decryptStream.Write(new byte[0], 0, 0));
                Assert.Throws<NullReferenceException>(() => decryptStream.Read(null, 0, 0)); // No arg validation on buffer?
                Assert.Throws<ArgumentOutOfRangeException>(() => decryptStream.Read(new byte[0], -1, 0));
                Assert.Throws<ArgumentOutOfRangeException>(() => decryptStream.Read(new byte[0], 0, -1));
                Assert.Throws<ArgumentOutOfRangeException>(() => decryptStream.Read(new byte[0], 0, -1));
                AssertExtensions.Throws<ArgumentException>(null, () => decryptStream.Read(new byte[3], 1, 4));

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
        public static void NestedCryptoStreams()
        {
            ICryptoTransform encryptor = new IdentityTransform(1, 1, true);
            using (MemoryStream output = new MemoryStream())
            using (CryptoStream encryptStream1 = new CryptoStream(output, encryptor, CryptoStreamMode.Write))
            using (CryptoStream encryptStream2 = new CryptoStream(encryptStream1, encryptor, CryptoStreamMode.Write))
            {
                encryptStream2.Write(new byte[] { 1, 2, 3, 4, 5 }, 0, 5);
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
        public static void FlushAsync()
        {
            ICryptoTransform encryptor = new IdentityTransform(1, 1, true);
            using (MemoryStream output = new MemoryStream())
            using (CryptoStream encryptStream = new CryptoStream(output, encryptor, CryptoStreamMode.Write))
            {
                encryptStream.WriteAsync(new byte[] { 1, 2, 3, 4, 5 }, 0, 5);
                Task waitable = encryptStream.FlushAsync(new Threading.CancellationToken(false));
                Assert.False(waitable.IsCanceled);

                encryptStream.WriteAsync(new byte[] { 1, 2, 3, 4, 5 }, 0, 5);
                waitable = encryptStream.FlushAsync(new Threading.CancellationToken(true));
                Assert.True(waitable.IsCanceled);
            }
        }

        [Fact]
        public static void FlushCalledOnFlushAsync_DeriveClass()
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

                Assert.Equal(false, output.CanRead);
            }

#if netcoreapp
            using (MemoryStream output = new MemoryStream())
            {
                using (CryptoStream encryptStream = new CryptoStream(output, encryptor, CryptoStreamMode.Write, leaveOpen: false))
                {
                    encryptStream.Dispose();
                }

                Assert.Equal(false, output.CanRead);
            }

            using (MemoryStream output = new MemoryStream())
            {
                using (CryptoStream encryptStream = new CryptoStream(output, encryptor, CryptoStreamMode.Write, leaveOpen: true))
                {
                    encryptStream.Dispose();
                }

                Assert.Equal(true, output.CanRead);
            }
#endif
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
