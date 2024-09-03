// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// helper.h : Defines helper functions
#include <xplatform.h>

const int32_t Array_Size = 10;
const int32_t CArray_Size = 20;

//////////////////////////////////////////////////////////////////////////////
// Verify helper methods
//////////////////////////////////////////////////////////////////////////////
template<typename T>
BOOL IsObjectEquals(T o1, T o2)
{
    // T::operator== required.
    return o1 == o2;
}

//Int32 helper
template<typename T>
T* InitArray(SIZE_T arrSize)
{
    T* pExpectArr = (T*)CoreClrAlloc(sizeof(T)*arrSize);
    for(SIZE_T i = 0;i<arrSize;++i)
    {
        pExpectArr[i] = (T)i;
    }
    return pExpectArr;
}

template<typename T>
T* InitExpectedArray(SIZE_T arrSize)
{
    T* pExpectArr = (T*)CoreClrAlloc(sizeof(T)*arrSize);
    for(SIZE_T i = 0;i<arrSize;++i)
    {
        pExpectArr[i] = (T)(arrSize - 1 - i);
    }
    return pExpectArr;
}

template<typename T>
BOOL EqualArray(T* actualArray, SIZE_T actualSize, T* expectedArray, SIZE_T expectedSize)
{
    int failures = 0;

    if(actualArray == NULL && expectedArray == NULL)
    {
        printf("Two arrays are equal. Both of them NULL\n");
        return TRUE;
    }
    else if(actualArray == NULL && expectedArray != NULL)
    {
        printf("Two arrays aren't equal. Array from Managed to Native is NULL,but the Compared is not NULL!\n");
        return FALSE;
    }
    else if(actualArray != NULL && expectedArray == NULL)
    {
        printf("Two arrays aren't equal. Array from Managed to Native is not NULL,but the Compared is NULL!\n");
        return FALSE;
    }
    else if(actualSize != expectedSize)
    {
        printf("Two arrays aren't equal. The arrays size are not equal!\n");
        return FALSE;
    }
    for(SIZE_T i = 0;i < actualSize; ++i)
    {
        if(actualArray[i] != expectedArray[i])
        {
            printf("Two arrays aren't equal.The value of index of %d isn't equal!\n",(int)i);
            printf("\tThe value in actual   rray is %d\n",(int)actualArray[i]);
            printf("\tThe value in expected array is %d\n",(int)expectedArray[i]);
            failures++;
        }
    }
    if( failures > 0 )
        return FALSE;
    return TRUE;
}

template<typename T>
BOOL CheckAndChangeArrayByRef(T ** ppActual, T* Actual_Array_Size, SIZE_T Expected_Array_Size, SIZE_T Return_Array_Size)
{
    T* pExpectedArr = InitArray<T>(Expected_Array_Size);
    if(!EqualArray(*ppActual, *Actual_Array_Size, pExpectedArr, Expected_Array_Size))
    {
        printf("ManagedtoNative Error in Method: %s!\n",__FUNCTION__);
        return FALSE;
    }

    CoreClrFree(pExpectedArr);
    CoreClrFree(*ppActual);
    *ppActual = (T*)CoreClrAlloc(sizeof(T)*Return_Array_Size);

    *Actual_Array_Size = ((T)Return_Array_Size);
    for(SIZE_T i = 0; i < Return_Array_Size; ++i)
    {
        (*ppActual)[i] = Return_Array_Size - 1 - i;
    }
    return TRUE;
}

template<typename T>
BOOL CheckAndChangeArrayByOut(T ** ppActual, T* Actual_Array_Size, SIZE_T Return_Array_Size)
{
    if(*ppActual != NULL )
    {
        printf("ManagedtoNative Error in Method: %s!\n",__FUNCTION__);
        printf("Array is not NULL");
        return FALSE;
    }

    CoreClrFree(*ppActual);
    *ppActual = (T*)CoreClrAlloc(sizeof(T)*Return_Array_Size);

    *Actual_Array_Size = ((T)Return_Array_Size);
    for(SIZE_T i = 0; i < Return_Array_Size; ++i)
    {
        (*ppActual)[i] = Return_Array_Size - 1 - i;
    }
    return TRUE;
}

template<typename T>
BOOL CheckArray(T* pReturnArr, SIZE_T actualArraySize, SIZE_T expectedArraySize)
{
    T* pExpectedArr = InitExpectedArray<T>(expectedArraySize);

    if(!EqualArray(pReturnArr, actualArraySize, pExpectedArr, expectedArraySize))
    {
        printf("ManagedtoNative Error in Method: %s!\n",__FUNCTION__);
        CoreClrFree(pExpectedArr);
        return FALSE;
    }
    else
    {
        CoreClrFree(pExpectedArr);
        return TRUE;
    }
}

//BSTR helper
#ifdef _WIN32
template<>
BOOL IsObjectEquals(BSTR o1, BSTR o2)
{
    if ( o1 == NULL && o2 == NULL )
        return TRUE;
    else if ( o1 == NULL && o2 != NULL )
        return FALSE;
    else if ( o1 != NULL && o2 == NULL )
        return FALSE;

    uint32_t uLen1 = SysStringLen(o1);
    uint32_t uLen2 = SysStringLen(o2);

    if (uLen1 != uLen2 )
        return FALSE;

    return memcmp(o1, o2, uLen1) == 0;
}

BSTR ToBSTR(int i)
{
    BSTR bstrRet = NULL;
    VarBstrFromI4(i, 0, 0, &bstrRet);

    return bstrRet;
}
BOOL CmpBSTR(BSTR bstr1, BSTR bstr2)
{
    uint32_t uLen1 = SysStringLen(bstr1);
    uint32_t uLen2 = SysStringLen(bstr2);

    if (uLen1 != uLen2 )
        return FALSE;
    return memcmp(bstr1, bstr2, uLen1) == 0;
}
BSTR* InitArrayBSTR(int32_t arrSize)
{
    BSTR* pExpectArr = (BSTR*)CoreClrAlloc(sizeof(BSTR)*arrSize);
    for(int32_t i = 0;i<arrSize;++i)
    {
        pExpectArr[i] = ToBSTR(i);
    }
    return pExpectArr;
}
BOOL EqualArrayBSTR(BSTR* actualBSTRArray, int32_t actualSize, BSTR* expectedBSTRArray, int32_t expectedSize)
{
    int failures = 0;

    if(actualBSTRArray == NULL && expectedBSTRArray == NULL)
    {
        printf("Two arrays are equal. Both of them NULL\n");
        return TRUE;
    }
    else if(actualBSTRArray == NULL && expectedBSTRArray != NULL)
    {
        printf("Two arrays aren't equal. Array from Managed to Native is NULL,but the Compared is not NULL!\n");
        return FALSE;
    }
    else if(actualBSTRArray != NULL && expectedBSTRArray == NULL)
    {
        printf("Two arrays aren't equal. Array from Managed to Native is not NULL,but the Compared is NULL!\n");
        return FALSE;
    }
    else if(actualSize != expectedSize)
    {
        printf("Two arrays aren't equal. The arrays size are not equal!\n");
        return FALSE;
    }
    for(int i = 0;i < actualSize; ++i)
    {
        if(!CmpBSTR(actualBSTRArray[i],expectedBSTRArray[i]))
        {
            printf("Two arrays aren't equal.The value of index of %d isn't equal!\n",i);
            printf("\tThe value in array from managed to native is %S\n",actualBSTRArray[i]);
            printf("\tThe value in expected array is %S\n",expectedBSTRArray[i]);
            failures++;
        }
    }
    if(failures>0)
        return FALSE;
    return TRUE;
}
#endif
