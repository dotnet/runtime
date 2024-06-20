// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace System.Numerics
{
    /// <summary>Represents a 4x4 matrix.</summary>
    /// <remarks><format type="text/markdown"><![CDATA[
    /// [!INCLUDE[vectors-are-rows-paragraph](~/includes/system-numerics-vectors-are-rows.md)]
    /// ]]></format></remarks>
    [Intrinsic]
    public partial struct Matrix4x4 : IEquatable<Matrix4x4>
    {
        // In an ideal world, we'd have 4x Vector4 fields. However, Matrix4x4 was shipped with
        // 16x public float fields and as such we cannot change the "backing" fields without it being
        // a breaking change. Likewise, we cannot switch to using something like ExplicitLayout
        // without it pessimizing other parts of the JIT and still preventing things like field promotion.
        //
        // This nested Impl struct works around this problem by relying on the JIT treating same sizeof
        // value type bitcasts as a no-op. Effectively the entire implementation is here in this type
        // and the public facing Matrix4x4 just defers to it with simple reinterpret casts inserted
        // at the relevant points.

        /// <summary>The first element of the first row.</summary>
        public float M11;

        /// <summary>The second element of the first row.</summary>
        public float M12;

        /// <summary>The third element of the first row.</summary>
        public float M13;

        /// <summary>The fourth element of the first row.</summary>
        public float M14;

        /// <summary>The first element of the second row.</summary>
        public float M21;

        /// <summary>The second element of the second row.</summary>
        public float M22;

        /// <summary>The third element of the second row.</summary>
        public float M23;

        /// <summary>The fourth element of the second row.</summary>
        public float M24;

        /// <summary>The first element of the third row.</summary>
        public float M31;

        /// <summary>The second element of the third row.</summary>
        public float M32;

        /// <summary>The third element of the third row.</summary>
        public float M33;

        /// <summary>The fourth element of the third row.</summary>
        public float M34;

        /// <summary>The first element of the fourth row.</summary>
        public float M41;

        /// <summary>The second element of the fourth row.</summary>
        public float M42;

        /// <summary>The third element of the fourth row.</summary>
        public float M43;

        /// <summary>The fourth element of the fourth row.</summary>
        public float M44;

        /// <summary>Creates a 4x4 matrix from the specified components.</summary>
        /// <param name="m11">The value to assign to the first element in the first row.</param>
        /// <param name="m12">The value to assign to the second element in the first row.</param>
        /// <param name="m13">The value to assign to the third element in the first row.</param>
        /// <param name="m14">The value to assign to the fourth element in the first row.</param>
        /// <param name="m21">The value to assign to the first element in the second row.</param>
        /// <param name="m22">The value to assign to the second element in the second row.</param>
        /// <param name="m23">The value to assign to the third element in the second row.</param>
        /// <param name="m24">The value to assign to the third element in the second row.</param>
        /// <param name="m31">The value to assign to the first element in the third row.</param>
        /// <param name="m32">The value to assign to the second element in the third row.</param>
        /// <param name="m33">The value to assign to the third element in the third row.</param>
        /// <param name="m34">The value to assign to the fourth element in the third row.</param>
        /// <param name="m41">The value to assign to the first element in the fourth row.</param>
        /// <param name="m42">The value to assign to the second element in the fourth row.</param>
        /// <param name="m43">The value to assign to the third element in the fourth row.</param>
        /// <param name="m44">The value to assign to the fourth element in the fourth row.</param>
        public Matrix4x4(float m11, float m12, float m13, float m14,
                         float m21, float m22, float m23, float m24,
                         float m31, float m32, float m33, float m34,
                         float m41, float m42, float m43, float m44)
        {
            Unsafe.SkipInit(out this);

            AsImpl().Init(
                m11, m12, m13, m14,
                m21, m22, m23, m24,
                m31, m32, m33, m34,
                m41, m42, m43, m44
            );
        }

        /// <summary>Creates a <see cref="Matrix4x4" /> object from a specified <see cref="Matrix3x2" /> object.</summary>
        /// <param name="value">A 3x2 matrix.</param>
        /// <remarks>This constructor creates a 4x4 matrix whose <see cref="M13" />, <see cref="M14" />, <see cref="M23" />, <see cref="M24" />, <see cref="M31" />, <see cref="M32" />, <see cref="M34" />, and <see cref="M43" /> components are zero, and whose <see cref="M33" /> and <see cref="M44" /> components are one.</remarks>
        public Matrix4x4(Matrix3x2 value)
        {
            Unsafe.SkipInit(out this);
            AsImpl().Init(in value.AsImpl());
        }

        /// <summary>Gets the multiplicative identity matrix.</summary>
        /// <value>Gets the multiplicative identity matrix.</value>
        public static Matrix4x4 Identity
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => Impl.Identity.AsM4x4();
        }

        /// <summary>Gets or sets the element at the specified indices.</summary>
        /// <param name="row">The index of the row containing the element to get or set.</param>
        /// <param name="column">The index of the column containing the element to get or set.</param>
        /// <returns>The element at [<paramref name="row" />][<paramref name="column" />].</returns>
        /// <exception cref="ArgumentOutOfRangeException">
        /// <paramref name="row" /> was less than zero or greater than the number of rows.
        /// -or-
        /// <paramref name="column" /> was less than zero or greater than the number of columns.
        /// </exception>
        public float this[int row, int column]
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            readonly get => AsROImpl()[row, column];

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            set => AsImpl()[row, column] = value;
        }

        /// <summary>Indicates whether the current matrix is the identity matrix.</summary>
        /// <value><see langword="true" /> if the current matrix is the identity matrix; otherwise, <see langword="false" />.</value>
        public readonly bool IsIdentity
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => AsROImpl().IsIdentity;
        }

        /// <summary>Gets or sets the translation component of this matrix.</summary>
        /// <value>The translation component of the current instance.</value>
        public Vector3 Translation
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            readonly get => AsROImpl().Translation;

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            set => AsImpl().Translation = value;
        }

        /// <summary>Adds each element in one matrix with its corresponding element in a second matrix.</summary>
        /// <param name="value1">The first matrix.</param>
        /// <param name="value2">The second matrix.</param>
        /// <returns>The matrix that contains the summed values.</returns>
        /// <remarks>The <see cref="op_Addition" /> method defines the operation of the addition operator for <see cref="Matrix4x4" /> objects.</remarks>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Matrix4x4 operator +(Matrix4x4 value1, Matrix4x4 value2)
            => (value1.AsImpl() + value2.AsImpl()).AsM4x4();

        /// <summary>Returns a value that indicates whether the specified matrices are equal.</summary>
        /// <param name="value1">The first matrix to compare.</param>
        /// <param name="value2">The second matrix to care</param>
        /// <returns><see langword="true" /> if <paramref name="value1" /> and <paramref name="value2" /> are equal; otherwise, <see langword="false" />.</returns>
        /// <remarks>Two matrices are equal if all their corresponding elements are equal.</remarks>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator ==(Matrix4x4 value1, Matrix4x4 value2)
            => value1.AsImpl() == value2.AsImpl();

        /// <summary>Returns a value that indicates whether the specified matrices are not equal.</summary>
        /// <param name="value1">The first matrix to compare.</param>
        /// <param name="value2">The second matrix to compare.</param>
        /// <returns><see langword="true" /> if <paramref name="value1" /> and <paramref name="value2" /> are not equal; otherwise, <see langword="false" />.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator !=(Matrix4x4 value1, Matrix4x4 value2)
            => value1.AsImpl() != value2.AsImpl();

        /// <summary>Multiplies two matrices together to compute the product.</summary>
        /// <param name="value1">The first matrix.</param>
        /// <param name="value2">The second matrix.</param>
        /// <returns>The product matrix.</returns>
        /// <remarks>The <see cref="Matrix4x4.op_Multiply" /> method defines the operation of the multiplication operator for <see cref="Matrix4x4" /> objects.</remarks>
        public static Matrix4x4 operator *(Matrix4x4 value1, Matrix4x4 value2)
            => (value1.AsImpl() * value2.AsImpl()).AsM4x4();

        /// <summary>Multiplies a matrix by a float to compute the product.</summary>
        /// <param name="value1">The matrix to scale.</param>
        /// <param name="value2">The scaling value to use.</param>
        /// <returns>The scaled matrix.</returns>
        /// <remarks>The <see cref="Matrix4x4.op_Multiply" /> method defines the operation of the multiplication operator for <see cref="Matrix4x4" /> objects.</remarks>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Matrix4x4 operator *(Matrix4x4 value1, float value2)
            => (value1.AsImpl() * value2).AsM4x4();

        /// <summary>Subtracts each element in a second matrix from its corresponding element in a first matrix.</summary>
        /// <param name="value1">The first matrix.</param>
        /// <param name="value2">The second matrix.</param>
        /// <returns>The matrix containing the values that result from subtracting each element in <paramref name="value2" /> from its corresponding element in <paramref name="value1" />.</returns>
        /// <remarks>The <see cref="op_Subtraction" /> method defines the operation of the subtraction operator for <see cref="Matrix4x4" /> objects.</remarks>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Matrix4x4 operator -(Matrix4x4 value1, Matrix4x4 value2)
            => (value1.AsImpl() - value2.AsImpl()).AsM4x4();

        /// <summary>Negates the specified matrix by multiplying all its values by -1.</summary>
        /// <param name="value">The matrix to negate.</param>
        /// <returns>The negated matrix.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Matrix4x4 operator -(Matrix4x4 value)
            => (-value.AsImpl()).AsM4x4();

        /// <summary>Adds each element in one matrix with its corresponding element in a second matrix.</summary>
        /// <param name="value1">The first matrix.</param>
        /// <param name="value2">The second matrix.</param>
        /// <returns>The matrix that contains the summed values of <paramref name="value1" /> and <paramref name="value2" />.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Matrix4x4 Add(Matrix4x4 value1, Matrix4x4 value2)
            => (value1.AsImpl() + value2.AsImpl()).AsM4x4();

        /// <summary>Creates a spherical billboard that rotates around a specified object position.</summary>
        /// <param name="objectPosition">The position of the object that the billboard will rotate around.</param>
        /// <param name="cameraPosition">The position of the camera.</param>
        /// <param name="cameraUpVector">The up vector of the camera.</param>
        /// <param name="cameraForwardVector">The forward vector of the camera.</param>
        /// <returns>The created billboard.</returns>
        public static Matrix4x4 CreateBillboard(Vector3 objectPosition, Vector3 cameraPosition, Vector3 cameraUpVector, Vector3 cameraForwardVector)
            => Impl.CreateBillboard(in objectPosition, in cameraPosition, in cameraUpVector, in cameraForwardVector).AsM4x4();

        /// <summary>Creates a cylindrical billboard that rotates around a specified axis.</summary>
        /// <param name="objectPosition">The position of the object that the billboard will rotate around.</param>
        /// <param name="cameraPosition">The position of the camera.</param>
        /// <param name="rotateAxis">The axis to rotate the billboard around.</param>
        /// <param name="cameraForwardVector">The forward vector of the camera.</param>
        /// <param name="objectForwardVector">The forward vector of the object.</param>
        /// <returns>The billboard matrix.</returns>
        public static Matrix4x4 CreateConstrainedBillboard(Vector3 objectPosition, Vector3 cameraPosition, Vector3 rotateAxis, Vector3 cameraForwardVector, Vector3 objectForwardVector)
            => Impl.CreateConstrainedBillboard(in objectPosition, in cameraPosition, in rotateAxis, in cameraForwardVector, in objectForwardVector).AsM4x4();

        /// <summary>Creates a matrix that rotates around an arbitrary vector.</summary>
        /// <param name="axis">The axis to rotate around.</param>
        /// <param name="angle">The angle to rotate around <paramref name="axis" />, in radians.</param>
        /// <returns>The rotation matrix.</returns>
        public static Matrix4x4 CreateFromAxisAngle(Vector3 axis, float angle)
            => Impl.CreateFromAxisAngle(in axis, angle).AsM4x4();

        /// <summary>Creates a rotation matrix from the specified Quaternion rotation value.</summary>
        /// <param name="quaternion">The source Quaternion.</param>
        /// <returns>The rotation matrix.</returns>
        public static Matrix4x4 CreateFromQuaternion(Quaternion quaternion)
            => Impl.CreateFromQuaternion(in quaternion).AsM4x4();

        /// <summary>Creates a rotation matrix from the specified yaw, pitch, and roll.</summary>
        /// <param name="yaw">The angle of rotation, in radians, around the Y axis.</param>
        /// <param name="pitch">The angle of rotation, in radians, around the X axis.</param>
        /// <param name="roll">The angle of rotation, in radians, around the Z axis.</param>
        /// <returns>The rotation matrix.</returns>
        public static Matrix4x4 CreateFromYawPitchRoll(float yaw, float pitch, float roll)
            => Impl.CreateFromYawPitchRoll(yaw, pitch, roll).AsM4x4();

        /// <summary>Creates a right-handed view matrix.</summary>
        /// <param name="cameraPosition">The position of the camera.</param>
        /// <param name="cameraTarget">The target towards which the camera is pointing.</param>
        /// <param name="cameraUpVector">The direction that is "up" from the camera's point of view.</param>
        /// <returns>The right-handed view matrix.</returns>
        public static Matrix4x4 CreateLookAt(Vector3 cameraPosition, Vector3 cameraTarget, Vector3 cameraUpVector)
        {
            Vector3 cameraDirection = cameraTarget - cameraPosition;
            return Impl.CreateLookTo(in cameraPosition, in cameraDirection, in cameraUpVector).AsM4x4();
        }

        /// <summary>Creates a left-handed view matrix.</summary>
        /// <param name="cameraPosition">The position of the camera.</param>
        /// <param name="cameraTarget">The target towards which the camera is pointing.</param>
        /// <param name="cameraUpVector">The direction that is "up" from the camera's point of view.</param>
        /// <returns>The left-handed view matrix.</returns>
        public static Matrix4x4 CreateLookAtLeftHanded(Vector3 cameraPosition, Vector3 cameraTarget, Vector3 cameraUpVector)
        {
            Vector3 cameraDirection = cameraTarget - cameraPosition;
            return Impl.CreateLookToLeftHanded(in cameraPosition, in cameraDirection, in cameraUpVector).AsM4x4();
        }

        /// <summary>Creates a right-handed view matrix.</summary>
        /// <param name="cameraPosition">The position of the camera.</param>
        /// <param name="cameraDirection">The direction in which the camera is pointing.</param>
        /// <param name="cameraUpVector">The direction that is "up" from the camera's point of view.</param>
        /// <returns>The right-handed view matrix.</returns>
        public static Matrix4x4 CreateLookTo(Vector3 cameraPosition, Vector3 cameraDirection, Vector3 cameraUpVector)
            => Impl.CreateLookTo(in cameraPosition, in cameraDirection, in cameraUpVector).AsM4x4();

        /// <summary>Creates a left-handed view matrix.</summary>
        /// <param name="cameraPosition">The position of the camera.</param>
        /// <param name="cameraDirection">The direction in which the camera is pointing.</param>
        /// <param name="cameraUpVector">The direction that is "up" from the camera's point of view.</param>
        /// <returns>The left-handed view matrix.</returns>
        public static Matrix4x4 CreateLookToLeftHanded(Vector3 cameraPosition, Vector3 cameraDirection, Vector3 cameraUpVector)
        {
            return Impl.CreateLookToLeftHanded(in cameraPosition, in cameraDirection, in cameraUpVector).AsM4x4();
        }

        /// <summary>Creates a right-handed orthographic perspective matrix from the given view volume dimensions.</summary>
        /// <param name="width">The width of the view volume.</param>
        /// <param name="height">The height of the view volume.</param>
        /// <param name="zNearPlane">The minimum Z-value of the view volume.</param>
        /// <param name="zFarPlane">The maximum Z-value of the view volume.</param>
        /// <returns>The right-handed orthographic projection matrix.</returns>
        public static Matrix4x4 CreateOrthographic(float width, float height, float zNearPlane, float zFarPlane)
            => Impl.CreateOrthographic(width, height, zNearPlane, zFarPlane).AsM4x4();

        /// <summary>Creates a left-handed orthographic perspective matrix from the given view volume dimensions.</summary>
        /// <param name="width">The width of the view volume.</param>
        /// <param name="height">The height of the view volume.</param>
        /// <param name="zNearPlane">The minimum Z-value of the view volume.</param>
        /// <param name="zFarPlane">The maximum Z-value of the view volume.</param>
        /// <returns>The left-handed orthographic projection matrix.</returns>
        public static Matrix4x4 CreateOrthographicLeftHanded(float width, float height, float zNearPlane, float zFarPlane)
            => Impl.CreateOrthographicLeftHanded(width, height, zNearPlane, zFarPlane).AsM4x4();

        /// <summary>Creates a right-handed customized orthographic projection matrix.</summary>
        /// <param name="left">The minimum X-value of the view volume.</param>
        /// <param name="right">The maximum X-value of the view volume.</param>
        /// <param name="bottom">The minimum Y-value of the view volume.</param>
        /// <param name="top">The maximum Y-value of the view volume.</param>
        /// <param name="zNearPlane">The minimum Z-value of the view volume.</param>
        /// <param name="zFarPlane">The maximum Z-value of the view volume.</param>
        /// <returns>The right-handed orthographic projection matrix.</returns>
        public static Matrix4x4 CreateOrthographicOffCenter(float left, float right, float bottom, float top, float zNearPlane, float zFarPlane)
            => Impl.CreateOrthographicOffCenter(left, right, bottom, top, zNearPlane, zFarPlane).AsM4x4();

        /// <summary>Creates a left-handed customized orthographic projection matrix.</summary>
        /// <param name="left">The minimum X-value of the view volume.</param>
        /// <param name="right">The maximum X-value of the view volume.</param>
        /// <param name="bottom">The minimum Y-value of the view volume.</param>
        /// <param name="top">The maximum Y-value of the view volume.</param>
        /// <param name="zNearPlane">The minimum Z-value of the view volume.</param>
        /// <param name="zFarPlane">The maximum Z-value of the view volume.</param>
        /// <returns>The left-handed orthographic projection matrix.</returns>
        public static Matrix4x4 CreateOrthographicOffCenterLeftHanded(float left, float right, float bottom, float top, float zNearPlane, float zFarPlane)
            => Impl.CreateOrthographicOffCenterLeftHanded(left, right, bottom, top, zNearPlane, zFarPlane).AsM4x4();

        /// <summary>Creates a right-handed perspective projection matrix from the given view volume dimensions.</summary>
        /// <param name="width">The width of the view volume at the near view plane.</param>
        /// <param name="height">The height of the view volume at the near view plane.</param>
        /// <param name="nearPlaneDistance">The distance to the near view plane.</param>
        /// <param name="farPlaneDistance">The distance to the far view plane.</param>
        /// <returns>The right-handed perspective projection matrix.</returns>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="nearPlaneDistance" /> is less than or equal to zero.
        /// -or-
        /// <paramref name="farPlaneDistance" /> is less than or equal to zero.
        /// -or-
        /// <paramref name="nearPlaneDistance" /> is greater than or equal to <paramref name="farPlaneDistance" />.</exception>
        public static Matrix4x4 CreatePerspective(float width, float height, float nearPlaneDistance, float farPlaneDistance)
            => Impl.CreatePerspective(width, height, nearPlaneDistance, farPlaneDistance).AsM4x4();

        /// <summary>Creates a left-handed perspective projection matrix from the given view volume dimensions.</summary>
        /// <param name="width">The width of the view volume at the near view plane.</param>
        /// <param name="height">The height of the view volume at the near view plane.</param>
        /// <param name="nearPlaneDistance">The distance to the near view plane.</param>
        /// <param name="farPlaneDistance">The distance to the far view plane.</param>
        /// <returns>The left-handed perspective projection matrix.</returns>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="nearPlaneDistance" /> is less than or equal to zero.
        /// -or-
        /// <paramref name="farPlaneDistance" /> is less than or equal to zero.
        /// -or-
        /// <paramref name="nearPlaneDistance" /> is greater than or equal to <paramref name="farPlaneDistance" />.</exception>
        public static Matrix4x4 CreatePerspectiveLeftHanded(float width, float height, float nearPlaneDistance, float farPlaneDistance)
            => Impl.CreatePerspectiveLeftHanded(width, height, nearPlaneDistance, farPlaneDistance).AsM4x4();

        /// <summary>Creates a right-handed perspective projection matrix based on a field of view, aspect ratio, and near and far view plane distances.</summary>
        /// <param name="fieldOfView">The field of view in the y direction, in radians.</param>
        /// <param name="aspectRatio">The aspect ratio, defined as view space width divided by height.</param>
        /// <param name="nearPlaneDistance">The distance to the near view plane.</param>
        /// <param name="farPlaneDistance">The distance to the far view plane.</param>
        /// <returns>The right-handed perspective projection matrix.</returns>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="fieldOfView" /> is less than or equal to zero.
        /// -or-
        /// <paramref name="fieldOfView" /> is greater than or equal to <see cref="float.Pi" />.
        /// <paramref name="nearPlaneDistance" /> is less than or equal to zero.
        /// -or-
        /// <paramref name="farPlaneDistance" /> is less than or equal to zero.
        /// -or-
        /// <paramref name="nearPlaneDistance" /> is greater than or equal to <paramref name="farPlaneDistance" />.</exception>
        public static Matrix4x4 CreatePerspectiveFieldOfView(float fieldOfView, float aspectRatio, float nearPlaneDistance, float farPlaneDistance)
            => Impl.CreatePerspectiveFieldOfView(fieldOfView, aspectRatio, nearPlaneDistance, farPlaneDistance).AsM4x4();

        /// <summary>Creates a left-handed perspective projection matrix based on a field of view, aspect ratio, and near and far view plane distances.</summary>
        /// <param name="fieldOfView">The field of view in the y direction, in radians.</param>
        /// <param name="aspectRatio">The aspect ratio, defined as view space width divided by height.</param>
        /// <param name="nearPlaneDistance">The distance to the near view plane.</param>
        /// <param name="farPlaneDistance">The distance to the far view plane.</param>
        /// <returns>The left-handed perspective projection matrix.</returns>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="fieldOfView" /> is less than or equal to zero.
        /// -or-
        /// <paramref name="fieldOfView" /> is greater than or equal to <see cref="float.Pi" />.
        /// <paramref name="nearPlaneDistance" /> is less than or equal to zero.
        /// -or-
        /// <paramref name="farPlaneDistance" /> is less than or equal to zero.
        /// -or-
        /// <paramref name="nearPlaneDistance" /> is greater than or equal to <paramref name="farPlaneDistance" />.</exception>
        public static Matrix4x4 CreatePerspectiveFieldOfViewLeftHanded(float fieldOfView, float aspectRatio, float nearPlaneDistance, float farPlaneDistance)
            => Impl.CreatePerspectiveFieldOfViewLeftHanded(fieldOfView, aspectRatio, nearPlaneDistance, farPlaneDistance).AsM4x4();

        /// <summary>Creates a right-handed customized perspective projection matrix.</summary>
        /// <param name="left">The minimum x-value of the view volume at the near view plane.</param>
        /// <param name="right">The maximum x-value of the view volume at the near view plane.</param>
        /// <param name="bottom">The minimum y-value of the view volume at the near view plane.</param>
        /// <param name="top">The maximum y-value of the view volume at the near view plane.</param>
        /// <param name="nearPlaneDistance">The distance to the near view plane.</param>
        /// <param name="farPlaneDistance">The distance to the far view plane.</param>
        /// <returns>The right-handed perspective projection matrix.</returns>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="nearPlaneDistance" /> is less than or equal to zero.
        /// -or-
        /// <paramref name="farPlaneDistance" /> is less than or equal to zero.
        /// -or-
        /// <paramref name="nearPlaneDistance" /> is greater than or equal to <paramref name="farPlaneDistance" />.</exception>
        public static Matrix4x4 CreatePerspectiveOffCenter(float left, float right, float bottom, float top, float nearPlaneDistance, float farPlaneDistance)
            => Impl.CreatePerspectiveOffCenter(left, right, bottom, top, nearPlaneDistance, farPlaneDistance).AsM4x4();

        /// <summary>Creates a left-handed customized perspective projection matrix.</summary>
        /// <param name="left">The minimum x-value of the view volume at the near view plane.</param>
        /// <param name="right">The maximum x-value of the view volume at the near view plane.</param>
        /// <param name="bottom">The minimum y-value of the view volume at the near view plane.</param>
        /// <param name="top">The maximum y-value of the view volume at the near view plane.</param>
        /// <param name="nearPlaneDistance">The distance to the near view plane.</param>
        /// <param name="farPlaneDistance">The distance to the far view plane.</param>
        /// <returns>The left-handed perspective projection matrix.</returns>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="nearPlaneDistance" /> is less than or equal to zero.
        /// -or-
        /// <paramref name="farPlaneDistance" /> is less than or equal to zero.
        /// -or-
        /// <paramref name="nearPlaneDistance" /> is greater than or equal to <paramref name="farPlaneDistance" />.</exception>
        public static Matrix4x4 CreatePerspectiveOffCenterLeftHanded(float left, float right, float bottom, float top, float nearPlaneDistance, float farPlaneDistance)
            => Impl.CreatePerspectiveOffCenterLeftHanded(left, right, bottom, top, nearPlaneDistance, farPlaneDistance).AsM4x4();

        /// <summary>Creates a matrix that reflects the coordinate system about a specified plane.</summary>
        /// <param name="value">The plane about which to create a reflection.</param>
        /// <returns>A new matrix expressing the reflection.</returns>
        public static Matrix4x4 CreateReflection(Plane value)
            => Impl.CreateReflection(in value).AsM4x4();

        /// <summary>Creates a matrix for rotating points around the X axis.</summary>
        /// <param name="radians">The amount, in radians, by which to rotate around the X axis.</param>
        /// <returns>The rotation matrix.</returns>
        public static Matrix4x4 CreateRotationX(float radians)
            => Impl.CreateRotationX(radians).AsM4x4();

        /// <summary>Creates a matrix for rotating points around the X axis from a center point.</summary>
        /// <param name="radians">The amount, in radians, by which to rotate around the X axis.</param>
        /// <param name="centerPoint">The center point.</param>
        /// <returns>The rotation matrix.</returns>
        public static Matrix4x4 CreateRotationX(float radians, Vector3 centerPoint)
            => Impl.CreateRotationX(radians, in centerPoint).AsM4x4();

        /// <summary>Creates a matrix for rotating points around the Y axis.</summary>
        /// <param name="radians">The amount, in radians, by which to rotate around the Y-axis.</param>
        /// <returns>The rotation matrix.</returns>
        public static Matrix4x4 CreateRotationY(float radians)
            => Impl.CreateRotationY(radians).AsM4x4();

        /// <summary>The amount, in radians, by which to rotate around the Y axis from a center point.</summary>
        /// <param name="radians">The amount, in radians, by which to rotate around the Y-axis.</param>
        /// <param name="centerPoint">The center point.</param>
        /// <returns>The rotation matrix.</returns>
        public static Matrix4x4 CreateRotationY(float radians, Vector3 centerPoint)
            => Impl.CreateRotationY(radians, in centerPoint).AsM4x4();

        /// <summary>Creates a matrix for rotating points around the Z axis.</summary>
        /// <param name="radians">The amount, in radians, by which to rotate around the Z-axis.</param>
        /// <returns>The rotation matrix.</returns>
        public static Matrix4x4 CreateRotationZ(float radians)
            => Impl.CreateRotationZ(radians).AsM4x4();

        /// <summary>Creates a matrix for rotating points around the Z axis from a center point.</summary>
        /// <param name="radians">The amount, in radians, by which to rotate around the Z-axis.</param>
        /// <param name="centerPoint">The center point.</param>
        /// <returns>The rotation matrix.</returns>
        public static Matrix4x4 CreateRotationZ(float radians, Vector3 centerPoint)
            => Impl.CreateRotationZ(radians, in centerPoint).AsM4x4();

        /// <summary>Creates a scaling matrix from the specified X, Y, and Z components.</summary>
        /// <param name="xScale">The value to scale by on the X axis.</param>
        /// <param name="yScale">The value to scale by on the Y axis.</param>
        /// <param name="zScale">The value to scale by on the Z axis.</param>
        /// <returns>The scaling matrix.</returns>
        public static Matrix4x4 CreateScale(float xScale, float yScale, float zScale)
            => Impl.CreateScale(xScale, yScale, zScale).AsM4x4();

        /// <summary>Creates a scaling matrix that is offset by a given center point.</summary>
        /// <param name="xScale">The value to scale by on the X axis.</param>
        /// <param name="yScale">The value to scale by on the Y axis.</param>
        /// <param name="zScale">The value to scale by on the Z axis.</param>
        /// <param name="centerPoint">The center point.</param>
        /// <returns>The scaling matrix.</returns>
        public static Matrix4x4 CreateScale(float xScale, float yScale, float zScale, Vector3 centerPoint)
            => Impl.CreateScale(xScale, yScale, zScale, in centerPoint).AsM4x4();

        /// <summary>Creates a scaling matrix from the specified vector scale.</summary>
        /// <param name="scales">The scale to use.</param>
        /// <returns>The scaling matrix.</returns>
        public static Matrix4x4 CreateScale(Vector3 scales)
            => Impl.CreateScale(in scales).AsM4x4();

        /// <summary>Creates a scaling matrix with a center point.</summary>
        /// <param name="scales">The vector that contains the amount to scale on each axis.</param>
        /// <param name="centerPoint">The center point.</param>
        /// <returns>The scaling matrix.</returns>
        public static Matrix4x4 CreateScale(Vector3 scales, Vector3 centerPoint)
            => Impl.CreateScale(scales, in centerPoint).AsM4x4();

        /// <summary>Creates a uniform scaling matrix that scale equally on each axis.</summary>
        /// <param name="scale">The uniform scaling factor.</param>
        /// <returns>The scaling matrix.</returns>
        public static Matrix4x4 CreateScale(float scale)
            => Impl.CreateScale(scale).AsM4x4();

        /// <summary>Creates a uniform scaling matrix that scales equally on each axis with a center point.</summary>
        /// <param name="scale">The uniform scaling factor.</param>
        /// <param name="centerPoint">The center point.</param>
        /// <returns>The scaling matrix.</returns>
        public static Matrix4x4 CreateScale(float scale, Vector3 centerPoint)
            => Impl.CreateScale(scale, in centerPoint).AsM4x4();

        /// <summary>Creates a matrix that flattens geometry into a specified plane as if casting a shadow from a specified light source.</summary>
        /// <param name="lightDirection">The direction from which the light that will cast the shadow is coming.</param>
        /// <param name="plane">The plane onto which the new matrix should flatten geometry so as to cast a shadow.</param>
        /// <returns>A new matrix that can be used to flatten geometry onto the specified plane from the specified direction.</returns>
        public static Matrix4x4 CreateShadow(Vector3 lightDirection, Plane plane)
            => Impl.CreateShadow(in lightDirection, in plane).AsM4x4();

        /// <summary>Creates a translation matrix from the specified 3-dimensional vector.</summary>
        /// <param name="position">The amount to translate in each axis.</param>
        /// <returns>The translation matrix.</returns>
        public static Matrix4x4 CreateTranslation(Vector3 position)
            => Impl.CreateTranslation(in position).AsM4x4();

        /// <summary>Creates a translation matrix from the specified X, Y, and Z components.</summary>
        /// <param name="xPosition">The amount to translate on the X axis.</param>
        /// <param name="yPosition">The amount to translate on the Y axis.</param>
        /// <param name="zPosition">The amount to translate on the Z axis.</param>
        /// <returns>The translation matrix.</returns>
        public static Matrix4x4 CreateTranslation(float xPosition, float yPosition, float zPosition)
            => Impl.CreateTranslation(xPosition, yPosition, zPosition).AsM4x4();

        /// <summary>Creates a right-handed viewport matrix from the specified parameters.</summary>
        /// <param name="x">X coordinate of the viewport upper left corner.</param>
        /// <param name="y">Y coordinate of the viewport upper left corner.</param>
        /// <param name="width">Viewport width.</param>
        /// <param name="height">Viewport height.</param>
        /// <param name="minDepth">Viewport minimum depth.</param>
        /// <param name="maxDepth">Viewport maximum depth.</param>
        /// <returns>The right-handed viewport matrix.</returns>
        /// <remarks>
        /// Viewport matrix
        /// |   width / 2   |        0       |          0          | 0 |
        /// |       0       |   -height / 2  |          0          | 0 |
        /// |       0       |        0       | minDepth - maxDepth | 0 |
        /// | x + width / 2 | y + height / 2 |       minDepth      | 1 |
        /// </remarks>
        public static Matrix4x4 CreateViewport(float x, float y, float width, float height, float minDepth, float maxDepth)
            => Impl.CreateViewport(x, y, width, height, minDepth, maxDepth).AsM4x4();

        /// <summary>Creates a left-handed viewport matrix from the specified parameters.</summary>
        /// <param name="x">X coordinate of the viewport upper left corner.</param>
        /// <param name="y">Y coordinate of the viewport upper left corner.</param>
        /// <param name="width">Viewport width.</param>
        /// <param name="height">Viewport height.</param>
        /// <param name="minDepth">Viewport minimum depth.</param>
        /// <param name="maxDepth">Viewport maximum depth.</param>
        /// <returns>The left-handed viewport matrix.</returns>
        /// <remarks>
        /// Viewport matrix
        /// |   width / 2   |        0       |          0          | 0 |
        /// |       0       |   -height / 2  |          0          | 0 |
        /// |       0       |        0       | maxDepth - minDepth | 0 |
        /// | x + width / 2 | y + height / 2 |       minDepth      | 1 |
        /// </remarks>
        public static Matrix4x4 CreateViewportLeftHanded(float x, float y, float width, float height, float minDepth, float maxDepth)
            => Impl.CreateViewportLeftHanded(x, y, width, height, minDepth, maxDepth).AsM4x4();

        /// <summary>Creates a world matrix with the specified parameters.</summary>
        /// <param name="position">The position of the object.</param>
        /// <param name="forward">The forward direction of the object.</param>
        /// <param name="up">The upward direction of the object. Its value is usually <c>[0, 1, 0]</c>.</param>
        /// <returns>The world matrix.</returns>
        /// <remarks><paramref name="position" /> is used in translation operations.</remarks>
        public static Matrix4x4 CreateWorld(Vector3 position, Vector3 forward, Vector3 up)
            => Impl.CreateWorld(in position, in forward, in up).AsM4x4();

        /// <summary>Attempts to extract the scale, translation, and rotation components from the given scale, rotation, or translation matrix. The return value indicates whether the operation succeeded.</summary>
        /// <param name="matrix">The source matrix.</param>
        /// <param name="scale">When this method returns, contains the scaling component of the transformation matrix if the operation succeeded.</param>
        /// <param name="rotation">When this method returns, contains the rotation component of the transformation matrix if the operation succeeded.</param>
        /// <param name="translation">When the method returns, contains the translation component of the transformation matrix if the operation succeeded.</param>
        /// <returns><see langword="true" /> if <paramref name="matrix" /> was decomposed successfully; otherwise,  <see langword="false" />.</returns>
        public static unsafe bool Decompose(Matrix4x4 matrix, out Vector3 scale, out Quaternion rotation, out Vector3 translation)
            => Impl.Decompose(in matrix.AsImpl(), out scale, out rotation, out translation);

        /// <summary>Tries to invert the specified matrix. The return value indicates whether the operation succeeded.</summary>
        /// <param name="matrix">The matrix to invert.</param>
        /// <param name="result">When this method returns, contains the inverted matrix if the operation succeeded.</param>
        /// <returns><see langword="true" /> if <paramref name="matrix" /> was converted successfully; otherwise,  <see langword="false" />.</returns>
        public static bool Invert(Matrix4x4 matrix, out Matrix4x4 result)
        {
            Unsafe.SkipInit(out result);
            return Impl.Invert(in matrix.AsImpl(), out result.AsImpl());
        }

        /// <summary>Performs a linear interpolation from one matrix to a second matrix based on a value that specifies the weighting of the second matrix.</summary>
        /// <param name="matrix1">The first matrix.</param>
        /// <param name="matrix2">The second matrix.</param>
        /// <param name="amount">The relative weighting of <paramref name="matrix2" />.</param>
        /// <returns>The interpolated matrix.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Matrix4x4 Lerp(Matrix4x4 matrix1, Matrix4x4 matrix2, float amount)
            => Impl.Lerp(in matrix1.AsImpl(), in matrix2.AsImpl(), amount).AsM4x4();

        /// <summary>Multiplies two matrices together to compute the product.</summary>
        /// <param name="value1">The first matrix.</param>
        /// <param name="value2">The second matrix.</param>
        /// <returns>The product matrix.</returns>
        public static Matrix4x4 Multiply(Matrix4x4 value1, Matrix4x4 value2)
            => (value1.AsImpl() * value2.AsImpl()).AsM4x4();

        /// <summary>Multiplies a matrix by a float to compute the product.</summary>
        /// <param name="value1">The matrix to scale.</param>
        /// <param name="value2">The scaling value to use.</param>
        /// <returns>The scaled matrix.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Matrix4x4 Multiply(Matrix4x4 value1, float value2)
            => (value1.AsImpl() * value2).AsM4x4();

        /// <summary>Negates the specified matrix by multiplying all its values by -1.</summary>
        /// <param name="value">The matrix to negate.</param>
        /// <returns>The negated matrix.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Matrix4x4 Negate(Matrix4x4 value)
            => (-value.AsImpl()).AsM4x4();

        /// <summary>Subtracts each element in a second matrix from its corresponding element in a first matrix.</summary>
        /// <param name="value1">The first matrix.</param>
        /// <param name="value2">The second matrix.</param>
        /// <returns>The matrix containing the values that result from subtracting each element in <paramref name="value2" /> from its corresponding element in <paramref name="value1" />.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Matrix4x4 Subtract(Matrix4x4 value1, Matrix4x4 value2)
            => (value1.AsImpl() - value2.AsImpl()).AsM4x4();

        /// <summary>Transforms the specified matrix by applying the specified Quaternion rotation.</summary>
        /// <param name="value">The matrix to transform.</param>
        /// <param name="rotation">The rotation t apply.</param>
        /// <returns>The transformed matrix.</returns>
        public static Matrix4x4 Transform(Matrix4x4 value, Quaternion rotation)
            => Impl.Transform(in value.AsImpl(), in rotation).AsM4x4();

        /// <summary>Transposes the rows and columns of a matrix.</summary>
        /// <param name="matrix">The matrix to transpose.</param>
        /// <returns>The transposed matrix.</returns>
        public static Matrix4x4 Transpose(Matrix4x4 matrix)
            => Impl.Transpose(in matrix.AsImpl()).AsM4x4();

        /// <summary>Returns a value that indicates whether this instance and a specified object are equal.</summary>
        /// <param name="obj">The object to compare with the current instance.</param>
        /// <returns><see langword="true" /> if the current instance and <paramref name="obj" /> are equal; otherwise, <see langword="false" />. If <paramref name="obj" /> is <see langword="null" />, the method returns <see langword="false" />.</returns>
        /// <remarks>The current instance and <paramref name="obj" /> are equal if <paramref name="obj" /> is a <see cref="Matrix4x4" /> object and the corresponding elements of each matrix are equal.</remarks>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override readonly bool Equals([NotNullWhen(true)] object? obj)
            => AsROImpl().Equals(obj);

        /// <summary>Returns a value that indicates whether this instance and another 4x4 matrix are equal.</summary>
        /// <param name="other">The other matrix.</param>
        /// <returns><see langword="true" /> if the two matrices are equal; otherwise, <see langword="false" />.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly bool Equals(Matrix4x4 other)
            => AsROImpl().Equals(in other.AsImpl());

        /// <summary>Calculates the determinant of the current 4x4 matrix.</summary>
        /// <returns>The determinant.</returns>
        public readonly float GetDeterminant()
            => AsROImpl().GetDeterminant();

        /// <summary>Returns the hash code for this instance.</summary>
        /// <returns>The hash code.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override readonly int GetHashCode()
            => AsROImpl().GetHashCode();

        /// <summary>Returns a string that represents this matrix.</summary>
        /// <returns>The string representation of this matrix.</returns>
        /// <remarks>The numeric values in the returned string are formatted by using the conventions of the current culture. For example, for the en-US culture, the returned string might appear as <c>{ {M11:1.1 M12:1.2 M13:1.3 M14:1.4} {M21:2.1 M22:2.2 M23:2.3 M24:2.4} {M31:3.1 M32:3.2 M33:3.3 M34:3.4} {M41:4.1 M42:4.2 M43:4.3 M44:4.4} }</c>.</remarks>
        public override readonly string ToString()
            => $"{{ {{M11:{M11} M12:{M12} M13:{M13} M14:{M14}}} {{M21:{M21} M22:{M22} M23:{M23} M24:{M24}}} {{M31:{M31} M32:{M32} M33:{M33} M34:{M34}}} {{M41:{M41} M42:{M42} M43:{M43} M44:{M44}}} }}";
    }
}
