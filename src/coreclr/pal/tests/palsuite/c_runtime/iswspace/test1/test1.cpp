// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

/*============================================================================
**
** Source:  test1.c
**
** Purpose: Tests iswspace with a range of wide characters.
**
**
**
**==========================================================================*/



#include <palsuite.h>

PALTEST(c_runtime_iswspace_test1_paltest_iswspace_test1, "c_runtime/iswspace/test1/paltest_iswspace_test1")
{
    int ret;
    int i;

    struct testChars
    {
        WCHAR charValue;
        int result;
    };

    /* create an array of chars that test the range of possible characters */
    struct testChars testChars1[] =    
    {
            {0x00,0}, /* null */
            {0x09,1}, /* open circle */
            {0x0D,1}, /* musical note */
            {0x20,1}, /* space */
            {0x3F,0}, /* ? */
            {0x5E,0}, /* ^ */
            {0x7B,0}, /* { */
            {0x86,0}, /* a with circle on top */
            {0x9F,0}, /* slanted f */
            {0xC4,0}, /* long dash */
            {0xE5,0} /* sigma */
    };

    if (0 != PAL_Initialize(argc, argv))
    {
        return FAIL;
    }

    for (i = 0; i < (sizeof(testChars1) / sizeof(struct testChars)); i++)
    {

        ret = iswspace(testChars1[i].charValue);

        if((ret==0) && (testChars1[i].result != 0))
            {
            Fail("ERROR:  wide character %#X IS considered a space, "
            "but iswspace did NOT indicate it was one with error %u.\n", 
            testChars1[i].charValue,
            GetLastError());
            }

        if((ret!=0) && (testChars1[i].result == 0))
        {
            Fail("ERROR: wide character %#X is NOT considered a space, " 
            "but iswspace DID indicate it was a space with error %u.\n", 
            testChars1[i].charValue,
            GetLastError());               
        }
    }


    PAL_Terminate();

    return PASS;
}

