//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information. 
//

/*============================================================
**
** Source : test.c
**
** Purpose: Test for GetEnvironmentVariable() function
** Pass a nonexisting environment variable and a null to
** the function to check return values.
**
**
**=========================================================*/

#include <palsuite.h>

#define BUFFER_SIZE 5000
#define SMALL_BUFFER_SIZE 5

int __cdecl main(int argc, char *argv[]) 
{

    int ReturnValueForNonExisting = 0;
    int ReturnValueForNull = 0;

    char pResultBuffer[BUFFER_SIZE];
    char pSmallBuffer[SMALL_BUFFER_SIZE];

    if(0 != (PAL_Initialize(argc, argv)))
    {
        return FAIL;
    }

    /* This variable doesn't exist, it should return 0 */
    ReturnValueForNonExisting = GetEnvironmentVariable("NonExistingVariable",
                                                       pSmallBuffer,
                                                       SMALL_BUFFER_SIZE);

    if(ReturnValueForNonExisting != 0) 
    {
        Fail("ERROR: The return should have been 0, but it was %d.  "
             "The function attempted to get an Environment Variable that "
             "doesn't exist and should return 0 as a result.\n",
             ReturnValueForNonExisting);
    }
  
  
    /* Passing a NULL string should return 0 */
    ReturnValueForNull = GetEnvironmentVariable(NULL,
                                                pResultBuffer,
                                                BUFFER_SIZE);
  
    if(ReturnValueForNull != 0) 
    {
        Fail("ERROR: The return should have been 0, but it was %d.  "
             "The function attempted to get a NULL pointer and should return "
             "0 as a result.\n",ReturnValueForNull);
    
    }

    PAL_Terminate();
    return PASS;
  }



