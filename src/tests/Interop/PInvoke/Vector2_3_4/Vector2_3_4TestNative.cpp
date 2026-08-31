// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "xplatform.h"
#include <new>

struct Vector2
{
    float x;
    float y;
};

struct Vector3
{
    float x;
    float y;
    float z;
};

struct Vector4
{
    float x;
    float y;
    float z;
    float w;
};

namespace
{
    BOOL operator==(Vector2 lhs, Vector2 rhs)
    {
        return lhs.x == rhs.x && lhs.y == rhs.y ? TRUE : FALSE;
    }
    BOOL operator==(Vector3 lhs, Vector3 rhs)
    {
        return lhs.x == rhs.x && lhs.y == rhs.y && lhs.z == rhs.z ? TRUE : FALSE;
    }
    BOOL operator==(Vector4 lhs, Vector4 rhs)
    {
        return lhs.x == rhs.x && lhs.y == rhs.y && lhs.z == rhs.z && lhs.w == rhs.w ? TRUE : FALSE;
    }
}

extern "C" DLL_EXPORT Vector4 STDMETHODCALLTYPE CreateVector4FromFloats(float x, float y, float z, float w)
{
    Vector4 result;
    result.x = x;
    result.y = y;
    result.z = z;
    result.w = w;
    return result;
}

extern "C" DLL_EXPORT Vector3 STDMETHODCALLTYPE CreateVector3FromFloats(float x, float y, float z)
{
    Vector3 result;
    result.x = x;
    result.y = y;
    result.z = z;
    return result;
}

extern "C" DLL_EXPORT Vector2 STDMETHODCALLTYPE CreateVector2FromFloats(float x, float y)
{
    Vector2 result;
    result.x = x;
    result.y = y;
    return result;
}

extern "C" DLL_EXPORT BOOL STDMETHODCALLTYPE Vector4EqualToFloats(Vector4 vec, float x, float y, float z, float w)
{
    Vector4 vecFromFloats;
    vecFromFloats.x = x;
    vecFromFloats.y = y;
    vecFromFloats.z = z;
    vecFromFloats.w = w;
    return vec == vecFromFloats;
}

extern "C" DLL_EXPORT BOOL STDMETHODCALLTYPE Vector3EqualToFloats(Vector3 vec, float x, float y, float z)
{
    Vector3 vecFromFloats;
    vecFromFloats.x = x;
    vecFromFloats.y = y;
    vecFromFloats.z = z;
    return vec == vecFromFloats;
}

extern "C" DLL_EXPORT BOOL STDMETHODCALLTYPE Vector2EqualToFloats(Vector2 vec, float x, float y)
{
    Vector2 vecFromFloats;
    vecFromFloats.x = x;
    vecFromFloats.y = y;
    return vec == vecFromFloats;
}

extern "C" DLL_EXPORT BOOL STDMETHODCALLTYPE ValidateAndChangeVector4(Vector4* vec, float expectedX, float expectedY, float expectedZ, float expectedW, float newX, float newY, float newZ, float newW)
{
    Vector4 vecExpected;
    vecExpected.x = expectedX;
    vecExpected.y = expectedY;
    vecExpected.z = expectedZ;
    vecExpected.w = expectedW;

    BOOL result = *vec == vecExpected;
    vec->x = newX;
    vec->y = newY;
    vec->z = newZ;
    vec->w = newW;
    return result;
}

extern "C" DLL_EXPORT BOOL STDMETHODCALLTYPE ValidateAndChangeVector3(Vector3* vec, float expectedX, float expectedY, float expectedZ, float newX, float newY, float newZ)
{
    Vector3 vecExpected;
    vecExpected.x = expectedX;
    vecExpected.y = expectedY;
    vecExpected.z = expectedZ;

    BOOL result = *vec == vecExpected;
    vec->x = newX;
    vec->y = newY;
    vec->z = newZ;
    return result;
}

extern "C" DLL_EXPORT BOOL STDMETHODCALLTYPE ValidateAndChangeVector2(Vector2* vec, float expectedX, float expectedY, float newX, float newY)
{
    Vector2 vecExpected;
    vecExpected.x = expectedX;
    vecExpected.y = expectedY;

    BOOL result = *vec == vecExpected;
    vec->x = newX;
    vec->y = newY;
    return result;
}

extern "C" DLL_EXPORT void STDMETHODCALLTYPE GetVector4ForFloats(float x, float y, float z, float w, Vector4* vec)
{
    vec->x = x;
    vec->y = y;
    vec->z = z;
    vec->w = w;
}

extern "C" DLL_EXPORT void STDMETHODCALLTYPE GetVector3ForFloats(float x, float y, float z, Vector3* vec)
{
    vec->x = x;
    vec->y = y;
    vec->z = z;
}

extern "C" DLL_EXPORT void STDMETHODCALLTYPE GetVector2ForFloats(float x, float y, Vector2* vec)
{
    vec->x = x;
    vec->y = y;
}


using Vector4Callback = Vector4(STDMETHODCALLTYPE*)(Vector4);

extern "C" DLL_EXPORT Vector4 STDMETHODCALLTYPE PassThroughVector4ToCallback(Vector4 vec, Vector4Callback cb)
{
    return cb(vec);
}

using Vector3Callback = Vector3(STDMETHODCALLTYPE*)(Vector3);

extern "C" DLL_EXPORT Vector3 STDMETHODCALLTYPE PassThroughVector3ToCallback(Vector3 vec, Vector3Callback cb)
{
    return cb(vec);
}

using Vector2Callback = Vector2(STDMETHODCALLTYPE*)(Vector2);

extern "C" DLL_EXPORT Vector2 STDMETHODCALLTYPE PassThroughVector2ToCallback(Vector2 vec, Vector2Callback cb)
{
    return cb(vec);
}
