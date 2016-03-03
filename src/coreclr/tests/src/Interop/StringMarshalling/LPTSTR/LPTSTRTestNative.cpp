#include <xplatform.h>

const WCHAR* strManaged = W("Managed\0String\0");
size_t   lenstrManaged = 7; // the length of strManaged

const WCHAR* strReturn = W("a\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0");

const WCHAR* strErrReturn = W("Error");

const WCHAR* strNative = W("Native\0String\0");
size_t lenstrNative = 7; //the len of strNative

extern "C" LPWSTR ReturnString()
{
	size_t length = wcslen(strReturn)+1;
    LPWSTR ret = (LPWSTR)CoTaskMemAlloc(sizeof(WCHAR)*length);
    memset(ret,'\0', sizeof(WCHAR)*length);
    wcsncpy_s(ret,length,strReturn,length-1);
    return ret;
}

extern "C" LPWSTR ReturnErrString()
{
	size_t length = wcslen(strErrReturn)+1;
    LPWSTR ret = (LPWSTR)CoTaskMemAlloc(sizeof(WCHAR)*length);
    memset(ret,'\0', sizeof(WCHAR)*length);
    wcsncpy_s(ret,length,strErrReturn,length-1);
    return ret;
}

//Test Method1

//Test Method2
extern "C" DLL_EXPORT LPWSTR Marshal_InOut(/*[In,Out]*/LPWSTR s)
{

    //Check the Input
	size_t len = wcslen(s);

    if((len != lenstrManaged)||(wmemcmp(s,(WCHAR*)strManaged,len)!=0))
    {
        printf("Error in Function Marshal_InOut(Native Client)\n");
        return ReturnErrString();
    }

    //In-Place Change
    wcsncpy_s(s,len+1,strNative,lenstrNative);

    //Return
    return ReturnString();
}

extern "C" DLL_EXPORT LPWSTR Marshal_Out(/*[Out]*/LPWSTR s)
{
    s = (LPWSTR)CoTaskMemAlloc(sizeof(WCHAR)*(lenstrNative+1));;
    memset(s,0, sizeof(WCHAR)*(lenstrNative + 1));

    //In-Place Change
    wcsncpy_s(s,lenstrNative+1,strNative,lenstrNative);

    //Return
    return ReturnString();
}


extern "C" DLL_EXPORT LPWSTR MarshalPointer_InOut(/*[in,out]*/LPWSTR *s)
{
    //Check the Input
	size_t len = wcslen(*s);
    if((len != lenstrManaged)||(wmemcmp(*s,(WCHAR*)strManaged,len)!=0))
    {
        printf("Error in Function MarshalPointer_InOut\n");     
        return ReturnErrString();
    }

    //Allocate New
    CoTaskMemFree(*s);

    //Alloc New
	size_t length = lenstrNative + 1;
    *s = (LPWSTR)CoTaskMemAlloc(length * sizeof(WCHAR));
    memset(*s,'\0',length  * sizeof(WCHAR));
    wcsncpy_s(*s,length,strNative,lenstrNative);

    //Return
    return ReturnString();
}

extern "C" DLL_EXPORT LPWSTR MarshalPointer_Out(/*[out]*/ LPWSTR *s)
{
	size_t length = lenstrNative+1;
    *s = (LPWSTR)CoTaskMemAlloc(sizeof(WCHAR)*length);
	memset(*s, '\0', length  * sizeof(WCHAR));
    wcsncpy_s(*s,length,strNative,lenstrNative);

    return ReturnString();
}

typedef LPTSTR (__stdcall * Test_Del_MarshalStrB_InOut)(/*[in,out]*/ LPTSTR s);
extern "C" DLL_EXPORT BOOL __stdcall ReverseP_MarshalStrB_InOut(Test_Del_MarshalStrB_InOut d, /*[in]*/ LPCTSTR  s)
{
    LPTSTR ret = d((LPTSTR)s);
    LPTSTR expectedret =(LPTSTR)W("Native");
    LPTSTR expectedstr = (LPTSTR)W("m");

	size_t lenret = _tcslen(ret);
	size_t lenexpectedret = _tcslen(expectedret);
    if((lenret != lenexpectedret)||(_tcsncmp(ret,expectedret,lenret)!=0))
    {
        printf("Error in ReverseP_MarshalStrB_InOut, Returned value didn't match\n");
        return FALSE;
    }

	size_t lenstr = _tcslen(s);
	size_t lenexpectedstr = _tcslen(expectedstr);
    if((lenstr != lenexpectedstr)||(_tcsncmp(s,expectedstr,lenstr)!=0))
    {
        printf("Error in ReverseP_MarshalStrB_InOut, Changed value didn't reflect on native side.\n");
        return FALSE;
    }

    return TRUE;
}

typedef LPTSTR (__cdecl * Test_Del_MarshalStrB_Out)(/*[out]*/ LPTSTR * s);
extern "C" DLL_EXPORT BOOL __stdcall ReverseP_MarshalStrB_Out(Test_Del_MarshalStrB_Out d)
{
    LPTSTR s;
    LPTSTR ret = d((LPTSTR*)&s);
    LPTSTR expectedret = (LPTSTR)W("Native");
    LPTSTR expectedstr = (LPTSTR)W("Managed");

    size_t lenret = _tcslen(ret);
	size_t lenexpectedret = _tcslen(expectedret);
    if((lenret != lenexpectedret)||(_tcsncmp(ret,expectedret,lenret)!=0))
    {
        printf("Error in ReverseP_MarshalStrB_Out, Returned value didn't match\n");
        return FALSE;
    }

	size_t lenstr = _tcslen(s);
	size_t lenexpectedstr = _tcslen(expectedstr);
    if((lenstr != lenexpectedstr)||(_tcsncmp(s,expectedstr,lenstr)!=0))
    {
        printf("Error in ReverseP_MarshalStrB_Out, Changed value didn't reflect on native side.\n");
        return FALSE;
    }

    return TRUE;

}