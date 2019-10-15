// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

/*============================================================
**
** Source: test3.c 
**
** Purpose:  Tests for CreateEventA.  Create an event with an empty name, 
** create an event with the same name as an already created event 
** object.
**
**
**=========================================================*/

#include <palsuite.h>

#define SWAPPTR ((VOID *)(-1))

struct testCase 
{
    LPSECURITY_ATTRIBUTES lpEventAttributes;
    BOOL bManualReset;
    BOOL bInitialState;
    char lpName[MAX_PATH + 2];
    DWORD dwNameLen; 
    DWORD lastError;
    BOOL bResult;
};

struct testCase testCases[]=
{
    {0, TRUE, FALSE, "", 0, ERROR_SUCCESS, PASS}, 
    {0, TRUE, FALSE, "", 5, ERROR_SUCCESS, PASS},
    {0, TRUE, FALSE, "", 5, ERROR_ALREADY_EXISTS, PASS},
    {0, TRUE, FALSE, "", 6, ERROR_INVALID_HANDLE, PASS},
    {0, TRUE, FALSE, "", MAX_PATH - 1 - 60, ERROR_SUCCESS, PASS},
    {0, TRUE, FALSE, "", MAX_PATH + 1, ERROR_FILENAME_EXCED_RANGE, PASS}
};

static HANDLE hEvent[sizeof(testCases)/sizeof(struct testCase)];

DWORD result[sizeof(testCases)/sizeof(struct testCase)];

int __cdecl main(int argc, char **argv)
{

    BOOL bRet = TRUE;
    const char *nonEventName = "aaaaaa";
    HANDLE hUnnamedEvent;
    HANDLE hFMap;
    DWORD dwRet;
    int i;
    
    if(0 != (PAL_Initialize(argc, argv)))
    {
        return ( FAIL );
    }

    hUnnamedEvent = CreateEventA(0, TRUE, FALSE, NULL);
    
    if ( NULL == hUnnamedEvent )
    {
        bRet = FALSE;
        Trace ( "PALSUITE ERROR: CreateEventA (%d, %d, %d, NULL) call "
                "returned NULL.\nGetLastError returned %u.\n", 0, TRUE, FALSE,
                GetLastError());
    }

    if (!CloseHandle(hUnnamedEvent))
    {
        bRet = FALSE;
        Trace("PALSUITE ERROR: CreateEventA: CloseHandle(%lp); call "
              "failed\nGetLastError returned '%u'.\n", hUnnamedEvent, 
              GetLastError());
    }

    /* Create non-event with the same name as one of the testCases */
    hFMap = CreateFileMappingA( SWAPPTR, NULL, PAGE_READONLY, 0, 1, 
                                nonEventName ); 

    if ( NULL == hFMap )
    {
        bRet = FALSE;
        Trace ( "PALSUITE ERROR: CreateFileMapping (%p, %p, %d, %d, %d, %s)"
                " call returned NULL.\nGetLastError returned %u.\n", 
                SWAPPTR, NULL, PAGE_READONLY, 0, 0, nonEventName, 
                GetLastError());
        goto done;
    }
    
    /* Create Events */
    for (i = 0; i < sizeof(testCases)/sizeof(struct testCase); i++)
    {
        /* create name */
        memset (testCases[i].lpName, '\0', (MAX_PATH + 2));
        memset (testCases[i].lpName, 'a', testCases[i].dwNameLen );

        SetLastError(ERROR_SUCCESS);

        hEvent[i] = CreateEventA( testCases[i].lpEventAttributes, 
                                 testCases[i].bManualReset, 
                                 testCases[i].bInitialState, 
                                 testCases[i].lpName); 
        
        if (hEvent[i] != INVALID_HANDLE_VALUE)
        {
            DWORD dwError = GetLastError();

            if (dwError != testCases[i].lastError)
            {
                bRet = FALSE;
                Trace ("PALSUITE ERROR:\nCreateEventA(%lp, %d, %d, %s)"
                       "\nGetLastError returned '%u', it should have returned"
                       "'%d' at index '%d'.\n", testCases[i].lpEventAttributes,
                       testCases[i].bManualReset, testCases[i].bInitialState, 
                       testCases[i].lpName, dwError,
                       testCases[i].lastError, i);
            }
            if ( ERROR_FILENAME_EXCED_RANGE == testCases[i].lastError )
            {
                result [i] = 1;
            }
            if ( ERROR_INVALID_HANDLE == testCases[i].lastError )
            {
                result [i] = 1;
            }
            /* 
             * If we expected the testcase to FAIL and it passed,
             * report an error.
             */
            if (testCases[i].bResult == FAIL)
            {
                bRet = FALSE;
                Trace ("PALSUITE ERROR:\nCreateEventA(%lp, %d, %d, %s)"
                       "\nShould have returned INVALID_HANDLE_VALUE but "
                       "didn't at index '%d'.\n", 
                       testCases[i].lpEventAttributes, 
                       testCases[i].bManualReset, 
                       testCases[i].bInitialState,
                       testCases[i].lpName, i);
            }
            /* 
             * If result hasn't been set already set it to 0 so all the 
             * resources will be freed.
             */
            if (!result[i] )
            {
                result[i] = 0;
            }
        }
        else 
        {
            /* 
             * If we get an INVALID_HANDLE_VALUE and we expected the 
             * test case to pass, report an error.
             */
            result[i] = 1;
            
            if (testCases[i].bResult == PASS)
            {
                bRet = FALSE;
                Trace ("PALSUITE ERROR:\nCreateEventA(%lp, %d, %d, %s);"
                       "\nReturned INVALID_HANDLE_VALUE at index '%d'.\n", 
                       testCases[i].lpEventAttributes, 
                       testCases[i].bManualReset, testCases[i].bInitialState,
                       testCases[i].lpName, i);
            }
        }
    }

    /* cleanup */
    for (i = 0; i < sizeof(testCases)/sizeof(struct testCase); i++)
    {
        if (result[i])
        {
            continue;
        }
        dwRet = WaitForSingleObject ( hEvent[i], 0 );
        
        if (dwRet != WAIT_TIMEOUT)
        {
            bRet = FALSE;
            Trace("PALSUITE ERROR: CreateEventA:\nWaitForSingleObject (%lp, "
                  "%d) call failed at index %d .\nGetLastError returned "
                  "'%u'.\n", hEvent[i], 0, i, GetLastError());
        }
        
        if (!CloseHandle(hEvent[i]))
        {
            bRet = FALSE;
            Trace("PALSUITE ERROR: CreateEventA: CloseHandle(%lp) call "
                  "failed at index %d\nGetLastError returned '%u'.\n", 
                  hEvent[i], i, GetLastError());
        }
    }

done:
    if (!CloseHandle(hFMap))
    {
        bRet = FALSE;
        Trace("PALSUITE ERROR: CreateEventW: CloseHandle(%p) call "
              "failed\nGetLastError returned '%u'.\n", hFMap, 
              GetLastError());
    }

    if (FALSE == bRet)
    {
        bRet = FAIL;
    }
    else 
    {
        bRet = PASS;
    }
    
    PAL_TerminateEx(bRet);
    
    return(bRet);
}
