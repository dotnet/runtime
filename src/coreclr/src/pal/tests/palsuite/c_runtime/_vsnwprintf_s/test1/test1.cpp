// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

/*=====================================================================
**
** Source:    test1.c
**
** Purpose:   Test #1 for the _vsnwprintf_s function.
**
**
**===================================================================*/

#include <palsuite.h>
#include "../_vsnwprintf_s.h"

/* memcmp is used to verify the results, so this test is dependent on it. */
/* ditto with wcslen */


int __cdecl main(int argc, char *argv[])
{
    WCHAR *checkstr;
    WCHAR buf[256] = { 0 };
    int ret;

    if (PAL_Initialize(argc, argv) != 0)
    {
        return(FAIL);
    }

    checkstr = convert("hello world");
    TestVsnwprintf_s(buf, 256, checkstr);
    if (memcmp(checkstr, buf, wcslen(checkstr)*2+2) != 0)
    {
        Fail("ERROR: Expected \"%s\", got \"%s\"\n",
            convertC(checkstr), convertC(buf));
    }

    TestVsnwprintf_s(buf, 256, convert("xxxxxxxxxxxxxxxxx"));
    ret = TestVsnwprintf_s(buf, 8, checkstr);
    if ((memcmp(checkstr, buf, 14)) != 0 || (buf[7] != 0))
    {
        Fail("ERROR: Expected \"%8s\", got \"%8s\"\n",
            convertC(checkstr), convertC(buf));
    }
    if (ret >= 0)
    {
        Fail("ERROR: Expected negative return value, got %d.\n", ret);
    }
    if (buf[8] != (WCHAR) 'x')
    {
        Fail("ERROR: buffer overflow using \"%s\" with length 8.\n", 
            convertC(checkstr));
    }

    
    PAL_Terminate();
    return PASS;
}
