#include "platformdefines.cpp"
#include <stdio.h>
#include <stdlib.h>
#include <xplatform.h>

//Value Pass N-->M	M--->N
//STDcall	 200	 9999
//Cdecl		 -1		 678


int WINAPI CdeTest()
{
	return -1;
}

typedef int (WINAPI *pFunc)();
typedef int (WINAPI *Cdeclcaller)(pFunc);
extern "C" DLL_EXPORT BOOL WINAPI DoCallBack_Cdecl(Cdeclcaller caller)
{
	printf("DoCallBack_Cdecl\n");
	
	if(678 != caller(CdeTest))
	{
	    printf("Error:The Return value from caller is wrong!\n");
		return FALSE;
	}
	return TRUE;
}