// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

// Getter and Setter: if/else logic
// Getter: 26 bytes il, should be inlined
// Setter: 40 bytes il, should not be inlined
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
                return (m_prop != 100) ? m_prop : 100;
            }
            set
            {
                if (value == 1)
                    m_prop = value + 99;
                else if (value == 2)
                    m_prop = value + 98;
                else
                    m_prop = value;
            }
        }
    }
    class IfElse
    {
        public static int Main()
        {
            A a = new A();
            a.prop = 1;
            int retval = a.prop;
            return retval;
        }
    }
}
