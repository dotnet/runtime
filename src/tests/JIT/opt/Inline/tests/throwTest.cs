// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// Getter and Setter: throw

using System;
using Xunit;

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
    public class throwTest
    {
        [Fact]
        public static int TestEntryPoint()
        {
            A a = new A();
            a.prop = 100;
            int retval = a.prop;
            return retval;
        }
    }
}
