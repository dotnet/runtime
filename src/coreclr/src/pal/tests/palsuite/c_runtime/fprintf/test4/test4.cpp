// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

/*============================================================================
**
** Source:      test4.c (fprintf)
**
** Purpose:     Tests the pointer specifier (%p).
**              This test is modeled after the fprintf series.
**
**
**==========================================================================*/

#include <palsuite.h>
#include "../fprintf.h"

/* 
 * Depends on memcmp, strlen, fopen, fseek and fgets.
 */

static void DoTest(char *formatstr, void* param, char* paramstr, 
                   char *checkstr1, char *checkstr2)
{
    FILE *fp;
    char buf[256] = { 0 };

    if ((fp = fopen("testfile.txt", "w+")) == NULL )
    {
        Fail("ERROR: fopen failed to create testfile\n");
    }

    if ((fprintf(fp, formatstr, param)) < 0)
    {
        Fail("ERROR: fprintf failed\n");
    }

    if ((fseek(fp, 0, SEEK_SET)) != 0)
    {
        Fail("ERROR: fseek failed\n");
    }

    if ((fgets(buf, 100, fp)) == NULL)
    {
        Fail("ERROR: fseek failed\n");
    }
   
    if (memcmp(buf, checkstr1, strlen(buf) + 1) != 0 &&
        memcmp(buf, checkstr2, strlen(buf) + 1) != 0 )
    {
        Fail("ERROR: failed to insert %s into \"%s\"\n"
            "Expected \"%s\" or \"%s\" got \"%s\".\n", 
            paramstr, formatstr, checkstr1, checkstr2, buf);
    }    
    
    if ((fclose( fp )) != 0)

    {

        Fail("ERROR: fclose failed to close \"testfile.txt\"\n");

    }
}


PALTEST(c_runtime_fprintf_test4_paltest_fprintf_test4, "c_runtime/fprintf/test4/paltest_fprintf_test4")
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
	DoTest("%p", NULL, "NULL", "0000000000000000", "0x0");
    DoTest("%p", ptr, "pointer to 0x123456", "0000000000123456", "0x123456");
    DoTest("%17p", ptr, "pointer to 0x123456", " 0000000000123456", " 0x123456");
    DoTest("%17p", ptr, "pointer to 0x123456", " 0000000000123456", "0x0123456");
    DoTest("%-17p", ptr, "pointer to 0x123456", "0000000000123456 ", "0x123456 ");
    DoTest("%+p", ptr, "pointer to 0x123456", "0000000000123456", "0x123456");
    DoTest("%#p", ptr, "pointer to 0x123456", "0X0000000000123456", "0x123456");
    DoTest("%lp", ptr, "pointer to 0x123456", "00123456", "0x123456");
    DoTest("%hp", ptr, "pointer to 0x123456", "00003456", "0x3456");
    DoTest("%Lp", ptr, "pointer to 0x123456", "00123456", "0x123456");
    DoI64Test("%I64p", lptr, "pointer to 0x1234567887654321", 
        "1234567887654321", "0x1234567887654321");
#else
	Trace("Testing for Non 64 Bit Platforms \n");
	DoTest("%p", NULL, "NULL", "00000000", "0x0");
    DoTest("%p", ptr, "pointer to 0x123456", "00123456", "0x123456");
    DoTest("%9p", ptr, "pointer to 0x123456", " 00123456", " 0x123456");
    DoTest("%09p", ptr, "pointer to 0x123456", " 00123456", "0x0123456");
    DoTest("%-9p", ptr, "pointer to 0x123456", "00123456 ", "0x123456 ");
    DoTest("%+p", ptr, "pointer to 0x123456", "00123456", "0x123456");
    DoTest("%#p", ptr, "pointer to 0x123456", "0X00123456", "0x123456");
    DoTest("%lp", ptr, "pointer to 0x123456", "00123456", "0x123456");
    DoTest("%hp", ptr, "pointer to 0x123456", "00003456", "0x3456");
    DoTest("%Lp", ptr, "pointer to 0x123456", "00123456", "0x123456");
    DoI64Test("%I64p", lptr, "pointer to 0x1234567887654321", 
        "1234567887654321", "0x1234567887654321");
#endif
	
    PAL_Terminate();
    return PASS;
}
