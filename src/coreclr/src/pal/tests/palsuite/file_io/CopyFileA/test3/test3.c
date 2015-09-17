//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information. 
//

/*=====================================================================
**
** Source:  test3.c
**
** Purpose: Tests the PAL implementation of the CopyFileA function
**          to see if a file can be copied to itself
**
**
**===================================================================*/

#include <palsuite.h>

int __cdecl main(int argc, char *argv[])
{

    BOOL bRc = TRUE;
    char* szSrcExisting = "src_existing.tmp";
    char* szDest = "src_dest.tmp";
    FILE* tempFile = NULL;
    int retCode;
    
    if (0 != PAL_Initialize(argc,argv))
    {
        return FAIL;
    }

     /* create the src_existing file */
    tempFile = fopen(szSrcExisting, "w");
    if (tempFile != NULL)
    {
        retCode = fputs("CopyFileA test file: src_existing.tmp ", tempFile);
        if(retCode < 0)
        {
            retCode = fclose(tempFile);
            if(retCode != 0)
            {
                Trace("CopyFileA: ERROR-> Couldn't close file: %s with error "
                      "%u.\n", szSrcExisting, GetLastError());
            }
            
            Fail("CopyFileA: ERROR-> Couldn't write to %s with error "
                "%u.\n", szSrcExisting, GetLastError());
        }
        retCode = fclose(tempFile);
        if(retCode != 0)
        {
            Fail("CopyFileA: ERROR-> Couldn't close file: %s with error "
                "%u.\n", szSrcExisting, GetLastError());
        }

    }
    else
    {
        Fail("CopyFileA: ERROR-> Couldn't create %s with "
            "error %ld\n",szSrcExisting,GetLastError());
    }

    /* set the file attributes of the source file to readonly */
    bRc = SetFileAttributes(szSrcExisting, FILE_ATTRIBUTE_READONLY);
    if(!bRc)
    {
        Fail("CopyFileA: ERROR-> Couldn't set file attributes for "
            "file %s with error %u\n", szSrcExisting, GetLastError());
    }

    // Check the file attributes to make sure SetFileAttributes() above actually succeeded
    DWORD fileAttributes = GetFileAttributesA(szSrcExisting);
    if (fileAttributes == INVALID_FILE_ATTRIBUTES)
    {
        Fail("CopyFileA: Failed to get file attributes for source file, %u\n", GetLastError());
    }
    if ((fileAttributes & FILE_ATTRIBUTE_READONLY) == 0)
    {
        Fail("CopyFileA: SetFileAttributes(read-only) on source file returned success but did not make it read-only.\n");
    }

    /* copy the file */
    bRc = CopyFileA(szSrcExisting,szDest,TRUE);
    if(!bRc)
    {
        Fail("CopyFileA: Cannot copy a file with error, %u",GetLastError());
    }
    
  
    /* try to get file attributes of destination file */
    fileAttributes = GetFileAttributesA(szDest);
    if (fileAttributes == INVALID_FILE_ATTRIBUTES)
    {
        Fail("CopyFileA: GetFileAttributes of destination file "
            "failed with error code %ld. \n",
            GetLastError());  
    }

    /* verify attributes of destination file to source file*/                    
    if((fileAttributes & FILE_ATTRIBUTE_READONLY) != FILE_ATTRIBUTE_READONLY)
    {
        Fail("CopyFileA : The file attributes of the "
            "destination file do not match the file "
            "attributes of the source file.\n");
    }
    
    /* set the attributes of the destination file to normal again */
    bRc = SetFileAttributesA(szDest, FILE_ATTRIBUTE_NORMAL);
    if(!bRc)
    {
        Fail("CopyFileA: ERROR-> Couldn't set file attributes for "
            "file %s with error %u\n", szDest, GetLastError());
    }

    /* delete the newly copied file */
    bRc = DeleteFile(szDest);
    if(!bRc)
    {
        Fail("CopyFileA: DeleteFile failed to delete the"
            "file correctly with error,%u.\n",GetLastError());
    }

    /* set the attributes of the source file to normal again */
    bRc = SetFileAttributesA(szSrcExisting, FILE_ATTRIBUTE_NORMAL);
    if(!bRc)
    {
        Fail("CopyFileA: ERROR-> Couldn't set file attributes for "
            "file %s with error %u\n", szSrcExisting, GetLastError());
    }    
    
    /* delete the original file */
    bRc = DeleteFile(szSrcExisting);
    if(!bRc)
    {
        Fail("CopyFileA: DeleteFile failed to delete the"
            "file correctly with error,%u.\n",GetLastError());
    }
 
    PAL_Terminate();
    return PASS;
    
}
