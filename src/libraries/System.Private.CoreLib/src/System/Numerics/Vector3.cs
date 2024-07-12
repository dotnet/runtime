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
        /// <summary>The X component of the vector.</summary>
        public float X;

        /// <summary>The Y component of the vector.</summary>
        public float Y;

        /// <summary>The Z component of the vector.</summary>
        public float Z;

        internal const int Count = 3;

        /// <summary>Creates a new <see cref="Vector3" /> object whose three elements have the same value.</summary>
        /// <param name="value">The value to assign to all three elements.</param>
        [Intrinsic]
        public Vector3(float value)
        {
            this = Create(value);
        }

        /// <summary>Creates a   new <see cref="Vector3" /> object from the specified <see cref="Vector2" /> object and the specified value.</summary>
        /// <param name="value">The vector with two elements.</param>
        /// <param name="z">The additional value to assign to the <see cref="Z" /> field.</param>
        [Intrinsic]
        public Vector3(Vector2 value, float z)
        {
            this = Create(value, z);
        }

        /// <summary>Creates a vector whose elements have the specified values.</summary>
        /// <param name="x">The value to assign to the <see cref="X" /> field.</param>
        /// <param name="y">The value to assign to the <see cref="Y" /> field.</param>
        /// <param name="z">The value to assign to the <see cref="Z" /> field.</param>
        [Intrinsic]
        public Vector3(float x, float y, float z)
        {
            this = Create(x, y, z);
        }

        /// <summary>Constructs a vector from the given <see cref="ReadOnlySpan{Single}" />. The span must contain at least 3 elements.</summary>
        /// <param name="values">The span of elements to assign to the vector.</param>
        [Intrinsic]
        public Vector3(ReadOnlySpan<float> values)
        {
            this = Create(values);
        }

        /// <inheritdoc cref="Vector4.E" />
        public static Vector3 E
        {
            [Intrinsic]
            get => Create(float.E);
        }

        /// <inheritdoc cref="Vector4.Epsilon" />
        public static Vector3 Epsilon
        {
            [Intrinsic]
            get => Create(float.Epsilon);
        }

        /// <inheritdoc cref="Vector4.NaN" />
        public static Vector3 NaN
        {
            [Intrinsic]
            get => Create(float.NaN);
        }

        /// <inheritdoc cref="Vector4.NegativeInfinity" />
        public static Vector3 NegativeInfinity
        {
            [Intrinsic]
            get => Create(float.NegativeInfinity);
        }

        /// <inheritdoc cref="Vector4.NegativeZero" />
        public static Vector3 NegativeZero
        {
            [Intrinsic]
            get => Create(float.NegativeZero);
        }

        /// <inheritdoc cref="Vector4.One" />
        public static Vector3 One
        {
            [Intrinsic]
            get => Create(1.0f);
        }

        /// <inheritdoc cref="Vector4.Pi" />
        public static Vector3 Pi
        {
            [Intrinsic]
            get => Create(float.Pi);
        }

        /// <inheritdoc cref="Vector4.PositiveInfinity" />
        public static Vector3 PositiveInfinity
        {
            [Intrinsic]
            get => Create(float.PositiveInfinity);
        }

        /// <inheritdoc cref="Vector4.Tau" />
        public static Vector3 Tau
        {
            [Intrinsic]
            get => Create(float.Tau);
        }

        /// <summary>Gets the vector (1,0,0).</summary>
        /// <value>The vector <c>(1,0,0)</c>.</value>
        public static Vector3 UnitX
        {
            [Intrinsic]
            get => CreateScalar(1.0f);
        }

        /// <summary>Gets the vector (0,1,0).</summary>
        /// <value>The vector <c>(0,1,0)</c>.</value>
        public static Vector3 UnitY
        {
            [Intrinsic]
            get => Create(0.0f, 1.0f, 0.0f);
        }

        /// <summary>Gets the vector (0,0,1).</summary>
        /// <value>The vector <c>(0,0,1)</c>.</value>
        public static Vector3 UnitZ
        {
            [Intrinsic]
            get => Create(0.0f, 0.0f, 1.0f);
        }

        /// <inheritdoc cref="Vector4.Zero" />
        public static Vector3 Zero
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
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            readonly get
            {
                if ((uint)index >= Count)
                {
                    ThrowHelper.ThrowArgumentOutOfRangeException(ExceptionArgument.index);
                }
                return this.AsVector128Unsafe().GetElement(index);
            }

            [Intrinsic]
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            set
            {
                if ((uint)index >= Count)
                {
                    ThrowHelper.ThrowArgumentOutOfRangeException(ExceptionArgument.index);
                }
                this = this.AsVector128Unsafe().WithElement(index, value).AsVector3();
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
        public static bool operator !=(Vector3 left, Vector3 right) => !(left == right);

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
        public static Vector3 operator *(float left, Vector3 right) => right * left;

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
        public static Vector3 Add(Vector3 left, Vector3 right) => left + right;

        /// <inheritdoc cref="ISimdVector{TSelf, T}.Clamp(TSelf, TSelf, TSelf)" />
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector3 Clamp(Vector3 value1, Vector3 min, Vector3 max) => Vector128.Clamp(value1.AsVector128Unsafe(), min.AsVector128Unsafe(), max.AsVector128Unsafe()).AsVector3();

        /// <inheritdoc cref="ISimdVector{TSelf, T}.ClampNative(TSelf, TSelf, TSelf)" />
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector3 ClampNative(Vector3 value1, Vector3 min, Vector3 max) => Vector128.ClampNative(value1.AsVector128Unsafe(), min.AsVector128Unsafe(), max.AsVector128Unsafe()).AsVector3();

        /// <inheritdoc cref="ISimdVector{TSelf, T}.CopySign(TSelf, TSelf)" />
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector3 CopySign(Vector3 value, Vector3 sign) => Vector128.CopySign(value.AsVector128Unsafe(), sign.AsVector128Unsafe()).AsVector3();

        /// <summary>Creates a new <see cref="Vector3" /> object whose three elements have the same value.</summary>
        /// <param name="value">The value to assign to all three elements.</param>
        /// <returns>A new <see cref="Vector3" /> whose three elements have the same value.</returns>
        [Intrinsic]
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
        public static Vector3 Create(float x, float y, float z) => Vector128.Create(x, y, z, 0).AsVector3();

        /// <summary>Constructs a vector from the given <see cref="ReadOnlySpan{Single}" />. The span must contain at least 3 elements.</summary>
        /// <param name="values">The span of elements to assign to the vector.</param>
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector3 Create(ReadOnlySpan<float> values)
        {
            if (values.Length < Count)
            {
                ThrowHelper.ThrowArgumentOutOfRangeException(ExceptionArgument.values);
            }
            return Unsafe.ReadUnaligned<Vector3>(ref Unsafe.As<float, byte>(ref MemoryMarshal.GetReference(values)));
        }

        /// <summary>Creates a vector with <see cref="X" /> initialized to the specified value and the remaining elements initialized to zero.</summary>
        /// <param name="x">The value to assign to the <see cref="X" /> field.</param>
        /// <returns>A new <see cref="Vector3" /> with <see cref="X" /> initialized <paramref name="x" /> and the remaining elements initialized to zero.</returns>
        [Intrinsic]
        internal static Vector3 CreateScalar(float x) => Vector128.CreateScalar(x).AsVector3();

        /// <summary>Creates a vector with <see cref="X" /> initialized to the specified value and the remaining elements left uninitialized.</summary>
        /// <param name="x">The value to assign to the <see cref="X" /> field.</param>
        /// <returns>A new <see cref="Vector3" /> with <see cref="X" /> initialized <paramref name="x" /> and the remaining elements left uninitialized.</returns>
        [Intrinsic]
        internal static Vector3 CreateScalarUnsafe(float x) => Vector128.CreateScalarUnsafe(x).AsVector3();

        /// <summary>Computes the cross product of two vectors.</summary>
        /// <param name="vector1">The first vector.</param>
        /// <param name="vector2">The second vector.</param>
        /// <returns>The cross product.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector3 Cross(Vector3 vector1, Vector3 vector2)
        {
            // This implementation is based on the DirectX Math Library XMVector3Cross method
            // https://github.com/microsoft/DirectXMath/blob/master/Inc/DirectXMathVector.inl

            Vector128<float> v1 = vector1.AsVector128();
            Vector128<float> v2 = vector2.AsVector128();

            Vector128<float> temp = Vector128.Shuffle(v1, Vector128.Create(1, 2, 0, 3)) * Vector128.Shuffle(v2, Vector128.Create(2, 0, 1, 3));

            return Vector128.MultiplyAddEstimate(
                -Vector128.Shuffle(v1, Vector128.Create(2, 0, 1, 3)),
                 Vector128.Shuffle(v2, Vector128.Create(1, 2, 0, 3)),
                 temp
            ).AsVector3();
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
        public static float Distance(Vector3 value1, Vector3 value2) => float.Sqrt(DistanceSquared(value1, value2));

        /// <summary>Returns the Euclidean distance squared between two specified points.</summary>
        /// <param name="value1">The first point.</param>
        /// <param name="value2">The second point.</param>
        /// <returns>The distance squared.</returns>
        [Intrinsic]
        public static float DistanceSquared(Vector3 value1, Vector3 value2) => (value1 - value2).LengthSquared();

        /// <summary>Divides the first vector by the second.</summary>
        /// <param name="left">The first vector.</param>
        /// <param name="right">The second vector.</param>
        /// <returns>The vector resulting from the division.</returns>
        [Intrinsic]
        public static Vector3 Divide(Vector3 left, Vector3 right) => left / right;

        /// <summary>Divides the specified vector by a specified scalar value.</summary>
        /// <param name="left">The vector.</param>
        /// <param name="divisor">The scalar value.</param>
        /// <returns>The vector that results from the division.</returns>
        [Intrinsic]
        public static Vector3 Divide(Vector3 left, float divisor) => left / divisor;

        /// <summary>Returns the dot product of two vectors.</summary>
        /// <param name="vector1">The first vector.</param>
        /// <param name="vector2">The second vector.</param>
        /// <returns>The dot product.</returns>
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float Dot(Vector3 vector1, Vector3 vector2) => Vector128.Dot(vector1.AsVector128(), vector2.AsVector128());

        /// <inheritdoc cref="Vector4.Exp(Vector4)" />
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector3 Exp(Vector3 vector) => Vector128.Exp(vector.AsVector128Unsafe()).AsVector3();

        /// <inheritdoc cref="Vector128.MultiplyAddEstimate(Vector128{float}, Vector128{float}, Vector128{float})" />
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector3 FusedMultiplyAdd(Vector3 left, Vector3 right, Vector3 addend) => Vector128.FusedMultiplyAdd(left.AsVector128Unsafe(), right.AsVector128Unsafe(), addend.AsVector128Unsafe()).AsVector3();

        /// <inheritdoc cref="Vector4.Hypot(Vector4, Vector4)" />
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector3 Hypot(Vector3 x, Vector3 y) => Vector128.Hypot(x.AsVector128Unsafe(), y.AsVector128Unsafe()).AsVector3();

        /// <inheritdoc cref="Vector4.Lerp(Vector4, Vector4, float)" />
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector3 Lerp(Vector3 value1, Vector3 value2, float amount) => Lerp(value1, value2, Create(amount));

        /// <inheritdoc cref="Vector4.Lerp(Vector4, Vector4, Vector4)" />
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector3 Lerp(Vector3 value1, Vector3 value2, Vector3 amount) => Vector128.Lerp(value1.AsVector128Unsafe(), value2.AsVector128Unsafe(), amount.AsVector128Unsafe()).AsVector3();

        /// <inheritdoc cref="Vector4.Log2(Vector4)" />
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector3 Log(Vector3 vector) => Vector128.Log(vector.AsVector128Unsafe()).AsVector3();

        /// <inheritdoc cref="Vector4.Log(Vector4)" />
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector3 Log2(Vector3 vector) => Vector128.Log2(vector.AsVector128Unsafe()).AsVector3();

        /// <inheritdoc cref="ISimdVector{TSelf, T}.Max(TSelf, TSelf)" />
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector3 Max(Vector3 value1, Vector3 value2) => Vector128.Max(value1.AsVector128Unsafe(), value2.AsVector128Unsafe()).AsVector3();

        /// <inheritdoc cref="ISimdVector{TSelf, T}.MaxMagnitude(TSelf, TSelf)" />
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector3 MaxMagnitude(Vector3 value1, Vector3 value2) => Vector128.MaxMagnitude(value1.AsVector128Unsafe(), value2.AsVector128Unsafe()).AsVector3();

        /// <inheritdoc cref="ISimdVector{TSelf, T}.MaxMagnitudeNumber(TSelf, TSelf)" />
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector3 MaxMagnitudeNumber(Vector3 value1, Vector3 value2) => Vector128.MaxMagnitudeNumber(value1.AsVector128Unsafe(), value2.AsVector128Unsafe()).AsVector3();

        /// <inheritdoc cref="ISimdVector{TSelf, T}.MaxNative(TSelf, TSelf)" />
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector3 MaxNative(Vector3 value1, Vector3 value2) => Vector128.MaxNative(value1.AsVector128Unsafe(), value2.AsVector128Unsafe()).AsVector3();

        /// <inheritdoc cref="ISimdVector{TSelf, T}.MaxNumber(TSelf, TSelf)" />
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector3 MaxNumber(Vector3 value1, Vector3 value2) => Vector128.MaxNumber(value1.AsVector128Unsafe(), value2.AsVector128Unsafe()).AsVector3();

        /// <inheritdoc cref="ISimdVector{TSelf, T}.Min(TSelf, TSelf)" />
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector3 Min(Vector3 value1, Vector3 value2) => Vector128.Min(value1.AsVector128Unsafe(), value2.AsVector128Unsafe()).AsVector3();

        /// <inheritdoc cref="ISimdVector{TSelf, T}.MinMagnitude(TSelf, TSelf)" />
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector3 MinMagnitude(Vector3 value1, Vector3 value2) => Vector128.MinMagnitude(value1.AsVector128Unsafe(), value2.AsVector128Unsafe()).AsVector3();

        /// <inheritdoc cref="ISimdVector{TSelf, T}.MinMagnitudeNumber(TSelf, TSelf)" />
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector3 MinMagnitudeNumber(Vector3 value1, Vector3 value2) => Vector128.MinMagnitudeNumber(value1.AsVector128Unsafe(), value2.AsVector128Unsafe()).AsVector3();

        /// <inheritdoc cref="ISimdVector{TSelf, T}.MinNative(TSelf, TSelf)" />
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector3 MinNative(Vector3 value1, Vector3 value2) => Vector128.MinNative(value1.AsVector128Unsafe(), value2.AsVector128Unsafe()).AsVector3();

        /// <inheritdoc cref="ISimdVector{TSelf, T}.MinNumber(TSelf, TSelf)" />
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector3 MinNumber(Vector3 value1, Vector3 value2) => Vector128.MinNumber(value1.AsVector128Unsafe(), value2.AsVector128Unsafe()).AsVector3();

        /// <summary>Returns a new vector whose values are the product of each pair of elements in two specified vectors.</summary>
        /// <param name="left">The first vector.</param>
        /// <param name="right">The second vector.</param>
        /// <returns>The element-wise product vector.</returns>
        [Intrinsic]
        public static Vector3 Multiply(Vector3 left, Vector3 right) => left * right;

        /// <summary>Multiplies a vector by a specified scalar.</summary>
        /// <param name="left">The vector to multiply.</param>
        /// <param name="right">The scalar value.</param>
        /// <returns>The scaled vector.</returns>
        [Intrinsic]
        public static Vector3 Multiply(Vector3 left, float right) => left * right;

        /// <summary>Multiplies a scalar value by a specified vector.</summary>
        /// <param name="left">The scaled value.</param>
        /// <param name="right">The vector.</param>
        /// <returns>The scaled vector.</returns>
        [Intrinsic]
        public static Vector3 Multiply(float left, Vector3 right) => left * right;

        /// <inheritdoc cref="Vector128.MultiplyAddEstimate(Vector128{float}, Vector128{float}, Vector128{float})" />
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector3 MultiplyAddEstimate(Vector3 left, Vector3 right, Vector3 addend) => Vector128.MultiplyAddEstimate(left.AsVector128Unsafe(), right.AsVector128Unsafe(), addend.AsVector128Unsafe()).AsVector3();

        /// <summary>Negates a specified vector.</summary>
        /// <param name="value">The vector to negate.</param>
        /// <returns>The negated vector.</returns>
        [Intrinsic]
        public static Vector3 Negate(Vector3 value) => -value;

        /// <summary>Returns a vector with the same direction as the specified vector, but with a length of one.</summary>
        /// <param name="value">The vector to normalize.</param>
        /// <returns>The normalized vector.</returns>
        [Intrinsic]
        public static Vector3 Normalize(Vector3 value) => value / value.Length();

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

            Vector3 tmp = Create(Dot(vector, normal));
            tmp += tmp;
            return MultiplyAddEstimate(-tmp, normal, vector);
        }

        /// <inheritdoc cref="Vector4.Round(Vector4)" />
        [Intrinsic]
        public static Vector3 Round(Vector3 vector) => Vector128.Round(vector.AsVector128Unsafe()).AsVector3();

        /// <inheritdoc cref="Vector4.Round(Vector4, MidpointRounding)" />
        [Intrinsic]
        public static Vector3 Round(Vector3 vector, MidpointRounding mode) => Vector128.Round(vector.AsVector128Unsafe(), mode).AsVector3();

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
        public static Vector3 Subtract(Vector3 left, Vector3 right) => left - right;

        /// <summary>Transforms a vector by a specified 4x4 matrix.</summary>
        /// <param name="position">The vector to transform.</param>
        /// <param name="matrix">The transformation matrix.</param>
        /// <returns>The transformed vector.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector3 Transform(Vector3 position, Matrix4x4 matrix) => Vector4.Transform(position, in matrix.AsImpl()).AsVector128().AsVector3();

        /// <summary>Transforms a vector by the specified Quaternion rotation value.</summary>
        /// <param name="value">The vector to rotate.</param>
        /// <param name="rotation">The rotation to apply.</param>
        /// <returns>The transformed vector.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector3 Transform(Vector3 value, Quaternion rotation) => Vector4.Transform(value, rotation).AsVector3();

        /// <summary>Transforms a vector normal by the given 4x4 matrix.</summary>
        /// <param name="normal">The source vector.</param>
        /// <param name="matrix">The matrix.</param>
        /// <returns>The transformed vector.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector3 TransformNormal(Vector3 normal, Matrix4x4 matrix) => TransformNormal(normal, in matrix.AsImpl());

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static Vector3 TransformNormal(Vector3 normal, in Matrix4x4.Impl matrix)
        {
            Vector4 result = matrix.X * normal.X;

            result = Vector4.MultiplyAddEstimate(matrix.Y, Vector4.Create(normal.Y), result);
            result = Vector4.MultiplyAddEstimate(matrix.Z, Vector4.Create(normal.Z), result);

            return result.AsVector3();
        }

        /// <inheritdoc cref="Vector4.Truncate(Vector4)" />
        [Intrinsic]
        public static Vector3 Truncate(Vector3 vector) => Vector128.Truncate(vector.AsVector128Unsafe()).AsVector3();

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

            if (array.Length < Count)
            {
                ThrowHelper.ThrowArgumentException_DestinationTooShort();
            }

            Unsafe.WriteUnaligned(ref Unsafe.As<float, byte>(ref array[0]), this);
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

            if ((array.Length - index) < Count)
            {
                ThrowHelper.ThrowArgumentException_DestinationTooShort();
            }

            Unsafe.WriteUnaligned(ref Unsafe.As<float, byte>(ref array[index]), this);
        }

        /// <summary>Copies the vector to the given <see cref="Span{T}" />. The length of the destination span must be at least 3.</summary>
        /// <param name="destination">The destination span which the values are copied into.</param>
        /// <exception cref="ArgumentException">If number of elements in source vector is greater than those available in destination span.</exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly void CopyTo(Span<float> destination)
        {
            if (destination.Length < Count)
            {
                ThrowHelper.ThrowArgumentException_DestinationTooShort();
            }

            Unsafe.WriteUnaligned(ref Unsafe.As<float, byte>(ref MemoryMarshal.GetReference(destination)), this);
        }

        /// <summary>Attempts to copy the vector to the given <see cref="Span{Single}" />. The length of the destination span must be at least 3.</summary>
        /// <param name="destination">The destination span which the values are copied into.</param>
        /// <returns><see langword="true" /> if the source vector was successfully copied to <paramref name="destination" />. <see langword="false" /> if <paramref name="destination" /> is not large enough to hold the source vector.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly bool TryCopyTo(Span<float> destination)
        {
            if (destination.Length < Count)
            {
                return false;
            }

            Unsafe.WriteUnaligned(ref Unsafe.As<float, byte>(ref MemoryMarshal.GetReference(destination)), this);
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

            return $"<{X.ToString(format, formatProvider)}{separator} {Y.ToString(format, formatProvider)}{separator} {Z.ToString(format, formatProvider)}>";
        }
    }
}
