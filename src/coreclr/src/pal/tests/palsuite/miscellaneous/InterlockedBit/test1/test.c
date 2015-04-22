//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

/*============================================================
**
** Source : test.c
**
** Purpose: Test for InterlockedBitTestAndReset() function
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
    { 0x00000000,  3, 0x00000000, 0 },
    { 0x12341234,  2, 0x12341230, 1 },
    { 0x12341234,  3, 0x12341234, 0 },
    { 0x12341234, 31, 0x12341234, 0 },
    { 0x12341234, 28, 0x02341234, 1 },
    { 0xffffffff, 28, 0xefffffff, 1 }
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

        UCHAR ret = InterlockedBitTestAndReset(
            &baseVal, /* Variable to manipulate */
            bitPosition);

        if (ret != test_data[i].expectedReturnValue)
        {
            Fail("ERROR: InterlockedBitTestAndReset(%d): Expected return value is %d,"
                 "Actual return value is %d.",
                 i,
                 test_data[i].expectedReturnValue,
                 ret);
        }

        if (baseVal != test_data[i].expectedValue)
        {
            Fail("ERROR: InterlockedBitTestAndReset(%d): Expected value is %x,"
                 "Actual value is %x.",
                 i,
                 test_data[i].expectedValue,
                 baseVal);
        }

    }

    PAL_Terminate();
    return PASS;
}

