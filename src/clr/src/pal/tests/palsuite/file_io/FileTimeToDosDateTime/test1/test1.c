// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

/*=====================================================================
**
** Source:  test1.c
**
** Purpose: Tests that FileTimeToDosDateTime successfully converts values.
**          Makes sure values are rounded up, and the limits of the function
**          pass.  Also tests that values outside the valid range fail.
**
**
**===================================================================*/

#include <palsuite.h>

typedef struct
{
    DWORD FileTimeLow;
    DWORD FileTimeHigh;
    WORD FatDate;
    WORD FatTime;
} testCase;

int __cdecl main(int argc, char **argv)
{    
    FILETIME FileTime;
    WORD ResultDate;
    WORD ResultTime;
    BOOL ret;
    int i;

    testCase testCases[] =
    {
        /* Test a normal time */
        {0x9BE00100, 0x1B4A02C, 0x14CF, 0x55AF}, /* 12/15/2000, 10:45:30 AM*/
        /* Test that 12/15/2000, 10:45:29 Gets rounded up */
        {0x9B476A80, 0x1B4A02C, 0x14CF, 0x55AF}, /* 12/15/2000, 10:45:30 AM*/
        /* Test that 12/15/2000, 10:45:31 Gets rounded up */
        {0x9C789780, 0x1B4A02C, 0x14CF, 0x55B0}, /* 12/15/2000, 10:45:32 AM*/
        
        /* Test the upper and lower limits of the function */
        {0xE1D58000, 0x1A8E79F, 0x0021, 0x0000}, /* 1/1/1980, 12:00:00 AM*/
        {0xb9de1300, 0x1e9eede, 0x739f, 0xbf7d}, /* 12/31/2037, 11:59:58 PM*/
     
        /* Tests that should fail */
        {0, 0, 0, 0},
        {0xE0A45300, 0x1A8E79F, 0, 0},
        {0x66D29301, 0x23868B8, 0, 0}
        
		/* All this accomplishes is for the date to overflow.
		Likely the only reason it fails in Windows is bacause the
		resulting date falls outside of the legal range.  Under BSD,
		it falls into a legal range. This being that BSD calculates time
		from 1900 to 2037, not 1980 to 2107.
		{0xFFFFFFFF, 0xFFFFFFF, 0, 0}
		*/
    };
    
    if (0 != PAL_Initialize(argc,argv))
    {
        return FAIL;
    }

    for (i=0; i<sizeof(testCases) / sizeof(testCase); i++)
    {
        ResultDate = 0xFFFF;
        ResultTime = 0xFFFF;

        FileTime.dwLowDateTime = testCases[i].FileTimeLow;
        FileTime.dwHighDateTime = testCases[i].FileTimeHigh;


        ret = FileTimeToDosDateTime(&FileTime, &ResultDate, &ResultTime);
        if (testCases[i].FatDate != 0 || testCases[i].FatTime != 0)
        {
            /* Expected it to pass */
            if (!ret)
            {
                Fail("FileTimeToDosDateTime failed for %X,%X!\n", 
                    testCases[i].FileTimeLow, testCases[i].FileTimeHigh);
            }

            if (ResultDate != testCases[i].FatDate || 
                ResultTime != testCases[i].FatTime)
            {
                Fail("FileTimeToDosDateTime did not convert %X,%X "
                    "successfully:\nExpected date to be %hX, time %hx.\n"
                    "Got %hX, %hX\n", testCases[i].FileTimeLow, 
                    testCases[i].FileTimeHigh, testCases[i].FatDate, 
                    testCases[i].FatTime, ResultDate, ResultTime);
            }
        }
        else
        {
            /* Expected it to fail. */
            if (ret)
            {
                Fail("FileTimeToDosDateTime passed for %X,%X!\n",
                    testCases[i].FileTimeLow, testCases[i].FileTimeHigh);
            }

            if (ResultDate != 0xFFFF || ResultTime != 0xFFFF)
            {
                Fail("FileTimeToDosDateTime failed, but modified output "
                    "parameters: %X %X\n", ResultDate, ResultTime);
            }
        }

    }

    PAL_Terminate();
    return PASS;
}

