// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Xunit;

namespace JitTest_isinst_call_cs
{
    public class BaseClass { }

    public class TestClass : BaseClass
    {
        private static object Method_To_Call(int cookie)
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

        private static bool Test_CALL(int cookie, bool flag)
        {
            try
            {
                TestClass inst;
                if (flag)
                {
                    inst = Method_To_Call(cookie) as TestClass;
                    return inst != null;
                }
                else
                {
                    inst = Method_To_Call(cookie) as TestClass;
                    return inst == null;
                }
            }
            catch (Exception)
            {
                return false;
            }
        }

        [Fact]
        public static int TestEntryPoint()
        {
            if (!Test_CALL(0, true))
            {
                Console.WriteLine("Failed => 101");
                return 101;
            }
            if (!Test_CALL(1, true))
            {
                Console.WriteLine("Failed => 102");
                return 102;
            }
            if (!Test_CALL(2, false))
            {
                Console.WriteLine("Failed => 103");
                return 103;
            }
            if (!Test_CALL(3, false))
            {
                Console.WriteLine("Failed => 104");
                return 104;
            }
            if (!Test_CALL(4, false))
            {
                Console.WriteLine("Failed => 105");
                return 105;
            }
            Console.WriteLine("Passed => 100");
            return 100;
        }
    }

    internal class DerivedClass : TestClass { }
    internal class OtherClass { }
}
