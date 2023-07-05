// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Xunit;

namespace JitTest_refanyval_operand_cs
{
    public struct Test
    {
        private int _m_v;

        private static int refanyval_ldobj()
        {
            Test T = new Test();
            T._m_v = 1;
            TypedReference R = __makeref(T);
            return __refvalue(R, Test)._m_v - 1;
        }

        private static int refanyval_initobj()
        {
            Test T = new Test();
            T._m_v = 1;
            TypedReference R = __makeref(T);
            __refvalue(R, Test) = new Test();
            return T._m_v;
        }

        private static int refanyval_cpobj()
        {
            Test T = new Test();
            T._m_v = 1;
            Test T1 = new Test();
            TypedReference R = __makeref(T);    //replace with cpobj in IL
            __refvalue(R, Test) = T1;
            return T._m_v;
        }

        private static int refanyval_stobj()
        {
            Test T = new Test();
            T._m_v = 1;
            Test T1 = new Test();
            TypedReference R = __makeref(T);
            __refvalue(R, Test) = T1;
            return T._m_v;
        }

        [Fact]
        public static int TestEntryPoint()
        {
            if (refanyval_ldobj() != 0)
            {
                return 101;
            }
            if (refanyval_initobj() != 0)
            {
                return 102;
            }
            if (refanyval_stobj() != 0)
            {
                return 103;
            }
            if (refanyval_cpobj() != 0)
            {
                return 104;
            }
            return 100;
        }
    }
}
