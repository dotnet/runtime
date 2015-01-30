//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information. 
//

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
