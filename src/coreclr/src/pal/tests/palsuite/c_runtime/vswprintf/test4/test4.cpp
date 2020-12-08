// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

/*=====================================================================
**
** Source:    test4.c
**
** Purpose:   Test #4 for the vswprintf function.
**
**
**===================================================================*/

#include <palsuite.h>
#include "../vswprintf.h"

/* memcmp is used to verify the results, so this test is dependent on it. */
/* ditto with wcslen */
static void DoPointerTest(WCHAR *formatstr, void* param, WCHAR* paramstr,
                   WCHAR *checkstr1)
{
    WCHAR buf[256] = { 0 };

    testvswp(buf, _countof(buf), formatstr, param);
    if (memcmp(buf, checkstr1, wcslen(checkstr1) + 2) != 0)

    {
        Fail("ERROR: failed to insert pointer to %#p into \"%s\"\n"
            "Expected \"%s\" got \"%s\".\n",
                paramstr,
                convertC(formatstr), 
                convertC(checkstr1), 
                convertC(buf));
    }    
}

static void DoI64DoubleTest(WCHAR *formatstr, INT64 value, WCHAR *valuestr,
                            WCHAR *checkstr1)
{
    WCHAR buf[256] = { 0 };
 
    testvswp(buf, _countof(buf), formatstr, value);
    if (memcmp(buf, checkstr1, wcslen(checkstr1) + 2) != 0)
    {
        Fail("ERROR: failed to insert %s into \"%s\"\n"
            "Expected \"%s\", got \"%s\".\n",
                value,
                convertC(formatstr),
                convertC(checkstr1),
                convertC(buf));
    }
}

PALTEST(c_runtime_vswprintf_test4_paltest_vswprintf_test4, "c_runtime/vswprintf/test4/paltest_vswprintf_test4")
{
    void *ptr = (void*) 0x123456;
    INT64 lptr = I64(0x1234567887654321);
    
    if (PAL_Initialize(argc, argv) != 0)
        return(FAIL);
    
/*
**  Run only on 64 bit platforms
*/
#if defined(HOST_64BIT)
	Trace("Testing for 64 Bit Platforms \n");
	DoPointerTest(convert("%p"), NULL, convert("NULL"), convert("0000000000000000"));
    DoPointerTest(convert("%p"), ptr, convert("pointer to 0x123456"), 
        convert("0000000000123456"));
    DoPointerTest(convert("%17p"), ptr, convert("pointer to 0x123456"), 
        convert(" 0000000000123456"));
    DoPointerTest(convert("%17p"), ptr, convert("pointer to 0x123456"), 
        convert(" 0000000000123456"));
    DoPointerTest(convert("%-17p"), ptr, convert("pointer to 0x123456"), 
        convert("0000000000123456 "));
    DoPointerTest(convert("%+p"), ptr, convert("pointer to 0x123456"), 
        convert("0000000000123456"));
    DoPointerTest(convert("%#p"), ptr, convert("pointer to 0x123456"), 
        convert("0X0000000000123456"));
    DoPointerTest(convert("%lp"), ptr, convert("pointer to 0x123456"), 
        convert("00123456"));
    DoPointerTest(convert("%hp"), ptr, convert("pointer to 0x123456"), 
        convert("00003456"));
    DoPointerTest(convert("%Lp"), ptr, convert("pointer to 0x123456"), 
        convert("00123456"));
    DoI64DoubleTest(convert("%I64p"), lptr, 
        convert("pointer to 0x1234567887654321"), convert("1234567887654321"));

#else
	Trace("Testing for Non 64 Bit Platforms \n");
	DoPointerTest(convert("%p"), NULL, convert("NULL"), convert("00000000"));
    DoPointerTest(convert("%p"), ptr, convert("pointer to 0x123456"), 
        convert("00123456"));
    DoPointerTest(convert("%9p"), ptr, convert("pointer to 0x123456"), 
        convert(" 00123456"));
    DoPointerTest(convert("%09p"), ptr, convert("pointer to 0x123456"), 
        convert(" 00123456"));
    DoPointerTest(convert("%-9p"), ptr, convert("pointer to 0x123456"), 
        convert("00123456 "));
    DoPointerTest(convert("%+p"), ptr, convert("pointer to 0x123456"), 
        convert("00123456"));
    DoPointerTest(convert("%#p"), ptr, convert("pointer to 0x123456"), 
        convert("0X00123456"));
    DoPointerTest(convert("%lp"), ptr, convert("pointer to 0x123456"), 
        convert("00123456"));
    DoPointerTest(convert("%hp"), ptr, convert("pointer to 0x123456"), 
        convert("00003456"));
    DoPointerTest(convert("%Lp"), ptr, convert("pointer to 0x123456"), 
        convert("00123456"));
    DoI64DoubleTest(convert("%I64p"), lptr, 
        convert("pointer to 0x1234567887654321"), convert("1234567887654321"));

#endif
	
    PAL_Terminate();
    return PASS;
}

