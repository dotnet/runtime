// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

/*============================================================================
**
** Source:      test1.c (fprintf)
**
** Purpose:     A single, basic, test case with no formatting.
**              Test modeled after the sprintf series.        
**  

**
**==========================================================================*/

#include <palsuite.h>

/* 
 * Depends on memcmp, strlen, fopen, fgets, fseek and fclose.
 */

PALTEST(c_runtime_fprintf_test1_paltest_fprintf_test1, "c_runtime/fprintf/test1/paltest_fprintf_test1")
{
    FILE *fp;
    char testfile[] = "testfile.txt";
    char checkstr[] = "hello world";
    char buf[256];

    if (PAL_Initialize(argc, argv) != 0)
    {
        return(FAIL);
    }

    if ((fp = fopen(testfile, "w+")) == NULL)
    {
        Fail("ERROR: fopen failed to create \"%s\"\n", testfile);
    }

    if ((fprintf(fp, "hello world")) < 0)
    {
        Fail("ERROR: fprintf failed to print to \"%s\"\n", testfile);
    }

    if ((fseek( fp, 0, SEEK_SET)) != 0)

    {

         Fail("ERROR: Fseek failed to set pointer to beginning of file\n" );

    }

 

    if ((fgets( buf, 100, fp )) == NULL)

    {

        Fail("ERROR: fgets failed\n");

    }

    
    if (memcmp(checkstr, buf, strlen(checkstr)+1) != 0)
    {
        Fail("ERROR: expected %s, got %s\n", checkstr, buf);
    }



    if ((fclose( fp )) != 0)

    {

        Fail("ERROR: fclose failed to close \"%s\"\n", testfile);

    }

    PAL_Terminate();
    return PASS;
}
