// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

/*============================================================================
**
** Source:  test1.c
**
** Purpose: Tests to see that fflush is working properly.  Flushes a couple
** buffers and checks the return value.  Can't figure out a way to test
** and ensure it is really dropping the buffers, since the system
** does this automatically most of the time ...
**
**
**==========================================================================*/

/* This function is really tough to test.  Right now it just tests 
   a bunch of return values.  No solid way to ensure that it is really
   flushing a buffer or not -- might have to be a manual test someday.
*/

#include <palsuite.h>


PALTEST(c_runtime_fflush_test1_paltest_fflush_test1, "c_runtime/fflush/test1/paltest_fflush_test1")
{
    
    int TheReturn;
    FILE* TheFile; 
    FILE* AnotherFile = NULL;
  
    PAL_Initialize(argc,argv);
     
    TheFile = fopen("theFile","w+");

    if(TheFile == NULL) 
    {
        Fail("ERROR: fopen failed.  Test depends on this function.");
    }
    
    TheReturn = fwrite("foo",3,3,TheFile);
    
    if(TheReturn != 3) 
    {
        Fail("ERROR: fwrite failed.  Test depends on this function.");
    }
  
    /* Test to see that FlushFileBuffers returns a success value */
    TheReturn = fflush(TheFile);

    if(TheReturn != 0) 
    {
        Fail("ERROR: The fflush function returned non-zero, which "
               "indicates failure, when trying to flush a buffer.");
    }

    /* Test to see that FlushFileBuffers returns a success value */
    TheReturn = fflush(NULL);

    if(TheReturn != 0) 
    {
        Fail("ERROR: The fflush function returned non-zero, which "
               "indicates failure, when trying to flush all buffers.");
    }

    /* Test to see that FlushFileBuffers returns a success value */
    TheReturn = fflush(AnotherFile);

    if(TheReturn != 0) 
    {
        Fail("ERROR: The fflush function returned non-zero, which "
               "indicates failure, when trying to flush a stream not "
               "associated with a file.");
    }
  
    PAL_Terminate();
    return PASS;
}


