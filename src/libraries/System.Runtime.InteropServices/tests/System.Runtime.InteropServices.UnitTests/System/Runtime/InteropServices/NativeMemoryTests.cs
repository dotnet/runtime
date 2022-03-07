// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Xunit;

namespace System.Runtime.InteropServices.Tests
{
    public unsafe class NativeMemoryTests
    {
        [Theory]
        [InlineData(1)]
        [InlineData(2)]
        [InlineData(4)]
        [InlineData(8)]
        [InlineData(16)]
        [InlineData(32)]
        [InlineData(64)]
        [InlineData(128)]
        [InlineData(256)]
        [InlineData(512)]
        [InlineData(1 * 1024)]
        [InlineData(2 * 1024)]
        [InlineData(4 * 1024)]
        [InlineData(8 * 1024)]
        [InlineData(16 * 1024)]
        [InlineData(64 * 1024)]
        [InlineData(1 * 1024 * 1024)]
        [InlineData(2 * 1024 * 1024)]
        [InlineData(4 * 1024 * 1024)]
        public void AlignedAllocTest(uint alignment)
        {
            void* ptr = NativeMemory.AlignedAlloc(1, alignment);

            Assert.True(ptr != null);
            Assert.True((nuint)ptr % alignment == 0);

            NativeMemory.AlignedFree(ptr);
        }

        [Fact]
        public void AlignedAllocLessThanVoidPtrAlignmentTest()
        {
            void* ptr = NativeMemory.AlignedAlloc(1, 1);
            Assert.True(ptr != null);
            NativeMemory.AlignedFree(ptr);
        }

        [Fact]
        public void AlignedAllocOOMTest()
        {
            Assert.Throws<OutOfMemoryException>(() => NativeMemory.AlignedAlloc(nuint.MaxValue - ((uint)sizeof(nuint) - 1), (uint)sizeof(nuint)));
        }

        [Fact]
        public void AlignedAllocZeroAlignmentTest()
        {
            Assert.Throws<ArgumentException>(() => NativeMemory.AlignedAlloc((uint)sizeof(nuint), 0));
        }

        [Fact]
        public void AlignedAllocNonPowerOfTwoAlignmentTest()
        {
            Assert.Throws<ArgumentException>(() => NativeMemory.AlignedAlloc((uint)sizeof(nuint), (uint)sizeof(nuint) + 1));
            Assert.Throws<ArgumentException>(() => NativeMemory.AlignedAlloc((uint)sizeof(nuint), (uint)sizeof(nuint) * 3));
        }

        [Fact]
        public void AlignedAllocOverflowByteCountTest()
        {
            // POSIX requires byteCount to be a multiple of alignment and so we will internally upsize.
            // This upsizing can overflow for certain values since we do (byteCount + (alignment - 1)) & ~(alignment - 1)
            //
            // However, this overflow is "harmless" since it will result in a value that is less than alignment
            // given that alignment is a power of two and will ultimately be a value less than alignment which
            // will be treated as invalid and result in OOM.
            //
            // Take for example a 64-bit system where the max power of two is (1UL << 63): 9223372036854775808
            // * 9223372036854775808 + 9223372036854775807 == ulong.MaxValue, so no overflow
            // * 9223372036854775809 + 9223372036854775807 == 0, so overflows and is less than alignment
            // *      ulong.MaxValue + 9223372036854775807 == 9223372036854775806, so overflows and is less than alignment
            //
            // Likewise, for small alignments such as 8 (which is the smallest on a 64-bit system for POSIX):
            // * 18446744073709551608 + 7 == ulong.MaxValue, so no overflow
            // * 18446744073709551609 + 7 == 0, so overflows and is less than alignment
            // *       ulong.MaxValue + 7 == 6, so overflows and is less than alignment

            nuint maxAlignment = (nuint)1 << ((sizeof(nuint) * 8) - 1);
            Assert.Throws<OutOfMemoryException>(() => NativeMemory.AlignedAlloc(maxAlignment + 1, maxAlignment));

            Assert.Throws<OutOfMemoryException>(() => NativeMemory.AlignedAlloc(nuint.MaxValue, (uint)sizeof(nuint)));
        }

        [Fact]
        public void AlignedAllocZeroSizeTest()
        {
            void* ptr = NativeMemory.AlignedAlloc(0, (uint)sizeof(nuint));

            Assert.True(ptr != null);
            Assert.True((nuint)ptr % (uint)sizeof(nuint) == 0);

            NativeMemory.AlignedFree(ptr);
        }

        [Fact]
        public void AlignedFreeTest()
        {
            // This should not throw
            NativeMemory.AlignedFree(null);
        }

        [Theory]
        [InlineData(1)]
        [InlineData(2)]
        [InlineData(4)]
        [InlineData(8)]
        [InlineData(16)]
        [InlineData(32)]
        [InlineData(64)]
        [InlineData(128)]
        [InlineData(256)]
        [InlineData(512)]
        [InlineData(1 * 1024)]
        [InlineData(2 * 1024)]
        [InlineData(4 * 1024)]
        [InlineData(8 * 1024)]
        [InlineData(16 * 1024)]
        [InlineData(64 * 1024)]
        [InlineData(1 * 1024 * 1024)]
        [InlineData(2 * 1024 * 1024)]
        [InlineData(4 * 1024 * 1024)]
        public void AlignedReallocTest(uint alignment)
        {
            void* ptr = NativeMemory.AlignedAlloc(1, alignment);

            Assert.True(ptr != null);
            Assert.True((nuint)ptr % alignment == 0);

            void* newPtr = NativeMemory.AlignedRealloc(ptr, 1, alignment);

            Assert.True(newPtr != null);
            Assert.True((nuint)newPtr % alignment == 0);

            NativeMemory.AlignedFree(newPtr);
        }

        [Fact]
        public void AlignedReallocLessThanVoidPtrAlignmentTest()
        {
            void* ptr = NativeMemory.AlignedAlloc(1, 1);
            Assert.True(ptr != null);

            void* newPtr = NativeMemory.AlignedRealloc(ptr, 1, 1);
            Assert.True(newPtr != null);
            NativeMemory.AlignedFree(newPtr);
        }

        [Fact]
        public void AlignedReallocNullPtrTest()
        {
            void* ptr = NativeMemory.AlignedRealloc(null, 1, (uint)sizeof(nuint));

            Assert.True(ptr != null);
            Assert.True((nuint)ptr % (uint)sizeof(nuint) == 0);

            NativeMemory.AlignedFree(ptr);
        }

        [Fact]
        public void AlignedReallocNullPtrOOMTest()
        {
            Assert.Throws<OutOfMemoryException>(() => NativeMemory.AlignedRealloc(null, nuint.MaxValue, (uint)sizeof(nuint)));
        }

        [Fact]
        public void AlignedReallocNullPtrZeroSizeTest()
        {
            void* ptr = NativeMemory.AlignedRealloc(null, 0, (uint)sizeof(nuint));

            Assert.True(ptr != null);
            Assert.True((nuint)ptr % (uint)sizeof(nuint) == 0);

            NativeMemory.AlignedFree(ptr);
        }

        [Fact]
        public void AlignedReallocZeroAlignmentTest()
        {
            void* ptr = NativeMemory.AlignedAlloc(1, (uint)sizeof(nuint));

            Assert.True(ptr != null);
            Assert.True((nuint)ptr % (uint)sizeof(nuint) == 0);

            Assert.Throws<ArgumentException>(() => NativeMemory.AlignedRealloc(ptr, (uint)sizeof(nuint), 0));
            NativeMemory.AlignedFree(ptr);
        }

        [Fact]
        public void AlignedReallocNonPowerOfTwoAlignmentTest()
        {
            void* ptr = NativeMemory.AlignedAlloc(1, (uint)sizeof(nuint));

            Assert.True(ptr != null);
            Assert.True((nuint)ptr % (uint)sizeof(nuint) == 0);

            Assert.Throws<ArgumentException>(() => NativeMemory.AlignedRealloc(ptr, (uint)sizeof(nuint), (uint)sizeof(nuint) + 1));
            Assert.Throws<ArgumentException>(() => NativeMemory.AlignedRealloc(ptr, (uint)sizeof(nuint), (uint)sizeof(nuint) * 3));
            NativeMemory.AlignedFree(ptr);
        }

        [Fact]
        public void AlignedReallocZeroSizeTest()
        {
            void* ptr = NativeMemory.AlignedAlloc(1, (uint)sizeof(nuint));

            Assert.True(ptr != null);
            Assert.True((nuint)ptr % (uint)sizeof(nuint) == 0);

            void* newPtr = NativeMemory.AlignedRealloc(ptr, 0, (uint)sizeof(nuint));

            Assert.True(newPtr != null);
            Assert.True((nuint)newPtr % (uint)sizeof(nuint) == 0);

            NativeMemory.AlignedFree(newPtr);
        }

        [Fact]
        public void AlignedReallocSmallerToLargerTest()
        {
            void* ptr = NativeMemory.AlignedAlloc(16, 16);

            Assert.True(ptr != null);
            Assert.True((nuint)ptr % 16 == 0);

            for (int i = 0; i < 16; i++)
            {
                ((byte*)ptr)[i] = (byte)i;
            }

            void* newPtr = NativeMemory.AlignedRealloc(ptr, 32, 16);

            Assert.True(newPtr != null);
            Assert.True((nuint)newPtr % 16 == 0);

            for (int i = 0; i < 16; i++)
            {
                Assert.True(((byte*)newPtr)[i] == i);
            }

            NativeMemory.AlignedFree(newPtr);
        }

        [Fact]
        public void AllocByteCountTest()
        {
            void* ptr = NativeMemory.Alloc(1);
            Assert.True(ptr != null);
            NativeMemory.Free(ptr);
        }

        [Fact]
        public void AllocElementCountTest()
        {
            void* ptr = NativeMemory.Alloc(1, 1);
            Assert.True(ptr != null);
            NativeMemory.Free(ptr);
        }

        [Fact]
        public void AllocByteCountOOMTest()
        {
            Assert.Throws<OutOfMemoryException>(() => NativeMemory.Alloc(nuint.MaxValue));
        }

        [Fact]
        public void AllocElementCountOOMTest()
        {
            Assert.Throws<OutOfMemoryException>(() => NativeMemory.Alloc(1, nuint.MaxValue));
            Assert.Throws<OutOfMemoryException>(() => NativeMemory.Alloc(nuint.MaxValue, 1));
            Assert.Throws<OutOfMemoryException>(() => NativeMemory.Alloc(nuint.MaxValue, nuint.MaxValue));
        }

        [Fact]
        public void AllocZeroByteCountTest()
        {
            void* ptr = NativeMemory.Alloc(0);
            Assert.True(ptr != null);
            NativeMemory.Free(ptr);
        }

        [Fact]
        public void AllocZeroElementCountTest()
        {
            void* ptr = NativeMemory.Alloc(0, 1);
            Assert.True(ptr != null);
            NativeMemory.Free(ptr);
        }

        [Fact]
        public void AllocZeroElementSizeTest()
        {
            void* ptr = NativeMemory.Alloc(1, 0);
            Assert.True(ptr != null);
            NativeMemory.Free(ptr);
        }

        [Fact]
        public void AllocZeroedByteCountTest()
        {
            void* ptr = NativeMemory.AllocZeroed(1);

            Assert.True(ptr != null);
            Assert.Equal(expected: 0, actual: ((byte*)ptr)[0]);

            NativeMemory.Free(ptr);
        }

        [Fact]
        public void AllocZeroedElementCountTest()
        {
            void* ptr = NativeMemory.AllocZeroed(1, 1);

            Assert.True(ptr != null);
            Assert.Equal(expected: 0, actual: ((byte*)ptr)[0]);

            NativeMemory.Free(ptr);
        }

        [Fact]
        public void AllocZeroedByteCountOOMTest()
        {
            Assert.Throws<OutOfMemoryException>(() => NativeMemory.AllocZeroed(nuint.MaxValue));
        }

        [Fact]
        public void AllocZeroedElementCountOOMTest()
        {
            Assert.Throws<OutOfMemoryException>(() => NativeMemory.AllocZeroed(1, nuint.MaxValue));
            Assert.Throws<OutOfMemoryException>(() => NativeMemory.AllocZeroed(nuint.MaxValue, 1));
            Assert.Throws<OutOfMemoryException>(() => NativeMemory.AllocZeroed(nuint.MaxValue, nuint.MaxValue));
        }

        [Fact]
        public void AllocZeroedZeroByteCountTest()
        {
            void* ptr = NativeMemory.AllocZeroed(0);
            Assert.True(ptr != null);
            NativeMemory.Free(ptr);
        }

        [Fact]
        public void AllocZeroedZeroElementCountTest()
        {
            void* ptr = NativeMemory.AllocZeroed(0, 1);
            Assert.True(ptr != null);
            NativeMemory.Free(ptr);
        }

        [Fact]
        public void AllocZeroedZeroElementSizeTest()
        {
            void* ptr = NativeMemory.AllocZeroed(1, 0);
            Assert.True(ptr != null);
            NativeMemory.Free(ptr);
        }

        [Fact]
        public void FreeTest()
        {
            // This should not throw
            NativeMemory.Free(null);
        }

        [Fact]
        public void ReallocTest()
        {
            void* ptr = NativeMemory.Alloc(1);
            Assert.True(ptr != null);

            void* newPtr = NativeMemory.Realloc(ptr, 1);
            Assert.True(newPtr != null);
            NativeMemory.Free(newPtr);
        }

        [Fact]
        public void ReallocNullPtrTest()
        {
            void* ptr = NativeMemory.Realloc(null, 1);
            Assert.True(ptr != null);
            NativeMemory.Free(ptr);
        }

        [Fact]
        public void ReallocNullPtrOOMTest()
        {
            Assert.Throws<OutOfMemoryException>(() => NativeMemory.Realloc(null, nuint.MaxValue));
        }

        [Fact]
        public void ReallocNullPtrZeroSizeTest()
        {
            void* ptr = NativeMemory.Realloc(null, 0);
            Assert.True(ptr != null);
            NativeMemory.Free(ptr);
        }

        [Fact]
        public void ReallocZeroSizeTest()
        {
            void* ptr = NativeMemory.Alloc(1);
            Assert.True(ptr != null);

            void* newPtr = NativeMemory.Realloc(ptr, 0);
            Assert.True(newPtr != null);
            NativeMemory.Free(newPtr);
        }

        [Fact]
        public void ReallocSmallerToLargerTest()
        {
            void* ptr = NativeMemory.Alloc(16);
            Assert.True(ptr != null);

            for (int i = 0; i < 16; i++)
            {
                ((byte*)ptr)[i] = (byte)i;
            }

            void* newPtr = NativeMemory.Realloc(ptr, 32);
            Assert.True(newPtr != null);

            for (int i = 0; i < 16; i++)
            {
                Assert.True(((byte*)newPtr)[i] == i);
            }

            NativeMemory.Free(newPtr);
        }
    }
}
