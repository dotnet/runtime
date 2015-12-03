// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

// Getter and Setter: SEH

using System;

namespace JitInliningTest
{
    public class A
    {
        private int _prop;
        public int prop
        {
            get { return _prop; }
            set
            {
                try
                {
                    _prop = value;
                }
                catch
                { }
            }
        }
    }
    internal class PropTest5
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
