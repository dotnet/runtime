// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Data;
using System.Data.SqlClient;
using System.Data.SqlTypes;
using System.Diagnostics;

namespace System.Data.SqlClient.ManualTesting.Tests
{
    public static class OutputParameter
    {
        public static void Run(string connectionString)
        {
            Console.WriteLine("Starting 'OutputParameter' tests");
            InvalidValueInOutParam(connectionString);
        }

        // Changing the value of an output parameter to a value of different type throws System.FormatException
        // You should be able to set an Output SqlParameter to an invalid value (e.g. a string in a decimal param) since we clear its value before starting
        private static void InvalidValueInOutParam(string connectionString)
        {
            Console.WriteLine("Test setting output SqlParameter to an invalid value");

            using (var connection = new SqlConnection(connectionString))
            {
                connection.Open();

                // Command simply set the outparam
                using (var command = new SqlCommand("SET @decimal = 1.23", connection))
                {

                    // Create valid param
                    var decimalParam = new SqlParameter("decimal", new decimal(2.34)) { SqlDbType = SqlDbType.Decimal, Direction = ParameterDirection.Output, Scale = 2, Precision = 5 };
                    command.Parameters.Add(decimalParam);
                    // Set value of param to invalid value
                    decimalParam.Value = "Not a decimal";

                    // Execute
                    command.ExecuteNonQuery();
                    // Validate
                    if (((decimal)decimalParam.Value) != new decimal(1.23))
                    {
                        Console.WriteLine("FAIL: Value is incorrect: {0}", decimalParam.Value);
                    }
                }
            }

            Console.WriteLine("Done");
        }
    }
}