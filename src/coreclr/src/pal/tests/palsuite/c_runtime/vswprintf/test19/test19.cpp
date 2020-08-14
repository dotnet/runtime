// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

/*=====================================================================
**
** Source:    test18.c
**
** Purpose:   Test #18 for the vswprintf function.
**
**
**===================================================================*/
 
#include <palsuite.h>
#include "../vswprintf.h"

/* memcmp is used to verify the results, so this test is dependent on it. */
/* ditto with wcslen */

#define DOTEST(a,b,c,d,e) DoTest(a,b,(void*)c,d,e)

void DoArgumentPrecTest(WCHAR *formatstr, int precision, void *param,
                        WCHAR *paramstr, WCHAR *checkstr1, WCHAR *checkstr2)
{
    WCHAR buf[256];
    
    testvswp(buf, _countof(buf), formatstr, precision, param);
    if (memcmp(buf, checkstr1, wcslen(checkstr1) + 2) != 0 &&
        memcmp(buf, checkstr2, wcslen(checkstr2) + 2) != 0)
    {
        Fail("ERROR: failed to insert %s into \"%s\" with precision %d\n"
            "Expected \"%s\" or \"%s\", got \"%s\".\n", 
            paramstr,
            convertC(formatstr),
            precision,
            convertC(checkstr1),
            convertC(checkstr2),
            convertC(buf));
    }            
}
void DoArgumentPrecDoubleTest(WCHAR *formatstr, int precision, double param,
    WCHAR *checkstr1, WCHAR *checkstr2)
{
    WCHAR buf[256];

    testvswp(buf, _countof(buf), formatstr, precision, param);
    if (memcmp(buf, checkstr1, wcslen(checkstr1) + 2) != 0 &&
        memcmp(buf, checkstr2, wcslen(checkstr2) + 2) != 0)
    {
        Fail("ERROR: failed to insert %f into \"%s\" with precision %d\n"
            "Expected \"%s\" or \"%s\", got \"%s\".\n", 
            param, convertC(formatstr),
            precision,
            convertC(checkstr1),
            convertC(checkstr2),
            convertC(buf));
    }            
}

/*
 * Uses memcmp & wcslen
 */

int __cdecl main(int argc, char *argv[])
{

    if (PAL_Initialize(argc, argv) != 0)
        return(FAIL);

    DoArgumentPrecTest(convert("%.*s"), 2, (void*)convert("bar"), convert("bar"), 
        convert("ba"), convert("ba"));
    DoArgumentPrecTest(convert("%.*c"), 0, (void*)'a', convert("a"), 
        convert("a"), convert("a"));
    DoArgumentPrecTest(convert("%.*c"), 4, (void*)'a', convert("a"), 
        convert("a"), convert("a"));
    DoArgumentPrecTest(convert("%.*C"), 0, (void*)'a', convert("a"), 
        convert("a"), convert("a"));
    DoArgumentPrecTest(convert("%.*C"), 4, (void*)'a', convert("a"), 
        convert("a"), convert("a"));
    DoArgumentPrecTest(convert("%.*d"), 1, (void*)42, convert("42"),
        convert("42"), convert("42"));
    DoArgumentPrecTest(convert("%.*d"), 3, (void*)42, convert("42"),
        convert("042"), convert("042"));
    DoArgumentPrecTest(convert("%.*i"), 1, (void*)42, convert("42"),
        convert("42"), convert("42"));
    DoArgumentPrecTest(convert("%.*i"), 3, (void*)42, convert("42"),
        convert("042"), convert("042"));
    DoArgumentPrecTest(convert("%.*o"), 1, (void*)42, convert("42"),
        convert("52"), convert("52"));
    DoArgumentPrecTest(convert("%.*o"), 3, (void*)42, convert("42"),
        convert("052"), convert("052"));
    DoArgumentPrecTest(convert("%.*u"), 1, (void*)42, convert("42"),
        convert("42"), convert("42"));
    DoArgumentPrecTest(convert("%.*u"), 3, (void*)42, convert("42"),
        convert("042"), convert("042"));
    DoArgumentPrecTest(convert("%.*x"), 1, (void*)0x42, convert("0x42"),
        convert("42"), convert("42"));
    DoArgumentPrecTest(convert("%.*x"), 3, (void*)0x42, convert("0x42"),
        convert("042"), convert("042"));
    DoArgumentPrecTest(convert("%.*X"), 1, (void*)0x42, convert("0x42"), 
        convert("42"), convert("42"));
    DoArgumentPrecTest(convert("%.*X"), 3, (void*)0x42, convert("0x42"), 
        convert("042"), convert("042"));


    DoArgumentPrecDoubleTest(convert("%.*e"), 1, 2.01, convert("2.0e+000"),
        convert("2.0e+00"));
    DoArgumentPrecDoubleTest(convert("%.*e"), 3, 2.01, convert("2.010e+000"),
        convert("2.010e+00"));
    DoArgumentPrecDoubleTest(convert("%.*E"), 1, 2.01, convert("2.0E+000"),
        convert("2.0E+00"));
    DoArgumentPrecDoubleTest(convert("%.*E"), 3, 2.01, convert("2.010E+000"),
        convert("2.010E+00"));
    DoArgumentPrecDoubleTest(convert("%.*f"), 1, 2.01, convert("2.0"),
        convert("2.0"));
    DoArgumentPrecDoubleTest(convert("%.*f"), 3, 2.01, convert("2.010"),
        convert("2.010"));
    DoArgumentPrecDoubleTest(convert("%.*g"), 1, 256.01, convert("3e+002"),
        convert("3e+02"));
    DoArgumentPrecDoubleTest(convert("%.*g"), 3, 256.01, convert("256"),
        convert("256"));
    DoArgumentPrecDoubleTest(convert("%.*g"), 4, 256.01, convert("256"), 
        convert("256"));
    DoArgumentPrecDoubleTest(convert("%.*g"), 6, 256.01, convert("256.01"),
        convert("256.01"));
    DoArgumentPrecDoubleTest(convert("%.*G"), 1, 256.01, convert("3E+002"),
        convert("3E+02"));
    DoArgumentPrecDoubleTest(convert("%.*G"), 3, 256.01, convert("256"),
        convert("256"));
    DoArgumentPrecDoubleTest(convert("%.*G"), 4, 256.01, convert("256"), 
        convert("256"));
    DoArgumentPrecDoubleTest(convert("%.*G"), 6, 256.01, convert("256.01"),
        convert("256.01"));

    PAL_Terminate();
    return PASS;
}
