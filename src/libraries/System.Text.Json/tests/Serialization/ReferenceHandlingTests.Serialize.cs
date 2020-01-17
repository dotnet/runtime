// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Text.Json.Serialization;
using Newtonsoft.Json;
using Xunit;

namespace System.Text.Json.Tests
{
    public static partial class ReferenceHandlingTests
    {
        private static JsonSerializerOptions _serializeOptionsPreserve = new JsonSerializerOptions { ReferenceHandling = ReferenceHandling.Preserve };
        private static JsonSerializerSettings _newtonsoftSerializeOptionsPreserve = new JsonSerializerSettings { PreserveReferencesHandling = PreserveReferencesHandling.All, ReferenceLoopHandling = ReferenceLoopHandling.Serialize };

        private class Employee
        {
            public string Name { get; set; }
            public Employee Manager { get; set; }
            public Employee Manager2 { get; set; }
            public List<Employee> Subordinates { get; set; }
            public List<Employee> Subordinates2 { get; set; }
            public Dictionary<string, Employee> Contacts { get; set; }
            public Dictionary<string, Employee> Contacts2 { get; set; }

            //Properties with default value to verify they get overwritten when deserializing into them.
            public List<string> SubordinatesString { get; set; } = new List<string> { "Bob" };
            public Dictionary<string, string> ContactsString { get; set; } = new Dictionary<string, string>() { { "Bob", "555-5555" } };
        }

        [Fact]
        public static void ExtensionDataDictionaryHandlesPreserveReferences()
        {
            Employee bob = new Employee { Name = "Bob" };

            EmployeeExtensionData angela = new EmployeeExtensionData();
            angela.Name = "Angela";

            angela.Manager = bob;
            bob.Subordinates = new List<Employee> { angela };

            var extensionData = new Dictionary<string, object>();
            extensionData["extString"] = "string value";
            extensionData["extNumber"] = 100;
            extensionData["extObject"] = bob;
            extensionData["extArray"] = bob.Subordinates;

            angela.ExtensionData = extensionData;

            string expected = JsonConvert.SerializeObject(angela, _newtonsoftSerializeOptionsPreserve);
            string actual = JsonSerializer.Serialize(angela, _serializeOptionsPreserve);

            Assert.Equal(expected, actual);
        }

        #region struct tests
        private struct EmployeeStruct
        {
            public string Name { get; set; }
            public JobStruct Job { get; set; }
            public ImmutableArray<RoleStruct> Roles { get; set; }
        }

        private struct JobStruct
        {
            public string Title { get; set; }
        }

        private struct RoleStruct
        {
            public string Description { get; set; }
        }

        [Fact]
        public static void ValueTypesShouldNotContainId()
        {
            //Struct as root.
            EmployeeStruct employee = new EmployeeStruct
            {
                Name = "Angela",
                //Struct as property.
                Job = new JobStruct
                {
                    Title = "Software Engineer"
                },
                //ImmutableArray<T> as property.
                Roles =
                    ImmutableArray.Create(
                        new RoleStruct
                        {
                            Description = "Contributor"
                        },
                        new RoleStruct
                        {
                            Description = "Infrastructure"
                        })
            };

            //ImmutableArray<T> as root.
            ImmutableArray<EmployeeStruct> array =
                //Struct as array element (same as struct being root).
                ImmutableArray.Create(employee);

            // Regardless of using preserve, do not emit $id to value types; that is why we compare against default.
            string actual = JsonSerializer.Serialize(array, _serializeOptionsPreserve);
            string expected = JsonSerializer.Serialize(array);

            Assert.Equal(expected, actual);
        }
        #endregion struct tests

        #region Encode JSON property with leading '$'
        private class MyPoco
        {
            public string Hello { get; set; }

            [Serialization.JsonExtensionData]
            public Dictionary<string, object> ExtensionData { get; set; }
        }

        [Fact]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/1780")]
        public static void DictionaryKeyContainingLeadingDollarSignShouldBeEncoded()
        {
            //$ Key in dictionary holding primitive type.
            Dictionary<string, object> dictionary = new Dictionary<string, object>();
            dictionary["$string"] = "Hello world";
            string json = JsonSerializer.Serialize(dictionary, _serializeOptionsPreserve);
            Assert.Equal(@"{""$id"":""1"",""\u0024string"":""Hello world""}", json);

            //$ Key in dictionary holding complex type.
            dictionary = new Dictionary<string, object>();
            dictionary["$object"] = new MyPoco { Hello = "World" };
            json = JsonSerializer.Serialize(dictionary, _serializeOptionsPreserve);
            Assert.Equal(@"{""$id"":""1"",""\u0024object"":{""$id"":""2"",""Hello"":""World""}}", json);

            //$ Key in ExtensionData dictionary
            MyPoco poco = new MyPoco();
            poco.ExtensionData["$string"] = "Hello world";
            poco.ExtensionData["$object"] = new MyPoco { Hello = "World" };
            json = JsonSerializer.Serialize(poco, _serializeOptionsPreserve);
            Assert.Equal(@"{""$id"":""1"",""\u0024string"":""Hello world"",""\u0024object"":{""$id"":""2"",""Hello"":""World""}}", json);

            //TODO:
            //Extend the scenarios to also cover CLR and F# properties with a leading $.
            //Also add scenarios where a NamingPolicy (DictionaryKey or Property) appends the leading $.
        }
        #endregion

        private class MyTestClass
        {
            public List<int> PreservableList { get; set; }
            public ImmutableArray<int> NonProservableArray { get; set; }
        }

        [Fact]
        public static void WriteWrappingBraceResetsCorrectly()
        {
            List<int> list = new List<int> { 10, 20, 30 };
            ImmutableArray<int> immutableArr = list.ToImmutableArray();
 
            var root = new MyTestClass();
            root.PreservableList = list;
            // Do not write any curly braces for ImmutableArray since is a value type.
            root.NonProservableArray = immutableArr;
            JsonSerializer.Serialize(root, _serializeOptionsPreserve);

            ImmutableArray<List<int>> immutablArraytOfLists = new List<List<int>> { list }.ToImmutableArray();
            JsonSerializer.Serialize(immutablArraytOfLists, _serializeOptionsPreserve);

            List<ImmutableArray<int>> listOfImmutableArrays = new List<ImmutableArray<int>> { immutableArr };
            JsonSerializer.Serialize(listOfImmutableArrays, _serializeOptionsPreserve);

            List<object> mixedListOfLists = new List<object> { list, immutableArr, list, immutableArr };
            JsonSerializer.Serialize(mixedListOfLists, _serializeOptionsPreserve);
        }
    }
}
