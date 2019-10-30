// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

/*============================================================================
**
** Source:  test4.c
**
** Purpose: Tests swprintf_s with pointers
**
**
**==========================================================================*/



#include <palsuite.h>
#include "../_snwprintf_s.h"

/* memcmp is used to verify the results, so this test is dependent on it. */
/* ditto with wcslen */


int __cdecl main(int argc, char *argv[])
{
    void *ptr = (void*) 0x123456;
    INT64 lptr = I64(0x1234567887654321);
    
    if (PAL_Initialize(argc, argv) != 0)
    {
        return FAIL;
    }

/*
**  Run only on 64 bit platforms
*/
#if defined(BIT64)
	Trace("Testing for 64 Bit Platforms \n");
	DoPointerTest(convert("%p"), NULL, convert("0000000000000000"));
    DoPointerTest(convert("%p"), ptr, convert("0000000000123456"));
    DoPointerTest(convert("%17p"), ptr, convert(" 0000000000123456"));
    DoPointerTest(convert("%17p"), ptr, convert(" 0000000000123456"));
    DoPointerTest(convert("%-17p"), ptr, convert("0000000000123456 "));
    DoPointerTest(convert("%+p"), ptr, convert("0000000000123456"));
    DoPointerTest(convert("% p"), ptr, convert("0000000000123456"));
    DoPointerTest(convert("%#p"), ptr, convert("0X0000000000123456"));
    DoPointerTest(convert("%lp"), ptr, convert("00123456"));
    DoPointerTest(convert("%hp"), ptr, convert("00003456"));
    DoPointerTest(convert("%Lp"), ptr, convert("00123456"));
    DoI64Test(convert("%I64p"), lptr, "1234567887654321", 
        convert("1234567887654321"));
#else
	Trace("Testing for Non 64 Bit Platforms \n");
	DoPointerTest(convert("%p"), NULL, convert("00000000"));
    DoPointerTest(convert("%p"), ptr, convert("00123456"));
    DoPointerTest(convert("%9p"), ptr, convert(" 00123456"));
    DoPointerTest(convert("%09p"), ptr, convert(" 00123456"));
    DoPointerTest(convert("%-9p"), ptr, convert("00123456 "));
    DoPointerTest(convert("%+p"), ptr, convert("00123456"));
    DoPointerTest(convert("% p"), ptr, convert("00123456"));
    DoPointerTest(convert("%#p"), ptr, convert("0X00123456"));
    DoPointerTest(convert("%lp"), ptr, convert("00123456"));
    DoPointerTest(convert("%hp"), ptr, convert("00003456"));
    DoPointerTest(convert("%Lp"), ptr, convert("00123456"));
    DoI64Test(convert("%I64p"), lptr, "1234567887654321", 
        convert("1234567887654321"));
#endif

	PAL_Terminate();
    return PASS;
}

