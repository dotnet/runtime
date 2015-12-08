// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

// Setter 34 bytes il (inlined), Getter 36 bytes il (not-inlined)

using System;

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
    internal class PropTest
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
