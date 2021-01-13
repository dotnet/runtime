// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

/*============================================================================
**
** Source:  test1.c
**
** Purpose: Call fseek to move a file pointer to the start of a file,
**          a position offset from the start, a position offset from the
**          current position, and a position offset from the end of the
**          file.  Check that the file pointer is at the correct position
**          after each seek.
**
**
**==========================================================================*/

#include <palsuite.h>

const char filename[] = "testfile.txt";

static BOOL Cleanup(HANDLE hFile)
{
    BOOL result= TRUE;

    if (fclose((PAL_FILE*)hFile))
    {
        Trace("fseek: ERROR -> Unable to close file \"%s\".\n", 
            filename);
        result= FALSE;
    }
    if (!DeleteFileA(filename))
    {
        result= FALSE;
        Trace("fseek: ERROR -> Unable to delete file \"%s\". ", 
            "GetLastError returned %u.\n", 
            filename,
            GetLastError());
    }
    return result;
}

PALTEST(c_runtime_fseek_test1_paltest_fseek_test1, "c_runtime/fseek/test1/paltest_fseek_test1")
{
    char outBuf[] = "0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZ";
    char inBuf[20];
    FILE * fp;
    int size = ( sizeof(outBuf)/sizeof(char) ) - 1;

    if (PAL_Initialize(argc, argv))
    {
        return FAIL;
    }


    /*create the file*/
    fp = fopen(filename, "w");
    if (fp == NULL)
    {
        Fail("Unable to open a file for write.\n");
    }
    if(fprintf(fp, outBuf) !=  size)
    {
        Trace("Unable to write to %s.\n", filename);
        Cleanup(fp);
        Fail("");
    }

    if (fclose(fp) != 0)
    {
        Trace("Unable to close newly written file.\n");
        if (!DeleteFileA(filename))
        {
            Trace("fseek: ERROR -> Unable to delete file \"%s\". ", 
                "GetLastError returned %u.\n", 
                filename,
                GetLastError());
        }
        Fail("");
    }

    fp = fopen(filename, "r");
    if (fp == NULL)
    {
        if (!DeleteFileA(filename))
        {
            Trace("_putw: ERROR -> Unable to delete file \"%s\". ", 
                "GetLastError returned %u.\n", 
                filename,
                GetLastError());
        }
        Fail("Unable to open a file for read.\n");
    }

    /*seek to the start*/
    if (fseek(fp, 0, SEEK_SET) != 0)
    {
        Cleanup(fp);
        Fail("fseek failed when seeking the start of a file.\n");
    }
    if (fgets(inBuf, 11, fp) != inBuf)
    {
        Cleanup(fp);
        Fail("Unable to read from file after using fseek to move to the start.\n");
    }
    if (strncmp(inBuf, outBuf, 10) != 0)
    {
        Cleanup(fp);
        Fail("fseek was asked to seek the start of a file," 
             "but didn't get there.\n");
    }

    /*Seek with an offset from the start*/

    if (fseek(fp, 10, SEEK_SET) != 0)
    {
        Cleanup(fp);
        Fail("fseek failed when called with SEEK_SET and a positive offset.\n");
    }
  
    if (fgets(inBuf, 6, fp) != inBuf)
    {
        Cleanup(fp);
        Fail("fgets failed after feek was called with SEEK_SET" 
             "and a positive offset.\n");
    }


    if (strncmp(inBuf, "ABCDE", 5) != 0)
    {
        Cleanup(fp);
        Fail("fseek did not move to the correct position when passed SEEK_SET"
             " and a positive offset.\n");
    }

    /*now move backwards and read the same string*/
    if (fseek(fp, -5, SEEK_CUR) != 0)
    {
        Cleanup(fp);
        Fail("fseek failed when passed SEEK_CUR and a negative offset.\n");
    }

    if (fgets(inBuf, 6, fp) != inBuf)
    {
        Cleanup(fp);
        Fail("fgets failed after fseek was called with SEEK_CUR and a " 
             "negative offset.\n");
    }

    if (strncmp(inBuf, "ABCDE", 5) != 0)
    {
        Cleanup(fp);
        Fail("fseek did not move to the correct position when called with"
             " SEEK_CUR and a negative offset.\n");
    }

    /*Try seeking relative to the end of the file.*/
    if (fseek(fp, -10, SEEK_END) != 0)
    {
        Cleanup(fp);
        Fail("fseek failed when called with SEEK_END and a negative"
             " offset.\n");
    }
    if (fgets(inBuf, 2, fp) != inBuf)
    {
        Cleanup(fp);
        Fail("fgets failed after fseek was called with SEEK_END and a "
             "negative offset\n");
    }

    if (strncmp(inBuf, "Q", 1) != 0)
    {
        Cleanup(fp);
        Fail("fseek did not move to the correct position when called with "
             "SEEK_END and a negative offset.\n");
    }


    /*close the file*/
    if(!Cleanup(fp))
    {
        Fail("");
    } 

    PAL_Terminate();
    return PASS;
}






