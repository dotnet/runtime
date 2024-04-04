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

    struct Struct {}

    public unsafe class Program
    {
        [Fact]
        public static int TestEntryPoint()
        {
            // RegFree COM is not supported on Windows Nano
            if (TestLibrary.Utilities.IsWindowsNanoServer)
            {
                return 100;
            }

            try
            {
                ValidationTests();
                ValidateNegativeTests();
            }
            catch (Exception e)
            {
                Console.WriteLine($"Test object interop failure: {e}");
                return 101;
            }

            return 100;
        }

        private static void ValidationTests()
        {
            Console.WriteLine($"Running {nameof(ValidationTests)} ...");

            var miscTypeTesting = (Server.Contract.Servers.MiscTypesTesting)new Server.Contract.Servers.MiscTypesTestingClass();

            Console.WriteLine("-- Primitives <=> VARIANT...");
            {
                object expected = null;
                Assert.Equal(expected, miscTypeTesting.Marshal_Variant(expected));
            }
            {
                var expected = DBNull.Value;
                Assert.Equal(expected, miscTypeTesting.Marshal_Variant(expected));
            }
            {
                var expected = (sbyte)0x0f;
                Assert.Equal(expected, miscTypeTesting.Marshal_Variant(expected));
            }
            {
                var expected = (short)0x07ff;
                Assert.Equal(expected, miscTypeTesting.Marshal_Variant(expected));
            }
            {
                var expected = (int)0x07ffffff;
                Assert.Equal(expected, miscTypeTesting.Marshal_Variant(expected));
            }
            {
                var expected = (long)0x07ffffffffffffff;
                Assert.Equal(expected, miscTypeTesting.Marshal_Variant(expected));
            }
            {
                var expected = true;
                Assert.Equal(expected, miscTypeTesting.Marshal_Variant(expected));
            }
            {
                var expected = false;
                Assert.Equal(expected, miscTypeTesting.Marshal_Variant(expected));
            }

            Console.WriteLine("-- BSTR <=> VARIANT...");
            {
                var expected = "The quick Fox jumped over the lazy Dog.";
                Assert.Equal(expected, miscTypeTesting.Marshal_Variant(expected));
            }

            Console.WriteLine("-- System.Guid <=> VARIANT...");
            {
                var expected = new Guid("{8EFAD956-B33D-46CB-90F4-45F55BA68A96}");
                Assert.Equal(expected, miscTypeTesting.Marshal_Variant(expected));
            }
        }

        private static void ValidateNegativeTests()
        {
            Console.WriteLine($"Running {nameof(ValidateNegativeTests)} ...");

            var miscTypeTesting = (Server.Contract.Servers.MiscTypesTesting)new Server.Contract.Servers.MiscTypesTestingClass();

            Console.WriteLine("-- User defined ValueType <=> VARIANT...");
            {
                Assert.Throws<NotSupportedException>(() => miscTypeTesting.Marshal_Variant(new Struct()));
            }
        }
    }
}
