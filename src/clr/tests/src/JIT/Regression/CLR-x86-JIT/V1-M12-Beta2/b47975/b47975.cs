// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

namespace Test
{
    using System;

    class AA
    {
        static int Method1(uint param2) { return 0; }

        static bool StaticFunc(bool param1, ulong param2) { return false; }

        static int TestFunc(int param1, object[] param2)
        {
            uint[] au = new uint[2];
            StaticFunc(
                StaticFunc(true, 0),
                au[0] + checked(au[1] * au[0])
            );
            return (int)param2[Method1(au[param1])];
        }

        static int Main()
        {
            try
            {
                TestFunc(0, null);
            }
            catch (NullReferenceException)
            {
                Console.WriteLine("passed");
                return 100;
            }
            return -1;
        }
    }
}
