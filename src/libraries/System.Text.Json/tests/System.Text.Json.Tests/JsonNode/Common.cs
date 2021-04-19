// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;

namespace System.Text.Json.Node.Tests
{
    public static partial class JsonNodeTests
    {
        internal const string ExpectedDomJson = "{\"MyString\":\"Hello!\",\"MyNull\":null,\"MyBoolean\":false,\"MyArray\":[2,3,42]," +
            "\"MyInt\":43,\"MyDateTime\":\"2020-07-08T00:00:00\",\"MyGuid\":\"ed957609-cdfe-412f-88c1-02daca1b4f51\"," +
            "\"MyObject\":{\"MyString\":\"Hello!!\"},\"Child\":{\"ChildProp\":1}}";

        internal const string Linq_Query_Json = @"
        [
          {
            ""OrderId"":100, ""Customer"":
            {
              ""Name"":""Customer1"",
              ""City"":""Fargo""
            }
          },
          {
            ""OrderId"":200, ""Customer"":
            {
              ""Name"":""Customer2"",
              ""City"":""Redmond""
            }
          },
          {
            ""OrderId"":300, ""Customer"":
            {
              ""Name"":""Customer3"",
              ""City"":""Fargo""
            }
          }
        ]";

        /// <summary>
        /// Helper class simulating external library
        /// </summary>
        internal static class EmployeesDatabase
        {
            private static int s_id = 0;
            public static KeyValuePair<string, JsonNode?> GetNextEmployee()
            {
                var employee = new JsonObject()
                {
                    { "name", "John" } ,
                    { "surname", "Smith"},
                    { "age", 45 }
                };

                return new KeyValuePair<string, JsonNode?>("employee" + s_id++, employee);
            }

            public static IEnumerable<KeyValuePair<string, JsonNode>> GetTenBestEmployees()
            {
                for (int i = 0; i < 10; i++)
                    yield return GetNextEmployee();
            }

            /// <summary>
            /// Returns following JsonObject:
            /// {
            ///     "phone numbers" : { "work" :  "425-555-0123", "home": "425-555-0134"  }
            ///     "reporting employees" : 
            ///     {
            ///         "software developers" :
            ///         {
            ///             "full time employees" : /JsonObject of 3 employees from database/ 
            ///             "intern employees" : /JsonObject of 2 employees from database/ 
            ///         },
            ///         "HR" : /JsonObject of 10 employees from database/ 
            ///     }
            /// }
            /// </summary>
            /// <returns></returns>
            public static JsonObject GetManager()
            {
                var manager = GetNextEmployee().Value as JsonObject;

                manager.Add
                (
                    "phone numbers",
                    new JsonObject()
                    {
                    { "work", "425-555-0123" }, { "home", "425-555-0134" }
                    }
                );

                manager.Add
                (
                    "reporting employees", new JsonObject
                    {
                    {
                        "software developers", new JsonObject
                        {
                            {
                                "full time employees", new JsonObject
                                {
                                    EmployeesDatabase.GetNextEmployee(),
                                    EmployeesDatabase.GetNextEmployee(),
                                    EmployeesDatabase.GetNextEmployee(),
                                }
                            },
                            {
                                "intern employees", new JsonObject
                                {
                                    EmployeesDatabase.GetNextEmployee(),
                                    EmployeesDatabase.GetNextEmployee(),
                                }
                            }
                        }
                    },
                    {
                        "HR", new JsonObject
                        {
                            {
                                "full time employees", new JsonObject(EmployeesDatabase.GetTenBestEmployees())
                            }
                        }
                    }
                    }
                );

                return manager;
            }
        }
    }
}
