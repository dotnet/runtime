#include <xplatform.h>
#include <stdio.h>
#include <stdlib.h>

const int iManaged = 10;
const int iNative = 11;


extern "C"  BOOL _cdecl MarshalRefInt_Cdcel(int * pint)
{
	//Check the Input	
	if(iManaged != *pint)
	{
		printf("The parameter for MarshalRefCharArray is wrong!\n");
		return FALSE;
	}
	*pint = iNative;
	return TRUE;
}

extern "C"  BOOL __stdcall MarshalRefInt_Stdcall(int * pint)
{
	//Check the Input
	if(iManaged != *pint)
	{
		printf("The parameter for MarshalRefCharArray is wrong!\n");
		return FALSE;
	}
	*pint = iNative;
	return TRUE;
}

typedef BOOL (_cdecl *Cdeclcaller)(int* pint);
extern "C"  BOOL __stdcall DoCallBack_MarshalRefInt_Cdecl(Cdeclcaller caller)
{
	//Check the Input
	int itemp = iNative;
	if(!caller(&itemp))
	{
	   printf("DoCallBack_MarshalRefInt_Cdecl:The Caller() return false!\n");
	   return FALSE; 
	}
	if(itemp!=iManaged)
	{
	    printf("DoCallBack_MarshalRefInt_Cdecl:The Reference Parameter returns wrong value\n");
		return FALSE;
	}
	return TRUE;
}

typedef BOOL (__stdcall *Stdcallcaller)(int* pint);
extern "C"  BOOL __stdcall DoCallBack_MarshalRefInt_Stdcall(Stdcallcaller caller)
{
	//Check the Input
	int itemp = iNative;
	if(!caller(&itemp))
	{
	    printf("DoCallBack_MarshalRefInt_Stdcall:The Caller() return FALSE!\n");
	    return FALSE;
	}
	if(itemp!=iManaged)
	{
	    printf("DoCallBack_MarshalRefInt_Stdcall: The Reference Parameter returns wrong value\n");
		return FALSE;
	}
	return TRUE;
}

typedef BOOL (_cdecl * DelegatePInvokeCdecl)(int * pint);
extern "C" DelegatePInvokeCdecl MarshalRefInt_DelegatePInvoke_Cdecl()
{
	return MarshalRefInt_Cdcel;
}


typedef BOOL (__stdcall * DelegatePInvokeStdcall)(int *pint);
extern "C" DelegatePInvokeStdcall __stdcall MarshalRefInt_DelegatePInvoke_StdCall()
{
	return MarshalRefInt_Stdcall;
}
