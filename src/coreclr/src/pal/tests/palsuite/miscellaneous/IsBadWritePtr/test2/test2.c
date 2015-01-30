//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information. 
//

/*============================================================
**
** Source: test2.c
**
** Purpose: 
** Create three consecuative pages, NOACCES, READWRITE and
** NOACCESS.  Check to ensure that the READWRITE page returns 0, to 
** ensure that IsBadWritePtr isn't overflowing.  Also check the other two
** pages to see that they return non-zero.
**
**
**=========================================================*/

#include <palsuite.h>
#define PAGE_SIZE 4096

int __cdecl main(int argc, char *argv[]) {
    
    LPVOID PageOne, PageTwo, PageThree;

    if(0 != (PAL_Initialize(argc, argv)))
    {
	return FAIL;
    }
    
    /* Reserve enough space for four pages.  We'll commit this memory
       and set the correct access for each page below.
    */
    
    PageOne = VirtualAlloc(NULL, 
			   PAGE_SIZE*4, 
			   MEM_RESERVE, 
			   PAGE_NOACCESS);

    if(PageOne == NULL)
    {
	Fail("ERROR: VirtualAlloc failed to reserve the required memory.\n");
    }

    /* Set the first Page to PAGE_NOACCESS */
    
    PageOne = VirtualAlloc(PageOne,
			   PAGE_SIZE,
			   MEM_COMMIT,
			   PAGE_NOACCESS);

    if(PageOne == NULL)
    {
	VirtualFree(PageOne,0,MEM_RELEASE);

	Fail("ERROR: VirtualAlloc failed to commit the required memory "
	     "for the first page.\n");
    }

    /* Set the second Page to PAGE_READWRITE */

    PageTwo = VirtualAlloc(((BYTE*)PageOne)+PAGE_SIZE,
			   PAGE_SIZE,
			   MEM_COMMIT,
			   PAGE_READWRITE);
    if(PageTwo == NULL)
    {
	VirtualFree(PageOne,0,MEM_RELEASE);

	Fail("ERROR: VirtualAlloc failed to allocate the required memory "
	     "for the second page. %d\n",GetLastError());
    }
    
    /* Set the third Page to PAGE_NOACCESS */

    PageThree = VirtualAlloc(((BYTE*)PageTwo) + (2 * PAGE_SIZE),
			     PAGE_SIZE,
			     MEM_COMMIT,
			     PAGE_NOACCESS);
      
    if(PageThree == NULL)
    {
	VirtualFree(PageOne,0,MEM_RELEASE);

	Fail("ERROR: VirtualAlloc failed to allocate the required memory. "
	     "For the third page.\n");
    }
    
    
/* Check that calling IsBadWritePtr on the first page returns non-zero */
    
    if(IsBadWritePtr(PageThree,PAGE_SIZE) == 0)
    {
	VirtualFree(PageOne,0,MEM_RELEASE);

	Fail("ERROR: Called IsBadWritePtr on a page which was set NOACCESS "
	     "but the return value was 0, indicating that the memory is "
	     "writable.\n");
    }

    /* Check that calling IsBadWritePtr on the middle page returns 0 */

    if(IsBadWritePtr(PageTwo,PAGE_SIZE) != 0)
    {
	VirtualFree(PageOne,0,MEM_RELEASE);

	Fail("ERROR: IsBadWritePtr didn't return 0 when called on a "
	     "page which should have been writable.\n");
    }

    /* Check that calling IsBadWritePtr on the third page returns non-zero */
    
    if(IsBadWritePtr(PageThree,PAGE_SIZE) == 0)
    {
	VirtualFree(PageOne,0,MEM_RELEASE);

	Fail("ERROR: Called IsBadWritePtr on a page which was set NOACCESS "
	     "but the return value was 0, indicating that the memory is "
	     "writable.\n");
    }
    VirtualFree(PageOne,0,MEM_RELEASE);
    PAL_Terminate();
    return PASS;
}





