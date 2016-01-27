// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;

namespace JitTest
{
    internal class Base
    {
        public override String ToString() { return "Base class"; }
    }

    internal class Test : Base
    {
        public override String ToString() { return "Test class"; }

        private static void TestFunc(Base obj)
        {
            Console.WriteLine(obj.ToString());
        }

        private static int Main()
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
