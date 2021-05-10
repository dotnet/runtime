// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.InteropServices;
using System.Text;
using System.Collections.Generic;

namespace SafeHandleTests
{
    class Test
    {
        public static int Main(string[] args)
        {
            try
            {
                Console.WriteLine("Running SafeHandleTest tests");
                SafeHandleTest.RunTest();
                Console.WriteLine("Running ReliableUnmarshal test");
                ReliableUnmarshalTest.RunTest();
                Console.WriteLine("Running InvalidSafeHandleMarshalling tests");
                InvalidSafeHandleMarshallingTests.RunTest();
                Console.WriteLine("Running SafeHandleLifetime tests");
                SafeHandleLifetimeTests.RunTest();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
                return 101;
            }

            return 100;
        }
    }
}
