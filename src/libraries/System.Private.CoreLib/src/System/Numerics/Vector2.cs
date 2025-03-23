// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics;

namespace System.Numerics
{
    /// <summary>Represents a vector with two single-precision floating-point values.</summary>
    /// <remarks><format type="text/markdown"><![CDATA[
    /// The <xref:System.Numerics.Vector2> structure provides support for hardware acceleration.
    /// [!INCLUDE[vectors-are-rows-paragraph](~/includes/system-numerics-vectors-are-rows.md)]
    /// ]]></format></remarks>
    [Intrinsic]
    public partial struct Vector2 : IEquatable<Vector2>, IFormattable
    {
        /// <summary>Specifies the alignment of the vector as used by the <see cref="LoadAligned(float*)" /> and <see cref="Vector.StoreAligned(Vector2, float*)" /> APIs.</summary>
        /// <remarks>
        ///     <para>
        ///       Different environments all have their own concepts of alignment/packing.
        ///       For example, a <c>Vector3</c> in .NET is 4-byte aligned and 12-bytes in size,
        ///       in GLSL a <c>vec3</c> is 16-byte aligned and 16-byte sized, while in HLSL a
        ///       <c>float3</c> is functionally 8-byte aligned and 12-byte sized. These differences
        ///       make it impossible to define a "correct" alignment; additionally, the nuance
        ///       in environments like HLSL where size is not a multiple of alignment introduce complications.
        ///     </para>
        ///     <para>
        ///       For the purposes of the <c>LoadAligned</c> and <c>StoreAligned</c> APIs we
        ///       therefore pick a value that allows for a broad range of compatibility while
        ///       also allowing more optimal codegen for various target platforms.
        ///     </para>
        /// </remarks>
        internal const int Alignment = 8;

        /// <summary>The X component of the vector.</summary>
        public float X;

        /// <summary>The Y component of the vector.</summary>
        public float Y;

        internal const int ElementCount = 2;

        /// <summary>Creates a new <see cref="Vector2" /> object whose two elements have the same value.</summary>
        /// <param name="value">The value to assign to both elements.</param>
        [Intrinsic]
        public Vector2(float value)
        {
            this = Create(value);
        }

        /// <summary>Creates a vector whose elements have the specified values.</summary>
        /// <param name="x">The value to assign to the <see cref="X" /> field.</param>
        /// <param name="y">The value to assign to the <see cref="Y" /> field.</param>
        [Intrinsic]
        public Vector2(float x, float y)
        {
            this = Create(x, y);
        }

        /// <summary>Constructs a vector from the given <see cref="ReadOnlySpan{Single}" />. The span must contain at least 2 elements.</summary>
        /// <param name="values">The span of elements to assign to the vector.</param>
        [Intrinsic]
        public Vector2(ReadOnlySpan<float> values)
        {
            this = Create(values);
        }

        /// <inheritdoc cref="Vector4.AllBitsSet" />
        public static Vector2 AllBitsSet
        {
            [Intrinsic]
            get => Vector128<float>.AllBitsSet.AsVector2();
        }

        /// <inheritdoc cref="Vector4.E" />
        public static Vector2 E
        {
            [Intrinsic]
            get => Create(float.E);
        }

        /// <inheritdoc cref="Vector4.Epsilon" />
        public static Vector2 Epsilon
        {
            [Intrinsic]
            get => Create(float.Epsilon);
        }

        /// <inheritdoc cref="Vector4.NaN" />
        public static Vector2 NaN
        {
            [Intrinsic]
            get => Create(float.NaN);
        }

        /// <inheritdoc cref="Vector4.NegativeInfinity" />
        public static Vector2 NegativeInfinity
        {
            [Intrinsic]
            get => Create(float.NegativeInfinity);
        }

        /// <inheritdoc cref="Vector4.NegativeZero" />
        public static Vector2 NegativeZero
        {
            [Intrinsic]
            get => Create(float.NegativeZero);
        }

        /// <inheritdoc cref="Vector4.One" />
        public static Vector2 One
        {
            [Intrinsic]
            get => Create(1.0f);
        }

        /// <inheritdoc cref="Vector4.Pi" />
        public static Vector2 Pi
        {
            [Intrinsic]
            get => Create(float.Pi);
        }

        /// <inheritdoc cref="Vector4.PositiveInfinity" />
        public static Vector2 PositiveInfinity
        {
            [Intrinsic]
            get => Create(float.PositiveInfinity);
        }

        /// <inheritdoc cref="Vector4.Tau" />
        public static Vector2 Tau
        {
            [Intrinsic]
            get => Create(float.Tau);
        }

        /// <summary>Gets the vector (1,0).</summary>
        /// <value>The vector <c>(1,0)</c>.</value>
        public static Vector2 UnitX
        {
            [Intrinsic]
            get => CreateScalar(1.0f);
        }

        /// <summary>Gets the vector (0,1).</summary>
        /// <value>The vector <c>(0,1)</c>.</value>
        public static Vector2 UnitY
        {
            [Intrinsic]
            get => Create(0.0f, 1.0f);
        }

        /// <inheritdoc cref="Vector4.Zero" />
        public static Vector2 Zero
        {
            [Intrinsic]
            get => default;
        }

        /// <summary>Gets or sets the element at the specified index.</summary>
        /// <param name="index">The index of the element to get or set.</param>
        /// <returns>The the element at <paramref name="index" />.</returns>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="index" /> was less than zero or greater than the number of elements.</exception>
        public float this[int index]
        {
            [Intrinsic]
            readonly get => this.GetElement(index);

            [Intrinsic]
            set
            {
                this = this.WithElement(index, value);
            }
        }

        /// <summary>Adds two vectors together.</summary>
        /// <param name="left">The first vector to add.</param>
        /// <param name="right">The second vector to add.</param>
        /// <returns>The summed vector.</returns>
        /// <remarks>The <see cref="op_Addition" /> method defines the addition operation for <see cref="Vector2" /> objects.</remarks>
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector2 operator +(Vector2 left, Vector2 right) => (left.AsVector128Unsafe() + right.AsVector128Unsafe()).AsVector2();

        /// <summary>Divides the first vector by the second.</summary>
        /// <param name="left">The first vector.</param>
        /// <param name="right">The second vector.</param>
        /// <returns>The vector that results from dividing <paramref name="left" /> by <paramref name="right" />.</returns>
        /// <remarks>The <see cref="Vector2.op_Division" /> method defines the division operation for <see cref="Vector2" /> objects.</remarks>
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector2 operator /(Vector2 left, Vector2 right) => (left.AsVector128Unsafe() / right.AsVector128Unsafe()).AsVector2();

        /// <summary>Divides the specified vector by a specified scalar value.</summary>
        /// <param name="value1">The vector.</param>
        /// <param name="value2">The scalar value.</param>
        /// <returns>The result of the division.</returns>
        /// <remarks>The <see cref="Vector2.op_Division" /> method defines the division operation for <see cref="Vector2" /> objects.</remarks>
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector2 operator /(Vector2 value1, float value2) => (value1.AsVector128Unsafe() / value2).AsVector2();

        /// <summary>Returns a value that indicates whether each pair of elements in two specified vectors is equal.</summary>
        /// <param name="left">The first vector to compare.</param>
        /// <param name="right">The second vector to compare.</param>
        /// <returns><see langword="true" /> if <paramref name="left" /> and <paramref name="right" /> are equal; otherwise, <see langword="false" />.</returns>
        /// <remarks>Two <see cref="Vector2" /> objects are equal if each value in <paramref name="left" /> is equal to the corresponding value in <paramref name="right" />.</remarks>
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator ==(Vector2 left, Vector2 right) => left.AsVector128() == right.AsVector128();

        /// <summary>Returns a value that indicates whether two specified vectors are not equal.</summary>
        /// <param name="left">The first vector to compare.</param>
        /// <param name="right">The second vector to compare.</param>
        /// <returns><see langword="true" /> if <paramref name="left" /> and <paramref name="right" /> are not equal; otherwise, <see langword="false" />.</returns>
        [Intrinsic]
        public static bool operator !=(Vector2 left, Vector2 right) => !(left == right);

        /// <summary>Returns a new vector whose values are the product of each pair of elements in two specified vectors.</summary>
        /// <param name="left">The first vector.</param>
        /// <param name="right">The second vector.</param>
        /// <returns>The element-wise product vector.</returns>
        /// <remarks>The <see cref="Vector2.op_Multiply" /> method defines the multiplication operation for <see cref="Vector2" /> objects.</remarks>
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector2 operator *(Vector2 left, Vector2 right) => (left.AsVector128Unsafe() * right.AsVector128Unsafe()).AsVector2();

        /// <summary>Multiplies the specified vector by the specified scalar value.</summary>
        /// <param name="left">The vector.</param>
        /// <param name="right">The scalar value.</param>
        /// <returns>The scaled vector.</returns>
        /// <remarks>The <see cref="Vector2.op_Multiply" /> method defines the multiplication operation for <see cref="Vector2" /> objects.</remarks>
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector2 operator *(Vector2 left, float right) => (left.AsVector128Unsafe() * right).AsVector2();

        /// <summary>Multiplies the scalar value by the specified vector.</summary>
        /// <param name="left">The vector.</param>
        /// <param name="right">The scalar value.</param>
        /// <returns>The scaled vector.</returns>
        /// <remarks>The <see cref="Vector2.op_Multiply" /> method defines the multiplication operation for <see cref="Vector2" /> objects.</remarks>
        [Intrinsic]
        public static Vector2 operator *(float left, Vector2 right) => right * left;

        /// <summary>Subtracts the second vector from the first.</summary>
        /// <param name="left">The first vector.</param>
        /// <param name="right">The second vector.</param>
        /// <returns>The vector that results from subtracting <paramref name="right" /> from <paramref name="left" />.</returns>
        /// <remarks>The <see cref="op_Subtraction" /> method defines the subtraction operation for <see cref="Vector2" /> objects.</remarks>
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector2 operator -(Vector2 left, Vector2 right) => (left.AsVector128Unsafe() - right.AsVector128Unsafe()).AsVector2();

        /// <summary>Negates the specified vector.</summary>
        /// <param name="value">The vector to negate.</param>
        /// <returns>The negated vector.</returns>
        /// <remarks>The <see cref="op_UnaryNegation" /> method defines the unary negation operation for <see cref="Vector2" /> objects.</remarks>
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector2 operator -(Vector2 value) => (-value.AsVector128Unsafe()).AsVector2();

        /// <inheritdoc cref="Vector4.op_BitwiseAnd(Vector4, Vector4)" />
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector2 operator &(Vector2 left, Vector2 right) => (left.AsVector128Unsafe() & right.AsVector128Unsafe()).AsVector2();

        /// <inheritdoc cref="Vector4.op_BitwiseOr(Vector4, Vector4)" />
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector2 operator |(Vector2 left, Vector2 right) => (left.AsVector128Unsafe() | right.AsVector128Unsafe()).AsVector2();

        /// <inheritdoc cref="Vector4.op_ExclusiveOr(Vector4, Vector4)" />
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector2 operator ^(Vector2 left, Vector2 right) => (left.AsVector128Unsafe() ^ right.AsVector128Unsafe()).AsVector2();

        /// <inheritdoc cref="Vector4.op_LeftShift(Vector4, int)" />
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector2 operator <<(Vector2 value, int shiftAmount) => (value.AsVector128Unsafe() << shiftAmount).AsVector2();

        /// <inheritdoc cref="Vector4.op_OnesComplement(Vector4)" />
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector2 operator ~(Vector2 value) => (~value.AsVector128Unsafe()).AsVector2();

        /// <inheritdoc cref="Vector4.op_RightShift(Vector4, int)" />
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector2 operator >>(Vector2 value, int shiftAmount) => (value.AsVector128Unsafe() >> shiftAmount).AsVector2();

        /// <inheritdoc cref="Vector4.op_UnaryPlus(Vector4)" />
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector2 operator +(Vector2 value) => value;

        /// <inheritdoc cref="Vector4.op_UnsignedRightShift(Vector4, int)" />
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector2 operator >>>(Vector2 value, int shiftAmount) => (value.AsVector128Unsafe() >>> shiftAmount).AsVector2();

        /// <summary>Returns a vector whose elements are the absolute values of each of the specified vector's elements.</summary>
        /// <param name="value">A vector.</param>
        /// <returns>The absolute value vector.</returns>
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector2 Abs(Vector2 value) => Vector128.Abs(value.AsVector128Unsafe()).AsVector2();

        /// <summary>Adds two vectors together.</summary>
        /// <param name="left">The first vector to add.</param>
        /// <param name="right">The second vector to add.</param>
        /// <returns>The summed vector.</returns>
        [Intrinsic]
        public static Vector2 Add(Vector2 left, Vector2 right) => left + right;

        /// <inheritdoc cref="Vector4.All(Vector4, float)" />
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool All(Vector2 vector, float value) => Vector128.All(vector, value);

        /// <inheritdoc cref="Vector4.AllWhereAllBitsSet(Vector4)" />
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool AllWhereAllBitsSet(Vector2 vector) => Vector128.AllWhereAllBitsSet(vector);

        /// <inheritdoc cref="Vector4.AndNot(Vector4, Vector4)" />
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector2 AndNot(Vector2 left, Vector2 right) => Vector128.AndNot(left.AsVector128Unsafe(), right.AsVector128Unsafe()).AsVector2();

        /// <inheritdoc cref="Vector4.Any(Vector4, float)" />
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool Any(Vector2 vector, float value) => Vector128.Any(vector, value);

        /// <inheritdoc cref="Vector4.AnyWhereAllBitsSet(Vector4)" />
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool AnyWhereAllBitsSet(Vector2 vector) => Vector128.AnyWhereAllBitsSet(vector);

        /// <inheritdoc cref="Vector4.BitwiseAnd(Vector4, Vector4)" />
        [Intrinsic]
        public static Vector2 BitwiseAnd(Vector2 left, Vector2 right) => left & right;

        /// <inheritdoc cref="Vector4.BitwiseOr(Vector4, Vector4)" />
        [Intrinsic]
        public static Vector2 BitwiseOr(Vector2 left, Vector2 right) => left | right;

        /// <inheritdoc cref="Vector4.Clamp(Vector4, Vector4, Vector4)" />
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector2 Clamp(Vector2 value1, Vector2 min, Vector2 max) => Vector128.Clamp(value1.AsVector128Unsafe(), min.AsVector128Unsafe(), max.AsVector128Unsafe()).AsVector2();

        /// <inheritdoc cref="Vector4.ClampNative(Vector4, Vector4, Vector4)" />
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector2 ClampNative(Vector2 value1, Vector2 min, Vector2 max) => Vector128.ClampNative(value1.AsVector128Unsafe(), min.AsVector128Unsafe(), max.AsVector128Unsafe()).AsVector2();

        /// <inheritdoc cref="Vector4.ConditionalSelect(Vector4, Vector4, Vector4)" />
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector2 ConditionalSelect(Vector2 condition, Vector2 left, Vector2 right) => Vector128.ConditionalSelect(condition.AsVector128Unsafe(), left.AsVector128Unsafe(), right.AsVector128Unsafe()).AsVector2();

        /// <inheritdoc cref="Vector4.CopySign(Vector4, Vector4)" />
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector2 CopySign(Vector2 value, Vector2 sign) => Vector128.CopySign(value.AsVector128Unsafe(), sign.AsVector128Unsafe()).AsVector2();

        /// <inheritdoc cref="Vector4.Cos(Vector4)" />
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector2 Cos(Vector2 vector) => Vector128.Cos(vector.AsVector128()).AsVector2();

        /// <inheritdoc cref="Vector4.Count(Vector4, float)" />
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int Count(Vector2 vector, float value) => Vector128.Count(vector, value);

        /// <inheritdoc cref="Vector4.CountWhereAllBitsSet(Vector4)" />
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int CountWhereAllBitsSet(Vector2 vector) => Vector128.CountWhereAllBitsSet(vector);

        /// <summary>Creates a new <see cref="Vector2" /> object whose two elements have the same value.</summary>
        /// <param name="value">The value to assign to all two elements.</param>
        /// <returns>A new <see cref="Vector2" /> whose two elements have the same value.</returns>
        [Intrinsic]
        public static Vector2 Create(float value) => Vector128.Create(value).AsVector2();

        /// <summary>Creates a vector whose elements have the specified values.</summary>
        /// <param name="x">The value to assign to the <see cref="X" /> field.</param>
        /// <param name="y">The value to assign to the <see cref="Y" /> field.</param>
        /// <returns>A new <see cref="Vector2" /> whose elements have the specified values.</returns>
        [Intrinsic]
        public static Vector2 Create(float x, float y) => Vector128.Create(x, y, 0, 0).AsVector2();

        /// <summary>Constructs a vector from the given <see cref="ReadOnlySpan{Single}" />. The span must contain at least 2 elements.</summary>
        /// <param name="values">The span of elements to assign to the vector.</param>
        /// <returns>A new <see cref="Vector2" /> whose elements have the specified values.</returns>
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector2 Create(ReadOnlySpan<float> values)
        {
            if (values.Length < ElementCount)
            {
                ThrowHelper.ThrowArgumentOutOfRangeException(ExceptionArgument.values);
            }
            return Unsafe.ReadUnaligned<Vector2>(ref Unsafe.As<float, byte>(ref MemoryMarshal.GetReference(values)));
        }

        /// <summary>Creates a vector with <see cref="X" /> initialized to the specified value and the remaining elements initialized to zero.</summary>
        /// <param name="x">The value to assign to the <see cref="X" /> field.</param>
        /// <returns>A new <see cref="Vector2" /> with <see cref="X" /> initialized <paramref name="x" /> and the remaining elements initialized to zero.</returns>
        [Intrinsic]
        internal static Vector2 CreateScalar(float x) => Vector128.CreateScalar(x).AsVector2();

        /// <summary>Creates a vector with <see cref="X" /> initialized to the specified value and the remaining elements left uninitialized.</summary>
        /// <param name="x">The value to assign to the <see cref="X" /> field.</param>
        /// <returns>A new <see cref="Vector2" /> with <see cref="X" /> initialized <paramref name="x" /> and the remaining elements left uninitialized.</returns>
        [Intrinsic]
        internal static Vector2 CreateScalarUnsafe(float x) => Vector128.CreateScalarUnsafe(x).AsVector2();

        /// <summary>
        /// Returns the z-value of the cross product of two vectors.
        /// Since the Vector2 is in the x-y plane, a 3D cross product only produces the z-value.
        /// </summary>
        /// <param name="value1">The first vector.</param>
        /// <param name="value2">The second vector.</param>
        /// <returns>The value of the z-coordinate from the cross product.</returns>
        /// <remarks>
        /// Return z-value = value1.X * value2.Y - value1.Y * value2.X
        /// <see cref="Cross"/> is the same as taking the <see cref="Dot"/> with the second vector
        /// that has been rotated 90-degrees.
        /// </remarks>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float Cross(Vector2 value1, Vector2 value2)
        {
            //return value1.X * value2.Y - value1.Y * value2.X;

            Vector128<float> mul =
                Vector128.Shuffle(value1.AsVector128Unsafe(), Vector128.Create(0, 1, 0, 1)) *
                Vector128.Shuffle(value2.AsVector128Unsafe(), Vector128.Create(1, 0, 1, 0));

            return (mul - Vector128.Shuffle(mul, Vector128.Create(1, 0, 1, 0))).ToScalar();
        }

        /// <inheritdoc cref="Vector4.DegreesToRadians(Vector4)" />
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector2 DegreesToRadians(Vector2 degrees) => Vector128.DegreesToRadians(degrees.AsVector128Unsafe()).AsVector2();

        /// <summary>Computes the Euclidean distance between the two given points.</summary>
        /// <param name="value1">The first point.</param>
        /// <param name="value2">The second point.</param>
        /// <returns>The distance.</returns>
        [Intrinsic]
        public static float Distance(Vector2 value1, Vector2 value2) => float.Sqrt(DistanceSquared(value1, value2));

        /// <summary>Returns the Euclidean distance squared between two specified points.</summary>
        /// <param name="value1">The first point.</param>
        /// <param name="value2">The second point.</param>
        /// <returns>The distance squared.</returns>
        [Intrinsic]
        public static float DistanceSquared(Vector2 value1, Vector2 value2) => (value1 - value2).LengthSquared();

        /// <summary>Divides the first vector by the second.</summary>
        /// <param name="left">The first vector.</param>
        /// <param name="right">The second vector.</param>
        /// <returns>The vector resulting from the division.</returns>
        [Intrinsic]
        public static Vector2 Divide(Vector2 left, Vector2 right) => left / right;

        /// <summary>Divides the specified vector by a specified scalar value.</summary>
        /// <param name="left">The vector.</param>
        /// <param name="divisor">The scalar value.</param>
        /// <returns>The vector that results from the division.</returns>
        [Intrinsic]
        public static Vector2 Divide(Vector2 left, float divisor) => left / divisor;

        /// <summary>Returns the dot product of two vectors.</summary>
        /// <param name="value1">The first vector.</param>
        /// <param name="value2">The second vector.</param>
        /// <returns>The dot product.</returns>
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float Dot(Vector2 value1, Vector2 value2) => Vector128.Dot(value1.AsVector128(), value2.AsVector128());

        /// <inheritdoc cref="Vector4.Exp(Vector4)" />
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector2 Exp(Vector2 vector) => Vector128.Exp(vector.AsVector128()).AsVector2();

        /// <inheritdoc cref="Vector4.Equals(Vector4, Vector4)" />
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector2 Equals(Vector2 left, Vector2 right) => Vector128.Equals(left.AsVector128Unsafe(), right.AsVector128Unsafe()).AsVector2();

        /// <inheritdoc cref="Vector4.EqualsAll(Vector4, Vector4)" />
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool EqualsAll(Vector2 left, Vector2 right) => Vector128.EqualsAll(left.AsVector128Unsafe(), right.AsVector128Unsafe());

        /// <inheritdoc cref="Vector4.EqualsAny(Vector4, Vector4)" />
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool EqualsAny(Vector2 left, Vector2 right) => Vector128.EqualsAny(left.AsVector128Unsafe(), right.AsVector128Unsafe());

        /// <inheritdoc cref="Vector128.MultiplyAddEstimate(Vector128{float}, Vector128{float}, Vector128{float})" />
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector2 FusedMultiplyAdd(Vector2 left, Vector2 right, Vector2 addend) => Vector128.FusedMultiplyAdd(left.AsVector128Unsafe(), right.AsVector128Unsafe(), addend.AsVector128Unsafe()).AsVector2();

        /// <inheritdoc cref="Vector4.GreaterThan(Vector4, Vector4)" />
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector2 GreaterThan(Vector2 left, Vector2 right) => Vector128.GreaterThan(left.AsVector128Unsafe(), right.AsVector128Unsafe()).AsVector2();

        /// <inheritdoc cref="Vector4.GreaterThanAll(Vector4, Vector4)" />
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool GreaterThanAll(Vector2 left, Vector2 right) => Vector128.GreaterThanAll(left.AsVector128Unsafe(), right.AsVector128Unsafe());

        /// <inheritdoc cref="Vector4.GreaterThanAny(Vector4, Vector4)" />
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool GreaterThanAny(Vector2 left, Vector2 right) => Vector128.GreaterThanAny(left.AsVector128Unsafe(), right.AsVector128Unsafe());

        /// <inheritdoc cref="Vector4.GreaterThanOrEqual(Vector4, Vector4)" />
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector2 GreaterThanOrEqual(Vector2 left, Vector2 right) => Vector128.GreaterThanOrEqual(left.AsVector128Unsafe(), right.AsVector128Unsafe()).AsVector2();

        /// <inheritdoc cref="Vector4.GreaterThanOrEqualAll(Vector4, Vector4)" />
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool GreaterThanOrEqualAll(Vector2 left, Vector2 right) => Vector128.GreaterThanOrEqualAll(left.AsVector128Unsafe(), right.AsVector128Unsafe());

        /// <inheritdoc cref="Vector4.GreaterThanOrEqualAny(Vector4, Vector4)" />
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool GreaterThanOrEqualAny(Vector2 left, Vector2 right) => Vector128.GreaterThanOrEqualAny(left.AsVector128Unsafe(), right.AsVector128Unsafe());

        /// <inheritdoc cref="Vector4.Hypot(Vector4, Vector4)" />
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector2 Hypot(Vector2 x, Vector2 y) => Vector128.Hypot(x.AsVector128Unsafe(), y.AsVector128Unsafe()).AsVector2();

        /// <inheritdoc cref="Vector4.IndexOf(Vector4, float)" />
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int IndexOf(Vector2 vector, float value) => Vector128.IndexOf(vector, value);

        /// <inheritdoc cref="Vector4.IndexOfWhereAllBitsSet(Vector4)" />
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int IndexOfWhereAllBitsSet(Vector2 vector) => Vector128.IndexOfWhereAllBitsSet(vector);

        /// <inheritdoc cref="Vector4.IsEvenInteger(Vector4)" />
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector2 IsEvenInteger(Vector2 vector) => Vector128.IsEvenInteger(vector.AsVector128()).AsVector2();

        /// <inheritdoc cref="Vector4.IsFinite(Vector4)" />
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector2 IsFinite(Vector2 vector) => Vector128.IsFinite(vector.AsVector128()).AsVector2();

        /// <inheritdoc cref="Vector4.IsInfinity(Vector4)" />
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector2 IsInfinity(Vector2 vector) => Vector128.IsInfinity(vector.AsVector128()).AsVector2();

        /// <inheritdoc cref="Vector4.IsInteger(Vector4)" />
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector2 IsInteger(Vector2 vector) => Vector128.IsInteger(vector.AsVector128()).AsVector2();

        /// <inheritdoc cref="Vector4.IsNaN(Vector4)" />
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector2 IsNaN(Vector2 vector) => Vector128.IsNaN(vector.AsVector128()).AsVector2();

        /// <inheritdoc cref="Vector4.IsNegative(Vector4)" />
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector2 IsNegative(Vector2 vector) => Vector128.IsNegative(vector.AsVector128()).AsVector2();

        /// <inheritdoc cref="Vector4.IsNegativeInfinity(Vector4)" />
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector2 IsNegativeInfinity(Vector2 vector) => Vector128.IsNegativeInfinity(vector.AsVector128()).AsVector2();

        /// <inheritdoc cref="Vector4.IsNormal(Vector4)" />
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector2 IsNormal(Vector2 vector) => Vector128.IsNormal(vector.AsVector128()).AsVector2();

        /// <inheritdoc cref="Vector4.IsOddInteger(Vector4)" />
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector2 IsOddInteger(Vector2 vector) => Vector128.IsOddInteger(vector.AsVector128()).AsVector2();

        /// <inheritdoc cref="Vector4.IsPositive(Vector4)" />
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector2 IsPositive(Vector2 vector) => Vector128.IsPositive(vector.AsVector128()).AsVector2();

        /// <inheritdoc cref="Vector4.IsPositiveInfinity(Vector4)" />
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector2 IsPositiveInfinity(Vector2 vector) => Vector128.IsPositiveInfinity(vector.AsVector128()).AsVector2();

        /// <inheritdoc cref="Vector4.IsSubnormal(Vector4)" />
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector2 IsSubnormal(Vector2 vector) => Vector128.IsSubnormal(vector.AsVector128()).AsVector2();

        /// <inheritdoc cref="Vector4.IsZero(Vector4)" />
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector2 IsZero(Vector2 vector) => Vector128.IsZero(vector.AsVector128()).AsVector2();

        /// <inheritdoc cref="Vector4.LastIndexOf(Vector4, float)" />
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int LastIndexOf(Vector2 vector, float value) => Vector128.LastIndexOf(vector, value);

        /// <inheritdoc cref="Vector4.LastIndexOfWhereAllBitsSet(Vector4)" />
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int LastIndexOfWhereAllBitsSet(Vector2 vector) => Vector128.LastIndexOfWhereAllBitsSet(vector);

        /// <inheritdoc cref="Vector4.Lerp(Vector4, Vector4, float)" />
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector2 Lerp(Vector2 value1, Vector2 value2, float amount) => Lerp(value1, value2, Create(amount));

        /// <inheritdoc cref="Vector4.Lerp(Vector4, Vector4, Vector4)" />
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector2 Lerp(Vector2 value1, Vector2 value2, Vector2 amount) => Vector128.Lerp(value1.AsVector128Unsafe(), value2.AsVector128Unsafe(), amount.AsVector128Unsafe()).AsVector2();

        /// <inheritdoc cref="Vector4.LessThan(Vector4, Vector4)" />
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector2 LessThan(Vector2 left, Vector2 right) => Vector128.LessThan(left.AsVector128Unsafe(), right.AsVector128Unsafe()).AsVector2();

        /// <inheritdoc cref="Vector4.LessThanAll(Vector4, Vector4)" />
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool LessThanAll(Vector2 left, Vector2 right) => Vector128.LessThanAll(left.AsVector128Unsafe(), right.AsVector128Unsafe());

        /// <inheritdoc cref="Vector4.LessThanAny(Vector4, Vector4)" />
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool LessThanAny(Vector2 left, Vector2 right) => Vector128.LessThanAny(left.AsVector128Unsafe(), right.AsVector128Unsafe());

        /// <inheritdoc cref="Vector4.LessThanOrEqual(Vector4, Vector4)" />
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector2 LessThanOrEqual(Vector2 left, Vector2 right) => Vector128.LessThanOrEqual(left.AsVector128Unsafe(), right.AsVector128Unsafe()).AsVector2();

        /// <inheritdoc cref="Vector4.LessThanOrEqualAll(Vector4, Vector4)" />
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool LessThanOrEqualAll(Vector2 left, Vector2 right) => Vector128.LessThanOrEqualAll(left.AsVector128Unsafe(), right.AsVector128Unsafe());

        /// <inheritdoc cref="Vector4.LessThanOrEqualAny(Vector4, Vector4)" />
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool LessThanOrEqualAny(Vector2 left, Vector2 right) => Vector128.LessThanOrEqualAny(left.AsVector128Unsafe(), right.AsVector128Unsafe());

        /// <inheritdoc cref="Vector4.Load(float*)" />
        [Intrinsic]
        [CLSCompliant(false)]
        public static unsafe Vector2 Load(float* source) => LoadUnsafe(in *source);

        /// <inheritdoc cref="Vector4.LoadAligned(float*)" />
        [Intrinsic]
        [CLSCompliant(false)]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe Vector2 LoadAligned(float* source)
        {
            if (((nuint)(source) % Alignment) != 0)
            {
                ThrowHelper.ThrowAccessViolationException();
            }

            return *(Vector2*)source;
        }

        /// <inheritdoc cref="Vector4.LoadAlignedNonTemporal(float*)" />
        [Intrinsic]
        [CLSCompliant(false)]
        public static unsafe Vector2 LoadAlignedNonTemporal(float* source) => LoadAligned(source);

        /// <inheritdoc cref="Vector128.LoadUnsafe{T}(ref readonly T)" />
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector2 LoadUnsafe(ref readonly float source)
        {
            ref readonly byte address = ref Unsafe.As<float, byte>(ref Unsafe.AsRef(in source));
            return Unsafe.ReadUnaligned<Vector2>(in address);
        }

        /// <inheritdoc cref="Vector4.LoadUnsafe(ref readonly float, nuint)" />
        [Intrinsic]
        [CLSCompliant(false)]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector2 LoadUnsafe(ref readonly float source, nuint elementOffset)
        {
            ref readonly byte address = ref Unsafe.As<float, byte>(ref Unsafe.Add(ref Unsafe.AsRef(in source), (nint)elementOffset));
            return Unsafe.ReadUnaligned<Vector2>(in address);
        }

        /// <inheritdoc cref="Vector4.Log2(Vector4)" />
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector2 Log(Vector2 vector) => Vector128.Log(Vector4.Create(vector, 1.0f, 1.0f).AsVector128()).AsVector2();

        /// <inheritdoc cref="Vector4.Log(Vector4)" />
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector2 Log2(Vector2 vector) => Vector128.Log2(Vector4.Create(vector, 1.0f, 1.0f).AsVector128()).AsVector2();

        /// <inheritdoc cref="Vector4.Max(Vector4, Vector4)" />
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector2 Max(Vector2 value1, Vector2 value2) => Vector128.Max(value1.AsVector128Unsafe(), value2.AsVector128Unsafe()).AsVector2();

        /// <inheritdoc cref="Vector4.MaxMagnitude(Vector4, Vector4)" />
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector2 MaxMagnitude(Vector2 value1, Vector2 value2) => Vector128.MaxMagnitude(value1.AsVector128Unsafe(), value2.AsVector128Unsafe()).AsVector2();

        /// <inheritdoc cref="Vector4.MaxMagnitudeNumber(Vector4, Vector4)" />
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector2 MaxMagnitudeNumber(Vector2 value1, Vector2 value2) => Vector128.MaxMagnitudeNumber(value1.AsVector128Unsafe(), value2.AsVector128Unsafe()).AsVector2();

        /// <inheritdoc cref="Vector4.MaxNative(Vector4, Vector4)" />
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector2 MaxNative(Vector2 value1, Vector2 value2) => Vector128.MaxNative(value1.AsVector128Unsafe(), value2.AsVector128Unsafe()).AsVector2();

        /// <inheritdoc cref="Vector4.MaxNumber(Vector4, Vector4)" />
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector2 MaxNumber(Vector2 value1, Vector2 value2) => Vector128.MaxNumber(value1.AsVector128Unsafe(), value2.AsVector128Unsafe()).AsVector2();

        /// <inheritdoc cref="Vector4.Min(Vector4, Vector4)" />
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector2 Min(Vector2 value1, Vector2 value2) => Vector128.Min(value1.AsVector128Unsafe(), value2.AsVector128Unsafe()).AsVector2();

        /// <inheritdoc cref="Vector4.MinMagnitude(Vector4, Vector4)" />
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector2 MinMagnitude(Vector2 value1, Vector2 value2) => Vector128.MinMagnitude(value1.AsVector128Unsafe(), value2.AsVector128Unsafe()).AsVector2();

        /// <inheritdoc cref="Vector4.MinMagnitudeNumber(Vector4, Vector4)" />
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector2 MinMagnitudeNumber(Vector2 value1, Vector2 value2) => Vector128.MinMagnitudeNumber(value1.AsVector128Unsafe(), value2.AsVector128Unsafe()).AsVector2();

        /// <inheritdoc cref="Vector4.MinNative(Vector4, Vector4)" />
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector2 MinNative(Vector2 value1, Vector2 value2) => Vector128.MinNative(value1.AsVector128Unsafe(), value2.AsVector128Unsafe()).AsVector2();

        /// <inheritdoc cref="Vector4.MinNumber(Vector4, Vector4)" />
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector2 MinNumber(Vector2 value1, Vector2 value2) => Vector128.MinNumber(value1.AsVector128Unsafe(), value2.AsVector128Unsafe()).AsVector2();

        /// <summary>Returns a new vector whose values are the product of each pair of elements in two specified vectors.</summary>
        /// <param name="left">The first vector.</param>
        /// <param name="right">The second vector.</param>
        /// <returns>The element-wise product vector.</returns>
        [Intrinsic]
        public static Vector2 Multiply(Vector2 left, Vector2 right) => left * right;

        /// <summary>Multiplies a vector by a specified scalar.</summary>
        /// <param name="left">The vector to multiply.</param>
        /// <param name="right">The scalar value.</param>
        /// <returns>The scaled vector.</returns>
        [Intrinsic]
        public static Vector2 Multiply(Vector2 left, float right) => left * right;

        /// <summary>Multiplies a scalar value by a specified vector.</summary>
        /// <param name="left">The scaled value.</param>
        /// <param name="right">The vector.</param>
        /// <returns>The scaled vector.</returns>
        [Intrinsic]
        public static Vector2 Multiply(float left, Vector2 right) => left * right;

        /// <inheritdoc cref="Vector128.MultiplyAddEstimate(Vector128{float}, Vector128{float}, Vector128{float})" />
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector2 MultiplyAddEstimate(Vector2 left, Vector2 right, Vector2 addend) => Vector128.MultiplyAddEstimate(left.AsVector128Unsafe(), right.AsVector128Unsafe(), addend.AsVector128Unsafe()).AsVector2();

        /// <summary>Negates a specified vector.</summary>
        /// <param name="value">The vector to negate.</param>
        /// <returns>The negated vector.</returns>
        [Intrinsic]
        public static Vector2 Negate(Vector2 value) => -value;

        /// <inheritdoc cref="Vector4.None(Vector4, float)" />
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool None(Vector2 vector, float value) => Vector128.None(vector, value);

        /// <inheritdoc cref="Vector4.NoneWhereAllBitsSet(Vector4)" />
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool NoneWhereAllBitsSet(Vector2 vector) => Vector128.NoneWhereAllBitsSet(vector);

        /// <summary>Returns a vector with the same direction as the specified vector, but with a length of one.</summary>
        /// <param name="value">The vector to normalize.</param>
        /// <returns>The normalized vector.</returns>
        [Intrinsic]
        public static Vector2 Normalize(Vector2 value) => value / value.Length();

        /// <inheritdoc cref="Vector4.OnesComplement(Vector4)" />
        [Intrinsic]
        public static Vector2 OnesComplement(Vector2 value) => ~value;

        /// <inheritdoc cref="Vector4.RadiansToDegrees(Vector4)" />
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector2 RadiansToDegrees(Vector2 radians) => Vector128.RadiansToDegrees(radians.AsVector128Unsafe()).AsVector2();

        /// <summary>Returns the reflection of a vector off a surface that has the specified normal.</summary>
        /// <param name="vector">The source vector.</param>
        /// <param name="normal">The normal of the surface being reflected off.</param>
        /// <returns>The reflected vector.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector2 Reflect(Vector2 vector, Vector2 normal)
        {
            // This implementation is based on the DirectX Math Library XMVector2Reflect method
            // https://github.com/microsoft/DirectXMath/blob/master/Inc/DirectXMathVector.inl

            Vector2 tmp = Create(Dot(vector, normal));
            tmp += tmp;
            return MultiplyAddEstimate(-tmp, normal, vector);
        }

        /// <inheritdoc cref="Vector4.Round(Vector4)" />
        [Intrinsic]
        public static Vector2 Round(Vector2 vector) => Vector128.Round(vector.AsVector128Unsafe()).AsVector2();

        /// <inheritdoc cref="Vector4.Round(Vector4, MidpointRounding)" />
        [Intrinsic]
        public static Vector2 Round(Vector2 vector, MidpointRounding mode) => Vector128.Round(vector.AsVector128Unsafe(), mode).AsVector2();

        /// <summary>Creates a new vector by selecting values from an input vector using a set of indices.</summary>
        /// <param name="vector">The input vector from which values are selected.</param>
        /// <param name="xIndex">The index used to select a value from <paramref name="vector" /> to be used as the value of <see cref="X" /> in the result.</param>
        /// <param name="yIndex">The index used to select a value from <paramref name="vector" /> to be used as the value of <see cref="Y" /> in the result</param>
        /// <returns>A new vector containing the values from <paramref name="vector" /> selected by the given indices.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector2 Shuffle(Vector2 vector, byte xIndex, byte yIndex)
        {
            // We do `AsVector128` instead of `AsVector128Unsafe` so that indices which
            // are out of range for Vector2 but in range for Vector128 still produce 0
            return Vector128.Shuffle(vector.AsVector128(), Vector128.Create(xIndex, yIndex, 2, 3)).AsVector2();
        }

        /// <inheritdoc cref="Vector4.Sin(Vector4)" />
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector2 Sin(Vector2 vector) => Vector128.Sin(vector.AsVector128()).AsVector2();

        /// <inheritdoc cref="Vector4.SinCos(Vector4)" />
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static (Vector2 Sin, Vector2 Cos) SinCos(Vector2 vector)
        {
            (Vector128<float> sin, Vector128<float> cos) = Vector128.SinCos(vector.AsVector128());
            return (sin.AsVector2(), cos.AsVector2());
        }

        /// <summary>Returns a vector whose elements are the square root of each of a specified vector's elements.</summary>
        /// <param name="value">A vector.</param>
        /// <returns>The square root vector.</returns>
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector2 SquareRoot(Vector2 value) => Vector128.Sqrt(value.AsVector128Unsafe()).AsVector2();

        /// <summary>Subtracts the second vector from the first.</summary>
        /// <param name="left">The first vector.</param>
        /// <param name="right">The second vector.</param>
        /// <returns>The difference vector.</returns>
        [Intrinsic]
        public static Vector2 Subtract(Vector2 left, Vector2 right) => left - right;

        /// <inheritdoc cref="Vector4.Sum(Vector4)" />
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float Sum(Vector2 value) => Vector128.Sum(value.AsVector128());

        /// <summary>Transforms a vector by a specified 3x2 matrix.</summary>
        /// <param name="position">The vector to transform.</param>
        /// <param name="matrix">The transformation matrix.</param>
        /// <returns>The transformed vector.</returns>
        public static Vector2 Transform(Vector2 position, Matrix3x2 matrix) => Transform(position, in matrix.AsImpl());

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static Vector2 Transform(Vector2 position, in Matrix3x2.Impl matrix)
        {
            Vector2 result = matrix.X * position.X;
            result = MultiplyAddEstimate(matrix.Y, Create(position.Y), result);
            return result + matrix.Z;
        }

        /// <summary>Transforms a vector by a specified 4x4 matrix.</summary>
        /// <param name="position">The vector to transform.</param>
        /// <param name="matrix">The transformation matrix.</param>
        /// <returns>The transformed vector.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector2 Transform(Vector2 position, Matrix4x4 matrix) => Vector4.Transform(position, in matrix.AsImpl()).AsVector128().AsVector2();

        /// <summary>Transforms a vector by the specified Quaternion rotation value.</summary>
        /// <param name="value">The vector to rotate.</param>
        /// <param name="rotation">The rotation to apply.</param>
        /// <returns>The transformed vector.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector2 Transform(Vector2 value, Quaternion rotation) => Vector4.Transform(value, rotation).AsVector2();

        /// <summary>Transforms a vector normal by the given 3x2 matrix.</summary>
        /// <param name="normal">The source vector.</param>
        /// <param name="matrix">The matrix.</param>
        /// <returns>The transformed vector.</returns>
        public static Vector2 TransformNormal(Vector2 normal, Matrix3x2 matrix) => TransformNormal(normal, in matrix.AsImpl());

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static Vector2 TransformNormal(Vector2 normal, in Matrix3x2.Impl matrix)
        {
            Vector2 result = matrix.X * normal.X;
            result = MultiplyAddEstimate(matrix.Y, Create(normal.Y), result);
            return result;
        }

        /// <summary>Transforms a vector normal by the given 4x4 matrix.</summary>
        /// <param name="normal">The source vector.</param>
        /// <param name="matrix">The matrix.</param>
        /// <returns>The transformed vector.</returns>
        public static Vector2 TransformNormal(Vector2 normal, Matrix4x4 matrix) => TransformNormal(normal, in matrix.AsImpl());

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static Vector2 TransformNormal(Vector2 normal, in Matrix4x4.Impl matrix)
        {
            Vector4 result = matrix.X * normal.X;
            result = Vector4.MultiplyAddEstimate(matrix.Y, Vector4.Create(normal.Y), result);
            return result.AsVector2();
        }

        /// <inheritdoc cref="Vector4.Truncate(Vector4)" />
        [Intrinsic]
        public static Vector2 Truncate(Vector2 vector) => Vector128.Truncate(vector.AsVector128Unsafe()).AsVector2();

        /// <inheritdoc cref="Vector4.Xor(Vector4, Vector4)" />
        [Intrinsic]
        public static Vector2 Xor(Vector2 left, Vector2 right) => left ^ right;

        /// <summary>Copies the elements of the vector to a specified array.</summary>
        /// <param name="array">The destination array.</param>
        /// <remarks><paramref name="array" /> must have at least two elements. The method copies the vector's elements starting at index 0.</remarks>
        /// <exception cref="NullReferenceException"><paramref name="array" /> is <see langword="null" />.</exception>
        /// <exception cref="ArgumentException">The number of elements in the current instance is greater than in the array.</exception>
        /// <exception cref="RankException"><paramref name="array" /> is multidimensional.</exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly void CopyTo(float[] array)
        {
            // We explicitly don't check for `null` because historically this has thrown `NullReferenceException` for perf reasons

            if (array.Length < ElementCount)
            {
                ThrowHelper.ThrowArgumentException_DestinationTooShort();
            }

            Unsafe.WriteUnaligned(ref Unsafe.As<float, byte>(ref array[0]), this);
        }

        /// <summary>Copies the elements of the vector to a specified array starting at a specified index position.</summary>
        /// <param name="array">The destination array.</param>
        /// <param name="index">The index at which to copy the first element of the vector.</param>
        /// <remarks><paramref name="array" /> must have a sufficient number of elements to accommodate the two vector elements. In other words, elements <paramref name="index" /> and <paramref name="index" /> + 1 must already exist in <paramref name="array" />.</remarks>
        /// <exception cref="NullReferenceException"><paramref name="array" /> is <see langword="null" />.</exception>
        /// <exception cref="ArgumentException">The number of elements in the current instance is greater than in the array.</exception>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="index" /> is less than zero.
        /// -or-
        /// <paramref name="index" /> is greater than or equal to the array length.</exception>
        /// <exception cref="RankException"><paramref name="array" /> is multidimensional.</exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly void CopyTo(float[] array, int index)
        {
            // We explicitly don't check for `null` because historically this has thrown `NullReferenceException` for perf reasons

            if ((uint)index >= (uint)array.Length)
            {
                ThrowHelper.ThrowStartIndexArgumentOutOfRange_ArgumentOutOfRange_IndexMustBeLess();
            }

            if ((array.Length - index) < ElementCount)
            {
                ThrowHelper.ThrowArgumentException_DestinationTooShort();
            }

            Unsafe.WriteUnaligned(ref Unsafe.As<float, byte>(ref array[index]), this);
        }

        /// <summary>Copies the vector to the given <see cref="Span{T}" />.The length of the destination span must be at least 2.</summary>
        /// <param name="destination">The destination span which the values are copied into.</param>
        /// <exception cref="ArgumentException">If number of elements in source vector is greater than those available in destination span.</exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly void CopyTo(Span<float> destination)
        {
            if (destination.Length < ElementCount)
            {
                ThrowHelper.ThrowArgumentException_DestinationTooShort();
            }

            Unsafe.WriteUnaligned(ref Unsafe.As<float, byte>(ref MemoryMarshal.GetReference(destination)), this);
        }

        /// <summary>Attempts to copy the vector to the given <see cref="Span{Single}" />. The length of the destination span must be at least 2.</summary>
        /// <param name="destination">The destination span which the values are copied into.</param>
        /// <returns><see langword="true" /> if the source vector was successfully copied to <paramref name="destination" />. <see langword="false" /> if <paramref name="destination" /> is not large enough to hold the source vector.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly bool TryCopyTo(Span<float> destination)
        {
            if (destination.Length < ElementCount)
            {
                return false;
            }

            Unsafe.WriteUnaligned(ref Unsafe.As<float, byte>(ref MemoryMarshal.GetReference(destination)), this);
            return true;
        }

        /// <summary>Returns a value that indicates whether this instance and a specified object are equal.</summary>
        /// <param name="obj">The object to compare with the current instance.</param>
        /// <returns><see langword="true" /> if the current instance and <paramref name="obj" /> are equal; otherwise, <see langword="false" />. If <paramref name="obj" /> is <see langword="null" />, the method returns <see langword="false" />.</returns>
        /// <remarks>The current instance and <paramref name="obj" /> are equal if <paramref name="obj" /> is a <see cref="Vector2" /> object and their <see cref="X" /> and <see cref="Y" /> elements are equal.</remarks>
        public override readonly bool Equals([NotNullWhen(true)] object? obj) => (obj is Vector2 other) && Equals(other);

        /// <summary>Returns a value that indicates whether this instance and another vector are equal.</summary>
        /// <param name="other">The other vector.</param>
        /// <returns><see langword="true" /> if the two vectors are equal; otherwise, <see langword="false" />.</returns>
        /// <remarks>Two vectors are equal if their <see cref="X" /> and <see cref="Y" /> elements are equal.</remarks>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly bool Equals(Vector2 other) => this.AsVector128().Equals(other.AsVector128());

        /// <summary>Returns the hash code for this instance.</summary>
        /// <returns>The hash code.</returns>
        public override readonly int GetHashCode() => HashCode.Combine(X, Y);

        /// <summary>Returns the length of the vector.</summary>
        /// <returns>The vector's length.</returns>
        /// <altmember cref="LengthSquared" />
        [Intrinsic]
        public readonly float Length() => float.Sqrt(LengthSquared());

        /// <summary>Returns the length of the vector squared.</summary>
        /// <returns>The vector's length squared.</returns>
        /// <remarks>This operation offers better performance than a call to the <see cref="Length" /> method.</remarks>
        /// <altmember cref="Length" />
        [Intrinsic]
        public readonly float LengthSquared() => Dot(this, this);

        /// <summary>Returns the string representation of the current instance using default formatting.</summary>
        /// <returns>The string representation of the current instance.</returns>
        /// <remarks>This method returns a string in which each element of the vector is formatted using the "G" (general) format string and the formatting conventions of the current thread culture. The "&lt;" and "&gt;" characters are used to begin and end the string, and the current culture's <see cref="NumberFormatInfo.NumberGroupSeparator" /> property followed by a space is used to separate each element.</remarks>
        public override readonly string ToString() => ToString("G", CultureInfo.CurrentCulture);

        /// <summary>Returns the string representation of the current instance using the specified format string to format individual elements.</summary>
        /// <param name="format">A standard or custom numeric format string that defines the format of individual elements.</param>
        /// <returns>The string representation of the current instance.</returns>
        /// <remarks>This method returns a string in which each element of the vector is formatted using <paramref name="format" /> and the current culture's formatting conventions. The "&lt;" and "&gt;" characters are used to begin and end the string, and the current culture's <see cref="NumberFormatInfo.NumberGroupSeparator" /> property followed by a space is used to separate each element.</remarks>
        /// <related type="Article" href="/dotnet/standard/base-types/standard-numeric-format-strings">Standard Numeric Format Strings</related>
        /// <related type="Article" href="/dotnet/standard/base-types/custom-numeric-format-strings">Custom Numeric Format Strings</related>
        public readonly string ToString([StringSyntax(StringSyntaxAttribute.NumericFormat)] string? format) => ToString(format, CultureInfo.CurrentCulture);

        /// <summary>Returns the string representation of the current instance using the specified format string to format individual elements and the specified format provider to define culture-specific formatting.</summary>
        /// <param name="format">A standard or custom numeric format string that defines the format of individual elements.</param>
        /// <param name="formatProvider">A format provider that supplies culture-specific formatting information.</param>
        /// <returns>The string representation of the current instance.</returns>
        /// <remarks>This method returns a string in which each element of the vector is formatted using <paramref name="format" /> and <paramref name="formatProvider" />. The "&lt;" and "&gt;" characters are used to begin and end the string, and the format provider's <see cref="NumberFormatInfo.NumberGroupSeparator" /> property followed by a space is used to separate each element.</remarks>
        /// <related type="Article" href="/dotnet/standard/base-types/custom-numeric-format-strings">Custom Numeric Format Strings</related>
        /// <related type="Article" href="/dotnet/standard/base-types/standard-numeric-format-strings">Standard Numeric Format Strings</related>
        public readonly string ToString([StringSyntax(StringSyntaxAttribute.NumericFormat)] string? format, IFormatProvider? formatProvider)
        {
            string separator = NumberFormatInfo.GetInstance(formatProvider).NumberGroupSeparator;

            return $"<{X.ToString(format, formatProvider)}{separator} {Y.ToString(format, formatProvider)}>";
        }
    }
}
