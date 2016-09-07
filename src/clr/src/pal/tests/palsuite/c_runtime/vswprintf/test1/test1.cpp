// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

/*=====================================================================
**
** Source:    test1.c
**
** Purpose:   Test #1 for the vswprintf function.
**
**
**===================================================================*/

#include <palsuite.h>
#include "../vswprintf.h"

/* memcmp is used to verify the results, so this test is dependent on it. */
/* ditto with wcslen */


int __cdecl main(int argc, char *argv[])
{
    WCHAR *checkstr = NULL;
    WCHAR buf[256] = { 0 };

    if (PAL_Initialize(argc, argv) != 0)
        return(FAIL);

	checkstr = convert("hello world");
    testvswp(buf, checkstr);

    if (memcmp(checkstr, buf, wcslen(checkstr)*2+2) != 0)
    {
        Fail("ERROR: Expected \"%s\", got \"%s\"\n", 
            convertC(checkstr), convertC(buf));
    }

	free(checkstr);
    PAL_Terminate();
    return PASS;
}
