// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#pragma once
#include <stdio.h>
#include <stdlib.h> // required by itoa
#include <iostream>
#include "platformdefines.h"

//////////////////////////////////////////////////////////////////////////////
// Macro definitions
//////////////////////////////////////////////////////////////////////////////
#define ARRAY_LENGTH 100
#define ROWS 2
#define COLUMNS 3

#define ELEM_PER_ROW_2D(__arr) (&(__arr[1][0]) - &(__arr[0][0]))
#define ROWS_2D(__arr) sizeof(__arr) / (ELEM_PER_ROW_2D(__arr) * sizeof(__arr[0][0]))

#define ENTERFUNC() printf("============ [%s]\t ============\n", __FUNCTION__)

#define CHECK_PARAM_NOT_EMPTY(__p) \
    ENTERFUNC(); \
    if ( (__p) == NULL ) \
    { \
    printf("[%s] Error: parameter actual is NULL\n", __FUNCTION__); \
    return false; \
    }

#define CHECK_PARAM_EMPTY(__p) \
    ENTERFUNC(); \
    if ( __p != NULL ) \
    { \
    printf("[%s] Error: parameter ppActual is not NULL\n", __FUNCTION__); \
    return false; \
    }

#if defined(_MSC_VER)
#define FUNCTIONNAME __FUNCSIG__
#else
#define FUNCTIONNAME __PRETTY_FUNCTION__
#endif //_MSC_VER

#define VERIFY_ERROR(__expect, __actual) \
    std::cout << '[' << FUNCTIONNAME << "] EXPECT: " << (__expect) << ", ACTUAL: " << (__actual) << std::endl

#define TRACE(__msg) \
    std::cout << __msg << std::endl

#define VERIFY_ERROR_MSG(__msg, __expect, __actual) \
    printf("["##FUNCTIONNAME##"] "##__msg, (__expect), (__actual))

//////////////////////////////////////////////////////////////////////////////
// Verify helper methods
//////////////////////////////////////////////////////////////////////////////

template<typename T>
bool IsObjectEquals(const T& o1, const T& o2)
{
    return o1 == o2;
}

template<>
bool IsObjectEquals(const LPSTR& o1, const LPSTR& o2)
{
    if (o1 == NULL)
    {
        return (o2 == NULL);
    }
    else if (o2 == NULL)
    {
        return false;
    }

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
bool IsObjectEquals(const LPCSTR& o1, const LPCSTR& o2)
{
    if (o1 == NULL)
    {
        return (o2 == NULL);
    }
    else if (o2 == NULL)
    {
        return false;
    }

    size_t cLen1 = strlen(o1);
    size_t cLen2 = strlen(o2);

    if (cLen1 != cLen2 )
    {
        printf("Not equals in %s\n",__FUNCTION__);
        return false;
    }

    return strncmp(o1, o2, cLen1) == 0;
}

#ifdef _WIN32
template<>
bool IsObjectEquals(const BSTR& o1, const BSTR& o2)
{
    if (o1 == NULL)
    {
        return (o2 == NULL);
    }
    else if (o2 == NULL)
    {
        return false;
    }

    uint32_t uLen1 = SysStringLen(o1);
    uint32_t uLen2 = SysStringLen(o2);

    if (uLen1 != uLen2 )
    {
        printf("Not equals in %s\n",__FUNCTION__);
        return false;
    }

    return memcmp(o1, o2, uLen1 * sizeof(*o1)) == 0;
}
#endif

//////////////////////////////////////////////////////////////////////////////
// Test Data Structure
//////////////////////////////////////////////////////////////////////////////

LPSTR ToString(int i)
{
    CHAR *pBuffer = (CHAR *)CoreClrAlloc(10 * sizeof(CHAR)); // 10 is enough for our case
    _itoa_s(i, pBuffer, 10, 10);

    return pBuffer;
}

#ifdef _WIN32
BSTR ToBSTR(int i)
{
    BSTR bstrRet = NULL;
    VarBstrFromI4(i, 0, 0, &bstrRet);

    return bstrRet;
}
#endif

struct TestStruct
{
    inline TestStruct()
        : x(0)
        , d(0)
        , l(0)
        , str(NULL)
    {
    }

    inline TestStruct(int v)
        : x(v)
        , d(v)
        , l(v)
    {
        str = ToString(v);
    }

    int x;
    double d;
    int64_t l;
    LPSTR str;

    inline bool operator==(const TestStruct &other) const
    {
        return IsObjectEquals(x, other.x) &&
            IsObjectEquals(d, other.d) &&
            IsObjectEquals(l, other.l) &&
            IsObjectEquals(str, other.str);
    }
};

typedef struct S2
{
    int32_t i32;
    uint32_t ui32;
    int16_t s1;
    uint16_t us1;
    uint8_t b;
    CHAR sb;
    int16_t i16;
    uint16_t ui16;
    int64_t i64;
    uint64_t ui64;
    FLOAT sgl;
    DOUBLE d;
}S2;

//////////////////////////////////////////////////////////////////////////////
// Other methods
//////////////////////////////////////////////////////////////////////////////

#ifdef _WIN32
// Do not output variants, and suppress the boring template compile warning
std::ostream &operator<<(std::ostream &ostr, VARIANT &v)
{
    return ostr;
}

std::ostream &operator<<(std::ostream &ostr, BSTR &b)
{
    std::wcout << b;
    return ostr;
}
#endif

//////////////////////////////////method for struct S2////////////////////////////////////////////////
void InstanceS2(S2 * pREC, int i32,uint32_t ui32,int16_t s1,uint16_t us1,uint8_t b,CHAR sb,int16_t i16,uint16_t ui16,
                int64_t i64,uint64_t ui64,FLOAT sgl,DOUBLE d)
{
    pREC->i32 = i32;
    pREC->ui32 = ui32;
    pREC->s1 = s1;
    pREC->us1 = us1;
    pREC->b = b;
    pREC->sb = sb;
    pREC->i16 = i16;
    pREC->ui16 = ui16;
    pREC->i64 = i64;
    pREC->ui64 = ui64;
    pREC->sgl = sgl;
    pREC->d = d;
}

bool ValidateS2LPArray(S2 * pREC, S2 * pRECCorrect, int numArrElement)
{
    for(int i = 0; i<numArrElement;i++)
    {
        if(pREC[i].i32 != pRECCorrect[i].i32)
        {
            printf("\t The field of int is not the expected!");
            printf("\t\tpREC[%d].i32 = %d\n", i, pREC[i].i32);
            printf("\t\tpRECCorrect[%d].i32 = %d\n", i, pRECCorrect[i].i32);
            return false;
        }
        else if(pREC[i].ui32 != pRECCorrect[i].ui32)
        {
            printf("\t The field of UInt32 is not the expected!");
            printf("\t\tpREC[%d].ui32 = %d\n", i, pREC[i].ui32);
            printf("\t\tpRECCorrect[%d].ui32 = %u\n", i, pRECCorrect[i].ui32);
            return false;
        }
        else if(pREC[i].s1 != pRECCorrect[i].s1)
        {
            printf("\t The field of int16_t is not the expected!");
            printf("\t\tpREC[%d].s1 = %d\n", i, pREC[i].s1);
            printf("\t\tpRECCorrect[%d].s1 = %d\n", i, pRECCorrect[i].s1);
            return false;
        }
        else if(pREC[i].us1 != pRECCorrect[i].us1)
        {
            printf("\t The field of ushort is not the expected!");
            printf("\t\tpREC[%d].us1 = %d\n", i, pREC[i].us1);
            printf("\t\tpRECCorrect[%d].us1 = %u\n", i, pRECCorrect[i].us1);
            return false;
        }
        else if(pREC[i].b != pRECCorrect[i].b)
        {
            printf("\t The field of byte is not the expected!");
            printf("\t\tpREC[%d].b = %d\n", i, pREC[i].b);
            printf("\t\tpRECCorrect[%d].b = %d\n", i, pRECCorrect[i].b);
            return false;
        }
        else if(pREC[i].sb != pRECCorrect[i].sb)
        {
            printf("\t The field of sbyte is not the expected!");
            printf("\t\tpREC[%d].sb = %d\n", i, pREC[i].sb);
            printf("\t\tpRECCorrect[%d].sb = %u\n", i, pRECCorrect[i].sb);
            return false;
        }
        else if(pREC[i].i16 != pRECCorrect[i].i16)
        {
            printf("\t The field of Int16 is not the expected!");
            printf("\t\tpREC[%d].i16 = %d\n", i, pREC[i].i16);
            printf("\t\tpRECCorrect[%d].i16 = %d\n", i, pRECCorrect[i].i16);
            return false;
        }
        else if(pREC[i].ui16 != pRECCorrect[i].ui16)
        {
            printf("\t The field of UInt16 is not the expected!");
            printf("\t\tpREC[%d].ui16 = %d\n", i, pREC[i].ui16);
            printf("\t\tpRECCorrect[%d].ui16 = %u\n", i, pRECCorrect[i].ui16);
            return false;
        }
        else if(pREC[i].i64 != pRECCorrect[i].i64)
        {
            printf("\t The field of Int64 is not the expected!");
            printf("\t\tpREC[%d].i64 = %" PRIi64 "\n", i, pREC[i].i64);
            printf("\t\tpRECCorrect[%d].i64 = %" PRIi64 "\n", i, pRECCorrect[i].i64);
            return false;
        }
        else if(pREC[i].ui64 != pRECCorrect[i].ui64)
        {
            printf("\t The field of UInt64 is not the expected!");
            printf("\t\tpREC[%d].ui64 = %" PRIi64 "\n", i, pREC[i].ui64);
            printf("\t\tpRECCorrect[%d].ui64 = %" PRIi64 "\n", i, pRECCorrect[i].ui64);
            return false;
        }
        else if(pREC[i].sgl != pRECCorrect[i].sgl)
        {
            printf("\t The field of single is not the expected!");
            printf("\t\tpREC[%d].sgl = %f\n", i, pREC[i].sgl);
            printf("\t\tpRECCorrect[%d].sgl = %f\n", i, pRECCorrect[i].sgl);
            return false;
        }
        else if(pREC[i].d != pRECCorrect[i].d)
        {
            printf("\t The field of double is not the expected!");
            printf("\t\tpREC[%d].d = %f\n", i, pREC[i].d);
            printf("\t\tpRECCorrect[%d].d = %f\n", i, pRECCorrect[i].d);
            return false;
        }
    }
    return true;
}
