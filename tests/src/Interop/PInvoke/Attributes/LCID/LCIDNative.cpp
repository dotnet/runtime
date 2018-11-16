// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#include <stdio.h>
#include <tchar.h>
#include <windows.h>
#include <xplatform.h>

const char* strManaged = "Managed\0String\0";
size_t lenstrManaged = 7; // the length of strManaged

const char* strReturn = "a\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0";
const char* strFalseReturn = "\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0";

const char* strNative = " Native\0String\0";
size_t lenstrNative = 7; //the len of strNative

extern "C" LPSTR ReturnString()
{
    size_t lenstrReturn = strlen(strReturn);
    LPSTR ret = (LPSTR)(CoTaskMemAlloc(sizeof(char)*(lenstrReturn+1)));
    memset(ret,'\0',lenstrReturn+1);
    strncpy_s(ret,lenstrReturn + 1,strReturn,1);
    return ret;
}

extern "C" LPSTR ReturnFalseString()
{
    size_t lenstrFalseReturn = strlen(strFalseReturn);
    LPSTR ret = (LPSTR)(CoTaskMemAlloc(sizeof(char)*(lenstrFalseReturn+1)));
    memset(ret,'\0',lenstrFalseReturn+1);
    strncpy_s(ret,lenstrFalseReturn + 1,strFalseReturn,1);
    return ret;
}

extern "C" void PrintExpectedAndActual(LPCSTR s, size_t len)
{
    //Expected
    printf("Expected:");
    for(size_t i = 0; i< lenstrManaged;++i)
        putchar(*(((const char *)strManaged)+i));
    printf("\tThe length of Expected:%d\n",static_cast<int>(lenstrManaged));

    //Actual
    printf("Actual:");
    for(size_t j = 0; j < len; ++j )
        putchar(*(((const char *)s) + j));
    printf("\tThe length of Actual:%d\n",static_cast<int>(len));
}

extern "C" DLL_EXPORT LPSTR STDMETHODCALLTYPE MarshalStringBuilder_LCID_As_First_Argument(int lcid, LPSTR s)
{
    printf("LCID:%d\n\n",lcid);

    //Check the Input
    size_t len = strlen(s);
    if((len != lenstrManaged)||(memcmp(s,strManaged,len)!=0))
    {
        printf("Error in Function MarshalStringBuilder_LCID_As_First_Argument(Native Client)\n");
        PrintExpectedAndActual(s, len);
        return ReturnFalseString();
    }

    //In-Place Change
    strncpy_s(s,len + 1,strNative,lenstrNative);

    //Return
    return ReturnString();
}

extern "C" DLL_EXPORT LPSTR STDMETHODCALLTYPE MarshalStringBuilder_LCID_As_Last_Argument_SetLastError(LPSTR s,int lcid)
{	
    //Check the Input
    size_t len = strlen(s);
    if((len != lenstrManaged)||(memcmp(s,strManaged,len)!=0))
    {
        printf("Error in Function MarshalStringBuilder_LCID_As_Last_Argument_SetLastError(Native Client)\n");
        PrintExpectedAndActual(s, len);
        return ReturnFalseString();
    }

    //In-Place Change
    strncpy_s(s,len + 1,strNative,lenstrNative);

    SetLastError(1090);
    //Return
    return ReturnString();
}

extern "C" DLL_EXPORT HRESULT STDMETHODCALLTYPE MarshalStringBuilder_LCID_PreserveSig_SetLastError(LPSTR s, int lcid, LPSTR * retVal)
{
    //Check the Input
    size_t len = strlen(s);
    if((len != lenstrManaged)||(memcmp(s,strManaged,len)!=0))
    {
        printf("Error in Function MarshalStringBuilder_LCID_PreserveSig_SetLastError\n");
        PrintExpectedAndActual(s, len);

        size_t lenstrFalseReturn = strlen(ReturnFalseString());
        *retVal = (LPSTR)CoTaskMemAlloc(sizeof(char)*(lenstrFalseReturn+1));
        memset(*retVal,'\0',lenstrFalseReturn+1);
        strncpy_s(*retVal,lenstrFalseReturn,ReturnFalseString(),lenstrFalseReturn);

        return S_FALSE;
    }

    //In-Place Change
    strncpy_s(s,len + 1,strNative,lenstrNative);

    //Set the error code.
    SetLastError(1090);

    size_t lenstrReturn = strlen(ReturnString());
    *retVal = (LPSTR)(CoTaskMemAlloc(sizeof(char)*(lenstrReturn+1)));
    memset(*retVal,'\0',lenstrReturn+1);
    strncpy_s(*retVal,lenstrReturn + 1,ReturnString(),lenstrReturn);

    return S_OK;
}
