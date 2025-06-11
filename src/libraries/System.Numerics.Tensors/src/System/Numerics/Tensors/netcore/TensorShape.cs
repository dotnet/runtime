// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace System.Numerics.Tensors
{
    // TensorShape tracks the core information required to safely interact with memory
    // for tensors and tensor spans alike.
    //
    // We avoid allocating for small ranks up to MaxInlineRank in size and allocate a
    // single buffer for larger ranks. This buffer will always be precisely `InlineBufferCount * rank`
    // in size where the first rank elements are the length, the next rank elements are
    // the strides, and then the next rank elements are the rank order.
    //
    // We cache both a flattened length and a linear length to avoid recalculating these
    // key properties. The flattened length is the total number of elements represented
    // by the tensor, however due to implicit broadcasting this may be greater than the
    // amount of memory that actually backs the tensor. While the linear length is the
    // backing storage size that is present. We can also have arbitrary strides, such as
    // lengths: [4], strides: [2]. In this case the flattenedLength = 4, while the
    // linearLength: 8. We can also have a slice of a tensor, such as lengths: [2, 2],
    // strides: [3, 1] (which could have been taken from a 3x3 dense tensor).
    //
    // These invariants allow us to safely and efficiently index into the backing storage
    // as we know that memory functionally wraps around after linearLength elements. It
    // also means that we only support broadcasting to greater dimensions and thus lengths
    // strides and the backing memory form a strict relationship that is validated by the
    // public constructors.

    [Flags]
    internal enum TensorFlags : uint
    {
        None = 0,
        IsDense = (1 << 0),
        IsBroadcast = (1 << 1),
        HasAnyDenseDimensions = (1 << 2),
    }

    [Experimental(Experimentals.TensorTDiagId, UrlFormat = Experimentals.SharedUrlFormat)]
    internal readonly struct TensorShape : IEquatable<TensorShape>
    {
        // The layout of the fields here is very particular and is intentionally designed to
        // be compact and cache-friendly. The current size on a 64-bit system is 108+4 bytes
        // and this fits within 2 cache lines, where 64 bytes is the typical cache line size.
        //
        // The TensorSpan and ReadOnlyTensorSpan types then track a byref field which takes
        // an additional 8 bytes. This leaves 8 bytes still available for use for other scenarios
        // if required.

        internal const int MaxInlineRank = 4;
        private const int InlineBufferCount = 3;

        private readonly nint[]? _metadata;                         // 8 bytes

        private readonly nint _flattenedLength;                     // 8 bytes
        private readonly nint _linearLength;                        // 8 bytes

        private readonly InlineBuffer<nint> _inlineLengths;         // 4x8 bytes (32)
        private readonly InlineBuffer<nint> _inlineStrides;         // 4x8 bytes (32)
        private readonly InlineBuffer<int>  _inlineLinearRankOrder; // 4x4 bytes (16)

        private readonly int _rank;
        private readonly TensorFlags _flags;                  // 4 bytes

        private TensorShape(nint linearLength, scoped ReadOnlySpan<nint> lengths, scoped ReadOnlySpan<nint> strides, scoped ReadOnlySpan<int> linearRankOrder)
        {
            int rank = lengths.Length;

            if (rank == 0)
            {
                lengths = [0];
                rank = 1;
            }
            Debug.Assert(rank >= 1);

            scoped Span<nint> destinationLengths;
            scoped Span<nint> destinationStrides;
            scoped Span<int> destinationLinearRankOrder;

            if (rank > MaxInlineRank)
            {
                nint[] metadata = new nint[rank * InlineBufferCount];

                destinationLengths = metadata.AsSpan(rank * 0, rank);
                destinationStrides = metadata.AsSpan(rank * 1, rank);
                destinationLinearRankOrder = MemoryMarshal.CreateSpan(
                    ref Unsafe.As<nint, int>(ref metadata[rank * 2]),
                    rank
                );

                _metadata = metadata;
            }
            else
            {
                destinationLengths = ((Span<nint>)_inlineLengths)[..rank];
                destinationStrides = ((Span<nint>)_inlineStrides)[..rank];
                destinationLinearRankOrder = ((Span<int>)_inlineLinearRankOrder)[..rank];
            }

            if (linearRankOrder.Length == 0)
            {
                // The linearRankOrder is expected to be in "row-major" order, that is otherwise
                // known as "big-endian" order where the dimensions that are farthest apart
                // (i.e. have the greatest impact to computing a linear index) appear first.
                //
                // So, when no rank order is specified by the user we simply populate this
                // as 0 to rank-1. In the case strides is also not specified, this will be
                // correct "as is"; otherwise, we will sort this based on which stride is
                // largest prior to validating the user provided strides.

                for (int i = 0; i < destinationLinearRankOrder.Length; i++)
                {
                    destinationLinearRankOrder[i] = i;
                }
            }
            else if (linearRankOrder.Length != rank)
            {
                ThrowHelper.ThrowArgument_InvalidTensorShape();
            }
            else
            {
                // If a rank order was specified, then we need to ensure that it is valid,
                // which should mean that when sorting we have values from 0 to rank-1.
                //
                // While this does copy the rank order twice, the typical rank is expected
                // to be small and so the cost should be minimal.

                linearRankOrder.CopyTo(destinationLinearRankOrder);
                destinationLinearRankOrder.Sort();

                for (int i = 0; i < linearRankOrder.Length; i++)
                {
                    if (destinationLinearRankOrder[i] != i)
                    {
                        ThrowHelper.ThrowArgument_InvalidTensorShape();
                    }
                }

                linearRankOrder.CopyTo(destinationLinearRankOrder);
            }

            nint flattenedLength = 1;
            nint maximumLinearIndex = 0;

            if (strides.Length == 0)
            {
                // When no strides are specified, we need to computing them based on the given
                // rank order. We use destinationLinearRankOrder here to ensure that we have a
                // correct order even if no rank order was specified by the user.
                //
                // To do this, we simply iterate the rank order from least to most significant
                // so that the strides match the expected order, being the product of all previous
                // dimension lengths.

                for (int i = 0; i < destinationLinearRankOrder.Length; i++)
                {
                    int rankIndex = destinationLinearRankOrder.Length - (i + 1);
                    int linearRankIndex = destinationLinearRankOrder[rankIndex];

                    nint length = lengths[linearRankIndex];

                    if (length < 0)
                    {
                        ThrowHelper.ThrowArgument_LengthIsNegative();
                    }

                    nint stride = flattenedLength;

                    if (length > 1)
                    {
                        maximumLinearIndex = checked(maximumLinearIndex + ((length - 1) * stride));
                    }
                    else
                    {
                        stride = 0;
                        _flags |= TensorFlags.IsBroadcast;
                    }

                    destinationLengths[linearRankIndex] = length;
                    destinationStrides[linearRankIndex] = stride;

                    flattenedLength = checked(flattenedLength * length);
                }

                // When the strides are automatically computed, then we must be dense
                _flags |= (TensorFlags.IsDense | TensorFlags.HasAnyDenseDimensions);
            }
            else if (strides.Length != lengths.Length)
            {
                ThrowHelper.ThrowArgument_InvalidTensorShape();
            }
            else
            {
                // If a strides was specified, then we need to ensure it is valid as well,
                // which should mean that when sorted by rank order (most to least significant)
                // each stride should be greater than or equal to the previous stride multiplied
                // by the dimension length.
                //
                // The reason it is "or equal" and not simply "greater than" is because a dimension
                // can be length 1 and thus the stride is the same as the previous rank.
                //
                // Additionally, when sorted we allow for the first n (most significant) strides to be
                // specified as 0 in which case we automatically compute that to be the same as stride
                // n+1. This makes it convenient to support implicit broadcasting where higher dimensions
                // aren't actually stored in memory.

                nint minimumNonZeroStride = 1;
                var sortedWithIndex = destinationLinearRankOrder.ToArray()
                    .Select((value, index) => new { Value = value, Index = index })
                    .OrderBy(x => x.Value)
                    .ToArray();

                int[] sortedOrder = sortedWithIndex.Select(x => x.Index).ToArray();
                bool isDense = true;

                for (int i = 0; i < destinationLinearRankOrder.Length; i++)
                {
                    int rankIndex = destinationLinearRankOrder.Length - (i + 1);
                    int linearRankIndex = sortedOrder[rankIndex];

                    if (linearRankIndex != rankIndex)
                    {
                        // We cannot be dense if we aren't following linear order
                        isDense = false;
                    }

                    nint length = lengths[linearRankIndex];

                    if (length < 0)
                    {
                        ThrowHelper.ThrowArgument_LengthIsNegative();
                    }

                    nint stride = strides[linearRankIndex];

                    if (stride < 0)
                    {
                        ThrowHelper.ThrowArgument_StrideIsNegative();
                    }

                    if (stride != 0)
                    {
                        if (stride < minimumNonZeroStride)
                        {
                            // The next stride needs to be at least as big as the
                            // previous stride times the dimension length, otherwise
                            // the linear rank ordering is incorrect
                            ThrowHelper.ThrowArgument_InvalidTensorShape();
                        }
                        else if (stride != minimumNonZeroStride)
                        {
                            isDense = false;
                        }

                        if (length <= 1)
                        {
                            // We require the stride to be zero if the dimension length
                            // is 0 or 1, as this is necessary for indexing with broadcast to
                            // work as expected.
                            ThrowHelper.ThrowArgument_InvalidTensorShape();
                        }

                        minimumNonZeroStride = checked(length * stride);
                        maximumLinearIndex = checked(maximumLinearIndex + (minimumNonZeroStride - stride));
                    }
                    else
                    {
                        _flags |= TensorFlags.IsBroadcast;

                        if (length != 1)
                        {
                            // We only cannot be dense if the broadcast is for more than 1 element
                            isDense = false;
                        }
                    }

                    destinationLengths[linearRankIndex] = length;
                    destinationStrides[linearRankIndex] = stride;

                    flattenedLength = checked(flattenedLength * length);
                }

                if (isDense)
                {
                    _flags |= (TensorFlags.IsDense | TensorFlags.HasAnyDenseDimensions);
                }
                else if (CalculateHasAnyDenseDimensions(lengths, strides))
                {
                    _flags |= TensorFlags.HasAnyDenseDimensions;
                }
            }

            // Once we've finished computing everything physically present in the input
            // we need to ensure that the linearLength is greater than or equal to the
            // minimumLinearLength so that lengths is in range of the backing buffer and
            // so that we can support broadcasting for anything that was length 1.

            nint minimumLinearLength = (flattenedLength != 0) ? (maximumLinearIndex + 1) : 0;

            if (linearLength != -1)
            {
                ArgumentOutOfRangeException.ThrowIfLessThan(linearLength, minimumLinearLength);
            }

            Debug.Assert((flattenedLength == minimumLinearLength) || (strides.Length != 0));

            _flattenedLength = flattenedLength;
            _linearLength = minimumLinearLength;

            _rank = rank;

            ValidateState();
        }

        private TensorShape(nint flattenedLength, nint linearLength, scoped ReadOnlySpan<nint> lengths, scoped ReadOnlySpan<nint> strides, scoped ReadOnlySpan<int> linearRankOrder, int rank, TensorFlags flags)
        {
            Debug.Assert(lengths.Length == rank);
            Debug.Assert(strides.Length == rank);
            Debug.Assert(linearRankOrder.Length == rank);

            scoped Span<nint> destinationLengths;
            scoped Span<nint> destinationStrides;
            scoped Span<int> destinationLinearRankOrder;

            if (rank > MaxInlineRank)
            {
                nint[] metadata = new nint[rank * InlineBufferCount];

                destinationLengths = metadata.AsSpan(rank * 0, rank);
                destinationStrides = metadata.AsSpan(rank * 1, rank);
                destinationLinearRankOrder = MemoryMarshal.CreateSpan(
                    ref Unsafe.As<nint, int>(ref metadata[rank * 2]),
                    rank
                );

                _metadata = metadata;
            }
            else
            {
                destinationLengths = ((Span<nint>)_inlineLengths)[..rank];
                destinationStrides = ((Span<nint>)_inlineStrides)[..rank];
                destinationLinearRankOrder = ((Span<int>)_inlineLinearRankOrder)[..rank];
            }

            _flattenedLength = flattenedLength;
            _linearLength = linearLength;

            lengths.CopyTo(destinationLengths);
            strides.CopyTo(destinationStrides);
            linearRankOrder.CopyTo(destinationLinearRankOrder);

            _rank = rank;
            _flags = flags;

            ValidateState();
        }

        public bool HasAnyDenseDimensions => (_flags & TensorFlags.HasAnyDenseDimensions) != 0;

        public bool IsBroadcast => (_flags & TensorFlags.IsBroadcast) != 0;

        public bool IsDense => (_flags & TensorFlags.IsDense) != 0;

        public nint FlattenedLength => _flattenedLength;

        public bool IsEmpty => _flattenedLength == 0;

        public nint LinearLength => _linearLength;

        [UnscopedRef]
        public ReadOnlySpan<nint> Lengths
        {
            get
            {
                if (_metadata is not nint[] metadata)
                {
                    return ((ReadOnlySpan<nint>)_inlineLengths)[.._rank];
                }
                else
                {
                    int rank = metadata.Length / InlineBufferCount;
                    return metadata.AsSpan(rank * 0, rank);
                }
            }
        }

        [UnscopedRef]
        public ReadOnlySpan<int> LinearRankOrder
        {
            get
            {
                if (_metadata is not nint[] metadata)
                {
                    return ((ReadOnlySpan<int>)_inlineLinearRankOrder)[.._rank];
                }
                else
                {
                    int rank = metadata.Length / InlineBufferCount;
                    return MemoryMarshal.CreateSpan(
                        ref Unsafe.As<nint, int>(ref metadata[rank * 2]),
                        rank
                    );
                }
            }
        }

        public int Rank => _rank;

        [UnscopedRef]
        public ReadOnlySpan<nint> Strides
        {
            get
            {
                if (_metadata is not nint[] metadata)
                {
                    return ((ReadOnlySpan<nint>)_inlineStrides)[.._rank];
                }
                else
                {
                    int rank = metadata.Length / InlineBufferCount;
                    return metadata.AsSpan(rank * 1, rank);
                }
            }
        }

        public static bool operator ==(in TensorShape left, in TensorShape right)
        {
            int rank = left.Rank;

            if (rank != right.Rank)
            {
                return false;
            }

            if (left.FlattenedLength != right.FlattenedLength)
            {
                return false;
            }

            if (left.LinearLength != right.LinearLength)
            {
                return false;
            }

            ReadOnlySpan<nint> leftLengths = left.Lengths;
            ReadOnlySpan<int> leftLinearRankOrder = left.LinearRankOrder;
            ReadOnlySpan<nint> leftStrides = left.Strides;

            ReadOnlySpan<nint> rightLengths = right.Lengths;
            ReadOnlySpan<int> rightLinearRankOrder = right.LinearRankOrder;
            ReadOnlySpan<nint> rightStrides = right.Strides;

            for (int i = 0; i < rank; i++)
            {
                // We need to compare lengths and strides based on the linearRankOrder
                // to ensure that two tensors representing the same memory, but where
                // the shapes are logically, but not physically, transposed are considered
                // equal.

                int leftRankIndex = leftLinearRankOrder[i];
                int rightRankIndex = rightLinearRankOrder[i];

                if (leftLengths[leftRankIndex] != rightLengths[rightRankIndex])
                {
                    return false;
                }

                if (leftStrides[leftRankIndex] != rightStrides[rightRankIndex])
                {
                    return false;
                }
            }

            return true;
        }

        public static bool operator !=(in TensorShape left, in TensorShape right) => !(left == right);

        public nint AdjustToNextIndex(in TensorShape destinationShape, nint linearOffset, Span<nint> indexes)
        {
            Debug.Assert(indexes.Length >= Rank);
            Debug.Assert(indexes.Length == destinationShape.Rank);

            ReadOnlySpan<nint> lengths = Lengths;
            ReadOnlySpan<nint> strides = Strides;

            ReadOnlySpan<nint> destinationLengths = destinationShape.Lengths;

            for (int i = 0; i < strides.Length; i++)
            {
                int rankIndex = lengths.Length - (i + 1);
                int destinationRankIndex = destinationShape.Lengths.Length - (i + 1);

                nint length = lengths[rankIndex];
                nint stride = strides[rankIndex];

                nint index = ++indexes[destinationRankIndex];
                linearOffset += stride;

                // We can have a scenario such as lengths: [1], destinationLengths: [2] in
                // which case we still need to keep incrementing the index but without
                // adjusting the linearOffset

                if (index < destinationLengths[destinationRankIndex])
                {
                    if (index >= length)
                    {
                        // We should only be here if we were broadcast
                        Debug.Assert((length == 1) && (stride == 0));
                    }
                    return linearOffset;
                }

                indexes[destinationRankIndex] = 0;
                linearOffset -= (stride * length);
            }

            if (indexes.Length != Rank)
            {
                for (int i = strides.Length; i < destinationLengths.Length; i++)
                {
                    int rankIndex = destinationLengths.Length - (i + 1);

                    nint length = destinationLengths[rankIndex];
                    // Strides are always 0 because we are broadcasting at this point in the loop.

                    nint index = ++indexes[rankIndex];

                    if (index < length)
                    {
                        // For any indexes that exist in the destinationShape but not
                        // in the srcShape we will only increment them if all lower
                        // indexes were 0. This means we're starting over at the beginning
                        // of the srcShape and the linearOffset must be 0.
                        break;
                    }

                    indexes[rankIndex] = 0;
                }
            }
            return 0;
        }

        public nint AdjustToPreviousIndex(in TensorShape destinationShape, nint linearOffset, Span<nint> indexes)
        {
            Debug.Assert(indexes.Length >= Rank);
            Debug.Assert(indexes.Length == destinationShape.Rank);

            ReadOnlySpan<nint> lengths = Lengths;
            ReadOnlySpan<nint> strides = Strides;

            ReadOnlySpan<nint> destinationLengths = destinationShape.Lengths;

            for (int i = 0; i < strides.Length; i++)
            {
                int rankIndex = lengths.Length - (i + 1);
                int destinationRankIndex = destinationShape.Lengths.Length - (i + 1);

                nint length = lengths[rankIndex];
                nint stride = strides[rankIndex];

                nint index = --indexes[destinationRankIndex];
                linearOffset -= stride;

                // We can have a scenario such as lengths: [1], destinationLengths: [2] in
                // which case we still need to keep incrementing the index but without
                // adjusting the linearOffset

                if (index >= 0)//destinationLengths[destinationRankIndex])
                {
                    if (index >= length)
                    {
                        // We should only be here if we were broadcast
                        Debug.Assert((length == 1) && (stride == 0));
                    }
                    return linearOffset;
                }

                indexes[destinationRankIndex] = lengths[rankIndex];
                linearOffset += (stride * length);
            }

            if (indexes.Length != Rank)
            {
                for (int i = destinationLengths.Length - 1; i >= strides.Length; i--)
                {
                    int rankIndex = destinationLengths.Length - (i + 1);

                    nint length = destinationLengths[rankIndex];
                    // Strides are always 0 because we are broadcasting at this point in the loop.

                    nint index = ++indexes[rankIndex];

                    if (index < length)
                    {
                        // For any indexes that exist in the destinationShape but not
                        // in the srcShape we will only increment them if all lower
                        // indexes were 0. This means we're starting over at the beginning
                        // of the srcShape and the linearOffset must be 0.
                        break;
                    }

                    indexes[rankIndex] = 0;
                }
            }
            return 0;
        }

        public static bool AdjustToNextIndex(Span<NRange> ranges, int dimension, ReadOnlySpan<nint> lengths)
        {
            NRange curRange = ranges[dimension];
            ranges[dimension] = new NRange(curRange.Start.Value + 1, curRange.End.Value + 1);

            for (int i = dimension; i >= 0; i--)
            {
                if (ranges[i].Start.Value >= lengths[i])
                {
                    ranges[i] = 0..1;
                    if (i == 0)
                        return false;
                    ranges[i - 1] = new NRange(ranges[i - 1].Start.Value + 1, ranges[i - 1].End.Value + 1);
                }
            }
            return true;
        }

        // Answer the question: Can shape2 turn into shape1 or vice-versa if allowBidirectional?
        public static bool AreCompatible(in TensorShape shape1, in TensorShape shape2, bool allowBidirectional)
        {
            int rankDelta = shape1.Rank - shape2.Rank;

            if (rankDelta < 0)
            {
                if (!allowBidirectional)
                {
                    return false;
                }

                ref readonly TensorShape tmpShape = ref shape1;
                shape1 = ref shape2;
                shape2 = ref tmpShape;

                rankDelta = -rankDelta;
                Debug.Assert(rankDelta > 0);
            }

            // We need both to be empty if either is empty

            if (shape1.IsEmpty)
            {
                return shape2.IsEmpty;
            }
            else if (shape2.IsEmpty)
            {
                return false;
            }

            // We need the lengths to be equal, length2 to be 1, or
            // length1 to be 1 and be doing a bidirectional check.

            ReadOnlySpan<nint> lengths1 = shape1.Lengths[rankDelta..];
            ReadOnlySpan<nint> lengths2 = shape2.Lengths;

            for (int i = 0; i < lengths1.Length; i++)
            {
                nint length1 = lengths1[i];
                nint length2 = lengths2[i];

                if (length1 == length2)
                {
                    continue;
                }
                else if ((length1 == 1) && allowBidirectional)
                {
                    continue;
                }
                else if (length2 == 1)
                {
                    continue;
                }

                return false;
            }

            if (!allowBidirectional)
            {
                // When we aren't bidirectionally compatible, then we
                // need to ensure that if stride1 is 0, then stride2
                // is also zero; otherwise we cannot safely operate.

                ReadOnlySpan<nint> strides1 = shape1.Strides[rankDelta..];
                ReadOnlySpan<nint> strides2 = shape2.Strides;

                for (int i = 0; i < strides1.Length; i++)
                {
                    nint stride1 = strides1[i];
                    nint stride2 = strides2[i];

                    if ((stride1 == 0) && (stride2 != 0))
                    {
                        return false;
                    }
                }
            }
            return true;
        }

        // Answer the question: Can shape2 turn into shape1Lengths
        public static bool AreCompatible(in ReadOnlySpan<nint> shape1Lengths, in TensorShape shape2)
        {
            int rankDelta = shape1Lengths.Length - shape2.Rank;

            if (rankDelta < 0)
            {
                return false;
            }

            // We need both to be empty if either is empty

            if (shape1Lengths.IsEmpty)
            {
                return shape2.IsEmpty;
            }
            else if (shape2.IsEmpty)
            {
                return false;
            }

            // We need the lengths to be equal, length2 to be 1, or
            // length1 to be 1 and be doing a bidirectional check.

            ReadOnlySpan<nint> lengths1 = shape1Lengths[rankDelta..];
            ReadOnlySpan<nint> lengths2 = shape2.Lengths;

            for (int i = 0; i < lengths1.Length; i++)
            {
                nint length1 = lengths1[i];
                nint length2 = lengths2[i];

                if (length1 == length2)
                {
                    continue;
                }
                else if (length2 == 1)
                {
                    continue;
                }

                return false;
            }

            return true;
        }

        public static bool AreLengthsTheSame(in TensorShape shape1, in TensorShape shape2)
        {
            return AreLengthsTheSame(shape1.Lengths, shape2.Lengths);
        }

        public static bool AreLengthsTheSame(ReadOnlySpan<nint> lengths1, ReadOnlySpan<nint> lengths2)
        {
            return lengths1.SequenceEqual(lengths2);
        }

        public static TensorShape Create(Array? array)
        {
            if (array is not null)
            {
                nint linearLength = (nint)array.LongLength;
                int rank = array.Rank;

                nint[]? lengthsArray = null;
                InlineBuffer<nint> lengthsBuffer;
                scoped Span<nint> lengths;

                if (rank > MaxInlineRank)
                {
                    lengthsArray = ArrayPool<nint>.Shared.Rent(rank);
                    lengths = lengthsArray.AsSpan(0, rank);
                }
                else
                {
                    Unsafe.SkipInit(out lengthsBuffer);
                    lengths = ((Span<nint>)lengthsBuffer)[..rank];
                }

                for (int i = 0; i < rank; i++)
                {
                    lengths[i] = array.GetLength(i);
                }

                TensorShape result = new TensorShape(
                    linearLength,
                    lengths,
                    strides: [],
                    linearRankOrder: []
                );

                if (lengthsArray is not null)
                {
                    ArrayPool<nint>.Shared.Return(lengthsArray);
                }
                return result;
            }
            return default;
        }

        public static TensorShape Create(Array? array, scoped ReadOnlySpan<int> start, scoped ReadOnlySpan<nint> lengths, scoped ReadOnlySpan<nint> strides, out nint linearOffset)
        {
            nint computedOffset = 0;

            nint[]? intermediateLengthsArray = null;
            InlineBuffer<nint> intermediateLengthsBuffer;
            scoped Span<nint> intermediateLengths = default;

            if (array is not null)
            {
                int rank = array.Rank;

                if ((start.Length != 0) && (start.Length != rank))
                {
                    ThrowHelper.ThrowArgument_StartIndexOutOfBounds();
                }

                nint linearLength = (nint)array.LongLength;

                if (lengths.Length == 0)
                {
                    // When no lengths are specified we need to retrieve them from the array
                    // since that has the expected shape. We don't need to validate the strides
                    // however as that will be done by the TensorShape constructor.

                    if (rank > MaxInlineRank)
                    {
                        intermediateLengthsArray = ArrayPool<nint>.Shared.Rent(rank);
                        intermediateLengths = intermediateLengthsArray.AsSpan(0, rank);
                    }
                    else
                    {
                        Unsafe.SkipInit(out intermediateLengthsBuffer);
                        intermediateLengths = ((Span<nint>)intermediateLengthsBuffer)[..rank];
                    }

                    for (int i = 0; i < rank; i++)
                    {
                        intermediateLengths[i] = array.GetLength(i);
                    }

                    lengths = intermediateLengths;
                }

                if (start.Length != 0)
                {
                    // In the case a starting index is specified, we need to compute the linear
                    // index that is the actual starting position. Additionally, if no lengths
                    // were specified we want to adjust the lengths computed from the array to
                    // ensure they remain correct. However, we don't validate or adjust the lengths
                    // if they were user specified as we expect them to already be correct. This
                    // is because we allow users to do a "reshape" as part of construction and so
                    // the lengths and strides can mismatch what the underlying multidimensional
                    // array may have itself.

                    nint stride = 1;

                    for (int i = 0; i < start.Length; i++)
                    {
                        int index = start.Length - (i + 1);
                        int offset = start[index];
                        int length = array.GetLength(index);

                        if ((offset < 0) || (offset > length))
                        {
                            ThrowHelper.ThrowArgument_StartIndexOutOfBounds();
                        }

                        computedOffset += (offset * stride);
                        stride *= length;

                        if (intermediateLengths.Length != 0)
                        {
                            intermediateLengths[index] -= offset;
                        }
                    }
                }

                if ((computedOffset < 0) || (computedOffset > linearLength))
                {
                    ThrowHelper.ThrowArgument_StartIndexOutOfBounds();
                }

                TensorShape result = new TensorShape(linearLength - computedOffset, lengths, strides, linearRankOrder: []);

                if (intermediateLengthsArray is not null)
                {
                    ArrayPool<nint>.Shared.Return(intermediateLengthsArray);
                }
                linearOffset = computedOffset;
                return result;
            }
            else if (start.Length != 0)
            {
                ThrowHelper.ThrowArgument_StartIndexOutOfBounds();
            }

            if ((lengths.Length != 0) || (strides.Length != 0))
            {
                ThrowHelper.ThrowArgument_InvalidTensorShape();
            }
            linearOffset = computedOffset;
            return default;
        }

        public static TensorShape Create(scoped ReadOnlySpan<nint> lengths)
            => new TensorShape(linearLength: -1, lengths, strides: [], linearRankOrder: []);

        public static TensorShape Create(scoped ReadOnlySpan<nint> lengths, scoped ReadOnlySpan<nint> strides)
            => new TensorShape(linearLength: -1, lengths, strides, linearRankOrder: []);

        public static TensorShape Create<T>(T[]? array)
        {
            if (array is not null)
            {
                int linearLength = array.Length;

                return new TensorShape(
                    flattenedLength: linearLength,
                    linearLength,
                    lengths: [linearLength],
                    strides: [1],
                    linearRankOrder: [0],
                    rank: 1,
                    TensorFlags.IsDense | TensorFlags.HasAnyDenseDimensions
                );
            }
            return default;
        }

        public static TensorShape Create<T>(T[]? array, scoped ReadOnlySpan<nint> lengths)
        {
            if (array is not null)
            {
                int linearLength = array.Length;
                return new TensorShape(linearLength, lengths, strides: [], linearRankOrder: []);
            }

            if (lengths.Length != 0)
            {
                ThrowHelper.ThrowArgument_InvalidTensorShape();
            }
            return default;
        }

        public static TensorShape Create<T>(T[]? array, scoped ReadOnlySpan<nint> lengths, scoped ReadOnlySpan<nint> strides)
        {
            if (array is not null)
            {
                int linearLength = array.Length;
                return new TensorShape(linearLength, lengths, strides, linearRankOrder: []);
            }

            if ((lengths.Length != 0) || (strides.Length != 0))
            {
                ThrowHelper.ThrowArgument_InvalidTensorShape();
            }
            return default;
        }

        public static TensorShape Create<T>(T[]? array, int start, scoped ReadOnlySpan<nint> lengths, scoped ReadOnlySpan<nint> strides)
        {
            if (array is not null)
            {
                int linearLength = array.Length;

                if ((start < 0) || (start > linearLength))
                {
                    ThrowHelper.ThrowArgument_StartIndexOutOfBounds();
                }

                linearLength -= start;
                return new TensorShape(linearLength, lengths, strides, linearRankOrder: []);
            }
            else if (start != 0)
            {
                ThrowHelper.ThrowArgument_StartIndexOutOfBounds();
            }

            if ((lengths.Length != 0) || (strides.Length != 0))
            {
                ThrowHelper.ThrowArgument_InvalidTensorShape();
            }
            return default;
        }

        public static TensorShape Create<T>(T[]? array, int start, scoped ReadOnlySpan<nint> lengths, scoped ReadOnlySpan<nint> strides, scoped ReadOnlySpan<int> linearRankOrder)
        {
            if (array is not null)
            {
                int linearLength = array.Length;

                if ((start < 0) || (start > linearLength))
                {
                    ThrowHelper.ThrowArgument_StartIndexOutOfBounds();
                }

                linearLength -= start;
                return new TensorShape(linearLength, lengths, strides, linearRankOrder);
            }
            else if (start != 0)
            {
                ThrowHelper.ThrowArgument_StartIndexOutOfBounds();
            }

            if ((lengths.Length != 0) || (strides.Length != 0))
            {
                ThrowHelper.ThrowArgument_InvalidTensorShape();
            }
            return default;
        }

        public static TensorShape Create<T>(ref T reference, nint linearLength)
        {
            if (!Unsafe.IsNullRef(ref reference))
            {
                return new TensorShape(
                    flattenedLength: linearLength,
                    linearLength,
                    lengths: [linearLength],
                    strides: [1],
                    linearRankOrder: [0],
                    rank: 1,
                    TensorFlags.IsDense | TensorFlags.HasAnyDenseDimensions
                );
            }
            else if (linearLength != 0)
            {
                ThrowHelper.ThrowArgument_LengthIsNonZeroForNullReference();
            }
            return default;
        }

        public static TensorShape Create<T>(ref T reference, nint linearLength, scoped ReadOnlySpan<nint> lengths)
            => Create(ref reference, linearLength, lengths, strides: [], linearRankOrder: []);

        public static TensorShape Create<T>(ref T reference, nint linearLength, scoped ReadOnlySpan<nint> lengths, scoped ReadOnlySpan<nint> strides)
            => Create(ref reference, linearLength, lengths, strides, linearRankOrder: []);

        public static TensorShape Create<T>(ref T reference, nint linearLength, scoped ReadOnlySpan<nint> lengths, scoped ReadOnlySpan<nint> strides, scoped ReadOnlySpan<int> linearRankOrder)
        {
            if (!Unsafe.IsNullRef(ref reference))
            {
                return new TensorShape(linearLength, lengths, strides, linearRankOrder);
            }
            else if (linearLength != 0)
            {
                ThrowHelper.ThrowArgument_LengthIsNonZeroForNullReference();
            }

            if ((lengths.Length != 0) || (strides.Length != 0))
            {
                ThrowHelper.ThrowArgument_InvalidTensorShape();
            }
            return default;
        }

        public static unsafe TensorShape Create<T>(T* address, nint linearLength)
            => Create(ref Unsafe.AsRef<T>(address), linearLength);

        public static unsafe TensorShape Create<T>(T* address, nint linearLength, scoped ReadOnlySpan<nint> lengths)
            => Create(ref Unsafe.AsRef<T>(address), linearLength, lengths, strides: []);

        public static unsafe TensorShape Create<T>(T* address, nint linearLength, scoped ReadOnlySpan<nint> lengths, scoped ReadOnlySpan<nint> strides)
            => Create(ref Unsafe.AsRef<T>(address), linearLength, lengths, strides);

        public override bool Equals([NotNullWhen(true)] object? obj)
            => (obj is TensorShape other) && (this == other);

        public bool Equals(TensorShape other) => (this == other);

        public override int GetHashCode() => base.GetHashCode();

        [Conditional("DEBUG")]
        private void ValidateState()
        {
            if (IsDense)
            {
                // We don't assert !IsEmpty as a zero lengthed slice that
                // tracks a byref to physical memory will still be marked
                // as dense. This is in contrast to the default empty span
                // which is not.

                Debug.Assert(FlattenedLength == LinearLength);
                Debug.Assert(HasAnyDenseDimensions);
            }
            else
            {
                Debug.Assert(HasAnyDenseDimensions == CalculateHasAnyDenseDimensions(Lengths, Strides));
            }
            Debug.Assert(IsBroadcast == Strides.Contains(0));
        }

        public nint GetLinearOffset<TGetOffsetAndLength, T>(ReadOnlySpan<T> state)
            where TGetOffsetAndLength : IGetOffsetAndLength<T>
        {
            ReadOnlySpan<nint> lengths = Lengths;
            ReadOnlySpan<nint> strides = Strides;

            if ((state.Length != lengths.Length) ||
                (state.Length != strides.Length))
            {
                ThrowHelper.ThrowArgumentOutOfRangeException();
            }

            nint linearOffset = 0;

            for (int i = 0; i < state.Length; i++)
            {
                nint offset = TGetOffsetAndLength.GetOffset(state[i], lengths[i]);
                linearOffset += (offset * strides[i]);
            }

            return linearOffset;
        }

        public nint GetLinearOffset(nint index, int dimension)
        {
            ReadOnlySpan<nint> lengths = Lengths;
            ReadOnlySpan<nint> strides = Strides;

            if ((uint)dimension > (uint)lengths.Length)
            {
                ThrowHelper.ThrowArgumentOutOfRangeException();
            }

            nint linearOffset = 0;

            for (int i = 0; i < dimension; i++)
            {
                int rankIndex = dimension - (i + 1);

                nint length = lengths[rankIndex];
                (index, nint remainder) = nint.DivRem(index, length);

                linearOffset += (remainder * strides[rankIndex]);
            }

            return linearOffset;
        }

        public TensorShape Slice<TGetOffsetAndLength, T>(ReadOnlySpan<T> state, out nint linearOffset)
            where TGetOffsetAndLength : IGetOffsetAndLength<T>
        {
            int rank = Rank;

            nint[]? intermediateLengthsArray = null;
            InlineBuffer<nint> intermediateLengthsBuffer;
            scoped Span<nint> intermediateLengths;

            nint[]? intermediateStridesArray = null;
            InlineBuffer<nint> intermediateStridesBuffer;
            scoped Span<nint> intermediateStrides;

            if (rank > MaxInlineRank)
            {
                intermediateLengthsArray = ArrayPool<nint>.Shared.Rent(rank);
                intermediateLengths = intermediateLengthsArray.AsSpan(0, rank);

                intermediateStridesArray = ArrayPool<nint>.Shared.Rent(rank);
                intermediateStrides = intermediateStridesArray.AsSpan(0, rank);
            }
            else
            {
                Unsafe.SkipInit(out intermediateLengthsBuffer);
                intermediateLengths = ((Span<nint>)intermediateLengthsBuffer)[..rank];

                Unsafe.SkipInit(out intermediateStridesBuffer);
                intermediateStrides = ((Span<nint>)intermediateStridesBuffer)[..rank];
            }

            ReadOnlySpan<nint> previousLengths = Lengths;
            ReadOnlySpan<nint> previousStrides = Strides;
            ReadOnlySpan<int> linearRankOrder = LinearRankOrder;

            if ((state.Length != previousLengths.Length) ||
                (state.Length != linearRankOrder.Length) ||
                (state.Length != previousStrides.Length))
            {
                ThrowHelper.ThrowArgumentOutOfRangeException();
            }

            // The previous strides and rank order persist in the new shape with
            // only the lengths having changed based on the new starting index.
            //
            // Accordingly, we can also simplify some of the checks as we can
            // assume that the previousShape is already valid and the new shape
            // will strictly be the same size or smaller.

            TensorFlags flags = TensorFlags.None;

            nint flattenedLength = 1;
            nint maximumLinearIndex = 0;
            nint minimumNonZeroStride = 1;

            nint computedOffset = 0;
            bool isDense = true;

            for (int i = 0; i < linearRankOrder.Length; i++)
            {
                int rankIndex = linearRankOrder.Length - (i + 1);
                int linearRankIndex = linearRankOrder[rankIndex];

                if (rankIndex != linearRankIndex)
                {
                    // We cannot be dense if we aren't following linear order
                    isDense = false;
                }

                nint previousLength = previousLengths[linearRankIndex];
                nint previousStride = previousStrides[linearRankIndex];

                (nint offset, nint length) = TGetOffsetAndLength.GetOffsetAndLength(state[linearRankIndex], previousLength);
                nint stride = (length > 1) ? previousStride : 0;

                if (stride != 0)
                {
                    maximumLinearIndex += ((length - 1) * stride);

                    if (stride != minimumNonZeroStride)
                    {
                        isDense = false;
                    }
                }
                else
                {
                    flags |= TensorFlags.IsBroadcast;

                    if (length != 1)
                    {
                        // We only cannot be dense if the broadcast is for more than 1 element
                        isDense = false;
                    }
                }

                intermediateLengths[linearRankIndex] = length;
                intermediateStrides[linearRankIndex] = stride;

                minimumNonZeroStride = stride * length;

                computedOffset += (offset * previousStride);
                flattenedLength *= length;
            }

            // We've computed the maximum linear index based on the strides
            // so the minimum length must be one higher than that value.

            nint minimumLinearLength = (flattenedLength != 0) ? (maximumLinearIndex + 1) : flattenedLength;
            Debug.Assert(minimumLinearLength <= _linearLength);

            if (isDense)
            {
                flags |= (TensorFlags.IsDense | TensorFlags.HasAnyDenseDimensions);
            }
            else if (CalculateHasAnyDenseDimensions(intermediateLengths, intermediateStrides))
            {
                flags |= TensorFlags.HasAnyDenseDimensions;
            }

            TensorShape result = new TensorShape(
                flattenedLength,
                minimumLinearLength,
                intermediateLengths,
                intermediateStrides,
                linearRankOrder,
                rank,
                flags
            );

            if (intermediateLengthsArray is not null)
            {
                ArrayPool<nint>.Shared.Return(intermediateLengthsArray);
            }

            if (intermediateStridesArray is not null)
            {
                ArrayPool<nint>.Shared.Return(intermediateStridesArray);
            }

            Debug.Assert(computedOffset == GetLinearOffset<TGetOffsetAndLength, T>(state));
            linearOffset = computedOffset;

            return result;
        }

        private static bool CalculateHasAnyDenseDimensions(ReadOnlySpan<nint> lengths, ReadOnlySpan<nint> strides)
        {
            // We aren't dense, but we might still have some dense dimension if
            // the least significant non 0 stride is 1 and all least significant
            // 0 strides have length 1. We cannot have a dense dimension for a
            // non-zero stride with more than 1 element since we cannot get a
            // single span for all elements in that index of the dimension.

            bool hasAnyDenseDimensions = false;

            for (int i = 0; i < strides.Length; i++)
            {
                int index = strides.Length - (i + 1);
                nint stride = strides[index];

                if (stride == 1)
                {
                    hasAnyDenseDimensions = true;
                    break;
                }

                if (stride != 0)
                {
                    break;
                }

                nint length = lengths[index];

                if (length != 1)
                {
                    break;
                }
            }

            return hasAnyDenseDimensions;
        }

        public interface IGetOffsetAndLength<T>
        {
            static abstract nint GetOffset(T state, nint length);
            static abstract (nint Offset, nint Length) GetOffsetAndLength(T state, nint length);
        }

        [InlineArray(MaxInlineRank)]
        public struct InlineBuffer<T>
        {
            public T e0;
        }

        public readonly struct GetOffsetAndLengthForNInt : IGetOffsetAndLength<nint>
        {
            public static nint GetOffset(nint index, nint length)
            {
                nint offset = index;

                if ((offset < 0) || (offset >= length))
                {
                    ThrowHelper.ThrowIndexOutOfRangeException();
                }
                return offset;
            }

            public static (nint Offset, nint Length) GetOffsetAndLength(nint index, nint length)
            {
                nint offset = GetOffset(index, length);
                return (offset, length - offset);
            }
        }

        public readonly struct GetOffsetAndLengthForNIndex : IGetOffsetAndLength<NIndex>
        {
            public static nint GetOffset(NIndex index, nint length)
            {
                nint offset = index.GetOffset(length);

                if ((offset < 0) || (offset >= length))
                {
                    ThrowHelper.ThrowIndexOutOfRangeException();
                }
                return offset;
            }

            public static (nint Offset, nint Length) GetOffsetAndLength(NIndex index, nint length)
            {
                nint offset = GetOffset(index, length);
                return (offset, length - offset);
            }
        }

        public readonly struct GetOffsetAndLengthForNRange : IGetOffsetAndLength<NRange>
        {
            public static nint GetOffset(NRange range, nint length)
            {
                return range.Start.GetOffset(length);
            }

            public static (nint Offset, nint Length) GetOffsetAndLength(NRange range, nint length)
            {
                return range.GetOffsetAndLength(length);
            }
        }
    }
}
