// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Xunit;

namespace System.Text.Json.Tests
{
    public static class JsonObjectTests
    {
        [Fact]
        public static void TestDefaultConstructor()
        {
            var jsonObject = new JsonObject();
            Assert.Equal(0, jsonObject.GetPropertyNames().Count);
            Assert.Equal(0, jsonObject.GetPropertyValues().Count);
        }

        [Fact]
        public static void TestIEnumerableKVPConstructor()
        {
            var jsonProperties = new List<KeyValuePair<string, JsonNode>>();
            jsonProperties.Add(new KeyValuePair<string, JsonNode>("number", new JsonNumber(17)));
            jsonProperties.Add(new KeyValuePair<string, JsonNode>("string", new JsonString("property value")));
            jsonProperties.Add(new KeyValuePair<string, JsonNode>("boolean", new JsonBoolean(true)));

            var jsonObject = new JsonObject(jsonProperties);

            Assert.Equal(17, ((JsonNumber)jsonObject["number"]).GetInt32());
            Assert.Equal("property value", ((JsonString)jsonObject["string"]).Value);
            Assert.True(((JsonBoolean)jsonObject["boolean"]).Value);
        }

        [Fact]
        public static void TestDuplicates()
        {
            Assert.Throws<ArgumentException>(() =>
            {
                JsonObject jsonObject = new JsonObject();
                jsonObject.Add("property", "value1");
                jsonObject.Add("property", "value2");
            });

            JsonObject jsonObject = new JsonObject() { { "property", "value" } };
            Assert.Equal("value", jsonObject["property"]);

            jsonObject["property"] = "indexer value";
            Assert.Equal("indexer value", jsonObject["property"]);
        }

        [Fact]
        public static void TestNumerics()
        {
            var jsonObject = new JsonObject();

            jsonObject.Add("byte", byte.MaxValue);
            Assert.Equal(byte.MaxValue, ((JsonNumber)jsonObject["byte"]).GetByte());

            jsonObject.Add("short", short.MaxValue);
            Assert.Equal(short.MaxValue, ((JsonNumber)jsonObject["short"]).GetInt16());

            jsonObject.Add("int", int.MaxValue);
            Assert.Equal(int.MaxValue, ((JsonNumber)jsonObject["int"]).GetInt32());

            jsonObject.Add("long", long.MaxValue);
            Assert.Equal(long.MaxValue, ((JsonNumber)jsonObject["long"]).GetInt64());

            jsonObject.Add("float", 3.14f);
            Assert.Equal(3.14f, ((JsonNumber)jsonObject["float"]).GetSingle());

            jsonObject.Add("double", 3.14);
            Assert.Equal(3.14, ((JsonNumber)jsonObject["double"]).GetDouble());

            jsonObject.Add("sbyte", sbyte.MaxValue);
            Assert.Equal(sbyte.MaxValue, ((JsonNumber)jsonObject["sbyte"]).GetSByte());

            jsonObject.Add("ushort", ushort.MaxValue);
            Assert.Equal(ushort.MaxValue, ((JsonNumber)jsonObject["ushort"]).GetUInt16());

            jsonObject.Add("uint", uint.MaxValue);
            Assert.Equal(uint.MaxValue, ((JsonNumber)jsonObject["uint"]).GetUInt32());

            jsonObject.Add("ulong", ulong.MaxValue);
            Assert.Equal(ulong.MaxValue, ((JsonNumber)jsonObject["ulong"]).GetUInt64());

            jsonObject.Add("decimal", decimal.One);
            Assert.Equal(decimal.One, ((JsonNumber)jsonObject["decimal"]).GetDecimal());
        }

        [Fact]
        public static void TestGuid()
        {
            var guidString = "ca761232-ed42-11ce-bacd-00aa0057b223";
            Guid guid = new Guid(guidString);
            var jsonObject = new JsonObject { { "guid", guid } };
            Assert.Equal(guidString, ((JsonString)jsonObject["guid"]).Value);
        }

        public static IEnumerable<object[]> DateTimeData =>
            new List<object[]>
            {
                new object[] { new DateTime(DateTime.MinValue.Ticks, DateTimeKind.Utc) },
                new object[] { new DateTime(2019, 1, 1) },
                new object[] { new DateTime(2019, 1, 1, new GregorianCalendar()) },
                new object[] { new DateTime(2019, 1, 1, new ChineseLunisolarCalendar()) }
            };

        [Theory]
        [MemberData(nameof(DateTimeData))]
        public static void TestDateTime(DateTime dateTime)
        {
            var jsonObject = new JsonObject { { "dateTime", dateTime } };
            Assert.Equal(dateTime.ToString("s", CultureInfo.InvariantCulture), ((JsonString)jsonObject["dateTime"]).Value);
        }

        [Theory]
        [MemberData(nameof(DateTimeData))]
        public static void TestDateTimeOffset(DateTimeOffset dateTimeOffset)
        {
            var jsonObject = new JsonObject { { "dateTimeOffset", dateTimeOffset } };
            Assert.Equal(dateTimeOffset.ToString("s", CultureInfo.InvariantCulture), ((JsonString)jsonObject["dateTimeOffset"]).Value);
        }

        [Fact]
        public static void TestCreatingJsonObject()
        {
            var developer = new JsonObject
            {
                { "name", "Kasia" },
                { "n\\u0061me", "Kasia" }, // different property name than above one
                { "age", 22 },
                { "is developer", true },
                { "null property", (JsonNode) null }
            };

            Assert.Equal("Kasia", ((JsonString)developer["name"]).Value);
            Assert.Equal("Kasia", ((JsonString)developer["n\\u0061me"]).Value);
            Assert.Equal(22, ((JsonNumber)developer["age"]).GetInt32());
            Assert.True(((JsonBoolean)developer["is developer"]).Value);
            Assert.IsType<JsonNull>(developer["null property"]);
        }

        [Fact]
        public static void TestCreatingJsonObjectNewMethods()
        {
            var developer = new JsonObject
            {
                { "name", new JsonString("Kasia") },
                { "age", new JsonNumber(22) },
                { "is developer", new JsonBoolean(true) },
                { "null property", new JsonNull() }
            };

            Assert.Equal("Kasia", ((JsonString)developer["name"]).Value);
            Assert.Equal(22, ((JsonNumber)developer["age"]).GetInt32());
            Assert.True(((JsonBoolean)developer["is developer"]).Value);
            Assert.IsType<JsonNull>(developer["null property"]);
        }

        [Fact]
        public static void TestCreatingJsonObjectDictionaryInitializerSyntax()
        {
            var developer = new JsonObject
            {
                ["name"] = "Kasia",
                ["age"] = 22,
                ["is developer"] = true,
                ["null property"] = null
            };

            Assert.Equal("Kasia", ((JsonString)developer["name"]).Value);
            Assert.Equal(22, ((JsonNumber)developer["age"]).GetInt32());
            Assert.True(((JsonBoolean)developer["is developer"]).Value);
            Assert.IsType<JsonNull>(developer["null property"]);
        }

        [Fact]
        public static void TestCreatingNestedJsonObject()
        {
            var person = new JsonObject
            {
                { "name", "John" },
                { "surname", "Smith" },
                {
                    "phone numbers", new JsonObject()
                    {
                        { "work", "123-456-7890" },
                        { "home", "123-456-7890" }
                    }
                },
                {
                    "addresses", new JsonObject()
                    {
                        {
                            "office", new JsonObject()
                            {
                                {  "address line 1", "One Microsoft Way" },
                                {  "city" , "Redmond" } ,
                                {  "zip code" , 98052 } ,
                                {  "state" , (int) AvailableStateCodes.WA }
                            }
                        },
                        {
                            "home", new JsonObject()
                            {
                                {  "address line 1", "Pear Ave" },
                                {  "address line 2", "1288" },
                                {  "city" , "Mountain View" } ,
                                {  "zip code" , 94043 } ,
                                {  "state" , (int) AvailableStateCodes.CA }
                            }
                        }
                    }
                }
            };

            Assert.IsType<JsonObject>(person["phone numbers"]);
            var phoneNumbers = person["phone numbers"] as JsonObject;
            Assert.IsType<JsonString>(phoneNumbers["work"]);
            Assert.IsType<JsonString>(phoneNumbers["home"]);

            Assert.IsType<JsonObject>(person["addresses"]);
            var addresses = person["addresses"] as JsonObject;
            Assert.IsType<JsonObject>(addresses["office"]);
            Assert.IsType<JsonObject>(addresses["home"]);
        }

        [Fact]
        public static void TestAssignmentDefinition()
        {
            JsonNode employee = EmployeesDatabase.GetNextEmployee().Value;
            Assert.IsType<JsonObject>(employee);
        }

        private static void CheckEmployeesAreDifferent(JsonObject employees)
        {
            string prevId = "";
            foreach (KeyValuePair<string, JsonNode> employee in employees)
            {
                Assert.NotEqual(prevId, employee.Key);
                prevId = employee.Key;

                Assert.IsType<JsonObject>(employee.Value);
            }
        }

        [Fact]
        public static void TestAddingKeyValuePair()
        {
            var employees = new JsonObject
            {
                EmployeesDatabase.GetNextEmployee(),
                EmployeesDatabase.GetNextEmployee(),
                EmployeesDatabase.GetNextEmployee(),
                EmployeesDatabase.GetNextEmployee(),
            };

            CheckEmployeesAreDifferent(employees);
        }

        [Fact]
        public static void TestAddingKeyValuePairAfterInitialization()
        {
            var employees = new JsonObject();
            foreach (KeyValuePair<string, JsonNode> employee in EmployeesDatabase.GetTenBestEmployees())
            {
                employees.Add(employee);
            }

            CheckEmployeesAreDifferent(employees);
        }

        [Fact]
        public static void TestAddingKeyValuePairsCollection()
        {
            var employees = new JsonObject(EmployeesDatabase.GetTenBestEmployees());

            CheckEmployeesAreDifferent(employees);
        }

        [Fact]
        public static void TestAddingKeyValuePairsCollectionAfterInitialization()
        {
            var employees = new JsonObject();
            employees.AddRange(EmployeesDatabase.GetTenBestEmployees());

            CheckEmployeesAreDifferent(employees);
        }

        [Fact]
        public static void TestAddingJsonArray()
        {
            var preferences = new JsonObject()
            {
                { "prime numbers", new JsonArray { 19, 37 } }
            };

            var primeNumbers = (JsonArray)preferences["prime numbers"];
            Assert.Equal(2, primeNumbers.Count);

            int[] expected = { 19, 37 };

            for (int i = 0; i < primeNumbers.Count; i++)
            {
                Assert.Equal(expected[i], ((JsonNumber)primeNumbers[i]).GetInt32());
            }
        }

        [Fact]
        public static void TestGetJsonArrayPropertyValue()
        {
            var jsonObject = new JsonObject()
            {
                { "array", new JsonArray() { 1, 2 } }
            };

            JsonArray jsonArray = jsonObject.GetJsonArrayPropertyValue("array");
            Assert.Equal(2, jsonArray.Count);
            Assert.Equal(1, jsonArray[0]);
            Assert.Equal(2, jsonArray[1]);
        }

        [Fact]
        public static void TestAddingNull()
        {
            var jsonObject = new JsonObject
            {
                { "null1", null },
                { "null2", (JsonNode)null },
                { "null3", (JsonNull)null },
                { "null4", new JsonNull() },
                { "null5", (string)null },
            };

            Assert.IsType<JsonNull>(jsonObject["null1"]);
            Assert.IsType<JsonNull>(jsonObject["null2"]);
            Assert.IsType<JsonNull>(jsonObject["null3"]);
            Assert.IsType<JsonNull>(jsonObject["null4"]);
            Assert.IsType<JsonNull>(jsonObject["null5"]);

            jsonObject["null1"] = null;
            Assert.IsType<JsonNull>(jsonObject["null1"]);
        }

        [Fact]
        public static void TestContains()
        {
            var person = new JsonObject
            {
                { "name", "John" },
                { "ssn", "123456789" },
            };

            Assert.True(person.ContainsProperty("ssn"));
            Assert.Equal("123456789", ((JsonString)person["ssn"]).Value);
            Assert.False(person.ContainsProperty("surname"));
        }

        [Fact]
        public static void TestAquiringAllValues()
        {
            var employees = new JsonObject(EmployeesDatabase.GetTenBestEmployees());
            IReadOnlyCollection<JsonNode> employeesWithoutId = employees.GetPropertyValues();

            Assert.Equal(10, employees.GetPropertyNames().Count);
            Assert.Equal(10, employees.GetPropertyValues().Count);

            foreach (JsonNode employee in employeesWithoutId)
            {
                Assert.IsType<JsonObject>(employee);
            }
        }

        [Fact]
        public static void TestReplacingsonObjectPrimaryTypes()
        {
            var person1 = new JsonObject
            {
                { "name", "John" },
                { "age", 45 },
                { "is_married", true }
            };


            person1["name"] = new JsonString("Bob");
            Assert.Equal("Bob", ((JsonString)person1["name"]).Value);

            person1["age"] = new JsonNumber(55);
            Assert.Equal(55, ((JsonNumber)person1["age"]).GetInt32());

            person1["is_married"] = new JsonBoolean(false);
            Assert.False(((JsonBoolean)person1["is_married"]).Value);

            var person2 = new JsonObject
            {
                { "name", "Bob" },
                { "age", 33 },
                { "is_married", true }
            };

            // Copy property from another JsonObject
            person1["age"] = person2["age"];

            Assert.Equal(33, ((JsonNumber)person1["age"]).GetInt32());

            // Copy property of different typoe
            person1["name"] = person2["name"];
        }

        [Fact]
        public static void TestModifyingJsonObjectKeyRemoveAdd()
        {
            JsonObject manager = EmployeesDatabase.GetManager();
            JsonObject reportingEmployees = manager.GetJsonObjectPropertyValue("reporting employees");

            static void ModifyProperty(JsonObject jsonObject, string previousName, string newName)
            {
                JsonNode previousValue = jsonObject[previousName];
                jsonObject.Remove(previousName);
                jsonObject.Add(newName, previousValue);
            }

            string previousName = "software developers";
            string newName = "software engineers";

            Assert.True(reportingEmployees.ContainsProperty(previousName));
            JsonNode previousValue = reportingEmployees[previousName];

            ModifyProperty(reportingEmployees, previousName, newName);

            Assert.False(reportingEmployees.ContainsProperty(previousName));
            Assert.True(reportingEmployees.ContainsProperty(newName));
            Assert.Equal(previousValue, reportingEmployees[newName]);
        }

        [Fact]
        public static void TestAccessingNestedJsonObjectCastWithAs()
        {
            JsonObject manager = EmployeesDatabase.GetManager();

            var reportingEmployees = manager["reporting employees"] as JsonObject;
            Assert.NotNull(reportingEmployees);

            var softwareDevelopers = reportingEmployees["software developers"] as JsonObject;
            Assert.NotNull(softwareDevelopers);

            var internDevelopers = softwareDevelopers["intern employees"] as JsonObject;
            Assert.NotNull(internDevelopers);

            internDevelopers.Add(EmployeesDatabase.GetNextEmployee());
        }

        [Fact]
        public static void TestAccessingNestedJsonObjectCastWithIs()
        {
            JsonObject manager = EmployeesDatabase.GetManager();

            static bool AddEmployee(JsonObject manager)
            {

                if (manager["reporting employees"] is JsonObject reportingEmployees)
                {
                    if (reportingEmployees["software developers"] is JsonObject softwareDevelopers)
                    {
                        if (softwareDevelopers["full time employees"] is JsonObject fullTimeEmployees)
                        {
                            fullTimeEmployees.Add(EmployeesDatabase.GetNextEmployee());
                            return true;
                        }
                    }
                }
                return false;
            }

            bool success = AddEmployee(manager);
            Assert.True(success);
        }

        [Fact]
        public static void TestAccessingNestedJsonObjectExplicitCast()
        {
            JsonObject manager = EmployeesDatabase.GetManager();

            // Should not throw any exceptions:
            ((JsonObject)((JsonObject)manager["reporting employees"])["HR"]).Add(EmployeesDatabase.GetNextEmployee());
        }

        [Fact]
        public static void TestAccessingNestedJsonObjectGetPropertyMethod()
        {
            JsonObject manager = EmployeesDatabase.GetManager();

            // Should not throw any exceptions:

            JsonObject internDevelopers = manager.GetJsonObjectPropertyValue("reporting employees")
                                          .GetJsonObjectPropertyValue("software developers")
                                          .GetJsonObjectPropertyValue("intern employees");
            internDevelopers.Add(EmployeesDatabase.GetNextEmployee());
        }

        [Fact]
        public static void TestAccessingNestedJsonObjectTryGetPropertyMethod()
        {
            JsonObject manager = EmployeesDatabase.GetManager();

            static bool AddEmployee(JsonObject manager)
            {
                if (manager.TryGetJsonObjectPropertyValue("reporting employees", out JsonObject reportingEmployees))
                {
                    if (reportingEmployees.TryGetJsonObjectPropertyValue("software developers", out JsonObject softwareDevelopers))
                    {
                        if (softwareDevelopers.TryGetJsonObjectPropertyValue("full time employees", out JsonObject fullTimeEmployees))
                        {
                            fullTimeEmployees.Add(EmployeesDatabase.GetNextEmployee());
                            return true;
                        }
                    }
                }

                return false;
            }

            bool success = AddEmployee(manager);
            Assert.True(success);
        }

        [Fact]
        public static void TestGetPropertyThrows()
        {
            Assert.Throws<KeyNotFoundException>(() =>
            {
                var jsonObject = new JsonObject()
                {
                    { "name", "value" }
                };

                jsonObject.GetPropertyValue("different name");
            });
        }

        [Fact]
        public static void TestTryGetProperty()
        {
            var jsonObject = new JsonObject()
            {
                { "name", "value" }
            };

            Assert.True(jsonObject.TryGetPropertyValue("name", out JsonNode property));
            Assert.Equal("value", ((JsonString)property).Value);
            Assert.False(jsonObject.TryGetPropertyValue("other", out property));
            Assert.Null(property);
        }

        [Fact]
        public static void TestGetJsonObjectPropertyThrows()
        {
            Assert.Throws<ArgumentException>(() =>
            {
                var jsonObject = new JsonObject()
                {
                    { "name", "value" }
                };

                jsonObject.GetJsonObjectPropertyValue("name");
            });
        }

        [Fact]
        public static void TestTryGetObjectPropertyFails()
        {
            var jsonObject = new JsonObject()
            {
                { "name", "value" }
            };

            Assert.False(jsonObject.TryGetJsonObjectPropertyValue("name", out JsonObject property));
            Assert.Null(property);

            Assert.False(jsonObject.TryGetJsonObjectPropertyValue("other", out property));
            Assert.Null(property);
        }

        [Fact]
        public static void TestGetJsonArrayPropertyThrows()
        {
            Assert.Throws<ArgumentException>(() =>
            {
                var jsonObject = new JsonObject()
                {
                    { "name", "value" }
                };

                jsonObject.GetJsonArrayPropertyValue("name");
            });
        }

        [Fact]
        public static void TestTryGetArrayPropertyFails()
        {
            var jsonObject = new JsonObject()
            {
                { "name", "value" }
            };

            Assert.False(jsonObject.TryGetJsonArrayPropertyValue("name", out JsonArray property));
            Assert.Null(property);

            Assert.False(jsonObject.TryGetJsonArrayPropertyValue("other", out property));
            Assert.Null(property);
        }

        [Fact]
        public static void TestArgumentNullValidation()
        {
            Assert.Throws<ArgumentNullException>(() => new JsonObject().Add(null, ""));
            Assert.Throws<ArgumentNullException>(() => new JsonObject().Add(null, new JsonObject()));
            Assert.Throws<ArgumentNullException>(() => new JsonObject().Add(null, (byte)17));
            Assert.Throws<ArgumentNullException>(() => new JsonObject().Add(null, (short)17));
            Assert.Throws<ArgumentNullException>(() => new JsonObject().Add(null, 17));
            Assert.Throws<ArgumentNullException>(() => new JsonObject().Add(null, (long)17));
            Assert.Throws<ArgumentNullException>(() => new JsonObject().Add(null, 3.14f));
            Assert.Throws<ArgumentNullException>(() => new JsonObject().Add(null, 3.14));
            Assert.Throws<ArgumentNullException>(() => new JsonObject().Add(null, decimal.One));
            Assert.Throws<ArgumentNullException>(() => new JsonObject().Add(null, (sbyte)17));
            Assert.Throws<ArgumentNullException>(() => new JsonObject().Add(null, (ushort)17));
            Assert.Throws<ArgumentNullException>(() => new JsonObject().Add(null, (uint)17));
            Assert.Throws<ArgumentNullException>(() => new JsonObject().Add(null, (ulong)17));
            Assert.Throws<ArgumentNullException>(() => new JsonObject().Add(new KeyValuePair<string, JsonNode>(null, new JsonObject())));
            Assert.Throws<ArgumentNullException>(() =>
            {
                var property = new KeyValuePair<string, JsonNode>(null, new JsonObject());
                new JsonObject().AddRange(new List<KeyValuePair<string, JsonNode>>() { property });
            });
            Assert.Throws<ArgumentNullException>(() =>
            {
                var property1 = new KeyValuePair<string, JsonNode>("regular property", new JsonObject());
                var property2 = new KeyValuePair<string, JsonNode>("regular property2", new JsonObject());
                var nullProperty = new KeyValuePair<string, JsonNode>(null, new JsonObject());
                var propertyList = new List<KeyValuePair<string, JsonNode>>() { property1, nullProperty, property2 };
                var jsonObject = new JsonObject(propertyList);
            });
            Assert.Throws<ArgumentNullException>(() => new JsonObject()[null] = new JsonString());
            Assert.Throws<ArgumentNullException>(() =>
            {
                var jsonObject = new JsonObject();
                JsonNode x = jsonObject[null];
            });
            Assert.Throws<ArgumentNullException>(() => new JsonObject().Remove(null));
            Assert.Throws<ArgumentNullException>(() => new JsonObject().ContainsProperty(null));
        }

        [Fact]
        public static void TestStringComparisonEnum()
        {
            var jsonObject = new JsonObject()
            {
                { "not encyclopaedia", "value1" },
                { "Encyclopaedia", "value2" },
                { "NOT encyclopaedia", "value3" },
                { "encyclopaedia", "value4" }
            };

            Assert.Equal(4, jsonObject.Count());

            Assert.False(jsonObject.ContainsProperty("ENCYCLOPAEDIA"));
            Assert.False(jsonObject.TryGetPropertyValue("ENCYCLOPAEDIA", out JsonNode jsonNode));
            Assert.Null(jsonNode);
            Assert.Throws<KeyNotFoundException>(() => jsonObject.GetPropertyValue("ENCYCLOPAEDIA"));
            jsonObject.Remove("ENCYCLOPAEDIA");
            Assert.Equal(4, jsonObject.Count());

            Assert.False(jsonObject.ContainsProperty("ENCYCLOPAEDIA", StringComparison.CurrentCulture));
            Assert.False(jsonObject.TryGetPropertyValue("ENCYCLOPAEDIA", StringComparison.CurrentCulture, out jsonNode));
            Assert.Null(jsonNode);
            Assert.Throws<KeyNotFoundException>(() => jsonObject.GetPropertyValue("ENCYCLOPAEDIA", StringComparison.CurrentCulture));
            jsonObject.Remove("ENCYCLOPAEDIA", StringComparison.CurrentCulture);
            Assert.Equal(4, jsonObject.Count());

            Assert.True(jsonObject.ContainsProperty("ENCYCLOPAEDIA", StringComparison.InvariantCultureIgnoreCase));
            Assert.True(jsonObject.TryGetPropertyValue("ENCYCLOPAEDIA", StringComparison.InvariantCultureIgnoreCase, out jsonNode));
            Assert.Equal("value2", jsonNode);
            Assert.Equal("value2", jsonObject.GetPropertyValue("ENCYCLOPAEDIA", StringComparison.InvariantCultureIgnoreCase));
            jsonObject.Remove("ENCYCLOPAEDIA", StringComparison.InvariantCultureIgnoreCase);
            Assert.Equal(3, jsonObject.Count());

            IReadOnlyCollection<JsonNode> values = jsonObject.GetPropertyValues();
            Assert.False(values.Contains("value2"));
            Assert.True(values.Contains("value1"));
            Assert.True(values.Contains("value3"));
            Assert.True(values.Contains("value4"));

            jsonObject = new JsonObject()
            {
                { "object first", new JsonObject() },
                { "object FIRST", new JsonArray() },
                { "array first", new JsonArray() },
                { "array FIRST", new JsonObject() }
            };

            Assert.Equal(0, jsonObject.GetJsonObjectPropertyValue("OBJECT first",
                StringComparison.InvariantCultureIgnoreCase).GetPropertyNames().Count);
            Assert.True(jsonObject.TryGetJsonObjectPropertyValue("OBJECT first", StringComparison.InvariantCultureIgnoreCase,
                out JsonObject objectProperty));
            Assert.Equal(0, objectProperty.GetPropertyNames().Count);

            Assert.Throws<ArgumentException>(() =>
                jsonObject.GetJsonArrayPropertyValue("OBJECT first", StringComparison.InvariantCultureIgnoreCase));
            Assert.False(jsonObject.TryGetJsonArrayPropertyValue("OBJECT first", StringComparison.InvariantCultureIgnoreCase,
                out JsonArray arrayProperty));
            Assert.False(jsonObject.TryGetJsonArrayPropertyValue("something different", StringComparison.InvariantCultureIgnoreCase,
                out arrayProperty));

            Assert.Equal(0, jsonObject.GetJsonArrayPropertyValue("ARRAY first",
                StringComparison.InvariantCultureIgnoreCase).Count);
            Assert.True(jsonObject.TryGetJsonArrayPropertyValue("ARRAY first", StringComparison.InvariantCultureIgnoreCase,
                out arrayProperty));
            Assert.Equal(0, arrayProperty.Count);

            Assert.Throws<ArgumentException>(() =>
                jsonObject.GetJsonObjectPropertyValue("ARRAY first", StringComparison.InvariantCultureIgnoreCase));
            Assert.False(jsonObject.TryGetJsonObjectPropertyValue("ARRAY first", StringComparison.InvariantCultureIgnoreCase,
                out objectProperty));
            Assert.False(jsonObject.TryGetJsonObjectPropertyValue("something different", StringComparison.InvariantCultureIgnoreCase,
                out objectProperty));

            Assert.Throws<ArgumentNullException>(() =>
               jsonObject.Remove(null, StringComparison.InvariantCultureIgnoreCase));
        }

        [Fact]
        public static void TestValueKind()
        {
            Assert.Equal(JsonValueKind.Object, new JsonObject().ValueKind);
        }

        [Fact]
        public static void TestRemoveLastProperty()
        {
            var jsonObject = new JsonObject()
            {
                { "first", "value1" },
                { "middle", "value2" },
                { "last", "" }
            };

            jsonObject.Remove("last");
            Assert.Equal(2, jsonObject.GetPropertyNames().Count());
            Assert.Equal(2, jsonObject.GetPropertyValues().Count());
            Assert.Equal("value1", jsonObject["first"]);
            Assert.Equal("value2", jsonObject["middle"]);
        }

        [Fact]
        public static void TestJsonObjectIEnumerator()
        {
            var jsonObject = new JsonObject()
            {
                ["first"] = 17,
                ["second"] = "value"
            };

            // Test generic IEnumerator:
            IEnumerator<KeyValuePair<string, JsonNode>> jsonObjectEnumerator = new JsonObjectEnumerator(jsonObject);

            Assert.Null(jsonObjectEnumerator.Current.Key);
            Assert.Null(jsonObjectEnumerator.Current.Value);

            jsonObjectEnumerator.MoveNext();
            Assert.Equal("first", jsonObjectEnumerator.Current.Key);
            Assert.Equal(17, jsonObjectEnumerator.Current.Value);
            jsonObjectEnumerator.MoveNext();
            Assert.Equal("second", jsonObjectEnumerator.Current.Key);
            Assert.Equal("value", jsonObjectEnumerator.Current.Value);

            jsonObjectEnumerator.Reset();

            jsonObjectEnumerator.MoveNext();
            Assert.Equal("first", jsonObjectEnumerator.Current.Key);
            Assert.Equal(17, jsonObjectEnumerator.Current.Value);
            jsonObjectEnumerator.MoveNext();
            Assert.Equal("second", jsonObjectEnumerator.Current.Key);
            Assert.Equal("value", jsonObjectEnumerator.Current.Value);
        }

        [Fact]
        public static void TestJsonObjectEmptyObjectEnumerator()
        {
            var jsonObject = new JsonObject();
            var jsonObjectEnumerator = new JsonObjectEnumerator(jsonObject);

            Assert.Null(jsonObjectEnumerator.Current.Key);
            Assert.Null(jsonObjectEnumerator.Current.Value);
            Assert.False(jsonObjectEnumerator.MoveNext());
        }
    }
}
