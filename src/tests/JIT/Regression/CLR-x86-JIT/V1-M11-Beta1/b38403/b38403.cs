// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//

using Xunit;
namespace Test
{
    using System;

    class AA
    {
        public static bool m_bStatic1 = false;
    }

    class CC
    {
        public static AA[] m_axStatic4 = new AA[7];
    }

    public class JJ
    {
        static CC m_xStatic2 = new CC();

        static void Static1(float param4, AA param5)
        {
            while (AA.m_bStatic1) ;
        }

        [Fact]
        public static int TestEntryPoint()
        {
            CC local5 = new CC();
            do
            {
                Static1(0.0f, CC.m_axStatic4[2]);
            } while (AA.m_bStatic1);
            return 100;
        }
    }
}
