// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Reflection;
using Xunit;

namespace System.Data.Odbc.Tests
{
    public class SQLLENTests
    {
        [Fact]
        public void SQLLEN_ImplicitConversion_HandlesMinusOne_64Bit()
        {
            // This test verifies that SQLLEN can properly handle the value 0x00000000FFFFFFFF
            // which represents -1 in 32-bit but 4294967295 in 64-bit representation.
            // This scenario occurs with some ODBC drivers (e.g., Filemaker Pro) that return
            // -1 as a 32-bit value in a 64-bit SQLLEN field.

            // Skip this test on 32-bit platforms as the issue only occurs on 64-bit
            if (IntPtr.Size != 8)
            {
                return;
            }

            // Load the SQLLEN type using reflection since it's internal
            Type sqllenType = typeof(OdbcConnection).Assembly.GetType("System.Data.Odbc.SQLLEN");
            Assert.NotNull(sqllenType);

            // Create a SQLLEN instance with IntPtr constructor
            // This simulates what happens when ODBC returns 0x00000000FFFFFFFF
            ConstructorInfo intPtrConstructor = sqllenType.GetConstructor(
                BindingFlags.Instance | BindingFlags.NonPublic,
                null,
                new[] { typeof(IntPtr) },
                null);
            Assert.NotNull(intPtrConstructor);

            // Create IntPtr with value 0x00000000FFFFFFFF (4294967295 as unsigned, -1 as signed 32-bit)
            IntPtr problematicValue = new IntPtr(0x00000000FFFFFFFF);
            object sqllenInstance = intPtrConstructor.Invoke(new object[] { problematicValue });

            // Get the implicit conversion operator to int
            MethodInfo implicitToInt = sqllenType.GetMethod(
                "op_Implicit",
                BindingFlags.Public | BindingFlags.Static,
                null,
                new[] { sqllenType },
                null);
            Assert.NotNull(implicitToInt);
            Assert.Equal(typeof(int), implicitToInt.ReturnType);

            // This should not throw OverflowException and should return -1
            int result = (int)implicitToInt.Invoke(null, new[] { sqllenInstance });
            Assert.Equal(-1, result);
        }

        [Fact]
        public void SQLLEN_ImplicitConversion_HandlesNormalValues()
        {
            // Verify that normal values still work correctly
            Type sqllenType = typeof(OdbcConnection).Assembly.GetType("System.Data.Odbc.SQLLEN");
            Assert.NotNull(sqllenType);

            // Test with value 100
            ConstructorInfo intConstructor = sqllenType.GetConstructor(
                BindingFlags.Instance | BindingFlags.NonPublic,
                null,
                new[] { typeof(int) },
                null);
            Assert.NotNull(intConstructor);

            object sqllenInstance = intConstructor.Invoke(new object[] { 100 });

            MethodInfo implicitToInt = sqllenType.GetMethod(
                "op_Implicit",
                BindingFlags.Public | BindingFlags.Static,
                null,
                new[] { sqllenType },
                null);
            Assert.NotNull(implicitToInt);

            int result = (int)implicitToInt.Invoke(null, new[] { sqllenInstance });
            Assert.Equal(100, result);
        }

        [Fact]
        public void SQLLEN_ImplicitConversion_HandlesZero()
        {
            Type sqllenType = typeof(OdbcConnection).Assembly.GetType("System.Data.Odbc.SQLLEN");
            Assert.NotNull(sqllenType);

            ConstructorInfo intConstructor = sqllenType.GetConstructor(
                BindingFlags.Instance | BindingFlags.NonPublic,
                null,
                new[] { typeof(int) },
                null);
            Assert.NotNull(intConstructor);

            object sqllenInstance = intConstructor.Invoke(new object[] { 0 });

            MethodInfo implicitToInt = sqllenType.GetMethod(
                "op_Implicit",
                BindingFlags.Public | BindingFlags.Static,
                null,
                new[] { sqllenType },
                null);
            Assert.NotNull(implicitToInt);

            int result = (int)implicitToInt.Invoke(null, new[] { sqllenInstance });
            Assert.Equal(0, result);
        }

        [Fact]
        public void SQLLEN_ImplicitConversion_HandlesNegativeValues()
        {
            Type sqllenType = typeof(OdbcConnection).Assembly.GetType("System.Data.Odbc.SQLLEN");
            Assert.NotNull(sqllenType);

            ConstructorInfo intConstructor = sqllenType.GetConstructor(
                BindingFlags.Instance | BindingFlags.NonPublic,
                null,
                new[] { typeof(int) },
                null);
            Assert.NotNull(intConstructor);

            object sqllenInstance = intConstructor.Invoke(new object[] { -42 });

            MethodInfo implicitToInt = sqllenType.GetMethod(
                "op_Implicit",
                BindingFlags.Public | BindingFlags.Static,
                null,
                new[] { sqllenType },
                null);
            Assert.NotNull(implicitToInt);

            int result = (int)implicitToInt.Invoke(null, new[] { sqllenInstance });
            Assert.Equal(-42, result);
        }
    }
}
