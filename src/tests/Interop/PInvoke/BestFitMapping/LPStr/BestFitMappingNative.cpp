// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include <stdlib.h>
#include <stdio.h>
#include <string.h>
#include <xplatform.h>

typedef struct TLPStr_Test_Struct
{
    LPSTR pStr;
} LPStr_Test_Struct;

typedef struct TLPStr_Test_Class
{
    LPSTR pStr;
} LPStr_Test_Class;

typedef struct TLPStrTestStructOfArrays
{
    LPSTR pStr1;
    LPSTR pStr2;
} LPStrTestStructOfArrays;

extern "C" bool DLL_EXPORT STDMETHODCALLTYPE LPStrBuffer_In_String(LPSTR pStr)
{
    printf ("xx %s \n", pStr);

    return TRUE;
}

extern "C" bool DLL_EXPORT STDMETHODCALLTYPE LPStrBuffer_InByRef_String(LPSTR* ppStr)
{
    printf ("yy %s \n", *ppStr);

    return TRUE;
}

extern "C" bool DLL_EXPORT STDMETHODCALLTYPE LPStrBuffer_InOutByRef_String(LPSTR* ppStr)
{
    printf ("zz %s \n", *ppStr);

    return TRUE;
}

extern "C" bool DLL_EXPORT STDMETHODCALLTYPE LPStrBuffer_In_StringBuilder(LPSTR pStr)
{
    return TRUE;
}

extern "C" bool DLL_EXPORT STDMETHODCALLTYPE LPStrBuffer_InByRef_StringBuilder(LPSTR* ppStr)
{
    return TRUE;
}

extern "C" bool DLL_EXPORT STDMETHODCALLTYPE LPStrBuffer_InOutByRef_StringBuilder(LPSTR* ppStr)
{
    return TRUE;
}

extern "C" bool DLL_EXPORT STDMETHODCALLTYPE LPStrBuffer_In_Struct_String (LPStr_Test_Struct strStruct)
{
    return TRUE;
}

extern "C" bool DLL_EXPORT STDMETHODCALLTYPE LPStrBuffer_InByRef_Struct_String (LPStr_Test_Struct* pSstrStruct)
{
    return TRUE;
}

extern "C" bool DLL_EXPORT STDMETHODCALLTYPE LPStrBuffer_InOutByRef_Struct_String (LPStr_Test_Struct* pStrStruct)
{
    return TRUE;
}

extern "C" bool DLL_EXPORT STDMETHODCALLTYPE LPStrBuffer_In_Array_String (LPSTR str[]) 
{
    printf ("%s \n", str[0]);
    printf ("%s \n", str[1]);
    printf ("%s \n", str[2]);

    return TRUE;
}

extern "C" bool DLL_EXPORT STDMETHODCALLTYPE LPStrBuffer_InByRef_Array_String (LPSTR* str[]) 
{
    printf ("%s \n", (*str)[0]);
    printf ("%s \n", (*str)[1]);
    printf ("%s \n", (*str)[2]);

    return TRUE;
}

extern "C" bool DLL_EXPORT STDMETHODCALLTYPE LPStrBuffer_InOutByRef_Array_String (LPSTR* str[]) 
{
    printf ("%s \n", (*str)[0]);
    printf ("%s \n", (*str)[1]);
    printf ("%s \n", (*str)[2]);

    return TRUE;
}

extern "C" bool DLL_EXPORT STDMETHODCALLTYPE LPStrBuffer_In_Class_String (LPStr_Test_Class strClass)
{
    return TRUE;
}

extern "C" bool DLL_EXPORT STDMETHODCALLTYPE LPStrBuffer_InByRef_Class_String (LPStr_Test_Class* pSstrClass)
{
    return TRUE;
}

extern "C" bool DLL_EXPORT STDMETHODCALLTYPE LPStrBuffer_InOutByRef_Class_String (LPStr_Test_Class* pStrClass)
{
    return TRUE;
}

extern "C" bool DLL_EXPORT STDMETHODCALLTYPE LPStrBuffer_In_Array_Struct (LPStr_Test_Struct str[]) 
{
    printf ("** %s \n", str[0].pStr);
    printf ("** %s \n", str[1].pStr);

    return TRUE;
}

extern "C" bool DLL_EXPORT STDMETHODCALLTYPE LPStrBuffer_InByRef_Array_Struct (LPStr_Test_Struct* str[]) 
{
    printf ("++ %s \n", (*str)[0].pStr);
    printf ("++ %s \n", (*str)[1].pStr);

    return TRUE;
}

extern "C" bool DLL_EXPORT STDMETHODCALLTYPE LPStrBuffer_InOutByRef_Array_Struct (LPStr_Test_Struct* str[]) 
{
    printf ("-- %s \n", (*str)[0].pStr);
    printf ("-- %s \n", (*str)[1].pStr);

    return TRUE;
}

extern "C" bool DLL_EXPORT STDMETHODCALLTYPE LPStrBuffer_In_Struct_String_nothrow (LPStr_Test_Struct strStruct)
{
    return TRUE;
}
