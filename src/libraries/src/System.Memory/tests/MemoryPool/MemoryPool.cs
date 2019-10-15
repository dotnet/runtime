// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Buffers;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Runtime.CompilerServices;
using Xunit;

namespace System.MemoryTests
{
    public static partial class MemoryPoolTests
    {
        [Fact]
        public static void ThereIsOnlyOneSharedPool()
        {
            MemoryPool<int> mp1 = MemoryPool<int>.Shared;
            MemoryPool<int> mp2 = MemoryPool<int>.Shared;
            Assert.Same(mp1, mp2);
        }

        [Fact]
        public static void DisposingTheSharedPoolIsANop()
        {
            MemoryPool<int> mp = MemoryPool<int>.Shared;
            mp.Dispose();
            mp.Dispose();
            using (IMemoryOwner<int> block = mp.Rent(10))
            {
                Assert.True(block.Memory.Length >= 10);
            }
        }

        [Fact]
        public static void RentWithTooLargeASize()
        {
            MemoryPool<int> pool = MemoryPool<int>.Shared;
            Assert.Throws<ArgumentOutOfRangeException>(() => pool.Rent(pool.MaxBufferSize + 1));
        }

        [Fact]
        public static void MemoryPoolSpan()
        {
            MemoryPool<int> pool = MemoryPool<int>.Shared;
            using (IMemoryOwner<int> block = pool.Rent(10))
            {
                Memory<int> memory = block.Memory;
                Span<int> sp = memory.Span;
                Assert.Equal(memory.Length, sp.Length);
                Assert.True(MemoryMarshal.TryGetArray(memory, out ArraySegment<int> arraySegment));
                using (MemoryHandle newMemoryHandle = memory.Pin())
                {
                    unsafe
                    {
                        void* pSpan = Unsafe.AsPointer(ref MemoryMarshal.GetReference(sp));
                        Assert.Equal((IntPtr)newMemoryHandle.Pointer, (IntPtr)pSpan);
                    }
                }
            }
        }


        [Theory]
        [InlineData(0)]
        [InlineData(3)]
        [InlineData(10)]
        public static void MemoryPoolPin(int elementIndex)
        {
            MemoryPool<int> pool = MemoryPool<int>.Shared;
            using (IMemoryOwner<int> block = pool.Rent(10))
            {
                Memory<int> memory = block.Memory;
                Span<int> sp = memory.Span;
                Assert.Equal(memory.Length, sp.Length);
                Assert.True(MemoryMarshal.TryGetArray(memory, out ArraySegment<int> segment));
                using (MemoryHandle newMemoryHandle = memory.Slice(elementIndex).Pin())
                {
                    unsafe
                    {
                        void* pSpan = Unsafe.AsPointer(ref MemoryMarshal.GetReference(sp.Slice(elementIndex)));
                        Assert.Equal((IntPtr)pSpan, ((IntPtr)newMemoryHandle.Pointer));
                    }
                }
            }
        }

        [Theory]
        [InlineData(-1)]
        [InlineData(int.MinValue)]
        public static void MemoryPoolPinBadOffset(int elementIndex)
        {
            MemoryPool<int> pool = MemoryPool<int>.Shared;
            IMemoryOwner<int> block = pool.Rent(10);
            Memory<int> memory = block.Memory;
            Span<int> sp = memory.Span;
            Assert.Equal(memory.Length, sp.Length);
            Assert.Throws<ArgumentOutOfRangeException>(() => memory.Slice(elementIndex).Pin());
        }

        [Fact]
        public static void MemoryPoolPinOffsetAtEnd()
        {
            MemoryPool<int> pool = MemoryPool<int>.Shared;
            IMemoryOwner<int> block = pool.Rent(10);
            Memory<int> memory = block.Memory;
            Span<int> sp = memory.Span;
            Assert.Equal(memory.Length, sp.Length);

            int elementIndex = memory.Length;
            Assert.True(MemoryMarshal.TryGetArray(memory, out ArraySegment<int> segment));
            using (MemoryHandle newMemoryHandle = memory.Slice(elementIndex).Pin())
            {
                unsafe
                {
                    void* pSpan = Unsafe.AsPointer(ref MemoryMarshal.GetReference(sp.Slice(elementIndex)));
                    Assert.Equal((IntPtr)pSpan, ((IntPtr)newMemoryHandle.Pointer));
                }
            }
        }

        [Fact]
        public static void MemoryPoolPinBadOffsetTooLarge()
        {
            MemoryPool<int> pool = MemoryPool<int>.Shared;
            IMemoryOwner<int> block = pool.Rent(10);
            Memory<int> memory = block.Memory;
            Span<int> sp = memory.Span;
            Assert.Equal(memory.Length, sp.Length);

            int elementIndex = memory.Length + 1;
            Assert.True(MemoryMarshal.TryGetArray(memory, out ArraySegment<int> segment));
            Assert.Throws<ArgumentOutOfRangeException>(() => memory.Slice(elementIndex).Pin());
        }

        [Fact]
        public static void EachRentalIsUniqueUntilDisposed()
        {
            MemoryPool<int> pool = MemoryPool<int>.Shared;
            List<IMemoryOwner<int>> priorBlocks = new List<IMemoryOwner<int>>();

            Random r = new Random(42);
            List<int> testInputs = new List<int>();
            for (int i = 0; i < 100; i++)
            {
                testInputs.Add((Math.Abs(r.Next() % 1000)) + 1);
            }

            foreach (int minBufferSize in testInputs)
            {
                IMemoryOwner<int> newBlock = pool.Rent(minBufferSize);
                Memory<int> memory = newBlock.Memory;
                Assert.True(memory.Length >= minBufferSize);
                Assert.True(MemoryMarshal.TryGetArray(newBlock.Memory, out ArraySegment<int> newArraySegment));
                foreach (IMemoryOwner<int> prior in priorBlocks)
                {
                    Assert.True(MemoryMarshal.TryGetArray(prior.Memory, out ArraySegment<int> priorArraySegment));
                    using (MemoryHandle priorMemoryHandle = prior.Memory.Pin())
                    {
                        using (MemoryHandle newMemoryHandle = newBlock.Memory.Pin())
                        {
                            unsafe
                            {
                                Assert.NotEqual((IntPtr)priorMemoryHandle.Pointer, (IntPtr)newMemoryHandle.Pointer);
                            }
                        }
                    }
                }
                priorBlocks.Add(newBlock);
            }

            foreach (IMemoryOwner<int> prior in priorBlocks)
            {
                Assert.True(MemoryMarshal.TryGetArray(prior.Memory, out ArraySegment<int> priorArraySegment));
                prior.Dispose();
            }
        }

        [Fact]
        public static void RentWithDefaultSize()
        {
            using (IMemoryOwner<int> block = MemoryPool<int>.Shared.Rent(minBufferSize: -1))
            {
                Assert.True(block.Memory.Length >= 1);
            }

            using (IMemoryOwner<int> block = MemoryPool<int>.Shared.Rent(minBufferSize: -1))
            {
                Assert.True(block.Memory.Length >= 1);
                block.Dispose();    // intentional double dispose
            }
        }

        [Theory]
        [MemberData(nameof(BadSizes))]
        public static void RentBadSizes(int badSize)
        {
            MemoryPool<int> pool = MemoryPool<int>.Shared;
            Assert.Throws<ArgumentOutOfRangeException>(() => pool.Rent(minBufferSize: badSize));
        }

        public static IEnumerable<object[]> BadSizes
        {
            get
            {
                yield return new object[] { -2 };
                yield return new object[] { int.MinValue };
            }
        }

        [Fact]
        public static void MemoryPoolTryGetArray()
        {
            using (IMemoryOwner<int> block = MemoryPool<int>.Shared.Rent(42))
            {
                Memory<int> memory = block.Memory;
                bool success = MemoryMarshal.TryGetArray(memory, out ArraySegment<int> arraySegment);
                Assert.True(success);
                Assert.Equal(memory.Length, arraySegment.Count);
                unsafe
                {
                    Assert.True(MemoryMarshal.TryGetArray(memory, out arraySegment));
                    fixed (int* pArray = arraySegment.Array)
                    {
                        void* pSpan = Unsafe.AsPointer(ref MemoryMarshal.GetReference(memory.Span));
                        Assert.Equal((IntPtr)pSpan, (IntPtr)pArray);
                    }
                }
            }
        }

        [Fact]
        public static void ExtraDisposesAreIgnored()
        {
            IMemoryOwner<int> block = MemoryPool<int>.Shared.Rent(42);
            block.Dispose();
            block.Dispose();
        }

        [Fact]
        public static void NoMemoryAfterDispose()
        {
            IMemoryOwner<int> block = MemoryPool<int>.Shared.Rent(42);
            block.Dispose();
            Assert.Throws<ObjectDisposedException>(() => block.Memory);
        }
    }
}
