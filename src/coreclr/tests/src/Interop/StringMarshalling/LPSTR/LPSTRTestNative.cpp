// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#include <xplatform.h>
#include <platformdefines.h>

const char* strManaged = "Managed\0String\0";
size_t   lenstrManaged = 7; // the length of strManaged

const char* strReturn = "a\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0";
const char *strerrReturn = "error";

const char* strNative = " Native\0String\0";
size_t lenstrNative = 7; //the len of strNative

//Test Method1

extern "C" LPSTR ReturnString()
{
    size_t strLength = strlen(strReturn);
    LPSTR ret = (LPSTR)(CoreClrAlloc(sizeof(char)* (strLength +1)));
    memset(ret,'\0',strLength+1);
    strncpy_s(ret,strLength + 1, strReturn, strLength);
    return ret;
}

extern "C" LPSTR ReturnErrorString()
{
    size_t strLength = strlen(strerrReturn);
    LPSTR ret = (LPSTR)(CoreClrAlloc(sizeof(char)*(strLength + 1)));
    memset(ret,'\0',strLength + 1);
    strncpy_s(ret,strLength + 1,strerrReturn,strLength);
    return ret;
}

//Test Method2
extern "C" DLL_EXPORT LPSTR Marshal_InOut(/*[In,Out]*/LPSTR s)
{
    //Check the Input
    size_t len = strlen(s);

    if((len != lenstrManaged)||(memcmp(s,strManaged,len)!=0))
    {
        printf("Error in Function Marshal_InOut(Native Client)\n");        
        
        for(size_t i = 0; i< lenstrManaged;++i)
            putchar(*(((char *)strManaged)+i));               
        
        for(size_t j = 0; j < len; ++j )
            putchar(*(((char *)s) + j));
        return ReturnErrorString();
    }

    //In-Place Change
    strncpy_s(s,len + 1,strNative,lenstrNative);

    //Return
    return ReturnString();
}


extern "C" DLL_EXPORT LPSTR Marshal_Out(/*[Out]*/LPSTR s)
{
    s = (LPSTR)(CoreClrAlloc(sizeof(char)*(lenstrNative+1)));

    memset(s,0,lenstrNative+1);
    //In-Place Change
    strncpy_s(s,lenstrNative+1,strNative,lenstrNative);

    //Return
    return ReturnString();
}


extern "C" DLL_EXPORT LPSTR MarshalPointer_InOut(/*[in,out]*/LPSTR *s)
{
    //Check the Input
    size_t len = strlen(*s);

    if((len != lenstrManaged)||(memcmp(*s,strManaged,len)!=0))
    {
        printf("Error in Function MarshalPointer_InOut\n");
        
        for(size_t i = 0; i< lenstrManaged;++i)
            putchar(*(((char *)strManaged)+i));
                
        for( size_t j = 0; j < len; ++j)
            putchar(*(((char *)*s) + j));
        
        return ReturnErrorString();
    }

    //Allocate New
    CoreClrFree(*s);
    *s = (LPSTR)CoreClrAlloc(sizeof(char)*(lenstrNative+1));
    memset(*s,0,lenstrNative+1);
    strncpy_s(*s,len + 1,strNative,lenstrNative);

    //Return
    return ReturnString();
}

extern "C" DLL_EXPORT LPSTR MarshalPointer_Out(/*[out]*/ LPSTR *s)
{
    *s = (LPSTR)CoreClrAlloc(sizeof(char)*(lenstrNative+1));
    memset(*s,0,lenstrNative+1);
    strncpy_s(*s,lenstrNative+1,strNative,lenstrNative);

    return ReturnString();
}

extern "C" DLL_EXPORT int __cdecl Writeline(char * pFormat, int i, char c, double d, short s, unsigned u)
{
	int sum = i;
	for (size_t it = 0; it < strlen(pFormat); it++)
	{
		sum += (int)(*pFormat);
	}	
	sum += (int)c;
	sum += (int)d;
	sum += (int)s;
	sum += (int)u;
	return sum;
}


typedef LPCWSTR (__stdcall * Test_DelMarshal_InOut)(/*[in]*/ LPCSTR s);
extern "C" DLL_EXPORT BOOL __cdecl RPinvoke_DelMarshal_InOut(Test_DelMarshal_InOut d, /*[in]*/ LPCSTR s)
{
    LPCWSTR str = d(s);
    LPCWSTR ret = W("Return");    

    size_t lenstr = TP_slen(str);
    size_t lenret = TP_slen(ret);

    if((lenret != lenstr)||(TP_wcsncmp(str,ret,lenstr)!=0))
    {
        printf("Error in RPinvoke_DelMarshal_InOut, Returned value didn't match\n");
        return FALSE;
    }
    
    CoreClrFree((LPVOID)str);

    return TRUE;
}

//
// PInvokeDef.cs explicitly declares that RPinvoke_DelMarshalPointer_Out uses STDCALL
//
typedef LPCSTR (__cdecl * Test_DelMarshalPointer_Out)(/*[out]*/ LPSTR * s);
extern "C" DLL_EXPORT BOOL __stdcall RPinvoke_DelMarshalPointer_Out(Test_DelMarshalPointer_Out d)
{
    LPSTR str;
    LPCSTR ret = d(&str);

    const char* changedstr = "Native";

    size_t lenstr = strlen(str);
    size_t lenchangedstr = strlen(changedstr);

    if((lenstr != lenchangedstr)||(strncmp(str,changedstr,lenstr)!=0))
    {
        printf("Error in RPinvoke_DelMarshal_InOut, Value didn't change\n");
        return FALSE;
    }

    LPCSTR expected = "Return";
    size_t lenret = strlen(ret);
    size_t lenexpected = strlen(expected);

    if((lenret != lenexpected)||(strncmp(ret,expected,lenret)!=0))
    {
        printf("Error in RPinvoke_DelMarshal_InOut, Return vaue is different than expected\n");
        return FALSE;
    }

    return TRUE;
}

//
// PInvokeDef.cs explicitly declares that ReverseP_MarshalStrB_InOut uses STDCALL
//
typedef LPSTR (__stdcall * Test_Del_MarshalStrB_InOut)(/*[in,out]*/ LPSTR s);
extern "C" DLL_EXPORT  BOOL __stdcall ReverseP_MarshalStrB_InOut(Test_Del_MarshalStrB_InOut d, /*[in]*/ LPCSTR s)
{
    LPSTR ret = d((LPSTR)s);
    LPCSTR expected = "Return";
    size_t lenret = strlen(ret);
    size_t lenexpected = strlen(expected);

    if((lenret != lenexpected)||(strncmp(ret,expected,lenret)!=0))
    {
        printf("Error in ReverseP_MarshalStrB_InOut, Return vaue is different than expected\n");
        return FALSE;
    }

    LPCSTR expectedchange = "m";
    size_t lenstr = strlen(s);
    size_t lenexpectedchange = strlen(expectedchange);
    
    if((lenstr != lenexpectedchange)||(strncmp(s,expectedchange,lenstr)!=0))
    {
        printf("Error in ReverseP_MarshalStrB_InOut, Value didn't get change\n");
        return FALSE;
    }
    return TRUE;
}
