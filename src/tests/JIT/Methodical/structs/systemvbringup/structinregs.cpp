#include <stdio.h>

#if defined(__GNUC__)
#define EXPORT(type) extern "C" __attribute__((visibility("default"))) type
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
#define CALLBACK  __stdcall
#define NATIVEAPI __stdcall
#else // _MSC_VER
#define CALLBACK
#define NATIVEAPI
#endif // !_MSC_VER

typedef unsigned char byte;

struct S1
{
    int x;
    int y;
    int z;
    int w;
};

struct S2
{
    int x;
    int y;
    float z;
};

struct S3
{
    int x;
    int y;
    double z;
};

struct S4
{
    int x;
    float y;
};

struct S5
{
    int x;
    double y;
};

struct S6
{
    short x;
    short y;
    int z;
    int w;
};

struct S7
{
    double x;
    int y;
    int z;
};

struct S8
{
    double x;
    int y;
};

struct S9
{
    int x;
    int y;
    float z;
    float w;
};

struct S10
{
    byte a;
    byte b;
    byte c;
    byte d;
    byte e;
    byte f;
    byte g;
    byte h;
};

struct S11
{
    byte a;
    byte b;
    byte c;
    byte d;
    double e;
};

struct S12
{
    byte a;
    byte b;
    byte c;
    byte d;
    byte e;
    byte f;
    byte g;
    byte h;
    long long i;
};

struct S13
{
    byte hasValue;
    int x;
};

struct S14
{
    byte x;
    long long y;
};

struct S15
{
    byte a;
    byte b;
    byte c;
    byte d;
    byte e;
    byte f;
    byte g;
    byte h;
    byte i;
};

struct S16
{
    byte x;
    short y;
};

struct S17
{
    float x;
    float y;
};

struct S18
{
    float x;
    int y;
    float z;
};

struct S19
{
    int x;
    float y;
    int z;
    float w;
};

struct S20
{
    long long x;
    long long y;
    long long z;
    long long w;
};

struct S28
{
    void* x;
    int y;
};

struct S29
{
    int x;
    void* y;
};

struct S30
{
    long long x;
    long long y;
};

typedef void (CALLBACK *PFNACTION1)(S1 s);
typedef void (CALLBACK *PFNACTION2)(S2 s);
typedef void (CALLBACK *PFNACTION3)(S3 s);
typedef void (CALLBACK *PFNACTION4)(S4 s);
typedef void (CALLBACK *PFNACTION5)(S5 s);
typedef void (CALLBACK *PFNACTION6)(S6 s);
typedef void (CALLBACK *PFNACTION7)(S7 s);
typedef void (CALLBACK *PFNACTION8)(S8 s);
typedef void (CALLBACK *PFNACTION9)(S9 s);
typedef void (CALLBACK *PFNACTION10)(S10 s);
typedef void (CALLBACK *PFNACTION11)(S11 s);
typedef void (CALLBACK *PFNACTION12)(S12 s);
typedef void (CALLBACK *PFNACTION13)(S13 s);
typedef void (CALLBACK *PFNACTION14)(S14 s);
typedef void (CALLBACK *PFNACTION15)(S15 s);
typedef void (CALLBACK *PFNACTION16)(S16 s);
typedef void (CALLBACK *PFNACTION17)(S17 s);
typedef void (CALLBACK *PFNACTION18)(S18 s);
typedef void (CALLBACK *PFNACTION19)(S19 s);
typedef void (CALLBACK *PFNACTION20)(S20 s);

typedef void (CALLBACK *PFNACTION28)(S28 s);
typedef void (CALLBACK *PFNACTION29)(S29 s);
typedef void (CALLBACK *PFNACTION30)(S30 s1, S30 s2, S30 s3);

EXPORT(void) NATIVEAPI InvokeCallback1(PFNACTION1 callback, S1 s)
{
    printf("Native S1: %d, %d, %d, %d\n", s.x, s.y, s.z, s.w);
    callback(s);
}

EXPORT(void) NATIVEAPI InvokeCallback2(PFNACTION2 callback, S2 s)
{
    printf("Native S2: %d, %d, %f\n", s.x, s.y, s.z);
    callback(s);
}
EXPORT(void) NATIVEAPI InvokeCallback3(PFNACTION3 callback, S3 s)
{
    printf("Native S3: %d, %d, %f\n", s.x, s.y, s.z);
    callback(s);
}
EXPORT(void) NATIVEAPI InvokeCallback4(PFNACTION4 callback, S4 s)
{
    printf("Native S4: %d, %f\n", s.x, s.y);
    callback(s);
}
EXPORT(void) NATIVEAPI InvokeCallback5(PFNACTION5 callback, S5 s)
{
    printf("Native S5: %d, %f\n", s.x, s.y);
    callback(s);
}
EXPORT(void) NATIVEAPI InvokeCallback6(PFNACTION6 callback, S6 s)
{
    printf("Native S6: %hd, %hd, %d, %d\n", s.x, s.y, s.z, s.w);
    callback(s);
}
EXPORT(void) NATIVEAPI InvokeCallback7(PFNACTION7 callback, S7 s)
{
    printf("Native S7: %f, %d, %d\n", s.x, s.y, s.z);
    callback(s);
}
EXPORT(void) NATIVEAPI InvokeCallback8(PFNACTION8 callback, S8 s)
{
    printf("Native S8: %f, %d\n", s.x, s.y);
    callback(s);
}
EXPORT(void) NATIVEAPI InvokeCallback9(PFNACTION9 callback, S9 s)
{
    printf("Native S9: %d, %d, %f, %f\n", s.x, s.y, s.z, s.w);
    callback(s);
}
EXPORT(void) NATIVEAPI InvokeCallback10(PFNACTION10 callback, S10 s)
{
    printf("Native S10: %hhd, %hhd, %hhd, %hhd, %hhd, %hhd, %hhd, %hhd\n", s.a, s.b, s.c, s.d, s.e, s.f, s.g, s.h);
    callback(s);
}

EXPORT(void) NATIVEAPI InvokeCallback11(PFNACTION11 callback, S11 s)
{
    printf("Native S11: %hhd, %hhd, %hhd, %hhd, %f\n", s.a, s.b, s.c, s.d, s.e);
    callback(s);
}
EXPORT(void) NATIVEAPI InvokeCallback12(PFNACTION12 callback, S12 s)
{
    printf("Native S12: %hhd, %hhd, %hhd, %hhd, %hhd, %hhd, %hhd, %hhd, %lld\n", s.a, s.b, s.c, s.d, s.e, s.f, s.g, s.h, s.i);
    callback(s);
}
EXPORT(void) NATIVEAPI InvokeCallback13(PFNACTION13 callback, S13 s)
{
    printf("Native S13: %hhd, %d\n", s.hasValue, s.x);
    callback(s);
}
EXPORT(void) NATIVEAPI InvokeCallback14(PFNACTION14 callback, S14 s)
{
    printf("Native S13: %hhd, %lld\n", s.x, s.y);
    callback(s);
}
EXPORT(void) NATIVEAPI InvokeCallback15(PFNACTION15 callback, S15 s)
{
    printf("Native S15: %hhd, %hhd, %hhd, %hhd, %hhd, %hhd, %hhd, %hhd, %hhd\n", s.a, s.b, s.c, s.d, s.e, s.f, s.g, s.h, s.i);
    callback(s);
}
EXPORT(void) NATIVEAPI InvokeCallback16(PFNACTION16 callback, S16 s)
{
    printf("Native S16: %hhd, %hd\n", s.x, s.y);
    callback(s);
}
EXPORT(void) NATIVEAPI InvokeCallback17(PFNACTION17 callback, S17 s)
{
    printf("Native S17: %f, %f\n", s.x, s.y);
    callback(s);
}
EXPORT(void) NATIVEAPI InvokeCallback18(PFNACTION18 callback, S18 s)
{
    printf("Native S18: %f, %d, %f\n", s.x, s.y, s.z);
    callback(s);
}
EXPORT(void) NATIVEAPI InvokeCallback19(PFNACTION19 callback, S19 s)
{
    printf("Native S19: %d, %f, %d, %f\n", s.x, s.y, s.z, s.w);
    callback(s);
}

EXPORT(void) NATIVEAPI InvokeCallback20(PFNACTION20 callback, S20 s)
{
#ifdef __GNUC__
    printf("Native S20: %lld, %lld, %lld, %lld\n", s.x, s.y, s.z, s.w);
#else
    printf("Native S20: %I64d, %I64d, %I64d, %I64d\n", s.x, s.y, s.z, s.w);
#endif

    callback(s);
}

EXPORT(void) NATIVEAPI InvokeCallback28(PFNACTION28 callback, S28 s)
{
    printf("Native S28: %p object, %d\n", s.x, s.y);
    callback(s);
}
EXPORT(void) NATIVEAPI InvokeCallback29(PFNACTION29 callback, S29 s)
{
    printf("Native S29: %d, %p object\n", s.x, s.y);
    callback(s);
}

EXPORT(void) NATIVEAPI InvokeCallback30(PFNACTION30 callback, S30 s1, S30 s2, S30 s3)
{
    printf("Native S30: %lld, %lld, %lld, %lld, %lld, %lld\n", s1.x, s1.y, s2.x, s2.y, s3.x, s3.y);
    callback(s1, s2, s3);
}

EXPORT(S1) NATIVEAPI InvokeCallback1R(PFNACTION1 callback, S1 s)
{
    printf("Native S1: %d, %d, %d, %d\n", s.x, s.y, s.z, s.w);
    callback(s);
    return s;
}

EXPORT(S2) NATIVEAPI InvokeCallback2R(PFNACTION2 callback, S2 s)
{
    printf("Native S2: %d, %d, %f\n", s.x, s.y, s.z);
    callback(s);
    return s;
}
EXPORT(S3) NATIVEAPI InvokeCallback3R(PFNACTION3 callback, S3 s)
{
    printf("Native S3: %d, %d, %f\n", s.x, s.y, s.z);
    callback(s);
    return s;
}
EXPORT(S4) NATIVEAPI InvokeCallback4R(PFNACTION4 callback, S4 s)
{
    printf("Native S4: %d, %f\n", s.x, s.y);
    callback(s);
    return s;
}
EXPORT(S5) NATIVEAPI InvokeCallback5R(PFNACTION5 callback, S5 s)
{
    printf("Native S5: %d, %f\n", s.x, s.y);
    callback(s);
    return s;
}
EXPORT(S6) NATIVEAPI InvokeCallback6R(PFNACTION6 callback, S6 s)
{
    printf("Native S6: %hd, %hd, %d, %d\n", s.x, s.y, s.z, s.w);
    callback(s);
    return s;
}
EXPORT(S7) NATIVEAPI InvokeCallback7R(PFNACTION7 callback, S7 s)
{
    printf("Native S7: %f, %d, %d\n", s.x, s.y, s.z);
    callback(s);
    return s;
}
EXPORT(S8) NATIVEAPI InvokeCallback8R(PFNACTION8 callback, S8 s)
{
    printf("Native S8: %f, %d\n", s.x, s.y);
    callback(s);
    return s;
}
EXPORT(S9) NATIVEAPI InvokeCallback9R(PFNACTION9 callback, S9 s)
{
    printf("Native S9: %d, %d, %f, %f\n", s.x, s.y, s.z, s.w);
    callback(s);
    return s;
}
EXPORT(S10) NATIVEAPI InvokeCallback10R(PFNACTION10 callback, S10 s)
{
    printf("Native S10: %hhd, %hhd, %hhd, %hhd, %hhd, %hhd, %hhd, %hhd\n", s.a, s.b, s.c, s.d, s.e, s.f, s.g, s.h);
    callback(s);
    return s;
}
EXPORT(S11) NATIVEAPI InvokeCallback11R(PFNACTION11 callback, S11 s)
{
    printf("Native S11: %hhd, %hhd, %hhd, %hhd, %f\n", s.a, s.b, s.c, s.d, s.e);
    callback(s);
    return s;
}
EXPORT(S12) NATIVEAPI InvokeCallback12R(PFNACTION12 callback, S12 s)
{
    printf("Native S12: %hhd, %hhd, %hhd, %hhd, %hhd, %hhd, %hhd, %hhd, %lld\n", s.a, s.b, s.c, s.d, s.e, s.f, s.g, s.h, s.i);
    callback(s);
    return s;
}
EXPORT(S13) NATIVEAPI InvokeCallback13R(PFNACTION13 callback, S13 s)
{
    printf("Native S13: %hhd, %d\n", s.hasValue, s.x);
    callback(s);
    return s;
}
EXPORT(S14) NATIVEAPI InvokeCallback14R(PFNACTION14 callback, S14 s)
{
    printf("Native S13: %hhd, %lld\n", s.x, s.y);
    callback(s);
    return s;
}
EXPORT(S15) NATIVEAPI InvokeCallback15R(PFNACTION15 callback, S15 s)
{
    printf("Native S15: %hhd, %hhd, %hhd, %hhd, %hhd, %hhd, %hhd, %hhd, %hhd\n", s.a, s.b, s.c, s.d, s.e, s.f, s.g, s.h, s.i);
    callback(s);
    return s;
}
EXPORT(S16) NATIVEAPI InvokeCallback16R(PFNACTION16 callback, S16 s)
{
    printf("Native S16: %hhd, %hd\n", s.x, s.y);
    callback(s);
    return s;
}
EXPORT(S17) NATIVEAPI InvokeCallback17R(PFNACTION17 callback, S17 s)
{
    printf("Native S17: %f, %f\n", s.x, s.y);
    callback(s);
    return s;
}
EXPORT(S18) NATIVEAPI InvokeCallback18R(PFNACTION18 callback, S18 s)
{
    printf("Native S18: %f, %d, %f\n", s.x, s.y, s.z);
    callback(s);
    return s;
}
EXPORT(S19) NATIVEAPI InvokeCallback19R(PFNACTION19 callback, S19 s)
{
    printf("Native S19: %d, %f, %d, %f\n", s.x, s.y, s.z, s.w);
    callback(s);
    return s;
}

EXPORT(S20) NATIVEAPI InvokeCallback20R(PFNACTION20 callback, S20 s)
{
#ifdef __GNUC__
    printf("Native S20: %lld, %lld, %lld, %lld\n", s.x, s.y, s.z, s.w);
#else
    printf("Native S20: %I64d, %I64d, %I64d, %I64d\n", s.x, s.y, s.z, s.w);
#endif
    callback(s);
    return s;
}

EXPORT(S28) NATIVEAPI InvokeCallback28R(PFNACTION28 callback, S28 s)
{
    printf("Native S28: %p object, %d\n", s.x, s.y);
    callback(s);
    return s;
}
EXPORT(S29) NATIVEAPI InvokeCallback29R(PFNACTION29 callback, S29 s)
{
    printf("Native S29: %d, %p object\n", s.x, s.y);
    callback(s);
    return s;
}
