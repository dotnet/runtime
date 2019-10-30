// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
//

namespace Bug
{
    using System;

    class DD
    {
        public double[] m_field1;
        public static DD[] m_static3 = new DD[2];

        public double[] Method2()
        {
            return new double[5];
        }

        public static int Main()
        {
            m_static3[0] = new DD();
            m_static3[0].m_field1 = m_static3[0].Method2();
            return 100;
        }
    }
}
