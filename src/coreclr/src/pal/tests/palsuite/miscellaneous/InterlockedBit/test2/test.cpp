// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

/*============================================================
**
** Source : test.c
**
** Purpose: Test for InterlockedBitTestAndSet() function
**
**
**=========================================================*/

#include <palsuite.h>

typedef struct tag_TEST_DATA
{
    LONG baseValue;
    UINT bitPosition;
    LONG expectedValue;
    UCHAR expectedReturnValue;
} TEST_DATA;

TEST_DATA test_data[] =
{
    { (LONG)0x00000000,  2, (LONG)0x00000004, 0 },
    { (LONG)0x12341234,  2, (LONG)0x12341234, 1 },
    { (LONG)0x12341234,  3, (LONG)0x1234123c, 0 },
    { (LONG)0x12341234, 31, (LONG)0x92341234, 0 },
    { (LONG)0x12341234, 28, (LONG)0x12341234, 1 },
    { (LONG)0xffffffff, 28, (LONG)0xffffffff, 1 }
};

int __cdecl main(int argc, char *argv[]) {

    /*
     * Initialize the PAL and return FAILURE if this fails
     */

    if(0 != (PAL_Initialize(argc, argv)))
    {
        return FAIL;
    }

    for (int i = 0; i < sizeof (test_data) / sizeof (TEST_DATA); i++)
    {
        LONG baseVal = test_data[i].baseValue;
        LONG bitPosition = test_data[i].bitPosition;

        UCHAR ret = InterlockedBitTestAndSet(
            &baseVal, /* Variable to manipulate */
            bitPosition);

        if (ret != test_data[i].expectedReturnValue)
        {
            Fail("ERROR: InterlockedBitTestAndSet(%d): Expected return value is %d,"
                 "Actual return value is %d.",
                 i,
                 test_data[i].expectedReturnValue,
                 ret);
        }

        if (baseVal != test_data[i].expectedValue)
        {
            Fail("ERROR: InterlockedBitTestAndSet(%d): Expected value is %x,"
                 "Actual value is %x.",
                 i,
                 test_data[i].expectedValue,
                 baseVal);
        }

    }

    PAL_Terminate();
    return PASS;
}

