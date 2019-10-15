// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Reflection.Internal;
using System.Reflection.Metadata.Tests;
using Xunit;

namespace System.Reflection.PortableExecutable.Tests
{
    public class PEMemoryBlockTests
    {
        [Fact]
        public unsafe void Default()
        {
            var peBlock = default(PEMemoryBlock);
            Assert.True(peBlock.Pointer == null);
            Assert.Equal(0, peBlock.Length);

            var reader1 = peBlock.GetReader();
            Assert.Equal(0, reader1.Length);

            var reader2 = peBlock.GetReader(0, 0);
            Assert.Equal(0, reader2.Length);

            AssertEx.Equal(new byte[0], peBlock.GetContent());
            AssertEx.Equal(new byte[0], peBlock.GetContent(0, 0));
        }

        [Fact]
        public void Default_Errors()
        {
            var peBlock = default(PEMemoryBlock);
            Assert.Throws<ArgumentOutOfRangeException>(() => peBlock.GetContent(1, 0));
            Assert.Throws<ArgumentOutOfRangeException>(() => peBlock.GetContent(0, 1));
            Assert.Throws<ArgumentOutOfRangeException>(() => peBlock.GetContent(-1, 0));
            Assert.Throws<ArgumentOutOfRangeException>(() => peBlock.GetContent(0, -1));
            Assert.Throws<ArgumentOutOfRangeException>(() => peBlock.GetContent(-1, -1));

            Assert.Throws<ArgumentOutOfRangeException>(() => peBlock.GetReader(1, 0));
            Assert.Throws<ArgumentOutOfRangeException>(() => peBlock.GetReader(0, 1));
            Assert.Throws<ArgumentOutOfRangeException>(() => peBlock.GetReader(-1, 0));
            Assert.Throws<ArgumentOutOfRangeException>(() => peBlock.GetReader(0, -1));
            Assert.Throws<ArgumentOutOfRangeException>(() => peBlock.GetReader(-1, -1));
        }

        [Fact]
        public unsafe void GetReader_NoOffset()
        {
            var array = ImmutableArray.Create(new byte[] { 1, 2, 3, 4 });
            var provider = new ByteArrayMemoryProvider(array);
            var block = provider.GetMemoryBlock();

            var peBlock = new PEMemoryBlock(block);

            var reader1 = peBlock.GetReader();
            Assert.Equal(4, reader1.Length);
            AssertEx.Equal(new byte[] { 1, 2, 3 }, reader1.ReadBytes(3));
            AssertEx.Equal(new byte[] { 4 }, reader1.ReadBytes(1));

            var reader2 = peBlock.GetReader(1, 2);
            Assert.Equal(2, reader2.Length);
            AssertEx.Equal(new byte[] { 2, 3 }, reader2.ReadBytes(2));

            var reader3 = peBlock.GetReader(4, 0);
            Assert.Equal(0, reader3.Length);
            AssertEx.Equal(new byte[] { }, reader3.ReadBytes(0));

            Assert.Throws<ArgumentOutOfRangeException>(() => peBlock.GetReader(0, 5));
            Assert.Throws<ArgumentOutOfRangeException>(() => peBlock.GetReader(4, 1));
            Assert.Throws<ArgumentOutOfRangeException>(() => peBlock.GetReader(5, 0));
        }

        [Fact]
        public unsafe void GetReader_Offset()
        {
            var array = ImmutableArray.Create(new byte[] { 0, 1, 2, 3, 4 });
            var provider = new ByteArrayMemoryProvider(array);
            var block = provider.GetMemoryBlock();

            var peBlock = new PEMemoryBlock(block, offset: 1);

            var reader1 = peBlock.GetReader();
            Assert.Equal(4, reader1.Length);
            AssertEx.Equal(new byte[] { 1, 2, 3 }, reader1.ReadBytes(3));
            AssertEx.Equal(new byte[] { 4 }, reader1.ReadBytes(1));

            var reader2 = peBlock.GetReader(1, 2);
            Assert.Equal(2, reader2.Length);
            AssertEx.Equal(new byte[] { 2, 3 }, reader2.ReadBytes(2));

            var reader3 = peBlock.GetReader(4, 0);
            Assert.Equal(0, reader3.Length);
            AssertEx.Equal(new byte[] { }, reader3.ReadBytes(0));

            Assert.Throws<ArgumentOutOfRangeException>(() => peBlock.GetReader(0, 5));
            Assert.Throws<ArgumentOutOfRangeException>(() => peBlock.GetReader(4, 1));
            Assert.Throws<ArgumentOutOfRangeException>(() => peBlock.GetReader(5, 0));
        }

        [Fact]
        public unsafe void GetContent_NoOffset()
        {
            var array = ImmutableArray.Create(new byte[] { 1, 2, 3, 4 });
            var provider = new ByteArrayMemoryProvider(array);
            var block = provider.GetMemoryBlock();

            var peBlock = new PEMemoryBlock(block);

            AssertEx.Equal(new byte[] { 1, 2, 3, 4 }, peBlock.GetContent());
            AssertEx.Equal(new byte[] { 2, 3 }, peBlock.GetContent(1, 2));
            AssertEx.Equal(new byte[] { }, peBlock.GetContent(4, 0));

            Assert.Throws<ArgumentOutOfRangeException>(() => peBlock.GetContent(0, 5));
            Assert.Throws<ArgumentOutOfRangeException>(() => peBlock.GetContent(4, 1));
            Assert.Throws<ArgumentOutOfRangeException>(() => peBlock.GetContent(5, 0));
        }

        [Fact]
        public unsafe void GetContent_Offset()
        {
            var array = ImmutableArray.Create(new byte[] { 0, 1, 2, 3, 4 });
            var provider = new ByteArrayMemoryProvider(array);
            var block = provider.GetMemoryBlock();

            var peBlock = new PEMemoryBlock(block, offset: 1);

            AssertEx.Equal(new byte[] { 1, 2, 3, 4 }, peBlock.GetContent());
            AssertEx.Equal(new byte[] { 2, 3 }, peBlock.GetContent(1, 2));
            AssertEx.Equal(new byte[] { }, peBlock.GetContent(4, 0));

            Assert.Throws<ArgumentOutOfRangeException>(() => peBlock.GetContent(0, 5));
            Assert.Throws<ArgumentOutOfRangeException>(() => peBlock.GetContent(4, 1));
            Assert.Throws<ArgumentOutOfRangeException>(() => peBlock.GetContent(5, 0));
        }
    }
}
