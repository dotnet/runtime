//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information. 
//

/*=====================================================================
**
** Source:    test4.c
**
** Purpose:   Test #4 for the _vsnprintf function.
**
**
**===================================================================*/

#include <palsuite.h>
#include "../_vsnprintf.h"

/*
 * Notes: memcmp is used, as is strlen.
 */

static void DoPointerTest(char *formatstr, void* param, char* paramstr,
                   char *checkstr1)
{
    char buf[256] = { 0 };

    Testvsnprintf(buf,256, formatstr, param);
    if (memcmp(buf, checkstr1, strlen(checkstr1) + 1) != 0)
    {
        Fail("ERROR: failed to insert %s into \"%s\"\n"
            "Expected \"%s\" got \"%s\".\n",
            paramstr, formatstr, checkstr1, buf);
    }
}

static void DoI64DoubleTest(char *formatstr, INT64 value, char *valuestr, char 
*checkstr1)
{
    char buf[256] = { 0 };

    Testvsnprintf(buf,256,formatstr, value);
    if (memcmp(buf, checkstr1, strlen(checkstr1) + 1) != 0)
    {
        Fail("ERROR: failed to insert %s into \"%s\"\n"
            "Expected \"%s\", got \"%s\".\n",
            valuestr, formatstr, checkstr1, buf);
    }
}

int __cdecl main(int argc, char *argv[])
{
    void *ptr = (void*) 0x123456;
    INT64 lptr = I64(0x1234567887654321);

    if (PAL_Initialize(argc, argv) != 0)
        return(FAIL);


	/*
**  Run only on 64 bit platforms
*/
#if defined(BIT64) && defined(PLATFORM_UNIX)
	Trace("Testing for 64 Bit Platforms \n");
	DoPointerTest("%p", NULL, "NULL", "0000000000000000");
    DoPointerTest("%p", ptr, "pointer to 0x123456", "0000000000123456");
    DoPointerTest("%17p", ptr, "pointer to 0x123456", " 0000000000123456");
    DoPointerTest("%17p", ptr, "pointer to 0x123456", " 0000000000123456");
    DoPointerTest("%-17p", ptr, "pointer to 0x123456", "0000000000123456 ");
    DoPointerTest("%+p", ptr, "pointer to 0x123456", "0000000000123456");
    DoPointerTest("%#p", ptr, "pointer to 0x123456", "0X0000000000123456");
    DoPointerTest("%lp", ptr, "pointer to 0x123456", "00123456");
    DoPointerTest("%hp", ptr, "pointer to 0x123456", "00003456");
    DoPointerTest("%Lp", ptr, "pointer to 0x123456", "00123456");
    DoI64DoubleTest("%I64p", lptr, "pointer to 0x1234567887654321",
        "1234567887654321");

#else
	Trace("Testing for Non 64 Bit Platforms \n");
	DoPointerTest("%p", NULL, "NULL", "00000000");
    DoPointerTest("%p", ptr, "pointer to 0x123456", "00123456");
    DoPointerTest("%9p", ptr, "pointer to 0x123456", " 00123456");
    DoPointerTest("%09p", ptr, "pointer to 0x123456", " 00123456");
    DoPointerTest("%-9p", ptr, "pointer to 0x123456", "00123456 ");
    DoPointerTest("%+p", ptr, "pointer to 0x123456", "00123456");
    DoPointerTest("%#p", ptr, "pointer to 0x123456", "0X00123456");
    DoPointerTest("%lp", ptr, "pointer to 0x123456", "00123456");
    DoPointerTest("%hp", ptr, "pointer to 0x123456", "00003456");
    DoPointerTest("%Lp", ptr, "pointer to 0x123456", "00123456");
    DoI64DoubleTest("%I64p", lptr, "pointer to 0x1234567887654321",
        "1234567887654321");
#endif


    

    PAL_Terminate();
    return PASS;
}

