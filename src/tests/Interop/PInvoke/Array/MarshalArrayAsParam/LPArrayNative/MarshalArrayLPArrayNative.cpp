// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include <xplatform.h>
#include "MarshalArray.h"

// MSVC versions before 19.38 generate incorrect code for this file when compiling with /O2
#if defined(_MSC_VER) && (_MSC_VER < 1938)
#pragma optimize("", off)
#endif

#if defined(_MSC_VER)
#define FUNCTIONNAME __FUNCSIG__
#else
#define FUNCTIONNAME __PRETTY_FUNCTION__
#endif //_MSC_VER

template<typename T>
bool Equals(T *pActual, int cActual, T *pExpected, int cExpected)
{
    if (pActual == NULL && pExpected == NULL)
        return true;
    else if (pActual == NULL && pExpected != NULL)
        return false;
    else if (pActual != NULL && pExpected == NULL)
        return false;
    else if (cActual != cExpected)
        return false;

    for (int i = 0; i < cExpected; ++i)
    {
        if (!IsObjectEquals(pActual[i], pExpected[i]))
        {
            printf("WARNING: Test error - %s\n", FUNCTIONNAME);
            return false;
        }
    }

    return true;
}

#define EQUALS(__actual, __cActual, __expected) Equals((__actual), (__cActual), (__expected), (int)sizeof(__expected) / sizeof(__expected[0]))
#define INIT_EXPECTED(__type, __size) 	\
    __type expected[(__size)]; \
for (size_t i = 0; i < (__size); ++i) \
    expected[i] = (__type)i

/////////////////////////////////////////// By Value /////////////////////////////////////////
extern "C" DLL_EXPORT BOOL CStyle_Array_Int(int *pActual, int cActual)
{
    CHECK_PARAM_NOT_EMPTY(pActual);

    INIT_EXPECTED(int, ARRAY_LENGTH);

    return EQUALS(pActual, cActual, expected);
}

#ifdef _WIN32
extern "C" DLL_EXPORT BOOL CStyle_Array_Object(VARIANT *pActual, int cActual)
{
    CHECK_PARAM_NOT_EMPTY(pActual);

    //VARIANT expected[ARRAY_LENGTH];
    size_t nullIdx = ARRAY_LENGTH / 2;

    for (size_t i = 0; i < ARRAY_LENGTH; ++i)
    {
        //VariantInit(&expected[i]);
        if (i == nullIdx)
        {
            if ((pActual[i].vt != VT_EMPTY))
            {
                printf("=====EMPTY VALUE NOT FOUND==== %s\n", __FUNCTION__);
                return FALSE;
            }
            else
            {
                continue;
            }
        }
        if ((pActual[i].vt != VT_I4) && ((size_t)(pActual[i].lVal) != i))
        {
            printf("====VARIANTS NOT EQUAL==== %s\n", __FUNCTION__);
            return TRUE;
        }
    }

    return TRUE;
    //EQUALS(pActual, cActual, expected);
}
#endif

extern "C" DLL_EXPORT BOOL CStyle_Array_Uint(uint32_t *pActual, int cActual)
{
    CHECK_PARAM_NOT_EMPTY(pActual);

    INIT_EXPECTED(uint32_t, ARRAY_LENGTH);

    return EQUALS(pActual, cActual, expected);
}

extern "C" DLL_EXPORT BOOL CStyle_Array_Short(int16_t *pActual, int cActual)
{
    CHECK_PARAM_NOT_EMPTY(pActual);

    INIT_EXPECTED(int16_t, ARRAY_LENGTH);

    return EQUALS(pActual, cActual, expected);
}

extern "C" DLL_EXPORT BOOL CStyle_Array_Word(uint16_t *pActual, int cActual)
{
    CHECK_PARAM_NOT_EMPTY(pActual);

    INIT_EXPECTED(uint16_t, ARRAY_LENGTH);

    return EQUALS(pActual, cActual, expected);
}

extern "C" DLL_EXPORT BOOL CStyle_Array_Long64(int64_t *pActual, int cActual)
{
    CHECK_PARAM_NOT_EMPTY(pActual);

    INIT_EXPECTED(int64_t, ARRAY_LENGTH);

    return EQUALS(pActual, cActual, expected);
}

extern "C" DLL_EXPORT BOOL CStyle_Array_ULong64(uint64_t *pActual, int cActual)
{
    CHECK_PARAM_NOT_EMPTY(pActual);

    INIT_EXPECTED(uint64_t, ARRAY_LENGTH);

    return EQUALS(pActual, cActual, expected);
}

extern "C" DLL_EXPORT BOOL CStyle_Array_Double(DOUBLE *pActual, int cActual)
{
    CHECK_PARAM_NOT_EMPTY(pActual);

    INIT_EXPECTED(DOUBLE, ARRAY_LENGTH);

    return EQUALS(pActual, cActual, expected);
}

extern "C" DLL_EXPORT BOOL CStyle_Array_Float(FLOAT *pActual, int cActual)
{
    CHECK_PARAM_NOT_EMPTY(pActual);

    INIT_EXPECTED(FLOAT, ARRAY_LENGTH);

    return EQUALS(pActual, cActual, expected);
}

extern "C" DLL_EXPORT BOOL CStyle_Array_Byte(uint8_t *pActual, int cActual)
{
    CHECK_PARAM_NOT_EMPTY(pActual);

    INIT_EXPECTED(uint8_t, ARRAY_LENGTH);

    return EQUALS(pActual, cActual, expected);
}

extern "C" DLL_EXPORT BOOL CStyle_Array_Char(CHAR *pActual, int cActual)
{
    CHECK_PARAM_NOT_EMPTY(pActual);

    INIT_EXPECTED(CHAR, ARRAY_LENGTH);

    return EQUALS(pActual, cActual, expected);
}

extern "C" DLL_EXPORT BOOL CStyle_Array_LPCSTR(LPCSTR *pActual, int cActual)
{
    CHECK_PARAM_NOT_EMPTY(pActual);

    LPSTR expected[ARRAY_LENGTH];
    size_t nullIdx = ARRAY_LENGTH / 2;
    for (size_t i = 0; i < ARRAY_LENGTH; ++i)
    {
        if (i == nullIdx)
        {
            expected[i] = NULL;
            continue;
        }
        expected[i] = ToString((int)i);
    }

    int retval = EQUALS((LPSTR *)pActual, cActual, expected);

    for (size_t i = 0; i < ARRAY_LENGTH; ++i)
    {
        if (i == nullIdx)
            continue;

        CoreClrFree(expected[i]);
    }

    return retval;
}

extern "C" DLL_EXPORT BOOL CStyle_Array_LPSTR(LPSTR *pActual, int cActual)
{
    CHECK_PARAM_NOT_EMPTY(pActual);

    return CStyle_Array_LPCSTR((LPCSTR *)pActual, cActual);
}

extern "C" DLL_EXPORT BOOL CStyle_Array_Struct(TestStruct *pActual, int cActual)
{
    CHECK_PARAM_NOT_EMPTY(pActual);

    TestStruct expected[ARRAY_LENGTH];
    for (size_t i = 0; i < ARRAY_LENGTH; ++i)
    {
        expected[i].x = (int)i;
        expected[i].d = (int)i;
        expected[i].l = (int64_t)i;
        expected[i].str = ToString((int)i);
    }

    return EQUALS(pActual, cActual, expected);
}

extern "C" DLL_EXPORT BOOL CStyle_Array_Bool(BOOL *pActual, int cActual)
{
    CHECK_PARAM_NOT_EMPTY(pActual);

    BOOL expected[ARRAY_LENGTH];
    for (size_t i = 0; i < ARRAY_LENGTH; ++i)
    {
        if (i % 2 == 0)
            expected[i] = TRUE;
        else
            expected[i] = FALSE;
    }

    return EQUALS(pActual, cActual, expected);
}

/////////////////////////////////////////// In Out By Value /////////////////////////////////////////
template<typename T>
void ChangeArrayValue(T *pArray, int cSize)
{
    for (int i = 0; i < cSize; ++i)
        pArray[i] = (T)(cSize - 1 - i);
}

template<>
void ChangeArrayValue(LPSTR *pArray, int cSize)
{
    for (int i = 0; i < cSize; ++i)
    {
        // Free resource
        CoreClrFree(pArray[i]);
        pArray[i] = ToString(cSize - 1 - i);
    }

    int nullIdx = cSize / 2 - 1;
    CoreClrFree(pArray[nullIdx]);
    pArray[nullIdx] = NULL;
}

template<>
void ChangeArrayValue(LPCSTR *pArray, int cSize)
{
    for (int i = 0; i < cSize; ++i)
    {
        // Free resource
        CoreClrFree((LPVOID)pArray[i]);
        pArray[i] = ToString(cSize - 1 - i);
    }

    int nullIdx = cSize / 2 - 1;
    CoreClrFree((LPVOID)pArray[nullIdx]);
    pArray[nullIdx] = NULL;
}

#ifdef _WIN32
template<>
void ChangeArrayValue(VARIANT *pArray, int cSize)
{
    for (int i = 0; i < cSize; ++i)
    {
        // Free resource
        VariantClear(&pArray[i]);
        pArray[i].vt = VT_I4;
        pArray[i].lVal = cSize - 1 - i;
    }

    int nullIdx = cSize / 2 - 1;
    VariantClear(&pArray[nullIdx]);
    pArray[nullIdx].vt = VT_EMPTY;
}
#endif

template<>
void ChangeArrayValue(TestStruct *pArray, int cSize)
{
    for (int i = 0; i < cSize; ++i)
    {
        int v = (cSize - 1 - i);
        pArray[i].x = v;
        pArray[i].d = v;
        pArray[i].l = v;
        pArray[i].str = ToString(v);
    }
}

extern "C" DLL_EXPORT BOOL CStyle_Array_Int_InOut(int *pActual, int cActual)
{
	CHECK_PARAM_NOT_EMPTY(pActual);

	BOOL retval = CStyle_Array_Int(pActual, cActual);
	ChangeArrayValue(pActual, cActual);

	return retval;
}

extern "C" DLL_EXPORT BOOL CStyle_Array_Int_InOut_Null(int *pActual)
{
	CHECK_PARAM_EMPTY(pActual);
    return true;
}

extern "C" DLL_EXPORT BOOL CStyle_Array_Int_InOut_ZeroLength(int *pActual)
{
	CHECK_PARAM_NOT_EMPTY(pActual);
	return true;
}

#ifdef _WIN32
extern "C" DLL_EXPORT BOOL CStyle_Array_Object_InOut(VARIANT *pActual, int cActual)
{
    CHECK_PARAM_NOT_EMPTY(pActual);

    BOOL retval = CStyle_Array_Object(pActual, cActual);
    ChangeArrayValue(pActual, cActual);

    return retval;
}
#endif

extern "C" DLL_EXPORT BOOL CStyle_Array_Uint_InOut(uint32_t *pActual, int cActual)
{
    CHECK_PARAM_NOT_EMPTY(pActual);

    BOOL retval = CStyle_Array_Uint(pActual, cActual);
    ChangeArrayValue(pActual, cActual);

    return retval;
}

extern "C" DLL_EXPORT BOOL CStyle_Array_Short_InOut(int16_t *pActual, int cActual)
{
    CHECK_PARAM_NOT_EMPTY(pActual);

    BOOL retval = CStyle_Array_Short(pActual, cActual);
    ChangeArrayValue(pActual, cActual);

    return retval;
}

extern "C" DLL_EXPORT BOOL CStyle_Array_Word_InOut(uint16_t *pActual, int cActual)
{
    CHECK_PARAM_NOT_EMPTY(pActual);

    BOOL retval = CStyle_Array_Word(pActual, cActual);
    ChangeArrayValue(pActual, cActual);

    return retval;
}

extern "C" DLL_EXPORT BOOL CStyle_Array_Long64_InOut(int64_t *pActual, int cActual)
{
    CHECK_PARAM_NOT_EMPTY(pActual);

    BOOL retval = CStyle_Array_Long64(pActual, cActual);
    ChangeArrayValue(pActual, cActual);

    return retval;
}

extern "C" DLL_EXPORT BOOL CStyle_Array_ULong64_InOut(uint64_t *pActual, int cActual)
{
    CHECK_PARAM_NOT_EMPTY(pActual);

    BOOL retval = CStyle_Array_ULong64(pActual, cActual);
    ChangeArrayValue(pActual, cActual);

    return retval;
}

extern "C" DLL_EXPORT BOOL CStyle_Array_Double_InOut(DOUBLE *pActual, int cActual)
{
    CHECK_PARAM_NOT_EMPTY(pActual);

    BOOL retval = CStyle_Array_Double(pActual, cActual);
    ChangeArrayValue(pActual, cActual);

    return retval;
}

extern "C" DLL_EXPORT BOOL CStyle_Array_Float_InOut(FLOAT *pActual, int cActual)
{
    CHECK_PARAM_NOT_EMPTY(pActual);

    BOOL retval = CStyle_Array_Float(pActual, cActual);
    ChangeArrayValue(pActual, cActual);

    return retval;
}

extern "C" DLL_EXPORT BOOL CStyle_Array_Byte_InOut(uint8_t *pActual, int cActual)
{
    CHECK_PARAM_NOT_EMPTY(pActual);

    BOOL retval = CStyle_Array_Byte(pActual, cActual);
    ChangeArrayValue(pActual, cActual);

    return retval;
}

extern "C" DLL_EXPORT BOOL CStyle_Array_Char_InOut(CHAR *pActual, int cActual)
{
    CHECK_PARAM_NOT_EMPTY(pActual);

    BOOL retval = CStyle_Array_Char(pActual, cActual);
    ChangeArrayValue(pActual, cActual);

    return retval;
}

extern "C" DLL_EXPORT BOOL CStyle_Array_LPSTR_InOut(LPSTR *pActual, int cActual)
{
    CHECK_PARAM_NOT_EMPTY(pActual);

    BOOL retval = CStyle_Array_LPSTR(pActual, cActual);
    ChangeArrayValue(pActual, cActual);

    return retval;
}

extern "C" DLL_EXPORT BOOL CStyle_Array_Struct_InOut(TestStruct *pActual, int cActual)
{
    CHECK_PARAM_NOT_EMPTY(pActual);

    BOOL retval = CStyle_Array_Struct(pActual, cActual);
    ChangeArrayValue(pActual, cActual);

    return retval;
}

extern "C" DLL_EXPORT BOOL CStyle_Array_Bool_InOut(BOOL *pActual, int cActual)
{
    CHECK_PARAM_NOT_EMPTY(pActual);

    BOOL retval = CStyle_Array_Bool(pActual, cActual);

    for (int i = 0; i < cActual; ++i)
    {
        if (i % 2 != 0)
            pActual[i] = TRUE;
        else
            pActual[i] = FALSE;
    }

    return retval;
}

/////////////////////////////////////////// Out By Value /////////////////////////////////////////
extern "C" DLL_EXPORT BOOL CStyle_Array_Int_Out(int *pActual, int cActual)
{
	CHECK_PARAM_NOT_EMPTY(pActual);

	ChangeArrayValue(pActual, cActual);

    return true;
}
extern "C" DLL_EXPORT BOOL CStyle_Array_Int_Out_Null(int *pActual)
{
	CHECK_PARAM_EMPTY(pActual);
	return true;
}

extern "C" DLL_EXPORT BOOL CStyle_Array_Int_Out_ZeroLength(int *pActual)
{
	CHECK_PARAM_NOT_EMPTY(pActual);
	return true;
}

#ifdef _WIN32
extern "C" DLL_EXPORT BOOL CStyle_Array_Object_Out(VARIANT *pActual, int cActual)
{
    CHECK_PARAM_NOT_EMPTY(pActual);

    ChangeArrayValue(pActual, cActual);

    return true;
}
#endif

extern "C" DLL_EXPORT BOOL CStyle_Array_Uint_Out(uint32_t *pActual, int cActual)
{
    CHECK_PARAM_NOT_EMPTY(pActual);

    ChangeArrayValue(pActual, cActual);

    return true;
}

extern "C" DLL_EXPORT BOOL CStyle_Array_Short_Out(int16_t *pActual, int cActual)
{
    CHECK_PARAM_NOT_EMPTY(pActual);

    ChangeArrayValue(pActual, cActual);

    return true;
}

extern "C" DLL_EXPORT BOOL CStyle_Array_Word_Out(uint16_t *pActual, int cActual)
{
    CHECK_PARAM_NOT_EMPTY(pActual);

    ChangeArrayValue(pActual, cActual);

    return true;
}

extern "C" DLL_EXPORT BOOL CStyle_Array_Long64_Out(int64_t *pActual, int cActual)
{
    CHECK_PARAM_NOT_EMPTY(pActual);

    ChangeArrayValue(pActual, cActual);

    return true;
}

extern "C" DLL_EXPORT BOOL CStyle_Array_ULong64_Out(uint64_t *pActual, int cActual)
{
    CHECK_PARAM_NOT_EMPTY(pActual);

    ChangeArrayValue(pActual, cActual);

    return true;
}

extern "C" DLL_EXPORT BOOL CStyle_Array_Double_Out(DOUBLE *pActual, int cActual)
{
    CHECK_PARAM_NOT_EMPTY(pActual);

    ChangeArrayValue(pActual, cActual);

    return true;
}

extern "C" DLL_EXPORT BOOL CStyle_Array_Float_Out(FLOAT *pActual, int cActual)
{
    CHECK_PARAM_NOT_EMPTY(pActual);

    ChangeArrayValue(pActual, cActual);

    return true;
}

extern "C" DLL_EXPORT BOOL CStyle_Array_Byte_Out(uint8_t *pActual, int cActual)
{
    CHECK_PARAM_NOT_EMPTY(pActual);

    ChangeArrayValue(pActual, cActual);

    return true;
}

extern "C" DLL_EXPORT BOOL CStyle_Array_Char_Out(CHAR *pActual, int cActual)
{
    CHECK_PARAM_NOT_EMPTY(pActual);

    ChangeArrayValue(pActual, cActual);

    return true;
}

extern "C" DLL_EXPORT BOOL CStyle_Array_LPSTR_Out(LPSTR *pActual, int cActual)
{
    CHECK_PARAM_NOT_EMPTY(pActual);

    ChangeArrayValue(pActual, cActual);

    return true;
}

extern "C" DLL_EXPORT BOOL CStyle_Array_Struct_Out(TestStruct *pActual, int cActual)
{
    CHECK_PARAM_NOT_EMPTY(pActual);

    ChangeArrayValue(pActual, cActual);

    return true;
}

extern "C" DLL_EXPORT BOOL CStyle_Array_Bool_Out(BOOL *pActual, int cActual)
{
    CHECK_PARAM_NOT_EMPTY(pActual);

    for (int i = 0; i < cActual; ++i)
    {
        if (i % 2 != 0)
            pActual[i] = TRUE;
        else
            pActual[i] = FALSE;
    }

    return true;
}

/////////////////////////////////////////// InAttribute ByRef /////////////////////////////////////////
extern "C" DLL_EXPORT BOOL CStyle_Array_Int_In_Ref(int **ppActual, int cActual)
{
    if (!CStyle_Array_Int(*ppActual, cActual))
    {
        return false;
    }
    ChangeArrayValue(*ppActual, cActual);
    return true;
}

#ifdef _WIN32
extern "C" DLL_EXPORT BOOL CStyle_Array_Object_In_Ref(VARIANT **ppActual, int cActual)
{
    if (!CStyle_Array_Object(*ppActual, cActual))
        return false;

    ChangeArrayValue(*ppActual, cActual);
    return true;
}
#endif

extern "C" DLL_EXPORT BOOL CStyle_Array_Uint_In_Ref(uint32_t **ppActual, int cActual)
{
    if (!CStyle_Array_Uint(*ppActual, cActual))
        return false;

    ChangeArrayValue(*ppActual, cActual);
    return true;
}

extern "C" DLL_EXPORT BOOL CStyle_Array_Short_In_Ref(int16_t **ppActual, int cActual)
{
    if (!CStyle_Array_Short(*ppActual, cActual))
        return false;

    ChangeArrayValue(*ppActual, cActual);
    return true;
}

extern "C" DLL_EXPORT BOOL CStyle_Array_Word_In_Ref(uint16_t **ppActual, int cActual)
{
    if (!CStyle_Array_Word(*ppActual, cActual))
        return false;

    ChangeArrayValue(*ppActual, cActual);
    return true;
}

extern "C" DLL_EXPORT BOOL CStyle_Array_Long64_In_Ref(int64_t **ppActual, int cActual)
{
    if (!CStyle_Array_Long64(*ppActual, cActual))
        return false;

    ChangeArrayValue(*ppActual, cActual);
    return true;
}

extern "C" DLL_EXPORT BOOL CStyle_Array_ULong64_In_Ref(uint64_t **ppActual, int cActual)
{
    if (!CStyle_Array_ULong64(*ppActual, cActual))
        return false;

    ChangeArrayValue(*ppActual, cActual);
    return true;
}

extern "C" DLL_EXPORT BOOL CStyle_Array_Double_In_Ref(DOUBLE **ppActual, int cActual)
{
    if (!CStyle_Array_Double(*ppActual, cActual))
        return false;

    ChangeArrayValue(*ppActual, cActual);
    return true;
}

extern "C" DLL_EXPORT BOOL CStyle_Array_Float_In_Ref(FLOAT **ppActual, int cActual)
{
    if (!CStyle_Array_Float(*ppActual, cActual))
        return false;

    ChangeArrayValue(*ppActual, cActual);
    return true;
}

extern "C" DLL_EXPORT BOOL CStyle_Array_Byte_In_Ref(uint8_t **ppActual, int cActual)
{
    if (!CStyle_Array_Byte(*ppActual, cActual))
        return false;

    ChangeArrayValue(*ppActual, cActual);
    return true;
}

extern "C" DLL_EXPORT BOOL CStyle_Array_Char_In_Ref(CHAR **ppActual, int cActual)
{
    if (!CStyle_Array_Char(*ppActual, cActual))
        return false;

    ChangeArrayValue(*ppActual, cActual);
    return true;
}

extern "C" DLL_EXPORT BOOL CStyle_Array_LPCSTR_In_Ref(LPCSTR **ppActual, int cActual)
{
    if (!CStyle_Array_LPCSTR(*ppActual, cActual))
        return false;

    ChangeArrayValue(*ppActual, cActual);
    return true;
}

extern "C" DLL_EXPORT BOOL CStyle_Array_LPSTR_In_Ref(LPSTR **ppActual, int cActual)
{
    if (!CStyle_Array_LPSTR(*ppActual, cActual))
        return false;

    ChangeArrayValue(*ppActual, cActual);
    return true;
}

extern "C" DLL_EXPORT BOOL CStyle_Array_Struct_In_Ref(TestStruct **ppActual, int cActual)
{
    if (!CStyle_Array_Struct(*ppActual, cActual))
        return false;

    ChangeArrayValue(*ppActual, cActual);
    return true;
}

extern "C" DLL_EXPORT BOOL CStyle_Array_Bool_In_Ref(BOOL **ppActual, int cActual)
{
    if (!CStyle_Array_Bool(*ppActual, cActual))
        return false;

    BOOL *pActual = *ppActual;

    for (int i = 0; i < cActual; ++i)
    {
        if (i % 2 != 0)
            pActual[i] = TRUE;
        else
            pActual[i] = FALSE;
    }

    return true;
}

/////////////////////////////////////////// OutAttribute ByRef /////////////////////////////////////////
extern "C" DLL_EXPORT BOOL CStyle_Array_Int_Out_Ref(int **ppActual, int cActual)
{
    CHECK_PARAM_EMPTY(*ppActual);

    ChangeArrayValue(*ppActual, cActual);

    return true;
}

#ifdef _WIN32
extern "C" DLL_EXPORT BOOL CStyle_Array_Object_Out_Ref(VARIANT **ppActual, int cActual)
{
    CHECK_PARAM_EMPTY(*ppActual);

    ChangeArrayValue(*ppActual, cActual);

    return true;
}
#endif

extern "C" DLL_EXPORT BOOL CStyle_Array_Uint_Out_Ref(uint32_t **ppActual, int cActual)
{
    CHECK_PARAM_EMPTY(*ppActual);

    ChangeArrayValue(*ppActual, cActual);

    return true;
}

extern "C" DLL_EXPORT BOOL CStyle_Array_Short_Out_Ref(int16_t **ppActual, int cActual)
{
    CHECK_PARAM_EMPTY(*ppActual);

    ChangeArrayValue(*ppActual, cActual);

    return true;
}

extern "C" DLL_EXPORT BOOL CStyle_Array_Word_Out_Ref(uint16_t **ppActual, int cActual)
{
    CHECK_PARAM_EMPTY(*ppActual);

    ChangeArrayValue(*ppActual, cActual);

    return true;
}

extern "C" DLL_EXPORT BOOL CStyle_Array_Long64_Out_Ref(int64_t **ppActual, int cActual)
{
    CHECK_PARAM_EMPTY(*ppActual);

    ChangeArrayValue(*ppActual, cActual);

    return true;
}

extern "C" DLL_EXPORT BOOL CStyle_Array_ULong64_Out_Ref(uint64_t **ppActual, int cActual)
{
    CHECK_PARAM_EMPTY(*ppActual);

    ChangeArrayValue(*ppActual, cActual);

    return true;
}

extern "C" DLL_EXPORT BOOL CStyle_Array_Double_Out_Ref(DOUBLE **ppActual, int cActual)
{
    CHECK_PARAM_EMPTY(*ppActual);

    ChangeArrayValue(*ppActual, cActual);

    return true;
}

extern "C" DLL_EXPORT BOOL CStyle_Array_Float_Out_Ref(FLOAT **ppActual, int cActual)
{
    CHECK_PARAM_EMPTY(*ppActual);

    ChangeArrayValue(*ppActual, cActual);

    return true;
}

extern "C" DLL_EXPORT BOOL CStyle_Array_Byte_Out_Ref(uint8_t **ppActual, int cActual)
{
    CHECK_PARAM_EMPTY(*ppActual);

    ChangeArrayValue(*ppActual, cActual);

    return true;
}

extern "C" DLL_EXPORT BOOL CStyle_Array_Char_Out_Ref(CHAR **ppActual, int cActual)
{
    CHECK_PARAM_EMPTY(*ppActual);

    ChangeArrayValue(*ppActual, cActual);

    return true;
}

extern "C" DLL_EXPORT BOOL CStyle_Array_LPCSTR_Out_Ref(LPCSTR **ppActual, int cActual)
{
    CHECK_PARAM_EMPTY(*ppActual);

    ChangeArrayValue(*ppActual, cActual);

    return true;
}

extern "C" DLL_EXPORT BOOL CStyle_Array_LPSTR_Out_Ref(LPSTR **ppActual, int cActual)
{
    CHECK_PARAM_EMPTY(*ppActual);

    ChangeArrayValue(*ppActual, cActual);

    return true;
}

extern "C" DLL_EXPORT BOOL CStyle_Array_Struct_Out_Ref(TestStruct **ppActual, int cActual)
{
    CHECK_PARAM_EMPTY(*ppActual);

    ChangeArrayValue(*ppActual, cActual);

    return true;
}

extern "C" DLL_EXPORT BOOL CStyle_Array_Bool_Out_Ref(BOOL **ppActual, int cActual)
{
    CHECK_PARAM_EMPTY(*ppActual);

    BOOL *pArray = *ppActual;
    for (int i = 0; i < cActual; ++i)
    {
        if (i % 2 != 0)
            pArray[i] = TRUE;
        else
            pArray[i] = FALSE;
    }

    return true;
}

/////////////////////////////////////////// InAttribute OutAttribute ByRef /////////////////////////////////////////
extern "C" DLL_EXPORT BOOL CStyle_Array_Int_InOut_Ref(int **ppActual, int cActual)
{
    CHECK_PARAM_NOT_EMPTY(*ppActual);

    BOOL retval = CStyle_Array_Int_In_Ref(ppActual, cActual);
    ChangeArrayValue(*ppActual, cActual);

    return retval;
}

#ifdef _WIN32
extern "C" DLL_EXPORT BOOL CStyle_Array_Object_InOut_Ref(VARIANT **ppActual, int cActual)
{
    CHECK_PARAM_NOT_EMPTY(*ppActual);

    BOOL retval = CStyle_Array_Object_In_Ref(ppActual, cActual);
    ChangeArrayValue(*ppActual, cActual);

    return retval;
}
#endif

extern "C" DLL_EXPORT BOOL CStyle_Array_Uint_InOut_Ref(uint32_t **ppActual, int cActual)
{
    CHECK_PARAM_NOT_EMPTY(*ppActual);

    BOOL retval = CStyle_Array_Uint_In_Ref(ppActual, cActual);
    ChangeArrayValue(*ppActual, cActual);

    return retval;
}

extern "C" DLL_EXPORT BOOL CStyle_Array_Short_InOut_Ref(int16_t **ppActual, int cActual)
{
    CHECK_PARAM_NOT_EMPTY(*ppActual);

    BOOL retval = CStyle_Array_Short_In_Ref(ppActual, cActual);
    ChangeArrayValue(*ppActual, cActual);

    return retval;
}

extern "C" DLL_EXPORT BOOL CStyle_Array_Word_InOut_Ref(uint16_t **ppActual, int cActual)
{
    CHECK_PARAM_NOT_EMPTY(*ppActual);

    BOOL retval = CStyle_Array_Word_In_Ref(ppActual, cActual);
    ChangeArrayValue(*ppActual, cActual);

    return retval;
}

extern "C" DLL_EXPORT BOOL CStyle_Array_Long64_InOut_Ref(int64_t **ppActual, int cActual)
{
    CHECK_PARAM_NOT_EMPTY(*ppActual);

    BOOL retval = CStyle_Array_Long64_In_Ref(ppActual, cActual);
    ChangeArrayValue(*ppActual, cActual);

    return retval;
}

extern "C" DLL_EXPORT BOOL CStyle_Array_ULong64_InOut_Ref(uint64_t **ppActual, int cActual)
{
    CHECK_PARAM_NOT_EMPTY(*ppActual);

    BOOL retval = CStyle_Array_ULong64_In_Ref(ppActual, cActual);
    ChangeArrayValue(*ppActual, cActual);

    return retval;
}

extern "C" DLL_EXPORT BOOL CStyle_Array_Double_InOut_Ref(DOUBLE **ppActual, int cActual)
{
    CHECK_PARAM_NOT_EMPTY(*ppActual);

    BOOL retval = CStyle_Array_Double_In_Ref(ppActual, cActual);
    ChangeArrayValue(*ppActual, cActual);

    return retval;
}

extern "C" DLL_EXPORT BOOL CStyle_Array_Float_InOut_Ref(FLOAT **ppActual, int cActual)
{
    CHECK_PARAM_NOT_EMPTY(*ppActual);

    BOOL retval = CStyle_Array_Float_In_Ref(ppActual, cActual);
    ChangeArrayValue(*ppActual, cActual);

    return retval;
}

extern "C" DLL_EXPORT BOOL CStyle_Array_Byte_InOut_Ref(uint8_t **ppActual, int cActual)
{
    CHECK_PARAM_NOT_EMPTY(*ppActual);

    BOOL retval = CStyle_Array_Byte_In_Ref(ppActual, cActual);
    ChangeArrayValue(*ppActual, cActual);

    return retval;
}

extern "C" DLL_EXPORT BOOL CStyle_Array_Char_InOut_Ref(CHAR **ppActual, int cActual)
{
    CHECK_PARAM_NOT_EMPTY(*ppActual);

    BOOL retval = CStyle_Array_Char_In_Ref(ppActual, cActual);
    ChangeArrayValue(*ppActual, cActual);

    return retval;
}

extern "C" DLL_EXPORT BOOL CStyle_Array_LPCSTR_InOut_Ref(LPCSTR **ppActual, int cActual)
{
    CHECK_PARAM_NOT_EMPTY(*ppActual);

    BOOL retval = CStyle_Array_LPCSTR_In_Ref(ppActual, cActual);
    ChangeArrayValue(*ppActual, cActual);

    return retval;
}

extern "C" DLL_EXPORT BOOL CStyle_Array_LPSTR_InOut_Ref(LPSTR **ppActual, int cActual)
{
    CHECK_PARAM_NOT_EMPTY(*ppActual);

    BOOL retval = CStyle_Array_LPSTR_In_Ref(ppActual, cActual);
    ChangeArrayValue(*ppActual, cActual);

    return retval;
}

extern "C" DLL_EXPORT BOOL CStyle_Array_Struct_InOut_Ref(TestStruct **ppActual, int cActual)
{
    CHECK_PARAM_NOT_EMPTY(*ppActual);

    BOOL retval = CStyle_Array_Struct_In_Ref(ppActual, cActual);
    ChangeArrayValue(*ppActual, cActual);

    return retval;
}

extern "C" DLL_EXPORT BOOL CStyle_Array_Bool_InOut_Ref(BOOL **ppActual, int cActual)
{
    CHECK_PARAM_NOT_EMPTY(*ppActual);

    BOOL retval = CStyle_Array_Bool_In_Ref(ppActual, cActual);

    BOOL *pArray = *ppActual;
    for (int i = 0; i < cActual; ++i)
    {
        if (i % 2 != 0)
            pArray[i] = TRUE;
        else
            pArray[i] = FALSE;
    }

    return retval;
}

////////////////////////////////Added marshal array of struct as LPArray//////////////////////////////////
extern "C" DLL_EXPORT BOOL MarshalArrayOfStructAsLPArrayByVal(S2 *pActual, int cActual, S2* pExpect)
{
    bool breturn = true;
    S2 *correctArr = pExpect;
    if (!(ValidateS2LPArray(pActual, correctArr, cActual)))
    {
        breturn = false;
    }
    for (int j = 0; j < cActual; j++)
    {
        InstanceS2(&pActual[j], 0, 32, 0, 16, 0, 8, 0, 16, 0, 64, 64.0F, 6.4);
    }
    return breturn;
}

extern "C" DLL_EXPORT BOOL MarshalArrayOfStructAsLPArrayByRef(S2 **pActual, int cActual, S2* pExpect)
{
    return MarshalArrayOfStructAsLPArrayByVal(*pActual, cActual, pExpect);
}

extern "C" DLL_EXPORT BOOL MarshalArrayOfStructAsLPArrayByValOut(S2 *pActual, int cActual)
{
    BOOL breturn = true;
    for (int j = 0; j < cActual; j++)
    {
        InstanceS2(&pActual[j], 0, 32, 0, 16, 0, 8, 0, 16, 0, 64, 64.0F, 6.4);
    }
    return breturn;
}

extern "C" DLL_EXPORT BOOL MarshalArrayOfStructAsLPArrayByValInOut(S2 *pActual, int cActual)
{
    BOOL breturn = true;
    return breturn;
}

extern "C" DLL_EXPORT BOOL MarshalArrayOfStructAsLPArrayByRefOut(S2 **pActual, int cActual)
{
    BOOL breturn = true;
    for (int j = 0; j < cActual; j++)
    {
        InstanceS2(&((*pActual)[j]), 0, 32, 0, 16, 0, 8, 0, 16, 0, 64, 64.0F, 6.4);
    }
    return breturn;
}

extern "C" DLL_EXPORT int Get_Multidimensional_Array_Sum(int* array, int rows, int columns)
{
    int sum = 0;
    for(int i = 0; i < rows; ++i)
    {
        for (int j = 0; j < columns; ++j)
        {
            sum += array[i * columns + j];
        }
    }
    return sum;
}
