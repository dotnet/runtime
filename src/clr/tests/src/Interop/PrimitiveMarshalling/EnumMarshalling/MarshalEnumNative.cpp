#include <xplatform.h>
#include <stdio.h>
#include <stdlib.h>
#include <string.h>

typedef void *voidPtr;

extern "C" DLL_EXPORT long _cdecl CdeclEnum(int r,BOOL *result) 
{
	if(r != 3)
	{
		printf("\nThe enum value is different from expected one\n");
		*(result)= FALSE;
		return 0;
	}
	return r;
}


extern "C" DLL_EXPORT voidPtr _cdecl GetFptr(int i)
{
	return (voidPtr) &CdeclEnum;
}
