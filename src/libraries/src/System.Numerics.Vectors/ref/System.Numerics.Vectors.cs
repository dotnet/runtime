// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
// ------------------------------------------------------------------------------
// Changes to this file must follow the http://aka.ms/api-review process.
// ------------------------------------------------------------------------------


namespace System.Numerics
{
    public partial struct Matrix3x2 : System.IEquatable<System.Numerics.Matrix3x2>
    {
        public float M11;
        public float M12;
        public float M21;
        public float M22;
        public float M31;
        public float M32;
        public Matrix3x2(float m11, float m12, float m21, float m22, float m31, float m32) { throw null; }
        public static System.Numerics.Matrix3x2 Identity { get { throw null; } }
        public bool IsIdentity { get { throw null; } }
        public System.Numerics.Vector2 Translation { get { throw null; } set { } }
        public static System.Numerics.Matrix3x2 Add(System.Numerics.Matrix3x2 value1, System.Numerics.Matrix3x2 value2) { throw null; }
        public static System.Numerics.Matrix3x2 CreateRotation(float radians) { throw null; }
        public static System.Numerics.Matrix3x2 CreateRotation(float radians, System.Numerics.Vector2 centerPoint) { throw null; }
        public static System.Numerics.Matrix3x2 CreateScale(System.Numerics.Vector2 scales) { throw null; }
        public static System.Numerics.Matrix3x2 CreateScale(System.Numerics.Vector2 scales, System.Numerics.Vector2 centerPoint) { throw null; }
        public static System.Numerics.Matrix3x2 CreateScale(float scale) { throw null; }
        public static System.Numerics.Matrix3x2 CreateScale(float scale, System.Numerics.Vector2 centerPoint) { throw null; }
        public static System.Numerics.Matrix3x2 CreateScale(float xScale, float yScale) { throw null; }
        public static System.Numerics.Matrix3x2 CreateScale(float xScale, float yScale, System.Numerics.Vector2 centerPoint) { throw null; }
        public static System.Numerics.Matrix3x2 CreateSkew(float radiansX, float radiansY) { throw null; }
        public static System.Numerics.Matrix3x2 CreateSkew(float radiansX, float radiansY, System.Numerics.Vector2 centerPoint) { throw null; }
        public static System.Numerics.Matrix3x2 CreateTranslation(System.Numerics.Vector2 position) { throw null; }
        public static System.Numerics.Matrix3x2 CreateTranslation(float xPosition, float yPosition) { throw null; }
        public bool Equals(System.Numerics.Matrix3x2 other) { throw null; }
        public override bool Equals(object obj) { throw null; }
        public float GetDeterminant() { throw null; }
        public override int GetHashCode() { throw null; }
        public static bool Invert(System.Numerics.Matrix3x2 matrix, out System.Numerics.Matrix3x2 result) { throw null; }
        public static System.Numerics.Matrix3x2 Lerp(System.Numerics.Matrix3x2 matrix1, System.Numerics.Matrix3x2 matrix2, float amount) { throw null; }
        public static System.Numerics.Matrix3x2 Multiply(System.Numerics.Matrix3x2 value1, System.Numerics.Matrix3x2 value2) { throw null; }
        public static System.Numerics.Matrix3x2 Multiply(System.Numerics.Matrix3x2 value1, float value2) { throw null; }
        public static System.Numerics.Matrix3x2 Negate(System.Numerics.Matrix3x2 value) { throw null; }
        public static System.Numerics.Matrix3x2 operator +(System.Numerics.Matrix3x2 value1, System.Numerics.Matrix3x2 value2) { throw null; }
        public static bool operator ==(System.Numerics.Matrix3x2 value1, System.Numerics.Matrix3x2 value2) { throw null; }
        public static bool operator !=(System.Numerics.Matrix3x2 value1, System.Numerics.Matrix3x2 value2) { throw null; }
        public static System.Numerics.Matrix3x2 operator *(System.Numerics.Matrix3x2 value1, System.Numerics.Matrix3x2 value2) { throw null; }
        public static System.Numerics.Matrix3x2 operator *(System.Numerics.Matrix3x2 value1, float value2) { throw null; }
        public static System.Numerics.Matrix3x2 operator -(System.Numerics.Matrix3x2 value1, System.Numerics.Matrix3x2 value2) { throw null; }
        public static System.Numerics.Matrix3x2 operator -(System.Numerics.Matrix3x2 value) { throw null; }
        public static System.Numerics.Matrix3x2 Subtract(System.Numerics.Matrix3x2 value1, System.Numerics.Matrix3x2 value2) { throw null; }
        public override string ToString() { throw null; }
    }
    public partial struct Matrix4x4 : System.IEquatable<System.Numerics.Matrix4x4>
    {
        public float M11;
        public float M12;
        public float M13;
        public float M14;
        public float M21;
        public float M22;
        public float M23;
        public float M24;
        public float M31;
        public float M32;
        public float M33;
        public float M34;
        public float M41;
        public float M42;
        public float M43;
        public float M44;
        public Matrix4x4(System.Numerics.Matrix3x2 value) { throw null; }
        public Matrix4x4(float m11, float m12, float m13, float m14, float m21, float m22, float m23, float m24, float m31, float m32, float m33, float m34, float m41, float m42, float m43, float m44) { throw null; }
        public static System.Numerics.Matrix4x4 Identity { get { throw null; } }
        public bool IsIdentity { get { throw null; } }
        public System.Numerics.Vector3 Translation { get { throw null; } set { } }
        public static System.Numerics.Matrix4x4 Add(System.Numerics.Matrix4x4 value1, System.Numerics.Matrix4x4 value2) { throw null; }
        public static System.Numerics.Matrix4x4 CreateBillboard(System.Numerics.Vector3 objectPosition, System.Numerics.Vector3 cameraPosition, System.Numerics.Vector3 cameraUpVector, System.Numerics.Vector3 cameraForwardVector) { throw null; }
        public static System.Numerics.Matrix4x4 CreateConstrainedBillboard(System.Numerics.Vector3 objectPosition, System.Numerics.Vector3 cameraPosition, System.Numerics.Vector3 rotateAxis, System.Numerics.Vector3 cameraForwardVector, System.Numerics.Vector3 objectForwardVector) { throw null; }
        public static System.Numerics.Matrix4x4 CreateFromAxisAngle(System.Numerics.Vector3 axis, float angle) { throw null; }
        public static System.Numerics.Matrix4x4 CreateFromQuaternion(System.Numerics.Quaternion quaternion) { throw null; }
        public static System.Numerics.Matrix4x4 CreateFromYawPitchRoll(float yaw, float pitch, float roll) { throw null; }
        public static System.Numerics.Matrix4x4 CreateLookAt(System.Numerics.Vector3 cameraPosition, System.Numerics.Vector3 cameraTarget, System.Numerics.Vector3 cameraUpVector) { throw null; }
        public static System.Numerics.Matrix4x4 CreateOrthographic(float width, float height, float zNearPlane, float zFarPlane) { throw null; }
        public static System.Numerics.Matrix4x4 CreateOrthographicOffCenter(float left, float right, float bottom, float top, float zNearPlane, float zFarPlane) { throw null; }
        public static System.Numerics.Matrix4x4 CreatePerspective(float width, float height, float nearPlaneDistance, float farPlaneDistance) { throw null; }
        public static System.Numerics.Matrix4x4 CreatePerspectiveFieldOfView(float fieldOfView, float aspectRatio, float nearPlaneDistance, float farPlaneDistance) { throw null; }
        public static System.Numerics.Matrix4x4 CreatePerspectiveOffCenter(float left, float right, float bottom, float top, float nearPlaneDistance, float farPlaneDistance) { throw null; }
        public static System.Numerics.Matrix4x4 CreateReflection(System.Numerics.Plane value) { throw null; }
        public static System.Numerics.Matrix4x4 CreateRotationX(float radians) { throw null; }
        public static System.Numerics.Matrix4x4 CreateRotationX(float radians, System.Numerics.Vector3 centerPoint) { throw null; }
        public static System.Numerics.Matrix4x4 CreateRotationY(float radians) { throw null; }
        public static System.Numerics.Matrix4x4 CreateRotationY(float radians, System.Numerics.Vector3 centerPoint) { throw null; }
        public static System.Numerics.Matrix4x4 CreateRotationZ(float radians) { throw null; }
        public static System.Numerics.Matrix4x4 CreateRotationZ(float radians, System.Numerics.Vector3 centerPoint) { throw null; }
        public static System.Numerics.Matrix4x4 CreateScale(System.Numerics.Vector3 scales) { throw null; }
        public static System.Numerics.Matrix4x4 CreateScale(System.Numerics.Vector3 scales, System.Numerics.Vector3 centerPoint) { throw null; }
        public static System.Numerics.Matrix4x4 CreateScale(float scale) { throw null; }
        public static System.Numerics.Matrix4x4 CreateScale(float scale, System.Numerics.Vector3 centerPoint) { throw null; }
        public static System.Numerics.Matrix4x4 CreateScale(float xScale, float yScale, float zScale) { throw null; }
        public static System.Numerics.Matrix4x4 CreateScale(float xScale, float yScale, float zScale, System.Numerics.Vector3 centerPoint) { throw null; }
        public static System.Numerics.Matrix4x4 CreateShadow(System.Numerics.Vector3 lightDirection, System.Numerics.Plane plane) { throw null; }
        public static System.Numerics.Matrix4x4 CreateTranslation(System.Numerics.Vector3 position) { throw null; }
        public static System.Numerics.Matrix4x4 CreateTranslation(float xPosition, float yPosition, float zPosition) { throw null; }
        public static System.Numerics.Matrix4x4 CreateWorld(System.Numerics.Vector3 position, System.Numerics.Vector3 forward, System.Numerics.Vector3 up) { throw null; }
        public static bool Decompose(System.Numerics.Matrix4x4 matrix, out System.Numerics.Vector3 scale, out System.Numerics.Quaternion rotation, out System.Numerics.Vector3 translation) { throw null; }
        public bool Equals(System.Numerics.Matrix4x4 other) { throw null; }
        public override bool Equals(object obj) { throw null; }
        public float GetDeterminant() { throw null; }
        public override int GetHashCode() { throw null; }
        public static bool Invert(System.Numerics.Matrix4x4 matrix, out System.Numerics.Matrix4x4 result) { throw null; }
        public static System.Numerics.Matrix4x4 Lerp(System.Numerics.Matrix4x4 matrix1, System.Numerics.Matrix4x4 matrix2, float amount) { throw null; }
        public static System.Numerics.Matrix4x4 Multiply(System.Numerics.Matrix4x4 value1, System.Numerics.Matrix4x4 value2) { throw null; }
        public static System.Numerics.Matrix4x4 Multiply(System.Numerics.Matrix4x4 value1, float value2) { throw null; }
        public static System.Numerics.Matrix4x4 Negate(System.Numerics.Matrix4x4 value) { throw null; }
        public static System.Numerics.Matrix4x4 operator +(System.Numerics.Matrix4x4 value1, System.Numerics.Matrix4x4 value2) { throw null; }
        public static bool operator ==(System.Numerics.Matrix4x4 value1, System.Numerics.Matrix4x4 value2) { throw null; }
        public static bool operator !=(System.Numerics.Matrix4x4 value1, System.Numerics.Matrix4x4 value2) { throw null; }
        public static System.Numerics.Matrix4x4 operator *(System.Numerics.Matrix4x4 value1, System.Numerics.Matrix4x4 value2) { throw null; }
        public static System.Numerics.Matrix4x4 operator *(System.Numerics.Matrix4x4 value1, float value2) { throw null; }
        public static System.Numerics.Matrix4x4 operator -(System.Numerics.Matrix4x4 value1, System.Numerics.Matrix4x4 value2) { throw null; }
        public static System.Numerics.Matrix4x4 operator -(System.Numerics.Matrix4x4 value) { throw null; }
        public static System.Numerics.Matrix4x4 Subtract(System.Numerics.Matrix4x4 value1, System.Numerics.Matrix4x4 value2) { throw null; }
        public override string ToString() { throw null; }
        public static System.Numerics.Matrix4x4 Transform(System.Numerics.Matrix4x4 value, System.Numerics.Quaternion rotation) { throw null; }
        public static System.Numerics.Matrix4x4 Transpose(System.Numerics.Matrix4x4 matrix) { throw null; }
    }
    public partial struct Plane : System.IEquatable<System.Numerics.Plane>
    {
        public float D;
        public System.Numerics.Vector3 Normal;
        public Plane(System.Numerics.Vector3 normal, float d) { throw null; }
        public Plane(System.Numerics.Vector4 value) { throw null; }
        public Plane(float x, float y, float z, float d) { throw null; }
        public static System.Numerics.Plane CreateFromVertices(System.Numerics.Vector3 point1, System.Numerics.Vector3 point2, System.Numerics.Vector3 point3) { throw null; }
        public static float Dot(System.Numerics.Plane plane, System.Numerics.Vector4 value) { throw null; }
        public static float DotCoordinate(System.Numerics.Plane plane, System.Numerics.Vector3 value) { throw null; }
        public static float DotNormal(System.Numerics.Plane plane, System.Numerics.Vector3 value) { throw null; }
        public bool Equals(System.Numerics.Plane other) { throw null; }
        public override bool Equals(object obj) { throw null; }
        public override int GetHashCode() { throw null; }
        public static System.Numerics.Plane Normalize(System.Numerics.Plane value) { throw null; }
        public static bool operator ==(System.Numerics.Plane value1, System.Numerics.Plane value2) { throw null; }
        public static bool operator !=(System.Numerics.Plane value1, System.Numerics.Plane value2) { throw null; }
        public override string ToString() { throw null; }
        public static System.Numerics.Plane Transform(System.Numerics.Plane plane, System.Numerics.Matrix4x4 matrix) { throw null; }
        public static System.Numerics.Plane Transform(System.Numerics.Plane plane, System.Numerics.Quaternion rotation) { throw null; }
    }
    public partial struct Quaternion : System.IEquatable<System.Numerics.Quaternion>
    {
        public float W;
        public float X;
        public float Y;
        public float Z;
        public Quaternion(System.Numerics.Vector3 vectorPart, float scalarPart) { throw null; }
        public Quaternion(float x, float y, float z, float w) { throw null; }
        public static System.Numerics.Quaternion Identity { get { throw null; } }
        public bool IsIdentity { get { throw null; } }
        public static System.Numerics.Quaternion Add(System.Numerics.Quaternion value1, System.Numerics.Quaternion value2) { throw null; }
        public static System.Numerics.Quaternion Concatenate(System.Numerics.Quaternion value1, System.Numerics.Quaternion value2) { throw null; }
        public static System.Numerics.Quaternion Conjugate(System.Numerics.Quaternion value) { throw null; }
        public static System.Numerics.Quaternion CreateFromAxisAngle(System.Numerics.Vector3 axis, float angle) { throw null; }
        public static System.Numerics.Quaternion CreateFromRotationMatrix(System.Numerics.Matrix4x4 matrix) { throw null; }
        public static System.Numerics.Quaternion CreateFromYawPitchRoll(float yaw, float pitch, float roll) { throw null; }
        public static System.Numerics.Quaternion Divide(System.Numerics.Quaternion value1, System.Numerics.Quaternion value2) { throw null; }
        public static float Dot(System.Numerics.Quaternion quaternion1, System.Numerics.Quaternion quaternion2) { throw null; }
        public bool Equals(System.Numerics.Quaternion other) { throw null; }
        public override bool Equals(object obj) { throw null; }
        public override int GetHashCode() { throw null; }
        public static System.Numerics.Quaternion Inverse(System.Numerics.Quaternion value) { throw null; }
        public float Length() { throw null; }
        public float LengthSquared() { throw null; }
        public static System.Numerics.Quaternion Lerp(System.Numerics.Quaternion quaternion1, System.Numerics.Quaternion quaternion2, float amount) { throw null; }
        public static System.Numerics.Quaternion Multiply(System.Numerics.Quaternion value1, System.Numerics.Quaternion value2) { throw null; }
        public static System.Numerics.Quaternion Multiply(System.Numerics.Quaternion value1, float value2) { throw null; }
        public static System.Numerics.Quaternion Negate(System.Numerics.Quaternion value) { throw null; }
        public static System.Numerics.Quaternion Normalize(System.Numerics.Quaternion value) { throw null; }
        public static System.Numerics.Quaternion operator +(System.Numerics.Quaternion value1, System.Numerics.Quaternion value2) { throw null; }
        public static System.Numerics.Quaternion operator /(System.Numerics.Quaternion value1, System.Numerics.Quaternion value2) { throw null; }
        public static bool operator ==(System.Numerics.Quaternion value1, System.Numerics.Quaternion value2) { throw null; }
        public static bool operator !=(System.Numerics.Quaternion value1, System.Numerics.Quaternion value2) { throw null; }
        public static System.Numerics.Quaternion operator *(System.Numerics.Quaternion value1, System.Numerics.Quaternion value2) { throw null; }
        public static System.Numerics.Quaternion operator *(System.Numerics.Quaternion value1, float value2) { throw null; }
        public static System.Numerics.Quaternion operator -(System.Numerics.Quaternion value1, System.Numerics.Quaternion value2) { throw null; }
        public static System.Numerics.Quaternion operator -(System.Numerics.Quaternion value) { throw null; }
        public static System.Numerics.Quaternion Slerp(System.Numerics.Quaternion quaternion1, System.Numerics.Quaternion quaternion2, float amount) { throw null; }
        public static System.Numerics.Quaternion Subtract(System.Numerics.Quaternion value1, System.Numerics.Quaternion value2) { throw null; }
        public override string ToString() { throw null; }
    }
    public static partial class Vector
    {
        public static bool IsHardwareAccelerated { get { throw null; } }
        public static System.Numerics.Vector<T> Abs<T>(System.Numerics.Vector<T> value) where T : struct { throw null; }
        public static System.Numerics.Vector<T> Add<T>(System.Numerics.Vector<T> left, System.Numerics.Vector<T> right) where T : struct { throw null; }
        public static System.Numerics.Vector<T> AndNot<T>(System.Numerics.Vector<T> left, System.Numerics.Vector<T> right) where T : struct { throw null; }
        public static System.Numerics.Vector<byte> AsVectorByte<T>(System.Numerics.Vector<T> value) where T : struct { throw null; }
        public static System.Numerics.Vector<double> AsVectorDouble<T>(System.Numerics.Vector<T> value) where T : struct { throw null; }
        public static System.Numerics.Vector<short> AsVectorInt16<T>(System.Numerics.Vector<T> value) where T : struct { throw null; }
        public static System.Numerics.Vector<int> AsVectorInt32<T>(System.Numerics.Vector<T> value) where T : struct { throw null; }
        public static System.Numerics.Vector<long> AsVectorInt64<T>(System.Numerics.Vector<T> value) where T : struct { throw null; }
        [System.CLSCompliantAttribute(false)]
        public static System.Numerics.Vector<sbyte> AsVectorSByte<T>(System.Numerics.Vector<T> value) where T : struct { throw null; }
        public static System.Numerics.Vector<float> AsVectorSingle<T>(System.Numerics.Vector<T> value) where T : struct { throw null; }
        [System.CLSCompliantAttribute(false)]
        public static System.Numerics.Vector<ushort> AsVectorUInt16<T>(System.Numerics.Vector<T> value) where T : struct { throw null; }
        [System.CLSCompliantAttribute(false)]
        public static System.Numerics.Vector<uint> AsVectorUInt32<T>(System.Numerics.Vector<T> value) where T : struct { throw null; }
        [System.CLSCompliantAttribute(false)]
        public static System.Numerics.Vector<ulong> AsVectorUInt64<T>(System.Numerics.Vector<T> value) where T : struct { throw null; }
        public static System.Numerics.Vector<T> BitwiseAnd<T>(System.Numerics.Vector<T> left, System.Numerics.Vector<T> right) where T : struct { throw null; }
        public static System.Numerics.Vector<T> BitwiseOr<T>(System.Numerics.Vector<T> left, System.Numerics.Vector<T> right) where T : struct { throw null; }
        public static System.Numerics.Vector<float> ConditionalSelect(System.Numerics.Vector<int> condition, System.Numerics.Vector<float> left, System.Numerics.Vector<float> right) { throw null; }
        public static System.Numerics.Vector<double> ConditionalSelect(System.Numerics.Vector<long> condition, System.Numerics.Vector<double> left, System.Numerics.Vector<double> right) { throw null; }
        public static System.Numerics.Vector<T> ConditionalSelect<T>(System.Numerics.Vector<T> condition, System.Numerics.Vector<T> left, System.Numerics.Vector<T> right) where T : struct { throw null; }
        public static System.Numerics.Vector<float> ConvertToSingle(System.Numerics.Vector<int> value) { throw null; }
        [System.CLSCompliant(false)]
        public static System.Numerics.Vector<float> ConvertToSingle(System.Numerics.Vector<uint> value) { throw null; }
        public static System.Numerics.Vector<int> ConvertToInt32(System.Numerics.Vector<float> value) { throw null; }
        [System.CLSCompliant(false)]
        public static System.Numerics.Vector<uint> ConvertToUInt32(System.Numerics.Vector<float> value) { throw null; }
        public static System.Numerics.Vector<double> ConvertToDouble(System.Numerics.Vector<long> value) { throw null; }
        [System.CLSCompliant(false)]
        public static System.Numerics.Vector<double> ConvertToDouble(System.Numerics.Vector<ulong> value) { throw null; }
        public static System.Numerics.Vector<long> ConvertToInt64(System.Numerics.Vector<double> value) { throw null; }
        [System.CLSCompliant(false)]
        public static System.Numerics.Vector<ulong> ConvertToUInt64(System.Numerics.Vector<double> value) { throw null; }
        public static System.Numerics.Vector<T> Divide<T>(System.Numerics.Vector<T> left, System.Numerics.Vector<T> right) where T : struct { throw null; }
        public static T Dot<T>(System.Numerics.Vector<T> left, System.Numerics.Vector<T> right) where T : struct { throw null; }
        public static System.Numerics.Vector<long> Equals(System.Numerics.Vector<double> left, System.Numerics.Vector<double> right) { throw null; }
        public static System.Numerics.Vector<int> Equals(System.Numerics.Vector<int> left, System.Numerics.Vector<int> right) { throw null; }
        public static System.Numerics.Vector<long> Equals(System.Numerics.Vector<long> left, System.Numerics.Vector<long> right) { throw null; }
        public static System.Numerics.Vector<int> Equals(System.Numerics.Vector<float> left, System.Numerics.Vector<float> right) { throw null; }
        public static System.Numerics.Vector<T> Equals<T>(System.Numerics.Vector<T> left, System.Numerics.Vector<T> right) where T : struct { throw null; }
        public static bool EqualsAll<T>(System.Numerics.Vector<T> left, System.Numerics.Vector<T> right) where T : struct { throw null; }
        public static bool EqualsAny<T>(System.Numerics.Vector<T> left, System.Numerics.Vector<T> right) where T : struct { throw null; }
        public static System.Numerics.Vector<long> GreaterThan(System.Numerics.Vector<double> left, System.Numerics.Vector<double> right) { throw null; }
        public static System.Numerics.Vector<int> GreaterThan(System.Numerics.Vector<int> left, System.Numerics.Vector<int> right) { throw null; }
        public static System.Numerics.Vector<long> GreaterThan(System.Numerics.Vector<long> left, System.Numerics.Vector<long> right) { throw null; }
        public static System.Numerics.Vector<int> GreaterThan(System.Numerics.Vector<float> left, System.Numerics.Vector<float> right) { throw null; }
        public static System.Numerics.Vector<T> GreaterThan<T>(System.Numerics.Vector<T> left, System.Numerics.Vector<T> right) where T : struct { throw null; }
        public static bool GreaterThanAll<T>(System.Numerics.Vector<T> left, System.Numerics.Vector<T> right) where T : struct { throw null; }
        public static bool GreaterThanAny<T>(System.Numerics.Vector<T> left, System.Numerics.Vector<T> right) where T : struct { throw null; }
        public static System.Numerics.Vector<long> GreaterThanOrEqual(System.Numerics.Vector<double> left, System.Numerics.Vector<double> right) { throw null; }
        public static System.Numerics.Vector<int> GreaterThanOrEqual(System.Numerics.Vector<int> left, System.Numerics.Vector<int> right) { throw null; }
        public static System.Numerics.Vector<long> GreaterThanOrEqual(System.Numerics.Vector<long> left, System.Numerics.Vector<long> right) { throw null; }
        public static System.Numerics.Vector<int> GreaterThanOrEqual(System.Numerics.Vector<float> left, System.Numerics.Vector<float> right) { throw null; }
        public static System.Numerics.Vector<T> GreaterThanOrEqual<T>(System.Numerics.Vector<T> left, System.Numerics.Vector<T> right) where T : struct { throw null; }
        public static bool GreaterThanOrEqualAll<T>(System.Numerics.Vector<T> left, System.Numerics.Vector<T> right) where T : struct { throw null; }
        public static bool GreaterThanOrEqualAny<T>(System.Numerics.Vector<T> left, System.Numerics.Vector<T> right) where T : struct { throw null; }
        public static System.Numerics.Vector<long> LessThan(System.Numerics.Vector<double> left, System.Numerics.Vector<double> right) { throw null; }
        public static System.Numerics.Vector<int> LessThan(System.Numerics.Vector<int> left, System.Numerics.Vector<int> right) { throw null; }
        public static System.Numerics.Vector<long> LessThan(System.Numerics.Vector<long> left, System.Numerics.Vector<long> right) { throw null; }
        public static System.Numerics.Vector<int> LessThan(System.Numerics.Vector<float> left, System.Numerics.Vector<float> right) { throw null; }
        public static System.Numerics.Vector<T> LessThan<T>(System.Numerics.Vector<T> left, System.Numerics.Vector<T> right) where T : struct { throw null; }
        public static bool LessThanAll<T>(System.Numerics.Vector<T> left, System.Numerics.Vector<T> right) where T : struct { throw null; }
        public static bool LessThanAny<T>(System.Numerics.Vector<T> left, System.Numerics.Vector<T> right) where T : struct { throw null; }
        public static System.Numerics.Vector<long> LessThanOrEqual(System.Numerics.Vector<double> left, System.Numerics.Vector<double> right) { throw null; }
        public static System.Numerics.Vector<int> LessThanOrEqual(System.Numerics.Vector<int> left, System.Numerics.Vector<int> right) { throw null; }
        public static System.Numerics.Vector<long> LessThanOrEqual(System.Numerics.Vector<long> left, System.Numerics.Vector<long> right) { throw null; }
        public static System.Numerics.Vector<int> LessThanOrEqual(System.Numerics.Vector<float> left, System.Numerics.Vector<float> right) { throw null; }
        public static System.Numerics.Vector<T> LessThanOrEqual<T>(System.Numerics.Vector<T> left, System.Numerics.Vector<T> right) where T : struct { throw null; }
        public static bool LessThanOrEqualAll<T>(System.Numerics.Vector<T> left, System.Numerics.Vector<T> right) where T : struct { throw null; }
        public static bool LessThanOrEqualAny<T>(System.Numerics.Vector<T> left, System.Numerics.Vector<T> right) where T : struct { throw null; }
        public static System.Numerics.Vector<T> Max<T>(System.Numerics.Vector<T> left, System.Numerics.Vector<T> right) where T : struct { throw null; }
        public static System.Numerics.Vector<T> Min<T>(System.Numerics.Vector<T> left, System.Numerics.Vector<T> right) where T : struct { throw null; }
        public static System.Numerics.Vector<T> Multiply<T>(T left, System.Numerics.Vector<T> right) where T : struct { throw null; }
        public static System.Numerics.Vector<T> Multiply<T>(System.Numerics.Vector<T> left, T right) where T : struct { throw null; }
        public static System.Numerics.Vector<T> Multiply<T>(System.Numerics.Vector<T> left, System.Numerics.Vector<T> right) where T : struct { throw null; }
        [System.CLSCompliant(false)]
        public static System.Numerics.Vector<byte> Narrow(System.Numerics.Vector<ushort> source1, System.Numerics.Vector<ushort> source2) { throw null; }
        [System.CLSCompliant(false)]
        public static System.Numerics.Vector<ushort> Narrow(System.Numerics.Vector<uint> source1, System.Numerics.Vector<uint> source2) { throw null; }
        [System.CLSCompliant(false)]
        public static System.Numerics.Vector<uint> Narrow(System.Numerics.Vector<ulong> source1, System.Numerics.Vector<ulong> source2) { throw null; }
        [System.CLSCompliant(false)]
        public static System.Numerics.Vector<sbyte> Narrow(System.Numerics.Vector<short> source1, System.Numerics.Vector<short> source2) { throw null; }
        public static System.Numerics.Vector<short> Narrow(System.Numerics.Vector<int> source1, System.Numerics.Vector<int> source2) { throw null; }
        public static System.Numerics.Vector<int> Narrow(System.Numerics.Vector<long> source1, System.Numerics.Vector<long> source2) { throw null; }
        public static System.Numerics.Vector<float> Narrow(System.Numerics.Vector<double> source1, System.Numerics.Vector<double> source2) { throw null; }
        public static System.Numerics.Vector<T> Negate<T>(System.Numerics.Vector<T> value) where T : struct { throw null; }
        public static System.Numerics.Vector<T> OnesComplement<T>(System.Numerics.Vector<T> value) where T : struct { throw null; }
        public static System.Numerics.Vector<T> SquareRoot<T>(System.Numerics.Vector<T> value) where T : struct { throw null; }
        public static System.Numerics.Vector<T> Subtract<T>(System.Numerics.Vector<T> left, System.Numerics.Vector<T> right) where T : struct { throw null; }
        [System.CLSCompliant(false)]
        public static void Widen(System.Numerics.Vector<byte> source, out System.Numerics.Vector<ushort> dest1, out System.Numerics.Vector<ushort> dest2) { dest1 = default(System.Numerics.Vector<ushort>); dest2 = default(System.Numerics.Vector<ushort>); }
        [System.CLSCompliant(false)]
        public static void Widen(System.Numerics.Vector<ushort> source, out System.Numerics.Vector<uint> dest1, out System.Numerics.Vector<uint> dest2) { dest1 = default(System.Numerics.Vector<uint>); dest2 = default(System.Numerics.Vector<uint>); }
        [System.CLSCompliant(false)]
        public static void Widen(System.Numerics.Vector<uint> source, out System.Numerics.Vector<ulong> dest1, out System.Numerics.Vector<ulong> dest2) { dest1 = default(System.Numerics.Vector<ulong>); dest2 = default(System.Numerics.Vector<ulong>); }
        [System.CLSCompliant(false)]
        public static void Widen(System.Numerics.Vector<sbyte> source, out System.Numerics.Vector<short> dest1, out System.Numerics.Vector<short> dest2) { dest1 = default(System.Numerics.Vector<short>); dest2 = default(System.Numerics.Vector<short>); }
        public static void Widen(System.Numerics.Vector<short> source, out System.Numerics.Vector<int> dest1, out System.Numerics.Vector<int> dest2) { dest1 = default(System.Numerics.Vector<int>); dest2 = default(System.Numerics.Vector<int>); }
        public static void Widen(System.Numerics.Vector<int> source, out System.Numerics.Vector<long> dest1, out System.Numerics.Vector<long> dest2) { dest1 = default(System.Numerics.Vector<long>); dest2 = default(System.Numerics.Vector<long>); }
        public static void Widen(System.Numerics.Vector<float> source, out System.Numerics.Vector<double> dest1, out System.Numerics.Vector<double> dest2) { dest1 = default(System.Numerics.Vector<double>); dest2 = default(System.Numerics.Vector<double>); }
        public static System.Numerics.Vector<T> Xor<T>(System.Numerics.Vector<T> left, System.Numerics.Vector<T> right) where T : struct { throw null; }
    }
    public partial struct Vector<T> : System.IEquatable<System.Numerics.Vector<T>>, System.IFormattable where T : struct
    {
        private int _dummy;
        public Vector(T value) { throw null; }
        public Vector(T[] values) { throw null; }
        public Vector(T[] values, int index) { throw null; }
        public static int Count { get { throw null; } }
        public T this[int index] { get { throw null; } }
        public static System.Numerics.Vector<T> One { get { throw null; } }
        public static System.Numerics.Vector<T> Zero { get { throw null; } }
        public void CopyTo(T[] destination) { }
        public void CopyTo(T[] destination, int startIndex) { }
        public bool Equals(System.Numerics.Vector<T> other) { throw null; }
        public override bool Equals(object obj) { throw null; }
        public override int GetHashCode() { throw null; }
        public static System.Numerics.Vector<T> operator +(System.Numerics.Vector<T> left, System.Numerics.Vector<T> right) { throw null; }
        public static System.Numerics.Vector<T> operator &(System.Numerics.Vector<T> left, System.Numerics.Vector<T> right) { throw null; }
        public static System.Numerics.Vector<T> operator |(System.Numerics.Vector<T> left, System.Numerics.Vector<T> right) { throw null; }
        public static System.Numerics.Vector<T> operator /(System.Numerics.Vector<T> left, System.Numerics.Vector<T> right) { throw null; }
        public static bool operator ==(System.Numerics.Vector<T> left, System.Numerics.Vector<T> right) { throw null; }
        public static System.Numerics.Vector<T> operator ^(System.Numerics.Vector<T> left, System.Numerics.Vector<T> right) { throw null; }
        public static explicit operator System.Numerics.Vector<byte>(System.Numerics.Vector<T> value) { throw null; }
        public static explicit operator System.Numerics.Vector<double>(System.Numerics.Vector<T> value) { throw null; }
        public static explicit operator System.Numerics.Vector<short>(System.Numerics.Vector<T> value) { throw null; }
        public static explicit operator System.Numerics.Vector<int>(System.Numerics.Vector<T> value) { throw null; }
        public static explicit operator System.Numerics.Vector<long>(System.Numerics.Vector<T> value) { throw null; }
        [System.CLSCompliantAttribute(false)]
        public static explicit operator System.Numerics.Vector<sbyte>(System.Numerics.Vector<T> value) { throw null; }
        public static explicit operator System.Numerics.Vector<float>(System.Numerics.Vector<T> value) { throw null; }
        [System.CLSCompliantAttribute(false)]
        public static explicit operator System.Numerics.Vector<ushort>(System.Numerics.Vector<T> value) { throw null; }
        [System.CLSCompliantAttribute(false)]
        public static explicit operator System.Numerics.Vector<uint>(System.Numerics.Vector<T> value) { throw null; }
        [System.CLSCompliantAttribute(false)]
        public static explicit operator System.Numerics.Vector<ulong>(System.Numerics.Vector<T> value) { throw null; }
        public static bool operator !=(System.Numerics.Vector<T> left, System.Numerics.Vector<T> right) { throw null; }
        public static System.Numerics.Vector<T> operator *(T factor, System.Numerics.Vector<T> value) { throw null; }
        public static System.Numerics.Vector<T> operator *(System.Numerics.Vector<T> value, T factor) { throw null; }
        public static System.Numerics.Vector<T> operator *(System.Numerics.Vector<T> left, System.Numerics.Vector<T> right) { throw null; }
        public static System.Numerics.Vector<T> operator ~(System.Numerics.Vector<T> value) { throw null; }
        public static System.Numerics.Vector<T> operator -(System.Numerics.Vector<T> left, System.Numerics.Vector<T> right) { throw null; }
        public static System.Numerics.Vector<T> operator -(System.Numerics.Vector<T> value) { throw null; }
        public override string ToString() { throw null; }
        public string ToString(string format) { throw null; }
        public string ToString(string format, System.IFormatProvider formatProvider) { throw null; }
    }
    public partial struct Vector2 : System.IEquatable<System.Numerics.Vector2>, System.IFormattable
    {
        public float X;
        public float Y;
        public Vector2(float value) { throw null; }
        public Vector2(float x, float y) { throw null; }
        public static System.Numerics.Vector2 One { get { throw null; } }
        public static System.Numerics.Vector2 UnitX { get { throw null; } }
        public static System.Numerics.Vector2 UnitY { get { throw null; } }
        public static System.Numerics.Vector2 Zero { get { throw null; } }
        public static System.Numerics.Vector2 Abs(System.Numerics.Vector2 value) { throw null; }
        public static System.Numerics.Vector2 Add(System.Numerics.Vector2 left, System.Numerics.Vector2 right) { throw null; }
        public static System.Numerics.Vector2 Clamp(System.Numerics.Vector2 value1, System.Numerics.Vector2 min, System.Numerics.Vector2 max) { throw null; }
        public void CopyTo(float[] array) { }
        public void CopyTo(float[] array, int index) { }
        public static float Distance(System.Numerics.Vector2 value1, System.Numerics.Vector2 value2) { throw null; }
        public static float DistanceSquared(System.Numerics.Vector2 value1, System.Numerics.Vector2 value2) { throw null; }
        public static System.Numerics.Vector2 Divide(System.Numerics.Vector2 left, System.Numerics.Vector2 right) { throw null; }
        public static System.Numerics.Vector2 Divide(System.Numerics.Vector2 left, float divisor) { throw null; }
        public static float Dot(System.Numerics.Vector2 value1, System.Numerics.Vector2 value2) { throw null; }
        public bool Equals(System.Numerics.Vector2 other) { throw null; }
        public override bool Equals(object obj) { throw null; }
        public override int GetHashCode() { throw null; }
        public float Length() { throw null; }
        public float LengthSquared() { throw null; }
        public static System.Numerics.Vector2 Lerp(System.Numerics.Vector2 value1, System.Numerics.Vector2 value2, float amount) { throw null; }
        public static System.Numerics.Vector2 Max(System.Numerics.Vector2 value1, System.Numerics.Vector2 value2) { throw null; }
        public static System.Numerics.Vector2 Min(System.Numerics.Vector2 value1, System.Numerics.Vector2 value2) { throw null; }
        public static System.Numerics.Vector2 Multiply(System.Numerics.Vector2 left, System.Numerics.Vector2 right) { throw null; }
        public static System.Numerics.Vector2 Multiply(System.Numerics.Vector2 left, float right) { throw null; }
        public static System.Numerics.Vector2 Multiply(float left, System.Numerics.Vector2 right) { throw null; }
        public static System.Numerics.Vector2 Negate(System.Numerics.Vector2 value) { throw null; }
        public static System.Numerics.Vector2 Normalize(System.Numerics.Vector2 value) { throw null; }
        public static System.Numerics.Vector2 operator +(System.Numerics.Vector2 left, System.Numerics.Vector2 right) { throw null; }
        public static System.Numerics.Vector2 operator /(System.Numerics.Vector2 left, System.Numerics.Vector2 right) { throw null; }
        public static System.Numerics.Vector2 operator /(System.Numerics.Vector2 value1, float value2) { throw null; }
        public static bool operator ==(System.Numerics.Vector2 left, System.Numerics.Vector2 right) { throw null; }
        public static bool operator !=(System.Numerics.Vector2 left, System.Numerics.Vector2 right) { throw null; }
        public static System.Numerics.Vector2 operator *(System.Numerics.Vector2 left, System.Numerics.Vector2 right) { throw null; }
        public static System.Numerics.Vector2 operator *(System.Numerics.Vector2 left, float right) { throw null; }
        public static System.Numerics.Vector2 operator *(float left, System.Numerics.Vector2 right) { throw null; }
        public static System.Numerics.Vector2 operator -(System.Numerics.Vector2 left, System.Numerics.Vector2 right) { throw null; }
        public static System.Numerics.Vector2 operator -(System.Numerics.Vector2 value) { throw null; }
        public static System.Numerics.Vector2 Reflect(System.Numerics.Vector2 vector, System.Numerics.Vector2 normal) { throw null; }
        public static System.Numerics.Vector2 SquareRoot(System.Numerics.Vector2 value) { throw null; }
        public static System.Numerics.Vector2 Subtract(System.Numerics.Vector2 left, System.Numerics.Vector2 right) { throw null; }
        public override string ToString() { throw null; }
        public string ToString(string format) { throw null; }
        public string ToString(string format, System.IFormatProvider formatProvider) { throw null; }
        public static System.Numerics.Vector2 Transform(System.Numerics.Vector2 position, System.Numerics.Matrix3x2 matrix) { throw null; }
        public static System.Numerics.Vector2 Transform(System.Numerics.Vector2 position, System.Numerics.Matrix4x4 matrix) { throw null; }
        public static System.Numerics.Vector2 Transform(System.Numerics.Vector2 value, System.Numerics.Quaternion rotation) { throw null; }
        public static System.Numerics.Vector2 TransformNormal(System.Numerics.Vector2 normal, System.Numerics.Matrix3x2 matrix) { throw null; }
        public static System.Numerics.Vector2 TransformNormal(System.Numerics.Vector2 normal, System.Numerics.Matrix4x4 matrix) { throw null; }
    }
    public partial struct Vector3 : System.IEquatable<System.Numerics.Vector3>, System.IFormattable
    {
        public float X;
        public float Y;
        public float Z;
        public Vector3(System.Numerics.Vector2 value, float z) { throw null; }
        public Vector3(float value) { throw null; }
        public Vector3(float x, float y, float z) { throw null; }
        public static System.Numerics.Vector3 One { get { throw null; } }
        public static System.Numerics.Vector3 UnitX { get { throw null; } }
        public static System.Numerics.Vector3 UnitY { get { throw null; } }
        public static System.Numerics.Vector3 UnitZ { get { throw null; } }
        public static System.Numerics.Vector3 Zero { get { throw null; } }
        public static System.Numerics.Vector3 Abs(System.Numerics.Vector3 value) { throw null; }
        public static System.Numerics.Vector3 Add(System.Numerics.Vector3 left, System.Numerics.Vector3 right) { throw null; }
        public static System.Numerics.Vector3 Clamp(System.Numerics.Vector3 value1, System.Numerics.Vector3 min, System.Numerics.Vector3 max) { throw null; }
        public void CopyTo(float[] array) { }
        public void CopyTo(float[] array, int index) { }
        public static System.Numerics.Vector3 Cross(System.Numerics.Vector3 vector1, System.Numerics.Vector3 vector2) { throw null; }
        public static float Distance(System.Numerics.Vector3 value1, System.Numerics.Vector3 value2) { throw null; }
        public static float DistanceSquared(System.Numerics.Vector3 value1, System.Numerics.Vector3 value2) { throw null; }
        public static System.Numerics.Vector3 Divide(System.Numerics.Vector3 left, System.Numerics.Vector3 right) { throw null; }
        public static System.Numerics.Vector3 Divide(System.Numerics.Vector3 left, float divisor) { throw null; }
        public static float Dot(System.Numerics.Vector3 vector1, System.Numerics.Vector3 vector2) { throw null; }
        public bool Equals(System.Numerics.Vector3 other) { throw null; }
        public override bool Equals(object obj) { throw null; }
        public override int GetHashCode() { throw null; }
        public float Length() { throw null; }
        public float LengthSquared() { throw null; }
        public static System.Numerics.Vector3 Lerp(System.Numerics.Vector3 value1, System.Numerics.Vector3 value2, float amount) { throw null; }
        public static System.Numerics.Vector3 Max(System.Numerics.Vector3 value1, System.Numerics.Vector3 value2) { throw null; }
        public static System.Numerics.Vector3 Min(System.Numerics.Vector3 value1, System.Numerics.Vector3 value2) { throw null; }
        public static System.Numerics.Vector3 Multiply(System.Numerics.Vector3 left, System.Numerics.Vector3 right) { throw null; }
        public static System.Numerics.Vector3 Multiply(System.Numerics.Vector3 left, float right) { throw null; }
        public static System.Numerics.Vector3 Multiply(float left, System.Numerics.Vector3 right) { throw null; }
        public static System.Numerics.Vector3 Negate(System.Numerics.Vector3 value) { throw null; }
        public static System.Numerics.Vector3 Normalize(System.Numerics.Vector3 value) { throw null; }
        public static System.Numerics.Vector3 operator +(System.Numerics.Vector3 left, System.Numerics.Vector3 right) { throw null; }
        public static System.Numerics.Vector3 operator /(System.Numerics.Vector3 left, System.Numerics.Vector3 right) { throw null; }
        public static System.Numerics.Vector3 operator /(System.Numerics.Vector3 value1, float value2) { throw null; }
        public static bool operator ==(System.Numerics.Vector3 left, System.Numerics.Vector3 right) { throw null; }
        public static bool operator !=(System.Numerics.Vector3 left, System.Numerics.Vector3 right) { throw null; }
        public static System.Numerics.Vector3 operator *(System.Numerics.Vector3 left, System.Numerics.Vector3 right) { throw null; }
        public static System.Numerics.Vector3 operator *(System.Numerics.Vector3 left, float right) { throw null; }
        public static System.Numerics.Vector3 operator *(float left, System.Numerics.Vector3 right) { throw null; }
        public static System.Numerics.Vector3 operator -(System.Numerics.Vector3 left, System.Numerics.Vector3 right) { throw null; }
        public static System.Numerics.Vector3 operator -(System.Numerics.Vector3 value) { throw null; }
        public static System.Numerics.Vector3 Reflect(System.Numerics.Vector3 vector, System.Numerics.Vector3 normal) { throw null; }
        public static System.Numerics.Vector3 SquareRoot(System.Numerics.Vector3 value) { throw null; }
        public static System.Numerics.Vector3 Subtract(System.Numerics.Vector3 left, System.Numerics.Vector3 right) { throw null; }
        public override string ToString() { throw null; }
        public string ToString(string format) { throw null; }
        public string ToString(string format, System.IFormatProvider formatProvider) { throw null; }
        public static System.Numerics.Vector3 Transform(System.Numerics.Vector3 position, System.Numerics.Matrix4x4 matrix) { throw null; }
        public static System.Numerics.Vector3 Transform(System.Numerics.Vector3 value, System.Numerics.Quaternion rotation) { throw null; }
        public static System.Numerics.Vector3 TransformNormal(System.Numerics.Vector3 normal, System.Numerics.Matrix4x4 matrix) { throw null; }
    }
    public partial struct Vector4 : System.IEquatable<System.Numerics.Vector4>, System.IFormattable
    {
        public float W;
        public float X;
        public float Y;
        public float Z;
        public Vector4(System.Numerics.Vector2 value, float z, float w) { throw null; }
        public Vector4(System.Numerics.Vector3 value, float w) { throw null; }
        public Vector4(float value) { throw null; }
        public Vector4(float x, float y, float z, float w) { throw null; }
        public static System.Numerics.Vector4 One { get { throw null; } }
        public static System.Numerics.Vector4 UnitW { get { throw null; } }
        public static System.Numerics.Vector4 UnitX { get { throw null; } }
        public static System.Numerics.Vector4 UnitY { get { throw null; } }
        public static System.Numerics.Vector4 UnitZ { get { throw null; } }
        public static System.Numerics.Vector4 Zero { get { throw null; } }
        public static System.Numerics.Vector4 Abs(System.Numerics.Vector4 value) { throw null; }
        public static System.Numerics.Vector4 Add(System.Numerics.Vector4 left, System.Numerics.Vector4 right) { throw null; }
        public static System.Numerics.Vector4 Clamp(System.Numerics.Vector4 value1, System.Numerics.Vector4 min, System.Numerics.Vector4 max) { throw null; }
        public void CopyTo(float[] array) { }
        public void CopyTo(float[] array, int index) { }
        public static float Distance(System.Numerics.Vector4 value1, System.Numerics.Vector4 value2) { throw null; }
        public static float DistanceSquared(System.Numerics.Vector4 value1, System.Numerics.Vector4 value2) { throw null; }
        public static System.Numerics.Vector4 Divide(System.Numerics.Vector4 left, System.Numerics.Vector4 right) { throw null; }
        public static System.Numerics.Vector4 Divide(System.Numerics.Vector4 left, float divisor) { throw null; }
        public static float Dot(System.Numerics.Vector4 vector1, System.Numerics.Vector4 vector2) { throw null; }
        public bool Equals(System.Numerics.Vector4 other) { throw null; }
        public override bool Equals(object obj) { throw null; }
        public override int GetHashCode() { throw null; }
        public float Length() { throw null; }
        public float LengthSquared() { throw null; }
        public static System.Numerics.Vector4 Lerp(System.Numerics.Vector4 value1, System.Numerics.Vector4 value2, float amount) { throw null; }
        public static System.Numerics.Vector4 Max(System.Numerics.Vector4 value1, System.Numerics.Vector4 value2) { throw null; }
        public static System.Numerics.Vector4 Min(System.Numerics.Vector4 value1, System.Numerics.Vector4 value2) { throw null; }
        public static System.Numerics.Vector4 Multiply(System.Numerics.Vector4 left, System.Numerics.Vector4 right) { throw null; }
        public static System.Numerics.Vector4 Multiply(System.Numerics.Vector4 left, float right) { throw null; }
        public static System.Numerics.Vector4 Multiply(float left, System.Numerics.Vector4 right) { throw null; }
        public static System.Numerics.Vector4 Negate(System.Numerics.Vector4 value) { throw null; }
        public static System.Numerics.Vector4 Normalize(System.Numerics.Vector4 vector) { throw null; }
        public static System.Numerics.Vector4 operator +(System.Numerics.Vector4 left, System.Numerics.Vector4 right) { throw null; }
        public static System.Numerics.Vector4 operator /(System.Numerics.Vector4 left, System.Numerics.Vector4 right) { throw null; }
        public static System.Numerics.Vector4 operator /(System.Numerics.Vector4 value1, float value2) { throw null; }
        public static bool operator ==(System.Numerics.Vector4 left, System.Numerics.Vector4 right) { throw null; }
        public static bool operator !=(System.Numerics.Vector4 left, System.Numerics.Vector4 right) { throw null; }
        public static System.Numerics.Vector4 operator *(System.Numerics.Vector4 left, System.Numerics.Vector4 right) { throw null; }
        public static System.Numerics.Vector4 operator *(System.Numerics.Vector4 left, float right) { throw null; }
        public static System.Numerics.Vector4 operator *(float left, System.Numerics.Vector4 right) { throw null; }
        public static System.Numerics.Vector4 operator -(System.Numerics.Vector4 left, System.Numerics.Vector4 right) { throw null; }
        public static System.Numerics.Vector4 operator -(System.Numerics.Vector4 value) { throw null; }
        public static System.Numerics.Vector4 SquareRoot(System.Numerics.Vector4 value) { throw null; }
        public static System.Numerics.Vector4 Subtract(System.Numerics.Vector4 left, System.Numerics.Vector4 right) { throw null; }
        public override string ToString() { throw null; }
        public string ToString(string format) { throw null; }
        public string ToString(string format, System.IFormatProvider formatProvider) { throw null; }
        public static System.Numerics.Vector4 Transform(System.Numerics.Vector2 position, System.Numerics.Matrix4x4 matrix) { throw null; }
        public static System.Numerics.Vector4 Transform(System.Numerics.Vector2 value, System.Numerics.Quaternion rotation) { throw null; }
        public static System.Numerics.Vector4 Transform(System.Numerics.Vector3 position, System.Numerics.Matrix4x4 matrix) { throw null; }
        public static System.Numerics.Vector4 Transform(System.Numerics.Vector3 value, System.Numerics.Quaternion rotation) { throw null; }
        public static System.Numerics.Vector4 Transform(System.Numerics.Vector4 vector, System.Numerics.Matrix4x4 matrix) { throw null; }
        public static System.Numerics.Vector4 Transform(System.Numerics.Vector4 value, System.Numerics.Quaternion rotation) { throw null; }
    }
}
