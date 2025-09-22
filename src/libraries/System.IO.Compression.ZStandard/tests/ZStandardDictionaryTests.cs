// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using Xunit;

namespace System.IO.Compression
{
    public class ZstandardDictionaryTests
    {
        [Fact]
        public void Create_WithValidBuffer_Succeeds()
        {
            // Arrange
            byte[] dictionaryData = CreateSampleDictionary();

            // Act
            using ZstandardDictionary dictionary = ZstandardDictionary.Create(dictionaryData);

            // Assert
            Assert.NotNull(dictionary);
        }

        [Fact]
        public void Create_WithValidBufferAndQuality_Succeeds()
        {
            // Arrange
            byte[] dictionaryData = CreateSampleDictionary();
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
            Assert.Throws<ArgumentException>(() => ZstandardDictionary.Create(emptyBuffer));
        }

        [Fact]
        public void Create_WithEmptyBufferAndQuality_ThrowsArgumentException()
        {
            // Arrange
            ReadOnlyMemory<byte> emptyBuffer = ReadOnlyMemory<byte>.Empty;
            int quality = 5;

            // Act & Assert
            Assert.Throws<ArgumentException>(() => ZstandardDictionary.Create(emptyBuffer, quality));
        }

        [Fact]
        public void Dispose_CanBeCalledMultipleTimes()
        {
            // Arrange
            byte[] dictionaryData = CreateSampleDictionary();
            ZstandardDictionary dictionary = ZstandardDictionary.Create(dictionaryData);

            // Act & Assert - Should not throw
            dictionary.Dispose();
            dictionary.Dispose();
        }

        [Fact]
        public void Dispose_WithUsing_ProperlyDisposes()
        {
            // Arrange
            byte[] dictionaryData = CreateSampleDictionary();

            // Act & Assert - Should not throw
            using (ZstandardDictionary dictionary = ZstandardDictionary.Create(dictionaryData))
            {
                Assert.NotNull(dictionary);
            }
            // Dictionary should be disposed here
        }

        [Fact]
        public void Create_WithReadOnlyMemory_Succeeds()
        {
            // Arrange
            byte[] dictionaryData = CreateSampleDictionary();
            ReadOnlyMemory<byte> memory = new ReadOnlyMemory<byte>(dictionaryData);

            // Act
            using ZstandardDictionary dictionary = ZstandardDictionary.Create(memory);

            // Assert
            Assert.NotNull(dictionary);
        }

        [Fact]
        public void Create_WithReadOnlyMemoryAndQuality_Succeeds()
        {
            // Arrange
            byte[] dictionaryData = CreateSampleDictionary();
            ReadOnlyMemory<byte> memory = new ReadOnlyMemory<byte>(dictionaryData);
            int quality = 10;

            // Act
            using ZstandardDictionary dictionary = ZstandardDictionary.Create(memory, quality);

            // Assert
            Assert.NotNull(dictionary);
        }

        private static byte[] CreateSampleDictionary() => ZstandardTestUtils.CreateSampleDictionary();
    }
}
