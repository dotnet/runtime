// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
//

namespace Test
{
    using System;

    struct AA
    {
        private double[] m_dummyField1;
        private double[] m_dummyField2;
        private double[] m_dummyField3;

        static int m_nStaticFld;

        public int Method1() { return 0; }

        static void Static1(ref AA[] param4, int param5)
        {
            param4[param4[param5].Method1()].Method1();
        }

        static int Main()
        {
            try
            {
                AA[] aa = null;
                Static1(ref aa, m_nStaticFld);
            }
            catch (Exception) { }
            return 100;
        }
    }
}
