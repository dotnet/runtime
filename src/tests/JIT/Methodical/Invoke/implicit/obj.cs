// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Xunit;

namespace JitTest_obj_implicit_cs
{
    public class Base
    {
        public override String ToString() { return "Base class"; }
    }

    public class Test : Base
    {
        public override String ToString() { return "Test class"; }

        private static void TestFunc(Base obj)
        {
            Console.WriteLine(obj.ToString());
        }

        [Fact]
        public static int TestEntryPoint()
        {
            try
            {
                TestFunc(new Base());
                TestFunc(new Test());
            }
            catch
            {
                Console.WriteLine("Failed w/ exception");
                return 1;
            }
            Console.WriteLine("Passed");
            return 100;
        }
    }
}
