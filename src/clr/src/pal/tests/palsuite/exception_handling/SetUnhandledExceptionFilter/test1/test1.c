// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

/*=============================================================
**
** Source: test1.c
**
** Purpose: Sets up a new unhandled exception filter, and makes sure it 
**          returns the previous filter.  Raises an unhandled exception and 
**          makes sure this reaches the filter.
**
**
**============================================================*/


#include <palsuite.h>

/* This isn't defined in the pal, so copied from win32 */
#define EXCEPTION_EXECUTE_HANDLER       1
#define EXCEPTION_CONTINUE_EXECUTION    -1


int InFilter = FALSE;

LONG PALAPI FirstFilter(LPEXCEPTION_POINTERS p)
{
    return EXCEPTION_EXECUTE_HANDLER;
}

LONG PALAPI ContinueFilter(LPEXCEPTION_POINTERS p)
{
    InFilter = TRUE;

    Trace("This test has succeeded as far at the automated checks can "
          "tell.  Manual verification is now required to be completely sure.\n");
    Trace("Now the PAL's handling of application errors will be tested "
          "with an exception code of %u.\n",
          p->ExceptionRecord->ExceptionCode);
    Trace("Please verify that the actions that the PAL now takes are "
          "as specified for it.\n");

    return EXCEPTION_CONTINUE_SEARCH;
}

int __cdecl main(int argc, char *argv[])
{
    LPTOP_LEVEL_EXCEPTION_FILTER OldFilter;    

    if(0 != (PAL_Initialize(argc, argv)))
    {
        return FAIL;
    }

    
    SetUnhandledExceptionFilter(FirstFilter);

    OldFilter = SetUnhandledExceptionFilter(ContinueFilter);
    if (OldFilter != FirstFilter)
    {
        Fail("SetUnhandledExceptionFilter did not return a pointer to the "
            "previous filter!\n");
    }

    /* 
     * Raise an unhandled exception.  This should cause our filter to be 
     * excecuted and the program to exit with a code the same as the
     * exception code.
     */
    RaiseException(3,0,0,0);
    

    /* 
     * This code should not be executed because the toplevelhandler is 
     * expected to "just" set the exit code and abend the program
     */
    Fail("An unhandled exception did not cause the program to abend with"
         "the exit code == the ExceptionCode!\n");

    PAL_Terminate();
    return FAIL;
}
