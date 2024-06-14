﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using System.Collections.Generic;

namespace System.Numerics.Tensors
{
    public interface IReadOnlyTensor<TSelf, T> : IEnumerable<T>
        where TSelf : IReadOnlyTensor<TSelf, T>
    {
        static abstract TSelf? Empty { get; }

        bool IsEmpty { get; }
        bool IsPinned { get; }
        nint FlattenedLength { get; }
        int Rank { get; }

        T this[params scoped ReadOnlySpan<nint> indexes] { get; }
        T this[params scoped ReadOnlySpan<NIndex> indexes] { get; }
        TSelf this[params scoped ReadOnlySpan<NRange> ranges] { get; }

        ReadOnlyTensorSpan<T> AsReadOnlyTensorSpan();
        ReadOnlyTensorSpan<T> AsReadOnlyTensorSpan(params scoped ReadOnlySpan<nint> start);
        ReadOnlyTensorSpan<T> AsReadOnlyTensorSpan(params scoped ReadOnlySpan<NIndex> startIndex);
        ReadOnlyTensorSpan<T> AsReadOnlyTensorSpan(params scoped ReadOnlySpan<NRange> range);

        void CopyTo(scoped TensorSpan<T> destination);
        void FlattenTo(scoped Span<T> destination);

        // These are not properties so that structs can implement the interface without allocating:
        void GetLengths(scoped Span<nint> destination);
        void GetStrides(scoped Span<nint> destination);

        ref readonly T GetPinnableReference();
        TSelf Slice(params scoped ReadOnlySpan<nint> start);
        TSelf Slice(params scoped ReadOnlySpan<NIndex> startIndex);
        TSelf Slice(params scoped ReadOnlySpan<NRange> range);
        bool TryCopyTo(scoped TensorSpan<T> destination);
        bool TryFlattenTo(scoped Span<T> destination);
    }
}
