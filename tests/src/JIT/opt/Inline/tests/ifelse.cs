// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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
                return (_prop != 100) ? _prop : 100;
            }
            set
            {
                if (value == 1)
                    _prop = value + 99;
                else if (value == 2)
                    _prop = value + 98;
                else
                    _prop = value;
            }
        }
    }
    internal class IfElse
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
