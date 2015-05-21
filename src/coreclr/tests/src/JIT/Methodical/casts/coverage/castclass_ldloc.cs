// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

namespace JitTest
{
    internal class BaseClass { }

    internal class TestClass : BaseClass
    {
        private static bool Test_LDLOC(object _obj, bool flag)
        {
            object obj = _obj;
            try
            {
                TestClass inst;
                if (flag)
                {
                    inst = (TestClass)obj;
                    return inst != null;
                }
                else
                {
                    inst = (TestClass)obj;
                    return obj == null;
                }
            }
            catch (Exception X)
            {
                return !flag && X is InvalidCastException;
            }
        }

        private static int Main()
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
