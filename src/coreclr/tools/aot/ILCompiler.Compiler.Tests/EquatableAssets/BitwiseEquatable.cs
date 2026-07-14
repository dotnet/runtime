// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

#pragma warning disable 649  // field is never assigned to
#pragma warning disable 660  // defines operator == but not Equals
#pragma warning disable 661  // defines operator == but not GetHashCode

namespace BitwiseEquatable
{
    // Positive cases: value types whose IEquatable<T>.Equals is a plain field-wise comparison. These
    // follow the standard pattern of also overriding object.Equals/GetHashCode; that override is
    // irrelevant to bitwise equatability because the IEquatable<T>.Equals is what gets used.

    public struct OneField : IEquatable<OneField>
    {
        public int A;
        public bool Equals(OneField other) => A == other.A;
        public override bool Equals(object obj) => obj is OneField other && Equals(other);
        public override int GetHashCode() => A;
    }

    public struct TwoFields : IEquatable<TwoFields>
    {
        public int A;
        public int B;
        public bool Equals(TwoFields other) => A == other.A && B == other.B;
        public override bool Equals(object obj) => obj is TwoFields other && Equals(other);
        public override int GetHashCode() => A;
    }

    public struct MixedPrimitives : IEquatable<MixedPrimitives>
    {
        public long A;
        public int B;
        public short C;
        public short D;
        public bool Equals(MixedPrimitives other) => A == other.A && B == other.B && C == other.C && D == other.D;
        public override bool Equals(object obj) => obj is MixedPrimitives other && Equals(other);
        public override int GetHashCode() => B;
    }

    public struct ForwardsToOp : IEquatable<ForwardsToOp>
    {
        public int A;
        public int B;
        public static bool operator ==(ForwardsToOp x, ForwardsToOp y) => x.A == y.A && x.B == y.B;
        public static bool operator !=(ForwardsToOp x, ForwardsToOp y) => !(x == y);
        public bool Equals(ForwardsToOp other) => this == other;
        public override bool Equals(object obj) => obj is ForwardsToOp other && Equals(other);
        public override int GetHashCode() => A;
    }

    // Recursive cases: a nested value-type field is compared through its own IEquatable<F>.Equals.

    public struct NestedField : IEquatable<NestedField>
    {
        public OneField A;
        public int B;
        public bool Equals(NestedField other) => A.Equals(other.A) && B == other.B;
        public override bool Equals(object obj) => obj is NestedField other && Equals(other);
        public override int GetHashCode() => B;
    }

    public struct NestedFieldLast : IEquatable<NestedFieldLast>
    {
        public int A;
        public TwoFields B;
        public bool Equals(NestedFieldLast other) => A == other.A && B.Equals(other.B);
        public override bool Equals(object obj) => obj is NestedFieldLast other && Equals(other);
        public override int GetHashCode() => A;
    }

    public struct AllNested : IEquatable<AllNested>
    {
        public OneField A;
        public TwoFields B;
        public bool Equals(AllNested other) => A.Equals(other.A) && B.Equals(other.B);
        public override bool Equals(object obj) => obj is AllNested other && Equals(other);
        public override int GetHashCode() => 0;
    }

    // Negative cases.

    public struct FloatField : IEquatable<FloatField>
    {
        public float A;
        public bool Equals(FloatField other) => A == other.A;
        public override bool Equals(object obj) => obj is FloatField other && Equals(other);
        public override int GetHashCode() => 0;
    }

    public struct PartialCompare : IEquatable<PartialCompare>
    {
        public int A;
        public int B;
        public bool Equals(PartialCompare other) => A == other.A;
        public override bool Equals(object obj) => obj is PartialCompare other && Equals(other);
        public override int GetHashCode() => A;
    }

    public struct OrCompare : IEquatable<OrCompare>
    {
        public int A;
        public int B;
        public bool Equals(OrCompare other) => A == other.A || B == other.B;
        public override bool Equals(object obj) => obj is OrCompare other && Equals(other);
        public override int GetHashCode() => A;
    }

    // Nested field whose own Equals is not a full field-wise comparison, so the outer type is not
    // memcmp-equivalent even though its layout is bit-comparable.
    public struct WrapsPartial : IEquatable<WrapsPartial>
    {
        public PartialCompare A;
        public int B;
        public bool Equals(WrapsPartial other) => A.Equals(other.A) && B == other.B;
        public override bool Equals(object obj) => obj is WrapsPartial other && Equals(other);
        public override int GetHashCode() => B;
    }

    // Nested field that introduces internal padding, so a byte-wise compare would inspect bytes the
    // field-wise Equals ignores. Both the nested type and the wrapper must be rejected.
    public struct Padded : IEquatable<Padded>
    {
        public byte A;
        public long B;
        public bool Equals(Padded other) => A == other.A && B == other.B;
        public override bool Equals(object obj) => obj is Padded other && Equals(other);
        public override int GetHashCode() => (int)B;
    }

    public struct WrapsPadded : IEquatable<WrapsPadded>
    {
        public Padded A;
        public bool Equals(WrapsPadded other) => A.Equals(other.A);
        public override bool Equals(object obj) => obj is WrapsPadded other && Equals(other);
        public override int GetHashCode() => 0;
    }

    public struct NotEquatable
    {
        public int A;
        public int B;
    }
}
