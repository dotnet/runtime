// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//

using Xunit;
namespace Test
{
    using System;

    public class AA
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
        [Fact]
        public static int TestEntryPoint()
        {
            Method1(new bool[3]);
            return 100;
        }
    }
}
