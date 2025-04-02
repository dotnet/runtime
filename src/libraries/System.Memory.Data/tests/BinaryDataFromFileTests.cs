// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.IO;
using System.Net.Mime;
using System.Threading.Tasks;
using Xunit;

namespace System.Tests
{
    public partial class BinaryDataFromFileTests : FileCleanupTestBase
    {
        [Fact]
        public async Task CanCreateBinaryDataFromFile()
        {
            byte[] buffer = "some data"u8.ToArray();
            string path = GetRandomFilePath();
            File.WriteAllBytes(path, buffer);

            BinaryData data = BinaryData.FromFile(path);
            Assert.Equal(buffer, data.ToArray());

            byte[] output = new byte[buffer.Length];
            var outputStream = data.ToStream();
            outputStream.Read(output, 0, (int)outputStream.Length);
            Assert.Equal(buffer, output);

            data = await BinaryData.FromFileAsync(path);
            Assert.Equal(buffer, data.ToArray());

            outputStream = data.ToStream();
            outputStream.Read(output, 0, (int)outputStream.Length);
            Assert.Equal(buffer, output);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData(MediaTypeNames.Application.Soap)]
        public async Task CanCreateBinaryDataFromFileWithMediaType(string? mediaType)
        {
            byte[] buffer = "some data"u8.ToArray();
            string path = GetRandomFilePath();
            File.WriteAllBytes(path, buffer);

            BinaryData data = BinaryData.FromFile(path, mediaType);
            Assert.Equal(buffer, data.ToArray());
            Assert.Equal(mediaType, data.MediaType);

            byte[] output = new byte[buffer.Length];
            var outputStream = data.ToStream();
            outputStream.Read(output, 0, (int)outputStream.Length);
            Assert.Equal(buffer, output);

            data = await BinaryData.FromFileAsync(path, mediaType);
            Assert.Equal(buffer, data.ToArray());
            Assert.Equal(mediaType, data.MediaType);

            outputStream = data.ToStream();
            outputStream.Read(output, 0, (int)outputStream.Length);
            Assert.Equal(buffer, output);

            //changing the backing buffer should not affect the BD instance
            buffer[3] = (byte)'z';
            Assert.NotEqual(buffer, data.ToMemory().ToArray());
        }
    }
}
