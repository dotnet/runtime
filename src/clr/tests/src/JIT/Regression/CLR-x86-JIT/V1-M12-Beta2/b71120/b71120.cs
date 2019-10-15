// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
//

namespace Test
{
    using System;

    public struct AA
    {
        public double[] Method1(String param1, bool[] param2, ref long param3, __arglist)
        { return null; }
    }

    public class BB
    {
        public static bool[] m_abStatic1 = (new bool[110]);
    }

    public struct CC
    {
        public static sbyte m_suStatic1;

        public double[] Method1()
        {
            try
            {
                throw new Exception();
            }
            catch (DivideByZeroException)
            {
                long local4 = ((long)CC.m_suStatic1);
                return new AA().Method1("121", BB.m_abStatic1, ref local4, __arglist());
            }
            return new double[] { 42.0 };
        }
        static int Main()
        {
            try
            {
                new CC().Method1();
                return 101;
            }
            catch (Exception)
            {
                return 100;
            }
        }
    }
}
