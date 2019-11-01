// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#include "Unmanaged.h"

BOOL APIENTRY DllMain( HANDLE hModule, DWORD  ul_reason_for_call, LPVOID lpReserved )
{
	/* No Need to do anything here -- for now */
	return TRUE;
}


EXPORT VOID UnmanagedCode( int iGiven )
{
	int i;

	printf("[unmanaged code] software divide by zero:\n");
	RaiseException( EXCEPTION_INT_DIVIDE_BY_ZERO, 0, 0, 0);

	
	printf("[unmanaged code] hardware divide by zero:\n");
	i = 5 / iGiven;

    printf("[unmanaged code] ... and survived? %i\n",i);
}
