// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//

using Xunit;
namespace Test
{
    using System;

    public class AA
    {
        public double m_dField2 = 47.26;
        public static float m_fForward1;
        internal static void Method2(object param2, ref double param4)
        {
            while (param4 != 0.0d)
            {
                do
                {
                } while ((object)m_fForward1 != param2);
            }
        }
        [Fact]
        public static int TestEntryPoint()
        {
            try
            {
                AA aa = null;
                Method2(null, ref aa.m_dField2);
            }
            catch (Exception)
            {
            }
            return 100;
        }
    }
}
