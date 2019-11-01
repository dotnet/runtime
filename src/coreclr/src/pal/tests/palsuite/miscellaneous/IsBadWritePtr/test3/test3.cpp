// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

/*============================================================
**
** Source: test3.c
**
** Purpose: 
** Check that IsBadWritePtr returns non-zero on Read-only memory.
**
**
**=========================================================*/

#include <palsuite.h>

int __cdecl main(int argc, char *argv[]) {
    
    LPVOID PageOne;

    if(0 != (PAL_Initialize(argc, argv)))
    {
	return FAIL;
    }
    
    /* Reserve enough space for four pages.  We'll commit this memory
       and set the correct access for each page below.
    */
    
    PageOne = VirtualAlloc(NULL, 
			   GetOsPageSize(), 
			   MEM_COMMIT, 
			   PAGE_READONLY);

    if(PageOne == NULL)
    {
	Fail("ERROR: VirtualAlloc failed to commit the required memory.\n");
    }

    if(IsBadWritePtr(PageOne,GetOsPageSize()) == 0)
    {
	VirtualFree(PageOne,0,MEM_RELEASE);

	Fail("ERROR: IsBadWritePtr returned 0 when checking a section of "
	     "read-only memory.  It should be non-zero.\n");
    }

    VirtualFree(PageOne,0,MEM_RELEASE);
    PAL_Terminate();
    return PASS;
}





