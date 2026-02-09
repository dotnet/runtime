// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include <platformdefines.h>

#ifdef WINDOWS
#include <immintrin.h>
#endif

// Basic integer function with __vectorcall
extern "C" DLL_EXPORT int
#if defined(WINDOWS)
__vectorcall
#endif
Double_Vectorcall(int a, int* b)
{
    if (b != nullptr)
        *b = a * 2;
    return a * 2;
}

// Float argument should be passed in XMM register with vectorcall
extern "C" DLL_EXPORT float
#if defined(WINDOWS)
__vectorcall
#endif
AddFloats_Vectorcall(float a, float b)
{
    return a + b;
}

// Double argument should be passed in XMM register with vectorcall
extern "C" DLL_EXPORT double
#if defined(WINDOWS)
__vectorcall
#endif
AddDoubles_Vectorcall(double a, double b)
{
    return a + b;
}

// Test mixed int/float arguments (vectorcall should use separate register banks)
extern "C" DLL_EXPORT double
#if defined(WINDOWS)
__vectorcall
#endif
MixedIntFloat_Vectorcall(int a, float b, int c, double d)
{
    return (double)a + (double)b + (double)c + d;
}

// Test multiple float args to exercise XMM0-XMM5 allocation
extern "C" DLL_EXPORT float
#if defined(WINDOWS)
__vectorcall
#endif
SixFloats_Vectorcall(float a, float b, float c, float d, float e, float f)
{
    return a + b + c + d + e + f;
}

// Test multiple double args
extern "C" DLL_EXPORT double
#if defined(WINDOWS)
__vectorcall
#endif
SixDoubles_Vectorcall(double a, double b, double c, double d, double e, double f)
{
    return a + b + c + d + e + f;
}

// Test return value (float should return in XMM0)
extern "C" DLL_EXPORT float
#if defined(WINDOWS)
__vectorcall
#endif
ReturnFloat_Vectorcall(int value)
{
    return (float)value;
}

// Test return value (double should return in XMM0)
extern "C" DLL_EXPORT double
#if defined(WINDOWS)
__vectorcall
#endif
ReturnDouble_Vectorcall(int value)
{
    return (double)value;
}

#if defined(WINDOWS) && (defined(TARGET_AMD64) || defined(TARGET_X86))
// Vector128 test (SSE - __m128)
extern "C" DLL_EXPORT __m128 __vectorcall AddVector128_Vectorcall(__m128 a, __m128 b)
{
    return _mm_add_ps(a, b);
}

// Vector128 test with mixed args - vectors should use XMM independently from int regs (x64)
extern "C" DLL_EXPORT __m128 __vectorcall MixedIntVector128_Vectorcall(int scalar, __m128 vec)
{
    __m128 scalerVec = _mm_set1_ps((float)scalar);
    return _mm_add_ps(scalerVec, vec);
}

#if defined(TARGET_AMD64)
// More than 4 vector args to test XMM5/XMM6 on x64 vectorcall (only 4 on standard win64)
extern "C" DLL_EXPORT __m128 __vectorcall SixVector128s_Vectorcall(__m128 a, __m128 b, __m128 c, __m128 d, __m128 e, __m128 f)
{
    __m128 sum = _mm_add_ps(a, b);
    sum = _mm_add_ps(sum, c);
    sum = _mm_add_ps(sum, d);
    sum = _mm_add_ps(sum, e);
    sum = _mm_add_ps(sum, f);
    return sum;
}

// HVA (Homogeneous Vector Aggregate) tests
// HVA2 - struct with 2 __m128 members, passed in XMM0, XMM1
struct HVA2 { __m128 v0; __m128 v1; };

extern "C" DLL_EXPORT HVA2 __vectorcall AddHVA2_Vectorcall(HVA2 a, HVA2 b)
{
    HVA2 result;
    result.v0 = _mm_add_ps(a.v0, b.v0);
    result.v1 = _mm_add_ps(a.v1, b.v1);
    return result;
}

// HVA3 - struct with 3 __m128 members
struct HVA3 { __m128 v0; __m128 v1; __m128 v2; };

extern "C" DLL_EXPORT HVA3 __vectorcall AddHVA3_Vectorcall(HVA3 a)
{
    // a uses positions 0,1,2 (XMM0, XMM1, XMM2)
    // Result returns in XMM0, XMM1, XMM2
    HVA3 result;
    __m128 one = _mm_set1_ps(1.0f);
    result.v0 = _mm_add_ps(a.v0, one);
    result.v1 = _mm_add_ps(a.v1, one);
    result.v2 = _mm_add_ps(a.v2, one);
    return result;
}

// HVA4 - struct with 4 __m128 members (max for vectorcall)
struct HVA4 { __m128 v0; __m128 v1; __m128 v2; __m128 v3; };

extern "C" DLL_EXPORT HVA4 __vectorcall AddHVA4_Vectorcall(HVA4 a)
{
    // a uses positions 0,1,2,3 (XMM0, XMM1, XMM2, XMM3)
    // Result returns in XMM0, XMM1, XMM2, XMM3
    HVA4 result;
    __m128 ten = _mm_set1_ps(10.0f);
    result.v0 = _mm_add_ps(a.v0, ten);
    result.v1 = _mm_add_ps(a.v1, ten);
    result.v2 = _mm_add_ps(a.v2, ten);
    result.v3 = _mm_add_ps(a.v3, ten);
    return result;
}

// Mixed HVA + scalar to test position counting
extern "C" DLL_EXPORT __m128 __vectorcall MixedHVA2Int_Vectorcall(int scalar, HVA2 hva)
{
    // scalar at position 0 (RCX), hva at positions 1,2 (XMM0, XMM1 - unused regs)
    __m128 scalerVec = _mm_set1_ps((float)scalar);
    __m128 sum = _mm_add_ps(hva.v0, hva.v1);
    return _mm_add_ps(sum, scalerVec);
}

// Discontiguous HVA test (Example 4 from Microsoft docs)
// Passes: a in RCX, b in XMM1, d in XMM3, e pushed on stack
// Passes c by element in [XMM0, XMM2, XMM4, XMM5] - discontiguous because
// vector arguments b and d were allocated first at positions 1 and 3.
extern "C" DLL_EXPORT float __vectorcall DiscontiguousHVA_Vectorcall(int a, float b, HVA4 c, __m128 d, int e)
{
    // Return sum of all inputs to verify correct passing
    float result = (float)a + b + (float)e;
    
    // Extract first element from each HVA member
    float c0[4], c1[4], c2[4], c3[4], d_arr[4];
    _mm_storeu_ps(c0, c.v0);
    _mm_storeu_ps(c1, c.v1);
    _mm_storeu_ps(c2, c.v2);
    _mm_storeu_ps(c3, c.v3);
    _mm_storeu_ps(d_arr, d);
    
    result += c0[0] + c1[0] + c2[0] + c3[0] + d_arr[0];
    return result;
}

// Vector128 multiplication test
extern "C" DLL_EXPORT __m128 __vectorcall MulVector128_Vectorcall(__m128 a, __m128 b)
{
    return _mm_mul_ps(a, b);
}

// HVA2 multiplication test - multiply corresponding vectors
extern "C" DLL_EXPORT HVA2 __vectorcall MulHVA2_Vectorcall(HVA2 a, HVA2 b)
{
    HVA2 result;
    result.v0 = _mm_mul_ps(a.v0, b.v0);
    result.v1 = _mm_mul_ps(a.v1, b.v1);
    return result;
}

// Vector256 tests - require AVX support
// Simple Vector256 addition
extern "C" DLL_EXPORT __m256 __vectorcall AddVector256_Vectorcall(__m256 a, __m256 b)
{
    return _mm256_add_ps(a, b);
}

// Simple Vector256 multiplication
extern "C" DLL_EXPORT __m256 __vectorcall MulVector256_Vectorcall(__m256 a, __m256 b)
{
    return _mm256_mul_ps(a, b);
}

// Vector256 return with scalar - verifies correct register allocation
extern "C" DLL_EXPORT __m256 __vectorcall Vector256MixedInt_Vectorcall(int scalar, __m256 v)
{
    __m256 scalerVec = _mm256_set1_ps((float)scalar);
    return _mm256_add_ps(v, scalerVec);
}

// HVA with Vector256 elements - 2 vectors = 64 bytes in YMM registers
typedef struct { __m256 v0; __m256 v1; } HVA2_256;

extern "C" DLL_EXPORT __m256 __vectorcall HVA2_256_Vectorcall(HVA2_256 hva)
{
    return _mm256_add_ps(hva.v0, hva.v1);
}

// --- Additional test functions for Vector64/128/256/512 edge cases ---

// Identity round-trip: pass a vector through and return it unchanged.
// Verifies basic argument passing and return without any computation.
extern "C" DLL_EXPORT __m128 __vectorcall IdentityVector128_Vectorcall(__m128 a)
{
    return a;
}

// Negate all elements of a Vector128.
extern "C" DLL_EXPORT __m128 __vectorcall NegateVector128_Vectorcall(__m128 a)
{
    return _mm_sub_ps(_mm_setzero_ps(), a);
}

// Horizontal sum: reduce a Vector128 to a single float (vector -> scalar).
extern "C" DLL_EXPORT float __vectorcall HsumVector128_Vectorcall(__m128 a)
{
    // SSE3 horizontal add: a = (a0+a1, a2+a3, a0+a1, a2+a3)
    __m128 shuf = _mm_movehdup_ps(a);    // (a1, a1, a3, a3)
    __m128 sums = _mm_add_ps(a, shuf);   // (a0+a1, *, a2+a3, *)
    shuf = _mm_movehl_ps(shuf, sums);    // (a2+a3, *, *, *)
    sums = _mm_add_ss(sums, shuf);       // (a0+a1+a2+a3, *, *, *)
    return _mm_cvtss_f32(sums);
}

// Scale a vector by a float scalar. Tests mixed scalar+vector.
extern "C" DLL_EXPORT __m128 __vectorcall ScaleVector128_Vectorcall(float scalar, __m128 v)
{
    return _mm_mul_ps(_mm_set1_ps(scalar), v);
}

// Chain two operations: (a + b) * c. Tests using a vectorcall result as input.
extern "C" DLL_EXPORT __m128 __vectorcall FmaVector128_Vectorcall(__m128 a, __m128 b, __m128 c)
{
    return _mm_mul_ps(_mm_add_ps(a, b), c);
}

// Return a constant vector. Tests returning a value not derived from arguments.
extern "C" DLL_EXPORT __m128 __vectorcall ConstVector128_Vectorcall()
{
    return _mm_set_ps(4.0f, 3.0f, 2.0f, 1.0f);
}

// Many integer args + one vector at the end. Tests that vector register
// allocation works when integer positions are exhausted.
extern "C" DLL_EXPORT __m128 __vectorcall ManyIntsOneVector_Vectorcall(int a, int b, int c, int d, __m128 v)
{
    __m128 sum = _mm_set1_ps((float)(a + b + c + d));
    return _mm_add_ps(v, sum);
}

// Identity for Vector256.
extern "C" DLL_EXPORT __m256 __vectorcall IdentityVector256_Vectorcall(__m256 a)
{
    return a;
}

// Negate all elements of a Vector256.
extern "C" DLL_EXPORT __m256 __vectorcall NegateVector256_Vectorcall(__m256 a)
{
    return _mm256_sub_ps(_mm256_setzero_ps(), a);
}

// Vector2 tests — 8-byte struct matching System.Numerics.Vector2 layout.
// On x64 vectorcall, 8-byte structs are passed in integer registers normally,
// but the JIT treats Vector2 as TYP_SIMD8 and passes it in XMM.
// Use a struct to match the managed Vector2 layout for these tests.
struct Vec2 { float x; float y; };

extern "C" DLL_EXPORT Vec2 __vectorcall IdentityVector2_Vectorcall(Vec2 a)
{
    return a;
}

extern "C" DLL_EXPORT Vec2 __vectorcall AddVector2_Vectorcall(Vec2 a, Vec2 b)
{
    Vec2 result;
    result.x = a.x + b.x;
    result.y = a.y + b.y;
    return result;
}

#endif // TARGET_AMD64

// Callback test - calls back into managed code using vectorcall
typedef double (__vectorcall *MixedCallbackFunc)(int a, float b, int c, double d);

extern "C" DLL_EXPORT double __vectorcall TestCallback_Vectorcall(MixedCallbackFunc callback)
{
    // Call the callback with known values and verify result
    return callback(1, 2.0f, 3, 4.0);
}

#endif // WINDOWS && (TARGET_AMD64 || TARGET_X86)
