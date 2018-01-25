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
            using (OwnedMemory<int> block = mp.Rent(10))
            {
                Assert.True(block.Length >= 10);
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
            using (OwnedMemory<int> block = pool.Rent(10))
            {
                Span<int> sp = block.Span;
                Assert.Equal(block.Length, sp.Length);
                using (MemoryHandle newMemoryHandle = block.Pin())
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
        [InlineData(10 * sizeof(int))]
        public static void MemoryPoolPin(int byteOffset)
        {
            MemoryPool<int> pool = MemoryPool<int>.Shared;
            using (OwnedMemory<int> block = pool.Rent(10))
            {
                Span<int> sp = block.Span;
                Assert.Equal(block.Length, sp.Length);
                using (MemoryHandle newMemoryHandle = block.Pin(byteOffset: byteOffset))
                {
                    unsafe
                    {
                        void* pSpan = Unsafe.AsPointer(ref MemoryMarshal.GetReference(sp));
                        Assert.Equal((IntPtr)pSpan, ((IntPtr)newMemoryHandle.Pointer) - byteOffset);
                    }
                }
            }
        }

        [Theory]
        [InlineData(-1)]
        [InlineData(int.MinValue)]
        public static void MemoryPoolPinBadOffset(int byteOffset)
        {
            MemoryPool<int> pool = MemoryPool<int>.Shared;
            OwnedMemory<int> block = pool.Rent(10);
            Span<int> sp = block.Span;
            Assert.Equal(block.Length, sp.Length);
            Assert.Throws<ArgumentOutOfRangeException>(() => block.Pin(byteOffset: byteOffset));
        }

        [Fact]
        public static void MemoryPoolPinOffsetAtEnd()
        {
            MemoryPool<int> pool = MemoryPool<int>.Shared;
            OwnedMemory<int> block = pool.Rent(10);
            Span<int> sp = block.Span;
            Assert.Equal(block.Length, sp.Length);

            int byteOffset = 0;
            try
            {
                byteOffset = checked(block.Length * sizeof(int));
            }
            catch (OverflowException)
            {
                return; // The pool gave us a very large block - too big to compute the byteOffset needed to carry out this test. Skip.
            }
            
            using (MemoryHandle newMemoryHandle = block.Pin(byteOffset: byteOffset))
            {
                unsafe
                {
                    void* pSpan = Unsafe.AsPointer(ref MemoryMarshal.GetReference(sp));
                    Assert.Equal((IntPtr)pSpan, ((IntPtr)newMemoryHandle.Pointer) - byteOffset);
                }
            }
        }

        [Fact]
        public static void MemoryPoolPinBadOffsetTooLarge()
        {
            MemoryPool<int> pool = MemoryPool<int>.Shared;
            OwnedMemory<int> block = pool.Rent(10);
            Span<int> sp = block.Span;
            Assert.Equal(block.Length, sp.Length);

            int byteOffset = 0;
            try
            {
                byteOffset = checked(block.Length * sizeof(int) + 1);
            }
            catch (OverflowException)
            {
                return; // The pool gave us a very large block - too big to compute the byteOffset needed to carry out this test. Skip.
            }

            Assert.Throws<ArgumentOutOfRangeException>(() => block.Pin(byteOffset: byteOffset));
        }

        [Fact]
        public static void EachRentalIsUniqueUntilDisposed()
        {
            MemoryPool<int> pool = MemoryPool<int>.Shared;
            List<OwnedMemory<int>> priorBlocks = new List<OwnedMemory<int>>();

            Random r = new Random(42);
            List<int> testInputs = new List<int>();
            for (int i = 0; i < 100; i++)
            {
                testInputs.Add((Math.Abs(r.Next() % 1000)) + 1);
            }

            foreach (int minBufferSize in testInputs)
            {
                OwnedMemory<int> newBlock = pool.Rent(minBufferSize);
                Assert.True(newBlock.Length >= minBufferSize);

                foreach (OwnedMemory<int> prior in priorBlocks)
                {
                    using (MemoryHandle priorMemoryHandle = prior.Pin())
                    {
                        using (MemoryHandle newMemoryHandle = newBlock.Pin())
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

            foreach (OwnedMemory<int> prior in priorBlocks)
            {
                prior.Dispose();
            }
        }

        [Fact]
        public static void RentWithDefaultSize()
        {
            using (OwnedMemory<int> block = MemoryPool<int>.Shared.Rent(minBufferSize: -1))
            {
                Assert.True(block.Length >= 1);
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
            using (OwnedMemory<int> block = MemoryPool<int>.Shared.Rent(42))
            {
                Memory<int> memory = block.Memory;
                bool success = memory.TryGetArray(out ArraySegment<int> arraySegment);
                Assert.True(success);
                Assert.Equal(block.Length, arraySegment.Count);
                unsafe
                {
                    void* pSpan = Unsafe.AsPointer(ref MemoryMarshal.GetReference(block.Span));
                    fixed (int* pArray = arraySegment.Array)
                    {
                        Assert.Equal((IntPtr)pSpan, (IntPtr)pArray);
                    }
                }
            }
        }

        [Fact]
        public static void RefCounting()
        {
            using (OwnedMemory<int> block = MemoryPool<int>.Shared.Rent(42))
            {
                block.Retain();
                block.Retain();
                block.Retain();

                bool moreToGo;
                moreToGo = block.Release();
                Assert.True(moreToGo);

                moreToGo = block.Release();
                Assert.True(moreToGo);

                moreToGo = block.Release();
                Assert.False(moreToGo);

                Assert.Throws<InvalidOperationException>(() => block.Release());
            }
        }

        [Fact]
        public static void IsDisposed()
        {
            OwnedMemory<int> block = MemoryPool<int>.Shared.Rent(42);
            Assert.False(block.IsDisposed);
            block.Dispose();
            Assert.True(block.IsDisposed);
            block.Dispose();
            Assert.True(block.IsDisposed);
        }

        [Fact]
        public static void ExtraDisposesAreIgnored()
        {
            OwnedMemory<int> block = MemoryPool<int>.Shared.Rent(42);
            block.Dispose();
            block.Dispose();
        }

        [Fact]
        public static void NoSpanAfterDispose()
        {
            OwnedMemory<int> block = MemoryPool<int>.Shared.Rent(42);
            block.Dispose();
            Assert.Throws<ObjectDisposedException>(() => block.Span.DontBox());
        }

        [Fact]
        public static void NoRetainAfterDispose()
        {
            OwnedMemory<int> block = MemoryPool<int>.Shared.Rent(42);
            block.Dispose();
            Assert.Throws<ObjectDisposedException>(() => block.Retain());
        }

        [Fact]
        public static void NoRelease_AfterDispose()
        {
            OwnedMemory<int> block = MemoryPool<int>.Shared.Rent(42);
            block.Dispose();
            Assert.Throws<ObjectDisposedException>(() => block.Release());
        }

        [Fact]
        public static void NoPinAfterDispose()
        {
            OwnedMemory<int> block = MemoryPool<int>.Shared.Rent(42);
            block.Dispose();
            Assert.Throws<ObjectDisposedException>(() => block.Pin());
        }

        [Fact]
        public static void NoTryGetArrayAfterDispose()
        {
            OwnedMemory<int> block = MemoryPool<int>.Shared.Rent(42);
            Memory<int> memory = block.Memory;
            block.Dispose();
            Assert.Throws<ObjectDisposedException>(() => memory.TryGetArray(out ArraySegment<int> arraySegment));
        }
    }
}

