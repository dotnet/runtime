// Licensed to the .NET Foundation under one or more agreements.
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
        : IEnumerable<T>,
          IEquatable<TSelf>,
          IEqualityOperators<TSelf, TSelf, bool>
        where TSelf : ITensor<TSelf, T>
    {
        // TODO: Determine if we can implement `IEqualityOperators<TSelf, T, bool>`.
        // It looks like C#/.NET currently hits limitations here as it believes TSelf and T could be the same type
        // Ideally we could annotate it such that they cannot be the same type and no conflicts would exist

        static TSelf? Empty { get; }

        bool IsEmpty { get; }
        bool IsPinned { get; }
        int Rank { get; }
        // THIS IS GOING TO PEND A DISCUSSION WITH THE LANGUAGE TEAM
        ReadOnlySpan<nint> Strides { get; }

        ref T this[params scoped ReadOnlySpan<nint> indices] { get; }

        static abstract implicit operator TensorSpan<T>(TSelf value);
        static abstract implicit operator ReadOnlyTensorSpan<T>(TSelf value);

        TensorSpan<T> AsTensorSpan(params scoped ReadOnlySpan<NRange> ranges);
        ReadOnlyTensorSpan<T> AsReadOnlyTensorSpan(params scoped ReadOnlySpan<NRange> ranges);
        ref T GetPinnableReference();
        TSelf Slice(params scoped ReadOnlySpan<NRange> ranges);

        void Clear();
        void CopyTo(TensorSpan<T> destination);
        void Fill(T value);
        bool TryCopyTo(TensorSpan<T> destination);

        // IEqualityOperators

        static abstract bool operator ==(TSelf left, TSelf right);
        static abstract bool operator ==(TSelf left, T right);

        static abstract bool operator !=(TSelf left, TSelf right);
        static abstract bool operator !=(TSelf left, T right);
    }
}
