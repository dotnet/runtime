// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Xunit;

namespace System.Runtime.InteropServices.Tests
{
    public unsafe class NativeMemoryTests
    {
        [Fact]
        public void AlignedAllocTest()
        {
            void* ptr = NativeMemory.AlignedAlloc(1, (uint)sizeof(nuint));
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
            NativeMemory.AlignedFree(ptr);
        }

        [Fact]
        public void AlignedFreeTest()
        {
            // This should not throw
            NativeMemory.AlignedFree(null);
        }

        [Fact]
        public void AllocTest()
        {
            void* ptr = NativeMemory.Alloc(1);
            Assert.True(ptr != null);
            NativeMemory.Free(ptr);
        }

        [Fact]
        public void AllocOOMTest()
        {
            Assert.Throws<OutOfMemoryException>(() => NativeMemory.Alloc(nuint.MaxValue));
        }

        [Fact]
        public void AllocZeroSizeTest()
        {
            void* ptr = NativeMemory.Alloc(0);
            Assert.True(ptr != null);
            NativeMemory.Free(ptr);
        }

        [Fact]
        public void AllocZeroedTest()
        {
            void* ptr = NativeMemory.AllocZeroed(1, 1);

            Assert.True(ptr != null);
            Assert.Equal(expected: 0, actual: ((byte*)ptr)[0]);

            NativeMemory.Free(ptr);
        }

        [Fact]
        public void AllocZeroedOOMTest()
        {
            Assert.Throws<OutOfMemoryException>(() => NativeMemory.AllocZeroed(1, nuint.MaxValue));
            Assert.Throws<OutOfMemoryException>(() => NativeMemory.AllocZeroed(nuint.MaxValue, 1));
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
    }
}
