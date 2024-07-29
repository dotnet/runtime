// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//

using Xunit;
namespace Test
{
    using System;

    public class AA
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

        [Fact]
        public static int TestEntryPoint()
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
