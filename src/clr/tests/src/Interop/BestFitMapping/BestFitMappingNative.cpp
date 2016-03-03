
#include <stdio.h>
#include <stdlib.h>
#include <locale.h>
#include <xplatform.h>


static int fails = 0; //record the fail numbers
// Overload methods for reportfailure	
static int ReportFailure(const char* s)
{
	printf(" === Fail:%s\n",s);
	return (++fails);
}

extern  "C" int GetResult()
{
	return fails;
}

//This method is used on Windows Only
extern "C" char _cdecl GetByteForWideChar()
{
#ifdef WINDOWS
	char * p = new char[3];
	WideCharToMultiByte(CP_ACP,0,W("\x263c"),-1,p,2,NULL,NULL);
	p[1]='\0';
	byte breturn = p[0];
	
	delete p;
    	
	return breturn;
#else
	return 0; //It wont be called MAC
#endif

}

//x86: Managed(Encoding: utf8)---->Marshaler(Encoding:ASCII)---->Native(Encoding:utf8)
//MAC(x64):Managed(Encoding:utf8)----->Marshaler(Encoding:utf8)---->Native(Encoding:utf8)
//Now  both side(Managed Side and native side) takes the utf8 encoding when comparing string
bool CheckInput(LPSTR str)
{
//int WideCharToMultiByte(
//  UINT CodePage, 
//  DWORD dwFlags, 
//  LPCWSTR lpWideCharStr, 
//  int cchWideChar, 
//  LPSTR lpMultiByteStr, 
//  int cbMultiByte, 
//  LPCSTR lpDefaultChar, 
//  LPBOOL lpUsedDefaultChar 
//);
#ifdef WINDOWS
	char * p = new char[3];
	WideCharToMultiByte(CP_ACP,0,W("\x263c"),-1,p,2,NULL,NULL);
	p[1]='\0';
#else
	char*  p = new char[4]; //00bc98e2,the utf8 code of "\263c",we can get these char value through the following code with C#
	p[0] = (char)0xe2;      //Encoding enc = Encoding.Default;//UTF8 Encoding
	p[1] = (char)0x98;      //Byte[] by = enc.GetBytes("\x263c");
	p[2] = (char)0xbc;
	p[3] = (char)0;
#endif
	if(0 != _tcsncmp(str,p,4))
	{
		printf("CheckInput:Expected:%s,Actual:%d\n",p,str[0]);
		delete []p;
		return false;
	}
	delete []p;
	return true;

}

//C Call,In attribute,LPstr
extern "C" LPSTR _cdecl CLPStr_In(LPSTR pStr)
{
	//Check the Input
	if(!CheckInput(pStr))
	{
		ReportFailure("CLPStr_In:Native Side");
	}

	//alloc,copy, since we cannot depend the Marshaler's activity.
	int len = strlen(pStr)+ 1 ; //+1, Include the NULL Character.
	LPSTR pBack = (LPSTR)CoTaskMemAlloc(sizeof(char) * len);
	strncpy(pBack,pStr,len);

	return pBack;
}
extern "C" LPSTR _cdecl CLPStr_Out(LPSTR pStr)
{
	const char* pTemp ="AAAA";
    int len = strlen(pTemp)+ 1 ; //+1, Include the NULL Character.
	LPSTR pBack = (LPSTR)CoTaskMemAlloc(sizeof(char) * len);
	strncpy(pBack,pTemp,strlen(pTemp)+1);
	
	strncpy(pStr,pTemp,strlen(pTemp)+1);
	
	return pBack;
}
extern "C" LPSTR _cdecl CLPStr_InOut(LPSTR pStr)
{
	//Check the Input
	if(!CheckInput(pStr))
	{
		ReportFailure("CLPStr_InOut:Native Side");
	}

	//alloc,copy, since we cannot depend the Marshaler's activity.
	int len = strlen(pStr)+ 1 ; //+1, Include the NULL Character.
	LPSTR pBack = (LPSTR)CoTaskMemAlloc(len);
	strncpy(pBack,pStr,len);

	return pBack;
}

extern "C" LPSTR _cdecl CLPStr_InByRef(LPSTR* ppStr)
{
	//Check the Input
	if(!CheckInput(*ppStr))
	{
		ReportFailure("CLPStr_InByRef:Native Side");
	}

	//alloc,copy, since we cannot depend the Marshaler's activity.
	int len = strlen(*ppStr)+ 1 ; //+1, Include the NULL Character.
	LPSTR pBack = (LPSTR)CoTaskMemAlloc(len);
	strncpy(pBack,*ppStr,len);

	return pBack;
}

extern "C" LPSTR _cdecl CLPStr_OutByRef(LPSTR* ppStr)
{
	const char* pTemp="AAAA";
    int len = strlen(pTemp)+ 1 ; //+1, Include the NULL Character.
	LPSTR pBack = (LPSTR)CoTaskMemAlloc(sizeof(char) * len);
	strncpy(pBack,pTemp,strlen(pTemp)+1);

    *ppStr = (LPSTR)CoTaskMemAlloc(sizeof(char) * len);
	strncpy(*ppStr,pTemp,strlen(pTemp)+1);
	return pBack;
}

extern "C" LPSTR _cdecl CLPStr_InOutByRef(LPSTR* ppStr)
{
	//Check the Input
	if(!CheckInput(*ppStr))
	{
		ReportFailure("CLPStr_InOutByRef:Native Side");
	}

	//alloc,copy, since we cannot depend the Marshaler's activity.
	int len = strlen(*ppStr)+ 1 ; //+1, Include the NULL Character.
	LPSTR pBack = (LPSTR)CoTaskMemAlloc(len);
	strncpy(pBack,*ppStr,len);
	return pBack;
}


typedef LPSTR (_cdecl* delegate_cdecl)(LPSTR* ppstr);
extern "C" delegate_cdecl CLPStr_DelegatePInvoke()
{
	return CLPStr_InOutByRef;
}

//stdcall

extern "C" LPSTR __stdcall SLPStr_In(LPSTR pStr)
{
	//Check the Input
	if(!CheckInput(pStr))
	{
		ReportFailure("SLPStr_In:NativeSide");
	}

	//alloc,copy, since we cannot depend the Marshaler's activity.
	int len = strlen(pStr)+ 1 ; //+1, Include the NULL Character.
	LPSTR pBack = (LPSTR)CoTaskMemAlloc(len);
	strncpy(pBack,pStr,len);
	return pBack;
}

extern "C" LPSTR __stdcall SLPStr_Out(LPSTR pStr)
{	
    const char* pTemp="AAAA";
    int len = strlen(pTemp)+ 1 ; //+1, Include the NULL Character.
	LPSTR pBack = (LPSTR)CoTaskMemAlloc(sizeof(char) * len);
	strncpy(pBack,pTemp,strlen(pTemp)+1);
	
	strncpy(pStr,pTemp,strlen(pTemp)+1);
	return pBack;
}

extern "C" LPSTR __stdcall SLPStr_InOut(LPSTR pStr)
{
	//Check the Input
	if(!CheckInput(pStr))
	{
		ReportFailure("SLPStr_InOut:NativeSide");
	}

	//alloc,copy, since we cannot depend the Marshaler's activity.
	int len = strlen(pStr)+ 1 ; //+1, Include the NULL Character.
	LPSTR pBack = (LPSTR)CoTaskMemAlloc(len);
	strncpy(pBack,pStr,len);
	return pBack;
}

extern "C" LPSTR __stdcall SLPStr_InByRef(LPSTR* ppStr)
{
	//Check the Input
	if(!CheckInput(*ppStr))
	{
		ReportFailure("SLPStr_InByRef:NativeSide");
	}
	//alloc,copy, since we cannot depend the Marshaler's activity.
	int len = strlen(*ppStr)+ 1 ; //+1, Include the NULL Character.
	LPSTR pBack = (LPSTR)CoTaskMemAlloc(len);
	strncpy(pBack,*ppStr,len);
	return pBack;
}

extern "C" LPSTR __stdcall SLPStr_OutByRef(LPSTR* ppStr)
{
	const char* pTemp="AAAA";
    int len = strlen(pTemp)+ 1 ; //+1, Include the NULL Character.
	LPSTR pBack = (LPSTR)CoTaskMemAlloc(sizeof(char) * len);
	strncpy(pBack,pTemp,strlen(pTemp)+1);
	
	*ppStr = (LPSTR)CoTaskMemAlloc(sizeof(char) * len);
	strncpy(*ppStr,pTemp,strlen(pTemp)+1);

	return pBack;
}

extern "C" LPSTR __stdcall SLPStr_InOutByRef(LPSTR* ppStr)
{
	//Check the Input
	if(!CheckInput(*ppStr))
	{
		ReportFailure("SLPStr_InOutByRef:NativeSide");
	}

	//alloc,copy, since we cannot depend the Marshaler's activity.
	int len = strlen(*ppStr)+ 1 ; //+1, Include the NULL Character.
	LPSTR pBack = (LPSTR)CoTaskMemAlloc(len);
	strncpy(pBack,*ppStr,len);
	return pBack;
}

typedef LPSTR (__stdcall *delegate_stdcall)(LPSTR* ppstr);
extern "C" delegate_stdcall SLPStr_DelegatePInvoke()
{
	return SLPStr_InOutByRef;
}

///Cdecl, Reverse PInvoke

typedef LPSTR (_cdecl *CCallBackIn)(LPSTR pstr);
extern "C" void _cdecl DoCCallBack_LPSTR_In(CCallBackIn callback)
{
  const char* pTemp = "AAAA";
  int len = strlen(pTemp)+1;
  LPSTR pStr = (LPSTR)CoTaskMemAlloc(len);
  strncpy(pStr,pTemp,len);

  if(!CheckInput(callback(pStr)))
  {
  	ReportFailure("DoCCallBack_LPSTR_In:NativeSide");
  }
  CoTaskMemFree(pStr);
}

typedef LPSTR (_cdecl *CCallBackOut)(LPSTR pstr);
extern "C" void _cdecl DoCCallBack_LPSTR_Out(CCallBackOut callback)
{
	int len = 10;
	LPSTR pStr = (LPSTR)CoTaskMemAlloc(len);
	
	//Check the return value
	if(!CheckInput(callback(pStr)))
	{
		ReportFailure("DoCCallBack_LPSTR_Out:NativeSide,the first check");
	}
	if(!CheckInput(pStr))
	{
		ReportFailure("DoCCallBack_LPSTR_Out:NativeSide,the Second Check");
	}
	CoTaskMemFree(pStr);
}

typedef LPSTR (_cdecl *CCallBackInOut)(LPSTR pstr);
extern "C" void _cdecl DoCCallBack_LPSTR_InOut(CCallBackInOut callback)
{
	const char* pTemp = "AAAA";
	int len = strlen(pTemp)+1;
	LPSTR pStr = (LPSTR)CoTaskMemAlloc(len);
	strncpy(pStr,pTemp,len);

	if(!CheckInput(callback(pStr)))
	{
		ReportFailure("DoCCallBack_LPSTR_InOut:NativeSide,the first check");
	}
	if(!CheckInput(pStr))
	{
		ReportFailure("DoCCallBack_LPSTR_InOut:NativeSide,the Second Check");
	}
	CoTaskMemFree(pStr);
}

typedef LPSTR (_cdecl *CallBackInByRef)(LPSTR* pstr);
extern "C" void _cdecl DoCCallBack_LPSTR_InByRef(CallBackInByRef callback)
{
	const char* pTemp = "AAAA";
	int len = strlen(pTemp)+1;
	LPSTR pStr = (LPSTR)CoTaskMemAlloc(len);
	strncpy(pStr,pTemp,len);

	if(!CheckInput(callback(&pStr)))
	{
		ReportFailure("DoCCallBack_LPSTR_InByRef:NativeSide");
	}
	CoTaskMemFree(pStr);
}

typedef LPSTR (_cdecl *CCallBackOutByRef)(LPSTR* pstr);
extern "C" void _cdecl DoCCallBack_LPSTR_OutByRef(CCallBackOutByRef callback)
{
	int len = 10;
	LPSTR pStr = (LPSTR)CoTaskMemAlloc(len);

	if(!CheckInput(callback(&pStr)))
	{
		ReportFailure("DoCCallBack_LPSTR_OutByRef:NativeSide,the first Check");
	}
	if(!CheckInput(pStr))
	{
		ReportFailure("DoCCallBack_LPSTR_OutByRef:NativeSide,the Second Check");
	}
	CoTaskMemFree(pStr);
}

typedef LPSTR (_cdecl *CCallBackInOutByRef)(LPSTR* pstr);
extern "C" void _cdecl DoCCallBack_LPSTR_InOutByRef(CCallBackInOutByRef callback)
{
	const char* pTemp = "AAAA";
	int len = strlen(pTemp)+1;
	LPSTR pStr = (LPSTR)CoTaskMemAlloc(len);
	strncpy(pStr,pTemp,len);

	if(!CheckInput(callback(&pStr)))
	{
		ReportFailure("DoCCallBack_LPSTR_InOutByRef:NativeSide");
	}
	if(!CheckInput(pStr))
	{
		ReportFailure("DoCCallBack_LPSTR_InOutByRef:NativeSide,the Second Check");
	}
	CoTaskMemFree(pStr);
}

///STDCALL Reverse PInvoke
typedef LPSTR (__stdcall *SCallBackIn)(LPSTR pstr);
extern "C" void _cdecl DoSCallBack_LPSTR_In(SCallBackIn callback)
{
	const char* pTemp = "AAAA";
	int len = strlen(pTemp)+1;
	LPSTR pStr = (LPSTR)CoTaskMemAlloc(len);
	strncpy(pStr,pTemp,len);
	
	if(!CheckInput(callback(pStr)))
	{
		ReportFailure("DoSCallBack_LPSTR_In:NativeSide");
	}
	CoTaskMemFree(pStr);
}

typedef LPSTR (__stdcall *SCallBackOut)(LPSTR pstr);
extern "C" void _cdecl DoSCallBack_LPSTR_Out(SCallBackOut callback)
{

	int len = 10;
	LPSTR pStr = (LPSTR)CoTaskMemAlloc(len);

	if(!CheckInput(callback(pStr)))
	{
		ReportFailure("DoSCallBack_LPSTR_Out:NativeSide,the first check");
	}
	if(!CheckInput(pStr))
	{
		ReportFailure("DoSCallBack_LPSTR_Out:NativeSide,the Second Check");
	}
	CoTaskMemFree(pStr);
}

typedef LPSTR (__stdcall *SCallBackInOut)(LPSTR pstr);
extern "C" void _cdecl DoSCallBack_LPSTR_InOut(SCallBackInOut callback)
{
	const char* pTemp = "AAAA";
	int len = strlen(pTemp)+1;
	LPSTR pStr = (LPSTR)CoTaskMemAlloc(len);
	strncpy(pStr,pTemp,len);

	if(!CheckInput(callback(pStr)))
	{
		ReportFailure("DoSCallBack_LPSTR_InOut:NativeSide,the first check");
	}
	if(!CheckInput(pStr))
	{
		ReportFailure("DoSCallBack_LPSTR_InOut:NativeSide,the second Check");
	}
	CoTaskMemFree(pStr);
}

typedef LPSTR (__stdcall *SCallBackInByRef)(LPSTR* pstr);
extern "C" void _cdecl DoSCallBack_LPSTR_InByRef(SCallBackInByRef callback)
{
	const char* pTemp = "AAAA";
	int len = strlen(pTemp)+1;
	LPSTR pStr = (LPSTR)CoTaskMemAlloc(len);
	strncpy(pStr,pTemp,len);

	if(!CheckInput(callback(&pStr)))
	{
		ReportFailure("DoSCallBack_LPSTR_InByRef:NativeSide");
	}
	CoTaskMemFree(pStr);
}

typedef LPSTR (__stdcall *SCallBackOutByRef)(LPSTR* pstr);
extern "C" void _cdecl DoSCallBack_LPSTR_OutByRef(SCallBackOutByRef callback)
{
	int len = 10;
	LPSTR pStr  = (LPSTR)CoTaskMemAlloc(len);
	
	if(!CheckInput(callback(&pStr)))
	{
		ReportFailure("DoSCallBack_LPSTR_OutByRef:NativeSide,the first check");
	}
	if(!CheckInput(pStr))
	{
		ReportFailure("DoSCallBack_LPSTR_OutByRef:NativeSide,the second Check");
	}
	CoTaskMemFree(pStr);
}

typedef LPSTR (__stdcall *SCallBackInOutByRef)(LPSTR* pstr);
extern "C" void _cdecl DoSCallBack_LPSTR_InOutByRef(SCallBackInOutByRef callback)
{
	const char* pTemp = "AAAA";
	int len = strlen(pTemp)+1;
	LPSTR pStr = (LPSTR)CoTaskMemAlloc(len);
	strncpy(pStr,pTemp,len);

	if(!CheckInput(callback(&pStr)))
	{
		ReportFailure("DoSCallBack_LPSTR_InOutByRef:NativeSide,the first check");
	}
	if(!CheckInput(pStr))
	{
		ReportFailure("DoSCallBack_LPSTR_InOutByRef:NativeSide,the second Check");
	}
	CoTaskMemFree(pStr);
}
