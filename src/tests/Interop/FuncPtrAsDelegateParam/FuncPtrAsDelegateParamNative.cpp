#include <platformdefines.h>
#include <stdio.h>
#include <stdlib.h>
#include <xplatform.h>

//Value Pass N-->M	M--->N
//STDcall	 200	 9999
//Cdecl		 -1		 678


int __cdecl CdeTest()
{
	return -1;
}

typedef int (__cdecl *pFunc)();
typedef int (__cdecl *Cdeclcaller)(pFunc);
extern "C" DLL_EXPORT BOOL __cdecl DoCallBack_Cdecl(Cdeclcaller caller)
{
	printf("DoCallBack_Cdecl\n");
	
	if(678 != caller(CdeTest))
	{
	    printf("Error:The Return value from caller is wrong!\n");
		return FALSE;
	}
	return TRUE;
}
