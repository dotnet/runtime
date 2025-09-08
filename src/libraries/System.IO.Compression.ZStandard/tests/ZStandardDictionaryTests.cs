// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using Xunit;

namespace System.IO.Compression
{
    public class ZStandardDictionaryTests
    {
        [Fact]
        public void Create_WithValidBuffer_Succeeds()
        {
            // Arrange
            byte[] dictionaryData = CreateSampleDictionary();

            // Act
            using ZStandardDictionary dictionary = ZStandardDictionary.Create(dictionaryData);

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
            using ZStandardDictionary dictionary = ZStandardDictionary.Create(dictionaryData, quality);

            // Assert
            Assert.NotNull(dictionary);
        }

        [Fact]
        public void Create_WithEmptyBuffer_ThrowsArgumentException()
        {
            // Arrange
            ReadOnlyMemory<byte> emptyBuffer = ReadOnlyMemory<byte>.Empty;

            // Act & Assert
            Assert.Throws<ArgumentException>(() => ZStandardDictionary.Create(emptyBuffer));
        }

        [Fact]
        public void Create_WithEmptyBufferAndQuality_ThrowsArgumentException()
        {
            // Arrange
            ReadOnlyMemory<byte> emptyBuffer = ReadOnlyMemory<byte>.Empty;
            int quality = 5;

            // Act & Assert
            Assert.Throws<ArgumentException>(() => ZStandardDictionary.Create(emptyBuffer, quality));
        }

        [Fact]
        public void Dispose_CanBeCalledMultipleTimes()
        {
            // Arrange
            byte[] dictionaryData = CreateSampleDictionary();
            ZStandardDictionary dictionary = ZStandardDictionary.Create(dictionaryData);

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
            using (ZStandardDictionary dictionary = ZStandardDictionary.Create(dictionaryData))
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
            using ZStandardDictionary dictionary = ZStandardDictionary.Create(memory);

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
            using ZStandardDictionary dictionary = ZStandardDictionary.Create(memory, quality);

            // Assert
            Assert.NotNull(dictionary);
        }

        private static byte[] CreateSampleDictionary()
        {
            // Create a simple dictionary with some sample data
            // In a real scenario, this would be a pre-trained ZStandard dictionary
            // For testing purposes, we'll use a simple byte array
            return "oaifa;w efiewajf  eajf ;oiewajfajf ;oiewjf "u8.ToArray();
            // {
            //     0x37, 0xA4, 0x30, 0xEC, // ZStandard magic number for dictionary
            //     0x01, 0x00, 0x00, 0x00, // Version and flags
            //     0x48, 0x65, 0x6C, 0x6C, 0x6F, 0x20, 0x57, 0x6F, 0x72, 0x6C, 0x64, // "Hello World"
            //     0x54, 0x68, 0x69, 0x73, 0x20, 0x69, 0x73, 0x20, 0x61, 0x20, 0x74, 0x65, 0x73, 0x74, // "This is a test"
            //     0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08, // Some additional bytes
            // };
        }
    }
}
