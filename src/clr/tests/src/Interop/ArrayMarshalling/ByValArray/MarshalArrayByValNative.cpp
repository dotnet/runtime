// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#include <xplatform.h>
#include <platformdefines.h>
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
    CHAR *pBuffer = (CHAR *)CoreClrAlloc(10 * sizeof(CHAR)); // 10 is enough for our case, WCHAR for BSTR
	snprintf(pBuffer, 10, "%d", i);
    return pBuffer;
}



TestStruct* InitTestStruct()
{
    TestStruct *expected = (TestStruct *)CoreClrAlloc( sizeof(TestStruct) * ARRAY_SIZE );

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
        printf("WARNING: Test error - %s\n", FUNCTIONNAME);
        return FALSE;
    }

    for ( int i = 0; i < cExpected; ++i )
    {
        if ( !IsObjectEquals(pActual[i], pExpected[i]) )
        {
            printf("WARNING: Test error - %s\n", FUNCTIONNAME);
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
extern "C" DLL_EXPORT BOOL STDMETHODCALLTYPE TakeIntArraySeqStructByVal( S_INTArray s, int size )
{
    CHECK_PARAM_NOT_EMPTY( s.arr );
    INIT_EXPECTED( INT, ARRAY_SIZE );
    return Equals( s.arr, size, expected, ARRAY_SIZE );
}

extern "C" DLL_EXPORT BOOL STDMETHODCALLTYPE TakeUIntArraySeqStructByVal( S_UINTArray s, int size )
{
    CHECK_PARAM_NOT_EMPTY( s.arr );
    INIT_EXPECTED( UINT, ARRAY_SIZE );
    return Equals( s.arr, size, expected, ARRAY_SIZE );
}

extern "C" DLL_EXPORT BOOL STDMETHODCALLTYPE TakeShortArraySeqStructByVal( S_SHORTArray s, int size )
{
    CHECK_PARAM_NOT_EMPTY( s.arr );
    INIT_EXPECTED( SHORT, ARRAY_SIZE );
    return Equals( s.arr, size, expected, ARRAY_SIZE );
}

extern "C" DLL_EXPORT BOOL STDMETHODCALLTYPE TakeWordArraySeqStructByVal( S_WORDArray s, int size )
{
    CHECK_PARAM_NOT_EMPTY( s.arr );
    INIT_EXPECTED( WORD, ARRAY_SIZE );
    return Equals( s.arr, size, expected, ARRAY_SIZE );
}

extern "C" DLL_EXPORT BOOL STDMETHODCALLTYPE TakeLong64ArraySeqStructByVal( S_LONG64Array s, int size )
{
    CHECK_PARAM_NOT_EMPTY( s.arr );
    INIT_EXPECTED( LONG64, ARRAY_SIZE );
    return Equals( s.arr, size, expected, ARRAY_SIZE );
}

extern "C" DLL_EXPORT BOOL STDMETHODCALLTYPE TakeULong64ArraySeqStructByVal( S_ULONG64Array s, int size )
{
    CHECK_PARAM_NOT_EMPTY( s.arr );
    INIT_EXPECTED( ULONG64, ARRAY_SIZE );
    return Equals( s.arr, size, expected, ARRAY_SIZE );
}

extern "C" DLL_EXPORT BOOL STDMETHODCALLTYPE TakeDoubleArraySeqStructByVal( S_DOUBLEArray s, int size )
{
    CHECK_PARAM_NOT_EMPTY( s.arr );
    INIT_EXPECTED( DOUBLE, ARRAY_SIZE );
    return Equals( s.arr, size, expected, ARRAY_SIZE );
}

extern "C" DLL_EXPORT BOOL STDMETHODCALLTYPE TakeFloatArraySeqStructByVal( S_FLOATArray s, int size )
{
    CHECK_PARAM_NOT_EMPTY( s.arr );
    INIT_EXPECTED( FLOAT, ARRAY_SIZE );
    return Equals( s.arr, size, expected, ARRAY_SIZE );
}

extern "C" DLL_EXPORT BOOL STDMETHODCALLTYPE TakeByteArraySeqStructByVal( S_BYTEArray s, int size )
{
    CHECK_PARAM_NOT_EMPTY( s.arr );
    INIT_EXPECTED( BYTE, ARRAY_SIZE );
    return Equals( s.arr, size, expected, ARRAY_SIZE );
}

extern "C" DLL_EXPORT BOOL STDMETHODCALLTYPE TakeCharArraySeqStructByVal( S_CHARArray s, int size )
{
    CHECK_PARAM_NOT_EMPTY( s.arr );
    INIT_EXPECTED( CHAR, ARRAY_SIZE );
    return Equals( s.arr, size, expected, ARRAY_SIZE );
}

extern "C" DLL_EXPORT BOOL STDMETHODCALLTYPE TakeIntPtrArraySeqStructByVal(S_DWORD_PTRArray s, int size)
{
	CHECK_PARAM_NOT_EMPTY(s.arr);
	INIT_EXPECTED( DWORD_PTR, ARRAY_SIZE);
	return Equals(s.arr, size, expected, ARRAY_SIZE);
}

extern "C" DLL_EXPORT BOOL STDMETHODCALLTYPE TakeLPSTRArraySeqStructByVal( S_LPSTRArray s, int size )
{
    CHECK_PARAM_NOT_EMPTY( s.arr );

    LPSTR expected[ARRAY_SIZE];
    for ( int i = 0; i < ARRAY_SIZE; ++i )
        expected[i] = ToString(i);

    return Equals( s.arr, size, expected, ARRAY_SIZE );
}

extern "C" DLL_EXPORT BOOL STDMETHODCALLTYPE TakeLPCSTRArraySeqStructByVal( S_LPCSTRArray s, int size )
{
    CHECK_PARAM_NOT_EMPTY( s.arr );

    LPSTR expected[ARRAY_SIZE];
    for ( int i = 0; i < ARRAY_SIZE; ++i )
        expected[i] = ToString(i);

    return Equals( s.arr, size, (LPCSTR *)expected, ARRAY_SIZE );
}



extern "C" DLL_EXPORT BOOL STDMETHODCALLTYPE TakeStructArraySeqStructByVal( S_StructArray s, int size )
{
    CHECK_PARAM_NOT_EMPTY( s.arr );

    TestStruct *expected = InitTestStruct();
    return TestStructEquals( s.arr,expected );
}

/*----------------------------------------------------------------------------
marshal sequential class
----------------------------------------------------------------------------*/
extern "C" DLL_EXPORT BOOL STDMETHODCALLTYPE TakeIntArraySeqClassByVal( S_INTArray *s, int size )
{
    return TakeIntArraySeqStructByVal( *s, size );
}

extern "C" DLL_EXPORT BOOL STDMETHODCALLTYPE TakeUIntArraySeqClassByVal( S_UINTArray *s, int size )
{
    return TakeUIntArraySeqStructByVal( *s, size );
}

extern "C" DLL_EXPORT BOOL STDMETHODCALLTYPE TakeShortArraySeqClassByVal( S_SHORTArray *s, int size )
{
    return TakeShortArraySeqStructByVal( *s, size );
}

extern "C" DLL_EXPORT BOOL STDMETHODCALLTYPE TakeWordArraySeqClassByVal( S_WORDArray *s, int size )
{
    return TakeWordArraySeqStructByVal( *s, size );
}

extern "C" DLL_EXPORT BOOL STDMETHODCALLTYPE TakeLong64ArraySeqClassByVal( S_LONG64Array *s, int size )
{
    return TakeLong64ArraySeqStructByVal( *s, size );
}

extern "C" DLL_EXPORT BOOL STDMETHODCALLTYPE TakeULong64ArraySeqClassByVal( S_ULONG64Array *s, int size )
{
    return TakeULong64ArraySeqStructByVal( *s, size );
}

extern "C" DLL_EXPORT BOOL STDMETHODCALLTYPE TakeDoubleArraySeqClassByVal( S_DOUBLEArray *s, int size )
{
    return TakeDoubleArraySeqStructByVal( *s, size );
}

extern "C" DLL_EXPORT BOOL STDMETHODCALLTYPE TakeFloatArraySeqClassByVal( S_FLOATArray *s, int size )
{
    return TakeFloatArraySeqStructByVal( *s, size );
}

extern "C" DLL_EXPORT BOOL STDMETHODCALLTYPE TakeByteArraySeqClassByVal( S_BYTEArray *s, int size )
{
    return TakeByteArraySeqStructByVal( *s, size );
}

extern "C" DLL_EXPORT BOOL STDMETHODCALLTYPE TakeCharArraySeqClassByVal( S_CHARArray *s, int size )
{
    return TakeCharArraySeqStructByVal( *s, size );
}

extern "C" DLL_EXPORT BOOL STDMETHODCALLTYPE TakeLPSTRArraySeqClassByVal( S_LPSTRArray *s, int size )
{
    return TakeLPSTRArraySeqStructByVal( *s, size );
}

extern "C" DLL_EXPORT BOOL STDMETHODCALLTYPE TakeLPCSTRArraySeqClassByVal( S_LPCSTRArray *s, int size )
{
    return TakeLPCSTRArraySeqStructByVal( *s, size );
}



extern "C" DLL_EXPORT BOOL STDMETHODCALLTYPE TakeStructArraySeqClassByVal( S_StructArray *s, int size )
{
    return TakeStructArraySeqStructByVal( *s, size );
}

/*----------------------------------------------------------------------------
marshal explicit struct
----------------------------------------------------------------------------*/
extern "C" DLL_EXPORT BOOL STDMETHODCALLTYPE TakeIntArrayExpStructByVal( S_INTArray s, int size )
{
    return TakeIntArraySeqStructByVal( s, size );
}

extern "C" DLL_EXPORT BOOL STDMETHODCALLTYPE TakeUIntArrayExpStructByVal( S_UINTArray s, int size )
{
    return TakeUIntArraySeqStructByVal( s, size );
}

extern "C" DLL_EXPORT BOOL STDMETHODCALLTYPE TakeShortArrayExpStructByVal( S_SHORTArray s, int size )
{
    return TakeShortArraySeqStructByVal( s, size );
}

extern "C" DLL_EXPORT BOOL STDMETHODCALLTYPE TakeWordArrayExpStructByVal( S_WORDArray s, int size )
{
    return TakeWordArraySeqStructByVal( s, size );
}

extern "C" DLL_EXPORT BOOL STDMETHODCALLTYPE TakeLong64ArrayExpStructByVal( S_LONG64Array s, int size )
{
    return TakeLong64ArraySeqStructByVal( s, size );
}

extern "C" DLL_EXPORT BOOL STDMETHODCALLTYPE TakeULong64ArrayExpStructByVal( S_ULONG64Array s, int size )
{
    return TakeULong64ArraySeqStructByVal( s, size );
}

extern "C" DLL_EXPORT BOOL STDMETHODCALLTYPE TakeDoubleArrayExpStructByVal( S_DOUBLEArray s, int size )
{
    return TakeDoubleArraySeqStructByVal( s, size );
}

extern "C" DLL_EXPORT BOOL STDMETHODCALLTYPE TakeFloatArrayExpStructByVal( S_FLOATArray s, int size )
{
    return TakeFloatArraySeqStructByVal( s, size );
}

extern "C" DLL_EXPORT BOOL STDMETHODCALLTYPE TakeByteArrayExpStructByVal( S_BYTEArray s, int size )
{
    return TakeByteArraySeqStructByVal( s, size );
}

extern "C" DLL_EXPORT BOOL STDMETHODCALLTYPE TakeCharArrayExpStructByVal( S_CHARArray s, int size )
{
    return TakeCharArraySeqStructByVal( s, size );
}

extern "C" DLL_EXPORT BOOL STDMETHODCALLTYPE TakeLPSTRArrayExpStructByVal( S_LPSTRArray s, int size )
{
    return TakeLPSTRArraySeqStructByVal( s, size );
}

extern "C" DLL_EXPORT BOOL STDMETHODCALLTYPE TakeLPCSTRArrayExpStructByVal( S_LPCSTRArray s, int size )
{
    return TakeLPCSTRArraySeqStructByVal( s, size );
}



extern "C" DLL_EXPORT BOOL STDMETHODCALLTYPE TakeStructArrayExpStructByVal( S_StructArray s, int size )
{
    return TakeStructArraySeqStructByVal( s, size );
}

/*----------------------------------------------------------------------------
marshal explicit class
----------------------------------------------------------------------------*/
extern "C" DLL_EXPORT BOOL STDMETHODCALLTYPE TakeIntArrayExpClassByVal( S_INTArray *s, int size )
{
    return TakeIntArraySeqStructByVal( *s, size );
}

extern "C" DLL_EXPORT BOOL STDMETHODCALLTYPE TakeUIntArrayExpClassByVal( S_UINTArray *s, int size )
{
    return TakeUIntArraySeqStructByVal( *s, size );
}

extern "C" DLL_EXPORT BOOL STDMETHODCALLTYPE TakeShortArrayExpClassByVal( S_SHORTArray *s, int size )
{
    return TakeShortArraySeqStructByVal( *s, size );
}

extern "C" DLL_EXPORT BOOL STDMETHODCALLTYPE TakeWordArrayExpClassByVal( S_WORDArray *s, int size )
{
    return TakeWordArraySeqStructByVal( *s, size );
}

extern "C" DLL_EXPORT BOOL STDMETHODCALLTYPE TakeLong64ArrayExpClassByVal( S_LONG64Array *s, int size )
{
    return TakeLong64ArraySeqStructByVal( *s, size );
}

extern "C" DLL_EXPORT BOOL STDMETHODCALLTYPE TakeULong64ArrayExpClassByVal( S_ULONG64Array *s, int size )
{
    return TakeULong64ArraySeqStructByVal( *s, size );
}

extern "C" DLL_EXPORT BOOL STDMETHODCALLTYPE TakeDoubleArrayExpClassByVal( S_DOUBLEArray *s, int size )
{
    return TakeDoubleArraySeqStructByVal( *s, size );
}

extern "C" DLL_EXPORT BOOL STDMETHODCALLTYPE TakeFloatArrayExpClassByVal( S_FLOATArray *s, int size )
{
    return TakeFloatArraySeqStructByVal( *s, size );
}

extern "C" DLL_EXPORT BOOL STDMETHODCALLTYPE TakeByteArrayExpClassByVal( S_BYTEArray *s, int size )
{
    return TakeByteArraySeqStructByVal( *s, size );
}

extern "C" DLL_EXPORT BOOL STDMETHODCALLTYPE TakeCharArrayExpClassByVal( S_CHARArray *s, int size )
{
    return TakeCharArraySeqStructByVal( *s, size );
}

extern "C" DLL_EXPORT BOOL STDMETHODCALLTYPE TakeLPSTRArrayExpClassByVal( S_LPSTRArray *s, int size )
{
    return TakeLPSTRArraySeqStructByVal( *s, size );
}

extern "C" DLL_EXPORT BOOL STDMETHODCALLTYPE TakeLPCSTRArrayExpClassByVal( S_LPCSTRArray *s, int size )
{
    return TakeLPCSTRArraySeqStructByVal( *s, size );
}



extern "C" DLL_EXPORT BOOL STDMETHODCALLTYPE TakeStructArrayExpClassByVal( S_StructArray *s, int size )
{
    return TakeStructArraySeqStructByVal( *s, size );
}

/*----------------------------------------------------------------------------
return a struct including a C array
----------------------------------------------------------------------------*/
extern "C" DLL_EXPORT S_INTArray* STDMETHODCALLTYPE S_INTArray_Ret()
{
    INIT_EXPECTED_STRUCT( S_INTArray, ARRAY_SIZE, INT );

    return expected;
}

extern "C" DLL_EXPORT S_UINTArray* STDMETHODCALLTYPE S_UINTArray_Ret()
{
    INIT_EXPECTED_STRUCT( S_UINTArray, ARRAY_SIZE, UINT );

    return expected;
}

extern "C" DLL_EXPORT S_SHORTArray* STDMETHODCALLTYPE S_SHORTArray_Ret()
{
    INIT_EXPECTED_STRUCT( S_SHORTArray, ARRAY_SIZE, SHORT );

    return expected;
}

extern "C" DLL_EXPORT S_WORDArray* STDMETHODCALLTYPE S_WORDArray_Ret()
{
    INIT_EXPECTED_STRUCT( S_WORDArray, ARRAY_SIZE, WORD );

    return expected;
}

extern "C" DLL_EXPORT S_LONG64Array* STDMETHODCALLTYPE S_LONG64Array_Ret()
{
    INIT_EXPECTED_STRUCT( S_LONG64Array, ARRAY_SIZE, LONG64 );

    return expected;
}

extern "C" DLL_EXPORT S_ULONG64Array* STDMETHODCALLTYPE S_ULONG64Array_Ret()
{
    INIT_EXPECTED_STRUCT( S_ULONG64Array, ARRAY_SIZE, ULONG64 );

    return expected;
}

extern "C" DLL_EXPORT S_DOUBLEArray* STDMETHODCALLTYPE S_DOUBLEArray_Ret()
{
    INIT_EXPECTED_STRUCT( S_DOUBLEArray, ARRAY_SIZE, DOUBLE );

    return expected;
}

extern "C" DLL_EXPORT S_FLOATArray* STDMETHODCALLTYPE S_FLOATArray_Ret()
{
    INIT_EXPECTED_STRUCT( S_FLOATArray, ARRAY_SIZE, FLOAT );

    return expected;
}

extern "C" DLL_EXPORT S_BYTEArray* STDMETHODCALLTYPE S_BYTEArray_Ret()
{
    INIT_EXPECTED_STRUCT( S_BYTEArray, ARRAY_SIZE, BYTE );

    return expected;
}

extern "C" DLL_EXPORT S_CHARArray* STDMETHODCALLTYPE S_CHARArray_Ret()
{
    INIT_EXPECTED_STRUCT( S_CHARArray, ARRAY_SIZE, CHAR );

    return expected;
}

extern "C" DLL_EXPORT S_LPSTRArray* STDMETHODCALLTYPE S_LPSTRArray_Ret()
{        
    S_LPSTRArray *expected = (S_LPSTRArray *)CoreClrAlloc( sizeof(S_LPSTRArray) );
    for ( int i = 0; i < ARRAY_SIZE; ++i )
        expected->arr[i] = ToString(i);

    return expected;
}


extern "C" DLL_EXPORT S_StructArray* STDMETHODCALLTYPE S_StructArray_Ret()
{
    S_StructArray *expected = (S_StructArray *)CoreClrAlloc( sizeof(S_StructArray) );
    for ( int i = 0; i < ARRAY_SIZE; ++i )
    {
        expected->arr[i].x = i;
        expected->arr[i].d = i;
        expected->arr[i].l = i;
        expected->arr[i].str = ToString(i);
    }

    return expected;
}
