// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

/*============================================================
**
** Source : test.c
**
** Purpose: Positive Test for GetUserName() function
**
**
**=========================================================*/

#define UNICODE
#include <palsuite.h>

int __cdecl main(int argc, char *argv[]) 
{

    WCHAR wzUserName[UNLEN+1];
    DWORD dwSize = sizeof(wzUserName)/sizeof(wzUserName[0]);
  
    // Initialize the PAL and return FAILURE if this fails
    if(0 != (PAL_Initialize(argc, argv)))
    {
        Fail ("ERROR: PAL_Initialize() call failed!\n");
    }

    if (0 == GetUserName(wzUserName, &dwSize))
    {
        Fail("ERROR: GetUserName failed with %d!\n", GetLastError());
    }
  
    // dwSize is the length of wzUserName with NULL
    if (dwSize <= 0 || dwSize > (sizeof(wzUserName)/sizeof(wzUserName[0])))
    {
        Fail("ERROR: GetUserName returned %S with dwSize = %u whereas the passed in buffer size is %d!\n",
                wzUserName, dwSize, sizeof(wzUserName)/sizeof(wzUserName[0]));
    }

    // dwSize is the length of wzUserName with NULL
    if (dwSize != wcslen(wzUserName)+1)
    {
        Fail("ERROR: GetUserName returned %S of length %d which is not equal to dwSize-1 = %u!\n",
                wzUserName, wcslen(wzUserName), dwSize-1);
    }

    printf ("GetUserName returned %S of length %u\n", wzUserName, dwSize-1);

    PAL_Terminate();
    return PASS;
}
