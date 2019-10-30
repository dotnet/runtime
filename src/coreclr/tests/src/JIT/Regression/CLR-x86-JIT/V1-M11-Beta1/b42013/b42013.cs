// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
//

namespace Test
{
    using System;

    class BB
    {
        ulong[] m_aulField = null;

        static void Func1(ref BB param1, double[] param2,
                                uint[] param3, ref bool param4) { }

        static uint[] Func2(long param1) { return null; }
        static bool[] Func3(ulong[] param4) { return null; }

        static void Func4(ref BB[] param1, ref long param2, ref long[] param3)
        {
            Func1(
                ref param1[(int)(param2 - param2)],
                null,
                Func2(param3[(int)param2]),
                ref Func3(new BB().m_aulField)[0]
            );
        }

        static int Main()
        {
            try
            {
                BB[] bb = null;
                long l = 0;
                long[] al = null;
                Func4(ref bb, ref l, ref al);
            }
            catch (NullReferenceException) { }
            return 100;
        }
    }
}
