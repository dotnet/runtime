// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using Xunit;

namespace System.Text.Json.Tests
{
    public static partial class JsonObjectTests
    {
        [Fact]
        public static void TestDefaultConstructor()
        {
            var jsonObject = new JsonObject();
            Assert.Equal(0, jsonObject.PropertyNames.Count);
            Assert.Equal(0, jsonObject.PropertyValues.Count);
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

        private static void TestDuplicates(DuplicatePropertyNameHandling duplicatePropertyNameHandling, string previousValue, string newValue, string expectedValue, bool useDefaultCtor = false)
        {
            JsonObject jsonObject = useDefaultCtor ? new JsonObject() : new JsonObject(duplicatePropertyNameHandling);
            jsonObject.Add("property", new JsonString(previousValue));

            Assert.Equal(previousValue, ((JsonString)jsonObject["property"]).Value);

            jsonObject.Add("property", new JsonString(newValue));

            Assert.Equal(expectedValue, ((JsonString) jsonObject["property"]).Value);

            // with indexer, property should change no matter which duplicates handling option is chosen:
            jsonObject["property"] = (JsonString)"indexer value";
            Assert.Equal("indexer value", (JsonString)jsonObject["property"]);
        }


        [Theory]
        [InlineData(DuplicatePropertyNameHandling.Replace, "value1", "value2", "value2")]
        [InlineData(DuplicatePropertyNameHandling.Replace, "value1", "value2", "value2", true)]
        [InlineData(DuplicatePropertyNameHandling.Ignore, "value1", "value2", "value1")]
        public static void TestDuplicatesReplaceAndIgnore(DuplicatePropertyNameHandling duplicatePropertyNameHandling, string previousValue, string newValue, string expectedValue, bool useDefaultCtor = false)
        {
            TestDuplicates(duplicatePropertyNameHandling, previousValue, newValue, expectedValue, useDefaultCtor);
        }

        [Fact]
        public static void TestDuplicatesError()
        {
            Assert.Throws<ArgumentException>(() => TestDuplicates(DuplicatePropertyNameHandling.Error, "", "", ""));

            JsonObject jsonObject = new JsonObject(DuplicatePropertyNameHandling.Error) { { "property", "" } };
            jsonObject["property"] = (JsonString) "indexer value";
            Assert.Equal("indexer value", (JsonString) jsonObject["property"]);
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
        public static void TestReadonlySpan()
        {
            var jsonObject = new JsonObject();
            
            var spanValue = new ReadOnlySpan<char>(new char[] { 's', 'p', 'a', 'n' });
            jsonObject.Add("span", spanValue);
            Assert.Equal("span", (JsonString)jsonObject["span"]);

            string property = null;
            spanValue = property.AsSpan();
            jsonObject.Add("span", spanValue);
            Assert.Equal("", (JsonString)jsonObject["span"]);
        }

        [Fact]
        public static void TestGuid()
        {
            var guidString = "ca761232-ed42-11ce-bacd-00aa0057b223";
            Guid guid = new Guid(guidString);
            var jsonObject = new JsonObject{ { "guid", guid } };
            Assert.Equal(guidString, (JsonString)jsonObject["guid"]);
        }

        [Fact]
        public static void TestDateTime()
        {
            DateTime dateTime = new DateTime(DateTime.MinValue.Ticks);
            var jsonObject = new JsonObject { { "dateTime", dateTime } };
            Assert.Equal(dateTime.ToString(), (JsonString)jsonObject["dateTime"]);
        }

        [Fact]
        public static void TestDateTimeOffset()
        {
            DateTimeOffset dateTimeOffset = new DateTime(DateTime.MinValue.Ticks, DateTimeKind.Utc);
            var jsonObject = new JsonObject { { "dateTimeOffset", dateTimeOffset } };
            Assert.Equal(dateTimeOffset.ToString(), (JsonString)jsonObject["dateTimeOffset"]);
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
            Assert.Null(developer["null property"]);
        }

        [Fact]
        public static void TestCreatingJsonObjectNewMethods()
        {
            var developer = new JsonObject
            {
                { "name", new JsonString("Kasia") },
                { "age", new JsonNumber(22) },
                { "is developer", new JsonBoolean(true) }
            };

            Assert.Equal("Kasia", ((JsonString)developer["name"]).Value);
            Assert.Equal(22, ((JsonNumber)developer["age"]).GetInt32());
            Assert.True(((JsonBoolean)developer["is developer"]).Value);
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
        public static void TestContains()
        {
            var person = new JsonObject
            {
                { "name", "John" },
                { "ssn", "123456789" },
            };

            Assert.True(person.ContainsProperty("ssn"));
            Assert.Equal("123456789", (JsonString)person["ssn"]);
            Assert.False(person.ContainsProperty("surname"));
        }

        [Fact]
        public static void TestAquiringAllValues()
        {
            var employees = new JsonObject(EmployeesDatabase.GetTenBestEmployees());
            ICollection<JsonNode> employeesWithoutId = employees.PropertyValues;

            Assert.Equal(10, employees.PropertyNames.Count);
            Assert.Equal(10, employees.PropertyValues.Count);

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
            Assert.Equal("Bob", (JsonString)person1["name"]);

            person1["age"] = new JsonNumber(55);
            Assert.Equal(55, (JsonNumber)person1["age"]);

            person1["is_married"] = new JsonBoolean(false);
            Assert.Equal(false, (JsonBoolean)person1["is_married"]);

            var person2 = new JsonObject
            {
                { "name", "Bob" },
                { "age", 33 },
                { "is_married", true }
            };

            // Copy property from another JsonObject
            person1["age"] = person2["age"];

            Assert.Equal(33, (JsonNumber) person1["age"]);

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
        public static void TestTryGetProperty ()
        {
            var jsonObject = new JsonObject()
            {
                { "name", "value" }
            };

            Assert.True(jsonObject.TryGetPropertyValue("name", out JsonNode property));
            Assert.Equal("value", (JsonString)property);
            Assert.False(jsonObject.TryGetPropertyValue("other", out property));
            Assert.Null(property);
        }

        [Fact]
        public static void TestGetJsonObjectPropertyThrows()
        {
            Assert.Throws<InvalidCastException>(() =>
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
        public static void TestDuplicatesEnumOutOfRange()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new JsonObject((DuplicatePropertyNameHandling)123));
            Assert.Throws<ArgumentOutOfRangeException>(() => new JsonObject((DuplicatePropertyNameHandling)(-1)));
            Assert.Throws<ArgumentOutOfRangeException>(() => new JsonObject((DuplicatePropertyNameHandling)3));
        }
    }
}
