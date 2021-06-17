// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Numerics;
using System.Runtime.InteropServices;

namespace System.Diagnostics.Metrics
{
    internal interface IObjectSequence
    {
        Span<object?> AsSpan();
    }

    internal partial struct ObjectSequence1 : IEquatable<ObjectSequence1>, IObjectSequence
    {
        public Span<object?> AsSpan()
        {
            return MemoryMarshal.CreateSpan(ref Value1, 1);
        }
    }

    internal partial struct ObjectSequence2 : IEquatable<ObjectSequence2>, IObjectSequence
    {
        public Span<object?> AsSpan()
        {
            return MemoryMarshal.CreateSpan(ref Value1, 2);
        }

        public override int GetHashCode() => HashCode.Combine(Value1?.GetHashCode(), Value2?.GetHashCode());
    }

    internal partial struct ObjectSequence3 : IEquatable<ObjectSequence3>, IObjectSequence
    {
        public Span<object?> AsSpan()
        {
            return MemoryMarshal.CreateSpan(ref Value1, 3);
        }

        public override int GetHashCode() => HashCode.Combine(Value1?.GetHashCode(), Value2?.GetHashCode(), Value3?.GetHashCode());
    }

    internal partial struct ObjectSequenceMany : IEquatable<ObjectSequenceMany>, IObjectSequence
    {

        public Span<object?> AsSpan()
        {
            return _values.AsSpan();
        }

        public override int GetHashCode()
        {
            int hash = 0;
            for (int i = 0; i < _values.Length; i++)
            {
                hash = (int)BitOperations.RotateLeft((uint)hash, 3);
                object? value = _values[i];
                if (value != null)
                {
                    hash ^= value.GetHashCode();
                }
            }
            return hash;
        }
    }
}
