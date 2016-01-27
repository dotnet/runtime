// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

/*============================================================================
**
** Source:  test1.c
**
** Purpose: Writes a series of integers to a file, test.dat,
**          then verifies the results.
**
** Dependency: fopen(...)
**             fclose(...)
**             CloseHandle(...) 
**             DeleteFileA(...)
**             _getw(...)
**
**
**
**==========================================================================*/


#include <palsuite.h>

const char testFileName[] = "test.dat";

static void Cleanup(HANDLE hFile)
{
    if (fclose(hFile))
    {
        Trace("_putw: ERROR -> Unable to close file \"%s\".\n", 
            testFileName);
    }
    if (!DeleteFileA(testFileName))
    {
        Trace("_putw: ERROR -> Unable to delete file \"%s\". ", 
            "GetLastError returned %u.\n", 
            testFileName,
            GetLastError());
    }
}


int __cdecl main(int argc, char **argv)
{

    FILE * pfTest = NULL;
    int  testArray[] = {0,1,-1,0x7FFFFFFF,0x80000000,0xFFFFFFFF,0xFFFFAAAA};
    int  i = 0;
    int  retValue = 0;

    /*
     *  Initialize the PAL and return FAIL if this fails
     */
    if (0 != (PAL_Initialize(argc, argv)))
    {
        return FAIL;
    }

    /*write the file that we will use to test */
    pfTest = fopen(testFileName, "w");
    if (pfTest == NULL)
    {
        Fail ("Unable to write test file.\n");
    }

    for (i = 0; i < sizeof(testArray)/sizeof(int) ; i++)
    {
        _putw(testArray[i], pfTest);

        if( ferror( pfTest ) )        
    {
            Cleanup(pfTest);
            Fail( "Error:in _putw -> error has occurred in the "
                "stream while writing to the file: \"test.dat\"\n");
        }
      
    }

    if (fclose(pfTest) != 0)
    {
        Cleanup(pfTest);
        Fail ("Error closing file after writing with _putw(..).\n");
    }

    /*open the new test file and compare*/
    pfTest = fopen(testFileName, "r");
    if (pfTest == NULL)
    {
        Fail ("Error opening \"%s\", which is odd, since I just finished "
                "creating that file.\n", testFileName);
    }
    retValue =_getw( pfTest );
    i = 0;
    while(retValue != EOF)
    {
        if(retValue != testArray[i])
        {
            Cleanup(pfTest);
            Fail ("Integers written by _putw are not in the correct format\n",
                testFileName);
        }
        retValue = _getw( pfTest );
        i++ ;
    }

    Cleanup(pfTest);  
    PAL_Terminate();
    return PASS;
}


