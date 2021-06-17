// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Numerics;
using System.Runtime.InteropServices;

namespace System.Diagnostics.Metrics
{
    internal interface IStringSequence
    {
        Span<string> AsSpan();
    }

    internal partial struct StringSequence1 : IEquatable<StringSequence1>, IStringSequence
    {

        public Span<string> AsSpan()
        {
            return MemoryMarshal.CreateSpan(ref Value1, 1);
        }
    }

    internal partial struct StringSequence2 : IEquatable<StringSequence2>, IStringSequence
    {
        public Span<string> AsSpan()
        {
            return MemoryMarshal.CreateSpan(ref Value1, 2);
        }

        public override int GetHashCode() => HashCode.Combine(Value1.GetHashCode(), Value2.GetHashCode());
    }

    internal partial struct StringSequence3 : IEquatable<StringSequence3>, IStringSequence
    {
        public Span<string> AsSpan()
        {
            return MemoryMarshal.CreateSpan(ref Value1, 3);
        }

        public override int GetHashCode() => HashCode.Combine(Value1.GetHashCode(), Value2.GetHashCode(), Value3.GetHashCode());
    }

    internal partial struct StringSequenceMany : IEquatable<StringSequenceMany>, IStringSequence
    {
        public override int GetHashCode()
        {
            int hash = 0;
            for (int i = 0; i < _values.Length; i++)
            {
                hash = (int)BitOperations.RotateLeft((uint)hash, 3);
                hash ^= _values[i].GetHashCode();
            }
            return hash;
        }
    }
}
