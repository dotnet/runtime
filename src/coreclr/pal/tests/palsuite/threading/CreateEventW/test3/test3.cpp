// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

/*============================================================
**
** Source: test3.c
**
** Purpose:  Tests for CreateEvent.  Create an unnamed event, create
** an event with an empty name, create an event with a name longer than
** MAX_PATH, MAX_PATH + 1, create an event with a name already taken
** by a non-event object, create an event with a name already taken
** by an event object.
**
**
**=========================================================*/
#include <palsuite.h>

#define SWAPPTR ((VOID *) (-1))

struct testCase
{
    LPSECURITY_ATTRIBUTES lpEventAttributes;
    BOOL bManualReset;
    BOOL bInitialState;
    WCHAR lpName[MAX_PATH + 2];
    DWORD dwNameLen;
    DWORD lastError;
    BOOL bResult;
};

PALTEST(threading_CreateEventW_test3_paltest_createeventw_test3, "threading/CreateEventW/test3/paltest_createeventw_test3")
{
    struct testCase testCases[]=
    {
        {0, TRUE, FALSE, {'\0'}, 0, ERROR_SUCCESS, PASS},
        {0, TRUE, FALSE, {'\0'}, 5, ERROR_SUCCESS, PASS},
        {0, TRUE, FALSE, {'\0'}, 5, ERROR_ALREADY_EXISTS, PASS},
        {0, TRUE, FALSE, {'\0'}, 6, ERROR_INVALID_HANDLE, PASS},
        {0, TRUE, FALSE, {'\0'}, MAX_PATH - 1 - 60, ERROR_SUCCESS, PASS},
        {0, TRUE, FALSE, {'\0'}, MAX_PATH - 60, ERROR_SUCCESS, PASS},
    };

    HANDLE hEvent[sizeof(testCases)/sizeof(struct testCase)];

    DWORD result[sizeof(testCases)/sizeof(struct testCase)];

    BOOL bRet = TRUE;
    WCHAR nonEventName[] = {'a','a','a','a','a','a','\0'};
    char name[MAX_PATH + 2];
    WCHAR *wName;
    HANDLE hFMap = NULL;
    HANDLE hUnnamedEvent;
    DWORD dwRet;
    int i;

    if(0 != (PAL_Initialize(argc, argv)))
    {
        return ( FAIL );
    }

    hUnnamedEvent = CreateEventW(0, TRUE, FALSE, NULL);

    if ( NULL == hUnnamedEvent )
    {
        bRet = FALSE;
        Trace ( "PALSUITE ERROR: CreateEventW (%d, %d, %d, NULL) call "
                "returned NULL.\nGetLastError returned %u.\n", 0, TRUE, FALSE,
                GetLastError());
        goto done;
    }

    if (!CloseHandle(hUnnamedEvent))
    {
        bRet = FALSE;
        Trace("PALSUITE ERROR: CreateEventW: CloseHandle(%lp); call "
              "failed\nGetLastError returned '%u'.\n", hUnnamedEvent,
              GetLastError());
    }

    /* Create non-event with the same name as one of the testCases */
    hFMap = CreateFileMappingW( SWAPPTR, NULL, PAGE_READONLY, 0, 1,
                                nonEventName );

    if ( NULL == hFMap )
    {
        bRet = FALSE;
        Trace ( "PALSUITE ERROR: CreateFileMapping (%p, %p, %d, %d, %d, %S)"
                " call returned NULL.\nGetLastError returned %u\n",
                SWAPPTR, NULL, PAGE_READONLY, 0, 0, nonEventName,
                GetLastError());
    }

    /* Create Events */
    for (i = 0; i < sizeof(testCases)/sizeof(struct testCase); i++)
    {
        /* create name */
        memset (name, '\0', MAX_PATH + 2);
        memset (name, 'a', testCases[i].dwNameLen );

        wName = convert(name);

        wcsncpy(testCases[i].lpName, wName,
                testCases[i].dwNameLen);

        free(wName);

        SetLastError(ERROR_SUCCESS);

        hEvent[i] = CreateEventW( testCases[i].lpEventAttributes,
                                  testCases[i].bManualReset,
                                  testCases[i].bInitialState,
                                  testCases[i].lpName);

        if (hEvent[i] != INVALID_HANDLE_VALUE)
        {
            DWORD dwError = GetLastError();

            if (dwError != testCases[i].lastError)
            {
                bRet = FALSE;
                Trace ("PALSUITE ERROR:\nCreateEvent(%lp, %d, %d, %S)"
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
                Trace ("PALSUITE ERROR:\nCreateEvent(%lp, %d, %d, %S)"
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
            if (!result[i])
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
                Trace ("PALSUITE ERROR:\nCreateEvent(%lp, %d, %d, %S);"
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
            Trace("PALSUITE ERROR: CreateEventW:\nWaitForSingleObject (%lp, "
                  "%d) call failed at index %d .\nGetLastError returned "
                  "'%u'.\n", hEvent[i], 0, i, GetLastError());
        }

        if (!CloseHandle(hEvent[i]))
        {
            bRet = FALSE;
            Trace("PALSUITE ERROR: CreateEventW: CloseHandle(%lp) call "
                  "failed at index %d\nGetLastError returned '%u'.\n",
                  hEvent[i], i, GetLastError());
        }
    }

done:
    if (hFMap != NULL && !CloseHandle(hFMap))
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



