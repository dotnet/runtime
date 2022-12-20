// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Xunit;

namespace System.Text.Json.Serialization.Tests
{
    public abstract partial class ReferenceHandlerTests : SerializerTests
    {
        private static readonly JsonSerializerOptions s_serializerOptionsPreserve = new JsonSerializerOptions { ReferenceHandler = ReferenceHandler.Preserve };
        private static readonly JsonSerializerSettings s_newtonsoftSerializerSettingsPreserve = new JsonSerializerSettings { PreserveReferencesHandling = PreserveReferencesHandling.All, ReferenceLoopHandling = ReferenceLoopHandling.Serialize };

        public class Employee
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
        public async Task ExtensionDataDictionaryHandlesPreserveReferences()
        {
            Employee bob = new Employee { Name = "Bob" };

            EmployeeExtensionData angela = new EmployeeExtensionData
            {
                Name = "Angela",

                Manager = bob
            };
            bob.Subordinates = new List<Employee> { angela };

            var extensionData = new Dictionary<string, object>
            {
                ["extString"] = "string value",
                ["extNumber"] = 100,
                ["extObject"] = bob,
                ["extArray"] = bob.Subordinates
            };

            angela.ExtensionData = extensionData;

            string expected = JsonConvert.SerializeObject(angela, s_newtonsoftSerializerSettingsPreserve);
            string actual = await Serializer.SerializeWrapper(angela, s_serializerOptionsPreserve);

            Assert.Equal(expected, actual);
        }

        #region struct tests
        public struct EmployeeStruct
        {
            public string Name { get; set; }
            public JobStruct Job { get; set; }
            public ImmutableArray<RoleStruct> Roles { get; set; }
        }

        public struct JobStruct
        {
            public string Title { get; set; }
        }

        public struct RoleStruct
        {
            public string Description { get; set; }
        }

        [Fact]
        public async Task ValueTypesShouldNotContainId()
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
            string actual = await Serializer.SerializeWrapper(array, s_serializerOptionsPreserve);
            string expected = await Serializer.SerializeWrapper(array);

            Assert.Equal(expected, actual);
        }
        #endregion struct tests

        #region Encode JSON property with leading '$'
        public class ClassWithExtensionData
        {
            public string Hello { get; set; }

            [JsonExtensionData]
            public Dictionary<string, object> ExtensionData { get; set; }
        }

        [Fact]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/1780")]
        public async Task DictionaryKeyContainingLeadingDollarSignShouldBeEncoded()
        {
            //$ Key in dictionary holding primitive type.
            Dictionary<string, object> dictionary = new Dictionary<string, object>
            {
                ["$string"] = "Hello world"
            };
            string json = await Serializer.SerializeWrapper(dictionary, s_serializerOptionsPreserve);
            Assert.Equal(@"{""$id"":""1"",""\u0024string"":""Hello world""}", json);

            //$ Key in dictionary holding complex type.
            dictionary = new Dictionary<string, object>
            {
                ["$object"] = new ClassWithExtensionData { Hello = "World" }
            };
            json = await Serializer.SerializeWrapper(dictionary, s_serializerOptionsPreserve);
            Assert.Equal(@"{""$id"":""1"",""\u0024object"":{""$id"":""2"",""Hello"":""World""}}", json);

            //$ Key in ExtensionData dictionary
            var poco = new ClassWithExtensionData
            {
                ExtensionData =
                {
                    ["$string"] = "Hello world",
                    ["$object"] = new ClassWithExtensionData
                    {
                        Hello = "World"
                    }
                }
            };
            json = await Serializer.SerializeWrapper(poco, s_serializerOptionsPreserve);
            Assert.Equal(@"{""$id"":""1"",""\u0024string"":""Hello world"",""\u0024object"":{""$id"":""2"",""Hello"":""World""}}", json);

            //TODO:
            //Extend the scenarios to also cover CLR and F# properties with a leading $.
            //Also add scenarios where a NamingPolicy (DictionaryKey or Property) appends the leading $.
        }
        #endregion

        public class ClassWithListAndImmutableArray
        {
            public List<int> PreservableList { get; set; }
            public ImmutableArray<int> NonProservableArray { get; set; }
        }

        [Fact]
        public async Task WriteWrappingBraceResetsCorrectly()
        {
            List<int> list = new List<int> { 10, 20, 30 };
            ImmutableArray<int> immutableArr = list.ToImmutableArray();

            var root = new ClassWithListAndImmutableArray
            {
                PreservableList = list,
                // Do not write any curly braces for ImmutableArray since is a value type.
                NonProservableArray = immutableArr
            };
            await Serializer.SerializeWrapper(root, s_serializerOptionsPreserve);

            ImmutableArray<List<int>> immutablArraytOfLists = new List<List<int>> { list }.ToImmutableArray();
            await Serializer.SerializeWrapper(immutablArraytOfLists, s_serializerOptionsPreserve);

            List<ImmutableArray<int>> listOfImmutableArrays = new List<ImmutableArray<int>> { immutableArr };
            await Serializer.SerializeWrapper(listOfImmutableArrays, s_serializerOptionsPreserve);

            List<object> mixedListOfLists = new List<object> { list, immutableArr, list, immutableArr };
            await Serializer.SerializeWrapper(mixedListOfLists, s_serializerOptionsPreserve);
        }

        public class ClassIncorrectHashCode
        {
            private static int s_index = 0;

            public override int GetHashCode()
            {
                s_index++;
                return s_index;
            }
        };

        [Fact]
        public async Task CustomHashCode()
        {
            // Test that POCO's implementation of GetHashCode is always used.
            ClassIncorrectHashCode elem = new ClassIncorrectHashCode();
            List<ClassIncorrectHashCode> list = new List<ClassIncorrectHashCode>()
            {
                elem,
                elem,
            };

            string json = await Serializer.SerializeWrapper(list, s_serializerOptionsPreserve);
            Assert.Equal(@"{""$id"":""1"",""$values"":[{""$id"":""2""},{""$ref"":""2""}]}", json);

            List<ClassIncorrectHashCode> listCopy = await Serializer.DeserializeWrapper<List<ClassIncorrectHashCode>>(json, s_serializerOptionsPreserve);
            // Make sure that our DefaultReferenceResolver calls the ReferenceEqualityComparer that implements RuntimeHelpers.GetHashCode, and never object.GetHashCode,
            // otherwise objects would not be correctly identified when searching for them in the dictionary.
            Assert.Same(listCopy[0], listCopy[1]);
        }
    }
}
