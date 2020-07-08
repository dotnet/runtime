// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
//

namespace Test
{
    using System;

    class BB
    {
        int[] an = null;
        object obj = null;

        static BB aa = null;
        static int n = 0;

        static int AA_Static2(bool[] param2, bool param3, int param6) { return 0; }
        static bool[] AA_Static4(ref int param3, int[] param4) { return null; }
        static float[] BB_Static1(int param4) { return null; }

        static void BB_Static2()
        {
            while ((uint)BB_Static1(
                            AA_Static2(
                                AA_Static4(ref n, aa.an),
                                false,
                                AA_Static2(
                                    AA_Static4(ref aa.an[2], aa.an),
                                (bool)aa.obj, 0)))[2] <= 0)
            {
            }
        }

        static int Main()
        {
            try
            {
                BB_Static2();
            }
            catch (Exception)
            {
                return 100;
            }
            return 1;
        }
    }
}
