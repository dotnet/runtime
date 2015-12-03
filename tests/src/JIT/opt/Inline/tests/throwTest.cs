// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

// Getter and Setter: throw

using System;

namespace JitInliningTest
{
    public class A
    {
        private int _prop;
        public int prop
        {
            get
            {
                if (_prop != 100)
                    throw new Exception("Excect m_prop=100");
                return _prop;
            }
            set
            {
                if (value != 1000 - 9 * value)
                    throw new Exception("Excect 100 as input");
                _prop = value;
            }
        }
    }
    internal class throwTest
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
