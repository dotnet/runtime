// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

namespace JitTest
{
    internal class StressTest2
    {
        public String m_helloStr = "Hello!";
        public StressTest m_parent = null;
    }

    internal class StressTest
    {
        private StressTest2 _m_internal;

        private StressTest()
        {
            _m_internal = new StressTest2();
            _m_internal.m_parent = this;
        }

        private static bool Scenario1()
        {
            StressTest2 S = new StressTest2();
            TypedReference R = __makeref(S);
            S = null;
            GC.Collect();
            try
            {
                string str = __refvalue(R, StressTest2).m_helloStr;
                return false;
            }
            catch (NullReferenceException)
            {
                return true;
            }
        }

        private static bool Scenario2()
        {
            StressTest S = new StressTest();
            TypedReference R = __makeref(S._m_internal);
            S = null;
            GC.Collect();
            return __refvalue(R, StressTest2).m_parent._m_internal == __refvalue(R, StressTest2);
        }

        private static int Main()
        {
            if (!Scenario1())
            {
                return 1;
            }
            if (!Scenario2())
            {
                return 2;
            }
            return 100;
        }
    }
}
