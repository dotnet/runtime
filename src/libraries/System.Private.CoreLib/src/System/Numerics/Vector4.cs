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
        /// <summary>The X component of the vector.</summary>
        public float X;

        /// <summary>The Y component of the vector.</summary>
        public float Y;

        /// <summary>The Z component of the vector.</summary>
        public float Z;

        /// <summary>The W component of the vector.</summary>
        public float W;

        internal const int Count = 4;

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
            readonly get => this.AsVector128().GetElement(index);

            [Intrinsic]
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            set
            {
                this = this.AsVector128().WithElement(index, value).AsVector4();
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

        /// <inheritdoc cref="ISimdVector{TSelf, T}.Clamp(TSelf, TSelf, TSelf)" />
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector4 Clamp(Vector4 value1, Vector4 min, Vector4 max) => Vector128.Clamp(value1.AsVector128(), min.AsVector128(), max.AsVector128()).AsVector4();

        /// <inheritdoc cref="ISimdVector{TSelf, T}.ClampNative(TSelf, TSelf, TSelf)" />
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector4 ClampNative(Vector4 value1, Vector4 min, Vector4 max) => Vector128.ClampNative(value1.AsVector128(), min.AsVector128(), max.AsVector128()).AsVector4();

        /// <inheritdoc cref="ISimdVector{TSelf, T}.CopySign(TSelf, TSelf)" />
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector4 CopySign(Vector4 value, Vector4 sign) => Vector128.CopySign(value.AsVector128(), sign.AsVector128()).AsVector4();

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

        /// <inheritdoc cref="Vector128.MultiplyAddEstimate(Vector128{float}, Vector128{float}, Vector128{float})" />
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector4 FusedMultiplyAdd(Vector4 left, Vector4 right, Vector4 addend) => Vector128.FusedMultiplyAdd(left.AsVector128(), right.AsVector128(), addend.AsVector128()).AsVector4();

        /// <inheritdoc cref="Vector128.Hypot(Vector128{float}, Vector128{float})" />
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector4 Hypot(Vector4 x, Vector4 y) => Vector128.Hypot(x.AsVector128(), y.AsVector128()).AsVector4();

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

        /// <inheritdoc cref="Vector128.Log(Vector128{float})" />
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector4 Log(Vector4 vector) => Vector128.Log(vector.AsVector128()).AsVector4();

        /// <inheritdoc cref="Vector128.Log2(Vector128{float})" />
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector4 Log2(Vector4 vector) => Vector128.Log2(vector.AsVector128()).AsVector4();

        /// <inheritdoc cref="ISimdVector{TSelf, T}.Max(TSelf, TSelf)" />
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector4 Max(Vector4 value1, Vector4 value2) => Vector128.Max(value1.AsVector128(), value2.AsVector128()).AsVector4();

        /// <inheritdoc cref="ISimdVector{TSelf, T}.MaxMagnitude(TSelf, TSelf)" />
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector4 MaxMagnitude(Vector4 value1, Vector4 value2) => Vector128.MaxMagnitude(value1.AsVector128(), value2.AsVector128()).AsVector4();

        /// <inheritdoc cref="ISimdVector{TSelf, T}.MaxMagnitudeNumber(TSelf, TSelf)" />
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector4 MaxMagnitudeNumber(Vector4 value1, Vector4 value2) => Vector128.MaxMagnitudeNumber(value1.AsVector128(), value2.AsVector128()).AsVector4();

        /// <inheritdoc cref="ISimdVector{TSelf, T}.MaxNative(TSelf, TSelf)" />
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector4 MaxNative(Vector4 value1, Vector4 value2) => Vector128.MaxNative(value1.AsVector128(), value2.AsVector128()).AsVector4();

        /// <inheritdoc cref="ISimdVector{TSelf, T}.MaxNumber(TSelf, TSelf)" />
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector4 MaxNumber(Vector4 value1, Vector4 value2) => Vector128.MaxNumber(value1.AsVector128(), value2.AsVector128()).AsVector4();

        /// <inheritdoc cref="ISimdVector{TSelf, T}.Min(TSelf, TSelf)" />
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector4 Min(Vector4 value1, Vector4 value2) => Vector128.Min(value1.AsVector128(), value2.AsVector128()).AsVector4();

        /// <inheritdoc cref="ISimdVector{TSelf, T}.MinMagnitude(TSelf, TSelf)" />
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector4 MinMagnitude(Vector4 value1, Vector4 value2) => Vector128.MinMagnitude(value1.AsVector128(), value2.AsVector128()).AsVector4();

        /// <inheritdoc cref="ISimdVector{TSelf, T}.MinMagnitudeNumber(TSelf, TSelf)" />
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector4 MinMagnitudeNumber(Vector4 value1, Vector4 value2) => Vector128.MinMagnitudeNumber(value1.AsVector128(), value2.AsVector128()).AsVector4();

        /// <inheritdoc cref="ISimdVector{TSelf, T}.MinNative(TSelf, TSelf)" />
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector4 MinNative(Vector4 value1, Vector4 value2) => Vector128.MinNative(value1.AsVector128(), value2.AsVector128()).AsVector4();

        /// <inheritdoc cref="ISimdVector{TSelf, T}.MinNumber(TSelf, TSelf)" />
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

        /// <summary>Returns a vector with the same direction as the specified vector, but with a length of one.</summary>
        /// <param name="vector">The vector to normalize.</param>
        /// <returns>The normalized vector.</returns>
        [Intrinsic]
        public static Vector4 Normalize(Vector4 vector) => vector / vector.Length();

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
        /// <altmember cref="LengthSquared"/>
        [Intrinsic]
        public readonly float Length() => float.Sqrt(LengthSquared());

        /// <summary>Returns the length of the vector squared.</summary>
        /// <returns>The vector's length squared.</returns>
        /// <remarks>This operation offers better performance than a call to the <see cref="Length" /> method.</remarks>
        /// <altmember cref="Length"/>
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
