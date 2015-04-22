// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

// Getter and Setter: throw
// Both have 32 bytes il, both should not be inlined.
using System;
namespace JitInliningTest
{
    public class A
    {
        private int m_prop;
        public int prop
        {
            get
            {
                if (m_prop != 100)
                    throw new Exception("Excect m_prop=100");
                return m_prop;
            }
            set
            {
                if (value != 1000 - 9 * value)
                    throw new Exception("Excect 100 as input");
                m_prop = value;
            }
        }
    }
    class throwTest
    {
        public static int Main()
        {
            A a = new A();
            a.prop = 100;
            int retval = a.prop;
            return retval;
        }
    }
}
