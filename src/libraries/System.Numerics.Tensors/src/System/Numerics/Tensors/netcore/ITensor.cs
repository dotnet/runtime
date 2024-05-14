﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Buffers;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace System.Numerics.Tensors
{
    public interface ITensor<TSelf, T>
        : IReadOnlyTensor<TSelf, T>
        where TSelf : ITensor<TSelf, T>
    {
        // TODO: Determine if we can implement `IEqualityOperators<TSelf, T, bool>`.
        // It looks like C#/.NET currently hits limitations here as it believes TSelf and T could be the same type
        // Ideally we could annotate it such that they cannot be the same type and no conflicts would exist

        static abstract TSelf Create(scoped ReadOnlySpan<nint> lengths, bool pinned = false);
        static abstract TSelf Create(scoped ReadOnlySpan<nint> lengths, scoped ReadOnlySpan<nint> strides, bool pinned = false);

        static abstract TSelf CreateUninitialized(scoped ReadOnlySpan<nint> lengths, bool pinned = false);
        static abstract TSelf CreateUninitialized(scoped ReadOnlySpan<nint> lengths, scoped ReadOnlySpan<nint> strides, bool pinned = false);

        bool IsReadOnly { get; }

        new T this[params ReadOnlySpan<nint> indexes] { get; set; }
        new T this[params ReadOnlySpan<NIndex> indexes] { get; set; }
        new TSelf this[params scoped ReadOnlySpan<NRange> ranges] { get; set; }

        TensorSpan<T> AsTensorSpan();
        TensorSpan<T> AsTensorSpan(params scoped ReadOnlySpan<nint> start);
        TensorSpan<T> AsTensorSpan(params scoped ReadOnlySpan<NIndex> startIndex);
        TensorSpan<T> AsTensorSpan(params scoped ReadOnlySpan<NRange> range);

        void Clear();
        void Fill(T value);
        new ref T GetPinnableReference();
    }
}
