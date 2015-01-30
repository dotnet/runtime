//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information. 
//

/*=====================================================================
**
** Source:  LocalFileTimeToFileTime.c (test2)
**
** Purpose: Tests the PAL implementation of LocalFileTimeToFileTime
**          Call LocalFileTimeToFileTime with a known valid local time, 
**          call FileTimeToLocalTime and make sure the two local times 
**          are the same. Then take those values and call 
**          LocalFileTimeToFileTime again and make sure the file times
**          are the same.
** 
** Depends
**          FileTimeToLocalFileTime
**
**
**===================================================================*/

#include <palsuite.h>


int __cdecl main(int argc, char **argv)
{
    const DWORD dwLowDateTime = -398124352; /* valid low date value */
    const DWORD dwHighDateTime = 29437061;  /* valid high date value */
    FILETIME FileTime01;
    FILETIME FileTime02;
    FILETIME LocalTime01;
    FILETIME LocalTime02;
    
    if (0 != PAL_Initialize(argc,argv))
    {
        return FAIL;
    }


    LocalTime01.dwLowDateTime = dwLowDateTime;
    LocalTime01.dwHighDateTime = dwHighDateTime;
  
    /* get the file time */
    if(!LocalFileTimeToFileTime(&LocalTime01,&FileTime01)) 
    {
        Fail("ERROR: LocalFileTimeToFileTime failed. "
               "GetLastError returned %u.\n",
               GetLastError());
    }

    /* now convert the file time to the local time */
    if(!FileTimeToLocalFileTime(&FileTime01,&LocalTime02)) 
    {
        Fail("ERROR: FileTimeToLocalFileTime failed. "
               "GetLastError returned %u.\n",
               GetLastError());
    }

    /* LocalTime02 should be the same as the original */
    if((LocalTime02.dwLowDateTime != dwLowDateTime) ||
        (LocalTime02.dwHighDateTime != dwHighDateTime))
    {
        Fail("ERROR: After converting times back and forth, the local"
            " times are not the same:\n"
            "orig.low = %u  orig.high = %u\n"
            "new.low  = %u  new.high  = %u.\n",
            dwLowDateTime,
            dwHighDateTime,
            LocalTime02.dwLowDateTime,
            LocalTime02.dwHighDateTime);
    }

    /* and back again */
    if(!LocalFileTimeToFileTime(&LocalTime02,&FileTime02)) 
    {
        Fail("ERROR: LocalFileTimeToFileTime failed. "
               "GetLastError returned %u.\n",
               GetLastError());
    }

    /* FileTime02 should be the same as FileTime01 */
    if((FileTime01.dwLowDateTime != FileTime02.dwLowDateTime) ||
        (FileTime01.dwHighDateTime != FileTime02.dwHighDateTime))
    {
        Fail("ERROR: After converting times back again, the file times"
            " are not the same:\n"
            "file01.low = %u  file01.high = %u\n"
            "file02.low = %u  file02.high = %u.\n",
            FileTime01.dwLowDateTime,
            FileTime01.dwHighDateTime,
            FileTime02.dwLowDateTime,
            FileTime02.dwHighDateTime);
    }


    PAL_Terminate();
    return PASS;
}

