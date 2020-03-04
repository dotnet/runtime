// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;
using Internal;
using Internal.Runtime.CompilerServices;
using static System.Runtime.Intrinsics.X86.Avx;
using static System.Runtime.Intrinsics.X86.Avx2;
using static System.Runtime.Intrinsics.X86.Popcnt;
using static System.Runtime.Intrinsics.X86.Popcnt.X64;

namespace System.Collections.Generic
{
    internal static class VectorizedSort
    {
        private static unsafe void Swap<TX>(TX *left, TX *right) where TX : unmanaged
        {
            var tmp = *left;
            *left  = *right;
            *right = tmp;
        }

        private static void Swap<TX>(Span<TX> span, int left, int right)
        {
            var tmp = span[left];
            span[left]  = span[right];
            span[right] = tmp;
        }

        private static unsafe void SwapIfGreater<TX>(TX *left, TX *right) where TX : unmanaged, IComparable<TX>
        {
            if ((*left).CompareTo(*right) <= 0) return;
            Swap(left, right);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static unsafe void InsertionSort<TX>(TX * left, TX * right) where TX : unmanaged, IComparable<TX>
        {
            for (var i = left; i < right; i++) {
                var j = i;
                var t = *(i + 1);
                while (j >= left && t.CompareTo(*j) < 0) {
                    *(j + 1) = *j;
                    j--;
                }
                *(j + 1) = t;
            }
        }

        private static void HeapSort<TX>(Span<TX> keys) where TX : unmanaged, IComparable<TX>
        {
            Debug.Assert(!keys.IsEmpty);

            var lo = 0;
            var hi = keys.Length - 1;

            var n = hi - lo + 1;
            for (var i = n / 2; i >= 1; i = i - 1)
            {
                DownHeap(keys, i, n, lo);
            }

            for (var i = n; i > 1; i--)
            {
                Swap(keys, lo, lo + i - 1);
                DownHeap(keys, 1, i - 1, lo);
            }
        }

        private static void DownHeap<TX>(Span<TX> keys, int i, int n, int lo) where TX : unmanaged, IComparable<TX>
        {
            Debug.Assert(lo >= 0);
            Debug.Assert(lo < keys.Length);

            var d = keys[lo + i - 1];
            while (i <= n / 2) {
                var child = 2 * i;
                if (child < n && keys[lo + child - 1].CompareTo(keys[lo + child]) < 0) {
                    child++;
                }

                if (keys[lo + child - 1].CompareTo(d) < 0)
                    break;

                keys[lo + i - 1] = keys[lo + child - 1];
                i                = child;
            }

            keys[lo + i - 1] = d;
        }

        // How much initial room needs to be made
        // during setup in full Vector25 units
        private const int SLACK_PER_SIDE_IN_VECTORS = 8;

        // Once we come out of the first unrolled loop
        // this will be the size of the second unrolled loop.
        private const int UNROLL2_SIZE_IN_VECTORS = 4;

        // Alignment in bytes
        private const ulong ALIGN = 32;
        private const ulong ALIGN_MASK = ALIGN - 1;

        internal unsafe ref struct VectorizedUnstableSortInt32
        {
            // We need this as a compile time constant
            private const int V256_N = 256 / 8 / sizeof(int);

            private const int SMALL_SORT_THRESHOLD_ELEMENTS = 112;
            private const int SLACK_PER_SIDE_IN_ELEMENTS = SLACK_PER_SIDE_IN_VECTORS * V256_N;
            private const int UNROLL2_SLACK_PER_SIDE_IN_ELEMENTS = UNROLL2_SIZE_IN_VECTORS  * V256_N;
            private const int EIGHTH_SLACK_PER_SIDE_IN_ELEMENTS = V256_N;

            // The formula goes like this:
            // 2 x the number of slack elements on each side +
            // 2 x amount of maximal bytes needed for alignment (32)
            // 8 more elements since we write with 8-way stores from both ends of the temporary area
            //   and we must make sure to accidentaly over-write from left -> right or vice-versa right on that edge...
            private const int PARTITION_TMP_SIZE_IN_ELEMENTS = (int) (2 * SLACK_PER_SIDE_IN_ELEMENTS + 2 * ALIGN / sizeof(int) + V256_N);

            private const long REALIGN_LEFT = 0x666;
            private const long REALIGN_RIGHT = 0x66600000000;
            internal const long REALIGN_BOTH = REALIGN_LEFT | REALIGN_RIGHT;
            private readonly int* _startPtr;
            private readonly int* _endPtr;
            private readonly int* _tempStart;
            private readonly int* _tempEnd;
#pragma warning disable 649
            private fixed int _temp[PARTITION_TMP_SIZE_IN_ELEMENTS];
            private int _depth;
#pragma warning restore 649
            private int _length;

            public VectorizedUnstableSortInt32(int* startPtr, int length) : this()
            {
                Debug.Assert(SMALL_SORT_THRESHOLD_ELEMENTS % V256_N == 0);

                _depth = 0;
                _startPtr = startPtr;
                _endPtr   = startPtr + length - 1;
                _length = length;
                fixed (int* pTemp = _temp) {
                    _tempStart = pTemp;
                    _tempEnd   = pTemp + PARTITION_TMP_SIZE_IN_ELEMENTS;
                }
            }


            [MethodImpl(MethodImplOptions.AggressiveOptimization)]
            internal void Sort()
            {
                // It makes no sense to sort arrays smaller than the max supported
                // bitonic sort with hybrid partitioning, so we special case those sized
                // and just copy the entire source to the tmp memory, pad it with
                // int.MaxValue and call BitonicSort
                if (_length <= BitonicSort<int>.MaxBitonicSortSize) {
                    CopyAndSortWithBitonic((uint) _length);
                    return;
                }

                var depthLimit = 2 * IntrospectiveSortUtilities.FloorLog2PlusOne(_length);
                HybridSort(_startPtr, _endPtr, REALIGN_BOTH, depthLimit);
            }

            private void CopyAndSortWithBitonic(uint cachedLength)
            {
                var start = _startPtr;
                var tmp = _tempStart;
                var byteCount = cachedLength * sizeof(int);

                var adjustedLength = cachedLength & ~0b111;
                Store(tmp + adjustedLength, Vector256.Create(int.MaxValue));
                Buffer.Memmove((byte*) tmp, (byte*) start, byteCount);
                BitonicSort<int>.Sort(tmp, (int) Math.Min(adjustedLength + 8, BitonicSort<int>.MaxBitonicSortSize));
                Buffer.Memmove((byte*) start, (byte*) tmp, byteCount);
            }

            [MethodImpl(MethodImplOptions.AggressiveOptimization)]
            internal void HybridSort(int* left, int* right, long realignHint, int depthLimit)
            {
                // In case of bad separation we might encounter a partition size of -1
                Debug.Assert(left <= right + 1);

                var length = (int) (right - left + 1);

                int* mid;
                switch (length) {

                    case -1:
                    case 0:
                    case 1:
                        return;
                    case 2:
                        SwapIfGreater(left, right);
                        return;
                    case 3:
                        mid = right - 1;
                        SwapIfGreater(left, mid);
                        SwapIfGreater(left, right);
                        SwapIfGreater(mid, right);
                        return;
                }

                _depth++;

                // SMALL_SORT_THRESHOLD_ELEMENTS is guaranteed (and asserted) to be a multiple of 8
                // So we can check if length is strictly smaller, knowing that we will round up to
                // SMALL_SORT_THRESHOLD_ELEMENTS exactly and no more
                // This is kind of critical given that we only limited # of implementation of
                // vectorized bitonic sort
                if (length < SMALL_SORT_THRESHOLD_ELEMENTS) {
                    var nextLength = (length & 7) > 0 ? (length + V256_N) & ~7: length;

                    Debug.Assert(nextLength <= BitonicSort<int>.MaxBitonicSortSize);
                    var extraSpaceNeeded = nextLength - length;
                    var fakeLeft = left - extraSpaceNeeded;
                    if (fakeLeft >= _startPtr) {
                        BitonicSort<int>.Sort(fakeLeft, nextLength);
                    }
                    else {
                        InsertionSort(left, right);
                    }
                    _depth--;
                    return;
                }

                // Detect a whole bunch of bad cases where partitioning
                // will not do well:
                // 1. Reverse sorted array
                // 2. High degree of repeated values (dutch flag problem, one value)
                if (depthLimit == 0)
                {
                    HeapSort(new Span<int>(left, (int) (right - left + 1)));
                    _depth--;
                    return;
                }
                depthLimit--;

                // This is going to be a bit weird:
                // Pre/Post alignment calculations happen here: we prepare hints to the
                // partition function of how much to align and in which direction (pre/post).
                // The motivation to do these calculations here and the actual alignment inside the partitioning code is
                // that here, we can cache those calculations.
                // As we recurse to the left we can reuse the left cached calculation, And when we recurse
                // to the right we reuse the right calculation, so we can avoid re-calculating the same aligned addresses
                // throughout the recursion, at the cost of a minor code complexity
                // Since we branch on the magi values REALIGN_LEFT & REALIGN_RIGHT its safe to assume
                // the we are not torturing the branch predictor.'

                // We use a long as a "struct" to pass on alignment hints to the partitioning
                // By packing 2 32 bit elements into it, as the JIT seem to not do this.
                // In reality  we need more like 2x 4bits for each side, but I don't think
                // there is a real difference'

                var preAlignedLeft = (int*)  ((ulong) left & ~ALIGN_MASK);
                var cannotPreAlignLeft = (preAlignedLeft - _startPtr) >> 63;
                var preAlignLeftOffset = (preAlignedLeft - left) + (V256_N & cannotPreAlignLeft);
                if ((realignHint & REALIGN_LEFT) != 0) {
                    // Alignment flow:
                    // * Calculate pre-alignment on the left
                    // * See it would cause us an out-of bounds read
                    // * Since we'd like to avoid that, we adjust for post-alignment
                    // * There are no branches since we do branch->arithmetic
                    realignHint &= unchecked((long) 0xFFFFFFFF00000000UL);
                    realignHint |= preAlignLeftOffset;
                }

                var preAlignedRight = (int*) (((ulong) right - 1 & ~ALIGN_MASK) + ALIGN);
                var cannotPreAlignRight = (_endPtr - preAlignedRight) >> 63;
                var preAlignRightOffset = (preAlignedRight - right - (V256_N & cannotPreAlignRight));
                if ((realignHint & REALIGN_RIGHT) != 0) {
                    // right is pointing just PAST the last element we intend to partition (where we also store the pivot)
                    // So we calculate alignment based on right - 1, and YES: I am casting to ulong before doing the -1, this
                    // is intentional since the whole thing is either aligned to 32 bytes or not, so decrementing the POINTER value
                    // by 1 is sufficient for the alignment, an the JIT sucks at this anyway
                    realignHint &= 0xFFFFFFFF;
                    realignHint |= preAlignRightOffset << 32;
                }

                Debug.Assert(((ulong) (left + (realignHint & 0xFFFFFFFF)) & ALIGN_MASK) == 0);
                Debug.Assert(((ulong) (right + (realignHint >> 32)) & ALIGN_MASK) == 0);

                // Compute median-of-three, of:
                // the first, mid and one before last elements
                mid = left + (right - left) / 2;
                SwapIfGreater(left, mid);
                SwapIfGreater(left, right - 1);
                SwapIfGreater(mid, right - 1);

                // Pivot is mid, place it in the right hand side
                Swap(mid, right);

                var sep = length < PARTITION_TMP_SIZE_IN_ELEMENTS ?
                    Partition1VectorInPlace(left, right, realignHint) :
                    Partition8VectorsInPlace(left, right, realignHint);

                HybridSort(left,    sep - 1, realignHint | REALIGN_RIGHT, depthLimit);
                HybridSort(sep + 1, right,   realignHint | REALIGN_LEFT,  depthLimit);
                _depth--;
            }

            /// <summary>
            /// Partition using Vectorized AVX2 intrinsics
            /// </summary>
            /// <param name="left">pointer (inclusive) to the first element to partition</param>
            /// <param name="right">pointer (exclusive) to the last element to partition, actually points to where the pivot before partitioning</param>
            /// <param name="hint">alignment instructions</param>
            /// <returns>Position of the pivot that was passed to the function inside the array</returns>
            [MethodImpl(MethodImplOptions.AggressiveOptimization)]
            private int* Partition8VectorsInPlace(int* left, int* right, long hint)
            {
                Debug.Assert(right - left >= SMALL_SORT_THRESHOLD_ELEMENTS, $"Not enough elements: {right-left} >= {SMALL_SORT_THRESHOLD_ELEMENTS}");

                Debug.Assert((((ulong) left) & 0x3) == 0);
                Debug.Assert((((ulong) right) & 0x3) == 0);
                // Vectorized double-pumped (dual-sided) partitioning:
                // We start with picking a pivot using the media-of-3 "method"
                // Once we have sensible pivot stored as the last element of the array
                // We process the array from both ends.
                //
                // To get this rolling, we first read 2 Vector256 elements from the left and
                // another 2 from the right, and store them in some temporary space
                // in order to leave enough "space" inside the vector for storing partitioned values.
                // Why 2 from each side? Because we need n+1 from each side
                // where n is the number of Vector256 elements we process in each iteration...
                // The reasoning behind the +1 is because of the way we decide from *which*
                // side to read, we may end up reading up to one more vector from any given side
                // and writing it in its entirety to the opposite side (this becomes slightly clearer
                // when reading the code below...)
                // Conceptually, the bulk of the processing looks like this after clearing out some initial
                // space as described above:

                // [.............................................................................]
                //  ^wl          ^rl                                               rr^        wr^
                // Where:
                // wl = writeLeft
                // rl = readLeft
                // rr = readRight
                // wr = writeRight

                // In every iteration, we select what side to read from based on how much
                // space is left between head read/write pointer on each side...
                // We read from where there is a smaller gap, e.g. that side
                // that is closer to the unfortunate possibility of its write head overwriting
                // its read head... By reading from THAT side, we're ensuring this does not happen

                // An additional unfortunate complexity we need to deal with is that the right pointer
                // must be decremented by another Vector256<T>.Count elements
                // Since the Load/Store primitives obviously accept start addresses
                var N = Vector256<int>.Count; // Treated as constant @ JIT time
                var pivot = *right;
                // We do this here just in case we need to pre-align to the right
                // We end up
                *right = int.MaxValue;

                var readLeft = left;
                var readRight = right;
                var writeLeft = left;
                var crappyWriteRight = right - N;

                var tmpStartLeft = _tempStart;
                var tmpLeft = tmpStartLeft;
                var tmpStartRight = _tempEnd;
                var tmpRight = tmpStartRight;

                // Broadcast the selected pivot
                var P = Vector256.Create(pivot);
                var pBase = PermTableFor32BitAlignedPtr;
                tmpRight -= N;

                #region Vector256 Alignment
                // the read heads always advance by 8 elements, or 32 bytes,
                // We can spend some extra time here to align the pointers
                // so they start at a cache-line boundary
                // Once that happens, we can read with Avx.LoadAlignedVector256
                // And also know for sure that our reads will never cross cache-lines
                // Otherwise, 50% of our AVX2 Loads will need to read from two cache-lines
                var leftAlign = unchecked((int) (hint & 0xFFFFFFFF));
                var rightAlign = unchecked((int) (hint >> 32));

                var preAlignedLeft = left + leftAlign;
                var preAlignedRight = right + rightAlign - N;

                // We preemptively go through the motions of
                // vectorized alignment, and at worst we re-neg
                // by not advancing the various read/tmp pointers
                // as if nothing ever happenned if the conditions
                // are wrong from vectorized alginment
                var RT0 = LoadAlignedVector256(preAlignedRight);
                var LT0 = LoadAlignedVector256(preAlignedLeft);
                var rtMask = (uint) MoveMask(CompareGreaterThan(RT0, P).AsSingle());
                var ltMask = (uint) MoveMask(CompareGreaterThan(LT0, P).AsSingle());
                var rtPopCount = Math.Max(PopCount(rtMask), (uint) rightAlign);
                var ltPopCount = PopCount(ltMask);
                RT0 = PermuteVar8x32(RT0, GetBytePermutationAligned(pBase, rtMask));
                LT0 = PermuteVar8x32(LT0, GetBytePermutationAligned(pBase, ltMask));
                Store(tmpRight, RT0);
                Store(tmpLeft, LT0);

                var rightAlignMask = ~((rightAlign - 1) >> 31);
                var leftAlignMask = leftAlign >> 31;

                tmpRight -= rtPopCount & rightAlignMask;
                rtPopCount = V256_N - rtPopCount;
                readRight += (rightAlign - N) & rightAlignMask;

                Store(tmpRight, LT0);
                tmpRight -= ltPopCount & leftAlignMask;
                ltPopCount =  V256_N - ltPopCount;
                tmpLeft += ltPopCount & leftAlignMask;
                tmpStartLeft += -leftAlign & leftAlignMask;
                readLeft += (leftAlign + N) & leftAlignMask;

                Store(tmpLeft,  RT0);
                tmpLeft       += rtPopCount & rightAlignMask;
                tmpStartRight -= rightAlign & rightAlignMask;

                if (leftAlign > 0) {
                    tmpRight += N;
                    readLeft = AlignLeftScalarUncommon(readLeft, pivot, ref tmpLeft, ref tmpRight);
                    tmpRight -= N;
                }

                if (rightAlign < 0) {
                    tmpRight += N;
                    readRight = AlignRightScalarUncommon(readRight, pivot, ref tmpLeft, ref tmpRight);
                    tmpRight -= N;
                }
                Debug.Assert(((ulong) readLeft & ALIGN_MASK) == 0);
                Debug.Assert(((ulong) readRight & ALIGN_MASK) == 0);

                Debug.Assert((((byte *) readRight - (byte *) readLeft) % (long) ALIGN) == 0);
                Debug.Assert((readRight -  readLeft) >= SLACK_PER_SIDE_IN_ELEMENTS * 2);

                #endregion

                // Make 8 vectors worth of space on each side by partitioning them straight into the temporary memory
                LoadAndPartition8Vectors(readLeft, P, pBase, ref tmpLeft, ref tmpRight);
                LoadAndPartition8Vectors(readRight - SLACK_PER_SIDE_IN_ELEMENTS, P, pBase, ref tmpLeft, ref tmpRight);
                tmpRight += N;

                // Adjust for the reading that was made above
                readLeft  += SLACK_PER_SIDE_IN_ELEMENTS;
                readRight -= SLACK_PER_SIDE_IN_ELEMENTS * 2;

                var writeRight = crappyWriteRight;

                while (readLeft < readRight) {
                    int* nextPtr;
                    if ((byte *) writeRight - (byte *) readRight  < (2*SLACK_PER_SIDE_IN_ELEMENTS - N)*sizeof(int)) {
                        nextPtr   =  readRight;
                        readRight -= SLACK_PER_SIDE_IN_ELEMENTS;
                    } else {
                        nextPtr  =  readLeft;
                        readLeft += SLACK_PER_SIDE_IN_ELEMENTS;
                    }

                    LoadAndPartition8Vectors(nextPtr, P, pBase, ref writeLeft, ref writeRight);
                }

                readRight += UNROLL2_SLACK_PER_SIDE_IN_ELEMENTS;

                while (readLeft < readRight) {
                    int* nextPtr;
                    if ((byte *) writeRight - (byte *) readRight  < (2*UNROLL2_SLACK_PER_SIDE_IN_ELEMENTS - N) * sizeof(int)) {
                        nextPtr   =  readRight;
                        readRight -= UNROLL2_SLACK_PER_SIDE_IN_ELEMENTS;
                    } else {
                        nextPtr  =  readLeft;
                        readLeft += UNROLL2_SLACK_PER_SIDE_IN_ELEMENTS;
                    }

                    Debug.Assert(readLeft - writeLeft >= UNROLL2_SLACK_PER_SIDE_IN_ELEMENTS,   $"left head overwrite {readLeft - writeLeft}");
                    Debug.Assert(writeRight - readRight >= UNROLL2_SLACK_PER_SIDE_IN_ELEMENTS, $"right head overwrite {writeRight - readRight}");

                    LoadAndPartition4Vectors(nextPtr, P, pBase, ref writeLeft, ref writeRight);
                }

                readRight += UNROLL2_SLACK_PER_SIDE_IN_ELEMENTS - N;

                while (readLeft <= readRight) {
                    int* nextPtr;
                    if (((byte *) writeRight - (byte *) readRight) < N * sizeof(int)) {
                        nextPtr   =  readRight;
                        readRight -= N;
                    } else {
                        nextPtr  =  readLeft;
                        readLeft += N;
                    }

                    PartitionBlock1V(LoadAlignedVector256(nextPtr), P, pBase, ref writeLeft, ref writeRight);
                }

                var boundary = writeLeft;

                // 3. Copy-back the 4 registers + remainder we partitioned in the beginning
                var leftTmpSize = (uint) (ulong) (tmpLeft - tmpStartLeft);
                Buffer.Memmove((byte*) boundary, (byte*) tmpStartLeft, leftTmpSize*sizeof(int));
                boundary += leftTmpSize;
                var rightTmpSize = (uint) (ulong) (tmpStartRight - tmpRight);
                Buffer.Memmove((byte*) boundary, (byte*) tmpRight, rightTmpSize*sizeof(int));

                // Shove to pivot back to the boundary
                var value = *boundary;
                *right = value;
                *boundary = pivot;

                Debug.Assert(boundary > left);
                Debug.Assert(boundary <= right);

                return boundary;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining|MethodImplOptions.AggressiveOptimization)]
            private static void LoadAndPartition8Vectors(int* dataPtr, Vector256<int> P, byte* pBase, ref int* writeLeftPtr, ref int* writeRightPtr)
            {
                var N = Vector256<int>.Count; // Treated as constant @ JIT time

                var L0 = LoadAlignedVector256(dataPtr + 0 * N);
                var L1 = LoadAlignedVector256(dataPtr + 1 * N);
                var L2 = LoadAlignedVector256(dataPtr + 2 * N);
                var L3 = LoadAlignedVector256(dataPtr + 3 * N);
                var L4 = LoadAlignedVector256(dataPtr + 4 * N);
                var L5 = LoadAlignedVector256(dataPtr + 5 * N);
                var L6 = LoadAlignedVector256(dataPtr + 6 * N);
                var L7 = LoadAlignedVector256(dataPtr + 7 * N);
                PartitionBlock4V(P, L0, L1, L2, L3, pBase, ref writeLeftPtr, ref writeRightPtr);
                PartitionBlock4V(P, L4, L5, L6, L7, pBase, ref writeLeftPtr, ref writeRightPtr);
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining|MethodImplOptions.AggressiveOptimization)]
            private static void LoadAndPartition4Vectors(int* dataPtr, Vector256<int> P, byte* pBase, ref int* writeLeft, ref int* writeRight)
            {
                var N = Vector256<int>.Count; // Treated as constant @ JIT time

                var L0 = LoadAlignedVector256(dataPtr + 0 * N);
                var L1 = LoadAlignedVector256(dataPtr + 1 * N);
                var L2 = LoadAlignedVector256(dataPtr + 2 * N);
                var L3 = LoadAlignedVector256(dataPtr + 3 * N);
                PartitionBlock4V(P, L0, L1, L2, L3, pBase, ref writeLeft, ref writeRight);
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining|MethodImplOptions.AggressiveOptimization)]
            private static void PartitionBlock4V(Vector256<int> P,  Vector256<int> L0, Vector256<int> L1, Vector256<int> L2,
                Vector256<int>                          L3, byte*          pBase,
                ref int*                                writeLeft,
                ref int*                                writeRight)
            {
                PartitionBlock1V(L0, P, pBase, ref writeLeft, ref writeRight);
                PartitionBlock1V(L1, P, pBase, ref writeLeft, ref writeRight);
                PartitionBlock1V(L2, P, pBase, ref writeLeft, ref writeRight);
                PartitionBlock1V(L3, P, pBase, ref writeLeft, ref writeRight);
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining|MethodImplOptions.AggressiveOptimization)]
            private static void PartitionBlock1V(Vector256<int> L0, Vector256<int> P, byte* pBase, ref int* writeLeft, ref int* writeRight)
            {
                // Looks kinda silly, the (ulong) (uint) thingy right?
                // Well, it's making a yucky lemonade out of lemons is what it is.
                // This is a crappy way of making the jit generate slightly less worse code
                // due to: https://github.com/dotnet/runtime/issues/431#issuecomment-568280829
                // To summarize: VMOVMASK is mis-understood as a 32-bit write by the CoreCLR 3.x JIT.
                // It's really a 64 bit write in 64 bit mode, in other words, it clears the entire register.
                // Again, the JIT *should* be aware that the destination register just had it's top 32 bits cleared.
                // It doesn't.
                // This causes a variety of issues, here it's that GetBytePermutation* method is generated
                // with suboptimal x86 code (see above issue/comment).
                // By forcefully clearing the 32-top bits by casting to ulong, we "help" the JIT further down the road
                // and the rest of the code is generated more cleanly.
                // In other words, until the issue is resolved we "pay" with a 2-byte instruction for this useless cast
                // But this helps the JIT generate slightly better code below (saving 3 bytes).
                var m0 = (ulong) (uint) MoveMask(CompareGreaterThan(L0, P).AsSingle());
                L0 = PermuteVar8x32(L0, GetBytePermutationAligned(pBase, m0));
                // We make sure the last use of m0 is for this PopCount operation. Why?
                // Again, this is to get the best code generated on an Intel CPU. This round it's intel's fault, yay.
                // There's a relatively well know CPU errata where POPCNT has a false dependency on the destination operand.
                // The JIT is already aware of this, so it will clear the destination operand before emitting a POPCNT:
                // https://github.com/dotnet/coreclr/issues/19555
                // By "delaying" the PopCount to this stage, it is highly likely (I don't know why, I just know it is...)
                // that the JIT will emit a POPCNT X,X instruction, where X is now both the source and the destination
                // for PopCount. This means that there is no need for clearing the destination register (it would even be
                // an error to do so). This saves about two bytes in the instruction stream.
                var pc = -((long) (int) PopCount(m0));
                Store(writeLeft,  L0);
                Store(writeRight, L0);
                // I comfortably ignored having negated the PopCount result after casting to (long)
                // The reasoning behind this is that be storing the PopCount as a negative
                // while also expressing the pointer bumping (next two lines) in this very specific form that
                // it is expressed: a summation of two variables with an optional constant (that CAN be negative)
                // We are allowing the JIT to encode this as two LEA opcodes in x64: https://www.felixcloutier.com/x86/lea
                // This saves a considerable amount of space in the instruction stream, which are then exploded
                // when this block is unrolled. All in all this is has a very clear benefit in perf while decreasing code
                // size.
                // TODO: Currently the entire sorting operation generates a right-hand popcount that needs to be negated
                //       If/When I re-write it to do left-hand comparison/pop-counting we can save another two bytes
                //       for the negation operation, which will also do its share to speed things up while lowering
                //       the native code size, yay for future me!
                writeRight = writeRight + pc;
                writeLeft  = writeLeft + pc + V256_N;
            }

            /// <summary>
            /// Partition using Vectorized AVX2 intrinsics
            /// </summary>
            /// <param name="left">pointer (inclusive) to the first element to partition</param>
            /// <param name="right">pointer (exclusive) to the last element to partition, actually points to where the pivot before partitioning</param>
            /// <param name="hint">alignment instructions</param>
            /// <returns>Position of the pivot that was passed to the function inside the array</returns>
            [MethodImpl(MethodImplOptions.AggressiveOptimization)]
            private int* Partition1VectorInPlace(int* left, int* right, long hint)
            {
                Debug.Assert((((ulong) left) & 0x3) == 0);
                Debug.Assert((((ulong) right) & 0x3) == 0);
                // Vectorized double-pumped (dual-sided) partitioning:
                // We start with picking a pivot using the media-of-3 "method"
                // Once we have sensible pivot stored as the last element of the array
                // We process the array from both ends.
                //
                // To get this rolling, we first read 2 Vector256 elements from the left and
                // another 2 from the right, and store them in some temporary space
                // in order to leave enough "space" inside the vector for storing partitioned values.
                // Why 2 from each side? Because we need n+1 from each side
                // where n is the number of Vector256 elements we process in each iteration...
                // The reasoning behind the +1 is because of the way we decide from *which*
                // side to read, we may end up reading up to one more vector from any given side
                // and writing it in its entirety to the opposite side (this becomes slightly clearer
                // when reading the code below...)
                // Conceptually, the bulk of the processing looks like this after clearing out some initial
                // space as described above:

                // [.............................................................................]
                //  ^wl          ^rl                                               rr^        wr^
                // Where:
                // wl = writeLeft
                // rl = readLeft
                // rr = readRight
                // wr = writeRight

                // In every iteration, we select what side to read from based on how much
                // space is left between head read/write pointer on each side...
                // We read from where there is a smaller gap, e.g. that side
                // that is closer to the unfortunate possibility of its write head overwriting
                // its read head... By reading from THAT side, we're ensuring this does not happen

                // An additional unfortunate complexity we need to deal with is that the right pointer
                // must be decremented by another Vector256<T>.Count elements
                // Since the Load/Store primitives obviously accept start addresses
                var N = Vector256<int>.Count; // Treated as constant @ JIT time
                var pivot = *right;
                // We do this here just in case we need to pre-align to the right
                // We end up
                *right = int.MaxValue;

                var readLeft = left;
                var readRight = right;
                var writeLeft = readLeft;
                var writeRight = readRight - N;

                var tmpStartLeft = _tempStart;
                var tmpLeft = tmpStartLeft;
                var tmpStartRight = _tempEnd;
                var tmpRight = tmpStartRight;

                // Broadcast the selected pivot
                var P = Vector256.Create(pivot);
                var pBase = PermTableFor32BitAlignedPtr;
                tmpRight -= N;

                // the read heads always advance by 8 elements, or 32 bytes,
                // We can spend some extra time here to align the pointers
                // so they start at a cache-line boundary
                // Once that happens, we can read with Avx.LoadAlignedVector256
                // And also know for sure that our reads will never cross cache-lines
                // Otherwise, 50% of our AVX2 Loads will need to read from two cache-lines

                var leftAlign = unchecked((int) (hint & 0xFFFFFFFF));
                var rightAlign = unchecked((int) (hint >> 32));

                var preAlignedLeft = left + leftAlign;
                var preAlignedRight = right + rightAlign - N;

                // We preemptively go through the motions of
                // vectorized alignment, and at worst we re-neg
                // by not advancing the various read/tmp pointers
                // as if nothing ever happened if the conditions
                // are wrong from vectorized alignment
                var RT0 = LoadAlignedVector256(preAlignedRight);
                var LT0 = LoadAlignedVector256(preAlignedLeft);
                var rtMask = (uint) MoveMask(CompareGreaterThan(RT0, P).AsSingle());
                var ltMask = (uint) MoveMask(CompareGreaterThan(LT0, P).AsSingle());
                var rtPopCount = Math.Max(PopCount(rtMask), (uint) rightAlign);
                var ltPopCount = PopCount(ltMask);
                RT0 = PermuteVar8x32(RT0, GetBytePermutationAligned(pBase, rtMask));
                LT0 = PermuteVar8x32(LT0, GetBytePermutationAligned(pBase, ltMask));
                Avx.Store(tmpRight, RT0);
                Avx.Store(tmpLeft, LT0);

                var rightAlignMask = ~((rightAlign - 1) >> 31);
                var leftAlignMask = leftAlign >> 31;

                tmpRight -= rtPopCount & rightAlignMask;
                rtPopCount = V256_N - rtPopCount;
                readRight += (rightAlign - N) & rightAlignMask;

                Avx.Store(tmpRight, LT0);
                tmpRight -= ltPopCount & leftAlignMask;
                ltPopCount =  V256_N - ltPopCount;
                tmpLeft += ltPopCount & leftAlignMask;
                tmpStartLeft += -leftAlign & leftAlignMask;
                readLeft += (leftAlign + N) & leftAlignMask;

                Avx.Store(tmpLeft,  RT0);
                tmpLeft       += rtPopCount & rightAlignMask;
                tmpStartRight -= rightAlign & rightAlignMask;

                if (leftAlign > 0) {
                    tmpRight += N;
                    readLeft = AlignLeftScalarUncommon(readLeft, pivot, ref tmpLeft, ref tmpRight);
                    tmpRight -= N;
                }

                if (rightAlign < 0) {
                    tmpRight += N;
                    readRight = AlignRightScalarUncommon(readRight, pivot, ref tmpLeft, ref tmpRight);
                    tmpRight -= N;
                }
                Debug.Assert(((ulong) readLeft & ALIGN_MASK) == 0);
                Debug.Assert(((ulong) readRight & ALIGN_MASK) == 0);

                Debug.Assert((((byte *) readRight - (byte *) readLeft) % (long) ALIGN) == 0);
                Debug.Assert((readRight -  readLeft) >= EIGHTH_SLACK_PER_SIDE_IN_ELEMENTS * 2);

                // Read ahead from left+right
                LT0 = LoadAlignedVector256(readLeft  + 0*N);
                RT0 = LoadAlignedVector256(readRight - 1*N);

                // Adjust for the reading that was made above
                readLeft  += 1*N;
                readRight -= 2*N;

                ltMask = (uint) MoveMask(CompareGreaterThan(LT0, P).AsSingle());
                rtMask = (uint) MoveMask(CompareGreaterThan(RT0, P).AsSingle());

                ltPopCount = PopCount(ltMask);
                rtPopCount = PopCount(rtMask);

                LT0 = PermuteVar8x32(LT0, GetBytePermutationAligned(pBase, ltMask));
                RT0 = PermuteVar8x32(RT0, GetBytePermutationAligned(pBase, rtMask));

                Store(tmpRight, LT0);
                tmpRight -= ltPopCount;
                ltPopCount = V256_N - ltPopCount;
                Store(tmpRight, RT0);
                tmpRight -= rtPopCount;
                rtPopCount = V256_N - rtPopCount;
                tmpRight += N;

                Store(tmpLeft, LT0);
                tmpLeft += ltPopCount;
                Store(tmpLeft, RT0);
                tmpLeft += rtPopCount;

                while (readRight >= readLeft) {

                    int* nextPtr;
                    if (((byte *) writeRight - (byte *) readRight) < N * sizeof(int)) {
                        nextPtr   =  readRight;
                        readRight -= N;
                    } else {
                        nextPtr  =  readLeft;
                        readLeft += N;
                    }

                    PartitionBlock1V(LoadAlignedVector256(nextPtr), P, pBase, ref writeLeft, ref writeRight);
                }

                var boundary = writeLeft;

                // 3. Copy-back the 4 registers + remainder we partitioned in the beginning
                var leftTmpSize = (uint) (ulong) (tmpLeft - tmpStartLeft);
                Buffer.Memmove((byte*) boundary, (byte*) tmpStartLeft, leftTmpSize*sizeof(int));
                boundary += leftTmpSize;
                var rightTmpSize = (uint) (ulong)  (tmpStartRight - tmpRight);
                Buffer.Memmove((byte*) boundary, (byte*) tmpRight, rightTmpSize*sizeof(int));

                // Shove to pivot back to the boundary
                var value = *boundary;
                *right = value;
                *boundary = pivot;

                Debug.Assert(boundary > left);
                Debug.Assert(boundary <= right);

                return boundary;
            }

            /// <summary>
            /// Called when the left hand side of the entire array does not have enough elements
            /// for us to align the memory with vectorized operations, so we do this uncommon slower alternative.
            /// Generally speaking this is probably called for all the partitioning calls on the left edge of the array
            /// </summary>
            /// <param name="readLeft"></param>
            /// <param name="pivot"></param>
            /// <param name="tmpLeft"></param>
            /// <param name="tmpRight"></param>
            /// <returns></returns>
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            private static int* AlignLeftScalarUncommon(int* readLeft, int pivot, ref int* tmpLeft, ref int* tmpRight)
            {
                if (((ulong) readLeft & ALIGN_MASK) == 0)
                    return readLeft;

                var nextAlign = (int*) (((ulong) readLeft + ALIGN) & ~ALIGN_MASK);
                while (readLeft < nextAlign) {
                    var v = *readLeft++;
                    if (v <= pivot) {
                        *tmpLeft++ = v;
                    } else {
                        *--tmpRight = v;
                    }
                }

                return readLeft;
            }

            /// <summary>
            /// Called when the right hand side of the entire array does not have enough elements
            /// for us to align the memory with vectorized operations, so we do this uncommon slower alternative.
            /// Generally speaking this is probably called for all the partitioning calls on the right edge of the array
            /// </summary>
            /// <param name="readRight"></param>
            /// <param name="pivot"></param>
            /// <param name="tmpLeft"></param>
            /// <param name="tmpRight"></param>
            /// <returns></returns>
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            private static int* AlignRightScalarUncommon(int* readRight, int pivot, ref int* tmpLeft, ref int* tmpRight)
            {
                if (((ulong) readRight & ALIGN_MASK) == 0)
                    return readRight;

                var nextAlign = (int*) ((ulong) readRight & ~ALIGN_MASK);
                while (readRight > nextAlign) {
                    var v = *--readRight;
                    if (v <= pivot) {
                        *tmpLeft++ = v;
                    } else {
                        *--tmpRight = v;
                    }
                }

                return readRight;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static unsafe Vector256<int> GetBytePermutationAligned(byte * pBase, uint index)
        {
            Debug.Assert(index <= 255);
            Debug.Assert(pBase != null);
            Debug.Assert(((ulong) (pBase + index * 8)) % 8 == 0);
            return Avx2.ConvertToVector256Int32(pBase + index * 8);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static unsafe Vector256<int> GetBytePermutationAligned(byte * pBase, ulong index)
        {
            Debug.Assert(index <= 255);
            Debug.Assert(pBase != null);
            Debug.Assert(((ulong) (pBase + index * 8)) % 8 == 0);
            return Avx2.ConvertToVector256Int32(pBase + index * 8);
        }

        private static readonly unsafe byte* PermTableFor32BitAlignedPtr = (byte*) AlignSpan(PermTableFor32Bit, PAGE_SIZE);

        private const uint PAGE_SIZE = 4096U;

        private static unsafe void * AlignSpan(ReadOnlySpan<byte> unalignedSpan, ulong alignment)
        {
            var alignedPtr = (byte*) Marshal.AllocHGlobal(unalignedSpan.Length + (int) alignment);
            var x = alignedPtr;
            if (((ulong) alignedPtr) % alignment != 0)
                alignedPtr = (byte *) (((ulong) alignedPtr + alignment) & ~(alignment - 1));

            Debug.Assert((ulong) alignedPtr % alignment == 0);
            unalignedSpan.CopyTo(new Span<byte>(alignedPtr, unalignedSpan.Length));
            return alignedPtr;
        }

        internal static ReadOnlySpan<byte> PermTableFor32Bit => new byte[] {
            64, 1, 2, 3, 4, 5, 6, 7, // 0b00000000 (0)|Left-PC: 8
            57, 2, 3, 4, 5, 6, 7, 0, // 0b00000001 (1)|Left-PC: 7
            56, 2, 3, 4, 5, 6, 7, 1, // 0b00000010 (2)|Left-PC: 7
            50, 3, 4, 5, 6, 7, 0, 1, // 0b00000011 (3)|Left-PC: 6
            56, 1, 3, 4, 5, 6, 7, 2, // 0b00000100 (4)|Left-PC: 7
            49, 3, 4, 5, 6, 7, 0, 2, // 0b00000101 (5)|Left-PC: 6
            48, 3, 4, 5, 6, 7, 1, 2, // 0b00000110 (6)|Left-PC: 6
            43, 4, 5, 6, 7, 0, 1, 2, // 0b00000111 (7)|Left-PC: 5
            56, 1, 2, 4, 5, 6, 7, 3, // 0b00001000 (8)|Left-PC: 7
            49, 2, 4, 5, 6, 7, 0, 3, // 0b00001001 (9)|Left-PC: 6
            48, 2, 4, 5, 6, 7, 1, 3, // 0b00001010 (10)|Left-PC: 6
            42, 4, 5, 6, 7, 0, 1, 3, // 0b00001011 (11)|Left-PC: 5
            48, 1, 4, 5, 6, 7, 2, 3, // 0b00001100 (12)|Left-PC: 6
            41, 4, 5, 6, 7, 0, 2, 3, // 0b00001101 (13)|Left-PC: 5
            40, 4, 5, 6, 7, 1, 2, 3, // 0b00001110 (14)|Left-PC: 5
            36, 5, 6, 7, 0, 1, 2, 3, // 0b00001111 (15)|Left-PC: 4
            56, 1, 2, 3, 5, 6, 7, 4, // 0b00010000 (16)|Left-PC: 7
            49, 2, 3, 5, 6, 7, 0, 4, // 0b00010001 (17)|Left-PC: 6
            48, 2, 3, 5, 6, 7, 1, 4, // 0b00010010 (18)|Left-PC: 6
            42, 3, 5, 6, 7, 0, 1, 4, // 0b00010011 (19)|Left-PC: 5
            48, 1, 3, 5, 6, 7, 2, 4, // 0b00010100 (20)|Left-PC: 6
            41, 3, 5, 6, 7, 0, 2, 4, // 0b00010101 (21)|Left-PC: 5
            40, 3, 5, 6, 7, 1, 2, 4, // 0b00010110 (22)|Left-PC: 5
            35, 5, 6, 7, 0, 1, 2, 4, // 0b00010111 (23)|Left-PC: 4
            48, 1, 2, 5, 6, 7, 3, 4, // 0b00011000 (24)|Left-PC: 6
            41, 2, 5, 6, 7, 0, 3, 4, // 0b00011001 (25)|Left-PC: 5
            40, 2, 5, 6, 7, 1, 3, 4, // 0b00011010 (26)|Left-PC: 5
            34, 5, 6, 7, 0, 1, 3, 4, // 0b00011011 (27)|Left-PC: 4
            40, 1, 5, 6, 7, 2, 3, 4, // 0b00011100 (28)|Left-PC: 5
            33, 5, 6, 7, 0, 2, 3, 4, // 0b00011101 (29)|Left-PC: 4
            32, 5, 6, 7, 1, 2, 3, 4, // 0b00011110 (30)|Left-PC: 4
            29, 6, 7, 0, 1, 2, 3, 4, // 0b00011111 (31)|Left-PC: 3
            56, 1, 2, 3, 4, 6, 7, 5, // 0b00100000 (32)|Left-PC: 7
            49, 2, 3, 4, 6, 7, 0, 5, // 0b00100001 (33)|Left-PC: 6
            48, 2, 3, 4, 6, 7, 1, 5, // 0b00100010 (34)|Left-PC: 6
            42, 3, 4, 6, 7, 0, 1, 5, // 0b00100011 (35)|Left-PC: 5
            48, 1, 3, 4, 6, 7, 2, 5, // 0b00100100 (36)|Left-PC: 6
            41, 3, 4, 6, 7, 0, 2, 5, // 0b00100101 (37)|Left-PC: 5
            40, 3, 4, 6, 7, 1, 2, 5, // 0b00100110 (38)|Left-PC: 5
            35, 4, 6, 7, 0, 1, 2, 5, // 0b00100111 (39)|Left-PC: 4
            48, 1, 2, 4, 6, 7, 3, 5, // 0b00101000 (40)|Left-PC: 6
            41, 2, 4, 6, 7, 0, 3, 5, // 0b00101001 (41)|Left-PC: 5
            40, 2, 4, 6, 7, 1, 3, 5, // 0b00101010 (42)|Left-PC: 5
            34, 4, 6, 7, 0, 1, 3, 5, // 0b00101011 (43)|Left-PC: 4
            40, 1, 4, 6, 7, 2, 3, 5, // 0b00101100 (44)|Left-PC: 5
            33, 4, 6, 7, 0, 2, 3, 5, // 0b00101101 (45)|Left-PC: 4
            32, 4, 6, 7, 1, 2, 3, 5, // 0b00101110 (46)|Left-PC: 4
            28, 6, 7, 0, 1, 2, 3, 5, // 0b00101111 (47)|Left-PC: 3
            48, 1, 2, 3, 6, 7, 4, 5, // 0b00110000 (48)|Left-PC: 6
            41, 2, 3, 6, 7, 0, 4, 5, // 0b00110001 (49)|Left-PC: 5
            40, 2, 3, 6, 7, 1, 4, 5, // 0b00110010 (50)|Left-PC: 5
            34, 3, 6, 7, 0, 1, 4, 5, // 0b00110011 (51)|Left-PC: 4
            40, 1, 3, 6, 7, 2, 4, 5, // 0b00110100 (52)|Left-PC: 5
            33, 3, 6, 7, 0, 2, 4, 5, // 0b00110101 (53)|Left-PC: 4
            32, 3, 6, 7, 1, 2, 4, 5, // 0b00110110 (54)|Left-PC: 4
            27, 6, 7, 0, 1, 2, 4, 5, // 0b00110111 (55)|Left-PC: 3
            40, 1, 2, 6, 7, 3, 4, 5, // 0b00111000 (56)|Left-PC: 5
            33, 2, 6, 7, 0, 3, 4, 5, // 0b00111001 (57)|Left-PC: 4
            32, 2, 6, 7, 1, 3, 4, 5, // 0b00111010 (58)|Left-PC: 4
            26, 6, 7, 0, 1, 3, 4, 5, // 0b00111011 (59)|Left-PC: 3
            32, 1, 6, 7, 2, 3, 4, 5, // 0b00111100 (60)|Left-PC: 4
            25, 6, 7, 0, 2, 3, 4, 5, // 0b00111101 (61)|Left-PC: 3
            24, 6, 7, 1, 2, 3, 4, 5, // 0b00111110 (62)|Left-PC: 3
            22, 7, 0, 1, 2, 3, 4, 5, // 0b00111111 (63)|Left-PC: 2
            56, 1, 2, 3, 4, 5, 7, 6, // 0b01000000 (64)|Left-PC: 7
            49, 2, 3, 4, 5, 7, 0, 6, // 0b01000001 (65)|Left-PC: 6
            48, 2, 3, 4, 5, 7, 1, 6, // 0b01000010 (66)|Left-PC: 6
            42, 3, 4, 5, 7, 0, 1, 6, // 0b01000011 (67)|Left-PC: 5
            48, 1, 3, 4, 5, 7, 2, 6, // 0b01000100 (68)|Left-PC: 6
            41, 3, 4, 5, 7, 0, 2, 6, // 0b01000101 (69)|Left-PC: 5
            40, 3, 4, 5, 7, 1, 2, 6, // 0b01000110 (70)|Left-PC: 5
            35, 4, 5, 7, 0, 1, 2, 6, // 0b01000111 (71)|Left-PC: 4
            48, 1, 2, 4, 5, 7, 3, 6, // 0b01001000 (72)|Left-PC: 6
            41, 2, 4, 5, 7, 0, 3, 6, // 0b01001001 (73)|Left-PC: 5
            40, 2, 4, 5, 7, 1, 3, 6, // 0b01001010 (74)|Left-PC: 5
            34, 4, 5, 7, 0, 1, 3, 6, // 0b01001011 (75)|Left-PC: 4
            40, 1, 4, 5, 7, 2, 3, 6, // 0b01001100 (76)|Left-PC: 5
            33, 4, 5, 7, 0, 2, 3, 6, // 0b01001101 (77)|Left-PC: 4
            32, 4, 5, 7, 1, 2, 3, 6, // 0b01001110 (78)|Left-PC: 4
            28, 5, 7, 0, 1, 2, 3, 6, // 0b01001111 (79)|Left-PC: 3
            48, 1, 2, 3, 5, 7, 4, 6, // 0b01010000 (80)|Left-PC: 6
            41, 2, 3, 5, 7, 0, 4, 6, // 0b01010001 (81)|Left-PC: 5
            40, 2, 3, 5, 7, 1, 4, 6, // 0b01010010 (82)|Left-PC: 5
            34, 3, 5, 7, 0, 1, 4, 6, // 0b01010011 (83)|Left-PC: 4
            40, 1, 3, 5, 7, 2, 4, 6, // 0b01010100 (84)|Left-PC: 5
            33, 3, 5, 7, 0, 2, 4, 6, // 0b01010101 (85)|Left-PC: 4
            32, 3, 5, 7, 1, 2, 4, 6, // 0b01010110 (86)|Left-PC: 4
            27, 5, 7, 0, 1, 2, 4, 6, // 0b01010111 (87)|Left-PC: 3
            40, 1, 2, 5, 7, 3, 4, 6, // 0b01011000 (88)|Left-PC: 5
            33, 2, 5, 7, 0, 3, 4, 6, // 0b01011001 (89)|Left-PC: 4
            32, 2, 5, 7, 1, 3, 4, 6, // 0b01011010 (90)|Left-PC: 4
            26, 5, 7, 0, 1, 3, 4, 6, // 0b01011011 (91)|Left-PC: 3
            32, 1, 5, 7, 2, 3, 4, 6, // 0b01011100 (92)|Left-PC: 4
            25, 5, 7, 0, 2, 3, 4, 6, // 0b01011101 (93)|Left-PC: 3
            24, 5, 7, 1, 2, 3, 4, 6, // 0b01011110 (94)|Left-PC: 3
            21, 7, 0, 1, 2, 3, 4, 6, // 0b01011111 (95)|Left-PC: 2
            48, 1, 2, 3, 4, 7, 5, 6, // 0b01100000 (96)|Left-PC: 6
            41, 2, 3, 4, 7, 0, 5, 6, // 0b01100001 (97)|Left-PC: 5
            40, 2, 3, 4, 7, 1, 5, 6, // 0b01100010 (98)|Left-PC: 5
            34, 3, 4, 7, 0, 1, 5, 6, // 0b01100011 (99)|Left-PC: 4
            40, 1, 3, 4, 7, 2, 5, 6, // 0b01100100 (100)|Left-PC: 5
            33, 3, 4, 7, 0, 2, 5, 6, // 0b01100101 (101)|Left-PC: 4
            32, 3, 4, 7, 1, 2, 5, 6, // 0b01100110 (102)|Left-PC: 4
            27, 4, 7, 0, 1, 2, 5, 6, // 0b01100111 (103)|Left-PC: 3
            40, 1, 2, 4, 7, 3, 5, 6, // 0b01101000 (104)|Left-PC: 5
            33, 2, 4, 7, 0, 3, 5, 6, // 0b01101001 (105)|Left-PC: 4
            32, 2, 4, 7, 1, 3, 5, 6, // 0b01101010 (106)|Left-PC: 4
            26, 4, 7, 0, 1, 3, 5, 6, // 0b01101011 (107)|Left-PC: 3
            32, 1, 4, 7, 2, 3, 5, 6, // 0b01101100 (108)|Left-PC: 4
            25, 4, 7, 0, 2, 3, 5, 6, // 0b01101101 (109)|Left-PC: 3
            24, 4, 7, 1, 2, 3, 5, 6, // 0b01101110 (110)|Left-PC: 3
            20, 7, 0, 1, 2, 3, 5, 6, // 0b01101111 (111)|Left-PC: 2
            40, 1, 2, 3, 7, 4, 5, 6, // 0b01110000 (112)|Left-PC: 5
            33, 2, 3, 7, 0, 4, 5, 6, // 0b01110001 (113)|Left-PC: 4
            32, 2, 3, 7, 1, 4, 5, 6, // 0b01110010 (114)|Left-PC: 4
            26, 3, 7, 0, 1, 4, 5, 6, // 0b01110011 (115)|Left-PC: 3
            32, 1, 3, 7, 2, 4, 5, 6, // 0b01110100 (116)|Left-PC: 4
            25, 3, 7, 0, 2, 4, 5, 6, // 0b01110101 (117)|Left-PC: 3
            24, 3, 7, 1, 2, 4, 5, 6, // 0b01110110 (118)|Left-PC: 3
            19, 7, 0, 1, 2, 4, 5, 6, // 0b01110111 (119)|Left-PC: 2
            32, 1, 2, 7, 3, 4, 5, 6, // 0b01111000 (120)|Left-PC: 4
            25, 2, 7, 0, 3, 4, 5, 6, // 0b01111001 (121)|Left-PC: 3
            24, 2, 7, 1, 3, 4, 5, 6, // 0b01111010 (122)|Left-PC: 3
            18, 7, 0, 1, 3, 4, 5, 6, // 0b01111011 (123)|Left-PC: 2
            24, 1, 7, 2, 3, 4, 5, 6, // 0b01111100 (124)|Left-PC: 3
            17, 7, 0, 2, 3, 4, 5, 6, // 0b01111101 (125)|Left-PC: 2
            16, 7, 1, 2, 3, 4, 5, 6, // 0b01111110 (126)|Left-PC: 2
            15, 0, 1, 2, 3, 4, 5, 6, // 0b01111111 (127)|Left-PC: 1
            56, 1, 2, 3, 4, 5, 6, 7, // 0b10000000 (128)|Left-PC: 7
            49, 2, 3, 4, 5, 6, 0, 7, // 0b10000001 (129)|Left-PC: 6
            48, 2, 3, 4, 5, 6, 1, 7, // 0b10000010 (130)|Left-PC: 6
            42, 3, 4, 5, 6, 0, 1, 7, // 0b10000011 (131)|Left-PC: 5
            48, 1, 3, 4, 5, 6, 2, 7, // 0b10000100 (132)|Left-PC: 6
            41, 3, 4, 5, 6, 0, 2, 7, // 0b10000101 (133)|Left-PC: 5
            40, 3, 4, 5, 6, 1, 2, 7, // 0b10000110 (134)|Left-PC: 5
            35, 4, 5, 6, 0, 1, 2, 7, // 0b10000111 (135)|Left-PC: 4
            48, 1, 2, 4, 5, 6, 3, 7, // 0b10001000 (136)|Left-PC: 6
            41, 2, 4, 5, 6, 0, 3, 7, // 0b10001001 (137)|Left-PC: 5
            40, 2, 4, 5, 6, 1, 3, 7, // 0b10001010 (138)|Left-PC: 5
            34, 4, 5, 6, 0, 1, 3, 7, // 0b10001011 (139)|Left-PC: 4
            40, 1, 4, 5, 6, 2, 3, 7, // 0b10001100 (140)|Left-PC: 5
            33, 4, 5, 6, 0, 2, 3, 7, // 0b10001101 (141)|Left-PC: 4
            32, 4, 5, 6, 1, 2, 3, 7, // 0b10001110 (142)|Left-PC: 4
            28, 5, 6, 0, 1, 2, 3, 7, // 0b10001111 (143)|Left-PC: 3
            48, 1, 2, 3, 5, 6, 4, 7, // 0b10010000 (144)|Left-PC: 6
            41, 2, 3, 5, 6, 0, 4, 7, // 0b10010001 (145)|Left-PC: 5
            40, 2, 3, 5, 6, 1, 4, 7, // 0b10010010 (146)|Left-PC: 5
            34, 3, 5, 6, 0, 1, 4, 7, // 0b10010011 (147)|Left-PC: 4
            40, 1, 3, 5, 6, 2, 4, 7, // 0b10010100 (148)|Left-PC: 5
            33, 3, 5, 6, 0, 2, 4, 7, // 0b10010101 (149)|Left-PC: 4
            32, 3, 5, 6, 1, 2, 4, 7, // 0b10010110 (150)|Left-PC: 4
            27, 5, 6, 0, 1, 2, 4, 7, // 0b10010111 (151)|Left-PC: 3
            40, 1, 2, 5, 6, 3, 4, 7, // 0b10011000 (152)|Left-PC: 5
            33, 2, 5, 6, 0, 3, 4, 7, // 0b10011001 (153)|Left-PC: 4
            32, 2, 5, 6, 1, 3, 4, 7, // 0b10011010 (154)|Left-PC: 4
            26, 5, 6, 0, 1, 3, 4, 7, // 0b10011011 (155)|Left-PC: 3
            32, 1, 5, 6, 2, 3, 4, 7, // 0b10011100 (156)|Left-PC: 4
            25, 5, 6, 0, 2, 3, 4, 7, // 0b10011101 (157)|Left-PC: 3
            24, 5, 6, 1, 2, 3, 4, 7, // 0b10011110 (158)|Left-PC: 3
            21, 6, 0, 1, 2, 3, 4, 7, // 0b10011111 (159)|Left-PC: 2
            48, 1, 2, 3, 4, 6, 5, 7, // 0b10100000 (160)|Left-PC: 6
            41, 2, 3, 4, 6, 0, 5, 7, // 0b10100001 (161)|Left-PC: 5
            40, 2, 3, 4, 6, 1, 5, 7, // 0b10100010 (162)|Left-PC: 5
            34, 3, 4, 6, 0, 1, 5, 7, // 0b10100011 (163)|Left-PC: 4
            40, 1, 3, 4, 6, 2, 5, 7, // 0b10100100 (164)|Left-PC: 5
            33, 3, 4, 6, 0, 2, 5, 7, // 0b10100101 (165)|Left-PC: 4
            32, 3, 4, 6, 1, 2, 5, 7, // 0b10100110 (166)|Left-PC: 4
            27, 4, 6, 0, 1, 2, 5, 7, // 0b10100111 (167)|Left-PC: 3
            40, 1, 2, 4, 6, 3, 5, 7, // 0b10101000 (168)|Left-PC: 5
            33, 2, 4, 6, 0, 3, 5, 7, // 0b10101001 (169)|Left-PC: 4
            32, 2, 4, 6, 1, 3, 5, 7, // 0b10101010 (170)|Left-PC: 4
            26, 4, 6, 0, 1, 3, 5, 7, // 0b10101011 (171)|Left-PC: 3
            32, 1, 4, 6, 2, 3, 5, 7, // 0b10101100 (172)|Left-PC: 4
            25, 4, 6, 0, 2, 3, 5, 7, // 0b10101101 (173)|Left-PC: 3
            24, 4, 6, 1, 2, 3, 5, 7, // 0b10101110 (174)|Left-PC: 3
            20, 6, 0, 1, 2, 3, 5, 7, // 0b10101111 (175)|Left-PC: 2
            40, 1, 2, 3, 6, 4, 5, 7, // 0b10110000 (176)|Left-PC: 5
            33, 2, 3, 6, 0, 4, 5, 7, // 0b10110001 (177)|Left-PC: 4
            32, 2, 3, 6, 1, 4, 5, 7, // 0b10110010 (178)|Left-PC: 4
            26, 3, 6, 0, 1, 4, 5, 7, // 0b10110011 (179)|Left-PC: 3
            32, 1, 3, 6, 2, 4, 5, 7, // 0b10110100 (180)|Left-PC: 4
            25, 3, 6, 0, 2, 4, 5, 7, // 0b10110101 (181)|Left-PC: 3
            24, 3, 6, 1, 2, 4, 5, 7, // 0b10110110 (182)|Left-PC: 3
            19, 6, 0, 1, 2, 4, 5, 7, // 0b10110111 (183)|Left-PC: 2
            32, 1, 2, 6, 3, 4, 5, 7, // 0b10111000 (184)|Left-PC: 4
            25, 2, 6, 0, 3, 4, 5, 7, // 0b10111001 (185)|Left-PC: 3
            24, 2, 6, 1, 3, 4, 5, 7, // 0b10111010 (186)|Left-PC: 3
            18, 6, 0, 1, 3, 4, 5, 7, // 0b10111011 (187)|Left-PC: 2
            24, 1, 6, 2, 3, 4, 5, 7, // 0b10111100 (188)|Left-PC: 3
            17, 6, 0, 2, 3, 4, 5, 7, // 0b10111101 (189)|Left-PC: 2
            16, 6, 1, 2, 3, 4, 5, 7, // 0b10111110 (190)|Left-PC: 2
            14, 0, 1, 2, 3, 4, 5, 7, // 0b10111111 (191)|Left-PC: 1
            48, 1, 2, 3, 4, 5, 6, 7, // 0b11000000 (192)|Left-PC: 6
            41, 2, 3, 4, 5, 0, 6, 7, // 0b11000001 (193)|Left-PC: 5
            40, 2, 3, 4, 5, 1, 6, 7, // 0b11000010 (194)|Left-PC: 5
            34, 3, 4, 5, 0, 1, 6, 7, // 0b11000011 (195)|Left-PC: 4
            40, 1, 3, 4, 5, 2, 6, 7, // 0b11000100 (196)|Left-PC: 5
            33, 3, 4, 5, 0, 2, 6, 7, // 0b11000101 (197)|Left-PC: 4
            32, 3, 4, 5, 1, 2, 6, 7, // 0b11000110 (198)|Left-PC: 4
            27, 4, 5, 0, 1, 2, 6, 7, // 0b11000111 (199)|Left-PC: 3
            40, 1, 2, 4, 5, 3, 6, 7, // 0b11001000 (200)|Left-PC: 5
            33, 2, 4, 5, 0, 3, 6, 7, // 0b11001001 (201)|Left-PC: 4
            32, 2, 4, 5, 1, 3, 6, 7, // 0b11001010 (202)|Left-PC: 4
            26, 4, 5, 0, 1, 3, 6, 7, // 0b11001011 (203)|Left-PC: 3
            32, 1, 4, 5, 2, 3, 6, 7, // 0b11001100 (204)|Left-PC: 4
            25, 4, 5, 0, 2, 3, 6, 7, // 0b11001101 (205)|Left-PC: 3
            24, 4, 5, 1, 2, 3, 6, 7, // 0b11001110 (206)|Left-PC: 3
            20, 5, 0, 1, 2, 3, 6, 7, // 0b11001111 (207)|Left-PC: 2
            40, 1, 2, 3, 5, 4, 6, 7, // 0b11010000 (208)|Left-PC: 5
            33, 2, 3, 5, 0, 4, 6, 7, // 0b11010001 (209)|Left-PC: 4
            32, 2, 3, 5, 1, 4, 6, 7, // 0b11010010 (210)|Left-PC: 4
            26, 3, 5, 0, 1, 4, 6, 7, // 0b11010011 (211)|Left-PC: 3
            32, 1, 3, 5, 2, 4, 6, 7, // 0b11010100 (212)|Left-PC: 4
            25, 3, 5, 0, 2, 4, 6, 7, // 0b11010101 (213)|Left-PC: 3
            24, 3, 5, 1, 2, 4, 6, 7, // 0b11010110 (214)|Left-PC: 3
            19, 5, 0, 1, 2, 4, 6, 7, // 0b11010111 (215)|Left-PC: 2
            32, 1, 2, 5, 3, 4, 6, 7, // 0b11011000 (216)|Left-PC: 4
            25, 2, 5, 0, 3, 4, 6, 7, // 0b11011001 (217)|Left-PC: 3
            24, 2, 5, 1, 3, 4, 6, 7, // 0b11011010 (218)|Left-PC: 3
            18, 5, 0, 1, 3, 4, 6, 7, // 0b11011011 (219)|Left-PC: 2
            24, 1, 5, 2, 3, 4, 6, 7, // 0b11011100 (220)|Left-PC: 3
            17, 5, 0, 2, 3, 4, 6, 7, // 0b11011101 (221)|Left-PC: 2
            16, 5, 1, 2, 3, 4, 6, 7, // 0b11011110 (222)|Left-PC: 2
            13, 0, 1, 2, 3, 4, 6, 7, // 0b11011111 (223)|Left-PC: 1
            40, 1, 2, 3, 4, 5, 6, 7, // 0b11100000 (224)|Left-PC: 5
            33, 2, 3, 4, 0, 5, 6, 7, // 0b11100001 (225)|Left-PC: 4
            32, 2, 3, 4, 1, 5, 6, 7, // 0b11100010 (226)|Left-PC: 4
            26, 3, 4, 0, 1, 5, 6, 7, // 0b11100011 (227)|Left-PC: 3
            32, 1, 3, 4, 2, 5, 6, 7, // 0b11100100 (228)|Left-PC: 4
            25, 3, 4, 0, 2, 5, 6, 7, // 0b11100101 (229)|Left-PC: 3
            24, 3, 4, 1, 2, 5, 6, 7, // 0b11100110 (230)|Left-PC: 3
            19, 4, 0, 1, 2, 5, 6, 7, // 0b11100111 (231)|Left-PC: 2
            32, 1, 2, 4, 3, 5, 6, 7, // 0b11101000 (232)|Left-PC: 4
            25, 2, 4, 0, 3, 5, 6, 7, // 0b11101001 (233)|Left-PC: 3
            24, 2, 4, 1, 3, 5, 6, 7, // 0b11101010 (234)|Left-PC: 3
            18, 4, 0, 1, 3, 5, 6, 7, // 0b11101011 (235)|Left-PC: 2
            24, 1, 4, 2, 3, 5, 6, 7, // 0b11101100 (236)|Left-PC: 3
            17, 4, 0, 2, 3, 5, 6, 7, // 0b11101101 (237)|Left-PC: 2
            16, 4, 1, 2, 3, 5, 6, 7, // 0b11101110 (238)|Left-PC: 2
            12, 0, 1, 2, 3, 5, 6, 7, // 0b11101111 (239)|Left-PC: 1
            32, 1, 2, 3, 4, 5, 6, 7, // 0b11110000 (240)|Left-PC: 4
            25, 2, 3, 0, 4, 5, 6, 7, // 0b11110001 (241)|Left-PC: 3
            24, 2, 3, 1, 4, 5, 6, 7, // 0b11110010 (242)|Left-PC: 3
            18, 3, 0, 1, 4, 5, 6, 7, // 0b11110011 (243)|Left-PC: 2
            24, 1, 3, 2, 4, 5, 6, 7, // 0b11110100 (244)|Left-PC: 3
            17, 3, 0, 2, 4, 5, 6, 7, // 0b11110101 (245)|Left-PC: 2
            16, 3, 1, 2, 4, 5, 6, 7, // 0b11110110 (246)|Left-PC: 2
            11, 0, 1, 2, 4, 5, 6, 7, // 0b11110111 (247)|Left-PC: 1
            24, 1, 2, 3, 4, 5, 6, 7, // 0b11111000 (248)|Left-PC: 3
            17, 2, 0, 3, 4, 5, 6, 7, // 0b11111001 (249)|Left-PC: 2
            16, 2, 1, 3, 4, 5, 6, 7, // 0b11111010 (250)|Left-PC: 2
            10, 0, 1, 3, 4, 5, 6, 7, // 0b11111011 (251)|Left-PC: 1
            16, 1, 2, 3, 4, 5, 6, 7, // 0b11111100 (252)|Left-PC: 2
            9, 0, 2, 3, 4, 5, 6, 7,  // 0b11111101 (253)|Left-PC: 1
            8, 1, 2, 3, 4, 5, 6, 7,  // 0b11111110 (254)|Left-PC: 1
            0, 1, 2, 3, 4, 5, 6, 7,  // 0b11111111 (255)|Left-PC: 0
        };
    }
}
