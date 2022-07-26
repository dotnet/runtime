// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include <stdio.h>
#include <string.h>
#include <minipal/utils.h>

#if defined(__GNUC__)
#define EXPORT(type) extern "C" __attribute__((visibility("default"))) type
#elif defined(_MSC_VER)
#define EXPORT(type) extern "C" __declspec(dllexport) type
#else // defined(__GNUC__)
#define EXPORT(type) type
#endif // !defined(__GNUC__)

#if !defined(_MSC_VER)
#if __i386__
#define __stdcall __attribute__((stdcall))
#else // __i386__
#define __stdcall
#endif  // !__i386__
#endif // !defined(_MSC_VER)

#ifdef _MSC_VER
#define CALLBACK __stdcall
#else // _MSC_VER
#define CALLBACK
#endif // !_MSC_VER

typedef struct _Vector3
{
    float x;
    float y;
    float z;
} Vector3;

typedef struct _DT
{
   Vector3 a;
   Vector3 b;
} DT;

typedef struct _ComplexDT
{
    int iv;
    DT vecs;
    char str[256];
    Vector3 v3;
} ComplexDT;

//
// PInvoke native call for Vector3 size check
//

EXPORT(int) __stdcall nativeCall_PInvoke_CheckVector3Size()
{
    printf("nativeCall_PInvoke_CheckVector3Size: sizeof(Vector3) == %d\n", (int)sizeof(Vector3));
    fflush(stdout);
    return sizeof(Vector3);
}

//
// PInvoke native call for Vector3 argument
//

EXPORT(float) __stdcall nativeCall_PInvoke_Vector3Arg(int i, Vector3 v1, char* s, Vector3 v2)
{
    float sum0 = v1.x + v1.y + v1.z;
    float sum1 = v2.x + v2.y + v2.z;
    printf("nativeCall_PInvoke_Vector3Arg:\n");
    printf("    iVal %d\n", i);
    printf("    sumOfEles(%f, %f, %f) = %f\n", v1.x, v1.y, v1.z, sum0);
    printf("    str  %s\n", s);
    printf("    sumOfEles(%f, %f, %f) = %f\n", v2.x, v2.y, v2.z, sum1);
    fflush(stdout);
    if ((strncmp(s, "abcdefg", strnlen(s, 32)) != 0) || i != 123) {
        return 0;
    }
    return sum0 + sum1;
}

//
// PInvoke native call for Vector3 argument
//
EXPORT(float) __stdcall nativeCall_PInvoke_Vector3Arg_Unix(
    Vector3 v3f32_xmm0,
    float   f32_xmm2,
    float   f32_xmm3,
    float   f32_xmm4,
    float   f32_xmm5,
    float   f32_xmm6,
    float   f32_xmm7,
    float   f32_mem0,
    Vector3 v3f32_mem1,
    float   f32_mem2,
    float   f32_mem3)
{
    printf("nativeCall_PInvoke_Vector3Arg_Unix:\n");
    printf("    v3f32_xmm0: %f %f %f\n", v3f32_xmm0.x, v3f32_xmm0.y, v3f32_xmm0.z);
    printf("    f32_xmm2 - f32_xmm7: %f %f %f %f %f %f\n", f32_xmm2, f32_xmm3,
        f32_xmm4, f32_xmm5, f32_xmm6, f32_xmm7);
    printf("    f32_mem0: %f\n", f32_mem0);
    printf("    v3f32_mem1: %f %f %f\n", v3f32_mem1.x, v3f32_mem1.y, v3f32_mem1.z);
    printf("    f32_mem2-3: %f %f\n", f32_mem2, f32_mem3);

    // sum = 1 + 2 + 3
    //  + 100 + 101 + 102 + 103 + 104 + 105 + 106
    //  + 10 + 20 + 30
    //  + 107 + 108
    //  = 1002
    float sum = v3f32_xmm0.x + v3f32_xmm0.y + v3f32_xmm0.z
        + f32_xmm2 + f32_xmm3 + f32_xmm4 + f32_xmm5 + f32_xmm6 + f32_xmm7 + f32_mem0 +
        + v3f32_mem1.x + v3f32_mem1.y + v3f32_mem1.z
        + f32_mem2 + f32_mem3;

    printf("    sum = %f\n", sum);

    return sum;
}


//
// PInvoke native call for Vector3 argument
//
EXPORT(float) __stdcall nativeCall_PInvoke_Vector3Arg_Unix2(
    Vector3 v3f32_xmm0,
    float   f32_xmm2,
    float   f32_xmm3,
    float   f32_xmm4,
    float   f32_xmm5,
    float   f32_xmm6,
    float   f32_xmm7,
    float   f32_mem0,
    Vector3 v3f32_mem1,
    float   f32_mem2,
    float   f32_mem3,
    Vector3 v3f32_mem4,
    float   f32_mem5)
{
    printf("nativeCall_PInvoke_Vector3Arg_Unix2:\n");
    printf("    v3f32_xmm0: %f %f %f\n", v3f32_xmm0.x, v3f32_xmm0.y, v3f32_xmm0.z);
    printf("    f32_xmm2 - f32_xmm7: %f %f %f %f %f %f\n",
      f32_xmm2, f32_xmm3, f32_xmm4, f32_xmm5, f32_xmm6, f32_xmm7);
    printf("    f32_mem0: %f\n", f32_mem0);
    printf("    v3f32_mem1: %f %f %f\n", v3f32_mem1.x, v3f32_mem1.y, v3f32_mem1.z);
    printf("    f32_mem2-3: %f %f\n", f32_mem2, f32_mem3);
    printf("    v3f32_mem4: %f %f %f\n", v3f32_mem4.x, v3f32_mem4.y, v3f32_mem4.z);
    printf("    f32_mem5: %f\n", f32_mem5);

    // sum = 1 + 2 + 3 +
    //  + 100 + 101 + 102 + 103 + 104 + 105 + 106
    //  + 4 + 5 + 6
    //  + 107 + 108
    //  + 7 + 8 + 9
    //  + 109
    //  = 6 + 15 + 24 + 1045 = 1090
    float sum = v3f32_xmm0.x + v3f32_xmm0.y + v3f32_xmm0.z
        + f32_xmm2 + f32_xmm3 + f32_xmm4 + f32_xmm5 + f32_xmm6 + f32_xmm7 + f32_mem0 +
        + v3f32_mem1.x + v3f32_mem1.y + v3f32_mem1.z
        + f32_mem2 + f32_mem3
        + v3f32_mem4.x + v3f32_mem4.y + v3f32_mem4.z
        + f32_mem5;

    printf("    sum = %f\n", sum);

    return sum;
}


//
// PInvoke native call for Vector3 argument
//

EXPORT(Vector3) __stdcall nativeCall_PInvoke_Vector3Ret()
{
    Vector3 ret;
    ret.x = 1;
    ret.y = 2;
    ret.z = 3;
    float sum = ret.x + ret.y + ret.z;
    printf("nativeCall_PInvoke_Vector3Ret:\n");
    printf("    Return value: (%f, %f, %f)\n", ret.x, ret.y, ret.z);
    printf("    Sum of return scalar values = %f\n", sum);
    fflush(stdout);
    return ret;
}

//
// PInvoke native call for Vector3 array
//

EXPORT(float) __stdcall nativeCall_PInvoke_Vector3Array(Vector3* arr)
{
    float sum = 0.0;
    printf("nativeCall_PInvoke_Vector3Array\n");
    for (unsigned i = 0; i < 2; ++i)
    {
        Vector3* e = &arr[i];
        printf("    arrEle[%d]: %f %f %f\n", i, e->x, e->y, e->z);
        sum += e->x + e->y + e->z;
    }
    printf("    Sum = %f\n", sum);
    fflush(stdout);
    return sum;
}

//
// PInvoke native call for Vector3 in struct
//

EXPORT(DT) __stdcall nativeCall_PInvoke_Vector3InStruct(DT data)
{
    printf("nativeCall_PInvoke_Vector3InStruct\n");
    DT ret;
    ret.a.x = data.a.x + 1;
    ret.a.y = data.a.y + 1;
    ret.a.z = data.a.z + 1;
    ret.b.x = data.b.x + 1;
    ret.b.y = data.b.y + 1;
    ret.b.z = data.b.z + 1;
    printf("    First struct member: (%f %f %f) -> (%f %f %f)\n",
        data.a.x, data.a.y, data.a.z, ret.a.x, ret.a.y, ret.a.z);
    printf("    Second struct member: (%f %f %f) -> (%f %f %f)\n",
        data.b.x, data.b.y, data.b.z, ret.b.x, ret.b.y, ret.b.z);
    float sum = ret.a.x + ret.a.y + ret.a.z + ret.b.x + ret.b.y + ret.b.z;
    printf("    Sum of all return scalar values = %f\n", sum);
    fflush(stdout);
    return ret;
}

//
// PInvoke native call for Vector3 in complex struct
//

EXPORT(void) __stdcall nativeCall_PInvoke_Vector3InComplexStruct(ComplexDT* arg)
{
    printf("nativeCall_PInvoke_Vector3InComplexStruct\n");
    printf("    Arg ival: %d\n", arg->iv);
    printf("    Arg Vector3 v1: (%f %f %f)\n", arg->vecs.a.x, arg->vecs.a.y, arg->vecs.a.z);
    printf("    Arg Vector3 v2: (%f %f %f)\n", arg->vecs.b.x, arg->vecs.b.y, arg->vecs.b.z);
    printf("    Arg Vector3 v3: (%f %f %f)\n", arg->v3.x, arg->v3.y, arg->v3.z);
    printf("    Arg string arg: %s\n", arg->str);

    arg->vecs.a.x = arg->vecs.a.x + 1;
    arg->vecs.a.y = arg->vecs.a.y + 1;
    arg->vecs.a.z = arg->vecs.a.z + 1;
    arg->vecs.b.x = arg->vecs.b.x + 1;
    arg->vecs.b.y = arg->vecs.b.y + 1;
    arg->vecs.b.z = arg->vecs.b.z + 1;
    arg->v3.x = arg->v3.x + 1;
    arg->v3.y = arg->v3.y + 1;
    arg->v3.z = arg->v3.z + 1;
    arg->iv = arg->iv + 1;
    snprintf(arg->str, ARRAY_SIZE(arg->str), "%s", "ret_string");

    printf("    Return ival: %d\n", arg->iv);
    printf("    Return Vector3 v1: (%f %f %f)\n", arg->vecs.a.x, arg->vecs.a.y, arg->vecs.a.z);
    printf("    Return Vector3 v2: (%f %f %f)\n", arg->vecs.b.x, arg->vecs.b.y, arg->vecs.b.z);
    printf("    Return Vector3 v3: (%f %f %f)\n", arg->v3.x, arg->v3.y, arg->v3.z);
    printf("    Return string arg: %s\n", arg->str);
    float sum = arg->vecs.a.x + arg->vecs.a.y + arg->vecs.a.z
        + arg->vecs.b.x + arg->vecs.b.y + arg->vecs.b.z
        + arg->v3.x + arg->v3.y + arg->v3.z;
    printf("    Sum of all return float scalar values = %f\n", sum);
    fflush(stdout);
}

//
// RPInvoke native call for Vector3 argument
//
typedef void (CALLBACK *CallBack_RPInvoke_Vector3Arg)(int i, Vector3 v1, char* s, Vector3 v2);


EXPORT(void) __stdcall nativeCall_RPInvoke_Vector3Arg(
  CallBack_RPInvoke_Vector3Arg notify)
{
    int i = 123;
    const static char* str = "abcdefg";
    Vector3 v1, v2;
    v1.x = 1; v1.y = 2; v1.z = 3;
    v2.x = 10; v2.y = 20; v2.z = 30;
    notify(i, v1, (char*)str, v2);
}



//
// RPInvoke native call for Vector3 argument
//
typedef void (CALLBACK *CallBack_RPInvoke_Vector3Arg_Unix)(
    Vector3 v3f32_xmm0,
    float   f32_xmm2,
    float   f32_xmm3,
    float   f32_xmm4,
    float   f32_xmm5,
    float   f32_xmm6,
    float   f32_xmm7,
    float   f32_mem0,
    Vector3 v3f32_mem1,
    float   f32_mem2,
    float   f32_mem3);


EXPORT(void) __stdcall nativeCall_RPInvoke_Vector3Arg_Unix(
  CallBack_RPInvoke_Vector3Arg_Unix notify)
{
    Vector3 v1, v2;
    v1.x = 1; v1.y = 2; v1.z = 3;
    v2.x = 10; v2.y = 20; v2.z = 30;
    float f0 = 100, f1 = 101, f2 = 102, f3 = 103, f4 = 104, f5 = 105, f6 = 106, f7 = 107, f8 = 108;
    notify(
        v1,
        f0, f1, f2, f3, f4, f5,
        f6, // mapped onto stack
        v2,
        f7, f8);
}



//
// RPInvoke native call for Vector3 argument
//
typedef void (CALLBACK *CallBack_RPInvoke_Vector3Arg_Unix2)(
    Vector3 v3f32_xmm0,
    float   f32_xmm2,
    float   f32_xmm3,
    float   f32_xmm4,
    float   f32_xmm5,
    float   f32_xmm6,
    float   f32_xmm7,
    float   f32_mem0,
    Vector3 v3f32_mem1,
    float   f32_mem2,
    float   f32_mem3,
    Vector3 v3f32_mem4,
    float   f32_mem5);


EXPORT(void) __stdcall nativeCall_RPInvoke_Vector3Arg_Unix2(
  CallBack_RPInvoke_Vector3Arg_Unix2 notify)
{
    Vector3 v1, v2, v3;
    v1.x = 1; v1.y = 2; v1.z = 3;
    v2.x = 4; v2.y = 5; v2.z = 6;
    v3.x = 7; v3.y = 8; v3.z = 9;
    float f0 = 100, f1 = 101, f2 = 102, f3 = 103, f4 = 104, f5 = 105, f6 = 106, f7 = 107, f8 = 108, f9 = 109;
    notify(
        v1,
        f0, f1, f2, f3, f4, f5,
        f6, // mapped onto stack
        v2,
        f7, f8,
        v3,
        f9);
}


//
// RPInvoke native call for Vector3 array
//

typedef Vector3 (CALLBACK *CallBack_RPInvoke_Vector3Ret)();

EXPORT(bool) __stdcall nativeCall_RPInvoke_Vector3Ret(
  CallBack_RPInvoke_Vector3Ret notify)
{
    Vector3 ret = notify();
    printf("nativeCall_RPInvoke_Vector3Ret:\n    Return value (%f %f %f)\n",
        ret.x, ret.y, ret.z);
    fflush(stdout);
    if (ret.x == 1 && ret.y == 2 && ret.z == 3) {
        return true;
    }
    return false;
}

//
// RPInvoke native call for Vector3 array
//

typedef void (CALLBACK *CallBack_RPInvoke_Vector3Array)(Vector3* v, int size);

static Vector3 arr[2];

EXPORT(void) __stdcall nativeCall_RPInvoke_Vector3Array(
  CallBack_RPInvoke_Vector3Array notify,
  int a)
{
    arr[0].x = a + 1.0f;
    arr[0].y = a + 2.0f;
    arr[0].z = a + 3.0f;
    arr[1].x = a + 10.0f;
    arr[1].y = a + 20.0f;
    arr[1].z = a + 30.0f;
    notify(arr, 2);
}

//
// RPInvoke native call for Vector3-in-struct test
//

typedef void (CALLBACK *CallBack_RPInvoke_Vector3InStruct)(DT v);

static DT v;

EXPORT(void) __stdcall nativeCall_RPInvoke_Vector3InStruct(
  CallBack_RPInvoke_Vector3InStruct notify,
  int a)
{
    v.a.x = a + 1.0f;
    v.a.y = a + 2.0f;
    v.a.z = a + 3.0f;
    v.b.x = a + 10.0f;
    v.b.y = a + 20.0f;
    v.b.z = a + 30.0f;
    notify(v);
}

//
// RPInvoke native call for complex Vector3-in-struct test
//

typedef bool (CALLBACK *CallBack_RPInvoke_Vector3InComplexStruct)(ComplexDT* v);

EXPORT(bool) __stdcall nativeCall_RPInvoke_Vector3InComplexStruct(
  CallBack_RPInvoke_Vector3InComplexStruct notify)
{
    static ComplexDT cdt;
    cdt.iv = 99;
    snprintf(cdt.str, ARRAY_SIZE("arg_string"), "%s", "arg_string");
    cdt.vecs.a.x = 1; cdt.vecs.a.y = 2; cdt.vecs.a.z = 3;
    cdt.vecs.b.x = 5; cdt.vecs.b.y = 6; cdt.vecs.b.z = 7;
    cdt.v3.x = 10; cdt.v3.y = 20; cdt.v3.z = 30;

    notify(&cdt);

    printf("    Native ival: %d\n", cdt.iv);
    printf("    Native Vector3 v1: (%f %f %f)\n", cdt.vecs.a.x, cdt.vecs.a.y, cdt.vecs.a.z);
    printf("    Native Vector3 v2: (%f %f %f)\n", cdt.vecs.b.x, cdt.vecs.b.y, cdt.vecs.b.z);
    printf("    Native Vector3 v3: (%f %f %f)\n", cdt.v3.x, cdt.v3.y, cdt.v3.z);
    printf("    Native string arg: %s\n", cdt.str);
    fflush(stdout);

    // Expected return value = 2 + 3 + 4 + 6 + 7 + 8 + 11 + 12 + 13 = 93
    float sum = cdt.vecs.a.x + cdt.vecs.a.y + cdt.vecs.a.z
        + cdt.vecs.b.x + cdt.vecs.b.y + cdt.vecs.b.z
        + cdt.v3.x + cdt.v3.y + cdt.v3.z;

    if ((sum != 93) || (cdt.iv != 100) || (strcmp(cdt.str, "ret_string")!=0) )
    {
        return false;
    }
    return true;
}
