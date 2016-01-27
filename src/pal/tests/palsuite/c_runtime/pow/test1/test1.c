// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

/*=====================================================================
**
** Source:  test1.c
**
** Purpose:  Call the pow function with various values, 
**           to test specified valid input and boundaries.
**
**
**===================================================================*/

/* Notes: SCENARIO                                             CASE
          - Both number and exponent may be non-integers         1
          - Exponent may be negative                             2
          - If number anything and exponent is 0, returns 1      3 
          - If number is 0 and exponent is positive, returns 0   4
          - Number may be negative with integer exponents        5
          - Other valid input returns the usual valid output     6
          - See test2 for infinite and nan input/output cases    
*/

#include <palsuite.h>

/* Error acceptance level to the 7th decimal */   
#define DELTA 0.0000001

struct testCase
{
    double Number;
    double Exponent;
    double CorrectValue;
};

int __cdecl main(int argc, char **argv)
{
    double result=0;
    double testDelta=99999;
    int i=0;

    struct testCase testCases[] =                            /* CASE  */
        {
            {0,           0,       1                      }, /* 3  */
            {0.0,         0.0,     1                      }, /* 3  */
            {-0,          0,       1                      }, /* 3  */
            {-0,          -0,      1                      }, /* 3  */
            {2,           0,       1                      }, /* 3  */
            {2,           0.0,     1                      }, /* 3  */
            {2,           -0.0,    1                      }, /* 3  */
            {42.3124234,  0,       1                      }, /* 3  */
            {-2,          -0.0,    1                      }, /* 3  */
            {-3.33132,    -0.0,    1                      }, /* 3  */
            {-999990,     0,       1                      }, /* 3  */
            {9999.9999,   0.0,     1                      }, /* 3  */
            {-9999.9999,  0.0,     1                      }, /* 3  */
            {0,           1,       0                      }, /* 4  */ 
            {0.0,         2,       0                      }, /* 4  */ 
            {0.0,         3,       0                      }, /* 4  */ 
            {-0,          9.99999, 0                      }, /* 4  */ 
            {2,           2,       4                      }, /* 6  */ 
            {2,           -2,      0.25                   }, /* 6  */
            {6.25,        2.5,     97.65625               }, /* 6  */
            {12345.12345, 9.99999, 
             82207881573997179707867981634171194834944.0  }, /* 6  */
            {12345,       75,
             7.2749844621476552703935675020036e+306       }, /* 6  */
            {-2.012,      2,       4.048144               }, /* 6  */
            {2,           1,       2                      }, /* 6  */
            {8,           1,       8                      }, /* 6  */
            {MAXLONG,     1,       MAXLONG                }, /* 6  */
            {4.321,       1,       4.321                  }, /* 6  */
            {-4.321,      1,      -4.321                  }  /* 6  */
        };
    if (0 != (PAL_Initialize(argc, argv)))
    {
        return FAIL;
    }

    /* Loop through each case. Call pow on each value and check the 
       result.
    */
    for(i = 0; i < sizeof(testCases) / sizeof(struct testCase); i++)
    {
        result = pow(testCases[i].Number, testCases[i].Exponent);
        testDelta = fabs(result - testCases[i].CorrectValue);

        if ( testDelta >= DELTA )
        {
            Fail("ERROR: pow took the '%f' to the exponent '%f' "
                 "to be %f instead of %f.\n", 
                 testCases[i].Number, 
                 testCases[i].Exponent, 
                 result, 
                 testCases[i].CorrectValue);
        }
    }

    PAL_Terminate();
    return PASS;
}













