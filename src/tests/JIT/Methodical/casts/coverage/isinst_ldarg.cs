// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Xunit;

namespace JitTest_isinst_ldarg_cs
{
    public class BaseClass { }

    public class TestClass : BaseClass
    {
        private static bool Test_LDARG(object obj, bool flag)
        {
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
            if (!Test_LDARG(new TestClass(), true))
            {
                Console.WriteLine("Failed => 101");
                return 101;
            }
            if (!Test_LDARG(new DerivedClass(), true))
            {
                Console.WriteLine("Failed => 102");
                return 102;
            }
            if (!Test_LDARG(new BaseClass(), false))
            {
                Console.WriteLine("Failed => 103");
                return 103;
            }
            if (!Test_LDARG(new OtherClass(), false))
            {
                Console.WriteLine("Failed => 104");
                return 104;
            }
            if (!Test_LDARG(null, false))
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
