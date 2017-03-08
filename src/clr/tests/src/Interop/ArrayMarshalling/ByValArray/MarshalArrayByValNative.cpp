// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#include <xplatform.h>

const int ARRAY_SIZE = 100;
template<typename T> bool IsObjectEquals(T o1, T o2);

/*----------------------------------------------------------------------------
macro definition
----------------------------------------------------------------------------*/

#define ENTERFUNC() printf("========== [%s]\t ==========\n", __FUNCTION__)

#define CHECK_PARAM_NOT_EMPTY(__p) \
    ENTERFUNC(); \
    if ( (__p) == NULL ) \
{ \
    printf("[%s] Error: parameter actual is NULL\n", __FUNCTION__); \
    return false; \
} \
    else 

#define INIT_EXPECTED(__type, __size) \
    __type expected[(__size)]; \
    for ( size_t i = 0; i < (__size); ++i) \
    expected[i] = (__type)i

#define INIT_EXPECTED_STRUCT(__type, __size, __array_type) \
    __type *expected = (__type *)::CoTaskMemAlloc( sizeof(__type) ); \
    for ( size_t i = 0; i < (__size); ++i) \
    expected->arr[i] = (__array_type)i

#define EQUALS(__actual, __cActual, __expected) Equals((__actual), (__cActual), (__expected), (int)sizeof(__expected) / sizeof(__expected[0]))

/*----------------------------------------------------------------------------
struct definition
----------------------------------------------------------------------------*/

typedef struct	{ INT		 arr[ARRAY_SIZE];		}	S_INTArray;
typedef struct  { UINT		 arr[ARRAY_SIZE];		}	S_UINTArray;
typedef struct  { SHORT		 arr[ARRAY_SIZE];		}	S_SHORTArray;
typedef struct  { WORD		 arr[ARRAY_SIZE];		}	S_WORDArray;
typedef struct  { LONG64	 arr[ARRAY_SIZE];		}	S_LONG64Array;

typedef struct  { ULONG64	 arr[ARRAY_SIZE];		}	S_ULONG64Array;
typedef struct  { DOUBLE	 arr[ARRAY_SIZE];		}	S_DOUBLEArray;
typedef struct  { FLOAT		 arr[ARRAY_SIZE];		}	S_FLOATArray;
typedef struct  { BYTE	  	 arr[ARRAY_SIZE];		}	S_BYTEArray;
typedef struct  { CHAR		 arr[ARRAY_SIZE];		}	S_CHARArray;

typedef struct  { DWORD_PTR  arr[ARRAY_SIZE];       }   S_DWORD_PTRArray;

typedef struct  { LPSTR		 arr[ARRAY_SIZE];		}	S_LPSTRArray;
typedef struct  { LPCSTR	 arr[ARRAY_SIZE];		}	S_LPCSTRArray;

//struct array in a struct
typedef struct  { INT		x;			DOUBLE	                   d;
LONG64	l;			LPSTR					 str;		}	TestStruct;

typedef struct  { TestStruct	 arr[ARRAY_SIZE];		}	S_StructArray;

typedef struct  { BOOL		 arr[ARRAY_SIZE];		}	S_BOOLArray;

/*----------------------------------------------------------------------------
helper function
----------------------------------------------------------------------------*/


LPSTR ToString(int i)
{
    CHAR *pBuffer = (CHAR *)::CoTaskMemAlloc(10 * sizeof(CHAR)); // 10 is enough for our case, WCHAR for BSTR
	snprintf(pBuffer, 10, "%d", i);
    return pBuffer;
}



TestStruct* InitTestStruct()
{
    TestStruct *expected = (TestStruct *)CoTaskMemAlloc( sizeof(TestStruct) * ARRAY_SIZE );

    for ( int i = 0; i < ARRAY_SIZE; i++)
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
        printf("WARNING: Test error - %s\n", __FUNCSIG__);        
        return FALSE;
    }

    for ( int i = 0; i < cExpected; ++i )
    {
        if ( !IsObjectEquals(pActual[i], pExpected[i]) )
        {
            printf("WARNING: Test error - %s\n", __FUNCSIG__);            
            return FALSE;
        }
    }

    return TRUE;
}

template<typename T> bool IsObjectEquals(T o1, T o2)
{
    // T::operator== required.
    return o1 == o2;
}

template<>
bool IsObjectEquals(LPSTR o1, LPSTR o2)
{
    size_t cLen1 = strlen(o1);
    size_t cLen2 = strlen(o2);

    if (cLen1 != cLen2 )
    {
        printf("Not equals in %s\n",__FUNCTION__);
        return false;
    }

    return strncmp(o1, o2, cLen1) == 0;
}

template<>
bool IsObjectEquals(LPCSTR o1, LPCSTR o2)
{
    size_t cLen1 = strlen(o1);
    size_t cLen2 = strlen(o2);

    if (cLen1 != cLen2 )
    {
        printf("Not equals in %s\n",__FUNCTION__);
        return false;
    }

    return strncmp(o1, o2, cLen1) == 0;
}


bool TestStructEquals(TestStruct Actual[], TestStruct Expected[])
{
    if ( Actual == NULL && Expected == NULL )
        return true;
    else if ( Actual == NULL && Expected != NULL )
        return false;
    else if ( Actual != NULL && Expected == NULL )
        return false;

    for ( size_t i = 0; i < ARRAY_SIZE; ++i )
    {
        if ( !(IsObjectEquals(Actual[i].x, Expected[i].x) &&
            IsObjectEquals(Actual[i].d, Expected[i].d) &&
            IsObjectEquals(Actual[i].l, Expected[i].l) &&
            IsObjectEquals(Actual[i].str, Expected[i].str) ))
        {
            printf("WARNING: Test error - %s\n", __FUNCSIG__);
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
extern "C" DLL_EXPORT BOOL NATIVEAPI TakeIntArraySeqStructByVal( S_INTArray s, int size )
{
    CHECK_PARAM_NOT_EMPTY( s.arr );
    INIT_EXPECTED( INT, ARRAY_SIZE );
    return Equals( s.arr, size, expected, ARRAY_SIZE );
}

extern "C" DLL_EXPORT BOOL NATIVEAPI TakeUIntArraySeqStructByVal( S_UINTArray s, int size )
{
    CHECK_PARAM_NOT_EMPTY( s.arr );
    INIT_EXPECTED( UINT, ARRAY_SIZE );
    return Equals( s.arr, size, expected, ARRAY_SIZE );
}

extern "C" DLL_EXPORT BOOL NATIVEAPI TakeShortArraySeqStructByVal( S_SHORTArray s, int size )
{
    CHECK_PARAM_NOT_EMPTY( s.arr );
    INIT_EXPECTED( SHORT, ARRAY_SIZE );
    return Equals( s.arr, size, expected, ARRAY_SIZE );
}

extern "C" DLL_EXPORT BOOL NATIVEAPI TakeWordArraySeqStructByVal( S_WORDArray s, int size )
{
    CHECK_PARAM_NOT_EMPTY( s.arr );
    INIT_EXPECTED( WORD, ARRAY_SIZE );
    return Equals( s.arr, size, expected, ARRAY_SIZE );
}

extern "C" DLL_EXPORT BOOL NATIVEAPI TakeLong64ArraySeqStructByVal( S_LONG64Array s, int size )
{
    CHECK_PARAM_NOT_EMPTY( s.arr );
    INIT_EXPECTED( LONG64, ARRAY_SIZE );
    return Equals( s.arr, size, expected, ARRAY_SIZE );
}

extern "C" DLL_EXPORT BOOL NATIVEAPI TakeULong64ArraySeqStructByVal( S_ULONG64Array s, int size )
{
    CHECK_PARAM_NOT_EMPTY( s.arr );
    INIT_EXPECTED( ULONG64, ARRAY_SIZE );
    return Equals( s.arr, size, expected, ARRAY_SIZE );
}

extern "C" DLL_EXPORT BOOL NATIVEAPI TakeDoubleArraySeqStructByVal( S_DOUBLEArray s, int size )
{
    CHECK_PARAM_NOT_EMPTY( s.arr );
    INIT_EXPECTED( DOUBLE, ARRAY_SIZE );
    return Equals( s.arr, size, expected, ARRAY_SIZE );
}

extern "C" DLL_EXPORT BOOL NATIVEAPI TakeFloatArraySeqStructByVal( S_FLOATArray s, int size )
{
    CHECK_PARAM_NOT_EMPTY( s.arr );
    INIT_EXPECTED( FLOAT, ARRAY_SIZE );
    return Equals( s.arr, size, expected, ARRAY_SIZE );
}

extern "C" DLL_EXPORT BOOL NATIVEAPI TakeByteArraySeqStructByVal( S_BYTEArray s, int size )
{
    CHECK_PARAM_NOT_EMPTY( s.arr );
    INIT_EXPECTED( BYTE, ARRAY_SIZE );
    return Equals( s.arr, size, expected, ARRAY_SIZE );
}

extern "C" DLL_EXPORT BOOL NATIVEAPI TakeCharArraySeqStructByVal( S_CHARArray s, int size )
{
    CHECK_PARAM_NOT_EMPTY( s.arr );
    INIT_EXPECTED( CHAR, ARRAY_SIZE );
    return Equals( s.arr, size, expected, ARRAY_SIZE );
}

extern "C" DLL_EXPORT BOOL NATIVEAPI TakeIntPtrArraySeqStructByVal(S_DWORD_PTRArray s, int size)
{
	CHECK_PARAM_NOT_EMPTY(s.arr);
	INIT_EXPECTED( DWORD_PTR, ARRAY_SIZE);
	return Equals(s.arr, size, expected, ARRAY_SIZE);
}

extern "C" DLL_EXPORT BOOL NATIVEAPI TakeLPSTRArraySeqStructByVal( S_LPSTRArray s, int size )
{
    CHECK_PARAM_NOT_EMPTY( s.arr );

    LPSTR expected[ARRAY_SIZE];
    for ( int i = 0; i < ARRAY_SIZE; ++i )
        expected[i] = ToString(i);

    return Equals( s.arr, size, expected, ARRAY_SIZE );
}

extern "C" DLL_EXPORT BOOL NATIVEAPI TakeLPCSTRArraySeqStructByVal( S_LPCSTRArray s, int size )
{
    CHECK_PARAM_NOT_EMPTY( s.arr );

    LPSTR expected[ARRAY_SIZE];
    for ( int i = 0; i < ARRAY_SIZE; ++i )
        expected[i] = ToString(i);

    return Equals( s.arr, size, (LPCSTR *)expected, ARRAY_SIZE );
}



extern "C" DLL_EXPORT BOOL NATIVEAPI TakeStructArraySeqStructByVal( S_StructArray s, int size )
{
    CHECK_PARAM_NOT_EMPTY( s.arr );

    TestStruct *expected = InitTestStruct();
    return TestStructEquals( s.arr,expected );
}

/*----------------------------------------------------------------------------
marshal sequential class
----------------------------------------------------------------------------*/
extern "C" DLL_EXPORT BOOL NATIVEAPI TakeIntArraySeqClassByVal( S_INTArray *s, int size )
{
    return TakeIntArraySeqStructByVal( *s, size );
}

extern "C" DLL_EXPORT BOOL NATIVEAPI TakeUIntArraySeqClassByVal( S_UINTArray *s, int size )
{
    return TakeUIntArraySeqStructByVal( *s, size );
}

extern "C" DLL_EXPORT BOOL NATIVEAPI TakeShortArraySeqClassByVal( S_SHORTArray *s, int size )
{
    return TakeShortArraySeqStructByVal( *s, size );
}

extern "C" DLL_EXPORT BOOL NATIVEAPI TakeWordArraySeqClassByVal( S_WORDArray *s, int size )
{
    return TakeWordArraySeqStructByVal( *s, size );
}

extern "C" DLL_EXPORT BOOL NATIVEAPI TakeLong64ArraySeqClassByVal( S_LONG64Array *s, int size )
{
    return TakeLong64ArraySeqStructByVal( *s, size );
}

extern "C" DLL_EXPORT BOOL NATIVEAPI TakeULong64ArraySeqClassByVal( S_ULONG64Array *s, int size )
{
    return TakeULong64ArraySeqStructByVal( *s, size );
}

extern "C" DLL_EXPORT BOOL NATIVEAPI TakeDoubleArraySeqClassByVal( S_DOUBLEArray *s, int size )
{
    return TakeDoubleArraySeqStructByVal( *s, size );
}

extern "C" DLL_EXPORT BOOL NATIVEAPI TakeFloatArraySeqClassByVal( S_FLOATArray *s, int size )
{
    return TakeFloatArraySeqStructByVal( *s, size );
}

extern "C" DLL_EXPORT BOOL NATIVEAPI TakeByteArraySeqClassByVal( S_BYTEArray *s, int size )
{
    return TakeByteArraySeqStructByVal( *s, size );
}

extern "C" DLL_EXPORT BOOL NATIVEAPI TakeCharArraySeqClassByVal( S_CHARArray *s, int size )
{
    return TakeCharArraySeqStructByVal( *s, size );
}

extern "C" DLL_EXPORT BOOL NATIVEAPI TakeLPSTRArraySeqClassByVal( S_LPSTRArray *s, int size )
{
    return TakeLPSTRArraySeqStructByVal( *s, size );
}

extern "C" DLL_EXPORT BOOL NATIVEAPI TakeLPCSTRArraySeqClassByVal( S_LPCSTRArray *s, int size )
{
    return TakeLPCSTRArraySeqStructByVal( *s, size );
}



extern "C" DLL_EXPORT BOOL NATIVEAPI TakeStructArraySeqClassByVal( S_StructArray *s, int size )
{
    return TakeStructArraySeqStructByVal( *s, size );
}

/*----------------------------------------------------------------------------
marshal explicit struct
----------------------------------------------------------------------------*/
extern "C" DLL_EXPORT BOOL NATIVEAPI TakeIntArrayExpStructByVal( S_INTArray s, int size )
{
    return TakeIntArraySeqStructByVal( s, size );
}

extern "C" DLL_EXPORT BOOL NATIVEAPI TakeUIntArrayExpStructByVal( S_UINTArray s, int size )
{
    return TakeUIntArraySeqStructByVal( s, size );
}

extern "C" DLL_EXPORT BOOL NATIVEAPI TakeShortArrayExpStructByVal( S_SHORTArray s, int size )
{
    return TakeShortArraySeqStructByVal( s, size );
}

extern "C" DLL_EXPORT BOOL NATIVEAPI TakeWordArrayExpStructByVal( S_WORDArray s, int size )
{
    return TakeWordArraySeqStructByVal( s, size );
}

extern "C" DLL_EXPORT BOOL NATIVEAPI TakeLong64ArrayExpStructByVal( S_LONG64Array s, int size )
{
    return TakeLong64ArraySeqStructByVal( s, size );
}

extern "C" DLL_EXPORT BOOL NATIVEAPI TakeULong64ArrayExpStructByVal( S_ULONG64Array s, int size )
{
    return TakeULong64ArraySeqStructByVal( s, size );
}

extern "C" DLL_EXPORT BOOL NATIVEAPI TakeDoubleArrayExpStructByVal( S_DOUBLEArray s, int size )
{
    return TakeDoubleArraySeqStructByVal( s, size );
}

extern "C" DLL_EXPORT BOOL NATIVEAPI TakeFloatArrayExpStructByVal( S_FLOATArray s, int size )
{
    return TakeFloatArraySeqStructByVal( s, size );
}

extern "C" DLL_EXPORT BOOL NATIVEAPI TakeByteArrayExpStructByVal( S_BYTEArray s, int size )
{
    return TakeByteArraySeqStructByVal( s, size );
}

extern "C" DLL_EXPORT BOOL NATIVEAPI TakeCharArrayExpStructByVal( S_CHARArray s, int size )
{
    return TakeCharArraySeqStructByVal( s, size );
}

extern "C" DLL_EXPORT BOOL NATIVEAPI TakeLPSTRArrayExpStructByVal( S_LPSTRArray s, int size )
{
    return TakeLPSTRArraySeqStructByVal( s, size );
}

extern "C" DLL_EXPORT BOOL NATIVEAPI TakeLPCSTRArrayExpStructByVal( S_LPCSTRArray s, int size )
{
    return TakeLPCSTRArraySeqStructByVal( s, size );
}



extern "C" DLL_EXPORT BOOL NATIVEAPI TakeStructArrayExpStructByVal( S_StructArray s, int size )
{
    return TakeStructArraySeqStructByVal( s, size );
}

/*----------------------------------------------------------------------------
marshal explicit class
----------------------------------------------------------------------------*/
extern "C" DLL_EXPORT BOOL NATIVEAPI TakeIntArrayExpClassByVal( S_INTArray *s, int size )
{
    return TakeIntArraySeqStructByVal( *s, size );
}

extern "C" DLL_EXPORT BOOL NATIVEAPI TakeUIntArrayExpClassByVal( S_UINTArray *s, int size )
{
    return TakeUIntArraySeqStructByVal( *s, size );
}

extern "C" DLL_EXPORT BOOL NATIVEAPI TakeShortArrayExpClassByVal( S_SHORTArray *s, int size )
{
    return TakeShortArraySeqStructByVal( *s, size );
}

extern "C" DLL_EXPORT BOOL NATIVEAPI TakeWordArrayExpClassByVal( S_WORDArray *s, int size )
{
    return TakeWordArraySeqStructByVal( *s, size );
}

extern "C" DLL_EXPORT BOOL NATIVEAPI TakeLong64ArrayExpClassByVal( S_LONG64Array *s, int size )
{
    return TakeLong64ArraySeqStructByVal( *s, size );
}

extern "C" DLL_EXPORT BOOL NATIVEAPI TakeULong64ArrayExpClassByVal( S_ULONG64Array *s, int size )
{
    return TakeULong64ArraySeqStructByVal( *s, size );
}

extern "C" DLL_EXPORT BOOL NATIVEAPI TakeDoubleArrayExpClassByVal( S_DOUBLEArray *s, int size )
{
    return TakeDoubleArraySeqStructByVal( *s, size );
}

extern "C" DLL_EXPORT BOOL NATIVEAPI TakeFloatArrayExpClassByVal( S_FLOATArray *s, int size )
{
    return TakeFloatArraySeqStructByVal( *s, size );
}

extern "C" DLL_EXPORT BOOL NATIVEAPI TakeByteArrayExpClassByVal( S_BYTEArray *s, int size )
{
    return TakeByteArraySeqStructByVal( *s, size );
}

extern "C" DLL_EXPORT BOOL NATIVEAPI TakeCharArrayExpClassByVal( S_CHARArray *s, int size )
{
    return TakeCharArraySeqStructByVal( *s, size );
}

extern "C" DLL_EXPORT BOOL NATIVEAPI TakeLPSTRArrayExpClassByVal( S_LPSTRArray *s, int size )
{
    return TakeLPSTRArraySeqStructByVal( *s, size );
}

extern "C" DLL_EXPORT BOOL NATIVEAPI TakeLPCSTRArrayExpClassByVal( S_LPCSTRArray *s, int size )
{
    return TakeLPCSTRArraySeqStructByVal( *s, size );
}



extern "C" DLL_EXPORT BOOL NATIVEAPI TakeStructArrayExpClassByVal( S_StructArray *s, int size )
{
    return TakeStructArraySeqStructByVal( *s, size );
}

/*----------------------------------------------------------------------------
return a struct including a C array
----------------------------------------------------------------------------*/
extern "C" DLL_EXPORT S_INTArray* NATIVEAPI S_INTArray_Ret()
{
    INIT_EXPECTED_STRUCT( S_INTArray, ARRAY_SIZE, INT );

    return expected;
}

extern "C" DLL_EXPORT S_UINTArray* NATIVEAPI S_UINTArray_Ret()
{
    INIT_EXPECTED_STRUCT( S_UINTArray, ARRAY_SIZE, UINT );

    return expected;
}

extern "C" DLL_EXPORT S_SHORTArray* NATIVEAPI S_SHORTArray_Ret()
{
    INIT_EXPECTED_STRUCT( S_SHORTArray, ARRAY_SIZE, SHORT );

    return expected;
}

extern "C" DLL_EXPORT S_WORDArray* NATIVEAPI S_WORDArray_Ret()
{
    INIT_EXPECTED_STRUCT( S_WORDArray, ARRAY_SIZE, WORD );

    return expected;
}

extern "C" DLL_EXPORT S_LONG64Array* NATIVEAPI S_LONG64Array_Ret()
{
    INIT_EXPECTED_STRUCT( S_LONG64Array, ARRAY_SIZE, LONG64 );

    return expected;
}

extern "C" DLL_EXPORT S_ULONG64Array* NATIVEAPI S_ULONG64Array_Ret()
{
    INIT_EXPECTED_STRUCT( S_ULONG64Array, ARRAY_SIZE, ULONG64 );

    return expected;
}

extern "C" DLL_EXPORT S_DOUBLEArray* NATIVEAPI S_DOUBLEArray_Ret()
{
    INIT_EXPECTED_STRUCT( S_DOUBLEArray, ARRAY_SIZE, DOUBLE );

    return expected;
}

extern "C" DLL_EXPORT S_FLOATArray* NATIVEAPI S_FLOATArray_Ret()
{
    INIT_EXPECTED_STRUCT( S_FLOATArray, ARRAY_SIZE, FLOAT );

    return expected;
}

extern "C" DLL_EXPORT S_BYTEArray* NATIVEAPI S_BYTEArray_Ret()
{
    INIT_EXPECTED_STRUCT( S_BYTEArray, ARRAY_SIZE, BYTE );

    return expected;
}

extern "C" DLL_EXPORT S_CHARArray* NATIVEAPI S_CHARArray_Ret()
{
    INIT_EXPECTED_STRUCT( S_CHARArray, ARRAY_SIZE, CHAR );

    return expected;
}

extern "C" DLL_EXPORT S_LPSTRArray* NATIVEAPI S_LPSTRArray_Ret()
{        
    S_LPSTRArray *expected = (S_LPSTRArray *)::CoTaskMemAlloc( sizeof(S_LPSTRArray) );
    for ( int i = 0; i < ARRAY_SIZE; ++i )
        expected->arr[i] = ToString(i);

    return expected;
}


extern "C" DLL_EXPORT S_StructArray* NATIVEAPI S_StructArray_Ret()
{
    S_StructArray *expected = (S_StructArray *)::CoTaskMemAlloc( sizeof(S_StructArray) );
    for ( int i = 0; i < ARRAY_SIZE; ++i )
    {
        expected->arr[i].x = i;
        expected->arr[i].d = i;
        expected->arr[i].l = i;
        expected->arr[i].str = ToString(i);
    }

    return expected;
}
