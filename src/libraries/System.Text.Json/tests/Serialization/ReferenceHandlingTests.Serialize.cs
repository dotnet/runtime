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
        public enum TestReferenceHandling
        {
            Default,
            Ignore,
            Preserve
        }

        private static JsonSerializerOptions _serializeOptionsError = new JsonSerializerOptions { ReferenceHandling = ReferenceHandling.Default };
        private static JsonSerializerOptions _serializeOptionsIgnore = new JsonSerializerOptions { ReferenceHandling = ReferenceHandling.Ignore };
        private static JsonSerializerOptions _serializeOptionsPreserve = new JsonSerializerOptions { ReferenceHandling = ReferenceHandling.Preserve };

        private static JsonSerializerSettings _newtonsoftSerializeOptionsError = new JsonSerializerSettings { ReferenceLoopHandling = ReferenceLoopHandling.Error};
        private static JsonSerializerSettings _newtonsoftSerializeOptionsIgnore = new JsonSerializerSettings { ReferenceLoopHandling = ReferenceLoopHandling.Ignore };
        private static JsonSerializerSettings _newtonsoftSerializeOptionsPreserve = new JsonSerializerSettings { PreserveReferencesHandling = PreserveReferencesHandling.All, ReferenceLoopHandling = ReferenceLoopHandling.Serialize };

        private class Employee
        {
            public static readonly List<string> SubordinatesDefault = new List<string> { "Bob" }; //how can I make these immutables?
            public static readonly Dictionary<string, string> ContactsDefault = new Dictionary<string, string>() { { "Bob", "555-5555" } };

            public string Name { get; set; }
            public Employee Manager { get; set; }
            public Employee Manager2 { get; set; }
            public List<Employee> Subordinates { get; set; }
            public List<Employee> Subordinates2 { get; set; }
            public Dictionary<string, Employee> Contacts { get; set; }
            public Dictionary<string, Employee> Contacts2 { get; set; }

            //Properties with default value.
            public List<string> SubordinatesString { get; set; } = SubordinatesDefault;
            public Dictionary<string, string> ContactsString { get; set; } = ContactsDefault;
        }

        [Fact]
        public static void ThrowByDefaultOnLoop()
        {
            Employee a = new Employee();
            a.Manager = a;

            JsonException ex = Assert.Throws<JsonException>(() => JsonSerializer.Serialize(a));
            //Assert.Equal("Invalid Reference Loop Detected!.", ex.Message);
            //TODO: Change default throw error msg in order to state that you can deal with loops with any of the other RefHandling options.
            Assert.Contains("A possible object cycle was detected which is not supported.", ex.Message);
        }

        #region Root Object
        [Theory]
        [InlineData(TestReferenceHandling.Ignore)]
        [InlineData(TestReferenceHandling.Preserve)]
        public static void ObjectLoop(TestReferenceHandling referenceHandling)
        {
            Employee angela = new Employee();
            angela.Manager = angela;

            string expected = JsonConvert.SerializeObject(angela, JsonNetSettings(referenceHandling));            
            string actual = JsonSerializer.Serialize(angela, SystemTextJsonOptions(referenceHandling));

            Assert.Equal(expected, actual);
        }

        [Theory]
        [InlineData(TestReferenceHandling.Ignore)]
        [InlineData(TestReferenceHandling.Preserve)]
        public static void ObjectArrayLoop(TestReferenceHandling referenceHandling)
        {
            Employee angela = new Employee();
            angela.Subordinates = new List<Employee> { angela };

            string expected = JsonConvert.SerializeObject(angela, JsonNetSettings(referenceHandling));
            string actual = JsonSerializer.Serialize(angela, SystemTextJsonOptions(referenceHandling));

            Assert.Equal(expected, actual);
        }

        [Theory]
        [InlineData(TestReferenceHandling.Ignore)]
        [InlineData(TestReferenceHandling.Preserve)]
        public static void ObjectDictionaryLoop(TestReferenceHandling referenceHandling)
        {
            Employee angela = new Employee();
            angela.Contacts = new Dictionary<string, Employee> { { "555-5555", angela } };

            string expected = JsonConvert.SerializeObject(angela, JsonNetSettings(referenceHandling));
            string actual = JsonSerializer.Serialize(angela, SystemTextJsonOptions(referenceHandling));

            Assert.Equal(expected, actual);
        }

        [Fact]
        public static void ObjectPreserveDuplicateObjects()
        {
            Employee angela = new Employee();

            angela.Manager = new Employee { Name = "Bob" };
            angela.Manager2 = angela.Manager;



            string expected = JsonConvert.SerializeObject(angela, JsonNetSettings(TestReferenceHandling.Preserve));
            string actual = JsonSerializer.Serialize(angela, SystemTextJsonOptions(TestReferenceHandling.Preserve));

            Assert.Equal(expected, actual);
        }

        [Fact]
        public static void ObjectPreserveDuplicateDictionaries()
        {
            Employee angela = new Employee();

            angela.Contacts = new Dictionary<string, Employee> { { "444-4444", new Employee { Name = "Bob" } } };
            angela.Contacts2 = angela.Contacts;

            string expected = JsonConvert.SerializeObject(angela, JsonNetSettings(TestReferenceHandling.Preserve));
            string actual = JsonSerializer.Serialize(angela, SystemTextJsonOptions(TestReferenceHandling.Preserve));

            Assert.Equal(expected, actual);
        }

        [Fact]
        public static void ObjectPreserveDuplicateArrays()
        {
            Employee angela = new Employee();

            angela.Subordinates = new List<Employee> { new Employee { Name = "Bob" } };
            angela.Subordinates2 = angela.Subordinates;

            string expected = JsonConvert.SerializeObject(angela, JsonNetSettings(TestReferenceHandling.Preserve));
            string actual = JsonSerializer.Serialize(angela, SystemTextJsonOptions(TestReferenceHandling.Preserve));

            Assert.Equal(expected, actual);
        }

        [Theory]
        [InlineData(TestReferenceHandling.Ignore)]
        [InlineData(TestReferenceHandling.Preserve)]
        //Check objects are correctly added/removed from the Hashset.
        public static void ObjectFoundTwiceOnSameDepth(TestReferenceHandling handling)
        {
            //Validate that the 'a' reference remains in the set when found somewhere else.
            //a--> b--> a
            //     └--> a  
            Employee angela = new Employee();
            Employee bob = new Employee();

            angela.Subordinates = new List<Employee> { bob };

            bob.Manager = angela;
            bob.Subordinates = new List<Employee> { angela };

            string expected = JsonConvert.SerializeObject(angela, JsonNetSettings(handling));
            string actual = JsonSerializer.Serialize(angela, SystemTextJsonOptions(handling));

            Assert.Equal(expected, actual);
        }
        #endregion

        #region Root Dictionary
        private class MyDictionary : Dictionary<string, MyDictionary> { }

        [Theory]
        [InlineData(TestReferenceHandling.Ignore)]
        [InlineData(TestReferenceHandling.Preserve)]
        public static void DictionaryLoop(TestReferenceHandling handling)
        {
            MyDictionary root = new MyDictionary();
            root["Self"] = root;
            root["Other"] = new MyDictionary();

            string expected = JsonConvert.SerializeObject(root, JsonNetSettings(handling));
            string actual = JsonSerializer.Serialize(root, SystemTextJsonOptions(handling));

            Assert.Equal(expected, actual);
        }

        [Theory]
        [InlineData(TestReferenceHandling.Ignore)]
        [InlineData(TestReferenceHandling.Preserve)]
        public static void DictionaryObjectLoop(TestReferenceHandling referenceHandling)
        {
            Dictionary<string, Employee> root = new Dictionary<string, Employee>();
            root["Angela"] = new Employee() { Name = "Angela", Contacts = root };

            string expected = JsonConvert.SerializeObject(root, JsonNetSettings(referenceHandling));
            string actual = JsonSerializer.Serialize(root, SystemTextJsonOptions(referenceHandling));

            Assert.Equal(expected, actual);
        }

        private class MyDictionaryArrayValues : Dictionary<string, List<MyDictionaryArrayValues>> { }

        [Theory]
        [InlineData(TestReferenceHandling.Ignore)]
        [InlineData(TestReferenceHandling.Preserve)]
        public static void DictionaryArrayLoop(TestReferenceHandling referenceHandling)
        {
            MyDictionaryArrayValues root = new MyDictionaryArrayValues();
            root["ArrayWithSelf"] = new List<MyDictionaryArrayValues> { root };

            string expected = JsonConvert.SerializeObject(root, JsonNetSettings(referenceHandling));
            string actual = JsonSerializer.Serialize(root, SystemTextJsonOptions(referenceHandling));

            Assert.Equal(expected, actual);
        }

        [Fact]
        public static void DictionaryPreserveDuplicateDictionaries()
        {
            MyDictionary root = new MyDictionary();
            root["Self1"] = root;
            root["Self2"] = root;

            string expected = JsonConvert.SerializeObject(root, JsonNetSettings(TestReferenceHandling.Preserve));
            string actual = JsonSerializer.Serialize(root, SystemTextJsonOptions(TestReferenceHandling.Preserve));

            Assert.Equal(expected, actual);
        }

        [Fact]
        public static void DictionaryPreserveDuplicateObjects()
        {
            Dictionary<string, Employee> root = new Dictionary<string, Employee>();
            root["Employee1"] = new Employee { Name = "Angela" };
            root["Employee2"] = root["Employee1"];

            string expected = JsonConvert.SerializeObject(root, JsonNetSettings(TestReferenceHandling.Preserve));
            string actual = JsonSerializer.Serialize(root, SystemTextJsonOptions(TestReferenceHandling.Preserve));

            Assert.Equal(expected, actual);
        }

        [Fact]
        public static void DictionaryPreserveDuplicateArrays()
        {
            MyDictionaryArrayValues root = new MyDictionaryArrayValues();
            root["Array1"] = new List<MyDictionaryArrayValues> { root };
            root["Array2"] = root["Array1"];

            string expected = JsonConvert.SerializeObject(root, JsonNetSettings(TestReferenceHandling.Preserve));
            string actual = JsonSerializer.Serialize(root, SystemTextJsonOptions(TestReferenceHandling.Preserve));

            Assert.Equal(expected, actual);
        }

        [Fact]
        public static void DictionaryNTimesUsingIgnore()
        {
            Dictionary<string, Employee> root = new Dictionary<string, Employee>();
            Employee elem = new Employee();
            elem.Contacts = root;
            elem.Contacts2 = root;

            root["angela"] = elem;

            string expected = JsonConvert.SerializeObject(root, JsonNetSettings(TestReferenceHandling.Ignore));
            string actual = JsonSerializer.Serialize(root, SystemTextJsonOptions(TestReferenceHandling.Ignore));

            Assert.Equal(expected, actual);
        }
        #endregion

        #region Root Array
        private class MyList : List<MyList> { }

        [Theory]
        [InlineData(TestReferenceHandling.Ignore)]
        [InlineData(TestReferenceHandling.Preserve)]
        public static void ArrayLoop(TestReferenceHandling referenceHandling)
        {
            MyList root = new MyList();
            root.Add(root);

            string expected = JsonConvert.SerializeObject(root, JsonNetSettings(referenceHandling));
            string actual = JsonSerializer.Serialize(root, SystemTextJsonOptions(referenceHandling));

            Assert.Equal(expected, actual);
        }

        [Theory]
        [InlineData(TestReferenceHandling.Ignore)]
        [InlineData(TestReferenceHandling.Preserve)]
        public static void ArrayObjectLoop(TestReferenceHandling referenceHandling)
        {
            List<Employee> root = new List<Employee>();
            root.Add(new Employee() { Name = "Angela", Subordinates = root });

            string expected = JsonConvert.SerializeObject(root, JsonNetSettings(referenceHandling));
            string actual = JsonSerializer.Serialize(root, SystemTextJsonOptions(referenceHandling));

            Assert.Equal(expected, actual);
        }

        private class MyListDictionaryValues : List<Dictionary<string, MyListDictionaryValues>> { }

        [Theory]
        [InlineData(TestReferenceHandling.Ignore)]
        [InlineData(TestReferenceHandling.Preserve)]
        public static void ArrayDictionaryLoop(TestReferenceHandling referenceHandling)
        {
            MyListDictionaryValues root = new MyListDictionaryValues();
            root.Add(new Dictionary<string, MyListDictionaryValues> { { "Root", root } });

            string expected = JsonConvert.SerializeObject(root, JsonNetSettings(referenceHandling));
            string actual = JsonSerializer.Serialize(root, SystemTextJsonOptions(referenceHandling));

            Assert.Equal(expected, actual);
        }

        [Fact]
        public static void ArrayPreserveDuplicateArrays()
        {
            MyList root = new MyList();
            root.Add(root);
            root.Add(root);
            root.Add(root);

            string expected = JsonConvert.SerializeObject(root, JsonNetSettings(TestReferenceHandling.Preserve));
            string actual = JsonSerializer.Serialize(root, SystemTextJsonOptions(TestReferenceHandling.Preserve));

            Assert.Equal(expected, actual);
        }

        [Fact]
        public static void ArrayPreserveDuplicateObjects()
        {
            List<Employee> root = new List<Employee>();
            root.Add(new Employee { Name = "Angela" });
            root.Add(root[0]);

            string expected = JsonConvert.SerializeObject(root, JsonNetSettings(TestReferenceHandling.Preserve));
            string actual = JsonSerializer.Serialize(root, SystemTextJsonOptions(TestReferenceHandling.Preserve));

            Assert.Equal(expected, actual);
        }

        [Fact]
        public static void ArrayPreserveDuplicateDictionaries()
        {
            MyListDictionaryValues root = new MyListDictionaryValues();
            root.Add(new Dictionary<string, MyListDictionaryValues>());
            root.Add(root[0]);

            string expected = JsonConvert.SerializeObject(root, JsonNetSettings(TestReferenceHandling.Preserve));
            string actual = JsonSerializer.Serialize(root, SystemTextJsonOptions(TestReferenceHandling.Preserve));

            Assert.Equal(expected, actual);
        }

        [Fact]//Check objects are correctly added/removed from the Hashset.
        public static void ObjectUnevenTimesUsingIgnore()
        {
            List<Employee> employees = new List<Employee>();

            Employee angela = new Employee();
            Employee bob = new Employee();

            bob.Manager = angela;
            angela.Manager = angela;

            employees.Add(bob);
            employees.Add(bob);
            employees.Add(bob);

            string expected = JsonConvert.SerializeObject(employees, JsonNetSettings(TestReferenceHandling.Ignore));
            string actual = JsonSerializer.Serialize(employees, SystemTextJsonOptions(TestReferenceHandling.Ignore));

            Assert.Equal(expected, actual);
        }

        [Fact]
        public static void ArrayNTimesUsingIgnore()
        {
            List<Employee> root = new List<Employee>();
            Employee elem = new Employee();
            elem.Subordinates = root;
            elem.Subordinates2 = root;

            root.Add(elem);

            string expected = JsonConvert.SerializeObject(root, JsonNetSettings(TestReferenceHandling.Ignore));
            string actual = JsonSerializer.Serialize(root, SystemTextJsonOptions(TestReferenceHandling.Ignore));

            Assert.Equal(expected, actual);
        }
        #endregion

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
            string actual = JsonSerializer.Serialize(array, SystemTextJsonOptions(TestReferenceHandling.Preserve));
            string expected = JsonSerializer.Serialize(array, SystemTextJsonOptions(TestReferenceHandling.Default));

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
        [ActiveIssue("Not yet created")]
        public static void DictionaryKeyContainingLeadingDollarSignShouldBeEncoded()
        {
            //$ Key in dictionary holding primitive type.
            Dictionary<string, object> dictionary = new Dictionary<string, object>();
            dictionary["$string"] = "Hello world";
            string json = JsonSerializer.Serialize(dictionary, SystemTextJsonOptions(TestReferenceHandling.Preserve));
            Assert.Equal(@"{""$id"":""1"",""\u0024string"":""Hello world""}", json);

            //$ Key in dictionary holding complex type.
            dictionary = new Dictionary<string, object>();
            dictionary["$object"] = new MyPoco { Hello = "World" };
            json = JsonSerializer.Serialize(dictionary, SystemTextJsonOptions(TestReferenceHandling.Preserve));
            Assert.Equal(@"{""$id"":""1"",""\u0024object"":{""$id"":""2"",""Hello"":""World""}}", json);

            //$ Key in ExtensionData dictionary
            MyPoco poco = new MyPoco();
            poco.ExtensionData["$string"] = "Hello world";
            poco.ExtensionData["$object"] = new MyPoco { Hello = "World" };
            json = JsonSerializer.Serialize(poco, SystemTextJsonOptions(TestReferenceHandling.Preserve));
            Assert.Equal(@"{""$id"":""1"",""\u0024string"":""Hello world"",""\u0024object"":{""$id"":""2"",""Hello"":""World""}}", json);

            //TODO:
            //Extend the scenarios to also cover CLR and F# properties with a leading $.
            //Also add scenarios where a NamingPolicy (DictionaryKey or Property) appends the leading $.
        }
        #endregion

        //utility
        private static JsonSerializerSettings JsonNetSettings(TestReferenceHandling referenceHandling)
        {
            switch(referenceHandling){
                case TestReferenceHandling.Default:
                    return _newtonsoftSerializeOptionsError;
                case TestReferenceHandling.Ignore:
                    return _newtonsoftSerializeOptionsIgnore;
                case TestReferenceHandling.Preserve:
                    return _newtonsoftSerializeOptionsPreserve;
            }

            return _newtonsoftSerializeOptionsError;
        }

        private static JsonSerializerOptions SystemTextJsonOptions(TestReferenceHandling referenceHandling)
        {
            switch (referenceHandling)
            {
                case TestReferenceHandling.Default:
                    return _serializeOptionsError;
                case TestReferenceHandling.Ignore:
                    return _serializeOptionsIgnore;
                case TestReferenceHandling.Preserve:
                    return _serializeOptionsPreserve;
            }

            return _serializeOptionsError;
        }
        //End utility

    }
}
