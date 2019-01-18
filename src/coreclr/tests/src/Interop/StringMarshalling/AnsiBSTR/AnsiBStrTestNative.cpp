// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
#include <xplatform.h>
#include "platformdefines.h"

const char strManaged[] = " \0Managed\0String\0 ";
size_t lenstrManaged = ARRAYSIZE(strManaged) - 1; // the length of strManaged

const char strReturn[] = "a";
size_t lenstrReturn = ARRAYSIZE(strReturn) - 1; //the length of strReturn

const char strNative[] = "Native String";
size_t lenstrNative = ARRAYSIZE(strNative) - 1; //the len of strNative

//Test Method1
extern "C" DLL_EXPORT BSTR STDMETHODCALLTYPE Marshal_In(/*[in]*/ BSTR s)
{
    //Check the input
    size_t len = TP_SysStringByteLen(s);
    if ((len != lenstrManaged) || (memcmp(s, strManaged, len) != 0))
    {
        printf("Error in Function Marshal_In(Native Client)\n");

        //Expected
        printf("Expected:");
        for (size_t i = 0; i < lenstrManaged; ++i)
            putchar(*(((char *)strManaged) + i));
        printf("\tThe length of Expected:%zd\n", lenstrManaged);

        //Actual
        printf("Actual:");
        for (size_t j = 0; j < len; ++j)
            putchar(*(((char *)s) + j));
        printf("\tThe length of Actual:%zd\n", len);
    }

    return CoreClrBStrAlloc(strReturn, lenstrReturn);
}

//Test Method2
extern "C" DLL_EXPORT BSTR STDMETHODCALLTYPE Marshal_InOut(/*[In,Out]*/ BSTR s)
{

    //Check the Input
    size_t len = TP_SysStringByteLen(s);
    if ((len != lenstrManaged) || (memcmp(s, strManaged, len) != 0))
    {
        printf("Error in Function Marshal_InOut(Native Client)\n");

        //Expected
        printf("Expected:");
        for (size_t i = 0; i < lenstrManaged; ++i)
            putchar(*(((char *)strManaged) + i));
        printf("\tThe length of Expected:%zd\n", lenstrManaged);

        //Actual
        printf("Actual:");
        for (size_t j = 0; j < len; ++j)
            putchar(*(((char *)s) + j));
        printf("\tThe length of Actual:%zd\n", len);
    }

    //In-Place Change
    memcpy((char *)s, strNative, lenstrNative);
    *((UINT *)s - 1) = (UINT) TP_SysStringByteLen(s);

    //Return
    return CoreClrBStrAlloc(strReturn, lenstrReturn);
}
extern "C" DLL_EXPORT BSTR STDMETHODCALLTYPE Marshal_Out(/*[Out]*/ BSTR s)
{
    s = CoreClrBStrAlloc(strNative, lenstrNative);

    //In-Place Change
    memcpy((char *)s, strNative, lenstrNative);
    *((UINT *)s - 1) = (UINT) TP_SysStringByteLen(s);

    //Return
    return CoreClrBStrAlloc(strReturn, lenstrReturn);
}

extern "C" DLL_EXPORT BSTR STDMETHODCALLTYPE MarshalPointer_In(/*[in]*/ BSTR *s)
{
    //CheckInput
    size_t len = TP_SysStringByteLen(*s);
    if ((len != lenstrManaged) || (memcmp(*s, strManaged, len) != 0))
    {
        printf("Error in Function MarshalPointer_In\n");

        //Expected
        printf("Expected:");
        for (size_t i = 0; i < lenstrManaged; ++i)
            putchar(*(((char *)strManaged) + i));
        printf("\tThe length of Expected:%zd\n", lenstrManaged);

        //Actual
        printf("Actual:");
        for (size_t j = 0; j < len; ++j)
            putchar(*(((char *)*s) + j));
        printf("\tThe length of Actual:%zd\n", len);
    }

    return CoreClrBStrAlloc(strReturn, lenstrReturn);
}

extern "C" DLL_EXPORT BSTR STDMETHODCALLTYPE MarshalPointer_InOut(/*[in,out]*/ BSTR *s)
{
    //Check the Input
    size_t len = TP_SysStringByteLen(*s);
    if ((len != lenstrManaged) || (memcmp(*s, strManaged, len) != 0))
    {
        printf("Error in Function MarshalPointer_InOut\n");

        //Expected
        printf("Expected:");
        for (size_t i = 0; i < lenstrManaged; ++i)
            putchar(*(((char *)strManaged) + i));

        printf("\tThe length of Expected:%zd\n", lenstrManaged);

        //Actual
        printf("Actual:");
        for (size_t j = 0; j < len; ++j)
            putchar(*(((char *)*s) + j));

        printf("\tThe length of Actual:%zd\n", len);
    }

    //Allocate New
    CoreClrBStrFree(*s);
    *s = CoreClrBStrAlloc(strNative, lenstrNative);

    //Return
    return CoreClrBStrAlloc(strReturn, lenstrReturn);
}

extern "C" DLL_EXPORT BSTR STDMETHODCALLTYPE MarshalPointer_Out(/*[out]*/ BSTR *s)
{
    *s = CoreClrBStrAlloc(strNative, lenstrNative);
    return CoreClrBStrAlloc(strReturn, lenstrReturn);
}
