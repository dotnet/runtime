// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics;

namespace System.Numerics
{
    /// <summary>Represents a vector that is used to encode three-dimensional physical rotations.</summary>
    /// <remarks>The <see cref="Quaternion" /> structure is used to efficiently rotate an object about the (x,y,z) vector by the angle theta, where:
    /// <c>w = cos(theta/2)</c></remarks>
    [Intrinsic]
    public struct Quaternion : IEquatable<Quaternion>
    {
        /// <summary>The X value of the vector component of the quaternion.</summary>
        public float X;

        /// <summary>The Y value of the vector component of the quaternion.</summary>
        public float Y;

        /// <summary>The Z value of the vector component of the quaternion.</summary>
        public float Z;

        /// <summary>The rotation component of the quaternion.</summary>
        public float W;

        internal const int Count = 4;

        /// <summary>Constructs a quaternion from the specified components.</summary>
        /// <param name="x">The value to assign to the X component of the quaternion.</param>
        /// <param name="y">The value to assign to the Y component of the quaternion.</param>
        /// <param name="z">The value to assign to the Z component of the quaternion.</param>
        /// <param name="w">The value to assign to the W component of the quaternion.</param>
        [Intrinsic]
        public Quaternion(float x, float y, float z, float w)
        {
            this = Create(x, y, z, w);
        }

        /// <summary>Creates a quaternion from the specified vector and rotation parts.</summary>
        /// <param name="vectorPart">The vector part of the quaternion.</param>
        /// <param name="scalarPart">The rotation part of the quaternion.</param>
        [Intrinsic]
        public Quaternion(Vector3 vectorPart, float scalarPart)
        {
            this = Create(vectorPart, scalarPart);
        }

        /// <summary>Gets a quaternion that represents a zero.</summary>
        /// <value>A quaternion whose values are <c>(0, 0, 0, 0)</c>.</value>
        public static Quaternion Zero
        {
            [Intrinsic]
            get => default;
        }

        /// <summary>Gets a quaternion that represents no rotation.</summary>
        /// <value>A quaternion whose values are <c>(0, 0, 0, 1)</c>.</value>
        public static Quaternion Identity
        {
            [Intrinsic]
            get => Create(0.0f, 0.0f, 0.0f, 1.0f);
        }

        /// <summary>Gets or sets the element at the specified index.</summary>
        /// <param name="index">The index of the element to get or set.</param>
        /// <returns>The element at <paramref name="index" />.</returns>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="index" /> was less than zero or greater than the number of elements.</exception>
        public float this[int index]
        {
            [Intrinsic]
            readonly get => this.AsVector128().GetElement(index);

            [Intrinsic]
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            set
            {
                this = this.AsVector128().WithElement(index, value).AsQuaternion();
            }
        }

        /// <summary>Gets a value that indicates whether the current instance is the identity quaternion.</summary>
        /// <value><see langword="true" /> if the current instance is the identity quaternion; otherwise, <see langword="false" />.</value>
        /// <altmember cref="Identity"/>
        public readonly bool IsIdentity => this == Identity;

        /// <summary>Adds each element in one quaternion with its corresponding element in a second quaternion.</summary>
        /// <param name="value1">The first quaternion.</param>
        /// <param name="value2">The second quaternion.</param>
        /// <returns>The quaternion that contains the summed values of <paramref name="value1" /> and <paramref name="value2" />.</returns>
        /// <remarks>The <see cref="op_Addition" /> method defines the operation of the addition operator for <see cref="Quaternion" /> objects.</remarks>
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Quaternion operator +(Quaternion value1, Quaternion value2) => (value1.AsVector128() + value2.AsVector128()).AsQuaternion();

        /// <summary>Divides one quaternion by a second quaternion.</summary>
        /// <param name="value1">The dividend.</param>
        /// <param name="value2">The divisor.</param>
        /// <returns>The quaternion that results from dividing <paramref name="value1" /> by <paramref name="value2" />.</returns>
        /// <remarks>The <see cref="op_Division" /> method defines the division operation for <see cref="Quaternion" /> objects.</remarks>
        public static Quaternion operator /(Quaternion value1, Quaternion value2) => value1 * Inverse(value2);

        /// <summary>Returns a value that indicates whether two quaternions are equal.</summary>
        /// <param name="value1">The first quaternion to compare.</param>
        /// <param name="value2">The second quaternion to compare.</param>
        /// <returns><see langword="true" /> if the two quaternions are equal; otherwise, <see langword="false" />.</returns>
        /// <remarks>Two quaternions are equal if each of their corresponding components is equal.
        /// The <see cref="op_Equality" /> method defines the operation of the equality operator for <see cref="Quaternion" /> objects.</remarks>
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator ==(Quaternion value1, Quaternion value2) => value1.AsVector128() == value2.AsVector128();

        /// <summary>Returns a value that indicates whether two quaternions are not equal.</summary>
        /// <param name="value1">The first quaternion to compare.</param>
        /// <param name="value2">The second quaternion to compare.</param>
        /// <returns><see langword="true" /> if <paramref name="value1" /> and <paramref name="value2" /> are not equal; otherwise, <see langword="false" />.</returns>
        [Intrinsic]
        public static bool operator !=(Quaternion value1, Quaternion value2) => !(value1 == value2);

        /// <summary>Returns the quaternion that results from multiplying two quaternions together.</summary>
        /// <param name="value1">The first quaternion.</param>
        /// <param name="value2">The second quaternion.</param>
        /// <returns>The product quaternion.</returns>
        /// <remarks>The <see cref="Quaternion.op_Multiply" /> method defines the operation of the multiplication operator for <see cref="Quaternion" /> objects.</remarks>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Quaternion operator *(Quaternion value1, Quaternion value2)
        {
            // This implementation is based on the DirectX Math Library XMQuaternionMultiply method
            // https://github.com/microsoft/DirectXMath/blob/master/Inc/DirectXMathMisc.inl

            Vector128<float> left = value1.AsVector128();
            Vector128<float> right = value2.AsVector128();

            Vector128<float> result = right * left.GetElement(3);
            result = Vector128.MultiplyAddEstimate(Vector128.Shuffle(right, Vector128.Create(3, 2, 1, 0)) * left.GetElement(0), Vector128.Create(+1.0f, -1.0f, +1.0f, -1.0f), result);
            result = Vector128.MultiplyAddEstimate(Vector128.Shuffle(right, Vector128.Create(2, 3, 0, 1)) * left.GetElement(1), Vector128.Create(+1.0f, +1.0f, -1.0f, -1.0f), result);
            result = Vector128.MultiplyAddEstimate(Vector128.Shuffle(right, Vector128.Create(1, 0, 3, 2)) * left.GetElement(2), Vector128.Create(-1.0f, +1.0f, +1.0f, -1.0f), result);
            return result.AsQuaternion();
        }

        /// <summary>Returns the quaternion that results from scaling all the components of a specified quaternion by a scalar factor.</summary>
        /// <param name="value1">The source quaternion.</param>
        /// <param name="value2">The scalar value.</param>
        /// <returns>The scaled quaternion.</returns>
        /// <remarks>The <see cref="Quaternion.op_Multiply" /> method defines the operation of the multiplication operator for <see cref="Quaternion" /> objects.</remarks>
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Quaternion operator *(Quaternion value1, float value2) => (value1.AsVector128() * value2).AsQuaternion();

        /// <summary>Subtracts each element in a second quaternion from its corresponding element in a first quaternion.</summary>
        /// <param name="value1">The first quaternion.</param>
        /// <param name="value2">The second quaternion.</param>
        /// <returns>The quaternion containing the values that result from subtracting each element in <paramref name="value2" /> from its corresponding element in <paramref name="value1" />.</returns>
        /// <remarks>The <see cref="op_Subtraction" /> method defines the operation of the subtraction operator for <see cref="Quaternion" /> objects.</remarks>
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Quaternion operator -(Quaternion value1, Quaternion value2) => (value1.AsVector128() - value2.AsVector128()).AsQuaternion();

        /// <summary>Reverses the sign of each component of the quaternion.</summary>
        /// <param name="value">The quaternion to negate.</param>
        /// <returns>The negated quaternion.</returns>
        /// <remarks>The <see cref="op_UnaryNegation" /> method defines the operation of the unary negation operator for <see cref="Quaternion" /> objects.</remarks>
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Quaternion operator -(Quaternion value) => (-value.AsVector128()).AsQuaternion();

        /// <summary>Adds each element in one quaternion with its corresponding element in a second quaternion.</summary>
        /// <param name="value1">The first quaternion.</param>
        /// <param name="value2">The second quaternion.</param>
        /// <returns>The quaternion that contains the summed values of <paramref name="value1" /> and <paramref name="value2" />.</returns>
        [Intrinsic]
        public static Quaternion Add(Quaternion value1, Quaternion value2) => value1 + value2;

        /// <summary>Concatenates two quaternions.</summary>
        /// <param name="value1">The first quaternion rotation in the series.</param>
        /// <param name="value2">The second quaternion rotation in the series.</param>
        /// <returns>A new quaternion representing the concatenation of the <paramref name="value1" /> rotation followed by the <paramref name="value2" /> rotation.</returns>
        public static Quaternion Concatenate(Quaternion value1, Quaternion value2) => value2 * value1;

        /// <summary>Returns the conjugate of a specified quaternion.</summary>
        /// <param name="value">The quaternion.</param>
        /// <returns>A new quaternion that is the conjugate of <see langword="value" />.</returns>
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Quaternion Conjugate(Quaternion value)
        {
            // This implementation is based on the DirectX Math Library XMQuaternionConjugate method
            // https://github.com/microsoft/DirectXMath/blob/master/Inc/DirectXMathMisc.inl

            return (value.AsVector128() * Vector128.Create(-1.0f, -1.0f, -1.0f, 1.0f)).AsQuaternion();
        }

        /// <summary>Creates a quaternion from the specified components.</summary>
        /// <param name="x">The value to assign to the X component of the quaternion.</param>
        /// <param name="y">The value to assign to the Y component of the quaternion.</param>
        /// <param name="z">The value to assign to the Z component of the quaternion.</param>
        /// <param name="w">The value to assign to the W component of the quaternion.</param>
        /// <returns>A new quaternion created from the specified components.</returns>>
        [Intrinsic]
        internal static Quaternion Create(float x, float y, float z, float w) => Vector128.Create(x, y, z, w).AsQuaternion();

        /// <summary>Creates a quaternion from the specified vector and rotation parts.</summary>
        /// <param name="vectorPart">The vector part of the quaternion.</param>
        /// <param name="scalarPart">The rotation part of the quaternion.</param>
        /// <returns>A new quaternion created from the specified vector and rotation parts.</returns>
        [Intrinsic]
        internal static Quaternion Create(Vector3 vectorPart, float scalarPart) => Vector4.Create(vectorPart, scalarPart).AsQuaternion();

        /// <summary>Creates a quaternion from a unit vector and an angle to rotate around the vector.</summary>
        /// <param name="axis">The unit vector to rotate around.</param>
        /// <param name="angle">The angle, in radians, to rotate around the vector.</param>
        /// <returns>The newly created quaternion.</returns>
        /// <remarks><paramref name="axis" /> vector must be normalized before calling this method or the resulting <see cref="Quaternion" /> will be incorrect.</remarks>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Quaternion CreateFromAxisAngle(Vector3 axis, float angle)
        {
            // This implementation is based on the DirectX Math Library XMQuaternionRotationNormal method
            // https://github.com/microsoft/DirectXMath/blob/master/Inc/DirectXMathMisc.inl

            (float s, float c) = float.SinCos(angle * 0.5f);
            return (Vector4.Create(axis, 1) * Vector4.Create(Vector3.Create(s), c)).AsQuaternion();
        }

        /// <summary>Creates a quaternion from the specified rotation matrix.</summary>
        /// <param name="matrix">The rotation matrix.</param>
        /// <returns>The newly created quaternion.</returns>
        public static Quaternion CreateFromRotationMatrix(Matrix4x4 matrix)
        {
            float trace = matrix.M11 + matrix.M22 + matrix.M33;

            Quaternion q = default;

            if (trace > 0.0f)
            {
                float s = float.Sqrt(trace + 1.0f);
                q.W = s * 0.5f;
                s = 0.5f / s;
                q.X = (matrix.M23 - matrix.M32) * s;
                q.Y = (matrix.M31 - matrix.M13) * s;
                q.Z = (matrix.M12 - matrix.M21) * s;
            }
            else
            {
                if (matrix.M11 >= matrix.M22 && matrix.M11 >= matrix.M33)
                {
                    float s = float.Sqrt(1.0f + matrix.M11 - matrix.M22 - matrix.M33);
                    float invS = 0.5f / s;
                    q.X = 0.5f * s;
                    q.Y = (matrix.M12 + matrix.M21) * invS;
                    q.Z = (matrix.M13 + matrix.M31) * invS;
                    q.W = (matrix.M23 - matrix.M32) * invS;
                }
                else if (matrix.M22 > matrix.M33)
                {
                    float s = float.Sqrt(1.0f + matrix.M22 - matrix.M11 - matrix.M33);
                    float invS = 0.5f / s;
                    q.X = (matrix.M21 + matrix.M12) * invS;
                    q.Y = 0.5f * s;
                    q.Z = (matrix.M32 + matrix.M23) * invS;
                    q.W = (matrix.M31 - matrix.M13) * invS;
                }
                else
                {
                    float s = float.Sqrt(1.0f + matrix.M33 - matrix.M11 - matrix.M22);
                    float invS = 0.5f / s;
                    q.X = (matrix.M31 + matrix.M13) * invS;
                    q.Y = (matrix.M32 + matrix.M23) * invS;
                    q.Z = 0.5f * s;
                    q.W = (matrix.M12 - matrix.M21) * invS;
                }
            }

            return q;
        }

        /// <summary>Creates a new quaternion from the given yaw, pitch, and roll.</summary>
        /// <param name="yaw">The yaw angle, in radians, around the Y axis.</param>
        /// <param name="pitch">The pitch angle, in radians, around the X axis.</param>
        /// <param name="roll">The roll angle, in radians, around the Z axis.</param>
        /// <returns>The resulting quaternion.</returns>
        public static Quaternion CreateFromYawPitchRoll(float yaw, float pitch, float roll)
        {
            (Vector3 sin, Vector3 cos) = Vector3.SinCos(Vector3.Create(roll, pitch, yaw) * 0.5f);

            (float sr, float cr) = (sin.X, cos.X);
            (float sp, float cp) = (sin.Y, cos.Y);
            (float sy, float cy) = (sin.Z, cos.Z);

            Quaternion result;

            result.X = cy * sp * cr + sy * cp * sr;
            result.Y = sy * cp * cr - cy * sp * sr;
            result.Z = cy * cp * sr - sy * sp * cr;
            result.W = cy * cp * cr + sy * sp * sr;

            return result;
        }

        /// <summary>Divides one quaternion by a second quaternion.</summary>
        /// <param name="value1">The dividend.</param>
        /// <param name="value2">The divisor.</param>
        /// <returns>The quaternion that results from dividing <paramref name="value1" /> by <paramref name="value2" />.</returns>
        public static Quaternion Divide(Quaternion value1, Quaternion value2) => value1 / value2;

        /// <summary>Calculates the dot product of two quaternions.</summary>
        /// <param name="quaternion1">The first quaternion.</param>
        /// <param name="quaternion2">The second quaternion.</param>
        /// <returns>The dot product.</returns>
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float Dot(Quaternion quaternion1, Quaternion quaternion2) => Vector128.Dot(quaternion1.AsVector128(), quaternion2.AsVector128());

        /// <summary>Returns the inverse of a quaternion.</summary>
        /// <param name="value">The quaternion.</param>
        /// <returns>The inverted quaternion.</returns>
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Quaternion Inverse(Quaternion value)
        {
            // This implementation is based on the DirectX Math Library XMQuaternionInverse method
            // https://github.com/microsoft/DirectXMath/blob/master/Inc/DirectXMathMisc.inl

            const float Epsilon = 1.192092896e-7f;

            //  -1   (       a              -v       )
            // q   = ( -------------   ------------- )
            //       (  a^2 + |v|^2  ,  a^2 + |v|^2  )

            Vector128<float> lengthSquared = Vector128.Create(value.LengthSquared());
            return Vector128.AndNot(
                (Conjugate(value).AsVector128() / lengthSquared),
                Vector128.LessThanOrEqual(lengthSquared, Vector128.Create(Epsilon))
            ).AsQuaternion();
        }

        /// <summary>Performs a linear interpolation between two quaternions based on a value that specifies the weighting of the second quaternion.</summary>
        /// <param name="quaternion1">The first quaternion.</param>
        /// <param name="quaternion2">The second quaternion.</param>
        /// <param name="amount">The relative weight of <paramref name="quaternion2" /> in the interpolation.</param>
        /// <returns>The interpolated quaternion.</returns>
        public static Quaternion Lerp(Quaternion quaternion1, Quaternion quaternion2, float amount)
        {
            Vector128<float> q2 = quaternion2.AsVector128();

            q2 = Vector128.ConditionalSelect(
                Vector128.GreaterThanOrEqual(Vector128.Create(Dot(quaternion1, quaternion2)), Vector128<float>.Zero),
                 q2,
                -q2
            );

            Vector128<float> result = Vector128.MultiplyAddEstimate(quaternion1.AsVector128(), Vector128.Create(1.0f - amount), q2 * amount);
            return Normalize(result.AsQuaternion());
        }

        /// <summary>Returns the quaternion that results from multiplying two quaternions together.</summary>
        /// <param name="value1">The first quaternion.</param>
        /// <param name="value2">The second quaternion.</param>
        /// <returns>The product quaternion.</returns>
        public static Quaternion Multiply(Quaternion value1, Quaternion value2) => value1 * value2;

        /// <summary>Returns the quaternion that results from scaling all the components of a specified quaternion by a scalar factor.</summary>
        /// <param name="value1">The source quaternion.</param>
        /// <param name="value2">The scalar value.</param>
        /// <returns>The scaled quaternion.</returns>
        [Intrinsic]
        public static Quaternion Multiply(Quaternion value1, float value2) => value1 * value2;

        /// <summary>Reverses the sign of each component of the quaternion.</summary>
        /// <param name="value">The quaternion to negate.</param>
        /// <returns>The negated quaternion.</returns>
        [Intrinsic]
        public static Quaternion Negate(Quaternion value) => -value;

        /// <summary>Divides each component of a specified <see cref="Quaternion" /> by its length.</summary>
        /// <param name="value">The quaternion to normalize.</param>
        /// <returns>The normalized quaternion.</returns>
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Quaternion Normalize(Quaternion value) => (value.AsVector128() / value.Length()).AsQuaternion();

        /// <summary>Interpolates between two quaternions, using spherical linear interpolation.</summary>
        /// <param name="quaternion1">The first quaternion.</param>
        /// <param name="quaternion2">The second quaternion.</param>
        /// <param name="amount">The relative weight of the second quaternion in the interpolation.</param>
        /// <returns>The interpolated quaternion.</returns>
        public static Quaternion Slerp(Quaternion quaternion1, Quaternion quaternion2, float amount)
        {
            const float SlerpEpsilon = 1e-6f;

            float cosOmega = Dot(quaternion1, quaternion2);
            float sign = 1.0f;

            if (cosOmega < 0.0f)
            {
                cosOmega = -cosOmega;
                sign = -1.0f;
            }

            float s1, s2;

            if (cosOmega > (1.0f - SlerpEpsilon))
            {
                // Too close, do straight linear interpolation.
                s1 = 1.0f - amount;
                s2 = amount * sign;
            }
            else
            {
                float omega = float.Acos(cosOmega);
                float invSinOmega = 1 / float.Sin(omega);

                s1 = float.Sin((1.0f - amount) * omega) * invSinOmega;
                s2 = float.Sin(amount * omega) * invSinOmega * sign;
            }

            return (quaternion1 * s1) + (quaternion2 * s2);
        }

        /// <summary>Subtracts each element in a second quaternion from its corresponding element in a first quaternion.</summary>
        /// <param name="value1">The first quaternion.</param>
        /// <param name="value2">The second quaternion.</param>
        /// <returns>The quaternion containing the values that result from subtracting each element in <paramref name="value2" /> from its corresponding element in <paramref name="value1" />.</returns>
        [Intrinsic]
        public static Quaternion Subtract(Quaternion value1, Quaternion value2) => value1 - value2;

        /// <summary>Returns a value that indicates whether this instance and a specified object are equal.</summary>
        /// <param name="obj">The object to compare with the current instance.</param>
        /// <returns><see langword="true" /> if the current instance and <paramref name="obj" /> are equal; otherwise, <see langword="false" />. If <paramref name="obj" /> is <see langword="null" />, the method returns <see langword="false" />.</returns>
        /// <remarks>The current instance and <paramref name="obj" /> are equal if <paramref name="obj" /> is a <see cref="Quaternion" /> object and the corresponding components of each matrix are equal.</remarks>
        public override readonly bool Equals([NotNullWhen(true)] object? obj) => (obj is Quaternion other) && Equals(other);

        /// <summary>Returns a value that indicates whether this instance and another quaternion are equal.</summary>
        /// <param name="other">The other quaternion.</param>
        /// <returns><see langword="true" /> if the two quaternions are equal; otherwise, <see langword="false" />.</returns>
        /// <remarks>Two quaternions are equal if each of their corresponding components is equal.</remarks>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly bool Equals(Quaternion other) => this.AsVector128().Equals(other.AsVector128());

        /// <summary>Returns the hash code for this instance.</summary>
        /// <returns>The hash code.</returns>
        public override readonly int GetHashCode() => HashCode.Combine(X, Y, Z, W);

        /// <summary>Calculates the length of the quaternion.</summary>
        /// <returns>The computed length of the quaternion.</returns>
        [Intrinsic]
        public readonly float Length() => float.Sqrt(LengthSquared());

        /// <summary>Calculates the squared length of the quaternion.</summary>
        /// <returns>The length squared of the quaternion.</returns>
        [Intrinsic]
        public readonly float LengthSquared() => Dot(this, this);

        /// <summary>Returns a string that represents this quaternion.</summary>
        /// <returns>The string representation of this quaternion.</returns>
        /// <remarks>The numeric values in the returned string are formatted by using the conventions of the current culture. For example, for the en-US culture, the returned string might appear as <c>{X:1.1 Y:2.2 Z:3.3 W:4.4}</c>.</remarks>
        public override readonly string ToString() => $"{{X:{X} Y:{Y} Z:{Z} W:{W}}}";
    }
}
