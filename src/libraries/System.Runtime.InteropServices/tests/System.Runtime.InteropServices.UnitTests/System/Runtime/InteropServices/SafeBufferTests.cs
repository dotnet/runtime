// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

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
        public unsafe void AcquirePointer_Disposed_ThrowsObjectDisposedException()
        {
            var buffer = new SubBuffer(true);
            buffer.Initialize(4);
            buffer.Dispose();

            byte* pointer = (byte*)12345;
            Assert.Throws<ObjectDisposedException>(() => buffer.AcquirePointer(ref pointer));
            Assert.True(pointer is null);
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

            Assert.Throws<ArgumentException>(null, () => buffer.Read<int>(byteOffset));
            Assert.Throws<ArgumentException>(null, () => buffer.Write<int>(byteOffset, 2));
        }

        [Fact]
        public void ReadArray_NullArray_ThrowsArgumentNullException()
        {
            var wrapper = new SubBuffer(true);
            AssertExtensions.Throws<ArgumentNullException>("array", () => wrapper.ReadArray<int>(0, null, 0, 0));
            AssertExtensions.Throws<ArgumentNullException>("array", () => wrapper.WriteArray<int>(0, null, 0, 0));
        }

        [Fact]
        public void ReadWriteSpan_EmptySpan_Passes()
        {
            var buffer = new SubBuffer(true);
            buffer.Initialize(0);

            buffer.ReadSpan<int>(0, Span<int>.Empty);
            buffer.WriteSpan<int>(0, ReadOnlySpan<int>.Empty);
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
        public void ReadWriteArray_NegativeCount_ThrowsArgumentException(int arrayLength, int index, int count)
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

        [Fact]
        public void ReadWrite_RoundTrip()
        {
            using var buffer = new HGlobalBuffer(100);

            int intValue = 1234;
            buffer.Write<int>(0, intValue);
            Assert.Equal(intValue, buffer.Read<int>(0));

            double doubleValue = 123.45;
            buffer.Write<double>(10, doubleValue);
            Assert.Equal(doubleValue, buffer.Read<double>(10));

            TestStruct structValue = new TestStruct
            {
                I = 1234,
                L = 987654321,
                D = double.MaxValue
            };
            buffer.Write<TestStruct>(0, structValue);
            Assert.Equal(structValue, buffer.Read<TestStruct>(0));
        }

        [Fact]
        public void ReadWriteSpanArray_RoundTrip()
        {
            using var buffer = new HGlobalBuffer(200);

            int[] intArray = new int[] { 11, 22, 33, 44 };
            TestArray(intArray);
            TestSpan<int>(intArray);

            TestStruct[] structArray = new TestStruct[]
            {
                new TestStruct { I = 11, L = 22, D = 33 },
                new TestStruct { I = 44, L = 55, D = 66 },
                new TestStruct { I = 77, L = 88, D = 99 },
                new TestStruct { I = 100, L = 200, D = 300 },
            };
            TestArray(structArray);
            TestSpan<TestStruct>(structArray);

            void TestArray<T>(T[] data)
                where T : struct
            {
                T[] destination = new T[data.Length];
                buffer.WriteArray(0, data, 0, data.Length);
                buffer.ReadArray(0, destination, 0, data.Length);
                Assert.Equal(data, destination);
            }

            void TestSpan<T>(ReadOnlySpan<T> data)
                where T : unmanaged
            {
                Span<T> destination = stackalloc T[data.Length];
                buffer.WriteSpan(0, data);
                buffer.ReadSpan(0, destination);
                for (int i = 0; i < data.Length; i++)
                    Assert.Equal(data[i], destination[i]);
            }
        }

        public class SubBuffer : SafeBuffer
        {
            public SubBuffer(bool ownsHandle) : base(ownsHandle) { }

            protected override bool ReleaseHandle()
            {
                throw new NotImplementedException();
            }
        }

        public class HGlobalBuffer : SafeBuffer
        {
            public HGlobalBuffer(int length) : base(true)
            {
                SetHandle(Marshal.AllocHGlobal(length));
                Initialize((ulong)length);
            }

            protected override bool ReleaseHandle()
            {
                Marshal.FreeHGlobal(handle);
                return true;
            }
        }

        public struct TestStruct
        {
            public int I;
            public long L;
            public double D;
        }
    }
}
