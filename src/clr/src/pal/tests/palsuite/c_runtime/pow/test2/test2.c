// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

/*=====================================================================
**
** Source:  test2.c
**
** Purpose:  Call the pow function with various values, 
**           to test special in/out values.
**
**
**===================================================================*/

/* Notes: The following table summarizes expected results:
          NUMBER       EXPONENT            RETURNS   CASE
          PosInf       > 0                 PosInf     1 
          PosInf       < 0                 0          2
          NegInf       > 0 (even int)      PosInf     3
          NegInf       > 0 (odd int)       NegInf     4
          NegInf       < 0 (odd int)       0          5
          NegInf       < 0 (even int)      0          6
          |num| > 1    PosInf              PosInf     7
          |num| < 1    PosInf              0          8
          |num| > 1    NegInf              0          9
          |num| < 1    NegInf              PosInf    10
          +1           PosInf              NaN       11
          +1           NegInf              NaN       12
          < 0          non-int             NaN       13
          0            < 0 (odd int)       NegInf    14
          0            < 0 (even int)      PosInf    15
          Large        Large               PosInf    16
          -Large       Large               NegInf    17
          Large        -Large              0         18
          -Large       -Large              0         19
*/

#include <palsuite.h>

struct testCase
{
    double Number;
    double Exponent;
};

int __cdecl main(int argc, char **argv)
{
    double zero = 0.0;
    double PosInf = 1.0 / zero;
    double NegInf = -1.0 / zero;
    double NaN = 0.0 / zero;
    volatile double result=0;
    int i=0;

    struct testCase retPosInf[] =
        {                                              /* CASE */
            {PosInf,         .3123                },   /*  1   */
            {PosInf,         3123                 },   /*  1   */
            {PosInf,         31.23                },   /*  1   */
            {NegInf,         2                    },   /*  3   */
            {NegInf,         3576                 },   /*  3   */
            {1.1,            PosInf               },   /*  7   */
            {6.2315,         PosInf               },   /*  7   */
            {423511,         PosInf               },   /*  7   */
            {-1.1,           PosInf               },   /*  7   */
            {-6.2315,        PosInf               },   /*  7   */
            {-423511,        PosInf               },   /*  7   */
            {0.1234,         NegInf               },   /* 10   */
            {-0.134,         NegInf               },   /* 10   */
            {0.1234,         NegInf               },   /* 10   */
            {0,              -1                   },   /* 14   */
            {0,              -3                   },   /* 14   */
            {0,              -1324391351          },   /* 14   */
            {0,              -2                   },   /* 15   */
            {0,              -35798               },   /* 15   */
            {MAXLONG,       MAXLONG               }    /* 16   */
        };

    struct testCase retNegInf[] =
        {
            {NegInf,         1                    },   /*  4   */
            {NegInf,         1324391315           },   /*  4   */
            {-(MAXLONG),     MAXLONG              }    /* 17   */
        };

    struct testCase retNaN[] =
        {
            {1,              PosInf               },   /*  11  */
            {1,              NegInf               },   /*  12  */
            {-1,             -0.1                 },   /*  13  */
            {-0.1,           -0.1                 },   /*  13  */
            {-1324391351,    -0.1                 },   /*  13  */
            {-3124.391351,   -0.1                 },   /*  13  */
            {-1,             0.1                  },   /*  13  */
            {-0.1,           0.1                  },   /*  13  */
            {-1324391351,    0.1                  },   /*  13  */
            {-3124.391351,   0.1                  }    /*  13  */
        };

    struct testCase retZero[] =
        {
            {PosInf,         -0.323               },   /*  2   */
            {PosInf,         -1                   },   /*  2   */
            {PosInf,         -1324391351          },   /*  2   */
            {PosInf,         -3124.391351         },   /*  2   */
            {NegInf,         -1                   },   /*  5   */
            {NegInf,         -3                   },   /*  5   */
            {NegInf,         -1324391351          },   /*  5   */
            {NegInf,         -2                   },   /*  6   */
            {NegInf,         -4                   },   /*  6   */
            {NegInf,         -1243913514          },   /*  6   */
            {0.132,          PosInf               },   /*  8   */
            {-0.132,         PosInf               },   /*  8   */
            {1.1,            NegInf               },   /*  9   */
            {-1.1,           NegInf               },   /*  9   */
            {2,              NegInf               },   /*  9   */
            {3,              NegInf               },   /*  9   */
            {-1324391353,    NegInf               },   /*  9   */
            {1324391354,     NegInf               },   /*  9   */
            {-31.24391353,   NegInf               },   /*  9   */
            {31.24391353,    NegInf               },   /*  9   */
            {MAXLONG,       -(MAXLONG)            },   /* 18   */
            {-(MAXLONG),    -(MAXLONG)            }    /* 19   */
        };


    if (0 != (PAL_Initialize(argc, argv)))
    {
        return FAIL;
    }

    /* Loop through each case. Call pow on each number/exponent pair 
       and check the result.
    */
    /* First those test cases returning positive infinity.          */
    for(i = 0; i < sizeof(retPosInf) / sizeof(struct testCase); i++)
    {
        result = pow(retPosInf[i].Number, retPosInf[i].Exponent);

        if ( result != PosInf )
        {
            Fail("ERROR: pow took '%f' to the exponent '%f' "
                 "to be %f instead of %f.\n", 
                 retPosInf[i].Number, 
                 retPosInf[i].Exponent, 
                 result, 
                 PosInf);
        }
    }

    /* First those test cases returning negative infinity.          */
    for(i = 0; i < sizeof(retNegInf) / sizeof(struct testCase); i++)
    {
        result = pow(retNegInf[i].Number, retNegInf[i].Exponent);

        if ( result != NegInf )
        {
            Fail("ERROR: pow took '%f' to the exponent '%f' "
                 "to be %f instead of %f.\n", 
                 retNegInf[i].Number, 
                 retNegInf[i].Exponent, 
                 result, 
                 NegInf);
        }
    }

    /* First those test cases returning non-numbers.          */
    for(i = 0; i < sizeof(retNaN) / sizeof(struct testCase); i++)
    {
        result = pow(retNaN[i].Number, retNaN[i].Exponent);

        if ( ! _isnan(result) )
        {
            Fail("ERROR: pow took '%f' to the exponent '%f' "
                 "to be %f instead of %f.\n", 
                 retNaN[i].Number, 
                 retNaN[i].Exponent, 
                 result, 
                 NaN);
        }
    }

    /* First those test cases returning zero.          */
    for(i = 0; i < sizeof(retZero) / sizeof(struct testCase); i++)
    {
        result = pow(retZero[i].Number, retZero[i].Exponent);

        if ( result != 0)
        {
            Fail("ERROR: pow took '%f' to the exponent '%f' "
                 "to be %f instead of %f.\n", 
                 retZero[i].Number, 
                 retZero[i].Exponent, 
                 result, 
                 0.0);
        }
    }

    PAL_Terminate();
    return PASS;
}













