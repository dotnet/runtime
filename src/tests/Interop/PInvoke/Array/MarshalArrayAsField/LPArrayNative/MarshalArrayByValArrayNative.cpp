// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include <iostream>
#include <xplatform.h>
#include "MarshalArray.h"

using namespace std;

// MSVC versions before 19.38 generate incorrect code for this file when compiling with /O2
#if defined(_MSC_VER) && (_MSC_VER < 1938)
#pragma optimize("", off)
#endif

/*----------------------------------------------------------------------------
macro definition
----------------------------------------------------------------------------*/

#define INIT_EXPECTED(__type, __size) \
    __type expected[(__size)]; \
    for ( size_t i = 0; i < (__size); ++i) \
    expected[i] = (__type)i

#define INIT_EXPECTED_STRUCT(__type, __size, __array_type) \
    __type *expected = (__type *)CoreClrAlloc( sizeof(__type) ); \
    for ( size_t i = 0; i < (__size); ++i) \
    expected->arr[i] = (__array_type)i

#define EQUALS(__actual, __cActual, __expected) Equals((__actual), (__cActual), (__expected), (int)sizeof(__expected) / sizeof(__expected[0]))

#if defined(_MSC_VER)
#define FUNCTIONNAME __FUNCSIG__
#else
#define FUNCTIONNAME __PRETTY_FUNCTION__
#endif //_MSC_VER


/*----------------------------------------------------------------------------
struct definition
----------------------------------------------------------------------------*/

typedef struct { int32_t		arr[ARRAY_LENGTH]; } S_INTArray;
typedef struct { uint32_t		arr[ARRAY_LENGTH]; } S_UINTArray;
typedef struct { int16_t		arr[ARRAY_LENGTH]; } S_SHORTArray;
typedef struct { uint16_t		arr[ARRAY_LENGTH]; } S_WORDArray;
typedef struct { int64_t		arr[ARRAY_LENGTH]; } S_LONG64Array;

typedef struct { uint64_t	arr[ARRAY_LENGTH]; } S_ULONG64Array;
typedef struct { DOUBLE		arr[ARRAY_LENGTH]; } S_DOUBLEArray;
typedef struct { FLOAT		arr[ARRAY_LENGTH]; } S_FLOATArray;
typedef struct { uint8_t		arr[ARRAY_LENGTH]; } S_BYTEArray;
typedef struct { CHAR		arr[ARRAY_LENGTH]; } S_CHARArray;

typedef struct { LPSTR		arr[ARRAY_LENGTH]; } S_LPSTRArray;
typedef struct { LPCSTR		arr[ARRAY_LENGTH]; } S_LPCSTRArray;
#ifdef _WIN32
typedef struct { BSTR		arr[ARRAY_LENGTH]; } S_BSTRArray;
#endif

//struct array in a struct
typedef struct { TestStruct	arr[ARRAY_LENGTH]; } S_StructArray;
typedef struct { BOOL		arr[ARRAY_LENGTH]; } S_BOOLArray;

enum class TestEnum : int32_t
{
    Red = 1,
    Green,
    Blue
};

typedef struct { TestEnum arr[3]; } EnregisterableNonBlittable;
typedef struct { int32_t i; } SimpleStruct;
typedef struct { SimpleStruct arr[3]; } EnregisterableUserType;

/*----------------------------------------------------------------------------
helper function
----------------------------------------------------------------------------*/

TestStruct* InitTestStruct()
{
    TestStruct *expected = (TestStruct *)CoreClrAlloc( sizeof(TestStruct) * ARRAY_LENGTH );

    for ( int i = 0; i < ARRAY_LENGTH; i++)
    {
        expected[i].x = i;
        expected[i].d = i;
        expected[i].l = i;
        expected[i].str = ToString(i);
    }

    return expected;
}

template<typename T>
BOOL Equals(T *pActual, int cActual, T *pExpected, int cExpected)
{
    if ( pActual == NULL && pExpected == NULL )
        return TRUE;
    else if ( cActual != cExpected )
    {
        printf("WARNING: Test error - %s\n", FUNCTIONNAME);
        printf("Array Length: expected: %d, actual: %d\n", cExpected, cActual);
        return FALSE;
    }

    for ( size_t i = 0; i < ((size_t) cExpected); ++i )
    {
        if ( !IsObjectEquals(pActual[i], pExpected[i]) )
        {
            printf("WARNING: Test error - %s\n", FUNCTIONNAME);
            printf("Array Element Not Equal: index: %d", static_cast<int>(i));
            return FALSE;
        }
    }

    return TRUE;
}

bool TestStructEquals(TestStruct Actual[], TestStruct Expected[])
{
    if ( Actual == NULL && Expected == NULL )
        return true;
    else if ( Actual == NULL && Expected != NULL )
        return false;
    else if ( Actual != NULL && Expected == NULL )
        return false;

    for ( int i = 0; i < ARRAY_LENGTH; ++i )
    {
        if ( !(IsObjectEquals(Actual[i].x, Expected[i].x) &&
            IsObjectEquals(Actual[i].d, Expected[i].d) &&
            IsObjectEquals(Actual[i].l, Expected[i].l) &&
            IsObjectEquals(Actual[i].str, Expected[i].str) ))
        {
            printf("WARNING: Test error - %s\n", FUNCTIONNAME);
            return false;
        }
    }

    return true;
}

/*----------------------------------------------------------------------------

Function

----------------------------------------------------------------------------*/

/*----------------------------------------------------------------------------
marshal sequential strut
----------------------------------------------------------------------------*/
extern "C" DLL_EXPORT BOOL __cdecl TakeIntArraySeqStructByVal( S_INTArray s, int size )
{
    CHECK_PARAM_NOT_EMPTY( s.arr );
    INIT_EXPECTED( int32_t, ARRAY_LENGTH );
    return Equals( s.arr, size, expected, ARRAY_LENGTH );
}

extern "C" DLL_EXPORT BOOL __cdecl TakeUIntArraySeqStructByVal( S_UINTArray s, int size )
{
    CHECK_PARAM_NOT_EMPTY( s.arr );
    INIT_EXPECTED( uint32_t, ARRAY_LENGTH );
    return Equals( s.arr, size, expected, ARRAY_LENGTH );
}

extern "C" DLL_EXPORT BOOL __cdecl TakeShortArraySeqStructByVal( S_SHORTArray s, int size )
{
    CHECK_PARAM_NOT_EMPTY( s.arr );
    INIT_EXPECTED( int16_t, ARRAY_LENGTH );
    return Equals( s.arr, size, expected, ARRAY_LENGTH );
}

extern "C" DLL_EXPORT BOOL __cdecl TakeWordArraySeqStructByVal( S_WORDArray s, int size )
{
    CHECK_PARAM_NOT_EMPTY( s.arr );
    INIT_EXPECTED( uint16_t, ARRAY_LENGTH );
    return Equals( s.arr, size, expected, ARRAY_LENGTH );
}

extern "C" DLL_EXPORT BOOL __cdecl TakeLong64ArraySeqStructByVal( S_LONG64Array s, int size )
{
    CHECK_PARAM_NOT_EMPTY( s.arr );
    INIT_EXPECTED( int64_t, ARRAY_LENGTH );
    return Equals( s.arr, size, expected, ARRAY_LENGTH );
}

extern "C" DLL_EXPORT BOOL __cdecl TakeULong64ArraySeqStructByVal( S_ULONG64Array s, int size )
{
    CHECK_PARAM_NOT_EMPTY( s.arr );
    INIT_EXPECTED( uint64_t, ARRAY_LENGTH );
    return Equals( s.arr, size, expected, ARRAY_LENGTH );
}

extern "C" DLL_EXPORT BOOL __cdecl TakeDoubleArraySeqStructByVal( S_DOUBLEArray s, int size )
{
    CHECK_PARAM_NOT_EMPTY( s.arr );
    INIT_EXPECTED( DOUBLE, ARRAY_LENGTH );
    return Equals( s.arr, size, expected, ARRAY_LENGTH );
}

extern "C" DLL_EXPORT BOOL __cdecl TakeFloatArraySeqStructByVal( S_FLOATArray s, int size )
{
    CHECK_PARAM_NOT_EMPTY( s.arr );
    INIT_EXPECTED( FLOAT, ARRAY_LENGTH );
    return Equals( s.arr, size, expected, ARRAY_LENGTH );
}

extern "C" DLL_EXPORT BOOL __cdecl TakeByteArraySeqStructByVal( S_BYTEArray s, int size )
{
    CHECK_PARAM_NOT_EMPTY( s.arr );
    INIT_EXPECTED( uint8_t, ARRAY_LENGTH );
    return Equals( s.arr, size, expected, ARRAY_LENGTH );
}

extern "C" DLL_EXPORT BOOL __cdecl TakeCharArraySeqStructByVal( S_CHARArray s, int size )
{
    CHECK_PARAM_NOT_EMPTY( s.arr );
    INIT_EXPECTED( CHAR, ARRAY_LENGTH );
    return Equals( s.arr, size, expected, ARRAY_LENGTH );
}

extern "C" DLL_EXPORT BOOL __cdecl TakeLPSTRArraySeqStructByVal( S_LPSTRArray s, int size )
{
    CHECK_PARAM_NOT_EMPTY( s.arr );

    LPSTR expected[ARRAY_LENGTH];
    for ( int i = 0; i < ARRAY_LENGTH; ++i )
        expected[i] = ToString(i);

    return Equals( s.arr, size, expected, ARRAY_LENGTH );
}

extern "C" DLL_EXPORT BOOL __cdecl TakeLPCSTRArraySeqStructByVal( S_LPCSTRArray s, int size )
{
    CHECK_PARAM_NOT_EMPTY( s.arr );

    LPSTR expected[ARRAY_LENGTH];
    for ( int i = 0; i < ARRAY_LENGTH; ++i )
        expected[i] = ToString(i);

    return Equals( s.arr, size, (LPCSTR *)expected, ARRAY_LENGTH );
}

#ifdef _WIN32
extern "C" DLL_EXPORT BOOL __cdecl TakeBSTRArraySeqStructByVal( S_BSTRArray s, int size )
{
    CHECK_PARAM_NOT_EMPTY( s.arr );

    BSTR expected[ARRAY_LENGTH];
    for ( int i = 0; i < ARRAY_LENGTH; ++i )
        expected[i] = ToBSTR(i);

    return Equals( s.arr, size, expected, ARRAY_LENGTH );
}
#endif

extern "C" DLL_EXPORT BOOL __cdecl TakeStructArraySeqStructByVal( S_StructArray s, int size )
{
    CHECK_PARAM_NOT_EMPTY( s.arr );

    TestStruct *expected = InitTestStruct();
    return TestStructEquals( s.arr,expected );
}

extern "C" DLL_EXPORT BOOL __cdecl TakeEnregistrableNonBlittableSeqStructByVal(EnregisterableNonBlittable s, TestEnum values[3])
{
    return s.arr[0] == values[0] && s.arr[1] == values[1] && s.arr[2] == values[2];
}

extern "C" DLL_EXPORT BOOL __cdecl TakeEnregisterableUserTypeStructByVal(EnregisterableUserType s, SimpleStruct values[3])
{
    return s.arr[0].i == values[0].i && s.arr[1].i == values[1].i && s.arr[2].i == values[2].i;
}


/*----------------------------------------------------------------------------
marshal sequential class
----------------------------------------------------------------------------*/
extern "C" DLL_EXPORT BOOL __cdecl TakeIntArraySeqClassByVal( S_INTArray *s, int size )
{
    return TakeIntArraySeqStructByVal( *s, size );
}

extern "C" DLL_EXPORT BOOL __cdecl TakeUIntArraySeqClassByVal( S_UINTArray *s, int size )
{
    return TakeUIntArraySeqStructByVal( *s, size );
}

extern "C" DLL_EXPORT BOOL __cdecl TakeShortArraySeqClassByVal( S_SHORTArray *s, int size )
{
    return TakeShortArraySeqStructByVal( *s, size );
}

extern "C" DLL_EXPORT BOOL __cdecl TakeWordArraySeqClassByVal( S_WORDArray *s, int size )
{
    return TakeWordArraySeqStructByVal( *s, size );
}

extern "C" DLL_EXPORT BOOL __cdecl TakeLong64ArraySeqClassByVal( S_LONG64Array *s, int size )
{
    return TakeLong64ArraySeqStructByVal( *s, size );
}

extern "C" DLL_EXPORT BOOL __cdecl TakeULong64ArraySeqClassByVal( S_ULONG64Array *s, int size )
{
    return TakeULong64ArraySeqStructByVal( *s, size );
}

extern "C" DLL_EXPORT BOOL __cdecl TakeDoubleArraySeqClassByVal( S_DOUBLEArray *s, int size )
{
    return TakeDoubleArraySeqStructByVal( *s, size );
}

extern "C" DLL_EXPORT BOOL __cdecl TakeFloatArraySeqClassByVal( S_FLOATArray *s, int size )
{
    return TakeFloatArraySeqStructByVal( *s, size );
}

extern "C" DLL_EXPORT BOOL __cdecl TakeByteArraySeqClassByVal( S_BYTEArray *s, int size )
{
    return TakeByteArraySeqStructByVal( *s, size );
}

extern "C" DLL_EXPORT BOOL __cdecl TakeCharArraySeqClassByVal( S_CHARArray *s, int size )
{
    return TakeCharArraySeqStructByVal( *s, size );
}

extern "C" DLL_EXPORT BOOL __cdecl TakeLPSTRArraySeqClassByVal( S_LPSTRArray *s, int size )
{
    return TakeLPSTRArraySeqStructByVal( *s, size );
}

extern "C" DLL_EXPORT BOOL __cdecl TakeLPCSTRArraySeqClassByVal( S_LPCSTRArray *s, int size )
{
    return TakeLPCSTRArraySeqStructByVal( *s, size );
}

#ifdef _WIN32
extern "C" DLL_EXPORT BOOL __cdecl TakeBSTRArraySeqClassByVal( S_BSTRArray *s, int size )
{
    return TakeBSTRArraySeqStructByVal( *s, size );
}
#endif

extern "C" DLL_EXPORT BOOL __cdecl TakeStructArraySeqClassByVal( S_StructArray *s, int size )
{
    return TakeStructArraySeqStructByVal( *s, size );
}

/*----------------------------------------------------------------------------
marshal explicit struct
----------------------------------------------------------------------------*/
extern "C" DLL_EXPORT BOOL __cdecl TakeIntArrayExpStructByVal( S_INTArray s, int size )
{
    return TakeIntArraySeqStructByVal( s, size );
}

extern "C" DLL_EXPORT BOOL __cdecl TakeUIntArrayExpStructByVal( S_UINTArray s, int size )
{
    return TakeUIntArraySeqStructByVal( s, size );
}

extern "C" DLL_EXPORT BOOL __cdecl TakeShortArrayExpStructByVal( S_SHORTArray s, int size )
{
    return TakeShortArraySeqStructByVal( s, size );
}

extern "C" DLL_EXPORT BOOL __cdecl TakeWordArrayExpStructByVal( S_WORDArray s, int size )
{
    return TakeWordArraySeqStructByVal( s, size );
}

extern "C" DLL_EXPORT BOOL __cdecl TakeLong64ArrayExpStructByVal( S_LONG64Array s, int size )
{
    return TakeLong64ArraySeqStructByVal( s, size );
}

extern "C" DLL_EXPORT BOOL __cdecl TakeULong64ArrayExpStructByVal( S_ULONG64Array s, int size )
{
    return TakeULong64ArraySeqStructByVal( s, size );
}

extern "C" DLL_EXPORT BOOL __cdecl TakeDoubleArrayExpStructByVal( S_DOUBLEArray s, int size )
{
    return TakeDoubleArraySeqStructByVal( s, size );
}

extern "C" DLL_EXPORT BOOL __cdecl TakeFloatArrayExpStructByVal( S_FLOATArray s, int size )
{
    return TakeFloatArraySeqStructByVal( s, size );
}

extern "C" DLL_EXPORT BOOL __cdecl TakeByteArrayExpStructByVal( S_BYTEArray s, int size )
{
    return TakeByteArraySeqStructByVal( s, size );
}

extern "C" DLL_EXPORT BOOL __cdecl TakeCharArrayExpStructByVal( S_CHARArray s, int size )
{
    return TakeCharArraySeqStructByVal( s, size );
}

extern "C" DLL_EXPORT BOOL __cdecl TakeLPSTRArrayExpStructByVal( S_LPSTRArray s, int size )
{
    return TakeLPSTRArraySeqStructByVal( s, size );
}

extern "C" DLL_EXPORT BOOL __cdecl TakeLPCSTRArrayExpStructByVal( S_LPCSTRArray s, int size )
{
    return TakeLPCSTRArraySeqStructByVal( s, size );
}

#ifdef _WIN32
extern "C" DLL_EXPORT BOOL __cdecl TakeBSTRArrayExpStructByVal( S_BSTRArray s, int size )
{
    return TakeBSTRArraySeqStructByVal( s, size );
}
#endif

extern "C" DLL_EXPORT BOOL __cdecl TakeStructArrayExpStructByVal( S_StructArray s, int size )
{
    return TakeStructArraySeqStructByVal( s, size );
}

/*----------------------------------------------------------------------------
marshal explicit class
----------------------------------------------------------------------------*/
extern "C" DLL_EXPORT BOOL __cdecl TakeIntArrayExpClassByVal( S_INTArray *s, int size )
{
    return TakeIntArraySeqStructByVal( *s, size );
}

extern "C" DLL_EXPORT BOOL __cdecl TakeUIntArrayExpClassByVal( S_UINTArray *s, int size )
{
    return TakeUIntArraySeqStructByVal( *s, size );
}

extern "C" DLL_EXPORT BOOL __cdecl TakeShortArrayExpClassByVal( S_SHORTArray *s, int size )
{
    return TakeShortArraySeqStructByVal( *s, size );
}

extern "C" DLL_EXPORT BOOL __cdecl TakeWordArrayExpClassByVal( S_WORDArray *s, int size )
{
    return TakeWordArraySeqStructByVal( *s, size );
}

extern "C" DLL_EXPORT BOOL __cdecl TakeLong64ArrayExpClassByVal( S_LONG64Array *s, int size )
{
    return TakeLong64ArraySeqStructByVal( *s, size );
}

extern "C" DLL_EXPORT BOOL __cdecl TakeULong64ArrayExpClassByVal( S_ULONG64Array *s, int size )
{
    return TakeULong64ArraySeqStructByVal( *s, size );
}

extern "C" DLL_EXPORT BOOL __cdecl TakeDoubleArrayExpClassByVal( S_DOUBLEArray *s, int size )
{
    return TakeDoubleArraySeqStructByVal( *s, size );
}

extern "C" DLL_EXPORT BOOL __cdecl TakeFloatArrayExpClassByVal( S_FLOATArray *s, int size )
{
    return TakeFloatArraySeqStructByVal( *s, size );
}

extern "C" DLL_EXPORT BOOL __cdecl TakeByteArrayExpClassByVal( S_BYTEArray *s, int size )
{
    return TakeByteArraySeqStructByVal( *s, size );
}

extern "C" DLL_EXPORT BOOL __cdecl TakeCharArrayExpClassByVal( S_CHARArray *s, int size )
{
    return TakeCharArraySeqStructByVal( *s, size );
}

extern "C" DLL_EXPORT BOOL __cdecl TakeLPSTRArrayExpClassByVal( S_LPSTRArray *s, int size )
{
    return TakeLPSTRArraySeqStructByVal( *s, size );
}

extern "C" DLL_EXPORT BOOL __cdecl TakeLPCSTRArrayExpClassByVal( S_LPCSTRArray *s, int size )
{
    return TakeLPCSTRArraySeqStructByVal( *s, size );
}

#ifdef _WIN32
extern "C" DLL_EXPORT BOOL __cdecl TakeBSTRArrayExpClassByVal( S_BSTRArray *s, int size )
{
    return TakeBSTRArraySeqStructByVal( *s, size );
}
#endif

extern "C" DLL_EXPORT BOOL __cdecl TakeStructArrayExpClassByVal( S_StructArray *s, int size )
{
    return TakeStructArraySeqStructByVal( *s, size );
}

/*----------------------------------------------------------------------------
return a struct including a C array
----------------------------------------------------------------------------*/
extern "C" DLL_EXPORT S_INTArray __cdecl S_INTArray_Ret_ByValue()
{
    INIT_EXPECTED_STRUCT( S_INTArray, ARRAY_LENGTH, int32_t );

    return *expected;
}

extern "C" DLL_EXPORT S_INTArray* __cdecl S_INTArray_Ret()
{
    INIT_EXPECTED_STRUCT( S_INTArray, ARRAY_LENGTH, int32_t );

    return expected;
}

extern "C" DLL_EXPORT S_UINTArray* __cdecl S_UINTArray_Ret()
{
    INIT_EXPECTED_STRUCT( S_UINTArray, ARRAY_LENGTH, uint32_t );

    return expected;
}

extern "C" DLL_EXPORT S_SHORTArray* __cdecl S_SHORTArray_Ret()
{
    INIT_EXPECTED_STRUCT( S_SHORTArray, ARRAY_LENGTH, int16_t );

    return expected;
}

extern "C" DLL_EXPORT S_WORDArray* __cdecl S_WORDArray_Ret()
{
    INIT_EXPECTED_STRUCT( S_WORDArray, ARRAY_LENGTH, uint16_t );

    return expected;
}

extern "C" DLL_EXPORT S_LONG64Array* __cdecl S_LONG64Array_Ret()
{
    INIT_EXPECTED_STRUCT( S_LONG64Array, ARRAY_LENGTH, int64_t );

    return expected;
}

extern "C" DLL_EXPORT S_ULONG64Array* __cdecl S_ULONG64Array_Ret()
{
    INIT_EXPECTED_STRUCT( S_ULONG64Array, ARRAY_LENGTH, uint64_t );

    return expected;
}

extern "C" DLL_EXPORT S_DOUBLEArray* __cdecl S_DOUBLEArray_Ret()
{
    INIT_EXPECTED_STRUCT( S_DOUBLEArray, ARRAY_LENGTH, DOUBLE );

    return expected;
}

extern "C" DLL_EXPORT S_FLOATArray* __cdecl S_FLOATArray_Ret()
{
    INIT_EXPECTED_STRUCT( S_FLOATArray, ARRAY_LENGTH, FLOAT );

    return expected;
}

extern "C" DLL_EXPORT S_BYTEArray* __cdecl S_BYTEArray_Ret()
{
    INIT_EXPECTED_STRUCT( S_BYTEArray, ARRAY_LENGTH, uint8_t );

    return expected;
}

extern "C" DLL_EXPORT S_CHARArray* __cdecl S_CHARArray_Ret()
{
    INIT_EXPECTED_STRUCT( S_CHARArray, ARRAY_LENGTH, CHAR );

    return expected;
}

extern "C" DLL_EXPORT S_LPSTRArray* __cdecl S_LPSTRArray_Ret()
{
    S_LPSTRArray *expected = (S_LPSTRArray *)CoreClrAlloc( sizeof(S_LPSTRArray) );
    for ( int i = 0; i < ARRAY_LENGTH; ++i )
        expected->arr[i] = ToString(i);

    return expected;
}

#ifdef _WIN32
extern "C" DLL_EXPORT S_BSTRArray* __cdecl S_BSTRArray_Ret()
{
    S_BSTRArray *expected = (S_BSTRArray *)CoreClrAlloc( sizeof(S_BSTRArray) );
    for ( int i = 0; i < ARRAY_LENGTH; ++i )
        expected->arr[i] = ToBSTR(i);

    return expected;
}
#endif

extern "C" DLL_EXPORT S_StructArray* __cdecl S_StructArray_Ret()
{
    S_StructArray *expected = (S_StructArray *)CoreClrAlloc( sizeof(S_StructArray) );
    for ( int i = 0; i < ARRAY_LENGTH; ++i )
    {
        expected->arr[i].x = i;
        expected->arr[i].d = i;
        expected->arr[i].l = i;
        expected->arr[i].str = ToString(i);
    }

    return expected;
}
