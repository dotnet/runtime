// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Xunit;
namespace NetClient
{
    using System;
    using System.Runtime.InteropServices;

    using TestLibrary;
    using Xunit;
    using Server.Contract;
    using Server.Contract.Servers;

    public class Program
    {
        class ManagedInner : AggregationTestingClass
        {
        }

        static void ValidateNativeOuter()
        {
            var managedInner = new ManagedInner();
            var nativeOuter = (AggregationTesting)managedInner;

            Assert.True(typeof(ManagedInner).IsCOMObject);
            Assert.True(typeof(AggregationTestingClass).IsCOMObject);
            Assert.False(typeof(AggregationTesting).IsCOMObject);
            Assert.True(Marshal.IsComObject(managedInner));
            Assert.True(Marshal.IsComObject(nativeOuter));

            Assert.True(nativeOuter.IsAggregated());
            Assert.True(nativeOuter.AreAggregated(managedInner, nativeOuter));
            Assert.False(nativeOuter.AreAggregated(nativeOuter, new object()));
        }

        [Fact]
        public static int TestEntryPoint()
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
