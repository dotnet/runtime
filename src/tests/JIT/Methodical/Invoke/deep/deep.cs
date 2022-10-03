// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Xunit;

namespace JitTest_deep_deep_cs
{
    public class Test
    {
        private static double[] Method(double arg1, float arg2, ref double refarg)
        {
            double[] ret = new double[4];
            if (arg1 < 11.0)
            {
                ret[0] = arg1 + Method(arg1 + 1.0, arg2, ref refarg)[0];
                ret[1] = arg2 + Method(arg1 + 2.0, arg2, ref ret[0])[1];
                ret[2] = arg1 + arg2 + Method(arg1 + 3.0, arg2, ref ret[1])[2];
                ret[3] = arg1 + arg2 * 2 + Method(arg1 + 4.0, arg2, ref ret[2])[3];
            }
            refarg += 1.0d;
            return ret;
        }

        private static double Method2(double arg1, int arg2, double refarg)
        {
            double[] ret = new double[4];
            if (arg1 < 11.0)
            {
                ret[0] = arg1 + Method2(arg1 + 1.0, arg2, refarg);
                ret[1] = arg2 + Method2(arg1 + 2.0, arg2, ret[0]);
                ret[2] = arg1 + arg2 + Method2(arg1 + 3.0, arg2, ret[1]);
                ret[3] = arg1 + arg2 * 2 + Method2(arg1 + 4.0, arg2, ret[2]);
            }
            return ret[0] + ret[1] + ret[2] + ret[3];
        }

        [Fact]
        public static int TestEntryPoint()
        {
            try
            {
                double L = -1.0;
                if (Method(0.0d, 1.0f, ref L)[3] != 18.0)
                    throw new Exception();

                L = Method2(

                    Method(0.0d, 0.0f, ref
                    Method(1.0d, 0.0f, ref
                    Method(2.0d, 0.0f, ref
                    Method(3.0d, 0.0f, ref
                    Method(4.0d, 0.0f, ref
                    Method(5.0d, 0.0f, ref
                    Method(6.0d, 0.0f, ref
                    Method(7.0d, 0.0f, ref
                    Method(8.0d, 0.0f, ref
                    Method(9.0d, 0.0f, ref
                    Method(0.0d, 1.0f, ref
                    Method(0.0d, 2.0f, ref
                    Method(0.0d, 3.0f, ref
                    Method(0.0d, 4.0f, ref
                    Method(0.0d, 5.0f, ref
                    Method(0.0d, 6.0f, ref
                    Method(0.0d, 7.0f, ref
                    Method(0.0d, 8.0f, ref
                    Method(0.0d, 9.0f, ref
                    Method(0.0d, 10.0f, ref
                    Method(0.0d, 11.0f, ref
                    Method(0.0d, 12.0f, ref L
                    )[1]
                    )[2]
                    )[3]
                    )[0]
                    )[1]
                    )[2]
                    )[3]
                    )[2]
                    )[1]
                    )[0]
                    )[0]
                    )[1]
                    )[2]
                    )[2]
                    )[3]
                    )[2]
                    )[1]
                    )[0]
                    )[1]
                    )[1]
                    )[1]
                    )[1]

                    , 0,
                    Method2(1.0d, 0,
                    Method2(2.0d, 0,
                    Method2(3.0d, 0,
                    Method2(4.0d, 0,
                    Method2(5.0d, 0,
                    Method2(6.0d, 0,
                    Method2(7.0d, 0,
                    Method2(8.0d, 0,
                    Method2(9.0d, 0,
                    Method2(0.0d, 1,
                    Method2(0.0d, 2,
                    Method2(0.0d, 3,
                    Method2(0.0d, 4,
                    Method2(0.0d, 5,
                    Method2(0.0d, 6,
                    Method2(0.0d, 7,
                    Method2(0.0d, 8,
                    Method2(0.0d, 9,
                    Method2(0.0d, 10,
                    Method2(0.0d, 11,
                    Method2(0.0d, 12, L
                    )
                    )
                    )
                    )
                    )
                    )
                    )
                    )
                    )
                    )
                    )
                    )
                    )
                    )
                    )
                    )
                    )
                    )
                    )
                    )
                    )
                    );
            }
            catch
            {
                Console.WriteLine("Failed w/ exception");
                return 1;
            }
            Console.WriteLine("Passed");
            return 100;
        }
    }
}
