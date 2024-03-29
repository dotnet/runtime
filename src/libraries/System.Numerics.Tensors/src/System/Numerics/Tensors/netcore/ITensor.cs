// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace System.Numerics.Tensors
{
    public interface ITensor<TSelf, T>
        : IEnumerable<T>,
          IEquatable<TSelf>,
          IEqualityOperators<TSelf, TSelf, bool>
        where TSelf : ITensor<TSelf, T>
        where T : IEquatable<T>
    {
        // TODO: Determine if we can implement `IEqualityOperators<TSelf, T, bool>`.
        // It looks like C#/.NET currently hits limitations here as it believes TSelf and T could be the same type
        // Ideally we could annotate it such that they cannot be the same type and no conflicts would exist

        // REVIEW: SHOULD THIS BE NULLABLE OR SUPPRESS ISSUE?
        static TSelf? Empty { get; }

        bool IsEmpty { get; }
        bool IsPinned { get; }
        int Rank { get; }
        ReadOnlySpan<nint> Strides { get; }

        ref T this[params nint[] indices] { get; }
        ref T this[ReadOnlySpan<nint> indices] { get; }

        static abstract implicit operator SpanND<T>(TSelf value);
        static abstract implicit operator ReadOnlySpanND<T>(TSelf value);

        SpanND<T> AsSpan(params NativeRange[] ranges);
        ReadOnlySpanND<T> AsReadOnlySpan(params NativeRange[] ranges);
        ref T GetPinnableReference();
        TSelf Slice(params NativeRange[] ranges);

        void Clear();
        void CopyTo(SpanND<T> destination);
        void Fill(T value);
        bool TryCopyTo(SpanND<T> destination);

        // IEqualityOperators

        static abstract bool operator ==(TSelf left, TSelf right);
        static abstract bool operator ==(TSelf left, T right);

        static abstract bool operator !=(TSelf left, TSelf right);
        static abstract bool operator !=(TSelf left, T right);
    }
}
