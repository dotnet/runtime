// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Xunit;

namespace JitTest_unbox_operand_cs
{
    public struct Test
    {
        private int _m_v;

        private static int unbox_ldobj()
        {
            Test T = new Test();
            T._m_v = 1;
            object R = T;
            return ((Test)R)._m_v - 1;
        }

        private static int unbox_initobj()
        {
            Test T = new Test();
            T._m_v = 1;
            object R = T;
            R = new Test();     //change to unbox<R> = new Test() in IL
            return ((Test)R)._m_v;
        }

        private static int unbox_cpobj()
        {
            Test T = new Test();
            T._m_v = 1;
            Test T1 = new Test();
            object R = T;
            R = T1;     //change to unbox<R> = T1 in IL
            return ((Test)R)._m_v;
        }

        private static int unbox_stobj()
        {
            Test T = new Test();
            T._m_v = 1;
            Test T1 = new Test();
            object R = T;
            R = T1;     //change to unbox<R> = T1 in IL 
            return ((Test)R)._m_v;
        }

        [Fact]
        public static int TestEntryPoint()
        {
            if (unbox_ldobj() != 0)
            {
                return 101;
            }
            if (unbox_initobj() != 0)
            {
                return 102;
            }
            if (unbox_stobj() != 0)
            {
                return 103;
            }
            if (unbox_cpobj() != 0)
            {
                return 104;
            }
            return 100;
        }
    }
}
