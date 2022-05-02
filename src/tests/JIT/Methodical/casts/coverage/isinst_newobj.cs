// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Xunit;

namespace JitTest_isinst_newobj_cs
{
    public class BaseClass { }

    public class TestClass : BaseClass
    {
        private static bool Test_NEWOBJ(TestClass _this, int cookie, bool flag)
        {
            TestClass inst;
            switch (cookie)
            {
                case 0:
                    try
                    {
                        inst = ((object)(new TestClass())) as TestClass;
                        return inst != null;
                    }
                    catch (Exception) { return false; }
                case 1:
                    try
                    {
                        inst = ((object)(new DerivedClass())) as TestClass;
                        return inst != null;
                    }
                    catch (Exception) { return false; }
                case 2:
                    try
                    {
                        inst = ((object)(new BaseClass())) as TestClass;
                        return inst == null;
                    }
                    catch (Exception) { return false; }
                case 3:
                    try
                    {
                        inst = ((object)(new OtherClass())) as TestClass;
                        return inst == null;
                    }
                    catch (Exception) { return false; }
                default:
                    return false;
            }
        }

        [Fact]
        public static int TestEntryPoint()
        {
            TestClass _this = new TestClass();
            if (!Test_NEWOBJ(_this, 0, true))
            {
                Console.WriteLine("Failed => 101");
                return 101;
            }
            if (!Test_NEWOBJ(_this, 1, true))
            {
                Console.WriteLine("Failed => 102");
                return 102;
            }
            if (!Test_NEWOBJ(_this, 2, false))
            {
                Console.WriteLine("Failed => 103");
                return 103;
            }
            if (!Test_NEWOBJ(_this, 3, false))
            {
                Console.WriteLine("Failed => 104");
                return 104;
            }
            Console.WriteLine("Passed => 100");
            return 100;
        }
    }

    internal class DerivedClass : TestClass { }
    internal class OtherClass { }
}
