// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

/*============================================================
**
** Source : test.c
**
** Purpose: Positive Test for GetComputerName() function
**
**
**=========================================================*/

#define UNICODE
#include <palsuite.h>

int __cdecl main(int argc, char *argv[]) 
{

    int HOST_NAME_MAX = 255;
    WCHAR wzComputerName[HOST_NAME_MAX+1];
    DWORD dwSize = sizeof(wzComputerName)/sizeof(wzComputerName[0]);
  
    // Initialize the PAL and return FAILURE if this fails
    if(0 != (PAL_Initialize(argc, argv)))
    {
        Fail ("ERROR: PAL_Initialize() call failed!\n");
    }

    if (0 == GetComputerName(wzComputerName, &dwSize))
    {
        Fail("ERROR: GetComputerName failed with %d!\n", GetLastError());
    }
  
    // dwSize is the length of wzComputerName without NULL 
    if (dwSize < 0 || dwSize > (sizeof(wzComputerName)/sizeof(wzComputerName[0]) - 1))
    {
        Fail("ERROR: GetComputerName returned %S with dwSize = %u whereas the passed in buffer size is %d!\n",
                wzComputerName, dwSize, sizeof(wzComputerName)/sizeof(wzComputerName[0]));
    }

    // dwSize is the length of wzComputerName without NULL
    if (dwSize != wcslen(wzComputerName))
    {
        Fail("ERROR: GetComputerName returned %S of length %d which is not equal to dwSize = %u!\n",
                wzComputerName, wcslen(wzComputerName), dwSize);
    }

    printf ("GetComputerName returned %S of length %u\n", wzComputerName, dwSize);

    PAL_Terminate();
    return PASS;
}
