// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics;

namespace System.Numerics
{
    /// <summary>Represents a vector with four single-precision floating-point values.</summary>
    /// <remarks><format type="text/markdown"><![CDATA[
    /// The <xref:System.Numerics.Vector4> structure provides support for hardware acceleration.
    /// [!INCLUDE[vectors-are-rows-paragraph](~/includes/system-numerics-vectors-are-rows.md)]
    /// ]]></format></remarks>
    [Intrinsic]
    public partial struct Vector4 : IEquatable<Vector4>, IFormattable
    {
        /// <summary>Specifies the alignment of the vector as used by the <see cref="LoadAligned(float*)" /> and <see cref="Vector.StoreAligned(Vector4, float*)" /> APIs.</summary>
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
        internal const int Alignment = 16;

        /// <summary>The X component of the vector.</summary>
        public float X;

        /// <summary>The Y component of the vector.</summary>
        public float Y;

        /// <summary>The Z component of the vector.</summary>
        public float Z;

        /// <summary>The W component of the vector.</summary>
        public float W;

        internal const int ElementCount = 4;

        /// <summary>Creates a new <see cref="Vector4" /> object whose four elements have the same value.</summary>
        /// <param name="value">The value to assign to all four elements.</param>
        [Intrinsic]
        public Vector4(float value)
        {
            this = Create(value);
        }

        /// <summary>Creates a   new <see cref="Vector4" /> object from the specified <see cref="Vector2" /> object and a Z and a W component.</summary>
        /// <param name="value">The vector to use for the X and Y components.</param>
        /// <param name="z">The Z component.</param>
        /// <param name="w">The W component.</param>
        [Intrinsic]
        public Vector4(Vector2 value, float z, float w)
        {
            this = Create(value, z, w);
        }

        /// <summary>Constructs a new <see cref="Vector4" /> object from the specified <see cref="Vector3" /> object and a W component.</summary>
        /// <param name="value">The vector to use for the X, Y, and Z components.</param>
        /// <param name="w">The W component.</param>
        [Intrinsic]
        public Vector4(Vector3 value, float w)
        {
            this = Create(value, w);
        }

        /// <summary>Creates a vector whose elements have the specified values.</summary>
        /// <param name="x">The value to assign to the <see cref="X" /> field.</param>
        /// <param name="y">The value to assign to the <see cref="Y" /> field.</param>
        /// <param name="z">The value to assign to the <see cref="Z" /> field.</param>
        /// <param name="w">The value to assign to the <see cref="W" /> field.</param>
        [Intrinsic]
        public Vector4(float x, float y, float z, float w)
        {
            this = Create(x, y, z, w);
        }

        /// <summary>Constructs a vector from the given <see cref="ReadOnlySpan{Single}" />. The span must contain at least 4 elements.</summary>
        /// <param name="values">The span of elements to assign to the vector.</param>
        [Intrinsic]
        public Vector4(ReadOnlySpan<float> values)
        {
            this = Create(values);
        }

        /// <summary>Gets a vector where all bits are set to <c>1</c>.</summary>
        /// <value>A vector where all bits are set to <c>1</c>.</value>
        public static Vector4 AllBitsSet
        {
            [Intrinsic]
            get => Vector128<float>.AllBitsSet.AsVector4();
        }

        /// <summary>Gets a vector whose elements are equal to <see cref="float.E" />.</summary>
        /// <value>A vector whose elements are equal to <see cref="float.E" /> (that is, it returns the vector <c>Create(float.E)</c>).</value>
        public static Vector4 E
        {
            [Intrinsic]
            get => Create(float.E);
        }

        /// <summary>Gets a vector whose elements are equal to <see cref="float.Epsilon" />.</summary>
        /// <value>A vector whose elements are equal to <see cref="float.Epsilon" /> (that is, it returns the vector <c>Create(float.Epsilon)</c>).</value>
        public static Vector4 Epsilon
        {
            [Intrinsic]
            get => Create(float.Epsilon);
        }

        /// <summary>Gets a vector whose elements are equal to <see cref="float.NaN" />.</summary>
        /// <value>A vector whose elements are equal to <see cref="float.NaN" /> (that is, it returns the vector <c>Create(float.NaN)</c>).</value>
        public static Vector4 NaN
        {
            [Intrinsic]
            get => Create(float.NaN);
        }

        /// <summary>Gets a vector whose elements are equal to <see cref="float.NegativeInfinity" />.</summary>
        /// <value>A vector whose elements are equal to <see cref="float.NegativeInfinity" /> (that is, it returns the vector <c>Create(float.NegativeInfinity)</c>).</value>
        public static Vector4 NegativeInfinity
        {
            [Intrinsic]
            get => Create(float.NegativeInfinity);
        }

        /// <summary>Gets a vector whose elements are equal to <see cref="float.NegativeZero" />.</summary>
        /// <value>A vector whose elements are equal to <see cref="float.NegativeZero" /> (that is, it returns the vector <c>Create(float.NegativeZero)</c>).</value>
        public static Vector4 NegativeZero
        {
            [Intrinsic]
            get => Create(float.NegativeZero);
        }

        /// <summary>Gets a vector whose elements are equal to one.</summary>
        /// <value>A vector whose elements are equal to one (that is, it returns the vector <c>Create(1)</c>).</value>
        public static Vector4 One
        {
            [Intrinsic]
            get => Create(1);
        }

        /// <summary>Gets a vector whose elements are equal to <see cref="float.Pi" />.</summary>
        /// <value>A vector whose elements are equal to <see cref="float.Pi" /> (that is, it returns the vector <c>Create(float.Pi)</c>).</value>
        public static Vector4 Pi
        {
            [Intrinsic]
            get => Create(float.Pi);
        }

        /// <summary>Gets a vector whose elements are equal to <see cref="float.PositiveInfinity" />.</summary>
        /// <value>A vector whose elements are equal to <see cref="float.PositiveInfinity" /> (that is, it returns the vector <c>Create(float.PositiveInfinity)</c>).</value>
        public static Vector4 PositiveInfinity
        {
            [Intrinsic]
            get => Create(float.PositiveInfinity);
        }

        /// <summary>Gets a vector whose elements are equal to <see cref="float.Tau" />.</summary>
        /// <value>A vector whose elements are equal to <see cref="float.Tau" /> (that is, it returns the vector <c>Create(float.Tau)</c>).</value>
        public static Vector4 Tau
        {
            [Intrinsic]
            get => Create(float.Tau);
        }

        /// <summary>Gets the vector (1,0,0,0).</summary>
        /// <value>The vector <c>(1,0,0,0)</c>.</value>
        public static Vector4 UnitX
        {
            [Intrinsic]
            get => CreateScalar(1.0f);
        }

        /// <summary>Gets the vector (0,1,0,0).</summary>
        /// <value>The vector <c>(0,1,0,0)</c>.</value>
        public static Vector4 UnitY
        {
            [Intrinsic]
            get => Create(0.0f, 1.0f, 0.0f, 0.0f);
        }

        /// <summary>Gets the vector (0,0,1,0).</summary>
        /// <value>The vector <c>(0,0,1,0)</c>.</value>
        public static Vector4 UnitZ
        {
            [Intrinsic]
            get => Create(0.0f, 0.0f, 1.0f, 0.0f);
        }

        /// <summary>Gets the vector (0,0,0,1).</summary>
        /// <value>The vector <c>(0,0,0,1)</c>.</value>
        public static Vector4 UnitW
        {
            [Intrinsic]
            get => Create(0.0f, 0.0f, 0.0f, 1.0f);
        }

        /// <summary>Gets a vector whose elements are equal to zero.</summary>
        /// <value>A vector whose elements are equal to zero (that is, it returns the vector <c>Create(0)</c>).</value>
        public static Vector4 Zero
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
        /// <remarks>The <see cref="op_Addition" /> method defines the addition operation for <see cref="Vector4" /> objects.</remarks>
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector4 operator +(Vector4 left, Vector4 right) => (left.AsVector128() + right.AsVector128()).AsVector4();

        /// <summary>Divides the first vector by the second.</summary>
        /// <param name="left">The first vector.</param>
        /// <param name="right">The second vector.</param>
        /// <returns>The vector that results from dividing <paramref name="left" /> by <paramref name="right" />.</returns>
        /// <remarks>The <see cref="Vector4.op_Division" /> method defines the division operation for <see cref="Vector4" /> objects.</remarks>
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector4 operator /(Vector4 left, Vector4 right) => (left.AsVector128() / right.AsVector128()).AsVector4();

        /// <summary>Divides the specified vector by a specified scalar value.</summary>
        /// <param name="value1">The vector.</param>
        /// <param name="value2">The scalar value.</param>
        /// <returns>The result of the division.</returns>
        /// <remarks>The <see cref="Vector4.op_Division" /> method defines the division operation for <see cref="Vector4" /> objects.</remarks>
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector4 operator /(Vector4 value1, float value2) => (value1.AsVector128() / value2).AsVector4();

        /// <summary>Returns a value that indicates whether each pair of elements in two specified vectors is equal.</summary>
        /// <param name="left">The first vector to compare.</param>
        /// <param name="right">The second vector to compare.</param>
        /// <returns><see langword="true" /> if <paramref name="left" /> and <paramref name="right" /> are equal; otherwise, <see langword="false" />.</returns>
        /// <remarks>Two <see cref="Vector4" /> objects are equal if each element in <paramref name="left" /> is equal to the corresponding element in <paramref name="right" />.</remarks>
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator ==(Vector4 left, Vector4 right) => left.AsVector128() == right.AsVector128();

        /// <summary>Returns a value that indicates whether two specified vectors are not equal.</summary>
        /// <param name="left">The first vector to compare.</param>
        /// <param name="right">The second vector to compare.</param>
        /// <returns><see langword="true" /> if <paramref name="left" /> and <paramref name="right" /> are not equal; otherwise, <see langword="false" />.</returns>
        [Intrinsic]
        public static bool operator !=(Vector4 left, Vector4 right) => !(left == right);

        /// <summary>Returns a new vector whose values are the product of each pair of elements in two specified vectors.</summary>
        /// <param name="left">The first vector.</param>
        /// <param name="right">The second vector.</param>
        /// <returns>The element-wise product vector.</returns>
        /// <remarks>The <see cref="Vector4.op_Multiply" /> method defines the multiplication operation for <see cref="Vector4" /> objects.</remarks>
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector4 operator *(Vector4 left, Vector4 right) => (left.AsVector128() * right.AsVector128()).AsVector4();

        /// <summary>Multiplies the specified vector by the specified scalar value.</summary>
        /// <param name="left">The vector.</param>
        /// <param name="right">The scalar value.</param>
        /// <returns>The scaled vector.</returns>
        /// <remarks>The <see cref="Vector4.op_Multiply" /> method defines the multiplication operation for <see cref="Vector4" /> objects.</remarks>
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector4 operator *(Vector4 left, float right) => (left.AsVector128() * right).AsVector4();

        /// <summary>Multiplies the scalar value by the specified vector.</summary>
        /// <param name="left">The vector.</param>
        /// <param name="right">The scalar value.</param>
        /// <returns>The scaled vector.</returns>
        /// <remarks>The <see cref="Vector4.op_Multiply" /> method defines the multiplication operation for <see cref="Vector4" /> objects.</remarks>
        [Intrinsic]
        public static Vector4 operator *(float left, Vector4 right) => right * left;

        /// <summary>Subtracts the second vector from the first.</summary>
        /// <param name="left">The first vector.</param>
        /// <param name="right">The second vector.</param>
        /// <returns>The vector that results from subtracting <paramref name="right" /> from <paramref name="left" />.</returns>
        /// <remarks>The <see cref="op_Subtraction" /> method defines the subtraction operation for <see cref="Vector4" /> objects.</remarks>
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector4 operator -(Vector4 left, Vector4 right) => (left.AsVector128() - right.AsVector128()).AsVector4();

        /// <summary>Negates the specified vector.</summary>
        /// <param name="value">The vector to negate.</param>
        /// <returns>The negated vector.</returns>
        /// <remarks>The <see cref="op_UnaryNegation" /> method defines the unary negation operation for <see cref="Vector4" /> objects.</remarks>
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector4 operator -(Vector4 value) => (-value.AsVector128()).AsVector4();

        /// <inheritdoc cref="Vector128{T}.op_BitwiseAnd(Vector128{T}, Vector128{T})" />
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector4 operator &(Vector4 left, Vector4 right) => (left.AsVector128() & right.AsVector128()).AsVector4();

        /// <inheritdoc cref="Vector128{T}.op_BitwiseOr(Vector128{T}, Vector128{T})" />
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector4 operator |(Vector4 left, Vector4 right) => (left.AsVector128() | right.AsVector128()).AsVector4();

        /// <inheritdoc cref="Vector128{T}.op_ExclusiveOr(Vector128{T}, Vector128{T})" />
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector4 operator ^(Vector4 left, Vector4 right) => (left.AsVector128() ^ right.AsVector128()).AsVector4();

        /// <inheritdoc cref="Vector128{T}.op_LeftShift(Vector128{T}, int)" />
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector4 operator <<(Vector4 value, int shiftAmount) => (value.AsVector128() << shiftAmount).AsVector4();

        /// <inheritdoc cref="Vector128{T}.op_OnesComplement(Vector128{T})" />
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector4 operator ~(Vector4 value) => (~value.AsVector128()).AsVector4();

        /// <inheritdoc cref="Vector128{T}.op_RightShift(Vector128{T}, int)" />
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector4 operator >>(Vector4 value, int shiftAmount) => (value.AsVector128() >> shiftAmount).AsVector4();

        /// <inheritdoc cref="Vector128{T}.op_UnaryPlus(Vector128{T})" />
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector4 operator +(Vector4 value) => value;

        /// <inheritdoc cref="Vector128{T}.op_UnsignedRightShift(Vector128{T}, int)" />
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector4 operator >>>(Vector4 value, int shiftAmount) => (value.AsVector128() >>> shiftAmount).AsVector4();

        /// <summary>Returns a vector whose elements are the absolute values of each of the specified vector's elements.</summary>
        /// <param name="value">A vector.</param>
        /// <returns>The absolute value vector.</returns>
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector4 Abs(Vector4 value) => Vector128.Abs(value.AsVector128()).AsVector4();

        /// <summary>Adds two vectors together.</summary>
        /// <param name="left">The first vector to add.</param>
        /// <param name="right">The second vector to add.</param>
        /// <returns>The summed vector.</returns>
        [Intrinsic]
        public static Vector4 Add(Vector4 left, Vector4 right) => left + right;

        /// <inheritdoc cref="Vector128.All{T}(Vector128{T}, T)" />
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool All(Vector4 vector, float value) => Vector128.All(vector.AsVector128(), value);

        /// <inheritdoc cref="Vector128.AllWhereAllBitsSet{T}(Vector128{T})" />
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool AllWhereAllBitsSet(Vector4 vector) => Vector128.AllWhereAllBitsSet(vector.AsVector128());

        /// <inheritdoc cref="Vector128.AndNot{T}(Vector128{T}, Vector128{T})" />
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector4 AndNot(Vector4 left, Vector4 right) => Vector128.AndNot(left.AsVector128(), right.AsVector128()).AsVector4();

        /// <inheritdoc cref="Vector128.Any{T}(Vector128{T}, T)" />
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool Any(Vector4 vector, float value) => Vector128.Any(vector.AsVector128(), value);

        /// <inheritdoc cref="Vector128.AnyWhereAllBitsSet{T}(Vector128{T})" />
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool AnyWhereAllBitsSet(Vector4 vector) => Vector128.AnyWhereAllBitsSet(vector.AsVector128());

        /// <inheritdoc cref="Vector128.BitwiseAnd{T}(Vector128{T}, Vector128{T})" />
        [Intrinsic]
        public static Vector4 BitwiseAnd(Vector4 left, Vector4 right) => left & right;

        /// <inheritdoc cref="Vector128.BitwiseOr{T}(Vector128{T}, Vector128{T})" />
        [Intrinsic]
        public static Vector4 BitwiseOr(Vector4 left, Vector4 right) => left | right;

        /// <inheritdoc cref="Vector128.Clamp{T}(Vector128{T}, Vector128{T}, Vector128{T})" />
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector4 Clamp(Vector4 value1, Vector4 min, Vector4 max) => Vector128.Clamp(value1.AsVector128(), min.AsVector128(), max.AsVector128()).AsVector4();

        /// <inheritdoc cref="Vector128.ClampNative{T}(Vector128{T}, Vector128{T}, Vector128{T})" />
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector4 ClampNative(Vector4 value1, Vector4 min, Vector4 max) => Vector128.ClampNative(value1.AsVector128(), min.AsVector128(), max.AsVector128()).AsVector4();

        /// <inheritdoc cref="Vector128.ConditionalSelect{T}(Vector128{T}, Vector128{T}, Vector128{T})" />
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector4 ConditionalSelect(Vector4 condition, Vector4 left, Vector4 right) => Vector128.ConditionalSelect(condition.AsVector128(), left.AsVector128(), right.AsVector128()).AsVector4();

        /// <inheritdoc cref="Vector128.CopySign{T}(Vector128{T}, Vector128{T})" />
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector4 CopySign(Vector4 value, Vector4 sign) => Vector128.CopySign(value.AsVector128(), sign.AsVector128()).AsVector4();

        /// <inheritdoc cref="Vector128.Cos(Vector128{float})" />
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector4 Cos(Vector4 vector) => Vector128.Cos(vector.AsVector128()).AsVector4();

        /// <inheritdoc cref="Vector128.Count{T}(Vector128{T}, T)" />
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int Count(Vector4 vector, float value) => Vector128.Count(vector.AsVector128(), value);

        /// <inheritdoc cref="Vector128.CountWhereAllBitsSet{T}(Vector128{T})" />
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int CountWhereAllBitsSet(Vector4 vector) => Vector128.CountWhereAllBitsSet(vector.AsVector128());

        /// <summary>Creates a new <see cref="Vector4" /> object whose four elements have the same value.</summary>
        /// <param name="value">The value to assign to all four elements.</param>
        /// <returns>A new <see cref="Vector4" /> whose four elements have the same value.</returns>
        [Intrinsic]
        public static Vector4 Create(float value) => Vector128.Create(value).AsVector4();

        /// <summary>Creates a new <see cref="Vector4" /> object from the specified <see cref="Vector2" /> object and a Z and a W component.</summary>
        /// <param name="vector">The vector to use for the X and Y components.</param>
        /// <param name="z">The Z component.</param>
        /// <param name="w">The W component.</param>
        /// <returns>A new <see cref="Vector4" /> from the specified <see cref="Vector2" /> object and a Z and a W component.</returns>
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector4 Create(Vector2 vector, float z, float w)
        {
            return vector.AsVector128Unsafe()
                         .WithElement(2, z)
                         .WithElement(3, w)
                         .AsVector4();
        }

        /// <summary>Constructs a new <see cref="Vector4" /> object from the specified <see cref="Vector3" /> object and a W component.</summary>
        /// <param name="vector">The vector to use for the X, Y, and Z components.</param>
        /// <param name="w">The W component.</param>
        /// <returns>A new <see cref="Vector4" /> from the specified <see cref="Vector3" /> object and a W component.</returns>
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector4 Create(Vector3 vector, float w)
        {
            return vector.AsVector128Unsafe()
                         .WithElement(3, w)
                         .AsVector4();
        }

        /// <summary>Creates a vector whose elements have the specified values.</summary>
        /// <param name="x">The value to assign to the <see cref="X" /> field.</param>
        /// <param name="y">The value to assign to the <see cref="Y" /> field.</param>
        /// <param name="z">The value to assign to the <see cref="Z" /> field.</param>
        /// <param name="w">The value to assign to the <see cref="W" /> field.</param>
        /// <returns>A new <see cref="Vector4" /> whose elements have the specified values.</returns>
        [Intrinsic]
        public static Vector4 Create(float x, float y, float z, float w) => Vector128.Create(x, y, z, w).AsVector4();

        /// <summary>Constructs a vector from the given <see cref="ReadOnlySpan{Single}" />. The span must contain at least 4 elements.</summary>
        /// <param name="values">The span of elements to assign to the vector.</param>
        /// <returns>A new <see cref="Vector4" /> whose elements have the specified values.</returns>
        [Intrinsic]
        public static Vector4 Create(ReadOnlySpan<float> values) => Vector128.Create(values).AsVector4();

        /// <summary>Creates a vector with <see cref="X" /> initialized to the specified value and the remaining elements initialized to zero.</summary>
        /// <param name="x">The value to assign to the <see cref="X" /> field.</param>
        /// <returns>A new <see cref="Vector4" /> with <see cref="X" /> initialized <paramref name="x" /> and the remaining elements initialized to zero.</returns>
        [Intrinsic]
        internal static Vector4 CreateScalar(float x) => Vector128.CreateScalar(x).AsVector4();

        /// <summary>Creates a vector with <see cref="X" /> initialized to the specified value and the remaining elements left uninitialized.</summary>
        /// <param name="x">The value to assign to the <see cref="X" /> field.</param>
        /// <returns>A new <see cref="Vector4" /> with <see cref="X" /> initialized <paramref name="x" /> and the remaining elements left uninitialized.</returns>
        [Intrinsic]
        internal static Vector4 CreateScalarUnsafe(float x) => Vector128.CreateScalarUnsafe(x).AsVector4();

        /// <summary>
        /// Computes the cross product of two vectors. For homogeneous coordinates,
        /// the product of the weights is the new weight for the resulting product.
        /// </summary>
        /// <param name="vector1">The first vector.</param>
        /// <param name="vector2">The second vector.</param>
        /// <returns>The cross product.</returns>
        /// <remarks>
        /// The proposed Cross function for <see cref="Vector4"/> is nearly the same as that for
        /// <see cref="Vector3.Cross"/> with the addition of the fourth value which is
        /// the product of the original two w's. This can be derived by symbolically performing
        /// the cross product for <see cref="Vector3"/> with values [x_1/w_1, y_1/w_1, z_1/w_1]
        /// and [x_2/w_2, y_2/w_2, z_2/w_2].
        /// </remarks>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector4 Cross(Vector4 vector1, Vector4 vector2)
        {
            //return new Vector4(
            //    (vector1.Y * vector2.Z) - (vector1.Z * vector2.Y),
            //    (vector1.Z * vector2.X) - (vector1.X * vector2.Z),
            //    (vector1.X * vector2.Y) - (vector1.Y * vector2.X),
            //    (vector1.W * vector2.W)
            //);

            // This implementation is based on the DirectX Math Library XMVector3Cross method
            // https://github.com/microsoft/DirectXMath/blob/master/Inc/DirectXMathVector.inl

            Vector128<float> v1 = vector1.AsVector128();
            Vector128<float> v2 = vector2.AsVector128();

            Vector128<float> m2 = Vector128.Shuffle(v1, Vector128.Create(2, 0, 1, 3)) *
                 Vector128.Shuffle(v2, Vector128.Create(1, 2, 0, 3));

            return Vector128.MultiplyAddEstimate(
                Vector128.Shuffle(v1, Vector128.Create(1, 2, 0, 3)),
                Vector128.Shuffle(v2, Vector128.Create(2, 0, 1, 3)),
                -m2.WithElement(3, 0)
            ).AsVector4();
        }

        /// <inheritdoc cref="Vector128.DegreesToRadians(Vector128{float})" />
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector4 DegreesToRadians(Vector4 degrees) => Vector128.DegreesToRadians(degrees.AsVector128()).AsVector4();

        /// <summary>Computes the Euclidean distance between the two given points.</summary>
        /// <param name="value1">The first point.</param>
        /// <param name="value2">The second point.</param>
        /// <returns>The distance.</returns>
        [Intrinsic]
        public static float Distance(Vector4 value1, Vector4 value2) => float.Sqrt(DistanceSquared(value1, value2));

        /// <summary>Returns the Euclidean distance squared between two specified points.</summary>
        /// <param name="value1">The first point.</param>
        /// <param name="value2">The second point.</param>
        /// <returns>The distance squared.</returns>
        [Intrinsic]
        public static float DistanceSquared(Vector4 value1, Vector4 value2) => (value1 - value2).LengthSquared();

        /// <summary>Divides the first vector by the second.</summary>
        /// <param name="left">The first vector.</param>
        /// <param name="right">The second vector.</param>
        /// <returns>The vector resulting from the division.</returns>
        [Intrinsic]
        public static Vector4 Divide(Vector4 left, Vector4 right) => left / right;

        /// <summary>Divides the specified vector by a specified scalar value.</summary>
        /// <param name="left">The vector.</param>
        /// <param name="divisor">The scalar value.</param>
        /// <returns>The vector that results from the division.</returns>
        [Intrinsic]
        public static Vector4 Divide(Vector4 left, float divisor) => left / divisor;

        /// <summary>Returns the dot product of two vectors.</summary>
        /// <param name="vector1">The first vector.</param>
        /// <param name="vector2">The second vector.</param>
        /// <returns>The dot product.</returns>
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float Dot(Vector4 vector1, Vector4 vector2) => Vector128.Dot(vector1.AsVector128(), vector2.AsVector128());

        /// <inheritdoc cref="Vector128.Exp(Vector128{float})" />
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector4 Exp(Vector4 vector) => Vector128.Exp(vector.AsVector128()).AsVector4();

        /// <inheritdoc cref="Vector128.Equals{T}(Vector128{T}, Vector128{T})" />
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector4 Equals(Vector4 left, Vector4 right) => Vector128.Equals(left.AsVector128(), right.AsVector128()).AsVector4();

        /// <inheritdoc cref="Vector128.EqualsAll{T}(Vector128{T}, Vector128{T})" />
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool EqualsAll(Vector4 left, Vector4 right) => Vector128.EqualsAll(left.AsVector128(), right.AsVector128());

        /// <inheritdoc cref="Vector128.EqualsAny{T}(Vector128{T}, Vector128{T})" />
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool EqualsAny(Vector4 left, Vector4 right) => Vector128.EqualsAny(left.AsVector128(), right.AsVector128());

        /// <inheritdoc cref="Vector128.MultiplyAddEstimate(Vector128{float}, Vector128{float}, Vector128{float})" />
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector4 FusedMultiplyAdd(Vector4 left, Vector4 right, Vector4 addend) => Vector128.FusedMultiplyAdd(left.AsVector128(), right.AsVector128(), addend.AsVector128()).AsVector4();

        /// <inheritdoc cref="Vector128.GreaterThan{T}(Vector128{T}, Vector128{T})" />
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector4 GreaterThan(Vector4 left, Vector4 right) => Vector128.GreaterThan(left.AsVector128(), right.AsVector128()).AsVector4();

        /// <inheritdoc cref="Vector128.GreaterThanAll{T}(Vector128{T}, Vector128{T})" />
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool GreaterThanAll(Vector4 left, Vector4 right) => Vector128.GreaterThanAll(left.AsVector128(), right.AsVector128());

        /// <inheritdoc cref="Vector128.GreaterThanAny{T}(Vector128{T}, Vector128{T})" />
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool GreaterThanAny(Vector4 left, Vector4 right) => Vector128.GreaterThanAny(left.AsVector128(), right.AsVector128());

        /// <inheritdoc cref="Vector128.GreaterThanOrEqual{T}(Vector128{T}, Vector128{T})" />
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector4 GreaterThanOrEqual(Vector4 left, Vector4 right) => Vector128.GreaterThanOrEqual(left.AsVector128(), right.AsVector128()).AsVector4();

        /// <inheritdoc cref="Vector128.GreaterThanOrEqualAll{T}(Vector128{T}, Vector128{T})" />
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool GreaterThanOrEqualAll(Vector4 left, Vector4 right) => Vector128.GreaterThanOrEqualAll(left.AsVector128(), right.AsVector128());

        /// <inheritdoc cref="Vector128.GreaterThanOrEqualAny{T}(Vector128{T}, Vector128{T})" />
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool GreaterThanOrEqualAny(Vector4 left, Vector4 right) => Vector128.GreaterThanOrEqualAny(left.AsVector128(), right.AsVector128());

        /// <inheritdoc cref="Vector128.Hypot(Vector128{float}, Vector128{float})" />
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector4 Hypot(Vector4 x, Vector4 y) => Vector128.Hypot(x.AsVector128(), y.AsVector128()).AsVector4();

        /// <inheritdoc cref="Vector128.IndexOf{T}(Vector128{T}, T)" />
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int IndexOf(Vector4 vector, float value) => Vector128.IndexOf(vector.AsVector128(), value);

        /// <inheritdoc cref="Vector128.IndexOfWhereAllBitsSet{T}(Vector128{T})" />
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int IndexOfWhereAllBitsSet(Vector4 vector) => Vector128.IndexOfWhereAllBitsSet(vector.AsVector128());

        /// <inheritdoc cref="Vector128.IsEvenInteger{T}(Vector128{T})" />
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector4 IsEvenInteger(Vector4 vector) => Vector128.IsEvenInteger(vector.AsVector128()).AsVector4();

        /// <inheritdoc cref="Vector128.IsFinite{T}(Vector128{T})" />
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector4 IsFinite(Vector4 vector) => Vector128.IsFinite(vector.AsVector128()).AsVector4();

        /// <inheritdoc cref="Vector128.IsInfinity{T}(Vector128{T})" />
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector4 IsInfinity(Vector4 vector) => Vector128.IsInfinity(vector.AsVector128()).AsVector4();

        /// <inheritdoc cref="Vector128.IsInteger{T}(Vector128{T})" />
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector4 IsInteger(Vector4 vector) => Vector128.IsInteger(vector.AsVector128()).AsVector4();

        /// <inheritdoc cref="Vector128.IsNaN{T}(Vector128{T})" />
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector4 IsNaN(Vector4 vector) => Vector128.IsNaN(vector.AsVector128()).AsVector4();

        /// <inheritdoc cref="Vector128.IsNegative{T}(Vector128{T})" />
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector4 IsNegative(Vector4 vector) => Vector128.IsNegative(vector.AsVector128()).AsVector4();

        /// <inheritdoc cref="Vector128.IsNegativeInfinity{T}(Vector128{T})" />
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector4 IsNegativeInfinity(Vector4 vector) => Vector128.IsNegativeInfinity(vector.AsVector128()).AsVector4();

        /// <inheritdoc cref="Vector128.IsNormal{T}(Vector128{T})" />
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector4 IsNormal(Vector4 vector) => Vector128.IsNormal(vector.AsVector128()).AsVector4();

        /// <inheritdoc cref="Vector128.IsOddInteger{T}(Vector128{T})" />
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector4 IsOddInteger(Vector4 vector) => Vector128.IsOddInteger(vector.AsVector128()).AsVector4();

        /// <inheritdoc cref="Vector128.IsPositive{T}(Vector128{T})" />
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector4 IsPositive(Vector4 vector) => Vector128.IsPositive(vector.AsVector128()).AsVector4();

        /// <inheritdoc cref="Vector128.IsPositiveInfinity{T}(Vector128{T})" />
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector4 IsPositiveInfinity(Vector4 vector) => Vector128.IsPositiveInfinity(vector.AsVector128()).AsVector4();

        /// <inheritdoc cref="Vector128.IsSubnormal{T}(Vector128{T})" />
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector4 IsSubnormal(Vector4 vector) => Vector128.IsSubnormal(vector.AsVector128()).AsVector4();

        /// <inheritdoc cref="Vector128.IsZero{T}(Vector128{T})" />
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector4 IsZero(Vector4 vector) => Vector128.IsZero(vector.AsVector128()).AsVector4();

        /// <inheritdoc cref="Vector128.LastIndexOf{T}(Vector128{T}, T)" />
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int LastIndexOf(Vector4 vector, float value) => Vector128.LastIndexOf(vector.AsVector128(), value);

        /// <inheritdoc cref="Vector128.LastIndexOfWhereAllBitsSet{T}(Vector128{T})" />
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int LastIndexOfWhereAllBitsSet(Vector4 vector) => Vector128.LastIndexOfWhereAllBitsSet(vector.AsVector128());

        /// <inheritdoc cref="Lerp(Vector4, Vector4, Vector4)" />
        /// <remarks><format type="text/markdown"><![CDATA[
        /// The behavior of this method changed in .NET 5.0. For more information, see [Behavior change for Vector2.Lerp and Vector4.Lerp](/dotnet/core/compatibility/3.1-5.0#behavior-change-for-vector2lerp-and-vector4lerp).
        /// ]]></format></remarks>
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector4 Lerp(Vector4 value1, Vector4 value2, float amount) => Lerp(value1, value2, Create(amount));

        /// <inheritdoc cref="Vector128.Lerp(Vector128{float}, Vector128{float}, Vector128{float})" />
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector4 Lerp(Vector4 value1, Vector4 value2, Vector4 amount) => Vector128.Lerp(value1.AsVector128(), value2.AsVector128(), amount.AsVector128()).AsVector4();

        /// <inheritdoc cref="Vector128.LessThan{T}(Vector128{T}, Vector128{T})" />
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector4 LessThan(Vector4 left, Vector4 right) => Vector128.LessThan(left.AsVector128(), right.AsVector128()).AsVector4();

        /// <inheritdoc cref="Vector128.LessThanAll{T}(Vector128{T}, Vector128{T})" />
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool LessThanAll(Vector4 left, Vector4 right) => Vector128.LessThanAll(left.AsVector128(), right.AsVector128());

        /// <inheritdoc cref="Vector128.LessThanAny{T}(Vector128{T}, Vector128{T})" />
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool LessThanAny(Vector4 left, Vector4 right) => Vector128.LessThanAny(left.AsVector128(), right.AsVector128());

        /// <inheritdoc cref="Vector128.LessThanOrEqual{T}(Vector128{T}, Vector128{T})" />
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector4 LessThanOrEqual(Vector4 left, Vector4 right) => Vector128.LessThanOrEqual(left.AsVector128(), right.AsVector128()).AsVector4();

        /// <inheritdoc cref="Vector128.LessThanOrEqualAll{T}(Vector128{T}, Vector128{T})" />
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool LessThanOrEqualAll(Vector4 left, Vector4 right) => Vector128.LessThanOrEqualAll(left.AsVector128(), right.AsVector128());

        /// <inheritdoc cref="Vector128.LessThanOrEqualAny{T}(Vector128{T}, Vector128{T})" />
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool LessThanOrEqualAny(Vector4 left, Vector4 right) => Vector128.LessThanOrEqualAny(left.AsVector128(), right.AsVector128());

        /// <inheritdoc cref="Vector128.Load{T}(T*)" />
        [Intrinsic]
        [CLSCompliant(false)]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe Vector4 Load(float* source) => Vector128.Load(source).AsVector4();

        /// <inheritdoc cref="Vector128.LoadAligned{T}(T*)" />
        [Intrinsic]
        [CLSCompliant(false)]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe Vector4 LoadAligned(float* source) => Vector128.LoadAligned(source).AsVector4();

        /// <inheritdoc cref="Vector128.LoadAlignedNonTemporal{T}(T*)" />
        [Intrinsic]
        [CLSCompliant(false)]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe Vector4 LoadAlignedNonTemporal(float* source) => Vector128.LoadAlignedNonTemporal(source).AsVector4();

        /// <inheritdoc cref="Vector128.LoadUnsafe{T}(ref readonly T)" />
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector4 LoadUnsafe(ref readonly float source) => Vector128.LoadUnsafe(in source).AsVector4();

        /// <inheritdoc cref="Vector128.LoadUnsafe{T}(ref readonly T, nuint)" />
        [Intrinsic]
        [CLSCompliant(false)]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector4 LoadUnsafe(ref readonly float source, nuint elementOffset) => Vector128.LoadUnsafe(in source, elementOffset).AsVector4();

        /// <inheritdoc cref="Vector128.Log(Vector128{float})" />
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector4 Log(Vector4 vector) => Vector128.Log(vector.AsVector128()).AsVector4();

        /// <inheritdoc cref="Vector128.Log2(Vector128{float})" />
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector4 Log2(Vector4 vector) => Vector128.Log2(vector.AsVector128()).AsVector4();

        /// <inheritdoc cref="Vector128.Max{T}(Vector128{T}, Vector128{T})" />
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector4 Max(Vector4 value1, Vector4 value2) => Vector128.Max(value1.AsVector128(), value2.AsVector128()).AsVector4();

        /// <inheritdoc cref="Vector128.MaxMagnitude{T}(Vector128{T}, Vector128{T})" />
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector4 MaxMagnitude(Vector4 value1, Vector4 value2) => Vector128.MaxMagnitude(value1.AsVector128(), value2.AsVector128()).AsVector4();

        /// <inheritdoc cref="Vector128.MaxMagnitudeNumber{T}(Vector128{T}, Vector128{T})" />
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector4 MaxMagnitudeNumber(Vector4 value1, Vector4 value2) => Vector128.MaxMagnitudeNumber(value1.AsVector128(), value2.AsVector128()).AsVector4();

        /// <inheritdoc cref="Vector128.MaxNative{T}(Vector128{T}, Vector128{T})" />
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector4 MaxNative(Vector4 value1, Vector4 value2) => Vector128.MaxNative(value1.AsVector128(), value2.AsVector128()).AsVector4();

        /// <inheritdoc cref="Vector128.MaxNumber{T}(Vector128{T}, Vector128{T})" />
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector4 MaxNumber(Vector4 value1, Vector4 value2) => Vector128.MaxNumber(value1.AsVector128(), value2.AsVector128()).AsVector4();

        /// <inheritdoc cref="Vector128.Min{T}(Vector128{T}, Vector128{T})" />
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector4 Min(Vector4 value1, Vector4 value2) => Vector128.Min(value1.AsVector128(), value2.AsVector128()).AsVector4();

        /// <inheritdoc cref="Vector128.MinMagnitude{T}(Vector128{T}, Vector128{T})" />
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector4 MinMagnitude(Vector4 value1, Vector4 value2) => Vector128.MinMagnitude(value1.AsVector128(), value2.AsVector128()).AsVector4();

        /// <inheritdoc cref="Vector128.MinMagnitudeNumber{T}(Vector128{T}, Vector128{T})" />
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector4 MinMagnitudeNumber(Vector4 value1, Vector4 value2) => Vector128.MinMagnitudeNumber(value1.AsVector128(), value2.AsVector128()).AsVector4();

        /// <inheritdoc cref="Vector128.MinNative{T}(Vector128{T}, Vector128{T})" />
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector4 MinNative(Vector4 value1, Vector4 value2) => Vector128.MinNative(value1.AsVector128(), value2.AsVector128()).AsVector4();

        /// <inheritdoc cref="Vector128.MinNumber{T}(Vector128{T}, Vector128{T})" />
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector4 MinNumber(Vector4 value1, Vector4 value2) => Vector128.MinNumber(value1.AsVector128(), value2.AsVector128()).AsVector4();

        /// <summary>Returns a new vector whose values are the product of each pair of elements in two specified vectors.</summary>
        /// <param name="left">The first vector.</param>
        /// <param name="right">The second vector.</param>
        /// <returns>The element-wise product vector.</returns>
        [Intrinsic]
        public static Vector4 Multiply(Vector4 left, Vector4 right) => left * right;

        /// <summary>Multiplies a vector by a specified scalar.</summary>
        /// <param name="left">The vector to multiply.</param>
        /// <param name="right">The scalar value.</param>
        /// <returns>The scaled vector.</returns>
        [Intrinsic]
        public static Vector4 Multiply(Vector4 left, float right) => left * right;

        /// <summary>Multiplies a scalar value by a specified vector.</summary>
        /// <param name="left">The scaled value.</param>
        /// <param name="right">The vector.</param>
        /// <returns>The scaled vector.</returns>
        [Intrinsic]
        public static Vector4 Multiply(float left, Vector4 right) => left * right;

        /// <inheritdoc cref="Vector128.MultiplyAddEstimate(Vector128{float}, Vector128{float}, Vector128{float})" />
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector4 MultiplyAddEstimate(Vector4 left, Vector4 right, Vector4 addend) => Vector128.MultiplyAddEstimate(left.AsVector128(), right.AsVector128(), addend.AsVector128()).AsVector4();

        /// <summary>Negates a specified vector.</summary>
        /// <param name="value">The vector to negate.</param>
        /// <returns>The negated vector.</returns>
        [Intrinsic]
        public static Vector4 Negate(Vector4 value) => -value;

        /// <inheritdoc cref="Vector128.None{T}(Vector128{T}, T)" />
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool None(Vector4 vector, float value) => Vector128.None(vector.AsVector128(), value);

        /// <inheritdoc cref="Vector128.NoneWhereAllBitsSet{T}(Vector128{T})" />
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool NoneWhereAllBitsSet(Vector4 vector) => Vector128.NoneWhereAllBitsSet(vector.AsVector128());

        /// <summary>Returns a vector with the same direction as the specified vector, but with a length of one.</summary>
        /// <param name="vector">The vector to normalize.</param>
        /// <returns>The normalized vector.</returns>
        [Intrinsic]
        public static Vector4 Normalize(Vector4 vector) => vector / vector.Length();

        /// <inheritdoc cref="Vector128.OnesComplement{T}(Vector128{T})" />
        [Intrinsic]
        public static Vector4 OnesComplement(Vector4 value) => ~value;

        /// <inheritdoc cref="Vector128.RadiansToDegrees(Vector128{float})" />
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector4 RadiansToDegrees(Vector4 radians) => Vector128.RadiansToDegrees(radians.AsVector128()).AsVector4();

        /// <inheritdoc cref="Vector128.Round(Vector128{float})" />
        [Intrinsic]
        public static Vector4 Round(Vector4 vector) => Vector128.Round(vector.AsVector128()).AsVector4();

        /// <inheritdoc cref="Vector128.Round(Vector128{float}, MidpointRounding)" />
        [Intrinsic]
        public static Vector4 Round(Vector4 vector, MidpointRounding mode) => Vector128.Round(vector.AsVector128(), mode).AsVector4();

        /// <summary>Creates a new vector by selecting values from an input vector using a set of indices.</summary>
        /// <param name="vector">The input vector from which values are selected.</param>
        /// <param name="xIndex">The index used to select a value from <paramref name="vector" /> to be used as the value of <see cref="X" /> in the result.</param>
        /// <param name="yIndex">The index used to select a value from <paramref name="vector" /> to be used as the value of <see cref="Y" /> in the result</param>
        /// <param name="zIndex">The index used to select a value from <paramref name="vector" /> to be used as the value of <see cref="Z" /> in the result</param>
        /// <param name="wIndex">The index used to select a value from <paramref name="vector" /> to be used as the value of <see cref="W" /> in the result</param>
        /// <returns>A new vector containing the values from <paramref name="vector" /> selected by the given indices.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector4 Shuffle(Vector4 vector, byte xIndex, byte yIndex, byte zIndex, byte wIndex)
        {
            return Vector128.Shuffle(vector.AsVector128(), Vector128.Create(xIndex, yIndex, zIndex, wIndex)).AsVector4();
        }

        /// <inheritdoc cref="Vector128.Sin(Vector128{float})" />
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector4 Sin(Vector4 vector) => Vector128.Sin(vector.AsVector128()).AsVector4();

        /// <inheritdoc cref="Vector128.SinCos(Vector128{float})" />
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static (Vector4 Sin, Vector4 Cos) SinCos(Vector4 vector)
        {
            (Vector128<float> sin, Vector128<float> cos) = Vector128.SinCos(vector.AsVector128());
            return (sin.AsVector4(), cos.AsVector4());
        }

        /// <summary>Returns a vector whose elements are the square root of each of a specified vector's elements.</summary>
        /// <param name="value">A vector.</param>
        /// <returns>The square root vector.</returns>
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector4 SquareRoot(Vector4 value) => Vector128.Sqrt(value.AsVector128()).AsVector4();

        /// <summary>Subtracts the second vector from the first.</summary>
        /// <param name="left">The first vector.</param>
        /// <param name="right">The second vector.</param>
        /// <returns>The difference vector.</returns>
        [Intrinsic]
        public static Vector4 Subtract(Vector4 left, Vector4 right) => left - right;

        /// <inheritdoc cref="Vector128.Sum{T}(Vector128{T})" />
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float Sum(Vector4 value) => Vector128.Sum(value.AsVector128());

        /// <summary>Transforms a two-dimensional vector by a specified 4x4 matrix.</summary>
        /// <param name="position">The vector to transform.</param>
        /// <param name="matrix">The transformation matrix.</param>
        /// <returns>The transformed vector.</returns>
        public static Vector4 Transform(Vector2 position, Matrix4x4 matrix) => Transform(position, in matrix.AsImpl());

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static Vector4 Transform(Vector2 position, in Matrix4x4.Impl matrix)
        {
            // This implementation is based on the DirectX Math Library XMVector2Transform method
            // https://github.com/microsoft/DirectXMath/blob/master/Inc/DirectXMathVector.inl

            Vector4 result = matrix.X * position.X;
            result = MultiplyAddEstimate(matrix.Y, Create(position.Y), result);
            return result + matrix.W;
        }

        /// <summary>Transforms a two-dimensional vector by the specified Quaternion rotation value.</summary>
        /// <param name="value">The vector to rotate.</param>
        /// <param name="rotation">The rotation to apply.</param>
        /// <returns>The transformed vector.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector4 Transform(Vector2 value, Quaternion rotation) => Transform(Create(value, 0.0f, 1.0f), rotation);

        /// <summary>Transforms a three-dimensional vector by a specified 4x4 matrix.</summary>
        /// <param name="position">The vector to transform.</param>
        /// <param name="matrix">The transformation matrix.</param>
        /// <returns>The transformed vector.</returns>
        public static Vector4 Transform(Vector3 position, Matrix4x4 matrix) => Transform(position, in matrix.AsImpl());

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static Vector4 Transform(Vector3 position, in Matrix4x4.Impl matrix)
        {
            // This implementation is based on the DirectX Math Library XMVector3Transform method
            // https://github.com/microsoft/DirectXMath/blob/master/Inc/DirectXMathVector.inl

            Vector4 result = matrix.X * position.X;
            result = MultiplyAddEstimate(matrix.Y, Create(position.Y), result);
            result = MultiplyAddEstimate(matrix.Z, Create(position.Z), result);
            return result + matrix.W;
        }

        /// <summary>Transforms a three-dimensional vector by the specified Quaternion rotation value.</summary>
        /// <param name="value">The vector to rotate.</param>
        /// <param name="rotation">The rotation to apply.</param>
        /// <returns>The transformed vector.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector4 Transform(Vector3 value, Quaternion rotation) => Transform(Create(value, 1.0f), rotation);

        /// <summary>Transforms a four-dimensional vector by a specified 4x4 matrix.</summary>
        /// <param name="vector">The vector to transform.</param>
        /// <param name="matrix">The transformation matrix.</param>
        /// <returns>The transformed vector.</returns>
        public static Vector4 Transform(Vector4 vector, Matrix4x4 matrix) => Transform(vector, in matrix.AsImpl());

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static Vector4 Transform(Vector4 vector, in Matrix4x4.Impl matrix)
        {
            // This implementation is based on the DirectX Math Library XMVector4Transform method
            // https://github.com/microsoft/DirectXMath/blob/master/Inc/DirectXMathVector.inl

            Vector4 result = matrix.X * vector.X;
            result = MultiplyAddEstimate(matrix.Y, Create(vector.Y), result);
            result = MultiplyAddEstimate(matrix.Z, Create(vector.Z), result);
            result = MultiplyAddEstimate(matrix.W, Create(vector.W), result);
            return result;
        }

        /// <summary>Transforms a four-dimensional vector by the specified Quaternion rotation value.</summary>
        /// <param name="value">The vector to rotate.</param>
        /// <param name="rotation">The rotation to apply.</param>
        /// <returns>The transformed vector.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector4 Transform(Vector4 value, Quaternion rotation)
        {
            // This implementation is based on the DirectX Math Library XMVector3Rotate method
            // https://github.com/microsoft/DirectXMath/blob/master/Inc/DirectXMathVector.inl

            Quaternion conjuagate = Quaternion.Conjugate(rotation);
            Quaternion temp = Quaternion.Concatenate(conjuagate, value.AsQuaternion());
            return Quaternion.Concatenate(temp, rotation).AsVector4();
        }

        /// <inheritdoc cref="Vector128.Truncate(Vector128{float})" />
        [Intrinsic]
        public static Vector4 Truncate(Vector4 vector) => Vector128.Truncate(vector.AsVector128()).AsVector4();

        /// <inheritdoc cref="Vector128.Xor{T}(Vector128{T}, Vector128{T})" />
        [Intrinsic]
        public static Vector4 Xor(Vector4 left, Vector4 right) => left ^ right;

        /// <summary>Copies the elements of the vector to a specified array.</summary>
        /// <param name="array">The destination array.</param>
        /// <remarks><paramref name="array" /> must have at least four elements. The method copies the vector's elements starting at index 0.</remarks>
        /// <exception cref="NullReferenceException"><paramref name="array" /> is <see langword="null" />.</exception>
        /// <exception cref="ArgumentException">The number of elements in the current instance is greater than in the array.</exception>
        /// <exception cref="RankException"><paramref name="array" /> is multidimensional.</exception>
        public readonly void CopyTo(float[] array) => this.AsVector128().CopyTo(array);

        /// <summary>Copies the elements of the vector to a specified array starting at a specified index position.</summary>
        /// <param name="array">The destination array.</param>
        /// <param name="index">The index at which to copy the first element of the vector.</param>
        /// <remarks><paramref name="array" /> must have a sufficient number of elements to accommodate the four vector elements. In other words, elements <paramref name="index" /> through <paramref name="index" /> + 3 must already exist in <paramref name="array" />.</remarks>
        /// <exception cref="NullReferenceException"><paramref name="array" /> is <see langword="null" />.</exception>
        /// <exception cref="ArgumentException">The number of elements in the current instance is greater than in the array.</exception>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="index" /> is less than zero.
        /// -or-
        /// <paramref name="index" /> is greater than or equal to the array length.</exception>
        /// <exception cref="RankException"><paramref name="array" /> is multidimensional.</exception>
        public readonly void CopyTo(float[] array, int index) => this.AsVector128().CopyTo(array, index);

        /// <summary>Copies the vector to the given <see cref="Span{T}" />. The length of the destination span must be at least 4.</summary>
        /// <param name="destination">The destination span which the values are copied into.</param>
        /// <exception cref="ArgumentException">If number of elements in source vector is greater than those available in destination span.</exception>
        public readonly void CopyTo(Span<float> destination) => this.AsVector128().CopyTo(destination);

        /// <summary>Attempts to copy the vector to the given <see cref="Span{Single}" />. The length of the destination span must be at least 4.</summary>
        /// <param name="destination">The destination span which the values are copied into.</param>
        /// <returns><see langword="true" /> if the source vector was successfully copied to <paramref name="destination" />. <see langword="false" /> if <paramref name="destination" /> is not large enough to hold the source vector.</returns>
        public readonly bool TryCopyTo(Span<float> destination) => this.AsVector128().TryCopyTo(destination);

        /// <summary>Returns a value that indicates whether this instance and another vector are equal.</summary>
        /// <param name="other">The other vector.</param>
        /// <returns><see langword="true" /> if the two vectors are equal; otherwise, <see langword="false" />.</returns>
        /// <remarks>Two vectors are equal if their <see cref="X" />, <see cref="Y" />, <see cref="Z" />, and <see cref="W" /> elements are equal.</remarks>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly bool Equals(Vector4 other) => this.AsVector128().Equals(other.AsVector128());

        /// <summary>Returns a value that indicates whether this instance and a specified object are equal.</summary>
        /// <param name="obj">The object to compare with the current instance.</param>
        /// <returns><see langword="true" /> if the current instance and <paramref name="obj" /> are equal; otherwise, <see langword="false" />. If <paramref name="obj" /> is <see langword="null" />, the method returns <see langword="false" />.</returns>
        /// <remarks>The current instance and <paramref name="obj" /> are equal if <paramref name="obj" /> is a <see cref="Vector4" /> object and their corresponding elements are equal.</remarks>
        public override readonly bool Equals([NotNullWhen(true)] object? obj) => (obj is Vector4 other) && Equals(other);

        /// <summary>Returns the hash code for this instance.</summary>
        /// <returns>The hash code.</returns>
        public override readonly int GetHashCode() => HashCode.Combine(X, Y, Z, W);

        /// <summary>Returns the length of this vector object.</summary>
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
        /// <related type="Article" href="/dotnet/standard/base-types/standard-numeric-format-strings">Standard Numeric Format Strings</related>
        /// <related type="Article" href="/dotnet/standard/base-types/custom-numeric-format-strings">Custom Numeric Format Strings</related>
        public readonly string ToString([StringSyntax(StringSyntaxAttribute.NumericFormat)] string? format, IFormatProvider? formatProvider)
        {
            string separator = NumberFormatInfo.GetInstance(formatProvider).NumberGroupSeparator;

            return $"<{X.ToString(format, formatProvider)}{separator} {Y.ToString(format, formatProvider)}{separator} {Z.ToString(format, formatProvider)}{separator} {W.ToString(format, formatProvider)}>";
        }
    }
}
