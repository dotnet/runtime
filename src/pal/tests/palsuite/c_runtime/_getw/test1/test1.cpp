// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

/*============================================================================
**
** Source:  test1.c
**
** Purpose: Several integers are read from a previously written file
**          using _getw.  The test passes if the values read match those known to
**          be in the file.
**
**
**==========================================================================*/

#include <palsuite.h>

/*Tests _getw using a previously written data file */
int __cdecl main(int argc, char **argv)
{
    const int testValues[] =
    {
        0,
        1,
        -1,
        0x7FFFFFFF,            /* largest positive integer on 32 bit systems */
        0x80000000,            /* largest negative integer on 32 bit systems */
        0xFFFFFFFF,
        0xFFFFAAAA
    };

    int i = 0;
    int input = 0;

    const char filename[] = "test.dat";


    FILE *fp = NULL;

    /*
     * Initialize the PAL and return FAIL if this fails
     */
    if (0 != (PAL_Initialize(argc, argv)))
    {
        return FAIL;
    }

    /* write the file that we will use to test */


    /*
      Don't uncomment this code, it was used to create the data file
      initially on windows, but if it is run on all test platforms, the
      tests will always pass.

      fp = fopen(filename, "w");
      if (fp == NULL)
      {
      Fail("Unable to open file for write.\n");
      }
      for (i = 0; i < sizeof(testValues) / sizeof(testValues[0]); i++)
      {
      _putw(testValues[i], fp);
      }

      if (fclose(fp) != 0)
      {
      Fail("Error closing file after writing to it with _putw.\n");
      }
    */


    /*Now read values back from the file and see if they match.*/
    fp = fopen(filename, "r");
    if (fp == NULL)
    {
        Fail ("Unable to open file for read.\n");
    }
    for (i = 0; i < sizeof(testValues) / sizeof(testValues[0]); i++)
    {
        input = _getw(fp);
        if (VAL32(input) != testValues[i])
        {
            Fail ("_getw did not get the expected values when reading "
                    "from a file.\n");
        }
    }

    if (fclose(fp) != 0)
    {
        Fail ("Error closing file after reading from it with _getw\n");
    }
    PAL_Terminate();
    return PASS;
}

