// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

// Setter 34 bytes il (inlined), Getter 36 bytes il (not-inlined)
using System;
namespace JitInliningTest
{
    public class A
    {
        private int m_prop;
        public int prop
        {
            get { return m_prop + m_prop * (m_prop + 1) * (m_prop - 1); }
            set { m_prop = value * value + (value + 1) * (value - 1) - (value + 2) * (value - 2) + (value + 3) * (value - 3); }
        }
    }
    class PropTest
    {
        public static int Main()
        {
            A a = new A();
            a.prop = 1;
            int retval = a.prop + 164;
            return retval;
        }
    }

}
