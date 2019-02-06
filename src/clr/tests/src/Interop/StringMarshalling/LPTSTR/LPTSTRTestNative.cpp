// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#include <xplatform.h>
#include <platformdefines.h>

const WCHAR* strManaged = W("Managed\0String\0");
size_t   lenstrManaged = 7; // the length of strManaged

const WCHAR* strReturn = W("a\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0");

const WCHAR* strErrReturn = W("Error");

const WCHAR* strNative = W(" Native\0String\0");
size_t lenstrNative = 7; //the len of strNative

extern "C" LPWSTR ReturnString()
{
	size_t length = TP_slen(strReturn)+1;
    LPWSTR ret = (LPWSTR)CoreClrAlloc(sizeof(WCHAR)*length);
    memset(ret,'\0', sizeof(WCHAR)*length);
    TP_wcsncpy_s(ret,length,strReturn,length-1);
    return ret;
}

extern "C" LPWSTR ReturnErrString()
{
	size_t length = TP_slen(strErrReturn)+1;
    LPWSTR ret = (LPWSTR)CoreClrAlloc(sizeof(WCHAR)*length);
    memset(ret,'\0', sizeof(WCHAR)*length);
    TP_wcsncpy_s(ret,length,strErrReturn,length-1);
    return ret;
}

//Test Method1
extern "C" DLL_EXPORT LPWSTR Marshal_In(/*[In]*/LPWSTR s)
{
    //Check the Input
    size_t len = TP_slen(s);

    if((len != lenstrManaged)||(TP_wmemcmp(s,(WCHAR*)strManaged,len)!=0))
    {
        printf("Error in Function Marshal_In(Native Client)\n");
        return ReturnErrString();
    }

    //Return
    return ReturnString();
}

//Test Method2
extern "C" DLL_EXPORT LPWSTR Marshal_InOut(/*[In,Out]*/LPWSTR s)
{

    //Check the Input
	size_t len = TP_slen(s);

    if((len != lenstrManaged)||(TP_wmemcmp(s,(WCHAR*)strManaged,len)!=0))
    {
        printf("Error in Function Marshal_InOut(Native Client)\n");
        return ReturnErrString();
    }

    //In-Place Change
    TP_wcsncpy_s(s,len+1,strNative,lenstrNative);

    //Return
    return ReturnString();
}

extern "C" DLL_EXPORT LPWSTR Marshal_Out(/*[Out]*/LPWSTR s)
{
    s = (LPWSTR)CoreClrAlloc(sizeof(WCHAR)*(lenstrNative+1));;
    memset(s,0, sizeof(WCHAR)*(lenstrNative + 1));

    //In-Place Change
    TP_wcsncpy_s(s,lenstrNative+1,strNative,lenstrNative);

    //Return
    return ReturnString();
}


extern "C" DLL_EXPORT LPWSTR MarshalPointer_InOut(/*[in,out]*/LPWSTR *s)
{
    //Check the Input
	size_t len = TP_slen(*s);
    if((len != lenstrManaged)||(TP_wmemcmp(*s,(WCHAR*)strManaged,len)!=0))
    {
        printf("Error in Function MarshalPointer_InOut\n");     
        return ReturnErrString();
    }

    //Allocate New
    CoreClrFree(*s);

    //Alloc New
	size_t length = lenstrNative + 1;
    *s = (LPWSTR)CoreClrAlloc(length * sizeof(WCHAR));
    memset(*s,'\0',length  * sizeof(WCHAR));
    TP_wcsncpy_s(*s,length,strNative,lenstrNative);

    //Return
    return ReturnString();
}

extern "C" DLL_EXPORT LPWSTR MarshalPointer_Out(/*[out]*/ LPWSTR *s)
{
	size_t length = lenstrNative+1;
    *s = (LPWSTR)CoreClrAlloc(sizeof(WCHAR)*length);
	memset(*s, '\0', length  * sizeof(WCHAR));
    TP_wcsncpy_s(*s,length,strNative,lenstrNative);

    return ReturnString();
}

typedef LPWSTR (__stdcall * Test_Del_MarshalStrB_InOut)(/*[in,out]*/ LPWSTR s);
extern "C" DLL_EXPORT BOOL STDMETHODCALLTYPE ReverseP_MarshalStrB_InOut(Test_Del_MarshalStrB_InOut d, /*[in]*/ LPCWSTR  s)
{
    LPWSTR ret = d((LPWSTR)s);
    LPWSTR expectedret =(LPWSTR)W("Native");
    LPWSTR expectedstr = (LPWSTR)W("m");

	size_t lenret = TP_slen(ret);
	size_t lenexpectedret = TP_slen(expectedret);
    if((lenret != lenexpectedret)||(TP_wcsncmp(ret,expectedret,lenret)!=0))
    {
        printf("Error in ReverseP_MarshalStrB_InOut, Returned value didn't match\n");
        return FALSE;
    }

	size_t lenstr = TP_slen(s);
	size_t lenexpectedstr = TP_slen(expectedstr);
    if((lenstr != lenexpectedstr)||(TP_wcsncmp(s,expectedstr,lenstr)!=0))
    {
        printf("Error in ReverseP_MarshalStrB_InOut, Changed value didn't reflect on native side.\n");
        return FALSE;
    }

    return TRUE;
}

typedef LPWSTR (__cdecl * Test_Del_MarshalStrB_Out)(/*[out]*/ LPWSTR * s);
extern "C" DLL_EXPORT BOOL STDMETHODCALLTYPE ReverseP_MarshalStrB_Out(Test_Del_MarshalStrB_Out d)
{
    LPWSTR s;
    LPWSTR ret = d((LPWSTR*)&s);
    LPWSTR expectedret = (LPWSTR)W("Native");
    LPWSTR expectedstr = (LPWSTR)W("Managed");

    size_t lenret = TP_slen(ret);
	size_t lenexpectedret = TP_slen(expectedret);
    if((lenret != lenexpectedret)||(TP_wcsncmp(ret,expectedret,lenret)!=0))
    {
        printf("Error in ReverseP_MarshalStrB_Out, Returned value didn't match\n");
        return FALSE;
    }

	size_t lenstr = TP_slen(s);
	size_t lenexpectedstr = TP_slen(expectedstr);
    if((lenstr != lenexpectedstr)||(TP_wcsncmp(s,expectedstr,lenstr)!=0))
    {
        printf("Error in ReverseP_MarshalStrB_Out, Changed value didn't reflect on native side.\n");
        return FALSE;
    }

    return TRUE;

}

// Verify that we append extra null terminators to our StringBuilder native buffers.
// Although this is a hidden implementation detail, it would be breaking behavior to stop doing this
// so we have a test for it. In particular, this detail prevents us from optimizing marshalling StringBuilders by pinning.
extern "C" DLL_EXPORT BOOL STDMETHODCALLTYPE Verify_NullTerminators_PastEnd(LPCWSTR buffer, int length)
{
    return buffer[length+1] == W('\0');
}
