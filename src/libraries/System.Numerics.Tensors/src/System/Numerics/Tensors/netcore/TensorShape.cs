﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
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
    // backing storage size that is present. This gives us the following invariants:
    //   * linearLength <= flattenedLength
    //   * (flattenedLength % linearLength) == 0
    //
    // These invariants allow us to safely and efficiently index into the backing storage
    // as we know that memory functionally wraps around after linearLength elements. It
    // also means that we only support broadcasting to greater dimensions and thus lengths
    // strides and the backing memory form a strict relationship that is validated by the
    // public constructors.

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

        private readonly int _rank;                                 // 4 bytes

        private TensorShape(nint linearLength, scoped ReadOnlySpan<nint> lengths, scoped ReadOnlySpan<nint> strides, scoped ReadOnlySpan<int> linearRankOrder)
        {
            int rank = lengths.Length;

            if (rank == 0)
            {
                lengths = [linearLength];
                rank = 1;
            }

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

            if (strides.Length == 0)
            {
                // When no strides is specified, we need to computing them based on the given
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

                    if (length <= 0)
                    {
                        ThrowHelper.ThrowArgument_LengthIsNegativeOrZero();
                    }

                    flattenedLength = checked(flattenedLength * length);

                    destinationLengths[linearRankIndex] = length;
                    destinationStrides[linearRankIndex] = flattenedLength;
                }
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

                int i = 0;

                while (i < destinationLinearRankOrder.Length)
                {
                    int rankIndex = destinationLinearRankOrder.Length - (i + 1);
                    int linearRankIndex = destinationLinearRankOrder[rankIndex];
                    nint length = lengths[linearRankIndex];

                    if (length <= 0)
                    {
                        ThrowHelper.ThrowArgument_LengthIsNegativeOrZero();
                    }

                    nint stride = strides[linearRankIndex];

                    if (stride < 0)
                    {
                        ThrowHelper.ThrowArgument_StrideIsNegative();
                    }

                    if (stride == 0)
                    {
                        // We end up handling i twice due to the break here
                        // but this shouldn't be significant and makes it
                        // easier to ensure that the flattened length is correct.
                        break;
                    }

                    flattenedLength = checked(flattenedLength * length);

                    destinationLengths[linearRankIndex] = length;
                    destinationStrides[linearRankIndex] = flattenedLength;

                    i++;
                }

                if (linearLength == -1)
                {
                    linearLength = flattenedLength;
                }
                else
                {
                    ArgumentOutOfRangeException.ThrowIfNotEqual(linearLength, flattenedLength);
                }

                while (i < destinationLinearRankOrder.Length)
                {
                    int rankIndex = destinationLinearRankOrder.Length - (i + 1);
                    int linearRankIndex = destinationLinearRankOrder[rankIndex];
                    nint length = lengths[linearRankIndex];

                    if (length <= 0)
                    {
                        ThrowHelper.ThrowArgument_LengthIsNegativeOrZero();
                    }

                    nint stride = strides[linearRankIndex];
                    ArgumentOutOfRangeException.ThrowIfNotEqual(stride, 0);

                    flattenedLength = checked(flattenedLength * length);

                    destinationLengths[linearRankIndex] = length;
                    destinationStrides[linearRankIndex] = 0;

                    i++;
                }
            }

            Debug.Assert((flattenedLength % linearLength) == 0);

            _flattenedLength = flattenedLength;
            _linearLength = linearLength;

            _rank = rank;
        }

        private TensorShape(nint flattenedLength, nint linearLength, scoped ReadOnlySpan<nint> lengths, scoped ReadOnlySpan<nint> strides, scoped ReadOnlySpan<int> linearRankOrder, int rank)
        {
            Debug.Assert((flattenedLength % linearLength) == 0);

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
        }

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

            for (int i = 0; i < strides.Length; i++)
            {
                int rankIndex = lengths.Length - (i + 1);

                nint length = lengths[rankIndex];
                nint stride = strides[rankIndex];

                nint index = ++indexes[rankIndex];
                linearOffset += stride;

                if (index < length)
                {
                    return linearOffset;
                }

                indexes[rankIndex] = 0;
                linearOffset -= (stride * length);
            }

            if (indexes.Length != Rank)
            {
                lengths = destinationShape.Lengths;
                for (int i = strides.Length; i < indexes.Length; i++)
                {
                    int rankIndex = lengths.Length - (i + 1);

                    nint length = lengths[rankIndex];
                    // Strides are always 0 because we are broadcasting at this point in the loop.

                    nint index = ++indexes[rankIndex];

                    if (index < length)
                    {
                        break;
                    }

                    indexes[rankIndex] = 0;
                }
            }

            return 0;
        }

        // can shape2 turn into shape1
        public static bool AreCompatible(in TensorShape shape1, in TensorShape shape2, bool allowBidirectional)
        {
            scoped ReadOnlySpan<nint> lengths1 = shape1.Lengths;
            scoped ReadOnlySpan<nint> lengths2 = shape2.Lengths;

            int rankDelta = shape1.Rank - shape2.Rank;

            if (rankDelta != 0)
            {
                if (rankDelta < 0)
                {
                    if (!allowBidirectional)
                    {
                        return false;
                    }

                    lengths1 = shape2.Lengths;
                    lengths2 = shape1.Lengths;

                    rankDelta = -rankDelta;
                    Debug.Assert(rankDelta > 0);
                }

                lengths1 = lengths1[rankDelta..];
            }

            // if equal or one is 1
            for (int i = 0; i < lengths1.Length; i++)
            {
                nint length1 = lengths1[i];
                nint length2 = lengths2[i];
                if ((length1 != length2) && (length1 != 1) && (length2 != 1))
                {
                    return false;
                }
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

                if (linearLength != 0)
                {
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
                        flattenedLength: linearLength,
                        linearLength,
                        lengths,
                        strides: [],
                        linearRankOrder: [],
                        rank
                    );

                    if (lengthsArray is not null)
                    {
                        ArrayPool<nint>.Shared.Return(lengthsArray);
                    }
                    return result;
                }
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

                if (linearLength != 0)
                {
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

        public static TensorShape Create(scoped ReadOnlySpan<nint> lengths, scoped ReadOnlySpan<nint> strides)
            => new TensorShape(linearLength: -1, lengths, strides, linearRankOrder: []);

        public static TensorShape Create<T>(T[]? array)
        {
            if (array is not null)
            {
                int linearLength = array.Length;

                if (linearLength != 0)
                {
                    return new TensorShape(
                        flattenedLength: linearLength,
                        linearLength,
                        lengths: [linearLength],
                        strides: [1],
                        linearRankOrder: [0],
                        rank: 1
                    );
                }
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

                if (linearLength != 0)
                {
                    return new TensorShape(linearLength, lengths, strides, linearRankOrder: []);
                }
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
                if (linearLength != 0)
                {
                    return new TensorShape(
                        flattenedLength: linearLength,
                        linearLength,
                        lengths: [linearLength],
                        strides: [1],
                        linearRankOrder: [0],
                        rank: 1
                    );
                }
            }
            else if (linearLength != 0)
            {
                ThrowHelper.ThrowArgument_LengthIsNonZeroForNullReference();
            }
            return default;
        }

        public static TensorShape Create<T>(ref T reference, nint linearLength, scoped ReadOnlySpan<nint> lengths, scoped ReadOnlySpan<nint> strides)
        {
            if (!Unsafe.IsNullRef(ref reference))
            {
                if (linearLength != 0)
                {
                    return new TensorShape(linearLength, lengths, strides, linearRankOrder: []);
                }
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

        public static unsafe TensorShape Create<T>(T* address, nint linearLength, scoped ReadOnlySpan<nint> lengths, scoped ReadOnlySpan<nint> strides)
            => Create(ref Unsafe.AsRef<T>(address), linearLength, lengths, strides);

        public override bool Equals([NotNullWhen(true)] object? obj)
            => (obj is TensorShape other) && (this == other);

        public bool Equals(TensorShape other) => (this == other);

        public override int GetHashCode() => base.GetHashCode();

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
                nint length = lengths[i];
                nint stride = strides[i];

                nint offset = TGetOffsetAndLength.GetOffset(state, i, length);
                linearOffset += (offset * stride);
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

            ReadOnlySpan<nint> previousLengths = Lengths;
            ReadOnlySpan<nint> strides = Strides;
            ReadOnlySpan<int> linearRankOrder = LinearRankOrder;

            if ((state.Length != previousLengths.Length) ||
                (state.Length != linearRankOrder.Length) ||
                (state.Length != strides.Length))
            {
                ThrowHelper.ThrowArgumentOutOfRangeException();
            }

            // The previous strides and rank order persist in the new shape with
            // only the lengths having changed based on the new starting index.
            //
            // Accordingly, we can also simplify some of the checks as we can
            // assume that the previousShape is already valid and the new shape
            // will strictly be the same size or smaller.

            nint flattenedLength = 0;
            nint linearLength = 0;
            nint computedOffset = 0;

            for (int i = 0; i < state.Length; i++)
            {
                int rankIndex = state.Length - (i + 1);
                int linearRankIndex = linearRankOrder[rankIndex];

                nint previousLength = previousLengths[linearRankIndex];
                nint stride = strides[linearRankIndex];

                (nint offset, nint length) = TGetOffsetAndLength.GetOffsetAndLength(state, linearRankIndex, previousLength);
                flattenedLength *= length;

                intermediateLengths[linearRankIndex] = length;
                computedOffset += (offset * stride);
            }

            TensorShape result = new TensorShape(
                flattenedLength,
                linearLength,
                intermediateLengths,
                strides,
                linearRankOrder,
                rank
            );

            if (intermediateLengthsArray is not null)
            {
                ArrayPool<nint>.Shared.Return(intermediateLengthsArray);
            }

            linearOffset = computedOffset;
            return result;
        }

        public interface IGetOffsetAndLength<T>
        {
            static abstract nint GetOffset(ReadOnlySpan<T> state, int rankIndex, nint previousLength);
            static abstract (nint Offset, nint Length) GetOffsetAndLength(ReadOnlySpan<T> state, int rankIndex, nint previousLength);
        }

        [InlineArray(MaxInlineRank)]
        public struct InlineBuffer<T>
        {
            public T e0;
        }

        public readonly struct GetOffsetAndLengthForNInt : IGetOffsetAndLength<nint>
        {
            public static nint GetOffset(ReadOnlySpan<nint> indexes, int rankIndex, nint previousLength)
            {
                nint offset = indexes[rankIndex];

                if ((offset < 0) || (offset >= previousLength))
                {
                    ThrowHelper.ThrowArgumentOutOfRangeException();
                }

                return offset;
            }

            public static (nint Offset, nint Length) GetOffsetAndLength(ReadOnlySpan<nint> indexes, int rankIndex, nint previousLength)
            {
                nint offset = GetOffset(indexes, rankIndex, previousLength);
                return (offset, previousLength - offset);
            }
        }

        public readonly struct GetOffsetAndLengthForNIndex : IGetOffsetAndLength<NIndex>
        {
            public static nint GetOffset(ReadOnlySpan<NIndex> indexes, int rankIndex, nint previousLength)
            {
                nint offset = indexes[rankIndex].GetOffset(previousLength);

                if ((offset < 0) || (offset >= previousLength))
                {
                    ThrowHelper.ThrowArgumentOutOfRangeException();
                }

                return offset;
            }

            public static (nint Offset, nint Length) GetOffsetAndLength(ReadOnlySpan<NIndex> indexes, int rankIndex, nint previousLength)
            {
                nint offset = GetOffset(indexes, rankIndex, previousLength);
                return (offset, previousLength - offset);
            }
        }

        public readonly struct GetOffsetAndLengthForNRange : IGetOffsetAndLength<NRange>
        {
            public static nint GetOffset(ReadOnlySpan<NRange> ranges, int rankIndex, nint previousLength)
            {
                return ranges[rankIndex].Start.GetOffset(previousLength);
            }

            public static (nint Offset, nint Length) GetOffsetAndLength(ReadOnlySpan<NRange> ranges, int rankIndex, nint previousLength)
            {
                return ranges[rankIndex].GetOffsetAndLength(previousLength);
            }
        }
    }
}
