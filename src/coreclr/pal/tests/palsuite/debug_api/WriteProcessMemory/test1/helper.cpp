// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

/*=============================================================
**
** Source: helper.c
**
** Purpose: This helper process sets up a several blocks of memory, 
** then uses a file to tell its parent process where that memory is 
** So it can do a WriteProcessMemory on it. When the parent process is done 
** we check here that it was written properly.
**
**
**============================================================*/

#include "commonconsts.h"

#include <palsuite.h>

struct allhandles_t 
{
    HANDLE hEvToHelper;
    HANDLE hEvFromHelper;
    char *valuesFileName;
};


/* function: wpmDoIt
 *
 * This is a general WriteProcessMemory testing function that sets up
 * the RAM pointed to and tells the companion process on the other end
 * of the handles in 'Comms' to attempt to alter 'lenDest' bytes at
 * '*pDest'.
 *
 * '*pBuffer'[0..'lenBuffer'] is expected to be a guard region
 * surrounding the '*pDest'[0..'lenDest'] region so that this function
 * can verify that only the proper bytes were altered.
 */

int wpmDoIt(struct allhandles_t Comms,
         char * pBuffer, unsigned int lenBuffer, 
         char * pDest, unsigned int lenDest, 
         const char* storageDescription)
{
    char *pCurr;
    FILE *commsFile;
    DWORD dwRet;

    if (pBuffer > pDest || lenDest > lenBuffer)
    {
        Trace("WriteProcessMemory::DoIt() test implementation: "
              "(pBuffer > pDest || lenDest > lenBuffer)\n");
        return FALSE;
    }

    /* set up the storage */
    memset(pBuffer, guardValue, lenBuffer);
    memset(pDest, initialValue, lenDest);

    /* tell the parent what RAM to adjust */
    if(!(commsFile = fopen(Comms.valuesFileName, "w"))) 
    {
        Trace("WriteProcessMemory: fopen of '%S' failed (%u). \n",  
             Comms.valuesFileName, GetLastError());
        return FALSE;
    }
    if (!fprintf(commsFile, "%u %u '%s'\n", 
                 pDest, lenDest, storageDescription))
    {
        Trace("WriteProcessMemory: fprintf to '%S' failed (%u). \n",  
             Comms.valuesFileName, GetLastError());
        return FALSE;
    }
    PEDANTIC1(fclose, (commsFile));

    /* Tell the parent the data is ready for it to adjust */
    PEDANTIC(ResetEvent, (Comms.hEvToHelper)); 
    PEDANTIC(SetEvent, (Comms.hEvFromHelper)); 

    dwRet = WaitForSingleObject(Comms.hEvToHelper, TIMEOUT); /* parent is done */
    if (dwRet != WAIT_OBJECT_0)
    {
        Trace("helper WaitForSingleObjectTest:  WaitForSingleObject "
              "failed (%u)\n", GetLastError());
        return FALSE;
    }

    /* check the stuff that SHOULD have changed */
    for (pCurr = pDest; pCurr < (pDest + lenDest); pCurr++) 
    {
        if ( *pCurr != nextValue)
        {
            Trace("When testing '%s': alteration test failed "
                  "at %u offset %u. Found '%c' instead of '%c'\n.",
                  storageDescription, pDest, pCurr - pDest, *pCurr, nextValue);
            Trace(" 'Altered' string: '%.*s'\n",lenBuffer, pBuffer);
            return FALSE;
        }
    }
    /* check the stuff that should NOT have changed */
    for (pCurr = pBuffer; pCurr < pDest; pCurr++ ) 
    {
        if ( *pCurr != guardValue)
        {
            Trace("When testing '%s': leading guard zone test failed "
                  "at %u offset %u. Found '%c' instead of '%c'\n.",
                  storageDescription, pDest, pCurr - pBuffer, *pCurr, guardValue);
            Trace(" 'Altered' string: '%.*s'\n",lenBuffer, pBuffer);
            return FALSE;
        }
    }
    for (pCurr = pDest + lenDest; pCurr < (pBuffer + lenBuffer); pCurr++ ) 
    {
        if ( *pCurr != guardValue)
        {
            Trace("When testing '%s': trailing guard zone test failed "
                  "at %u offset %u. Found '%c' instead of '%c'\n.",
                  storageDescription, pDest + lenDest, pCurr - pBuffer, *pCurr, guardValue);
            Trace(" 'Altered' string: '%.*s'\n",lenBuffer, pBuffer);
            return FALSE;
        }
    }

    return TRUE;
}

PALTEST(debug_api_WriteProcessMemory_test1_paltest_writeprocessmemory_test1_helper, "debug_api/WriteProcessMemory/test1/paltest_writeprocessmemory_test1_helper")
{
     
    BOOL  success = TRUE;  /* assume success */
    struct allhandles_t Comms = {0,0,0} ;

    /* variables to track storage to alter */
    char *pTarget = NULL;
    unsigned int sizeTarget;

    if(0 != (PAL_Initialize(argc, argv)))
    {
        return FAIL;
    }

    /* hook up with the events created by the parent */
    Comms.hEvToHelper = OpenEventW(EVENT_ALL_ACCESS, 0, szcToHelperEvName);
    if (!Comms.hEvToHelper) 
    {
        Fail("WriteProcessMemory: OpenEvent of '%S' failed (%u). "
             "(the event should already exist!)\n", 
             szcToHelperEvName, GetLastError());
    }
    Comms.hEvFromHelper = OpenEventW(EVENT_ALL_ACCESS, 0, szcFromHelperEvName);
    if (!Comms.hEvToHelper) 
    {
        Trace("WriteProcessMemory: OpenEvent of '%S' failed (%u). "
              "(the event should already exist!)\n",  
              szcFromHelperEvName, GetLastError());
        success = FALSE;
        goto EXIT;
    }
    Comms.valuesFileName = argv[1];

    {
        char autoAllocatedOnStack[51];

        /* Get the parent process to write to the local stack */
        success &= wpmDoIt(Comms, autoAllocatedOnStack, 
                          sizeof(autoAllocatedOnStack),
                          autoAllocatedOnStack + sizeof(int), 
                          sizeof(autoAllocatedOnStack) - 2 * sizeof(int),
                          "const size array on stack with int sized guards");
    }

    /* Get the parent process to write to stuff on the heap */
    sizeTarget =  2 * sizeof(int) + 23 ;  /* 23 is just a random prime > 16 */
    if (!(pTarget = (char*)malloc(sizeTarget))) 
    {
        Trace("WriteProcessMemory helper: unable to allocate '%s'->%d bytes of memory"
              "(%u).\n",
              argv[3], sizeTarget, GetLastError());
        success = FALSE;
        goto EXIT;
        
    }
    success &= wpmDoIt(Comms, pTarget, sizeTarget,
                      pTarget + sizeof(int), 
                      sizeTarget - 2 * sizeof(int),
                      "array on heap with int sized guards");

    /* just to be nice try something 16 - 2 * sizeof(int) bytes long */
    {
        char autoAllocatedOnStack[16];

        /* Get the parent process to write to the local stack */
        success &= wpmDoIt(Comms, autoAllocatedOnStack, 
                          sizeof(autoAllocatedOnStack),
                          autoAllocatedOnStack + sizeof(int), 
                          sizeof(autoAllocatedOnStack) - 2 * sizeof(int),
                          "another 16 byte array on stack with int sized guards inside");
    }

    /* NOTE: Don't try 0 bytes long.  Win32 WriteProcessMemory claims
     * it writes 8 bytes in that case! */

    /* and 1 byte long... */
    {
        char autoAllocatedOnStack[1+ 2 * sizeof(int)];

        /* Get the parent process to write to the local stack */
        success &= wpmDoIt(Comms, autoAllocatedOnStack, 
                           sizeof(autoAllocatedOnStack),
                           autoAllocatedOnStack + sizeof(int), 
                           1,
                           "no bytes with int sized guards outside on stack");
    }


EXIT:
    /* Tell the parent that we are done */
    if (!DeleteFile(Comms.valuesFileName))
    {
        Trace("helper: DeleteFile failed so parent (test1) is unlikely "
             "to exit cleanly\n");
    }
    PEDANTIC(ResetEvent, (Comms.hEvToHelper)); 
    if (!SetEvent(Comms.hEvFromHelper))
    {
        Trace("helper: SetEvent failed so parent (test1) is unlikely "
              "to exit cleanly\n");
    }

    free(pTarget);
    PEDANTIC(CloseHandle, (Comms.hEvToHelper));
    PEDANTIC(CloseHandle, (Comms.hEvFromHelper));

    if (!success) 
    {
        Fail("");
    }

    PAL_TerminateEx(success ? PASS : FAIL);

    return success ? PASS : FAIL;
}
