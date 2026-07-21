// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using System.Linq;
using Xunit;

namespace System.IO.Compression
{
    public class ZstandardDictionaryTests
    {
        [Fact]
        public void Create_WithValidBuffer_Succeeds()
        {
            // Arrange
            byte[] dictionaryData = ZstandardTestUtils.CreateSampleDictionary();

            // Act
            using ZstandardDictionary dictionary = ZstandardDictionary.Create(dictionaryData);

            // Assert
            Assert.NotNull(dictionary);
        }

        [Fact]
        public void Create_WithValidBufferAndQuality_Succeeds()
        {
            // Arrange
            byte[] dictionaryData = ZstandardTestUtils.CreateSampleDictionary();
            int quality = 5;

            // Act
            using ZstandardDictionary dictionary = ZstandardDictionary.Create(dictionaryData, quality);

            // Assert
            Assert.NotNull(dictionary);
        }

        [Fact]
        public void Create_WithEmptyBuffer_ThrowsArgumentException()
        {
            // Arrange
            ReadOnlyMemory<byte> emptyBuffer = ReadOnlyMemory<byte>.Empty;

            // Act & Assert
            Assert.Throws<ArgumentException>(() => ZstandardDictionary.Create(emptyBuffer.Span));
        }

        [Fact]
        public void Create_WithEmptyBufferAndQuality_ThrowsArgumentException()
        {
            // Arrange
            ReadOnlyMemory<byte> emptyBuffer = ReadOnlyMemory<byte>.Empty;
            int quality = 5;

            // Act & Assert
            Assert.Throws<ArgumentException>(() => ZstandardDictionary.Create(emptyBuffer.Span, quality));
        }

        [Fact]
        public void Dispose_CanBeCalledMultipleTimes()
        {
            // Arrange
            byte[] dictionaryData = ZstandardTestUtils.CreateSampleDictionary();
            ZstandardDictionary dictionary = ZstandardDictionary.Create(dictionaryData);

            // Act & Assert - Should not throw
            dictionary.Dispose();
            dictionary.Dispose();
        }

        [Fact]
        public void Train_ArgumentChecks()
        {
            // empty samples
            Assert.Throws<ArgumentException>(() => ZstandardDictionary.Train(Span<byte>.Empty, new int[] { }, 50));

            // Sum of lengths does not match the length of the samples buffer
            Assert.Throws<ArgumentException>(() => ZstandardDictionary.Train("AbbCCCddddEEEEE"u8.ToArray(), new int[] { 1, 1, 3, 4, 5 }, 50));
            Assert.Throws<ArgumentException>(() => ZstandardDictionary.Train("AbbCCCddddEEEEE"u8.ToArray(), new int[] { 2, 2, 3, 4, 5 }, 50));

            // too few samples
            Assert.Throws<ArgumentException>(() => ZstandardDictionary.Train("AbbCCCdddd"u8.ToArray(), new int[] { 1, 2, 3, 4 }, 0));

            // Invalid max dictionary size
            Assert.Throws<ArgumentOutOfRangeException>(() => ZstandardDictionary.Train("AbbCCCddddEEEEE"u8.ToArray(), new int[] { 1, 2, 3, 4, 5 }, -50));
            Assert.Throws<ArgumentOutOfRangeException>(() => ZstandardDictionary.Train("AbbCCCddddEEEEE"u8.ToArray(), new int[] { 1, 2, 3, 4, 5 }, 0));

            // negative sample length
            Assert.Throws<ArgumentException>(() => ZstandardDictionary.Train("AbbCCCddddEEEEE"u8.ToArray(), new int[] { 1, -2, 3, 4, 5 }, 50));
        }

        [Fact]
        public void Train_TooFewTooSmallSamples()
        {
            byte[] samples = "AABBAABBAABBAABBAABB"u8.ToArray();
            int[] sampleLengths = [4, 4, 4, 4, 4]; // 5 samples of 4 bytes each
            int maxDictionarySize = 256;

            Assert.Throws<IOException>(() => ZstandardDictionary.Train(samples, sampleLengths, maxDictionarySize));
        }

        [Fact]
        public void Train_ValidSamples_Succeeds()
        {
            int sampleCount = 200;
            int sampleSize = 100;

            byte[] samples = new byte[sampleCount * sampleSize];
            for (int i = 0; i < samples.Length; i++)
            {
                samples[i] = (byte)(i % 256);
            }

            int[] sampleLengths = Enumerable.Repeat(sampleSize, sampleCount).ToArray();
            int maxDictionarySize = 256;

            // Act
            using ZstandardDictionary dictionary = ZstandardDictionary.Train(samples, sampleLengths, maxDictionarySize);

            // Assert
            Assert.NotNull(dictionary);
            Assert.True(dictionary.Data.Length > 0 && dictionary.Data.Length <= maxDictionarySize);
        }
    }
}
