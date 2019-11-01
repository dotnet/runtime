// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
//

namespace Test
{
    using System;

    class BB
    {
        public static float[] m_afField3 = new float[7];

        public static bool Method2(__arglist) { return false; }
        public static float[] Static1(ref float param1) { return new float[7]; }
        public static double[] Static2(float param2) { return (new double[7]); }

        static int Main()
        {
            Method2(
                __arglist(
                    (int)Static2(Static1(ref Static1(ref BB.m_afField3[2])[2])[2])[2]
                )
            );
            return 100;
        }
    }
}
