// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

/*============================================================================
**
** Source:      test19.c (fprintf)
**
** Purpose:     Tests the variable length precision argument.
**              This test is modeled after the fprintf series.
**
**
**==========================================================================*/

#include <palsuite.h>
#include "../fprintf.h"

/* 
 * Depends on memcmp, strlen, fopen, fseek and fgets.
 */

#define DOTEST(a,b,c,d,e,f) DoTest(a,b,(void*)c,d,e,f)

void DoTest(char *formatstr, int precision, void *param, 
            char *paramstr, char *checkstr1, char *checkstr2)
{
    FILE *fp;
    char buf[256];

    if ((fp = fopen("testfile.txt", "w+")) == NULL )
    {
        Fail("ERROR: fopen failed to create testfile\n");
    }

    if ((fprintf(fp, formatstr, precision, param)) < 0)
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
        memcmp(buf, checkstr2, strlen(buf) + 1) != 0)
    {
        Fail("ERROR: failed to insert %s into \"%s\" with precision %d\n"
            "Expected \"%s\" or \"%s\", got \"%s\".\n", paramstr, formatstr,
 precision,
            checkstr1, checkstr2, buf);
    }
    
    if ((fclose( fp )) != 0)

    {
        Fail("ERROR: fclose failed to close \"testfile.txt\"\n");
    }
            
}

void DoublePrecTest(char *formatstr, int precision, 
                    double param, char *checkstr1, char *checkstr2)
{
    FILE *fp;
    char buf[256];

    if ((fp = fopen("testfile.txt", "w+")) == NULL )
    {
        Fail("ERROR: fopen failed to create testfile\n");
    }

    if ((fprintf(fp, formatstr, precision, param)) < 0)
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
        memcmp(buf, checkstr2, strlen(buf) + 1) != 0)
    {
        Fail("ERROR: failed to insert %f into \"%s\" with precision %d\n"
            "Expected \"%s\" or \"%s\", got \"%s\".\n", 
            param, formatstr, precision, checkstr1, checkstr2, buf);
    }

    if ((fclose( fp )) != 0)
    {
        Fail("ERROR: fclose failed to close \"testfile.txt\"\n");
    }
            
}

PALTEST(c_runtime_fprintf_test19_paltest_fprintf_test19, "c_runtime/fprintf/test19/paltest_fprintf_test19")
{

    if (PAL_Initialize(argc, argv) != 0)
        return(FAIL);

    DOTEST("%.*s", 2, "bar", "bar", "ba", "ba");
    DOTEST("%.*S", 2, convert("bar"), "bar", "ba", "ba");

    //DOTEST("%.*n", 4, 2, "2", "0002");
    DOTEST("%.*c", 0, 'a', "a", "a", "a");
    DOTEST("%.*c", 4, 'a', "a", "a", "a");
    DOTEST("%.*C", 0, (WCHAR)'a', "a", "a", "a");
    DOTEST("%.*C", 4, (WCHAR)'a', "a", "a", "a");
    DOTEST("%.*d", 1, 42, "42", "42", "42");
    DOTEST("%.*d", 3, 42, "42", "042", "042");
    DOTEST("%.*i", 1, 42, "42", "42", "42");
    DOTEST("%.*i", 3, 42, "42", "042", "042");
    DOTEST("%.*o", 1, 42, "42", "52", "52");
    DOTEST("%.*o", 3, 42, "42", "052", "052");
    DOTEST("%.*u", 1, 42, "42", "42", "42");
    DOTEST("%.*u", 3, 42, "42", "042", "042");
    DOTEST("%.*x", 1, 0x42, "0x42", "42", "42");
    DOTEST("%.*x", 3, 0x42, "0x42", "042", "042");
    DOTEST("%.*X", 1, 0x42, "0x42", "42", "42");
    DOTEST("%.*X", 3, 0x42, "0x42", "042", "042");
    

    DoublePrecTest("%.*e", 1, 2.01, "2.0e+000", "2.0e+00");
    DoublePrecTest("%.*e", 3, 2.01, "2.010e+000", "2.010e+00");
    DoublePrecTest("%.*E", 1, 2.01, "2.0E+000", "2.0E+00");
    DoublePrecTest("%.*E", 3, 2.01, "2.010E+000", "2.010E+00");
    DoublePrecTest("%.*f", 1, 2.01, "2.0", "2.0");
    DoublePrecTest("%.*f", 3, 2.01, "2.010", "2.010");
    DoublePrecTest("%.*g", 1, 256.01, "3e+002", "3e+02");
    DoublePrecTest("%.*g", 3, 256.01, "256", "256");
    DoublePrecTest("%.*g", 4, 256.01, "256", "256");
    DoublePrecTest("%.*g", 6, 256.01, "256.01", "256.01");
    DoublePrecTest("%.*G", 1, 256.01, "3E+002", "3E+02");
    DoublePrecTest("%.*G", 3, 256.01, "256", "256");
    DoublePrecTest("%.*G", 4, 256.01, "256", "256");
    DoublePrecTest("%.*G", 6, 256.01, "256.01", "256.01");

    PAL_Terminate();
    return PASS;
}
