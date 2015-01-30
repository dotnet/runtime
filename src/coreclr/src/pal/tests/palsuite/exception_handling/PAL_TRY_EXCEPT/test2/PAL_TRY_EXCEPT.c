//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information. 
//

/*=====================================================================
**
** Source:  PAL_TRY_EXCEPT.c (test 2)
**
** Purpose: Tests the PAL implementation of the PAL_TRY and 
**          PAL_EXCEPT functions. Tests that the EXCEPTION block
**          is missed if no exceptions happen
**
**
**===================================================================*/



#include <palsuite.h>


int __cdecl main(int argc, char *argv[])
{
    BOOL bTry = FALSE;
    BOOL bExcept = FALSE;

    if (0 != PAL_Initialize(argc, argv))
    {
        return FAIL;
    }


    /*
    ** Test to make sure we skip the exception block.
    */

    PAL_TRY 
    {
        bTry = TRUE;    /* indicate we hit the PAL_TRY block */
    }
    PAL_EXCEPT(EXCEPTION_EXECUTE_HANDLER)
    {
        bExcept = TRUE; /* indicate we hit the PAL_EXCEPT block */
    }
    PAL_ENDTRY;

    if (!bTry)
    {
        Trace("PAL_TRY_EXCEPT: ERROR -> It appears the code in the PAL_TRY"
            " block was not executed.\n");
    }

    if (bExcept)
    {
        Trace("PAL_TRY_EXCEPT: ERROR -> It appears the code in the PAL_EXCEPT"
            " block was executed even though no exception was supposed to"
            " happen.\n");
    }

    /* did we hit the correct code blocks? */
    if(!bTry || bExcept)
    {
        Fail("");
    }

    PAL_Terminate();  
    return PASS;

}
