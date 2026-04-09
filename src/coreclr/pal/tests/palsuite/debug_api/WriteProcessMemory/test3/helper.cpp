// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

/*=============================================================
**
** Source: helper.c

**
==============================================================*/


/*
**
** Purpose: This helper process sets up a several blocks of memory
** that should be unwritable from the parent process, then uses a file
** to tell its parent process where that memory is so it can attempt a
** WriteProcessMemory on it. When the parent process is done we check
** here that it was (properly) unable to change the contents of the
** memory.
*/

#include "commonconsts.h"

#include <palsuite.h>

struct allhandles_t 
{
    HANDLE hEvToHelper;
    HANDLE hEvFromHelper;
    char *valuesFileName;
};


/* function: wpmVerifyCant
 *
 * This is a general WriteProcessMemory testing function that sets up
 * the RAM pointed to and tells the companion process on the other end
 * of the handles in 'Comms' to attempt to alter 'lenDest' bytes at
 * '*pDest'.
 *
 * However, the memory at pDest[0..lenDest] is expected to be unwritable by
 * the companion process.  The companion is expects this.  This function 
 * verifies that no bytes were affected
 */

int wpmVerifyCant(struct allhandles_t Comms,
                  char * pDest, unsigned int lenDest, 
                  unsigned int lenLegitDest,
                  DWORD dwExpectedErrorCode,
                  const char* storageDescription)
{
    char *pCurr;
    FILE *commsFile;
    DWORD dwRet;

    unsigned int lenSafe = min(lenDest, lenLegitDest);

    PAL_TRY 
    {
        memset(pDest, initialValue, lenSafe);
    } 
    PAL_EXCEPT_EX (setup, EXCEPTION_EXECUTE_HANDLER)
    {
        Trace("WriteProcessMemory: bug in test values for '%s' (%p, %u, %u), "
              "the initial memset threw an exception.\n",
              storageDescription, pDest, lenDest, lenSafe);
    }
    PAL_ENDTRY;

    /* tell the parent what RAM to attempt to adjust */
    if(!(commsFile = fopen(Comms.valuesFileName, "w"))) 
    {
        Trace("WriteProcessMemory: fopen of '%S' failed (%u). \n",  
             Comms.valuesFileName, GetLastError());
        return FALSE;
    }
    if (!fprintf(commsFile, "%u %u %u '%s'\n", 
                 pDest, lenDest, dwExpectedErrorCode, storageDescription))
    {
        Trace("WriteProcessMemory: fprintf to '%S' failed (%u). \n",  
             Comms.valuesFileName, GetLastError());
        return FALSE;
    }
    PEDANTIC1(fclose, (commsFile));

    /* Tell the parent the data is ready for it to adjust */
    PEDANTIC(ResetEvent, (Comms.hEvToHelper)); 
    PEDANTIC(SetEvent, (Comms.hEvFromHelper)); 

    dwRet = WaitForSingleObject(Comms.hEvToHelper, TIMEOUT); 
    if (dwRet != WAIT_OBJECT_0)
    {
        Trace("helper WaitForSingleObjectTest:  WaitForSingleObject "
              "failed (%u)\n", GetLastError());
        return FALSE;
    }

    PAL_TRY 
    {
        /* check the stuff (as much as we can) that should NOT have changed */
        for (pCurr = pDest; pCurr < (pDest + lenSafe); pCurr++ ) 
        {
            if ( *pCurr != initialValue)
            {
                Trace("When testing '%s': real memory values preservation failed "
                      "at %u offset %u. Found '%c' instead of '%c'\n.",
                      storageDescription, pDest, pCurr - pDest, 
                      *pCurr, initialValue);
                return FALSE;
            }
        }
    } 
    PAL_EXCEPT_EX (testing, EXCEPTION_EXECUTE_HANDLER)
    {
        Trace("WriteProcessMemory: bug in test values for '%s' (%p, %u, %u), "
              "the verification pass threw an exception.\n",
              storageDescription, pDest, lenDest, lenSafe);
    }
    PAL_ENDTRY;

    return TRUE;
}

PALTEST(debug_api_WriteProcessMemory_test3_paltest_writeprocessmemory_test3_helper, "debug_api/WriteProcessMemory/test3/paltest_writeprocessmemory_test3_helper")
{
    BOOL  success = TRUE;  /* assume success */
    struct allhandles_t Comms = {0,0,0} ;

    SYSTEM_INFO sysinfo;
     
    char* Memory;

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
        success = FALSE;
        goto EXIT;
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

    /* test setup */
    GetSystemInfo(&sysinfo);

    {
        unsigned int allocSize = sysinfo.dwPageSize * 2;
        unsigned int writeLen = allocSize * 2;

        /* First test: overrun the allocated memory */
        Memory = (char*)VirtualAlloc(NULL, allocSize, 
                                     MEM_COMMIT, PAGE_READWRITE);
        
        if(Memory == NULL)
        {
            Fail("ERROR: Attempted to commit two pages, but the "
                 " VirtualAlloc call failed.  "
                 "GetLastError() returned %u.\n",GetLastError());
        }
        success &= wpmVerifyCant(Comms, Memory, writeLen, allocSize, 
                                 ERROR_INVALID_ADDRESS,
                                 "should not write beyond committed allocation");
        
        PEDANTIC1(VirtualFree, (Memory, allocSize, 
                               MEM_DECOMMIT | MEM_RELEASE));
    }

    {
        /* Allocate the memory as readonly */
        unsigned int allocSize = sysinfo.dwPageSize * 2;
        unsigned int writeLen = allocSize;

        Memory = (char*)VirtualAlloc(NULL, allocSize, 
                                     MEM_COMMIT, PAGE_READONLY);
        
        if(Memory == NULL)
        {
            Fail("ERROR: Attempted to commit two pages readonly, but the "
                 " VirtualAlloc call failed.  "
                 "GetLastError() returned %u.\n",GetLastError());
        }
        success &= wpmVerifyCant(Comms, Memory, writeLen, 0, 
                                 ERROR_NOACCESS,
                                 "should not write in READONLY allocation");
        
        PEDANTIC1(VirtualFree, (Memory, allocSize, 
                               MEM_DECOMMIT | MEM_RELEASE));
    }


    {
        /* attempt to write to memory that is not committed yet */
        unsigned int allocSize = sysinfo.dwPageSize * 2;
        unsigned int writeLen = allocSize;

        Memory = (char*)VirtualAlloc(NULL, allocSize, 
                                     MEM_RESERVE, PAGE_NOACCESS);
        
        if(Memory == NULL)
        {
            Fail("ERROR: Attempted to reserve two pages, but the "
                 " VirtualAlloc call failed.  "
                 "GetLastError() returned %u.\n",GetLastError());
        }
        success &= wpmVerifyCant(Comms, Memory, writeLen, 0, 
                                 ERROR_INVALID_ADDRESS,
                                 "should not write in memory that is"
                                 " RESERVED but not COMMITTED");
        
        PEDANTIC1(VirtualFree, (Memory, allocSize, MEM_RELEASE));
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

    PEDANTIC(CloseHandle, (Comms.hEvToHelper));
    PEDANTIC(CloseHandle, (Comms.hEvFromHelper));

    if (!success) 
    {
        Fail("");
    }

    PAL_TerminateEx(success ? PASS : FAIL);

    return success ? PASS : FAIL;
}
