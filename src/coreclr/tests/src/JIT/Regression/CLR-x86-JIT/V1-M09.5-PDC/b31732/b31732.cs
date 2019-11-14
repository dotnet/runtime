// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
//

namespace Test
{
    using System;

    class AA
    {
        public object m_xField2 = null;
        public static float Method1(bool[] param1)
        {
            AA local7 = new AA();
            try
            {
                while (param1[2])
                {
                    do
                    {
                    } while (param1[2] == ((bool)(new AA().m_xField2)));
                    do
                    {
                    } while (param1[2]);
                }
            }
            catch (Exception)
            {
            }
            return 0.0f;
        }
        static int Main()
        {
            Method1(new bool[3]);
            return 100;
        }
    }
}
