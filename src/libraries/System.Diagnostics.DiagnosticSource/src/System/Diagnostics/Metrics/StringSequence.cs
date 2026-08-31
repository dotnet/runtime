// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Numerics;
using System.Runtime.InteropServices;

namespace System.Diagnostics.Metrics
{
    internal partial struct StringSequence1 : IEquatable<StringSequence1>, IStringSequence
    {
        public string Value1;

        public StringSequence1(string value1)
        {
            Value1 = value1;
        }

        public override int GetHashCode() => Value1.GetHashCode();

        public bool Equals(StringSequence1 other)
        {
            return Value1 == other.Value1;
        }

        //GetHashCode() is in the platform specific files
        public override bool Equals(object? obj)
        {
            return obj is StringSequence1 ss1 && Equals(ss1);
        }
    }

    internal partial struct StringSequence2 : IEquatable<StringSequence2>, IStringSequence
    {
        public string Value1;
        public string Value2;

        public StringSequence2(string value1, string value2)
        {
            Value1 = value1;
            Value2 = value2;
        }

        public bool Equals(StringSequence2 other)
        {
            return Value1 == other.Value1 && Value2 == other.Value2;
        }

        //GetHashCode() is in the platform specific files
        public override bool Equals(object? obj)
        {
            return obj is StringSequence2 ss2 && Equals(ss2);
        }
    }

    internal partial struct StringSequence3 : IEquatable<StringSequence3>, IStringSequence
    {
        public string Value1;
        public string Value2;
        public string Value3;

        public StringSequence3(string value1, string value2, string value3)
        {
            Value1 = value1;
            Value2 = value2;
            Value3 = value3;
        }

        public bool Equals(StringSequence3 other)
        {
            return Value1 == other.Value1 && Value2 == other.Value2 && Value3 == other.Value3;
        }

        //GetHashCode() is in the platform specific files
        public override bool Equals(object? obj)
        {
            return obj is StringSequence3 ss3 && Equals(ss3);
        }
    }

    internal partial struct StringSequenceMany : IEquatable<StringSequenceMany>, IStringSequence
    {
        private readonly string[] _values;

        public StringSequenceMany(string[] values) =>
            _values = values;

        public Span<string> AsSpan() =>
            _values.AsSpan();

        public bool Equals(StringSequenceMany other) =>
            _values.AsSpan().SequenceEqual(other._values.AsSpan());

        //GetHashCode() is in the platform specific files
        public override bool Equals(object? obj) =>
            obj is StringSequenceMany ssm && Equals(ssm);
    }
}
