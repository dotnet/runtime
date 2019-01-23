// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#include <stdio.h>
#include <xplatform.h>
#include <platformdefines.h>

LPCWSTR strManaged = W("Managed\0String\0");
LPCWSTR strNative = W(" Native\0String\0");

size_t lenstrManaged = 7; // the length of strManaged
size_t lenstrNative = 7; //the len of strNative

extern "C" DLL_EXPORT bool STDMETHODCALLTYPE MarshalStringPointer_InOut(/*[in,out]*/LPWSTR *s)
{
    //Check the Input
    size_t len = TP_slen(*s);
    if((len != lenstrManaged)||(TP_wcsncmp(*s,strManaged, lenstrManaged)!=0))
    {
        printf("Error in Function MarshalStringPointer_InOut\n");

        //Expected
        printf("Expected:");
        wprintf(L"%ls",strManaged);
        printf("\tThe length of Expected:%d\n",static_cast<int>(lenstrManaged));

        //Actual
        printf("Actual:");
        wprintf(L"%ls",*s);
        printf("\tThe length of Actual:%d\n",static_cast<int>(len));

        return false;
    }

    //Allocate New
    CoreClrFree(*s);

    //Alloc New
    size_t length = lenstrNative + 1;
    *s = (LPWSTR)CoreClrAlloc(length * sizeof(WCHAR));
    memset(*s,'\0',length * sizeof(WCHAR));
    TP_wcsncpy_s(*s,length,strNative,lenstrNative);

    //Return
    return true;
}
