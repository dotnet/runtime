// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

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

PALTEST(miscellaneous_IsBadWritePtr_test2_paltest_isbadwriteptr_test2, "miscellaneous/IsBadWritePtr/test2/paltest_isbadwriteptr_test2")
{
    
    LPVOID PageOne, PageTwo, PageThree;

    if(0 != (PAL_Initialize(argc, argv)))
    {
	return FAIL;
    }
    
    /* Reserve enough space for four pages.  We'll commit this memory
       and set the correct access for each page below.
    */
    
    PageOne = VirtualAlloc(NULL, 
			   GetOsPageSize()*4,
			   MEM_RESERVE, 
			   PAGE_NOACCESS);

    if(PageOne == NULL)
    {
	Fail("ERROR: VirtualAlloc failed to reserve the required memory.\n");
    }

    /* Set the first Page to PAGE_NOACCESS */
    
    PageOne = VirtualAlloc(PageOne,
			   GetOsPageSize(),
			   MEM_COMMIT,
			   PAGE_NOACCESS);

    if(PageOne == NULL)
    {
	VirtualFree(PageOne,0,MEM_RELEASE);

	Fail("ERROR: VirtualAlloc failed to commit the required memory "
	     "for the first page.\n");
    }

    /* Set the second Page to PAGE_READWRITE */

    PageTwo = VirtualAlloc(((BYTE*)PageOne)+GetOsPageSize(),
			   GetOsPageSize(),
			   MEM_COMMIT,
			   PAGE_READWRITE);
    if(PageTwo == NULL)
    {
	VirtualFree(PageOne,0,MEM_RELEASE);

	Fail("ERROR: VirtualAlloc failed to allocate the required memory "
	     "for the second page. %d\n",GetLastError());
    }
    
    /* Set the third Page to PAGE_NOACCESS */

    PageThree = VirtualAlloc(((BYTE*)PageTwo) + (2 * GetOsPageSize()),
			     GetOsPageSize(),
			     MEM_COMMIT,
			     PAGE_NOACCESS);
      
    if(PageThree == NULL)
    {
	VirtualFree(PageOne,0,MEM_RELEASE);

	Fail("ERROR: VirtualAlloc failed to allocate the required memory. "
	     "For the third page.\n");
    }
    
    
/* Check that calling IsBadWritePtr on the first page returns non-zero */
    
    if(IsBadWritePtr(PageThree,GetOsPageSize()) == 0)
    {
	VirtualFree(PageOne,0,MEM_RELEASE);

	Fail("ERROR: Called IsBadWritePtr on a page which was set NOACCESS "
	     "but the return value was 0, indicating that the memory is "
	     "writable.\n");
    }

    /* Check that calling IsBadWritePtr on the middle page returns 0 */

    if(IsBadWritePtr(PageTwo,GetOsPageSize()) != 0)
    {
	VirtualFree(PageOne,0,MEM_RELEASE);

	Fail("ERROR: IsBadWritePtr didn't return 0 when called on a "
	     "page which should have been writable.\n");
    }

    /* Check that calling IsBadWritePtr on the third page returns non-zero */
    
    if(IsBadWritePtr(PageThree,GetOsPageSize()) == 0)
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





