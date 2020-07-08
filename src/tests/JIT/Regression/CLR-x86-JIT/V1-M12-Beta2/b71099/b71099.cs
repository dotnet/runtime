// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
//

namespace Test
{
    using System;

    public struct AA
    {
        public static uint m_uStatic2;

        public static bool Static3(ref bool[] param1, ref sbyte[] param2)
        {
            double[] local3 = new double[] { 106.0, 7.0, 122.0, 55.0, 112.0 };
            uint[] local4 = new uint[] { AA.m_uStatic2, 124u, AA.m_uStatic2, 5u };
            long local5 = new long[] { }[23];
            float[] local6 = new float[] { 54.0f };
            sbyte local7 = ((sbyte)CC.m_xStatic1);
            bool local8 = ((bool)CC.m_xStatic1);
            return ((bool)CC.m_xStatic1);
        }
    }

    public class CC
    {
        public static object m_xStatic1 = null;
    }

    class App
    {
        static int Main()
        {
            try
            {
                AA.Static3(
                    ref App.m_abFwd12,
                    ref App.m_asuFwd6);
                return 101;
            }
            catch (IndexOutOfRangeException)
            {
                return 100;
            }
        }
        public static sbyte[] m_asuFwd6;
        public static bool[] m_abFwd12;
    }
}
