// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

/*=====================================================================
**
** Source:  SetFileTime.c
**
** Purpose: Tests the PAL implementation of the SetFileTime function.
** This test first sets a valid file time on the file which is opened.
** Then it calls GetFileTime, and compares the values.  They should
** be the same. Note: Access time isn't checked in this test.  It will
** be dealt with seperatly due to odd behaviour.
**
** Depends:
**        CreateFile
**        GetFileTime
**
**
**===================================================================*/

#include <palsuite.h>

int __cdecl main(int argc, char **argv)
{

#if WIN32
    FILETIME Creation;
    FILETIME SetCreation;
#endif
    FILETIME LastWrite;
    FILETIME SetLastWrite;
    HANDLE TheFileHandle;
    BOOL result;

    if (0 != PAL_Initialize(argc,argv))
    {
        return FAIL;
    }
  
    /* Populate some FILETIME structures with values 
       These values are valid Creation, Access and Write times
       which I generated, and should work properly.
    */
#if WIN32
    SetCreation.dwLowDateTime = 458108416;
    SetCreation.dwHighDateTime = 29436904;
#endif
    
    SetLastWrite.dwLowDateTime = -1995099136;
    SetLastWrite.dwHighDateTime = 29436915;
    

    /* Open the file to get a HANDLE */
    TheFileHandle = CreateFile("the_file",
                               GENERIC_READ|GENERIC_WRITE,
                               0,
                               NULL,
                               OPEN_ALWAYS, 
                               FILE_ATTRIBUTE_NORMAL,
                               NULL);                       
    
    if(TheFileHandle == INVALID_HANDLE_VALUE) 
    {
        Fail("ERROR: Failed to open the file.  The error number "
               "returned was %d.\n",GetLastError());
    }

    /* Set the new file time */
#if WIN32
    result = SetFileTime(TheFileHandle,
                         &SetCreation, NULL, &SetLastWrite);
#else
    result = SetFileTime(TheFileHandle,
                         NULL, NULL, &SetLastWrite);
#endif
    if(result == 0) 
    {
        Fail("ERROR: SetFileTime failed when trying to set the "
             "new file time. The GetLastError was %d.\n",GetLastError());
    }


    /* Then get the file time of the file */
#if WIN32
    result = GetFileTime(TheFileHandle, &Creation, NULL, &LastWrite);
#else
    result = GetFileTime(TheFileHandle, NULL, NULL, &LastWrite);
#endif

    if(result == 0) 
    {
        Fail("ERROR: GetFileTime failed, and this tests depends "
               "upon it working properly, in order to ensure that the "
             "file time was set with SetFileTime. GetLastError() "
             "returned %d.\n",GetLastError());
    }

    /* Compare the write time we Set to the write time aquired with
       Get. They should be the same.
    */

    if(LastWrite.dwLowDateTime != SetLastWrite.dwLowDateTime      ||
       LastWrite.dwHighDateTime != SetLastWrite.dwHighDateTime) 
    {        
        Fail("ERROR: After setting the write time, it is not "
             "equal to what it was set to.  Either Set of GetFileTime are "
             "broken.\n");
    }

    /* Within FreeBSD, the Creation time is ignored when SetFileTime 
       is called.  Since FreeBSD has no equivalent.  For that reason,
       it is not checked with the following test.
    */
    
#if WIN32
    if(Creation.dwHighDateTime != SetCreation.dwHighDateTime      ||
       Creation.dwLowDateTime != SetCreation.dwLowDateTime)
    {
        Fail("ERROR: After setting the file time, the Creation "
               "time is not what it should be.  Either Set or GetFileTime "
               "are broken.");
    }
#endif

    PAL_Terminate();
    return PASS;
}

