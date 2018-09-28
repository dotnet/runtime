// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace System.Runtime.InteropServices.Tests
{
    public class SafeBufferTests
    {
        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void Ctor_Bool(bool ownsHandle)
        {
            var buffer = new SubBuffer(ownsHandle);
            Assert.True(buffer.IsInvalid);
        }

        [Fact]
        public void Initialize_InvalidNumBytes_ThrowsArgumentOutOfRangeException()
        {
            var buffer = new SubBuffer(true);
            AssertExtensions.Throws<ArgumentOutOfRangeException>("numBytes", () => buffer.Initialize(ulong.MaxValue));
        }

        [Fact]
        [SkipOnTargetFramework(~TargetFrameworkMonikers.NetFramework)]
        public void Initialize_NumBytesTimesSizeOfEachElement_NetFramework_ThrowsOverflowException()
        {
            var buffer = new SubBuffer(true);
            Assert.Throws<OverflowException>(() => buffer.Initialize(uint.MaxValue, uint.MaxValue));
            Assert.Throws<OverflowException>(() => buffer.Initialize<int>(uint.MaxValue));
        }

        [Fact]
        [SkipOnTargetFramework(TargetFrameworkMonikers.NetFramework)]
        public void Initialize_NumBytesTimesSizeOfEachElement_ThrowsArgumentOutOfRangeExceptionIfNot64Bit()
        {
            var buffer = new SubBuffer(true);
            AssertExtensions.ThrowsIf<ArgumentOutOfRangeException>(!Environment.Is64BitProcess, () => buffer.Initialize(uint.MaxValue, uint.MaxValue));
            AssertExtensions.ThrowsIf<ArgumentOutOfRangeException>(!Environment.Is64BitProcess, () => buffer.Initialize<int>(uint.MaxValue));
        }

        [Fact]
        public unsafe void AcquirePointer_NotInitialized_ThrowsInvalidOperationException()
        {
            var wrapper = new SubBuffer(true);
            byte* pointer = null;
            Assert.Throws<InvalidOperationException>(() => wrapper.AcquirePointer(ref pointer));
        }

        [Fact]
        public void ReleasePointer_NotInitialized_ThrowsInvalidOperationException()
        {
            var wrapper = new SubBuffer(true);
            Assert.Throws<InvalidOperationException>(() => wrapper.ReleasePointer());
        }

        [Fact]
        public void ReadWrite_NotInitialized_ThrowsInvalidOperationException()
        {
            var wrapper = new SubBuffer(true);

            Assert.Throws<InvalidOperationException>(() => wrapper.Read<int>(0));
            Assert.Throws<InvalidOperationException>(() => wrapper.Write(0, 2));
        }

        [Theory]
        [InlineData(4)]
        [InlineData(3)]
        [InlineData(ulong.MaxValue)]
        public void ReadWrite_NotEnoughSpaceInBuffer_ThrowsArgumentException(ulong byteOffset)
        {
            var buffer = new SubBuffer(true);
            buffer.Initialize(4);

            Assert.Throws<ArgumentException>(null, () => buffer.Read<int>(4));
            Assert.Throws<ArgumentException>(null, () => buffer.Write<int>(4, 2));
        }

        [Fact]
        public void ReadArray_NullArray_ThrowsArgumentNullException()
        {
            var wrapper = new SubBuffer(true);
            AssertExtensions.Throws<ArgumentNullException>("array", () => wrapper.ReadArray<int>(0, null, 0, 0));
            AssertExtensions.Throws<ArgumentNullException>("array", () => wrapper.WriteArray<int>(0, null, 0, 0));
        }

        [Fact]
        public void ReadArray_NegativeIndex_ThrowsArgumentOutOfRangeException()
        {
            var wrapper = new SubBuffer(true);
            AssertExtensions.Throws<ArgumentOutOfRangeException>("index", () => wrapper.ReadArray(0, new int[0], -1, 0));
            AssertExtensions.Throws<ArgumentOutOfRangeException>("index", () => wrapper.WriteArray(0, new int[0], -1, 0));
        }

        [Fact]
        public void ReadWriteArray_NegativeCount_ThrowsArgumentOutOfRangeException()
        {
            var wrapper = new SubBuffer(true);
            AssertExtensions.Throws<ArgumentOutOfRangeException>("count", () => wrapper.ReadArray(0, new int[0], 0, -1));
            AssertExtensions.Throws<ArgumentOutOfRangeException>("count", () => wrapper.WriteArray(0, new int[0], 0, -1));
        }

        [Theory]
        [InlineData(0, 1, 0)]
        [InlineData(0, 0, 1)]
        [InlineData(2, 3, 0)]
        [InlineData(2, 2, 1)]
        [InlineData(2, 1, 2)]
        [InlineData(2, 0, 3)]
        public void ReadWriteArray_NegativeCount_ThrowsArgumentOutOfRangeException(int arrayLength, int index, int count)
        {
            var wrapper = new SubBuffer(true);
            AssertExtensions.Throws<ArgumentException>(null, () => wrapper.ReadArray(0, new int[arrayLength], index, count));
            AssertExtensions.Throws<ArgumentException>(null, () => wrapper.WriteArray(0, new int[arrayLength], index, count));
        }

        [Fact]
        public void ReadWriteArray_NotInitialized_ThrowsInvalidOperationException()
        {
            var wrapper = new SubBuffer(true);
            Assert.Throws<InvalidOperationException>(() => wrapper.ReadArray(0, new int[0], 0, 0));
            Assert.Throws<InvalidOperationException>(() => wrapper.WriteArray(0, new int[0], 0, 0));
        }

        [Fact]
        public void ByteLength_GetNotInitialized_ThrowsInvalidOperationException()
        {
            var wrapper = new SubBuffer(true);
            Assert.Throws<InvalidOperationException>(() => wrapper.ByteLength);
        }

        public class SubBuffer : SafeBuffer
        {
            public SubBuffer(bool ownsHandle) : base(ownsHandle) { }

            protected override bool ReleaseHandle()
            {
                throw new NotImplementedException();
            }
        }
    }
}
