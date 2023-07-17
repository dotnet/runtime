// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// Getter and Setter: SEH

using System;
using Xunit;

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
    public class PropTest5
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
