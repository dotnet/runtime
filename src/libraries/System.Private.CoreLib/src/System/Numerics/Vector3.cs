// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics;

namespace System.Numerics
{
    /// <summary>Represents a vector with three  single-precision floating-point values.</summary>
    /// <remarks><format type="text/markdown"><![CDATA[
    /// The <xref:System.Numerics.Vector3> structure provides support for hardware acceleration.
    /// [!INCLUDE[vectors-are-rows-paragraph](~/includes/system-numerics-vectors-are-rows.md)]
    /// ]]></format></remarks>
    [Intrinsic]
    public partial struct Vector3 : IEquatable<Vector3>, IFormattable
    {
        /// <summary>Specifies the alignment of the vector as used by the <see cref="LoadAligned(float*)" /> and <see cref="Vector.StoreAligned(Vector3, float*)" /> APIs.</summary>
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

        /// <summary>The Z component of the vector.</summary>
        public float Z;

        internal const int ElementCount = 3;

        /// <summary>Creates a new <see cref="Vector3" /> object whose three elements have the same value.</summary>
        /// <param name="value">The value to assign to all three elements.</param>
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Vector3(float value)
        {
            this = Create(value);
        }

        /// <summary>Creates a   new <see cref="Vector3" /> object from the specified <see cref="Vector2" /> object and the specified value.</summary>
        /// <param name="value">The vector with two elements.</param>
        /// <param name="z">The additional value to assign to the <see cref="Z" /> field.</param>
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Vector3(Vector2 value, float z)
        {
            this = Create(value, z);
        }

        /// <summary>Creates a vector whose elements have the specified values.</summary>
        /// <param name="x">The value to assign to the <see cref="X" /> field.</param>
        /// <param name="y">The value to assign to the <see cref="Y" /> field.</param>
        /// <param name="z">The value to assign to the <see cref="Z" /> field.</param>
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Vector3(float x, float y, float z)
        {
            this = Create(x, y, z);
        }

        /// <summary>Constructs a vector from the given <see cref="ReadOnlySpan{Single}" />. The span must contain at least 3 elements.</summary>
        /// <param name="values">The span of elements to assign to the vector.</param>
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Vector3(ReadOnlySpan<float> values)
        {
            this = Create(values);
        }

        /// <inheritdoc cref="Vector4.AllBitsSet" />
        public static Vector3 AllBitsSet
        {
            [Intrinsic]
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => Vector128<float>.AllBitsSet.AsVector3();
        }

        /// <inheritdoc cref="Vector4.E" />
        public static Vector3 E
        {
            [Intrinsic]
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => Vector128<float>.E.AsVector3();
        }

        /// <inheritdoc cref="Vector4.Epsilon" />
        public static Vector3 Epsilon
        {
            [Intrinsic]
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => Vector128<float>.Epsilon.AsVector3();
        }

        /// <inheritdoc cref="Vector4.NaN" />
        public static Vector3 NaN
        {
            [Intrinsic]
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => Vector128<float>.NaN.AsVector3();
        }

        /// <inheritdoc cref="Vector4.NegativeInfinity" />
        public static Vector3 NegativeInfinity
        {
            [Intrinsic]
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => Vector128<float>.NegativeInfinity.AsVector3();
        }

        /// <inheritdoc cref="Vector4.NegativeZero" />
        public static Vector3 NegativeZero
        {
            [Intrinsic]
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => Vector128<float>.NegativeZero.AsVector3();
        }

        /// <inheritdoc cref="Vector4.One" />
        public static Vector3 One
        {
            [Intrinsic]
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => Vector128<float>.One.AsVector3();
        }

        /// <inheritdoc cref="Vector4.Pi" />
        public static Vector3 Pi
        {
            [Intrinsic]
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => Vector128<float>.Pi.AsVector3();
        }

        /// <inheritdoc cref="Vector4.PositiveInfinity" />
        public static Vector3 PositiveInfinity
        {
            [Intrinsic]
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => Vector128<float>.PositiveInfinity.AsVector3();
        }

        /// <inheritdoc cref="Vector4.Tau" />
        public static Vector3 Tau
        {
            [Intrinsic]
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => Vector128<float>.Tau.AsVector3();
        }

        /// <summary>Gets the vector (1,0,0).</summary>
        /// <value>The vector <c>(1,0,0)</c>.</value>
        public static Vector3 UnitX
        {
            [Intrinsic]
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => Vector128.CreateScalar(1.0f).AsVector3();
        }

        /// <summary>Gets the vector (0,1,0).</summary>
        /// <value>The vector <c>(0,1,0)</c>.</value>
        public static Vector3 UnitY
        {
            [Intrinsic]
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => Vector128.Create(0.0f, 1.0f, 0.0f, 0.0f).AsVector3();
        }

        /// <summary>Gets the vector (0,0,1).</summary>
        /// <value>The vector <c>(0,0,1)</c>.</value>
        public static Vector3 UnitZ
        {
            [Intrinsic]
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => Vector128.Create(0.0f, 0.0f, 1.0f, 0.0f).AsVector3();
        }

        /// <inheritdoc cref="Vector4.Zero" />
        public static Vector3 Zero
        {
            [Intrinsic]
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => default;
        }

        /// <summary>Gets or sets the element at the specified index.</summary>
        /// <param name="index">The index of the element to get or set.</param>
        /// <returns>The the element at <paramref name="index" />.</returns>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="index" /> was less than zero or greater than the number of elements.</exception>
        public float this[int index]
        {
            [Intrinsic]
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            readonly get => this.GetElement(index);

            [Intrinsic]
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            set
            {
                this = this.WithElement(index, value);
            }
        }

        /// <summary>Adds two vectors together.</summary>
        /// <param name="left">The first vector to add.</param>
        /// <param name="right">The second vector to add.</param>
        /// <returns>The summed vector.</returns>
        /// <remarks>The <see cref="op_Addition" /> method defines the addition operation for <see cref="Vector3" /> objects.</remarks>
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector3 operator +(Vector3 left, Vector3 right) => (left.AsVector128Unsafe() + right.AsVector128Unsafe()).AsVector3();

        /// <summary>Divides the first vector by the second.</summary>
        /// <param name="left">The first vector.</param>
        /// <param name="right">The second vector.</param>
        /// <returns>The vector that results from dividing <paramref name="left" /> by <paramref name="right" />.</returns>
        /// <remarks>The <see cref="Vector3.op_Division" /> method defines the division operation for <see cref="Vector3" /> objects.</remarks>
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector3 operator /(Vector3 left, Vector3 right) => (left.AsVector128Unsafe() / right.AsVector128Unsafe()).AsVector3();

        /// <summary>Divides the specified vector by a specified scalar value.</summary>
        /// <param name="value1">The vector.</param>
        /// <param name="value2">The scalar value.</param>
        /// <returns>The result of the division.</returns>
        /// <remarks>The <see cref="Vector3.op_Division" /> method defines the division operation for <see cref="Vector3" /> objects.</remarks>
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector3 operator /(Vector3 value1, float value2) => (value1.AsVector128Unsafe() / value2).AsVector3();

        /// <summary>Returns a value that indicates whether each pair of elements in two specified vectors is equal.</summary>
        /// <param name="left">The first vector to compare.</param>
        /// <param name="right">The second vector to compare.</param>
        /// <returns><see langword="true" /> if <paramref name="left" /> and <paramref name="right" /> are equal; otherwise, <see langword="false" />.</returns>
        /// <remarks>Two <see cref="Vector3" /> objects are equal if each element in <paramref name="left" /> is equal to the corresponding element in <paramref name="right" />.</remarks>
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator ==(Vector3 left, Vector3 right) => left.AsVector128() == right.AsVector128();

        /// <summary>Returns a value that indicates whether two specified vectors are not equal.</summary>
        /// <param name="left">The first vector to compare.</param>
        /// <param name="right">The second vector to compare.</param>
        /// <returns><see langword="true" /> if <paramref name="left" /> and <paramref name="right" /> are not equal; otherwise, <see langword="false" />.</returns>
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator !=(Vector3 left, Vector3 right) => left.AsVector128() != right.AsVector128();

        /// <summary>Returns a new vector whose values are the product of each pair of elements in two specified vectors.</summary>
        /// <param name="left">The first vector.</param>
        /// <param name="right">The second vector.</param>
        /// <returns>The element-wise product vector.</returns>
        /// <remarks>The <see cref="Vector3.op_Multiply" /> method defines the multiplication operation for <see cref="Vector3" /> objects.</remarks>
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector3 operator *(Vector3 left, Vector3 right) => (left.AsVector128Unsafe() * right.AsVector128Unsafe()).AsVector3();

        /// <summary>Multiplies the specified vector by the specified scalar value.</summary>
        /// <param name="left">The vector.</param>
        /// <param name="right">The scalar value.</param>
        /// <returns>The scaled vector.</returns>
        /// <remarks>The <see cref="Vector3.op_Multiply" /> method defines the multiplication operation for <see cref="Vector3" /> objects.</remarks>
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector3 operator *(Vector3 left, float right) => (left.AsVector128Unsafe() * right).AsVector3();

        /// <summary>Multiplies the scalar value by the specified vector.</summary>
        /// <param name="left">The vector.</param>
        /// <param name="right">The scalar value.</param>
        /// <returns>The scaled vector.</returns>
        /// <remarks>The <see cref="Vector3.op_Multiply" /> method defines the multiplication operation for <see cref="Vector3" /> objects.</remarks>
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector3 operator *(float left, Vector3 right) => (right.AsVector128Unsafe() * left).AsVector3();

        /// <summary>Subtracts the second vector from the first.</summary>
        /// <param name="left">The first vector.</param>
        /// <param name="right">The second vector.</param>
        /// <returns>The vector that results from subtracting <paramref name="right" /> from <paramref name="left" />.</returns>
        /// <remarks>The <see cref="op_Subtraction" /> method defines the subtraction operation for <see cref="Vector3" /> objects.</remarks>
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector3 operator -(Vector3 left, Vector3 right) => (left.AsVector128Unsafe() - right.AsVector128Unsafe()).AsVector3();

        /// <summary>Negates the specified vector.</summary>
        /// <param name="value">The vector to negate.</param>
        /// <returns>The negated vector.</returns>
        /// <remarks>The <see cref="op_UnaryNegation" /> method defines the unary negation operation for <see cref="Vector3" /> objects.</remarks>
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector3 operator -(Vector3 value) => (-value.AsVector128Unsafe()).AsVector3();

        /// <inheritdoc cref="Vector4.op_BitwiseAnd(Vector4, Vector4)" />
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector3 operator &(Vector3 left, Vector3 right) => (left.AsVector128Unsafe() & right.AsVector128Unsafe()).AsVector3();

        /// <inheritdoc cref="Vector4.op_BitwiseOr(Vector4, Vector4)" />
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector3 operator |(Vector3 left, Vector3 right) => (left.AsVector128Unsafe() | right.AsVector128Unsafe()).AsVector3();

        /// <inheritdoc cref="Vector4.op_ExclusiveOr(Vector4, Vector4)" />
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector3 operator ^(Vector3 left, Vector3 right) => (left.AsVector128Unsafe() ^ right.AsVector128Unsafe()).AsVector3();

        /// <inheritdoc cref="Vector4.op_LeftShift(Vector4, int)" />
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector3 operator <<(Vector3 value, int shiftAmount) => (value.AsVector128Unsafe() << shiftAmount).AsVector3();

        /// <inheritdoc cref="Vector4.op_OnesComplement(Vector4)" />
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector3 operator ~(Vector3 value) => (~value.AsVector128Unsafe()).AsVector3();

        /// <inheritdoc cref="Vector4.op_RightShift(Vector4, int)" />
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector3 operator >>(Vector3 value, int shiftAmount) => (value.AsVector128Unsafe() >> shiftAmount).AsVector3();

        /// <inheritdoc cref="Vector4.op_UnaryPlus(Vector4)" />
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector3 operator +(Vector3 value) => value;

        /// <inheritdoc cref="Vector4.op_UnsignedRightShift(Vector4, int)" />
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector3 operator >>>(Vector3 value, int shiftAmount) => (value.AsVector128Unsafe() >>> shiftAmount).AsVector3();

        /// <summary>Returns a vector whose elements are the absolute values of each of the specified vector's elements.</summary>
        /// <param name="value">A vector.</param>
        /// <returns>The absolute value vector.</returns>
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector3 Abs(Vector3 value) => Vector128.Abs(value.AsVector128Unsafe()).AsVector3();

        /// <summary>Adds two vectors together.</summary>
        /// <param name="left">The first vector to add.</param>
        /// <param name="right">The second vector to add.</param>
        /// <returns>The summed vector.</returns>
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector3 Add(Vector3 left, Vector3 right) => (left.AsVector128Unsafe() + right.AsVector128Unsafe()).AsVector3();

        /// <inheritdoc cref="Vector4.All(Vector4, float)" />
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool All(Vector3 vector, float value) => Vector128.All(vector, value);

        /// <inheritdoc cref="Vector4.AllWhereAllBitsSet(Vector4)" />
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool AllWhereAllBitsSet(Vector3 vector) => Vector128.AllWhereAllBitsSet(vector);

        /// <inheritdoc cref="Vector4.AndNot(Vector4, Vector4)" />
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector3 AndNot(Vector3 left, Vector3 right) => Vector128.AndNot(left.AsVector128Unsafe(), right.AsVector128Unsafe()).AsVector3();

        /// <inheritdoc cref="Vector4.Any(Vector4, float)" />
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool Any(Vector3 vector, float value) => Vector128.Any(vector, value);

        /// <inheritdoc cref="Vector4.AnyWhereAllBitsSet(Vector4)" />
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool AnyWhereAllBitsSet(Vector3 vector) => Vector128.AnyWhereAllBitsSet(vector);

        /// <inheritdoc cref="Vector4.BitwiseAnd(Vector4, Vector4)" />
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector3 BitwiseAnd(Vector3 left, Vector3 right) => (left.AsVector128Unsafe() & right.AsVector128Unsafe()).AsVector3();

        /// <inheritdoc cref="Vector4.BitwiseOr(Vector4, Vector4)" />
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector3 BitwiseOr(Vector3 left, Vector3 right) => (left.AsVector128Unsafe() | right.AsVector128Unsafe()).AsVector3();

        /// <inheritdoc cref="Vector4.Clamp(Vector4, Vector4, Vector4)" />
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector3 Clamp(Vector3 value1, Vector3 min, Vector3 max) => Vector128.Clamp(value1.AsVector128Unsafe(), min.AsVector128Unsafe(), max.AsVector128Unsafe()).AsVector3();

        /// <inheritdoc cref="Vector4.ClampNative(Vector4, Vector4, Vector4)" />
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector3 ClampNative(Vector3 value1, Vector3 min, Vector3 max) => Vector128.ClampNative(value1.AsVector128Unsafe(), min.AsVector128Unsafe(), max.AsVector128Unsafe()).AsVector3();

        /// <inheritdoc cref="Vector4.ConditionalSelect(Vector4, Vector4, Vector4)" />
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector3 ConditionalSelect(Vector3 condition, Vector3 left, Vector3 right) => Vector128.ConditionalSelect(condition.AsVector128Unsafe(), left.AsVector128Unsafe(), right.AsVector128Unsafe()).AsVector3();

        /// <inheritdoc cref="Vector4.CopySign(Vector4, Vector4)" />
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector3 CopySign(Vector3 value, Vector3 sign) => Vector128.CopySign(value.AsVector128Unsafe(), sign.AsVector128Unsafe()).AsVector3();

        /// <inheritdoc cref="Vector4.Cos(Vector4)" />
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector3 Cos(Vector3 vector) => Vector128.Cos(vector.AsVector128()).AsVector3();

        /// <inheritdoc cref="Vector4.Count(Vector4, float)" />
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int Count(Vector3 vector, float value) => Vector128.Count(vector, value);

        /// <inheritdoc cref="Vector4.CountWhereAllBitsSet(Vector4)" />
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int CountWhereAllBitsSet(Vector3 vector) => Vector128.CountWhereAllBitsSet(vector);

        /// <summary>Creates a new <see cref="Vector3" /> object whose three elements have the same value.</summary>
        /// <param name="value">The value to assign to all three elements.</param>
        /// <returns>A new <see cref="Vector3" /> whose three elements have the same value.</returns>
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector3 Create(float value) => Vector128.Create(value).AsVector3();

        /// <summary>Creates a new <see cref="Vector3" /> object from the specified <see cref="Vector2" /> object and a Z and a W component.</summary>
        /// <param name="vector">The vector to use for the X and Y components.</param>
        /// <param name="z">The Z component.</param>
        /// <returns>A new <see cref="Vector3" /> from the specified <see cref="Vector2" /> object and a Z and a W component.</returns>
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector3 Create(Vector2 vector, float z)
        {
            return vector.AsVector128Unsafe()
                         .WithElement(2, z)
                         .AsVector3();
        }

        /// <summary>Creates a vector whose elements have the specified values.</summary>
        /// <param name="x">The value to assign to the <see cref="X" /> field.</param>
        /// <param name="y">The value to assign to the <see cref="Y" /> field.</param>
        /// <param name="z">The value to assign to the <see cref="Z" /> field.</param>
        /// <returns>A new <see cref="Vector3" /> whose elements have the specified values.</returns>
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector3 Create(float x, float y, float z) => Vector128.Create(x, y, z, 0).AsVector3();

        /// <summary>Constructs a vector from the given <see cref="ReadOnlySpan{Single}" />. The span must contain at least 3 elements.</summary>
        /// <param name="values">The span of elements to assign to the vector.</param>
        /// <returns>A new <see cref="Vector3" /> whose elements have the specified values.</returns>
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector3 Create(ReadOnlySpan<float> values)
        {
            if (values.Length < ElementCount)
            {
                ThrowHelper.ThrowArgumentOutOfRangeException(ExceptionArgument.values);
            }
            return Unsafe.As<float, Vector3>(ref MemoryMarshal.GetReference(values));
        }

        /// <summary>Creates a vector with <see cref="X" /> initialized to the specified value and the remaining elements initialized to zero.</summary>
        /// <param name="x">The value to assign to the <see cref="X" /> field.</param>
        /// <returns>A new <see cref="Vector3" /> with <see cref="X" /> initialized <paramref name="x" /> and the remaining elements initialized to zero.</returns>
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector3 CreateScalar(float x) => Vector128.CreateScalar(x).AsVector3();

        /// <summary>Creates a vector with <see cref="X" /> initialized to the specified value and the remaining elements left uninitialized.</summary>
        /// <param name="x">The value to assign to the <see cref="X" /> field.</param>
        /// <returns>A new <see cref="Vector3" /> with <see cref="X" /> initialized <paramref name="x" /> and the remaining elements left uninitialized.</returns>
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector3 CreateScalarUnsafe(float x) => Vector128.CreateScalarUnsafe(x).AsVector3();

        /// <summary>Computes the cross product of two vectors.</summary>
        /// <param name="vector1">The first vector.</param>
        /// <param name="vector2">The second vector.</param>
        /// <returns>The cross product.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector3 Cross(Vector3 vector1, Vector3 vector2) => Cross(vector1.AsVector128Unsafe(), vector2.AsVector128Unsafe()).AsVector3();

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static Vector128<float> Cross(Vector128<float> vector1, Vector128<float> vector2)
        {
            // This implementation is based on the DirectX Math Library XMVector3Cross method
            // https://github.com/microsoft/DirectXMath/blob/master/Inc/DirectXMathVector.inl

            Vector128<float> temp1 = Vector128.Shuffle(vector1, Vector128.Create(1, 2, 0, 3)) * Vector128.Shuffle(vector2, Vector128.Create(2, 0, 1, 3));
            Vector128<float> temp2 = Vector128.Shuffle(vector1, Vector128.Create(2, 0, 1, 3)) * Vector128.Shuffle(vector2, Vector128.Create(1, 2, 0, 3));

            return temp1 - temp2;
        }

        /// <inheritdoc cref="Vector4.DegreesToRadians(Vector4)" />
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector3 DegreesToRadians(Vector3 degrees) => Vector128.DegreesToRadians(degrees.AsVector128Unsafe()).AsVector3();

        /// <summary>Computes the Euclidean distance between the two given points.</summary>
        /// <param name="value1">The first point.</param>
        /// <param name="value2">The second point.</param>
        /// <returns>The distance.</returns>
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float Distance(Vector3 value1, Vector3 value2) => Vector128.Distance(value1.AsVector128(), value2.AsVector128());

        /// <summary>Returns the Euclidean distance squared between two specified points.</summary>
        /// <param name="value1">The first point.</param>
        /// <param name="value2">The second point.</param>
        /// <returns>The distance squared.</returns>
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float DistanceSquared(Vector3 value1, Vector3 value2) => Vector128.DistanceSquared(value1.AsVector128(), value2.AsVector128());

        /// <summary>Divides the first vector by the second.</summary>
        /// <param name="left">The first vector.</param>
        /// <param name="right">The second vector.</param>
        /// <returns>The vector resulting from the division.</returns>
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector3 Divide(Vector3 left, Vector3 right) => (left.AsVector128Unsafe() / right.AsVector128Unsafe()).AsVector3();

        /// <summary>Divides the specified vector by a specified scalar value.</summary>
        /// <param name="left">The vector.</param>
        /// <param name="divisor">The scalar value.</param>
        /// <returns>The vector that results from the division.</returns>
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector3 Divide(Vector3 left, float divisor) => (left.AsVector128Unsafe() / divisor).AsVector3();

        /// <summary>Returns the dot product of two vectors.</summary>
        /// <param name="vector1">The first vector.</param>
        /// <param name="vector2">The second vector.</param>
        /// <returns>The dot product.</returns>
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float Dot(Vector3 vector1, Vector3 vector2) => Vector128.Dot(vector1.AsVector128(), vector2.AsVector128());

        /// <inheritdoc cref="Vector4.Exp(Vector4)" />
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector3 Exp(Vector3 vector) => Vector128.Exp(vector.AsVector128()).AsVector3();

        /// <inheritdoc cref="Vector4.Equals(Vector4, Vector4)" />
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector3 Equals(Vector3 left, Vector3 right) => Vector128.Equals(left.AsVector128Unsafe(), right.AsVector128Unsafe()).AsVector3();

        /// <inheritdoc cref="Vector4.EqualsAll(Vector4, Vector4)" />
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool EqualsAll(Vector3 left, Vector3 right) => Vector128.EqualsAll(left.AsVector128(), right.AsVector128());

        /// <inheritdoc cref="Vector4.EqualsAny(Vector4, Vector4)" />
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool EqualsAny(Vector3 left, Vector3 right) => Vector128.EqualsAny(Vector128.Equals(left.AsVector128(), right.AsVector128()).AsInt32(), Vector128.Create(-1, -1, -1, 0));

        /// <inheritdoc cref="Vector128.MultiplyAddEstimate(Vector128{float}, Vector128{float}, Vector128{float})" />
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector3 FusedMultiplyAdd(Vector3 left, Vector3 right, Vector3 addend) => Vector128.FusedMultiplyAdd(left.AsVector128Unsafe(), right.AsVector128Unsafe(), addend.AsVector128Unsafe()).AsVector3();

        /// <inheritdoc cref="Vector4.GreaterThan(Vector4, Vector4)" />
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector3 GreaterThan(Vector3 left, Vector3 right) => Vector128.GreaterThan(left.AsVector128Unsafe(), right.AsVector128Unsafe()).AsVector3();

        /// <inheritdoc cref="Vector4.GreaterThanAll(Vector4, Vector4)" />
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool GreaterThanAll(Vector3 left, Vector3 right) => Vector128.EqualsAll(Vector128.GreaterThan(left.AsVector128(), right.AsVector128()).AsInt32(), Vector128.Create(-1, -1, -1, 0));

        /// <inheritdoc cref="Vector4.GreaterThanAny(Vector4, Vector4)" />
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool GreaterThanAny(Vector3 left, Vector3 right) => Vector128.EqualsAny(Vector128.GreaterThan(left.AsVector128(), right.AsVector128()).AsInt32(), Vector128.Create(-1, -1, -1, 0));

        /// <inheritdoc cref="Vector4.GreaterThanOrEqual(Vector4, Vector4)" />
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector3 GreaterThanOrEqual(Vector3 left, Vector3 right) => Vector128.GreaterThanOrEqual(left.AsVector128Unsafe(), right.AsVector128Unsafe()).AsVector3();

        /// <inheritdoc cref="Vector4.GreaterThanOrEqualAll(Vector4, Vector4)" />
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool GreaterThanOrEqualAll(Vector3 left, Vector3 right) => Vector128.EqualsAll(Vector128.GreaterThanOrEqual(left.AsVector128(), right.AsVector128()).AsInt32(), Vector128<int>.AllBitsSet);

        /// <inheritdoc cref="Vector4.GreaterThanOrEqualAny(Vector4, Vector4)" />
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool GreaterThanOrEqualAny(Vector3 left, Vector3 right) => Vector128.EqualsAny(Vector128.GreaterThanOrEqual(left.AsVector128(), right.AsVector128()).AsInt32(), Vector128.Create(-1, -1, -1, 0));

        /// <inheritdoc cref="Vector4.Hypot(Vector4, Vector4)" />
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector3 Hypot(Vector3 x, Vector3 y) => Vector128.Hypot(x.AsVector128Unsafe(), y.AsVector128Unsafe()).AsVector3();

        /// <inheritdoc cref="Vector4.IndexOf(Vector4, float)" />
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int IndexOf(Vector3 vector, float value) => Vector128.IndexOf(vector, value);

        /// <inheritdoc cref="Vector4.IndexOfWhereAllBitsSet(Vector4)" />
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int IndexOfWhereAllBitsSet(Vector3 vector) => Vector128.IndexOfWhereAllBitsSet(vector);

        /// <inheritdoc cref="Vector4.IsEvenInteger(Vector4)" />
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector3 IsEvenInteger(Vector3 vector) => Vector128.IsEvenInteger(vector.AsVector128Unsafe()).AsVector3();

        /// <inheritdoc cref="Vector4.IsFinite(Vector4)" />
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector3 IsFinite(Vector3 vector) => Vector128.IsFinite(vector.AsVector128Unsafe()).AsVector3();

        /// <inheritdoc cref="Vector4.IsInfinity(Vector4)" />
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector3 IsInfinity(Vector3 vector) => Vector128.IsInfinity(vector.AsVector128Unsafe()).AsVector3();

        /// <inheritdoc cref="Vector4.IsInteger(Vector4)" />
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector3 IsInteger(Vector3 vector) => Vector128.IsInteger(vector.AsVector128Unsafe()).AsVector3();

        /// <inheritdoc cref="Vector4.IsNaN(Vector4)" />
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector3 IsNaN(Vector3 vector) => Vector128.IsNaN(vector.AsVector128Unsafe()).AsVector3();

        /// <inheritdoc cref="Vector4.IsNegative(Vector4)" />
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector3 IsNegative(Vector3 vector) => Vector128.IsNegative(vector.AsVector128Unsafe()).AsVector3();

        /// <inheritdoc cref="Vector4.IsNegativeInfinity(Vector4)" />
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector3 IsNegativeInfinity(Vector3 vector) => Vector128.IsNegativeInfinity(vector.AsVector128Unsafe()).AsVector3();

        /// <inheritdoc cref="Vector4.IsNormal(Vector4)" />
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector3 IsNormal(Vector3 vector) => Vector128.IsNormal(vector.AsVector128Unsafe()).AsVector3();

        /// <inheritdoc cref="Vector4.IsOddInteger(Vector4)" />
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector3 IsOddInteger(Vector3 vector) => Vector128.IsOddInteger(vector.AsVector128Unsafe()).AsVector3();

        /// <inheritdoc cref="Vector4.IsPositive(Vector4)" />
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector3 IsPositive(Vector3 vector) => Vector128.IsPositive(vector.AsVector128Unsafe()).AsVector3();

        /// <inheritdoc cref="Vector4.IsPositiveInfinity(Vector4)" />
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector3 IsPositiveInfinity(Vector3 vector) => Vector128.IsPositiveInfinity(vector.AsVector128Unsafe()).AsVector3();

        /// <inheritdoc cref="Vector4.IsSubnormal(Vector4)" />
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector3 IsSubnormal(Vector3 vector) => Vector128.IsSubnormal(vector.AsVector128Unsafe()).AsVector3();

        /// <inheritdoc cref="Vector4.IsZero(Vector4)" />
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector3 IsZero(Vector3 vector) => Vector128.IsZero(vector.AsVector128Unsafe()).AsVector3();

        /// <inheritdoc cref="Vector4.LastIndexOf(Vector4, float)" />
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int LastIndexOf(Vector3 vector, float value) => Vector128.LastIndexOf(vector, value);

        /// <inheritdoc cref="Vector4.LastIndexOfWhereAllBitsSet(Vector4)" />
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int LastIndexOfWhereAllBitsSet(Vector3 vector) => Vector128.LastIndexOfWhereAllBitsSet(vector);


        /// <inheritdoc cref="Vector4.Lerp(Vector4, Vector4, float)" />
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector3 Lerp(Vector3 value1, Vector3 value2, float amount) => Vector128.Lerp(value1.AsVector128Unsafe(), value2.AsVector128Unsafe(), Vector128.Create(amount)).AsVector3();

        /// <inheritdoc cref="Vector4.Lerp(Vector4, Vector4, Vector4)" />
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector3 Lerp(Vector3 value1, Vector3 value2, Vector3 amount) => Vector128.Lerp(value1.AsVector128Unsafe(), value2.AsVector128Unsafe(), amount.AsVector128Unsafe()).AsVector3();

        /// <inheritdoc cref="Vector4.LessThan(Vector4, Vector4)" />
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector3 LessThan(Vector3 left, Vector3 right) => Vector128.LessThan(left.AsVector128Unsafe(), right.AsVector128Unsafe()).AsVector3();

        /// <inheritdoc cref="Vector4.LessThanAll(Vector4, Vector4)" />
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool LessThanAll(Vector3 left, Vector3 right) => Vector128.EqualsAll(Vector128.LessThan(left.AsVector128(), right.AsVector128()).AsInt32(), Vector128.Create(-1, -1, -1, 0));

        /// <inheritdoc cref="Vector4.LessThanAny(Vector4, Vector4)" />
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool LessThanAny(Vector3 left, Vector3 right) => Vector128.EqualsAny(Vector128.LessThan(left.AsVector128(), right.AsVector128()).AsInt32(), Vector128.Create(-1, -1, -1, 0));

        /// <inheritdoc cref="Vector4.LessThanOrEqual(Vector4, Vector4)" />
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector3 LessThanOrEqual(Vector3 left, Vector3 right) => Vector128.LessThanOrEqual(left.AsVector128Unsafe(), right.AsVector128Unsafe()).AsVector3();

        /// <inheritdoc cref="Vector4.LessThanOrEqualAll(Vector4, Vector4)" />
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool LessThanOrEqualAll(Vector3 left, Vector3 right) => Vector128.EqualsAll(Vector128.LessThanOrEqual(left.AsVector128(), right.AsVector128()).AsInt32(), Vector128<int>.AllBitsSet);

        /// <inheritdoc cref="Vector4.LessThanOrEqualAny(Vector4, Vector4)" />
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool LessThanOrEqualAny(Vector3 left, Vector3 right) => Vector128.EqualsAny(Vector128.LessThanOrEqual(left.AsVector128(), right.AsVector128()).AsInt32(), Vector128.Create(-1, -1, -1, 0));

        /// <inheritdoc cref="Vector4.Load(float*)" />
        [Intrinsic]
        [CLSCompliant(false)]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe Vector3 Load(float* source) => *(Vector3*)source;

        /// <inheritdoc cref="Vector4.LoadAligned(float*)" />
        [Intrinsic]
        [CLSCompliant(false)]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe Vector3 LoadAligned(float* source)
        {
            if (((nuint)(source) % Alignment) != 0)
            {
                ThrowHelper.ThrowAccessViolationException();
            }
            return *(Vector3*)source;
        }

        /// <inheritdoc cref="Vector4.LoadAlignedNonTemporal(float*)" />
        [Intrinsic]
        [CLSCompliant(false)]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe Vector3 LoadAlignedNonTemporal(float* source) => LoadAligned(source);

        /// <inheritdoc cref="Vector128.LoadUnsafe{T}(ref readonly T)" />
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector3 LoadUnsafe(ref readonly float source) => Unsafe.As<float, Vector3>(ref Unsafe.AsRef(in source));

        /// <inheritdoc cref="Vector4.LoadUnsafe(ref readonly float, nuint)" />
        [Intrinsic]
        [CLSCompliant(false)]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector3 LoadUnsafe(ref readonly float source, nuint elementOffset) => Unsafe.As<float, Vector3>(ref Unsafe.Add(ref Unsafe.AsRef(in source), (nint)elementOffset));

        /// <inheritdoc cref="Vector4.Log(Vector4)" />
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector3 Log(Vector3 vector) => Vector128.Log(Vector4.Create(vector, 1.0f).AsVector128()).AsVector3();

        /// <inheritdoc cref="Vector4.Log2(Vector4)" />
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector3 Log2(Vector3 vector) => Vector128.Log2(Vector4.Create(vector, 1.0f).AsVector128()).AsVector3();

        /// <inheritdoc cref="Vector4.Max(Vector4, Vector4)" />
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector3 Max(Vector3 value1, Vector3 value2) => Vector128.Max(value1.AsVector128Unsafe(), value2.AsVector128Unsafe()).AsVector3();

        /// <inheritdoc cref="Vector4.MaxMagnitude(Vector4, Vector4)" />
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector3 MaxMagnitude(Vector3 value1, Vector3 value2) => Vector128.MaxMagnitude(value1.AsVector128Unsafe(), value2.AsVector128Unsafe()).AsVector3();

        /// <inheritdoc cref="Vector4.MaxMagnitudeNumber(Vector4, Vector4)" />
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector3 MaxMagnitudeNumber(Vector3 value1, Vector3 value2) => Vector128.MaxMagnitudeNumber(value1.AsVector128Unsafe(), value2.AsVector128Unsafe()).AsVector3();

        /// <inheritdoc cref="Vector4.MaxNative(Vector4, Vector4)" />
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector3 MaxNative(Vector3 value1, Vector3 value2) => Vector128.MaxNative(value1.AsVector128Unsafe(), value2.AsVector128Unsafe()).AsVector3();

        /// <inheritdoc cref="Vector4.MaxNumber(Vector4, Vector4)" />
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector3 MaxNumber(Vector3 value1, Vector3 value2) => Vector128.MaxNumber(value1.AsVector128Unsafe(), value2.AsVector128Unsafe()).AsVector3();

        /// <inheritdoc cref="Vector4.Min(Vector4, Vector4)" />
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector3 Min(Vector3 value1, Vector3 value2) => Vector128.Min(value1.AsVector128Unsafe(), value2.AsVector128Unsafe()).AsVector3();

        /// <inheritdoc cref="Vector4.MinMagnitude(Vector4, Vector4)" />
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector3 MinMagnitude(Vector3 value1, Vector3 value2) => Vector128.MinMagnitude(value1.AsVector128Unsafe(), value2.AsVector128Unsafe()).AsVector3();

        /// <inheritdoc cref="Vector4.MinMagnitudeNumber(Vector4, Vector4)" />
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector3 MinMagnitudeNumber(Vector3 value1, Vector3 value2) => Vector128.MinMagnitudeNumber(value1.AsVector128Unsafe(), value2.AsVector128Unsafe()).AsVector3();

        /// <inheritdoc cref="Vector4.MinNative(Vector4, Vector4)" />
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector3 MinNative(Vector3 value1, Vector3 value2) => Vector128.MinNative(value1.AsVector128Unsafe(), value2.AsVector128Unsafe()).AsVector3();

        /// <inheritdoc cref="Vector4.MinNumber(Vector4, Vector4)" />
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector3 MinNumber(Vector3 value1, Vector3 value2) => Vector128.MinNumber(value1.AsVector128Unsafe(), value2.AsVector128Unsafe()).AsVector3();

        /// <summary>Returns a new vector whose values are the product of each pair of elements in two specified vectors.</summary>
        /// <param name="left">The first vector.</param>
        /// <param name="right">The second vector.</param>
        /// <returns>The element-wise product vector.</returns>
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector3 Multiply(Vector3 left, Vector3 right) => (left.AsVector128Unsafe() * right.AsVector128Unsafe()).AsVector3();

        /// <summary>Multiplies a vector by a specified scalar.</summary>
        /// <param name="left">The vector to multiply.</param>
        /// <param name="right">The scalar value.</param>
        /// <returns>The scaled vector.</returns>
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector3 Multiply(Vector3 left, float right) => (left.AsVector128Unsafe() * right).AsVector3();

        /// <summary>Multiplies a scalar value by a specified vector.</summary>
        /// <param name="left">The scaled value.</param>
        /// <param name="right">The vector.</param>
        /// <returns>The scaled vector.</returns>
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector3 Multiply(float left, Vector3 right) => (right.AsVector128Unsafe() * left).AsVector3();

        /// <inheritdoc cref="Vector128.MultiplyAddEstimate(Vector128{float}, Vector128{float}, Vector128{float})" />
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector3 MultiplyAddEstimate(Vector3 left, Vector3 right, Vector3 addend) => Vector128.MultiplyAddEstimate(left.AsVector128Unsafe(), right.AsVector128Unsafe(), addend.AsVector128Unsafe()).AsVector3();

        /// <summary>Negates a specified vector.</summary>
        /// <param name="value">The vector to negate.</param>
        /// <returns>The negated vector.</returns>
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector3 Negate(Vector3 value) => (-value.AsVector128Unsafe()).AsVector3();

        /// <inheritdoc cref="Vector4.None(Vector4, float)" />
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool None(Vector3 vector, float value) => Vector128.None(vector, value);

        /// <inheritdoc cref="Vector4.NoneWhereAllBitsSet(Vector4)" />
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool NoneWhereAllBitsSet(Vector3 vector) => Vector128.NoneWhereAllBitsSet(vector);

        /// <summary>Returns a vector with the same direction as the specified vector, but with a length of one.</summary>
        /// <param name="value">The vector to normalize.</param>
        /// <returns>The normalized vector.</returns>
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector3 Normalize(Vector3 value) => Vector128.Normalize(value.AsVector128()).AsVector3();

        /// <inheritdoc cref="Vector4.OnesComplement(Vector4)" />
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector3 OnesComplement(Vector3 value) => (~value.AsVector128Unsafe()).AsVector3();

        /// <inheritdoc cref="Vector4.RadiansToDegrees(Vector4)" />
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector3 RadiansToDegrees(Vector3 radians) => Vector128.RadiansToDegrees(radians.AsVector128Unsafe()).AsVector3();

        /// <summary>Returns the reflection of a vector off a surface that has the specified normal.</summary>
        /// <param name="vector">The source vector.</param>
        /// <param name="normal">The normal of the surface being reflected off.</param>
        /// <returns>The reflected vector.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector3 Reflect(Vector3 vector, Vector3 normal)
        {
            // This implementation is based on the DirectX Math Library XMVector3Reflect method
            // https://github.com/microsoft/DirectXMath/blob/master/Inc/DirectXMathVector.inl

            Vector128<float> vVector = vector.AsVector128();
            Vector128<float> vNormal = normal.AsVector128();

            Vector128<float> tmp = Vector128.Create(Vector128.Dot(vVector, vNormal));
            return Vector128.MultiplyAddEstimate(-(tmp + tmp), vNormal, vVector).AsVector3();
        }

        /// <inheritdoc cref="Vector4.Round(Vector4)" />
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector3 Round(Vector3 vector) => Vector128.Round(vector.AsVector128Unsafe()).AsVector3();

        /// <inheritdoc cref="Vector4.Round(Vector4, MidpointRounding)" />
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector3 Round(Vector3 vector, MidpointRounding mode) => Vector128.Round(vector.AsVector128Unsafe(), mode).AsVector3();

        /// <summary>Creates a new vector by selecting values from an input vector using a set of indices.</summary>
        /// <param name="vector">The input vector from which values are selected.</param>
        /// <param name="xIndex">The index used to select a value from <paramref name="vector" /> to be used as the value of <see cref="X" /> in the result.</param>
        /// <param name="yIndex">The index used to select a value from <paramref name="vector" /> to be used as the value of <see cref="Y" /> in the result</param>
        /// <param name="zIndex">The index used to select a value from <paramref name="vector" /> to be used as the value of <see cref="Z" /> in the result</param>
        /// <returns>A new vector containing the values from <paramref name="vector" /> selected by the given indices.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector3 Shuffle(Vector3 vector, byte xIndex, byte yIndex, byte zIndex)
        {
            // We do `AsVector128` instead of `AsVector128Unsafe` so that indices which
            // are out of range for Vector3 but in range for Vector128 still produce 0
            return Vector128.Shuffle(vector.AsVector128(), Vector128.Create(xIndex, yIndex, zIndex, 3)).AsVector3();
        }

        /// <inheritdoc cref="Vector4.Sin(Vector4)" />
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector3 Sin(Vector3 vector) => Vector128.Sin(vector.AsVector128()).AsVector3();

        /// <inheritdoc cref="Vector4.SinCos(Vector4)" />
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static (Vector3 Sin, Vector3 Cos) SinCos(Vector3 vector)
        {
            (Vector128<float> sin, Vector128<float> cos) = Vector128.SinCos(vector.AsVector128());
            return (sin.AsVector3(), cos.AsVector3());
        }

        /// <summary>Returns a vector whose elements are the square root of each of a specified vector's elements.</summary>
        /// <param name="value">A vector.</param>
        /// <returns>The square root vector.</returns>
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector3 SquareRoot(Vector3 value) => Vector128.Sqrt(value.AsVector128Unsafe()).AsVector3();

        /// <summary>Subtracts the second vector from the first.</summary>
        /// <param name="left">The first vector.</param>
        /// <param name="right">The second vector.</param>
        /// <returns>The difference vector.</returns>
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector3 Subtract(Vector3 left, Vector3 right) => (left.AsVector128Unsafe() - right.AsVector128Unsafe()).AsVector3();

        /// <inheritdoc cref="Vector4.Sum(Vector4)" />
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float Sum(Vector3 value) => Vector128.Sum(value.AsVector128());

        /// <summary>Transforms a vector by a specified 4x4 matrix.</summary>
        /// <param name="position">The vector to transform.</param>
        /// <param name="matrix">The transformation matrix.</param>
        /// <returns>The transformed vector.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector3 Transform(Vector3 position, Matrix4x4 matrix) => Transform(position.AsVector128Unsafe(), in matrix.AsROImpl()).AsVector3();

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static Vector128<float> Transform(Vector128<float> position, in Matrix4x4.Impl matrix)
        {
            // This implementation is based on the DirectX Math Library XMVector3Transform method
            // https://github.com/microsoft/DirectXMath/blob/master/Inc/DirectXMathVector.inl

            Vector128<float> result = matrix.X * position.GetElement(0);
            result = Vector128.MultiplyAddEstimate(matrix.Y, Vector128.Create(position.GetElement(1)), result);
            result = Vector128.MultiplyAddEstimate(matrix.Z, Vector128.Create(position.GetElement(2)), result);
            return result + matrix.W;
        }

        /// <summary>Transforms a vector by the specified Quaternion rotation value.</summary>
        /// <param name="value">The vector to rotate.</param>
        /// <param name="rotation">The rotation to apply.</param>
        /// <returns>The transformed vector.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector3 Transform(Vector3 value, Quaternion rotation) => Vector4.Transform(value.AsVector128Unsafe().WithElement(3, 1.0f), rotation.AsVector128()).AsVector3();

        /// <summary>Transforms a vector normal by the given 4x4 matrix.</summary>
        /// <param name="normal">The source vector.</param>
        /// <param name="matrix">The matrix.</param>
        /// <returns>The transformed vector.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector3 TransformNormal(Vector3 normal, Matrix4x4 matrix) => TransformNormal(normal.AsVector128Unsafe(), in matrix.AsROImpl()).AsVector3();

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static Vector128<float> TransformNormal(Vector128<float> normal, in Matrix4x4.Impl matrix)
        {
            Vector128<float> result = matrix.X * normal.GetElement(0);
            result = Vector128.MultiplyAddEstimate(matrix.Y, Vector128.Create(normal.GetElement(1)), result);
            result = Vector128.MultiplyAddEstimate(matrix.Z, Vector128.Create(normal.GetElement(2)), result);
            return result;
        }

        /// <inheritdoc cref="Vector4.Truncate(Vector4)" />
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector3 Truncate(Vector3 vector) => Vector128.Truncate(vector.AsVector128Unsafe()).AsVector3();

        /// <inheritdoc cref="Vector4.Xor(Vector4, Vector4)" />
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector3 Xor(Vector3 left, Vector3 right) => (left.AsVector128Unsafe() ^ right.AsVector128Unsafe()).AsVector3();

        /// <summary>Copies the elements of the vector to a specified array.</summary>
        /// <param name="array">The destination array.</param>
        /// <remarks><paramref name="array" /> must have at least three elements. The method copies the vector's elements starting at index 0.</remarks>
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
            Unsafe.As<float, Vector3>(ref array[0]) = this;
        }

        /// <summary>Copies the elements of the vector to a specified array starting at a specified index position.</summary>
        /// <param name="array">The destination array.</param>
        /// <param name="index">The index at which to copy the first element of the vector.</param>
        /// <remarks><paramref name="array" /> must have a sufficient number of elements to accommodate the three vector elements. In other words, elements <paramref name="index" />, <paramref name="index" /> + 1, and <paramref name="index" /> + 2 must already exist in <paramref name="array" />.</remarks>
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

            Unsafe.As<float, Vector3>(ref array[index]) = this;
        }

        /// <summary>Copies the vector to the given <see cref="Span{T}" />. The length of the destination span must be at least 3.</summary>
        /// <param name="destination">The destination span which the values are copied into.</param>
        /// <exception cref="ArgumentException">If number of elements in source vector is greater than those available in destination span.</exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly void CopyTo(Span<float> destination)
        {
            if (destination.Length < ElementCount)
            {
                ThrowHelper.ThrowArgumentException_DestinationTooShort();
            }
            Unsafe.As<float, Vector3>(ref MemoryMarshal.GetReference(destination)) = this;
        }

        /// <summary>Attempts to copy the vector to the given <see cref="Span{Single}" />. The length of the destination span must be at least 3.</summary>
        /// <param name="destination">The destination span which the values are copied into.</param>
        /// <returns><see langword="true" /> if the source vector was successfully copied to <paramref name="destination" />. <see langword="false" /> if <paramref name="destination" /> is not large enough to hold the source vector.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly bool TryCopyTo(Span<float> destination)
        {
            if (destination.Length < ElementCount)
            {
                return false;
            }

            Unsafe.As<float, Vector3>(ref MemoryMarshal.GetReference(destination)) = this;
            return true;
        }

        /// <summary>Returns a value that indicates whether this instance and a specified object are equal.</summary>
        /// <param name="obj">The object to compare with the current instance.</param>
        /// <returns><see langword="true" /> if the current instance and <paramref name="obj" /> are equal; otherwise, <see langword="false" />. If <paramref name="obj" /> is <see langword="null" />, the method returns <see langword="false" />.</returns>
        /// <remarks>The current instance and <paramref name="obj" /> are equal if <paramref name="obj" /> is a <see cref="Vector3" /> object and their corresponding elements are equal.</remarks>
        public override readonly bool Equals([NotNullWhen(true)] object? obj) => (obj is Vector3 other) && Equals(other);

        /// <summary>Returns a value that indicates whether this instance and another vector are equal.</summary>
        /// <param name="other">The other vector.</param>
        /// <returns><see langword="true" /> if the two vectors are equal; otherwise, <see langword="false" />.</returns>
        /// <remarks>Two vectors are equal if their <see cref="X" />, <see cref="Y" />, and <see cref="Z" /> elements are equal.</remarks>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly bool Equals(Vector3 other) => this.AsVector128().Equals(other.AsVector128());

        /// <summary>Returns the hash code for this instance.</summary>
        /// <returns>The hash code.</returns>
        public override readonly int GetHashCode() => HashCode.Combine(X, Y, Z);

        /// <summary>Returns the length of this vector object.</summary>
        /// <returns>The vector's length.</returns>
        /// <altmember cref="LengthSquared" />
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly float Length() => Vector128.Length(this.AsVector128());

        /// <summary>Returns the length of the vector squared.</summary>
        /// <returns>The vector's length squared.</returns>
        /// <remarks>This operation offers better performance than a call to the <see cref="Length" /> method.</remarks>
        /// <altmember cref="Length" />
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly float LengthSquared() => Vector128.LengthSquared(this.AsVector128());

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
            var handler = new DefaultInterpolatedStringHandler(literalLength: 4 + (separator.Length * 2), formattedCount: 3, formatProvider, stackalloc char[512]);
            handler.AppendLiteral("<");
            handler.AppendFormatted(X, format);
            handler.AppendLiteral(separator);
            handler.AppendLiteral(" ");
            handler.AppendFormatted(Y, format);
            handler.AppendLiteral(separator);
            handler.AppendLiteral(" ");
            handler.AppendFormatted(Z, format);
            handler.AppendLiteral(">");
            return handler.ToStringAndClear();
        }
    }
}
