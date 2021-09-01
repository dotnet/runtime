// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading.Tasks;
using Xunit;

namespace System.Text.Json.Serialization.Tests
{
    public static partial class ReferenceHandlerTests
    {
        private static readonly JsonSerializerOptions s_deserializerOptionsPreserve = new JsonSerializerOptions { ReferenceHandler = ReferenceHandler.Preserve };

        private class EmployeeWithContacts
        {
            public string Name { get; set; }
            public EmployeeWithContacts Manager { get; set; }
            public List<EmployeeWithContacts> Subordinates { get; set; }
            public Dictionary<string, EmployeeWithContacts> Contacts { get; set; }
        }

        #region Root Object
        [Fact]
        public static void ObjectWithoutMetadata()
        {
            string json = "{}";
            Employee employee = JsonSerializer.Deserialize<Employee>(json, s_deserializerOptionsPreserve);
            Assert.NotNull(employee);
        }

        [Fact] //Employee list as a property and then use reference to itself on nested Employee.
        public static void ObjectReferenceLoop()
        {
            string json =
            @"{
                ""$id"": ""1"",
                ""Name"": ""Angela"",
                ""Manager"": {
                    ""$ref"": ""1""
                }
            }";

            Employee angela = JsonSerializer.Deserialize<Employee>(json, s_deserializerOptionsPreserve);
            Assert.Same(angela, angela.Manager);
        }

        [Fact] // Employee whose subordinates is a preserved list. EmployeeListEmployee
        public static void ObjectReferenceLoopInList()
        {
            string json =
            @"{
                ""$id"": ""1"",
                ""Subordinates"": {
                    ""$id"": ""2"",
                    ""$values"": [
                        {
                            ""$ref"": ""1""
                        }
                    ]
                }
            }";

            Employee employee = JsonSerializer.Deserialize<Employee>(json, s_deserializerOptionsPreserve);
            Assert.Equal(1, employee.Subordinates.Count);
            Assert.Same(employee, employee.Subordinates[0]);
        }

        [Fact] // Employee whose subordinates is a preserved list. EmployeeListEmployee
        public static void ObjectReferenceLoopInDictionary()
        {
            string json =
            @"{
                ""$id"": ""1"",
                ""Contacts"":{
                    ""$id"": ""2"",
                    ""Angela"":{
                        ""$ref"": ""1""
                    }
                }
            }";

            EmployeeWithContacts employee = JsonSerializer.Deserialize<EmployeeWithContacts>(json, s_deserializerOptionsPreserve);
            Assert.Same(employee, employee.Contacts["Angela"]);
        }

        [Fact] //Employee list as a property and then use reference to itself on nested Employee.
        public static void ObjectWithArrayReferenceDeeper()
        {
            string json =
            @"{
                ""$id"": ""1"",
                ""Subordinates"": {
                    ""$id"": ""2"",
                    ""$values"": [
                        {
                            ""$id"": ""3"",
                            ""Name"": ""Angela"",
                            ""Subordinates"":{
                                ""$ref"": ""2""
                            }
                        }
                    ]
                }
            }";

            Employee employee = JsonSerializer.Deserialize<Employee>(json, s_deserializerOptionsPreserve);
            Assert.Same(employee.Subordinates, employee.Subordinates[0].Subordinates);
        }

        [Fact]
        public static void ObjectWithDictionaryReferenceDeeper()
        {
            string json =
            @"{
                ""$id"": ""1"",
                ""Contacts"": {
                    ""$id"": ""2"",
                    ""Angela"": {
                        ""$id"": ""3"",
                        ""Name"": ""Angela"",
                        ""Contacts"": {
                            ""$ref"": ""2""
                        }
                    }
                }
            }";

            EmployeeWithContacts employee = JsonSerializer.Deserialize<EmployeeWithContacts>(json, s_deserializerOptionsPreserve);
            Assert.Same(employee.Contacts, employee.Contacts["Angela"].Contacts);
        }

        private class ClassWithSubsequentListProperties
        {
            public List<int> MyList { get; set; }
            public List<int> MyListCopy { get; set; }
        }

        [Fact]
        public static void PreservedArrayIntoArrayProperty()
        {
            string json = @"
            {
                ""MyList"": {
                    ""$id"": ""1"",
                    ""$values"": [
                        10,
                        20,
                        30,
                        40
                    ]
                },
                ""MyListCopy"": { ""$ref"": ""1"" }
            }";

            ClassWithSubsequentListProperties instance = JsonSerializer.Deserialize<ClassWithSubsequentListProperties>(json, s_deserializerOptionsPreserve);
            Assert.Equal(4, instance.MyList.Count);
            Assert.Same(instance.MyList, instance.MyListCopy);
        }

        [Fact]
        public static void PreservedArrayIntoInitializedProperty()
        {
            string json = @"{
                ""$id"": ""1"",
                ""SubordinatesString"": {
                    ""$id"": ""2"",
                    ""$values"": [
                    ]
                },
                ""Manager"": {
                    ""SubordinatesString"":{
                        ""$ref"": ""2""
                    }
                }
            }";

            Employee employee = JsonSerializer.Deserialize<Employee>(json, s_deserializerOptionsPreserve);
            // presereved array.
            Assert.Empty(employee.SubordinatesString);
            // reference to preserved array.
            Assert.Empty(employee.Manager.SubordinatesString);
            Assert.Same(employee.Manager.SubordinatesString, employee.SubordinatesString);
        }

        [Fact] // Verify ReadStackFrame.DictionaryPropertyIsPreserved is being reset properly.
        public static void DictionaryPropertyOneAfterAnother()
        {
            string json = @"{
                ""$id"": ""1"",
                ""Contacts"": {
                    ""$id"": ""2""
                },
                ""Contacts2"": {
                    ""$ref"": ""2""
                }
            }";

            Employee employee = JsonSerializer.Deserialize<Employee>(json, s_deserializerOptionsPreserve);
            Assert.Same(employee.Contacts, employee.Contacts2);

            json = @"{
                ""$id"": ""1"",
                ""Contacts"": {
                    ""$id"": ""2""
                },
                ""Contacts2"": {
                    ""$id"": ""3""
                }
            }";

            employee = JsonSerializer.Deserialize<Employee>(json, s_deserializerOptionsPreserve);
            Assert.Equal(0, employee.Contacts.Count);
            Assert.Equal(0, employee.Contacts2.Count);
        }

        [Fact]
        public static void ObjectPropertyLengthZero()
        {
            string json = @"{
                """": 1
            }";

            ClassWithZeroLengthProperty<int> root = JsonSerializer.Deserialize<ClassWithZeroLengthProperty<int>>(json, s_deserializerOptionsPreserve);
            Assert.Equal(1, root.ZeroLengthProperty);
        }

        [Fact]
        public static async Task TestJsonPathDoesNotFailOnMultiThreads()
        {
            const int ThreadCount = 8;
            const int ConcurrentTestsCount = 4;
            Task[] tasks = new Task[ThreadCount * ConcurrentTestsCount];

            for (int i = 0; i < tasks.Length; i++)
            {
                tasks[i++] = Task.Run(() => TestIdTask());
                tasks[i++] = Task.Run(() => TestRefTask());
                tasks[i++] = Task.Run(() => TestIdTask());
                tasks[i] = Task.Run(() => TestRefTask());
            }

            await Task.WhenAll(tasks);
        }

        private static void TestIdTask()
        {
            JsonException ex = Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<Employee>(@"{""$id"":1}", s_deserializerOptionsPreserve));
            Assert.Equal("$.$id", ex.Path);
        }

        private static void TestRefTask()
        {
            JsonException ex = Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<Employee>(@"{""$ref"":1}", s_deserializerOptionsPreserve));
            Assert.Equal("$.$ref", ex.Path);
        }
        #endregion

        #region Root Dictionary
        [Fact]
        public static void DictionaryWithoutMetadata()
        {
            string json = "{}";
            Dictionary<string, string> dictionary = JsonSerializer.Deserialize<Dictionary<string, string>>(json, s_deserializerOptionsPreserve);
            Assert.NotNull(dictionary);
        }

        [Fact] //Employee list as a property and then use reference to itself on nested Employee.
        public static void DictionaryReferenceLoop()
        {
            string json =
            @"{
                ""$id"": ""1"",
                ""Angela"": {
                    ""$id"": ""2"",
                    ""Name"": ""Angela"",
                    ""Contacts"": {
                        ""$ref"": ""1""
                    }
                }
            }";

            Dictionary<string, EmployeeWithContacts> dictionary = JsonSerializer.Deserialize<Dictionary<string, EmployeeWithContacts>>(json, s_deserializerOptionsPreserve);

            Assert.Same(dictionary, dictionary["Angela"].Contacts);
        }

        [Fact]
        public static void DictionaryReferenceLoopInList()
        {
            string json =
            @"{
                ""$id"": ""1"",
                ""Angela"": {
                    ""$id"": ""2"",
                    ""Name"": ""Angela"",
                    ""Subordinates"": {
                        ""$id"": ""3"",
                        ""$values"": [
                            {
                                ""$id"": ""4"",
                                ""Name"": ""Bob"",
                                ""Contacts"": {
                                    ""$ref"": ""1""
                                }
                            }
                        ]
                    }
                }
            }";

            Dictionary<string, EmployeeWithContacts> dictionary = JsonSerializer.Deserialize<Dictionary<string, EmployeeWithContacts>>(json, s_deserializerOptionsPreserve);
            Assert.Same(dictionary, dictionary["Angela"].Subordinates[0].Contacts);
        }

        [Fact]
        public static void DictionaryDuplicatedObject()
        {
            string json =
            @"{
              ""555"": { ""$id"": ""1"", ""Name"": ""Angela"" },
              ""556"": { ""Name"": ""Bob"" },
              ""557"": { ""$ref"": ""1"" }
            }";

            Dictionary<string, Employee> directory = JsonSerializer.Deserialize<Dictionary<string, Employee>>(json, s_deserializerOptionsPreserve);
            Assert.Same(directory["555"], directory["557"]);
        }

        [Fact]
        public static void DictionaryOfArrays()
        {
            string json =
            @"{
                ""$id"": ""1"",
                ""Array1"": {
                    ""$id"": ""2"",
                    ""$values"": []
                },
                ""Array2"": {
                    ""$ref"": ""2""
                }
            }";

            Dictionary<string, List<int>> dict = JsonSerializer.Deserialize<Dictionary<string, List<int>>>(json, s_deserializerOptionsPreserve);
            Assert.Same(dict["Array1"], dict["Array2"]);
        }

        [Fact]
        public static void DictionaryOfDictionaries()
        {
            string json = @"{
                ""$id"": ""1"",
                ""Dictionary1"": {
                    ""$id"": ""2"",
                    ""value1"": 1,
                    ""value2"": 2,
                    ""value3"": 3
                },
                ""Dictionary2"": {
                    ""$ref"": ""2""
                }
            }";

            Dictionary<string, Dictionary<string, int>> root = JsonSerializer.Deserialize<Dictionary<string, Dictionary<string, int>>>(json, s_deserializerOptionsPreserve);
            Assert.Same(root["Dictionary1"], root["Dictionary2"]);
        }

        [Fact]
        public static void DictionaryKeyLengthZero()
        {
            string json = @"{
                """": 1
            }";

            Dictionary<string, int> root = JsonSerializer.Deserialize<Dictionary<string, int>>(json, s_deserializerOptionsPreserve);
            Assert.Equal(1, root[""]);
        }
        #endregion

        #region Root Array
        [Fact]
        public static void PreservedArrayIntoRootArray()
        {
            string json = @"
            {
                ""$id"": ""1"",
                ""$values"": [
                    10,
                    20,
                    30,
                    40
                ]
            }";

            List<int> myList = JsonSerializer.Deserialize<List<int>>(json, s_deserializerOptionsPreserve);
            Assert.Equal(4, myList.Count);
        }

        [Fact] // Preserved list that contains an employee whose subordinates is a reference to the root list.
        public static void ArrayNestedArray()
        {
            string json =
            @"{
                ""$id"": ""1"",
                ""$values"":[
                    {
                        ""$id"":""2"",
                        ""Name"": ""Angela"",
                        ""Subordinates"": {
                            ""$ref"": ""1""
                        }
                    }
                ]
            }";

            List<Employee> employees = JsonSerializer.Deserialize<List<Employee>>(json, s_deserializerOptionsPreserve);

            Assert.Same(employees, employees[0].Subordinates);
        }

        [Fact]
        public static void EmptyArray()
        {
            string json =
            @"{
              ""$id"": ""1"",
              ""Subordinates"": {
                ""$id"": ""2"",
                ""$values"": []
              },
              ""Name"": ""Angela""
            }";

            Employee angela = JsonSerializer.Deserialize<Employee>(json, s_deserializerOptionsPreserve);

            Assert.NotNull(angela);
            Assert.NotNull(angela.Subordinates);
            Assert.Equal(0, angela.Subordinates.Count);
        }

        [Fact]
        public static void ArrayWithDuplicates() //Make sure the serializer can understand lists that were wrapped in braces.
        {
            string json =
            @"{
                ""$id"": ""1"",
                ""$values"":[
                    {
                        ""$id"": ""2"",
                        ""Name"": ""Angela""
                    },
                    {
                        ""$id"": ""3"",
                        ""Name"": ""Bob""
                    },
                    {
                        ""$ref"": ""2""
                    },
                    {
                        ""$ref"": ""3""
                    },
                    {
                        ""$id"": ""4""
                    },
                    {
                        ""$ref"": ""4""
                    }
                ]
            }";

            List<Employee> employees = JsonSerializer.Deserialize<List<Employee>>(json, s_deserializerOptionsPreserve);
            Assert.Equal(6, employees.Count);
            Assert.Same(employees[0], employees[2]);
            Assert.Same(employees[1], employees[3]);
            Assert.Same(employees[4], employees[5]);
        }

        [Fact]
        public static void ArrayNotPreservedWithDuplicates() //Make sure the serializer can understand lists that were wrapped in braces.
        {
            string json =
            @"[
                {
                    ""$id"": ""2"",
                    ""Name"": ""Angela""
                },
                {
                    ""$id"": ""3"",
                    ""Name"": ""Bob""
                },
                {
                    ""$ref"": ""2""
                },
                {
                    ""$ref"": ""3""
                },
                {
                    ""$id"": ""4""
                },
                {
                    ""$ref"": ""4""
                }
            ]";

            Employee[] employees = JsonSerializer.Deserialize<Employee[]>(json, s_deserializerOptionsPreserve);
            Assert.Equal(6, employees.Length);
            Assert.Same(employees[0], employees[2]);
            Assert.Same(employees[1], employees[3]);
            Assert.Same(employees[4], employees[5]);
        }

        [Fact]
        public static void ArrayWithNestedPreservedArray()
        {
            string json = @"{
                ""$id"": ""1"",
                ""$values"": [
                    {
                        ""$id"": ""2"",
                        ""$values"": [ 1, 2, 3 ]
                    }
                ]
            }";

            List<List<int>> root = JsonSerializer.Deserialize<List<List<int>>>(json, s_deserializerOptionsPreserve);
            Assert.Equal(1, root.Count);
            Assert.Equal(3, root[0].Count);
        }

        [Fact]
        public static void ArrayWithNestedPreservedArrayAndReference()
        {
            string json = @"{
                ""$id"": ""1"",
                ""$values"": [
                    {
                        ""$id"": ""2"",
                        ""$values"": [ 1, 2, 3 ]
                    },
                    { ""$ref"": ""2"" }
                ]
            }";

            List<List<int>> root = JsonSerializer.Deserialize<List<List<int>>>(json, s_deserializerOptionsPreserve);
            Assert.Equal(2, root.Count);
            Assert.Equal(3, root[0].Count);
            Assert.Same(root[0], root[1]);
        }

        private class ListWrapper
        {
            public List<List<int>> NestedList { get; set; } = new List<List<int>> { new List<int> { 1 } };
        }

        [Fact]
        public static void ArrayWithNestedPreservedArrayAndDefaultValues()
        {
            string json = @"{
                ""$id"": ""1"",
                ""NestedList"": {
                    ""$id"": ""2"",
                    ""$values"": [
                        {
                            ""$id"": ""3"",
                            ""$values"": [
                                1,
                                2,
                                3
                            ]
                        }
                    ]
                }
            }";

            ListWrapper root = JsonSerializer.Deserialize<ListWrapper>(json, s_deserializerOptionsPreserve);
            Assert.Equal(1, root.NestedList.Count);
            Assert.Equal(3, root.NestedList[0].Count);
        }

        [Fact]
        public static void ArrayWithMetadataWithinArray_UsingPreserve()
        {
            const string json =
            @"[
                {
                    ""$id"": ""1"",
                    ""$values"": []
                }
            ]";

            List<List<Employee>> root = JsonSerializer.Deserialize<List<List<Employee>>>(json, s_serializerOptionsPreserve);
            Assert.Equal(1, root.Count);
            Assert.Equal(0, root[0].Count);
        }

        [Fact]
        public static void ObjectWithinArray_UsingDefault()
        {
            const string json =
            @"[
                {
                    ""$id"": ""1"",
                    ""$values"": []
                }
            ]";

            JsonException ex = Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<List<List<Employee>>>(json));
            Assert.Equal("$[0]", ex.Path);
        }
        #endregion

        #region Converter
        [Fact] //This only demonstrates that behavior with converters remain the same.
        public static void DeserializeWithListConverter()
        {
            string json =
            @"{
                ""$id"": ""1"",
                ""Subordinates"": {
                    ""$id"": ""2"",
                    ""$values"": [
                        {
                            ""$ref"": ""1""
                        }
                    ]
                },
                ""Name"": ""Angela"",
                ""Manager"": {
                    ""Subordinates"": {
                        ""$ref"": ""2""
                    }
                }
            }";

            var options = new JsonSerializerOptions
            {
                ReferenceHandler = ReferenceHandler.Preserve,
                Converters = { new ListOfEmployeeConverter() }
            };
            Employee angela = JsonSerializer.Deserialize<Employee>(json, options);
            Assert.Equal(0, angela.Subordinates.Count);
            Assert.Equal(0, angela.Manager.Subordinates.Count);
        }

        //NOTE: If you implement a converter, you are on your own when handling metadata properties and therefore references.Newtonsoft does the same.
        //However; is there a way to recall preserved references previously found in the payload and to store new ones found in the converter's payload? that would be a cool enhancement.
        private class ListOfEmployeeConverter : JsonConverter<List<Employee>>
        {
            public override List<Employee> Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
            {
                int startObjectCount = 0;
                int endObjectCount = 0;

                while (true)
                {
                    switch (reader.TokenType)
                    {
                        case JsonTokenType.StartObject:
                            startObjectCount++; break;
                        case JsonTokenType.EndObject:
                            endObjectCount++; break;
                    }

                    if (startObjectCount == endObjectCount)
                    {
                        break;
                    }

                    reader.Read();
                }

                return new List<Employee>();
            }

            public override void Write(Utf8JsonWriter writer, List<Employee> value, JsonSerializerOptions options)
            {
                throw new NotImplementedException();
            }
        }
        #endregion

        #region Null/non-existent reference
        [Fact]
        public static void ObjectNull()
        {
            string json =
            @"{
                ""$ref"": ""1""
            }";

            JsonException ex = Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<Employee>(json, s_deserializerOptionsPreserve));
            Assert.Equal("$.$ref", ex.Path);
        }

        [Fact()]
        public static void ArrayNull()
        {
            string json =
            @"{
                ""$ref"": ""1""
            }";

            JsonException ex = Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<List<Employee>>(json, s_deserializerOptionsPreserve));
            Assert.Equal("$.$ref", ex.Path);
        }

        [Fact]
        public static void DictionaryNull()
        {
            string json =
            @"{
                ""$ref"": ""1""
            }";

            JsonException ex = Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<Dictionary<string, Employee>>(json, s_deserializerOptionsPreserve));
            Assert.Equal("$.$ref", ex.Path);
        }

        [Fact]
        public static void ObjectPropertyNull()
        {
            string json =
            @"{
                ""Manager"": {
                    ""$ref"": ""1""
                }
            }";

            JsonException ex = Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<Employee>(json, s_deserializerOptionsPreserve));
            Assert.Equal("$.Manager.$ref", ex.Path);
        }

        [Fact]
        public static void ArrayPropertyNull()
        {
            string json =
            @"{
                ""Subordinates"": {
                    ""$ref"": ""1""
                }
            }";

            JsonException ex = Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<Employee>(json, s_deserializerOptionsPreserve));
            Assert.Equal("$.Subordinates.$ref", ex.Path);
        }

        [Fact]
        public static void DictionaryPropertyNull()
        {
            string json =
            @"{
                ""Contacts"": {
                    ""$ref"": ""1""
                }
            }";

            JsonException ex = Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<Employee>(json, s_deserializerOptionsPreserve));
            Assert.Equal("$.Contacts.$ref", ex.Path);
        }

        #endregion

        #region Throw cases
        [Fact]
        public static void JsonPath()
        {
            string json = @"[0";
            JsonException ex = Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<List<int>>(json, s_deserializerOptionsPreserve));

            Assert.Equal("$[0]", ex.Path);
        }

        [Fact]
        public static void JsonPathObject()
        {
            string json = @"{ ""Name"": ""A";
            JsonException ex = Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<Employee>(json, s_deserializerOptionsPreserve));

            Assert.Equal("$.Name", ex.Path);
        }

        [Fact]
        public static void JsonPathIncompletePropertyAfterMetadata()
        {
            string json =
            @"{
                ""$id"": ""1"",
                ""Nam";
            JsonException ex = Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<Employee>(json, s_deserializerOptionsPreserve));

            // Since $id was parsed correctly, the path should just be "$".
            Assert.Equal("$", ex.Path);
        }

        [Fact]
        public static void JsonPathIncompleteMetadataAfterProperty()
        {
            string json =
            @"{
                ""Name"": ""Angela"",
                ""$i";
            JsonException ex = Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<Employee>(json, s_deserializerOptionsPreserve));

            // Since "Name" was parsed correctly, the path should just be "$".
            Assert.Equal("$", ex.Path);
        }

        [Fact]
        public static void JsonPathCompleteMetadataButNotValue()
        {
            string json =
            @"{
                ""$id"":";
            JsonException ex = Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<Employee>(json, s_deserializerOptionsPreserve));

            Assert.Equal("$.$id", ex.Path);
        }

        [Fact]
        public static void JsonPathIncompleteMetadataValue()
        {
            string json =
            @"{
                ""$id"": ""1";
            JsonException ex = Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<Employee>(json, s_deserializerOptionsPreserve));

            Assert.Equal("$.$id", ex.Path);
        }

        [Fact]
        public static void JsonPathNestedObject()
        {
            string json = @"{ ""Name"": ""A"", ""Manager"": { ""Name"": ""B";
            JsonException ex = Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<Employee>(json, s_deserializerOptionsPreserve));

            Assert.Equal("$.Manager.Name", ex.Path);
        }

        [Fact]
        public static void JsonPathNestedArray()
        {
            string json = @"{ ""Subordinates"":";
            JsonException ex = Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<Employee>(json, s_deserializerOptionsPreserve));

            Assert.Equal("$.Subordinates", ex.Path);
        }

        [Fact]
        public static void JsonPathPreservedArray()
        {
            string json =
            @"{
                ""$id"": ""1"",
                ""$values"":[
                    1";
            JsonException ex = Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<List<int>>(json, s_deserializerOptionsPreserve));

            Assert.Equal("$.$values[0]", ex.Path);
        }

        [Fact]
        public static void JsonPathIncompleteArrayId()
        {
            string json =
            @"{
                ""$id"": ""1";
            JsonException ex = Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<List<int>>(json, s_deserializerOptionsPreserve));

            Assert.Equal("$.$id", ex.Path);
        }

        [Fact]
        public static void JsonPathIncompleteArrayValues()
        {
            string json =
            @"{
                ""$id"": ""1"",
                ""$values"":";
            JsonException ex = Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<List<int>>(json, s_deserializerOptionsPreserve));

            Assert.Equal("$.$values", ex.Path);
        }

        [Fact]
        public static void JsonPathCurlyBraceOnArray()
        {
            string json = "{";
            JsonException ex = Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<List<int>>(json, s_deserializerOptionsPreserve));

            Assert.Equal("$", ex.Path);
        }

        [Fact]
        public static void ThrowOnStructWithReference()
        {
            string json =
            @"[
                {
                    ""$id"": ""1"",
                    ""Name"": ""Angela""
                },
                {
                    ""$ref"": ""1""
                }
            ]";

            JsonException ex = Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<List<EmployeeStruct>>(json, s_deserializerOptionsPreserve));
            Assert.Equal("$[1].$ref", ex.Path);
            Assert.Contains($"'{typeof(EmployeeStruct)}'", ex.Message);
        }

        [Theory]
        [InlineData(@"{""$iz"": ""1""}", "$.$iz")]
        [InlineData(@"{""$rez"": ""1""}", "$.$rez")]
        [InlineData(@"{""$valuez"": []}", "$.$valuez")]
        public static void InvalidMetadataPropertyNameWithSameLengthIsNotRecognized(string json, string expectedPath)
        {
            JsonException ex = Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<Employee>(json, s_deserializerOptionsPreserve));
            Assert.Equal(expectedPath, ex.Path);
        }
        #endregion

        #region Throw on immutables
        private class EmployeeWithImmutable
        {
            public ImmutableList<EmployeeWithImmutable> Subordinates { get; set; }
            public EmployeeWithImmutable[] SubordinatesArray { get; set; }
            public ImmutableDictionary<string, EmployeeWithImmutable> Contacts { get; set; }
        }

        [Fact]
        public static void ImmutableEnumerableAsRoot()
        {
            string json =
            @"{
                ""$id"": ""1"",
                ""$values"": []
            }";

            JsonException ex;

            ex = Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<ImmutableList<EmployeeWithImmutable>>(json, s_deserializerOptionsPreserve));
            Assert.Equal("$.$id", ex.Path);
            Assert.Contains($"'{typeof(ImmutableList<EmployeeWithImmutable>)}'", ex.Message);

            ex = Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<EmployeeWithImmutable[]>(json, s_deserializerOptionsPreserve));
            Assert.Equal("$.$id", ex.Path);
            Assert.Contains($"'{typeof(EmployeeWithImmutable[])}'", ex.Message);
        }

        [Fact]
        public static void ImmutableDictionaryAsRoot()
        {
            string json =
            @"{
                ""$id"": ""1"",
                ""Employee1"": {}
            }";

            JsonException ex = Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<ImmutableDictionary<string, EmployeeWithImmutable>>(json, s_deserializerOptionsPreserve));
            Assert.Equal("$.$id", ex.Path);
            Assert.Contains($"'{typeof(ImmutableDictionary<string, EmployeeWithImmutable>)}'", ex.Message);
        }

        [Fact]
        public static void ImmutableEnumerableAsProperty()
        {
            string json =
            @"{
                ""$id"": ""1"",
                ""Subordinates"": {
                    ""$id"": ""2"",
                    ""$values"": []
                }
            }";

            JsonException ex;

            ex = Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<EmployeeWithImmutable>(json, s_deserializerOptionsPreserve));
            Assert.Equal("$.Subordinates.$id", ex.Path);
            Assert.Contains($"'{typeof(ImmutableList<EmployeeWithImmutable>)}'", ex.Message);

            json =
            @"{
                ""$id"": ""1"",
                ""SubordinatesArray"": {
                    ""$id"": ""2"",
                    ""$values"": []
                }
            }";

            ex = Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<EmployeeWithImmutable>(json, s_deserializerOptionsPreserve));
            Assert.Equal("$.SubordinatesArray.$id", ex.Path);
            Assert.Contains($"'{typeof(EmployeeWithImmutable[])}'", ex.Message);
        }

        [Fact]
        public static void ImmutableDictionaryAsProperty()
        {
            string json =
            @"{
                ""$id"": ""1"",
                ""Contacts"": {
                    ""$id"": ""2"",
                    ""Employee1"": {}
                }
            }";

            JsonException ex = Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<EmployeeWithImmutable>(json, s_deserializerOptionsPreserve));
            Assert.Equal("$.Contacts.$id", ex.Path);
            Assert.Contains($"'{typeof(ImmutableDictionary<string, EmployeeWithImmutable>)}'", ex.Message);
        }

        [Fact]
        public static void ImmutableDictionaryPreserveNestedObjects()
        {
            string json =
            @"{
                ""Angela"": {
                    ""$id"": ""1"",
                    ""Name"": ""Angela"",
                    ""Subordinates"": {
                        ""$id"": ""2"",
                        ""$values"": [
                            {
                                ""$id"": ""3"",
                                ""Name"": ""Carlos"",
                                ""Manager"": {
                                    ""$ref"": ""1""
                                }
                            }
                        ]
                    }
                },
                ""Bob"": {
                    ""$id"": ""4"",
                    ""Name"": ""Bob""
                },
                ""Carlos"": {
                    ""$ref"": ""3""
                }
            }";

            // Must not throw since the references are to nested objects, not the immutable dictionary itself.
            ImmutableDictionary<string, Employee> dictionary = JsonSerializer.Deserialize<ImmutableDictionary<string, Employee>>(json, s_deserializerOptionsPreserve);
            Assert.Same(dictionary["Angela"], dictionary["Angela"].Subordinates[0].Manager);
            Assert.Same(dictionary["Carlos"], dictionary["Angela"].Subordinates[0]);
        }

        [Theory]
        [InlineData(@"{""$id"":{}}", JsonTokenType.StartObject)]
        [InlineData(@"{""$id"":[]}", JsonTokenType.StartArray)]
        [InlineData(@"{""$id"":null}", JsonTokenType.Null)]
        [InlineData(@"{""$id"":true}", JsonTokenType.True)]
        [InlineData(@"{""$id"":false}", JsonTokenType.False)]
        [InlineData(@"{""$id"":9}", JsonTokenType.Number)]
        // Invalid JSON, the reader will throw before we reach the serializer validation.
        [InlineData(@"{""$id"":}", JsonTokenType.None)]
        [InlineData(@"{""$id"":]", JsonTokenType.None)]
        public static void MetadataId_StartsWithInvalidToken(string json, JsonTokenType incorrectToken)
        {
            JsonException ex = Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<Employee>(json, s_deserializerOptionsPreserve));
            Assert.True(incorrectToken == JsonTokenType.None || ex.Message.Contains($"'{incorrectToken}'"));
            Assert.Equal("$.$id", ex.Path);

            ex = Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<Dictionary<string, string>>(json, s_deserializerOptionsPreserve));
            Assert.True(incorrectToken == JsonTokenType.None || ex.Message.Contains($"'{incorrectToken}'"));
            Assert.Equal("$.$id", ex.Path);

            ex = Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<List<int>>(json, s_deserializerOptionsPreserve));
            Assert.True(incorrectToken == JsonTokenType.None || ex.Message.Contains($"'{incorrectToken}'"));
            Assert.Equal("$.$id", ex.Path);
        }


        [Theory]
        [InlineData(@"{""$ref"":{}}", JsonTokenType.StartObject)]
        [InlineData(@"{""$ref"":[]}", JsonTokenType.StartArray)]
        [InlineData(@"{""$ref"":null}", JsonTokenType.Null)]
        [InlineData(@"{""$ref"":true}", JsonTokenType.True)]
        [InlineData(@"{""$ref"":false}", JsonTokenType.False)]
        [InlineData(@"{""$ref"":9}", JsonTokenType.Number)]
        // Invalid JSON, the reader will throw before we reach the serializer validation.
        [InlineData(@"{""$ref"":}", JsonTokenType.None)]
        [InlineData(@"{""$ref"":]", JsonTokenType.None)]
        public static void MetadataRef_StartsWithInvalidToken(string json, JsonTokenType incorrectToken)
        {
            JsonException ex = Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<Employee>(json, s_deserializerOptionsPreserve));
            Assert.True(incorrectToken == JsonTokenType.None || ex.Message.Contains($"'{incorrectToken}'"));
            Assert.Equal("$.$ref", ex.Path);

            ex = Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<Dictionary<string, string>>(json, s_deserializerOptionsPreserve));
            Assert.True(incorrectToken == JsonTokenType.None || ex.Message.Contains($"'{incorrectToken}'"));
            Assert.Equal("$.$ref", ex.Path);

            ex = Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<List<int>>(json, s_deserializerOptionsPreserve));
            Assert.True(incorrectToken == JsonTokenType.None || ex.Message.Contains($"'{incorrectToken}'"));
            Assert.Equal("$.$ref", ex.Path);
        }

        [Theory]
        [InlineData(@"{""$id"":""1"",""$values"":{}}", JsonTokenType.StartObject)]
        [InlineData(@"{""$id"":""1"",""$values"":null}", JsonTokenType.Null)]
        [InlineData(@"{""$id"":""1"",""$values"":true}", JsonTokenType.True)]
        [InlineData(@"{""$id"":""1"",""$values"":false}", JsonTokenType.False)]
        [InlineData(@"{""$id"":""1"",""$values"":9}", JsonTokenType.Number)]
        [InlineData(@"{""$id"":""1"",""$values"":""9""}", JsonTokenType.String)]
        // Invalid JSON, the reader will throw before we reach the serializer validation.
        [InlineData(@"{""$id"":""1"",""$values"":}", JsonTokenType.None)]
        [InlineData(@"{""$id"":""1"",""$values"":]", JsonTokenType.None)]
        public static void MetadataValues_StartsWithInvalidToken(string json, JsonTokenType incorrectToken)
        {
            JsonException ex = Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<List<int>>(json, s_deserializerOptionsPreserve));
            Assert.True(incorrectToken == JsonTokenType.None || ex.Message.Contains($"'{incorrectToken}'"));
            Assert.Equal("$.$values", ex.Path);
        }
        #endregion

        #region Ground Rules/Corner cases

        private class Order
        {
            public int ProductId { get; set; }
            public int Quantity { get; set; }
        }

        [Fact]
        public static void OnlyStringTypeIsAllowed()
        {
            string json = @"{
                ""$id"": 1,
                ""ProductId"": 1,
                ""Quantity"": 10
            }";

            JsonException ex = Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<Order>(json, s_deserializerOptionsPreserve));
            Assert.Equal("$.$id", ex.Path);

            json = @"[
                {
                    ""$id"": ""1"",
                    ""ProductId"": 1,
                    ""Quantity"": 10
                },
                {
                    ""$ref"": 1
                }
            ]";

            ex = Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<List<Order>>(json, s_deserializerOptionsPreserve));
            Assert.Equal("$[1].$ref", ex.Path);
        }

        #region Reference objects ($ref)
        [Fact]
        public static void ReferenceObjectsShouldNotContainMoreProperties()
        {
            //Regular property before $ref
            string json = @"{
                ""$id"": ""1"",
                ""Name"": ""Angela"",
                ""Manager"": {
                    ""Name"": ""Bob"",
                    ""$ref"": ""1""
                }
            }";

            JsonException ex = Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<Employee>(json, s_deserializerOptionsPreserve));
            Assert.Equal("$.Manager.$ref", ex.Path);

            //Regular dictionary key before $ref
            json = @"{
                ""Angela"": {
                    ""Name"": ""Angela"",
                    ""$ref"": ""1""
                }
            }";

            ex = Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<Dictionary<string, Dictionary<string, string>>>(json, s_deserializerOptionsPreserve));
            Assert.Equal("$.Angela.$ref", ex.Path);

            //Regular property after $ref
            json = @"{
                ""$id"": ""1"",
                ""Name"": ""Angela"",
                ""Manager"": {
                    ""$ref"": ""1"",
                    ""Name"": ""Bob""
                }
            }";

            ex = Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<Employee>(json, s_deserializerOptionsPreserve));
            Assert.Equal("$.Manager.Name", ex.Path);

            //Metadata property before $ref
            json = @"{
                ""$id"": ""1"",
                ""Name"": ""Angela"",
                ""Manager"": {
                    ""$id"": ""2"",
                    ""$ref"": ""1""
                }
            }";

            ex = Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<Employee>(json, s_deserializerOptionsPreserve));
            Assert.Equal("$.Manager.$ref", ex.Path);

            //Metadata property after $ref
            json = @"{
                ""$id"": ""1"",
                ""Name"": ""Angela"",
                ""Manager"": {
                    ""$ref"": ""1"",
                    ""$id"": ""2""
                }
            }";

            ex = Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<Employee>(json, s_deserializerOptionsPreserve));
            Assert.Equal("$.Manager.$id", ex.Path);
        }

        [Fact]
        public static void ReferenceObjectBeforePreservedObject()
        {
            string json = @"[
                {
                    ""$ref"": ""1""
                },
                {
                    ""$id"": ""1"",
                    ""Name"": ""Angela""
                }
            ]";

            JsonException ex = Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<List<Employee>>(json, s_deserializerOptionsPreserve));
            Assert.Contains("'1'", ex.Message);
            Assert.Equal("$[0].$ref", ex.Path);
        }

        [Theory]
        [MemberData(nameof(ReadSuccessCases))]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/58204", TestPlatforms.iOS | TestPlatforms.tvOS)]
        public static void ReadTestClassesWithExtensionOption(Type classType, byte[] data)
        {
            var options = new JsonSerializerOptions { IncludeFields = true, ReferenceHandler = ReferenceHandler.Preserve };
            object obj = JsonSerializer.Deserialize(data, classType, options);
            Assert.IsAssignableFrom<ITestClass>(obj);
            ((ITestClass)obj).Verify();
        }

        public static IEnumerable<object[]> ReadSuccessCases
        {
            get
            {
                return TestData.ReadSuccessCases;
            }
        }

        #endregion

        #region Preserved objects ($id)
        [Fact]
        public static void MoreThanOneId()
        {
            string json = @"{
                ""$id"": ""1"",
                ""$id"": ""2"",
                ""Name"": ""Angela"",
                ""Manager"": {
                    ""$ref"": ""1""
                }
            }";

            JsonException ex = Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<Employee>(json, s_deserializerOptionsPreserve));

            Assert.Equal("$.$id", ex.Path);
        }

        [Fact]
        public static void IdIsNotFirstProperty()
        {
            string json = @"{
                ""Name"": ""Angela"",
                ""$id"": ""1"",
                ""Manager"": {
                    ""$ref"": ""1""
                }
            }";

            JsonException ex = Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<Employee>(json, s_deserializerOptionsPreserve));
            Assert.Equal("$.$id", ex.Path);

            json = @"{
                ""Name"": ""Angela"",
                ""$id"": ""1""
            }";

            ex = Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<Dictionary<string, string>>(json, s_deserializerOptionsPreserve));
            Assert.Equal("$.$id", ex.Path);
        }

        [Fact]
        public static void DuplicatedId()
        {
            string json = @"[
                {
                    ""$id"": ""1"",
                    ""Name"": ""Angela""
                },
                {
                    ""$id"": ""1"",
                    ""Name"": ""Bob""
                }
            ]";

            JsonException ex = Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<List<Employee>>(json, s_deserializerOptionsPreserve));

            Assert.Equal("$[1].$id", ex.Path);
            Assert.Contains("'1'", ex.Message);
        }

        class ClassWithTwoListProperties
        {
            public List<string> List1 { get; set; }
            public List<string> List2 { get; set; }
        }

        [Fact]
        public static void DuplicatedIdArray()
        {
            string json = @"{
                ""List1"": {
                        ""$id"": ""1"",
                        ""$values"": []
                    },
                ""List2"": {
                        ""$id"": ""1"",
                        ""$values"": []
                    }
            }";

            JsonException ex = Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<ClassWithTwoListProperties>(json, s_deserializerOptionsPreserve));

            Assert.Equal("$.List2.$id", ex.Path);
            Assert.Contains("'1'", ex.Message);
        }

        [Theory]
        [InlineData(@"{""$id"":""A"", ""Manager"":{""$ref"":""A""}}")]
        [InlineData(@"{""$id"":""00000000-0000-0000-0000-000000000000"", ""Manager"":{""$ref"":""00000000-0000-0000-0000-000000000000""}}")]
        [InlineData("{\"$id\":\"A\u0467\", \"Manager\":{\"$ref\":\"A\u0467\"}}")]
        public static void TestOddStringsInMetadata(string json)
        {
            Employee root = JsonSerializer.Deserialize<Employee>(json, s_deserializerOptionsPreserve);
            Assert.NotNull(root);
            Assert.Same(root, root.Manager);
        }
        #endregion

        #region Preserved arrays ($id and $values)
        [Fact]
        public static void PreservedArrayWithoutMetadata()
        {
            string json = "{}";
            JsonException ex = Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<List<int>>(json, s_deserializerOptionsPreserve));

            Assert.Equal("$", ex.Path);
            Assert.Contains(typeof(List<int>).ToString(), ex.Message);
        }

        [Fact]
        public static void PreservedArrayWithoutValues()
        {
            string json = @"{
                ""$id"": ""1""
            }";
            JsonException ex = Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<List<int>>(json, s_deserializerOptionsPreserve));

            Assert.Equal("$", ex.Path);
            Assert.Contains("$values", ex.Message);
            Assert.Contains(typeof(List<int>).ToString(), ex.Message);
        }

        [Fact]
        public static void PreservedArrayWithoutId()
        {
            string json = @"{
                ""$values"": []
            }";

            JsonException ex = Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<List<int>>(json, s_deserializerOptionsPreserve));

            Assert.Equal("$.$values", ex.Path);
        }

        [Fact]
        public static void PreservedArrayValuesContainsNull()
        {
            string json = @"{
                ""$id"": ""1"",
                ""$values"": null
            }";

            JsonException ex = Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<List<int>>(json, s_deserializerOptionsPreserve));

            Assert.Equal("$.$values", ex.Path);
        }

        [Fact]
        public static void PreservedArrayValuesContainsValue()
        {
            string json = @"{
                ""$id"": ""1"",
                ""$values"": 1
            }";

            JsonException ex = Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<List<Employee>>(json, s_deserializerOptionsPreserve));

            Assert.Equal("$.$values", ex.Path);
        }

        [Fact]
        public static void PreservedArrayValuesContainsObject()
        {
            string json = @"{
                ""$id"": ""1"",
                ""$values"": {}
            }";

            JsonException ex = Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<List<Employee>>(json, s_deserializerOptionsPreserve));

            Assert.Equal("$.$values", ex.Path);
        }

        [Fact]
        public static void PreservedArrayExtraProperties()
        {
            string json = @"{
                ""LeadingProperty"": 0
                ""$id"": ""1"",
                ""$values"": []
            }";

            JsonException ex = Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<List<Employee>>(json, s_deserializerOptionsPreserve));

            Assert.Equal("$.LeadingProperty", ex.Path);
            Assert.Contains(typeof(List<Employee>).ToString(), ex.Message);

            json = @"{
                ""$id"": ""1"",
                ""$values"": [],
                ""TrailingProperty"": 0
            }";

            ex = Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<List<Employee>>(json, s_deserializerOptionsPreserve));

            Assert.Equal("$.TrailingProperty", ex.Path);
            Assert.Contains(typeof(List<Employee>).ToString(), ex.Message);
            Assert.Contains("TrailingProperty", ex.Message);
        }
        #endregion

        #region JSON Objects if not collection
        private class EmployeeExtensionData : Employee
        {
            [JsonExtensionData]
            [Newtonsoft.Json.JsonExtensionData]
            public IDictionary<string, object> ExtensionData { get; set; }
        }

        [Fact]
        public static void JsonObjectNonCollectionTest()
        {
            // $values Not Valid
            string json = @"{
                ""$id"": ""1"",
                ""$values"": ""test""
            }";

            JsonException ex = Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<EmployeeExtensionData>(json, s_deserializerOptionsPreserve));
            Assert.Equal("$.$values", ex.Path);

            ex = Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<Dictionary<string, string>>(json, s_deserializerOptionsPreserve));
            Assert.Equal("$.$values", ex.Path);

            // $.* Not valid (i.e: $test)
            json = @"{
                ""$id"": ""1"",
                ""$test"": ""test""
            }";

            ex = Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<EmployeeExtensionData>(json, s_deserializerOptionsPreserve));
            Assert.Equal("$.$test", ex.Path);

            ex = Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<Dictionary<string, string>>(json, s_deserializerOptionsPreserve));
            Assert.Equal("$.$test", ex.Path);

            json = @"{
                ""$id"": ""1"",
                ""\u0024test"": ""test""
            }";

            // \u0024.* Valid (i.e: \u0024test)
            EmployeeExtensionData employee = JsonSerializer.Deserialize<EmployeeExtensionData>(json, s_deserializerOptionsPreserve);
            Assert.Equal("test", ((JsonElement)employee.ExtensionData["$test"]).GetString());

            Dictionary<string, string> dictionary = JsonSerializer.Deserialize<Dictionary<string, string>>(json, s_deserializerOptionsPreserve);
            Assert.Equal("test", dictionary["$test"]);
        }
        #endregion

        #region JSON Objects if collection
        [Fact]
        public static void JsonObjectCollectionTest()
        {

            // $ref Valid under conditions: must be the only property in the object.
            string json = @"{
                ""$ref"": ""1""
            }";

            JsonException ex = Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<List<string>>(json, s_deserializerOptionsPreserve));
            Assert.Equal("$.$ref", ex.Path);

            // $id Valid under conditions: must be the first property in the object.
            // $values Valid under conditions: must be after $id.
            json = @"{
                ""$id"": ""1"",
                ""$values"": []
            }";

            List<string> root = JsonSerializer.Deserialize<List<string>>(json, s_deserializerOptionsPreserve);
            Assert.NotNull(root);
            Assert.Equal(0, root.Count);

            // $.* Not valid (i.e: $test)
            json = @"{
                ""$id"": ""1"",
                ""$test"": ""test""
            }";

            ex = Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<List<string>>(json, s_deserializerOptionsPreserve));
        }
        #endregion
        #endregion

        [Fact]
        public static void ReferenceIsAssignableFrom()
        {
            const string json = @"{""Derived"":{""$id"":""my_id_1""},""Base"":{""$ref"":""my_id_1""}}";
            BaseAndDerivedWrapper root = JsonSerializer.Deserialize<BaseAndDerivedWrapper>(json, s_serializerOptionsPreserve);

            Assert.Same(root.Base, root.Derived);
        }

        [Fact]
        public static void ReferenceIsNotAssignableFrom()
        {
            const string json = @"{""Base"":{""$id"":""my_id_1""},""Derived"":{""$ref"":""my_id_1""}}";
            InvalidOperationException ex = Assert.Throws<InvalidOperationException>(() => JsonSerializer.Deserialize<BaseAndDerivedWrapper>(json, s_serializerOptionsPreserve));

            Assert.Contains("my_id_1", ex.Message);
            Assert.Contains(typeof(Derived).ToString(), ex.Message);
            Assert.Contains(typeof(Base).ToString(), ex.Message);
        }

        private class BaseAndDerivedWrapper
        {
            public Base Base { get; set; }
            public Derived Derived { get; set; }
        }

        private class Derived : Base { }
        private class Base { }
    }
}
