// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Xunit;

namespace JitTest_isinst_ldloc_cs
{
    public class BaseClass { }

    public class TestClass : BaseClass
    {
        private static bool Test_LDLOC(object _obj, bool flag)
        {
            object obj = _obj;
            try
            {
                TestClass inst;
                if (flag)
                {
                    inst = obj as TestClass;
                    return inst != null;
                }
                else
                {
                    inst = obj as TestClass;
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
            if (!Test_LDLOC(new TestClass(), true))
            {
                Console.WriteLine("Failed => 101");
                return 101;
            }
            if (!Test_LDLOC(new DerivedClass(), true))
            {
                Console.WriteLine("Failed => 102");
                return 102;
            }
            if (!Test_LDLOC(new BaseClass(), false))
            {
                Console.WriteLine("Failed => 103");
                return 103;
            }
            if (!Test_LDLOC(new OtherClass(), false))
            {
                Console.WriteLine("Failed => 104");
                return 104;
            }
            if (!Test_LDLOC(null, false))
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
