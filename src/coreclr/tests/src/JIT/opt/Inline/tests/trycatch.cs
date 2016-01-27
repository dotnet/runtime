// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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
