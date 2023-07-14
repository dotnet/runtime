// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// Setter 34 bytes il (inlined), Getter 36 bytes il (not-inlined)

using System;
using Xunit;

namespace JitInliningTest
{
    public class A
    {
        private int _prop;
        public int prop
        {
            get { return _prop + _prop * (_prop + 1) * (_prop - 1); }
            set { _prop = value * value + (value + 1) * (value - 1) - (value + 2) * (value - 2) + (value + 3) * (value - 3); }
        }
    }
    public class PropTest
    {
        [Fact]
        public static int TestEntryPoint()
        {
            A a = new A();
            a.prop = 1;
            int retval = a.prop + 164;
            return retval;
        }
    }
}
