// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

namespace Test
{
    using System;

    class AA
    {
        static double m_dStatic3 = 273.31;
        static int Main()
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
