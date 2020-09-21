// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace NetClient
{
    using System;
    using System.Runtime.InteropServices;

    using TestLibrary;
    using Server.Contract;
    using Server.Contract.Servers;

    class Program
    {
        class ManagedInner : AggregationTestingClass
        {
        }

        static void ValidateNativeOuter()
        {
            var managedInner = new ManagedInner();
            var nativeOuter = (AggregationTesting)managedInner;

            Assert.IsTrue(nativeOuter.IsAggregated());
            Assert.IsTrue(nativeOuter.AreAggregated(managedInner, nativeOuter));
            Assert.IsFalse(nativeOuter.AreAggregated(nativeOuter, new object()));
        }

        static int Main(string[] doNotUse)
        {
            // RegFree COM is not supported on Windows Nano
            if (Utilities.IsWindowsNanoServer)
            {
                return 100;
            }

            try
            {
                ValidateNativeOuter();
            }
            catch (Exception e)
            {
                Console.WriteLine($"Test Failure: {e}");
                return 101;
            }

            return 100;
        }
    }
}
