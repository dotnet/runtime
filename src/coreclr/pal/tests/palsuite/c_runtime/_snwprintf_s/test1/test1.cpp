// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

/*============================================================================
**
** Source:  test1.c
**
** Purpose: General test to see if swprintf_s works correctly
**
**
**==========================================================================*/



#include <palsuite.h>
#include "../_snwprintf_s.h"

/* memcmp is used to verify the results, so this test is dependent on it. */
/* ditto with wcslen */


PALTEST(c_runtime__snwprintf_s_test1_paltest_snwprintf_test1, "c_runtime/_snwprintf_s/test1/paltest_snwprintf_test1")
{
    WCHAR *checkstr;
    WCHAR buf[256] = { 0 };
    int ret;

    if (PAL_Initialize(argc, argv) != 0)
    {
        return FAIL;
    }

    checkstr = convert("hello world");
    _snwprintf_s(buf, 256, _TRUNCATE, checkstr);
    if (memcmp(checkstr, buf, wcslen(checkstr)*2+2) != 0)
    {
        Fail("ERROR: Expected \"%s\", got \"%s\"\n", 
            convertC(checkstr), convertC(buf));
    }

    _snwprintf_s(buf, 256, _TRUNCATE, convert("xxxxxxxxxxxxxxxxx"));
    ret = _snwprintf_s(buf, 8, _TRUNCATE, checkstr);
    if ((memcmp(checkstr, buf, 14) != 0) || (buf[7] != 0))
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
