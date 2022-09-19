// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

namespace System.Reflection.Internal
{
    internal sealed class ByteSequenceComparer : IEqualityComparer<byte[]>, IEqualityComparer<ImmutableArray<byte>>
    {
        internal static readonly ByteSequenceComparer Instance = new ByteSequenceComparer();

        private ByteSequenceComparer()
        {
        }

        internal static bool Equals(ImmutableArray<byte> x, ImmutableArray<byte> y)
        {
            return x.AsSpan().SequenceEqual(y.AsSpan());
        }

        internal static bool Equals(byte[] left, int leftStart, byte[] right, int rightStart, int length)
        {
            return left.AsSpan(leftStart, length).SequenceEqual(right.AsSpan(rightStart, length));
        }

        internal static bool Equals(byte[]? left, byte[]? right)
        {
            return left.AsSpan().SequenceEqual(right.AsSpan());
        }

        // Both hash computations below use the FNV-1a algorithm (http://en.wikipedia.org/wiki/Fowler%E2%80%93Noll%E2%80%93Vo_hash_function).

        internal static int GetHashCode(byte[] x)
        {
            Debug.Assert(x != null);
            return Hash.GetFNVHashCode(x);
        }

        internal static int GetHashCode(ImmutableArray<byte> x)
        {
            Debug.Assert(!x.IsDefault);
            return Hash.GetFNVHashCode(x.AsSpan());
        }

        bool IEqualityComparer<byte[]>.Equals(byte[]? x, byte[]? y)
        {
            return Equals(x, y);
        }

        int IEqualityComparer<byte[]>.GetHashCode(byte[] x)
        {
            return GetHashCode(x);
        }

        bool IEqualityComparer<ImmutableArray<byte>>.Equals(ImmutableArray<byte> x, ImmutableArray<byte> y)
        {
            return Equals(x, y);
        }

        int IEqualityComparer<ImmutableArray<byte>>.GetHashCode(ImmutableArray<byte> x)
        {
            return GetHashCode(x);
        }
    }
}
