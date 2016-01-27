// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

/*=============================================================
**
** Source: helper.c
**
** Purpose: A child process which will attempt to read and write to files
** which were locked in the parent.
**
**
**============================================================*/

#include <palsuite.h>

#define BUF_SIZE 128

int __cdecl main(int argc, char *argv[])
{
    HANDLE TheFile;
    int result = 0;
    char DataBuffer[BUF_SIZE];
    DWORD BytesRead, BytesWritten;
    char fileName[] = "testfile.tmp";
    
    if(0 != (PAL_Initialize(argc, argv)))
    {
        return FAIL;
    }
    
    /* Open the same file that the parent has opened and locked */
    TheFile = CreateFile(fileName,     
                         GENERIC_READ|GENERIC_WRITE, 
                         FILE_SHARE_READ|FILE_SHARE_WRITE,
                         NULL,     
                         OPEN_EXISTING,                 
                         FILE_ATTRIBUTE_NORMAL, 
                         NULL);
    
    if (TheFile == INVALID_HANDLE_VALUE) 
    { 
        Fail("ERROR: Could not open file '%s' with CreateFile.",fileName); 
    }

    /* Attempt to Read 5 bytes from this file.  Since it is locked, this
       should fail.
    */
    
    if(ReadFile(TheFile, DataBuffer, 5, &BytesRead, NULL) != 0)
    {
        Trace("ERROR: ReadFile should have failed!  It was called on "
              "a locked file. But, it returned non-zero indicating success.");
        result = 1;
    }

    /* Attempt to Write 5 bytes to this file.  Since it is locked this should
       fail.
    */

    memset(DataBuffer,'X',BUF_SIZE);

    if(WriteFile(TheFile, DataBuffer, 5,&BytesWritten, NULL) != 0)
    {
        Trace("ERROR: WriteFile should have failed!  It was called on "
              "a locked file. But, it returned non-zero indicating success.");
        result = 1;
    } 
   
    /* Check to ensure that the number of Bytes read/written is still 0, 
       since nothing should have been read or written.
    */
    
    if(BytesRead != 0 || BytesWritten !=0)
    {
        Trace("ERROR: The number of bytes read is %d and written is %d.  "
              "These should both be 0, as the file was locked.",
              BytesRead,BytesWritten);
        result = 1;
    }

    PAL_TerminateEx(result);
    return result;
}


