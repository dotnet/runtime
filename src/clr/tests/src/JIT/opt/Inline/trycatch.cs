// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

// Getter and Setter: SEH
// Setter has try/catch, should not be inlined
using System;
namespace JitInliningTest
{
    public class A
    {
        private int m_prop;
        public int prop
        {
            get { return m_prop; }
            set
            {
                try
                {
                    m_prop = value;
                }
                catch
                { }
            }

        }
    }
    class PropTest5
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
