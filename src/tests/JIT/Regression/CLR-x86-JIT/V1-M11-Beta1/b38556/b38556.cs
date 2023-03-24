// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//

using Xunit;
namespace Test
{
    using System;

    public class JJ
    {
        uint[] m_au = null;
        static uint[] s_au = new uint[7];
        static JJ[] m_ax = new JJ[7];

        uint[] AA_Method1(ref uint param1, ref uint[] param2) { return param2; }
        static void AA_Static1(ref uint param2, ref uint param4) { }
        static JJ CC_Static1() { return new JJ(); }

        internal static void FF_Static1(ref uint param3)
        {
            CC_Static1();
            AA_Static1(
                ref m_ax[0].m_au[2],
                ref m_ax[0].AA_Method1(ref s_au[0], ref s_au)[0]
            );
        }
        static void Main1()
        {
            FF_Static1(ref m_ax[0].AA_Method1(
                    ref s_au[0],
                    ref s_au)[0]);
        }
        [Fact]
        public static int TestEntryPoint()
        {
            try
            {
                Main1();
                return -1;
            }
            catch (NullReferenceException)
            {
                return 100;
            }
        }
    }
}
