// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

/*============================================================================
**
** Source:      test4.c
**
** Purpose:     Tests the pointer specifier (%p).
**              This test is modeled after the sprintf series.
**
**
**==========================================================================*/

#include <palsuite.h>
#include "../fwprintf.h"

/* 
 * Depends on memcmp, strlen, fopen, fseek and fgets.
 */

int __cdecl main(int argc, char *argv[])
{
    void *ptr = (void*) 0x123456;
    INT64 lptr = I64(0x1234567887654321);

    if (PAL_Initialize(argc, argv) != 0)
    {
        return(FAIL);
    }

/*
**  Run only on 64 bit platforms
*/
#if defined(BIT64)
	Trace("Testing for 64 Bit Platforms \n");
	DoPointerTest(convert("%p"), NULL, "NULL", "0000000000000000", "0x0");
    DoPointerTest(convert("%p"), ptr, "pointer to 0x123456", "0000000000123456", 
        "0x123456");
    DoPointerTest(convert("%17p"), ptr, "pointer to 0x123456", " 0000000000123456", 
        " 0x123456");
    DoPointerTest(convert("%17p"), ptr, "pointer to 0x123456", " 0000000000123456", 
        "0x0123456");
    DoPointerTest(convert("%-17p"), ptr, "pointer to 0x123456", "0000000000123456 ", 
        "0x123456 ");
    DoPointerTest(convert("%+p"), ptr, "pointer to 0x123456", "0000000000123456", 
        "0x123456");
    DoPointerTest(convert("%#p"), ptr, "pointer to 0x123456", "0X0000000000123456", 
        "0x123456");
    DoPointerTest(convert("%lp"), ptr, "pointer to 0x123456", "00123456", 
        "0x123456");
    DoPointerTest(convert("%hp"), ptr, "pointer to 0x123456", "00003456", 
        "0x3456");
    DoPointerTest(convert("%Lp"), ptr, "pointer to 0x123456", "00123456", 
        "0x123456");
    DoI64Test(convert("%I64p"), lptr, "pointer to 0x1234567887654321", 
        "1234567887654321", "0x1234567887654321");
#else
	Trace("Testing for Non 64 Bit Platforms \n");
	DoPointerTest(convert("%p"), NULL, "NULL", "00000000", "0x0");
    DoPointerTest(convert("%p"), ptr, "pointer to 0x123456", "00123456", 
        "0x123456");
    DoPointerTest(convert("%9p"), ptr, "pointer to 0x123456", " 00123456", 
        " 0x123456");
    DoPointerTest(convert("%09p"), ptr, "pointer to 0x123456", " 00123456", 
        "0x0123456");
    DoPointerTest(convert("%-9p"), ptr, "pointer to 0x123456", "00123456 ", 
        "0x123456 ");
    DoPointerTest(convert("%+p"), ptr, "pointer to 0x123456", "00123456", 
        "0x123456");
    DoPointerTest(convert("%#p"), ptr, "pointer to 0x123456", "0X00123456", 
        "0x123456");
    DoPointerTest(convert("%lp"), ptr, "pointer to 0x123456", "00123456", 
        "0x123456");
    DoPointerTest(convert("%hp"), ptr, "pointer to 0x123456", "00003456", 
        "0x3456");
    DoPointerTest(convert("%Lp"), ptr, "pointer to 0x123456", "00123456", 
        "0x123456");
    DoI64Test(convert("%I64p"), lptr, "pointer to 0x1234567887654321", 
        "1234567887654321", "0x1234567887654321");
#endif

	PAL_Terminate();
    return PASS;
}
