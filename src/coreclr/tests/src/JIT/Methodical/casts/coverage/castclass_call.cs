// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

namespace JitTest
{
    internal class BaseClass { }

    internal class TestClass : BaseClass
    {
        private object Method_To_Call(int cookie)
        {
            switch (cookie)
            {
                case 0:
                    return new TestClass();
                case 1:
                    return new DerivedClass();
                case 2:
                    return new BaseClass();
                case 3:
                    return new OtherClass();
                default:
                    return null;
            }
        }

        private static bool Test_CALL(TestClass _this, int cookie, bool flag)
        {
            try
            {
                TestClass inst;
                if (flag)
                {
                    inst = (TestClass)(_this.Method_To_Call(cookie));
                    return inst != null;
                }
                else
                {
                    inst = (TestClass)(_this.Method_To_Call(cookie));
                    return inst == null && _this.Method_To_Call(cookie) == null;
                }
            }
            catch (Exception X)
            {
                return !flag && X is InvalidCastException;
            }
        }

        private static int Main()
        {
            TestClass _this = new TestClass();
            if (!Test_CALL(_this, 0, true))
            {
                Console.WriteLine("Failed => 101");
                return 101;
            }
            if (!Test_CALL(_this, 1, true))
            {
                Console.WriteLine("Failed => 102");
                return 102;
            }
            if (!Test_CALL(_this, 2, false))
            {
                Console.WriteLine("Failed => 103");
                return 103;
            }
            if (!Test_CALL(_this, 3, false))
            {
                Console.WriteLine("Failed => 104");
                return 104;
            }
            if (!Test_CALL(_this, 4, false))
            {
                Console.WriteLine("Failed => 104");
                return 105;
            }
            Console.WriteLine("Passed => 100");
            return 100;
        }
    }

    internal class DerivedClass : TestClass { }
    internal class OtherClass { }
}
