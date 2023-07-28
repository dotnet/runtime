// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//

using Xunit;
namespace Test
{
    using System;

    public class AA
    {
        static double m_dStatic3 = 273.31;
        [Fact]
        public static int TestEntryPoint()
        {
            try
            {
                bool[] param1 = new bool[20];
                object[] param2 = new object[10];
                double local3 = 25.76;
                uint uField1 = 0;
                if ((double)uField1 <= m_dStatic3)
                {
                    do
                    {
                        do
                        {
                            do
                            {
                            } while (((bool)(param2[2])));
                            do
                            {
                            } while (0.70 <= local3);
                        } while (param1[2]);
                    } while (param1[2]);
                }
            }
            catch (NullReferenceException)
            {
                return 100;
            }
            return 1;
        }
    }
}
