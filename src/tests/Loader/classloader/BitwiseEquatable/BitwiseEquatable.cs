// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

using Xunit;

#pragma warning disable CS0649 // field is never assigned to

namespace BitwiseEquatableTests
{
    public static class BitwiseEquatable
    {
        private static readonly MethodInfo s_isBitwiseEquatable =
            typeof(RuntimeHelpers).GetMethod("IsBitwiseEquatable", BindingFlags.Static | BindingFlags.NonPublic)!;

        private static bool IsBitwiseEquatable(Type t) =>
            (bool)s_isBitwiseEquatable.MakeGenericMethod(t).Invoke(null, null)!;

        [Theory]
        // Primitives: '==' and Equals lower to the same bit-for-bit compare.
        [InlineData(typeof(int), true)]
        [InlineData(typeof(Int128), true)]
        [InlineData(typeof(UInt128), true)]
        // A SIMD/Unsafe-backed body isn't a recognized field-wise shape, but Guid is a known
        // bitwise-equatable type special-cased by the runtime (matching NativeAOT), so it stays true.
        [InlineData(typeof(Guid), true)]
        // Plain field-wise IEquatable<T>.Equals.
        [InlineData(typeof(Point), true)]
        [InlineData(typeof(ThreeFields), true)]
        [InlineData(typeof(OneField), true)]
        // 'Equals(other) => this == other' forwarding into a field-wise op_Equality.
        [InlineData(typeof(ForwardsToOp), true)]
        // Nested value-type fields compared through their own field-wise IEquatable<T>.Equals.
        [InlineData(typeof(Nested), true)]
        [InlineData(typeof(NestedLast), true)]
        [InlineData(typeof(AllNested), true)]
        // Nested type's Equals ignores a field, or is internally padded.
        [InlineData(typeof(WrapsPartial), false)]
        [InlineData(typeof(WrapsPadded), false)]
        // No IEquatable<T> at all: legacy path still accepts safe blittable fields.
        [InlineData(typeof(PlainNoEquatable), true)]
        // float/double are never bitwise (NaN and signed-zero semantics differ from memcmp).
        [InlineData(typeof(HasFloat), false)]
        // Equals ignores a field, does custom logic, or forwards to a non-op_Equality helper.
        [InlineData(typeof(IgnoresField), false)]
        [InlineData(typeof(CustomLogic), false)]
        [InlineData(typeof(CallsHelper), false)]
        // Explicit padding means memcmp inspects bytes Equals does not.
        [InlineData(typeof(WithPadding), false)]
        // Overrides object.Equals only; no IEquatable<T>.
        [InlineData(typeof(OverriddenOnly), false)]
        // Primitive fields compared via '.Equals' rather than '=='.
        [InlineData(typeof(PrimEquals), true)]
        [InlineData(typeof(MixedEquals), true)]
        [InlineData(typeof(FloatEquals), false)]
        // Record structs: Roslyn emits EqualityComparer<F>.Default.Equals(this.F, other.F) per field.
        [InlineData(typeof(RecTwo), true)]
        [InlineData(typeof(RecNested), true)]
        [InlineData(typeof(RecMixed), true)]
        [InlineData(typeof(RecPadded), false)]
        [InlineData(typeof(RecFloat), false)]
        public static void IsBitwiseEquatable_MatchesExpected(Type type, bool expected)
        {
            Assert.Equal(expected, IsBitwiseEquatable(type));
        }

        // The following structs have no Equals/GetHashCode override, so ValueType.Equals/GetHashCode go
        // through CanCompareBitsOrUseFastGetHashCode. The IsNotTightlyPacked fix moved a struct with a
        // multi-byte value-type field off the reflection slow path onto the memcmp fast path; either way
        // the result must match the obvious value semantics.

        [Fact]
        public static void TightlyPacked_EqualsAndHash_AreConsistent()
        {
            var a = new PlainOuter { X = new PlainInner { A = 1, B = 2 }, C = 3 };
            var b = new PlainOuter { X = new PlainInner { A = 1, B = 2 }, C = 3 };
            var c = new PlainOuter { X = new PlainInner { A = 1, B = 9 }, C = 3 };

            Assert.True(a.Equals(b));
            Assert.False(a.Equals(c));
            Assert.Equal(a.GetHashCode(), b.GetHashCode());
        }
    }

    public struct PlainInner { public int A; public int B; }
    public struct PlainOuter { public PlainInner X; public int C; }

    public record struct RecTwo(int X, int Y);
    public record struct RecNested(RecTwo P, long Z);
    public record struct RecMixed(long A, int B, short C, short D);
    public record struct RecPadded(int X, byte Y);
    public record struct RecFloat(float X, int Y);

    public readonly struct Point : IEquatable<Point>
    {
        public readonly int X; public readonly int Y;
        public bool Equals(Point o) => X == o.X && Y == o.Y;
        public override bool Equals(object o) => o is Point p && Equals(p);
        public override int GetHashCode() => 0;
    }

    public readonly struct ThreeFields : IEquatable<ThreeFields>
    {
        public readonly int A; public readonly int B; public readonly int C;
        public bool Equals(ThreeFields o) => A == o.A && B == o.B && C == o.C;
        public override bool Equals(object o) => o is ThreeFields p && Equals(p);
        public override int GetHashCode() => 0;
    }

    public readonly struct OneField : IEquatable<OneField>
    {
        public readonly long V;
        public bool Equals(OneField o) => V == o.V;
        public override bool Equals(object o) => o is OneField p && Equals(p);
        public override int GetHashCode() => 0;
    }

    public readonly struct ForwardsToOp : IEquatable<ForwardsToOp>
    {
        public readonly int Lo; public readonly int Hi;
        public bool Equals(ForwardsToOp o) => this == o;
        public static bool operator ==(ForwardsToOp a, ForwardsToOp b) => a.Lo == b.Lo && a.Hi == b.Hi;
        public static bool operator !=(ForwardsToOp a, ForwardsToOp b) => !(a == b);
        public override bool Equals(object o) => o is ForwardsToOp p && Equals(p);
        public override int GetHashCode() => 0;
    }

    public readonly struct Nested : IEquatable<Nested>
    {
        public readonly Point P; public readonly int Z;
        public bool Equals(Nested o) => P.Equals(o.P) && Z == o.Z;
        public override bool Equals(object o) => o is Nested p && Equals(p);
        public override int GetHashCode() => 0;
    }

    public readonly struct NestedLast : IEquatable<NestedLast>
    {
        public readonly int Z; public readonly Point P;
        public bool Equals(NestedLast o) => Z == o.Z && P.Equals(o.P);
        public override bool Equals(object o) => o is NestedLast p && Equals(p);
        public override int GetHashCode() => 0;
    }

    public readonly struct AllNested : IEquatable<AllNested>
    {
        public readonly Point A; public readonly OneField B;
        public bool Equals(AllNested o) => A.Equals(o.A) && B.Equals(o.B);
        public override bool Equals(object o) => o is AllNested p && Equals(p);
        public override int GetHashCode() => 0;
    }

    public readonly struct WrapsPartial : IEquatable<WrapsPartial>
    {
        public readonly IgnoresField A; public readonly int Z;
        public bool Equals(WrapsPartial o) => A.Equals(o.A) && Z == o.Z;
        public override bool Equals(object o) => o is WrapsPartial p && Equals(p);
        public override int GetHashCode() => 0;
    }

    public readonly struct WrapsPadded : IEquatable<WrapsPadded>
    {
        public readonly WithPadding A; public readonly int Z;
        public bool Equals(WrapsPadded o) => A.Equals(o.A) && Z == o.Z;
        public override bool Equals(object o) => o is WrapsPadded p && Equals(p);
        public override int GetHashCode() => 0;
    }

    public struct PlainNoEquatable { public int A; public int B; }

    public readonly struct HasFloat : IEquatable<HasFloat>
    {
        public readonly int A; public readonly float F;
        public bool Equals(HasFloat o) => A == o.A && F == o.F;
        public override bool Equals(object o) => o is HasFloat p && Equals(p);
        public override int GetHashCode() => 0;
    }

    public readonly struct IgnoresField : IEquatable<IgnoresField>
    {
        public readonly int A; public readonly int B;
        public bool Equals(IgnoresField o) => A == o.A;
        public override bool Equals(object o) => o is IgnoresField p && Equals(p);
        public override int GetHashCode() => 0;
    }

    public readonly struct CustomLogic : IEquatable<CustomLogic>
    {
        public readonly int A;
        public bool Equals(CustomLogic o) => (A & 0xF) == (o.A & 0xF);
        public override bool Equals(object o) => o is CustomLogic p && Equals(p);
        public override int GetHashCode() => 0;
    }

    public readonly struct CallsHelper : IEquatable<CallsHelper>
    {
        public readonly int A; public readonly int B;
        public bool Equals(CallsHelper o) => Cmp(this, o);
        private static bool Cmp(CallsHelper a, CallsHelper b) => a.A == b.A && a.B == b.B;
        public override bool Equals(object o) => o is CallsHelper p && Equals(p);
        public override int GetHashCode() => 0;
    }

    [StructLayout(LayoutKind.Explicit, Size = 16)]
    public readonly struct WithPadding : IEquatable<WithPadding>
    {
        [FieldOffset(0)] public readonly byte A;
        [FieldOffset(8)] public readonly int B;
        public bool Equals(WithPadding o) => A == o.A && B == o.B;
        public override bool Equals(object o) => o is WithPadding p && Equals(p);
        public override int GetHashCode() => 0;
    }

    public struct OverriddenOnly
    {
        public int A;
        public override bool Equals(object o) => o is OverriddenOnly p && p.A == A;
        public override int GetHashCode() => A;
    }

    public readonly struct PrimEquals : IEquatable<PrimEquals>
    {
        public readonly byte A; public readonly sbyte B; public readonly short C; public readonly int D; public readonly long E;
        public bool Equals(PrimEquals o) => A.Equals(o.A) && B.Equals(o.B) && C.Equals(o.C) && D.Equals(o.D) && E.Equals(o.E);
        public override bool Equals(object o) => o is PrimEquals p && Equals(p);
        public override int GetHashCode() => 0;
    }

    public readonly struct MixedEquals : IEquatable<MixedEquals>
    {
        public readonly int A; public readonly int B;
        public bool Equals(MixedEquals o) => A == o.A && B.Equals(o.B);
        public override bool Equals(object o) => o is MixedEquals p && Equals(p);
        public override int GetHashCode() => 0;
    }

    public readonly struct FloatEquals : IEquatable<FloatEquals>
    {
        public readonly int A; public readonly float F;
        public bool Equals(FloatEquals o) => A.Equals(o.A) && F.Equals(o.F);
        public override bool Equals(object o) => o is FloatEquals p && Equals(p);
        public override int GetHashCode() => 0;
    }
}
