// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using System.Collections.Generic;
using System.Text;
using Xunit;

namespace System.IO.Compression
{
    public class BrotliDictionaryTests
    {
        private const string RepeatedText = "This is a repeated text that should be in the dictionary. ";

        [Fact]
        public void CreateFromBuffer_EmptyBuffer_ThrowsArgumentException()
        {
            Assert.Throws<ArgumentException>(() => BrotliDictionary.CreateFromBuffer(Array.Empty<byte>()));
        }

        [Theory]
        [InlineData(-1)]
        [InlineData(12)]
        public void CreateFromBuffer_InvalidQuality_ThrowsArgumentOutOfRangeException(int quality)
        {
            // Arrange
            byte[] dictionaryData = Encoding.UTF8.GetBytes(RepeatedText);

            // Act & Assert
            Assert.Throws<ArgumentOutOfRangeException>(() => BrotliDictionary.CreateFromBuffer(dictionaryData, quality));
        }

        [Fact]
        public void BrotliEncoder_WithDictionary_CompressesData()
        {
            // Arrange
            byte[] dictionaryData = Encoding.UTF8.GetBytes(RepeatedText);
            byte[] dataToCompress = Encoding.UTF8.GetBytes(RepeatedText + RepeatedText + "Additional text");
            byte[] compressedWithDictionary = new byte[BrotliEncoder.GetMaxCompressedLength(dataToCompress.Length)];
            byte[] compressedWithoutDictionary = new byte[BrotliEncoder.GetMaxCompressedLength(dataToCompress.Length)];

            // Act
            using (BrotliDictionary dictionary = BrotliDictionary.CreateFromBuffer(dictionaryData))
            {
                BrotliEncoder encoder = new BrotliEncoder(11, 22);
                encoder.AttachDictionary(dictionary);
                OperationStatus statusWithDict = encoder.Compress(dataToCompress, compressedWithDictionary, out int bytesConsumedWithDict, out int bytesWrittenWithDict, true);
                encoder.Dispose();

                BrotliEncoder encoderWithoutDict = new BrotliEncoder(11, 22);
                OperationStatus statusWithoutDict = encoderWithoutDict.Compress(dataToCompress, compressedWithoutDictionary, out int bytesConsumedWithoutDict, out int bytesWrittenWithoutDict, true);
                encoderWithoutDict.Dispose();

                // Resize arrays to actual compressed size
                Array.Resize(ref compressedWithDictionary, bytesWrittenWithDict);
                Array.Resize(ref compressedWithoutDictionary, bytesWrittenWithoutDict);

                // Assert
                Assert.Equal(OperationStatus.Done, statusWithDict);
                Assert.Equal(bytesConsumedWithDict, dataToCompress.Length);
                Assert.Equal(OperationStatus.Done, statusWithoutDict);
                Assert.Equal(bytesConsumedWithoutDict, dataToCompress.Length);

                // With a proper dictionary containing repeated text, the compression with dictionary should be more efficient
                Assert.True(bytesWrittenWithDict <= bytesWrittenWithoutDict,
                    $"Dictionary compression size ({bytesWrittenWithDict}) was not smaller than regular compression ({bytesWrittenWithoutDict})");
            }
        }

        [Fact]
        public void BrotliDecoder_WithDictionary_DecompressesData()
        {
            // Arrange
            byte[] dictionaryData = Encoding.UTF8.GetBytes(RepeatedText);
            byte[] originalData = Encoding.UTF8.GetBytes(RepeatedText + "Additional text that references the dictionary content");
            byte[] compressedData = new byte[BrotliEncoder.GetMaxCompressedLength(originalData.Length)];
            byte[] decompressedData = new byte[originalData.Length];

            // Act - Compress with dictionary
            using (BrotliDictionary dictionary = BrotliDictionary.CreateFromBuffer(dictionaryData))
            {
                // Compress with dictionary
                BrotliEncoder encoder = new BrotliEncoder(11, 22);
                encoder.AttachDictionary(dictionary);
                OperationStatus compressStatus = encoder.Compress(originalData, compressedData, out int bytesConsumed, out int bytesWritten, true);
                encoder.Dispose();

                Assert.Equal(OperationStatus.Done, compressStatus);
                Assert.Equal(bytesConsumed, originalData.Length);

                // Resize array to actual compressed size
                Array.Resize(ref compressedData, bytesWritten);

                // Decompress with dictionary
                BrotliDecoder decoder = new BrotliDecoder();
                decoder.AttachDictionary(dictionary);
                OperationStatus decompressStatus = decoder.Decompress(compressedData, decompressedData, out int bytesConsumedDecomp, out int bytesWrittenDecomp);
                decoder.Dispose();

                // Assert
                Assert.Equal(OperationStatus.Done, decompressStatus);
                Assert.Equal(bytesConsumedDecomp, compressedData.Length);
                Assert.Equal(bytesWrittenDecomp, originalData.Length);
                Assert.Equal(originalData, decompressedData);
            }
        }

        [Fact]
        public void BrotliStream_WithDictionary_RoundTrip()
        {
            // Arrange
            byte[] dictionaryData = Encoding.UTF8.GetBytes(RepeatedText);
            byte[] originalData = Encoding.UTF8.GetBytes(RepeatedText + RepeatedText + "More text to compress using the dictionary");
            byte[] compressedData;
            byte[] decompressedData = new byte[originalData.Length];

            using BrotliDictionary dictionary = BrotliDictionary.CreateFromBuffer(dictionaryData);

            // Act - Compress
            using (MemoryStream compressedStream = new MemoryStream())
            {
                using (BrotliStream compressionStream = new BrotliStream(compressedStream, CompressionMode.Compress))
                {
                    compressionStream.AttachDictionary(dictionary);
                    compressionStream.Write(originalData, 0, originalData.Length);
                }
                compressedData = compressedStream.ToArray();
            }

            // Act - Decompress
            using (MemoryStream compressedStream = new MemoryStream(compressedData))
            using (BrotliStream decompressionStream = new BrotliStream(compressedStream, CompressionMode.Decompress))
            {
                decompressionStream.AttachDictionary(dictionary);
                int bytesRead = decompressionStream.Read(decompressedData, 0, decompressedData.Length);

                // Assert
                Assert.Equal(originalData.Length, bytesRead);
                Assert.Equal(originalData, decompressedData);
            }
        }

        [Fact]
        public void BrotliDecoder_MismatchedDictionary_ThrowsException()
        {
            // Arrange
            byte[] dictionaryData = Encoding.UTF8.GetBytes(RepeatedText);
            byte[] originalData = Encoding.UTF8.GetBytes(RepeatedText + "Additional text that references the dictionary content");
            byte[] compressedData = new byte[BrotliEncoder.GetMaxCompressedLength(originalData.Length)];

            using (BrotliDictionary dictionary = BrotliDictionary.CreateFromBuffer(dictionaryData))
            {
                // Compress with dictionary
                BrotliEncoder encoder = new BrotliEncoder(11, 22);
                encoder.AttachDictionary(dictionary);
                OperationStatus compressStatus = encoder.Compress(originalData, compressedData, out int bytesConsumed, out int bytesWritten, true);
                encoder.Dispose();

                Assert.Equal(OperationStatus.Done, compressStatus);

                // Resize array to actual compressed size
                Array.Resize(ref compressedData, bytesWritten);
            }

            // Attempt to decompress with a different dictionary
            using (BrotliDictionary differentDictionary = BrotliDictionary.CreateFromBuffer(Encoding.UTF8.GetBytes("Different dictionary content")))
            {
                BrotliDecoder decoder = new BrotliDecoder();
                decoder.AttachDictionary(differentDictionary);
                OperationStatus decompressStatus = decoder.Decompress(compressedData, new byte[originalData.Length], out _, out _);
                Assert.Equal(OperationStatus.InvalidData, decompressStatus);
            }

            // Attempt to decompress without a dictionary
            {
                BrotliDecoder decoder = new BrotliDecoder();
                OperationStatus decompressStatus = decoder.Decompress(compressedData, new byte[originalData.Length], out _, out _);
                Assert.Equal(OperationStatus.InvalidData, decompressStatus);
            }
        }
    }
}
