// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using Xunit;

namespace System.IO.Tests
{
    public class ReadOnlyMemoryStreamTests
    {
        [Fact]
        public void ConstructorFromMemoryImplicitConversion()
        {
            byte[] buffer = { 1, 2, 3, 4, 5 };
            Memory<byte> memory = buffer;
            Stream stream = new ReadOnlyMemoryStream(memory);

            Assert.Equal(5, stream.Length);
            Assert.True(stream.CanRead);
        }

        [Fact]
        public void WorksWithSlicedMemory()
        {
            byte[] largeBuffer = { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9 };
            ReadOnlyMemory<byte> slice = largeBuffer.AsMemory(3, 4);
            Stream stream = new ReadOnlyMemoryStream(slice);

            Assert.Equal(4, stream.Length);

            byte[] result = new byte[4];
            int bytesRead = stream.Read(result, 0, 4);

            Assert.Equal(4, bytesRead);
            Assert.Equal(new byte[] { 3, 4, 5, 6 }, result);
        }

        [Fact]
        public void ReadFromUnmanagedMemory()
        {
            byte[] expected = [1, 2, 3, 4, 5];

            using var manager = new NativeMemoryManager(expected.Length);
            expected.CopyTo(manager.GetSpan());

            using var stream = new ReadOnlyMemoryStream(manager.Memory);

            byte[] result = new byte[expected.Length];
            int bytesRead = stream.Read(result);

            Assert.Equal(expected.Length, bytesRead);
            Assert.Equal(expected, result);
        }
    }
}
