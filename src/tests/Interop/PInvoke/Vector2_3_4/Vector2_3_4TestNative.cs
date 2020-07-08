// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Numerics;
using System.Runtime.InteropServices;

#pragma warning disable 0618
static class Vector2_3_4TestNative
{
    public struct Vector2Wrapper
    {
        public Vector2 vec;
    };

    public struct Vector3Wrapper
    {
        public Vector3 vec;
    };

    public struct Vector4Wrapper
    {
        public Vector4 vec;
    };

    [DllImport(nameof(Vector2_3_4TestNative))]
    public static extern Vector2 CreateVector2FromFloats(float x, float y);
    [DllImport(nameof(Vector2_3_4TestNative))]
    public static extern Vector3 CreateVector3FromFloats(float x, float y, float z);
    [DllImport(nameof(Vector2_3_4TestNative))]
    public static extern Vector4 CreateVector4FromFloats(float x, float y, float z, float w);

    [DllImport(nameof(Vector2_3_4TestNative), EntryPoint = "CreateVector2FromFloats")]
    public static extern Vector2Wrapper CreateWrappedVector2FromFloats(float x, float b);
    [DllImport(nameof(Vector2_3_4TestNative), EntryPoint = "CreateVector3FromFloats")]
    public static extern Vector3Wrapper CreateWrappedVector3FromFloats(float x, float y, float z);
    [DllImport(nameof(Vector2_3_4TestNative), EntryPoint = "CreateVector4FromFloats")]
    public static extern Vector4Wrapper CreateWrappedVector4FromFloats(float x, float y, float z, float w);

    [DllImport(nameof(Vector2_3_4TestNative))]
    public static extern bool Vector2EqualToFloats(Vector2 vec, float x, float y);
    [DllImport(nameof(Vector2_3_4TestNative))]
    public static extern bool Vector3EqualToFloats(Vector3 vec, float x, float y, float z);
    [DllImport(nameof(Vector2_3_4TestNative))]
    public static extern bool Vector4EqualToFloats(Vector4 vec, float x, float y, float z, float w);

    [DllImport(nameof(Vector2_3_4TestNative))]
    public static extern bool ValidateAndChangeVector2(ref Vector2 dec, float expectedX, float expectedY, float newX, float newY);
    [DllImport(nameof(Vector2_3_4TestNative))]
    public static extern bool ValidateAndChangeVector3(ref Vector3 dec, float expectedX, float expectedY, float expectedZ, float newX, float newY, float newZ);
    [DllImport(nameof(Vector2_3_4TestNative))]
    public static extern bool ValidateAndChangeVector4(ref Vector4 dec, float expectedX, float expectedY, float expectedZ, float expectedW, float newX, float newY, float newZ, float newW);

    [DllImport(nameof(Vector2_3_4TestNative))]
    public static extern void GetVector2ForFloats(float x, float y, out Vector2 dec);
    [DllImport(nameof(Vector2_3_4TestNative))]
    public static extern void GetVector3ForFloats(float x, float y, float z, out Vector3 dec);
    [DllImport(nameof(Vector2_3_4TestNative))]
    public static extern void GetVector4ForFloats(float x, float y, float z, float w, out Vector4 dec);

    [DllImport(nameof(Vector2_3_4TestNative), EntryPoint = "Vector2EqualToFloats")]
    public static extern bool WrappedVector2EqualToFloats(Vector2Wrapper vec, float x, float y);
    [DllImport(nameof(Vector2_3_4TestNative), EntryPoint = "Vector3EqualToFloats")]
    public static extern bool WrappedVector3EqualToFloats(Vector3Wrapper vec, float x, float y, float z);
    [DllImport(nameof(Vector2_3_4TestNative), EntryPoint = "Vector4EqualToFloats")]
    public static extern bool WrappedVector4EqualToFloats(Vector4Wrapper vec, float x, float y, float z, float w);

    [DllImport(nameof(Vector2_3_4TestNative), EntryPoint = "ValidateAndChangeVector2")]
    public static extern bool ValidateAndChangeWrappedVector2(ref Vector2Wrapper dec, float expectedX, float expectedY, float newX, float newY);
    [DllImport(nameof(Vector2_3_4TestNative), EntryPoint = "ValidateAndChangeVector3")]
    public static extern bool ValidateAndChangeWrappedVector3(ref Vector3Wrapper dec, float expectedX, float expectedY, float expectedZ, float newX, float newY, float newZ);
    [DllImport(nameof(Vector2_3_4TestNative), EntryPoint = "ValidateAndChangeVector4")]
    public static extern bool ValidateAndChangeWrappedVector4(ref Vector4Wrapper dec, float expectedX, float expectedY, float expectedZ, float expectedW, float newX, float newY, float newZ, float newW);

    public delegate Vector2 Vector2Callback(Vector2 dec);
    public delegate Vector3 Vector3Callback(Vector3 dec);
    public delegate Vector4 Vector4Callback(Vector4 dec);

    [DllImport(nameof(Vector2_3_4TestNative))]
    public static extern Vector2 PassThroughVector2ToCallback(Vector2 startingVector, Vector2Callback callback);
    [DllImport(nameof(Vector2_3_4TestNative))]
    public static extern Vector3 PassThroughVector3ToCallback(Vector3 startingVector, Vector3Callback callback);
    [DllImport(nameof(Vector2_3_4TestNative))]
    public static extern Vector4 PassThroughVector4ToCallback(Vector4 startingVector, Vector4Callback callback);
}
