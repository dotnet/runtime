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

        public override int GetHashCode() => HashCode.Combine(Value1, Value2);
    }

    internal partial struct StringSequence3 : IEquatable<StringSequence3>, IStringSequence
    {
        public Span<string> AsSpan()
        {
            return MemoryMarshal.CreateSpan(ref Value1, 3);
        }

        public override int GetHashCode() => HashCode.Combine(Value1, Value2, Value3);
    }

    internal partial struct StringSequenceMany : IEquatable<StringSequenceMany>, IStringSequence
    {
        public override int GetHashCode()
        {
            HashCode h = default;
            for (int i = 0; i < _values.Length; i++)
            {
                h.Add(_values[i]);
            }
            return h.ToHashCode();
        }
    }
}
