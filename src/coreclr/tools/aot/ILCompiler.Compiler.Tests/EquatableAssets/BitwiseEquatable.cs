// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

#pragma warning disable 649  // field is never assigned to
#pragma warning disable 660  // defines operator == but not Equals
#pragma warning disable 661  // defines operator == but not GetHashCode

namespace BitwiseEquatable
{
    // Positive cases: value types whose IEquatable<T>.Equals is a plain field-wise comparison.

    public struct OneField : IEquatable<OneField>
    {
        public int A;
        public bool Equals(OneField other) => A == other.A;
    }

    public struct TwoFields : IEquatable<TwoFields>
    {
        public int A;
        public int B;
        public bool Equals(TwoFields other) => A == other.A && B == other.B;
    }

    public struct MixedPrimitives : IEquatable<MixedPrimitives>
    {
        public long A;
        public int B;
        public short C;
        public short D;
        public bool Equals(MixedPrimitives other) => A == other.A && B == other.B && C == other.C && D == other.D;
    }

    public struct ForwardsToOp : IEquatable<ForwardsToOp>
    {
        public int A;
        public int B;
        public static bool operator ==(ForwardsToOp x, ForwardsToOp y) => x.A == y.A && x.B == y.B;
        public static bool operator !=(ForwardsToOp x, ForwardsToOp y) => !(x == y);
        public bool Equals(ForwardsToOp other) => this == other;
    }

    // Negative cases.

    public struct FloatField : IEquatable<FloatField>
    {
        public float A;
        public bool Equals(FloatField other) => A == other.A;
    }

    public struct PartialCompare : IEquatable<PartialCompare>
    {
        public int A;
        public int B;
        public bool Equals(PartialCompare other) => A == other.A;
    }

    public struct OrCompare : IEquatable<OrCompare>
    {
        public int A;
        public int B;
        public bool Equals(OrCompare other) => A == other.A || B == other.B;
    }

    public struct NestedField : IEquatable<NestedField>
    {
        public OneField A;
        public int B;
        public bool Equals(NestedField other) => A.Equals(other.A) && B == other.B;
    }

    public struct NotEquatable
    {
        public int A;
        public int B;
    }
}
