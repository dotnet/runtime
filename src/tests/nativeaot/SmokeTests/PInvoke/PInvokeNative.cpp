// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include <stdlib.h>
#include <stdio.h>
#include <string.h>
#include <stdint.h>
#ifdef TARGET_WINDOWS
#include <windows.h>
#include <wtypes.h>
#define DLL_EXPORT extern "C" __declspec(dllexport)
#else
#include<errno.h>
#define HANDLE size_t
#define DLL_EXPORT extern "C" __attribute((visibility("default")))
#endif

#ifndef TARGET_WINDOWS
#define __stdcall
#endif

#ifndef TARGET_WINDOWS
#define __cdecl
#endif

#if (_MSC_VER >= 1400)         // Check MSC version
#pragma warning(push)
#pragma warning(disable: 4996) // Disable deprecation
#endif

void* MemAlloc(long bytes)
{
#ifdef TARGET_WINDOWS
    return (unsigned char *)CoTaskMemAlloc(bytes);
#else
    return (unsigned char *)malloc(bytes);
#endif
}

void MemFree(void *p)
{
#ifdef TARGET_WINDOWS
    CoTaskMemFree(p);
#else
    free(p);
#endif
}

DLL_EXPORT int __stdcall Square(int intValue)
{
    return intValue * intValue;
}

DLL_EXPORT int __stdcall IsTrue(bool value)
{
    if (value == true)
        return 1;
    return 0;
}

DLL_EXPORT int __stdcall CheckIncremental(int *array, int sz)
{
    if (array == NULL)
        return 1;

    for (int i = 0; i < sz; i++)
    {
        if (array[i] != i)
            return 1;
    }
    return 0;
}

struct Foo
{
    int a;
    float b;
};

DLL_EXPORT int __stdcall CheckIncremental_Foo(Foo *array, int sz)
{
    if (array == NULL)
        return 1;

    for (int i = 0; i < sz; i++)
    {
        if (array[i].a != i || array[i].b != i)
            return 1;
    }
    return 0;
}

DLL_EXPORT int __stdcall Inc(int *val)
{
    if (val == NULL)
        return -1;

    *val = *val + 1;
    return 0;
}

DLL_EXPORT int __stdcall VerifyByRefFoo(Foo *val)
{
    if (val->a != 10)
        return -1;
    if (val->b != 20)
        return -1;

    val->a++;
    val->b++;

    return 0;
}

static Foo s_foo = { 42, 43.0 };

DLL_EXPORT Foo* __stdcall VerifyByRefFooReturn()
{
    return &s_foo;
}

DLL_EXPORT bool __stdcall GetNextChar(short *value)
{
    if (value == NULL)
        return false;

    *value = *value + 1;
    return true;
}

int CompareAnsiString(const char *val, const char * expected)
{
    return strcmp(val, expected) == 0 ? 1 : 0;
}

int CompareUnicodeString(const unsigned short *val, const unsigned short *expected)
{
    if (val == NULL && expected == NULL)
        return 1;

    if (val == NULL || expected == NULL)
        return 0;
    const unsigned short *p = val;
    const unsigned short *q = expected;

    while (*p  && *q && *p == *q)
    {
        p++;
        q++;
    }
    return *p == 0 && *q == 0;
}

DLL_EXPORT int __stdcall VerifyAnsiString(char *val)
{
    if (val == NULL)
        return 0;

    return CompareAnsiString(val, "Hello World");
}

void CopyAnsiString(char *dst, const char *src)
{
    if (src == NULL || dst == NULL)
        return;

    const char *q = src;
    char *p = dst;
    while (*q)
    {
        *p++ = *q++;
    }
    *p = '\0';
}

DLL_EXPORT int __stdcall VerifyAnsiStringOut(char **val)
{
    if (val == NULL)
        return 0;

    *val = (char*)MemAlloc(sizeof(char) * 12);
    CopyAnsiString(*val, "Hello World");
    return 1;
}

DLL_EXPORT int __stdcall VerifyAnsiStringRef(char **val)
{
    if (val == NULL)
        return 0;

    if (!CompareAnsiString(*val, "Hello World"))
    {
        MemFree(*val);
        return 0;
    }

    *val = (char*)MemAlloc(sizeof(char) * 13);
    CopyAnsiString(*val, "Hello World!");
    return 1;
}

DLL_EXPORT int __stdcall VerifyAnsiStringArray(char **val)
{
    if (val == NULL || *val == NULL)
        return 0;

    return CompareAnsiString(val[0], "Hello") && CompareAnsiString(val[1], "World");
}

void ToUpper(char *val)
{
    if (val == NULL)
        return;
    char *p = val;
    while (*p != '\0')
    {
        if (*p >= 'a' && *p <= 'z')
        {
            *p = *p - 'a' + 'A';
        }
        p++;
    }
}

DLL_EXPORT void __stdcall ToUpper(char **val)
{
    if (val == NULL)
        return;

    ToUpper(val[0]);
    ToUpper(val[1]);
}

DLL_EXPORT int __stdcall VerifyUnicodeString(unsigned short *val)
{
    if (val == NULL)
        return 0;

    unsigned short expected[] = {'H', 'e', 'l', 'l', 'o', ' ', 'W', 'o', 'r', 'l', 'd', 0};

    return CompareUnicodeString(val, expected);
}

DLL_EXPORT int __stdcall VerifyUnicodeStringOut(unsigned short **val)
{
    if (val == NULL)
        return 0;
    unsigned short *p = (unsigned short *)MemAlloc(sizeof(unsigned short) * 12);
    unsigned short expected[] = { 'H', 'e', 'l', 'l', 'o', ' ', 'W', 'o', 'r', 'l', 'd', 0 };
    for (int i = 0; i < 12; i++)
        p[i] = expected[i];

    *val = p;
    return 1;
}

DLL_EXPORT int __stdcall VerifyUnicodeStringRef(unsigned short **val)
{
    if (val == NULL)
        return 0;

    unsigned short expected[] = { 'H', 'e', 'l', 'l', 'o', ' ', 'W', 'o', 'r', 'l', 'd', 0};
    unsigned short *p = expected;
    unsigned short *q = *val;

    if (!CompareUnicodeString(p, q))
        return 0;

    MemFree(*val);

    p = (unsigned short*)MemAlloc(sizeof(unsigned short) * 13);
    int i;
    for (i = 0; i < 11; i++)
        p[i] = expected[i];
    p[i++] = '!';
    p[i] = '\0';
    *val = p;
    return 1;
}

DLL_EXPORT bool __stdcall VerifySizeParamIndex(unsigned char ** arrByte, unsigned char *arrSize)
{
    *arrSize = 10;
    *arrByte = (unsigned char *)MemAlloc(sizeof(unsigned char) * (*arrSize));

    if (*arrByte == NULL)
        return false;

    for (int i = 0; i < *arrSize; i++)
    {
        (*arrByte)[i] = (unsigned char)i;
    }
    return true;
}

DLL_EXPORT bool __stdcall LastErrorTest()
{
    int lasterror;
#ifdef TARGET_WINDOWS
    lasterror = GetLastError();
    SetLastError(12345);
#else
    lasterror = errno;
    errno = 12345;
#endif
    return lasterror == 0;
}

DLL_EXPORT void* __stdcall AllocateMemory(int bytes)
{
    void *mem = malloc(bytes);
    return mem;
}

DLL_EXPORT bool __stdcall ReleaseMemory(void *mem)
{
   free(mem);
   return true;
}

DLL_EXPORT bool __stdcall SafeHandleTest(HANDLE sh, long shValue)
{
    return (long)((size_t)(sh)) == shValue;
}

DLL_EXPORT long __stdcall SafeHandleOutTest(HANDLE **sh)
{
    *sh = (HANDLE *)malloc(100);
    return (long)((size_t)(*sh));
}

DLL_EXPORT long __stdcall SafeHandleRefTest(HANDLE **sh, bool alloc)
{
    if (alloc)
        *sh = (HANDLE *)malloc(100);
    return (long)((size_t)(*sh));
}

DLL_EXPORT bool __stdcall ReversePInvoke_Int(int(__stdcall *fnPtr) (int, int, int, int, int, int, int, int, int, int))
{
    return fnPtr(1, 2, 3, 4, 5, 6, 7, 8, 9, 10) == 55;
}

typedef bool(__stdcall *StringFuncPtr) (char *);
DLL_EXPORT bool __stdcall ReversePInvoke_String(StringFuncPtr fnPtr)
{
    char str[] = "Hello World";
    return fnPtr(str);
}

struct DelegateFieldStruct
{
    StringFuncPtr fnPtr;
};

DLL_EXPORT bool __stdcall ReversePInvoke_DelegateField(DelegateFieldStruct p)
{
    char str[] = "Hello World";
    return p.fnPtr(str);
}

typedef bool(__stdcall *OutStringFuncPtr) (char **);
DLL_EXPORT bool __stdcall ReversePInvoke_OutString(OutStringFuncPtr fnPtr)
{
    char *pResult;
    fnPtr(&pResult);
    return strcmp(pResult, "Hello there!") == 0;
}

typedef bool(__stdcall *ArrayFuncPtr) (int *, size_t sz);
DLL_EXPORT bool __stdcall ReversePInvoke_Array(ArrayFuncPtr fnPtr)
{
    int a[42];
    for (int i = 0; i < 42; i++) a[i] = i;
    return fnPtr(a, 42);
}

bool CheckString(char *str)
{
   return CompareAnsiString(str, "Hello World!") == 1;
}


DLL_EXPORT StringFuncPtr __stdcall GetDelegate()
{
    return CheckString;
}

DLL_EXPORT bool __stdcall Callback(StringFuncPtr *fnPtr)
{
    char str[] = "Hello World";
    if ((*fnPtr)(str) == false)
      return false;
   *fnPtr = CheckString;
   return true;
}

// returns
// -1 if val is null
//  1 if val is "Hello World"
//  0 otherwise
DLL_EXPORT int __stdcall VerifyUnicodeStringBuilder(unsigned short *val)
{
    if (val == NULL)
        return -1;

    if (!VerifyUnicodeString(val))
        return 0;

    for (int i = 0; val[i] != '\0'; i++)
    {
        if ((char)val[i] >= 'a' && (char)val[i] <= 'z')
        {
            val[i] += 'A' - 'a';
        }
    }
    return 1;
}

DLL_EXPORT int __stdcall VerifyUnicodeStringBuilderOut(unsigned short *val)
{
    if (val == NULL)
        return 0;

    unsigned short src[] = { 'H', 'e', 'l', 'l', 'o', ' ', 'W', 'o', 'r', 'l', 'd', 0 };
    for (int i = 0; i < 12; i++)
        val[i] = src[i];

    return 1;
}

DLL_EXPORT int __stdcall VerifyAnsiStringBuilderOut(char *val)
{
    if (val == NULL)
        return 0;

    CopyAnsiString(val, "Hello World!");
    return 1;
}

// returns
// -1 if val is null
//  1 if val is "Hello World"
//  0 otherwise
DLL_EXPORT int __stdcall VerifyAnsiStringBuilder(char *val)
{
    if (val == NULL)
        return -1;

    if (!VerifyAnsiString(val))
        return 0;

    for (int i = 0; val[i] != '\0'; i++)
    {
        if (val[i] >= 'a' && val[i] <= 'z')
        {
             val[i] += 'A' - 'a';
        }
    }
    return 1;
}

DLL_EXPORT int* __stdcall ReversePInvoke_Unused(void(__stdcall *fnPtr) (void))
{
    return 0;
}

struct NativeSequentialStruct
{
    short s;
    int a;
    float b;
    char *str;
};

struct NativeSequentialStruct2
{
    float a;
    int b;
};

DLL_EXPORT bool __stdcall StructTest(NativeSequentialStruct nss)
{
    if (nss.s != 100)
        return false;

    if (nss.a != 1)
        return false;

    if (nss.b != 10.0)
       return false;


    if (!CompareAnsiString(nss.str, "Hello"))
        return false;

    return true;
}

DLL_EXPORT bool __stdcall StructTest_Sequential2(NativeSequentialStruct2 nss)
{
    if (nss.a != 10.0)
        return false;

    if (nss.b != 123)
       return false;

    return true;
}

DLL_EXPORT void __stdcall StructTest_ByRef(NativeSequentialStruct *nss)
{
    nss->a++;
    nss->b++;

    char *p = nss->str;
    while (*p != '\0')
    {
        *p = *p + 1;
        p++;
    }
}

DLL_EXPORT void __stdcall StructTest_ByOut(NativeSequentialStruct *nss)
{
    nss->s = 1;
    nss->a = 1;
    nss->b = 1.0;

    int arrSize = 7;
    char *p;
    p = (char *)MemAlloc(sizeof(char) * arrSize);

    for (int i = 0; i < arrSize; i++)
    {
        *(p + i) = i + '0';
    }
    *(p + arrSize) = '\0';
    nss->str = p;
}

DLL_EXPORT bool __stdcall StructTest_Array(NativeSequentialStruct *nss, int length)
{
    if (nss == NULL)
        return false;

    char expected[16];

    for (int i = 0; i < 3; i++)
    {
        if (nss[i].s != 0)
            return false;
        if (nss[i].a != i)
            return false;
        if (nss[i].b != i*i)
            return false;
        sprintf(expected, "%d", i);

        if (CompareAnsiString(expected, nss[i].str) == 0)
            return false;
    }
    return true;
}



typedef struct {
    int a;
    int b;
    int c;
    short inlineArray[128];
    char inlineString[11];
} inlineStruct;

typedef struct {
    int a;
    unsigned short inlineString[11];
} inlineUnicodeStruct;


DLL_EXPORT bool __stdcall InlineArrayTest(inlineStruct* p, inlineUnicodeStruct *q)
{
    for (short i = 0; i < 128; i++)
    {
        if (p->inlineArray[i] != i)
            return false;
        p->inlineArray[i] = i + 1;
    }

    if (CompareAnsiString(p->inlineString, "Hello") != 1)
       return false;

    unsigned short expected[] = { 'H', 'e', 'l', 'l', 'o', ' ', 'W', 'o', 'r', 'l', 0 };
    if (CompareUnicodeString(q->inlineString, expected) != 1)
        return false;

    q->inlineString[5] = p->inlineString[5] = ' ';
    q->inlineString[6] = p->inlineString[6] = 'W';
    q->inlineString[7] = p->inlineString[7] = 'o';
    q->inlineString[8] = p->inlineString[8] = 'r';
    q->inlineString[9] = p->inlineString[9] = 'l';
    q->inlineString[10] = p->inlineString[10] = 'd';

	return true;
}

struct NativeExplicitStruct
{
    int a;
    char padding1[8];
    float b;
    char padding2[8];
    char *str;
};

DLL_EXPORT bool __stdcall StructTest_Explicit(NativeExplicitStruct nes)
{
    if (nes.a != 100)
        return false;

    if (nes.b != 100.0)
        return false;


    if (!CompareAnsiString(nes.str, "Hello"))
        return false;

    return true;
}

struct NativeNestedStruct
{
    int a;
    NativeExplicitStruct nes;
};

DLL_EXPORT bool __stdcall StructTest_Nested(NativeNestedStruct nns)
{
    if (nns.a != 100)
        return false;

    return StructTest_Explicit(nns.nes);
}

DLL_EXPORT bool __stdcall VerifyAnsiCharArrayIn(char *a)
{
    return CompareAnsiString(a, "Hello World") == 1;
}

DLL_EXPORT bool __stdcall VerifyAnsiCharArrayOut(char *a)
{
    if (a == NULL)
        return false;

    CopyAnsiString(a, "Hello World!");
    return true;
}

DLL_EXPORT bool __stdcall IsNULL(void *a)
{
    return a == NULL;
}

DLL_EXPORT void __cdecl SetLastErrorFunc(int errorCode)
{
#ifdef TARGET_WINDOWS
    SetLastError(errorCode);
#else
    errno = errorCode;
#endif
}
DLL_EXPORT void* __stdcall GetFunctionPointer()
{
    return (void*)&SetLastErrorFunc;
}

typedef struct {
    int c;
    char inlineString[260];
} inlineString;

DLL_EXPORT  bool __stdcall InlineStringTest(inlineString* p)
{
    CopyAnsiString(p->inlineString, "Hello World!");
    return true;
}
struct Callbacks
{
    int(__stdcall *callback0) (void);
    int(__stdcall *callback1) (void);
    int(__stdcall *callback2) (void);
};

DLL_EXPORT bool __stdcall RegisterCallbacks(Callbacks *callbacks)
{
    return callbacks->callback0() == 0 && callbacks->callback1() == 1 && callbacks->callback2() == 2;
}

DLL_EXPORT int __stdcall ValidateSuccessCall(int errorCode)
{
    return errorCode;
}

DLL_EXPORT int __stdcall ValidateIntResult(int errorCode, int* result)
{
    *result = 42;
    return errorCode;
}

#ifndef DECIMAL_NEG // defined in wtypes.h
typedef struct tagDEC {
    uint16_t wReserved;
    union {
        struct {
            uint8_t scale;
            uint8_t sign;
        };
        uint16_t signscale;
    };
    uint32_t Hi32;
    union {
        struct {
            uint32_t Lo32;
            uint32_t Mid32;
        };
        uint64_t Lo64;
    };
} DECIMAL;
#endif

DLL_EXPORT DECIMAL __stdcall DecimalTest(DECIMAL value)
{
    DECIMAL zero;
    memset(&zero, 0, sizeof(DECIMAL));

    if (value.Lo32 != 100) {
        return zero;
    }

    if (value.Mid32 != 101) {
        return zero;
    }

    if (value.Hi32 != 102) {
        return zero;
    }

    if (value.sign != 0) {
        return zero;
    }

    if (value.scale != 1) {
        return zero;
    }

    value.sign = 128;
    value.scale = 2;
    value.Lo32 = 99;
    value.Mid32 = 98;
    value.Hi32 = 97;

    return value;
}

#if (_MSC_VER >= 1400)         // Check MSC version
#pragma warning(pop)           // Renable previous depreciations
#endif
