﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using Newtonsoft.Json;
using Xunit;

namespace System.Text.Json.Tests
{
    public static partial class ReferenceHandlingTests
    {

        [Fact]
        public static void ThrowByDefaultOnLoop()
        {
            Employee a = new Employee();
            a.Manager = a;

            JsonException ex = Assert.Throws<JsonException>(() => JsonSerializer.Serialize(a));
            //TODO: Change default throw error msg in order to state that you can deal with loops with any of the other RefHandling options.
        }

        #region Root Object
        [Fact]
        public static void ObjectLoop()
        {
            Employee angela = new Employee();
            angela.Manager = angela;

            // Compare parity with Newtonsoft.Json
            string expected = JsonConvert.SerializeObject(angela, _newtonsoftSerializeOptionsPreserve);
            string actual = JsonSerializer.Serialize(angela, _serializeOptionsPreserve);

            Assert.Equal(expected, actual);

            // Ensure round-trip
            Employee angelaCopy = JsonSerializer.Deserialize<Employee>(actual, _serializeOptionsPreserve);
            Assert.Same(angelaCopy.Manager, angelaCopy);
        }

        [Fact]
        public static void ObjectArrayLoop()
        {
            Employee angela = new Employee();
            angela.Subordinates = new List<Employee> { angela };

            string expected = JsonConvert.SerializeObject(angela, _newtonsoftSerializeOptionsPreserve);
            string actual = JsonSerializer.Serialize(angela, _serializeOptionsPreserve);

            Assert.Equal(expected, actual);

            Employee angelaCopy = JsonSerializer.Deserialize<Employee>(actual, _serializeOptionsPreserve);
            Assert.Same(angelaCopy.Subordinates[0], angelaCopy);
        }

        [Fact]
        public static void ObjectDictionaryLoop()
        {
            Employee angela = new Employee();
            angela.Contacts = new Dictionary<string, Employee> { { "555-5555", angela } };

            string expected = JsonConvert.SerializeObject(angela, _newtonsoftSerializeOptionsPreserve);
            string actual = JsonSerializer.Serialize(angela, _serializeOptionsPreserve);

            Assert.Equal(expected, actual);

            Employee angelaCopy = JsonSerializer.Deserialize<Employee>(actual, _serializeOptionsPreserve);
            Assert.Same(angelaCopy.Contacts["555-5555"], angelaCopy);
        }

        [Fact]
        public static void ObjectPreserveDuplicateObjects()
        {
            Employee angela = new Employee();

            angela.Manager = new Employee { Name = "Bob" };
            angela.Manager2 = angela.Manager;



            string expected = JsonConvert.SerializeObject(angela, _newtonsoftSerializeOptionsPreserve);
            string actual = JsonSerializer.Serialize(angela, _serializeOptionsPreserve);

            Assert.Equal(expected, actual);

            Employee angelaCopy = JsonSerializer.Deserialize<Employee>(actual, _serializeOptionsPreserve);
            Assert.Same(angelaCopy.Manager, angelaCopy.Manager2);
        }

        [Fact]
        public static void ObjectPreserveDuplicateDictionaries()
        {
            Employee angela = new Employee();

            angela.Contacts = new Dictionary<string, Employee> { { "444-4444", new Employee { Name = "Bob" } } };
            angela.Contacts2 = angela.Contacts;

            string expected = JsonConvert.SerializeObject(angela, _newtonsoftSerializeOptionsPreserve);
            string actual = JsonSerializer.Serialize(angela, _serializeOptionsPreserve);

            Assert.Equal(expected, actual);

            Employee angelaCopy = JsonSerializer.Deserialize<Employee>(actual, _serializeOptionsPreserve);
            Assert.Same(angelaCopy.Contacts, angelaCopy.Contacts2);
        }

        [Fact]
        public static void ObjectPreserveDuplicateArrays()
        {
            Employee angela = new Employee();

            angela.Subordinates = new List<Employee> { new Employee { Name = "Bob" } };
            angela.Subordinates2 = angela.Subordinates;

            string expected = JsonConvert.SerializeObject(angela, _newtonsoftSerializeOptionsPreserve);
            string actual = JsonSerializer.Serialize(angela, _serializeOptionsPreserve);

            Assert.Equal(expected, actual);

            Employee angelaCopy = JsonSerializer.Deserialize<Employee>(actual, _serializeOptionsPreserve);
            Assert.Same(angelaCopy.Subordinates, angelaCopy.Subordinates2);
        }

        [Fact]
        public static void KeyValuePairTest()
        {
            KeyValuePair<string, string> kvp = new KeyValuePair<string, string>("key", "value");
            string json = JsonSerializer.Serialize(kvp, _deserializeOptions);
            KeyValuePair<string, string> kvp2 = JsonSerializer.Deserialize<KeyValuePair<string, string>>(json, _deserializeOptions);

            Assert.Equal(kvp.Key, kvp2.Key);
            Assert.Equal(kvp.Value, kvp2.Value);
        }
        #endregion Root Object

        #region Root Dictionary
        private class MyDictionary : Dictionary<string, MyDictionary> { }

        [Fact]
        public static void DictionaryLoop()
        {
            MyDictionary root = new MyDictionary();
            root["Self"] = root;
            root["Other"] = new MyDictionary();

            string expected = JsonConvert.SerializeObject(root, _newtonsoftSerializeOptionsPreserve);
            string actual = JsonSerializer.Serialize(root, _serializeOptionsPreserve);

            Assert.Equal(expected, actual);

            MyDictionary rootCopy = JsonSerializer.Deserialize<MyDictionary>(actual, _serializeOptionsPreserve);
            Assert.Same(rootCopy, rootCopy["Self"]);
        }

        [Fact]
        public static void DictionaryPreserveDuplicateDictionaries()
        {
            MyDictionary root = new MyDictionary();
            root["Self1"] = root;
            root["Self2"] = root;

            string expected = JsonConvert.SerializeObject(root, _newtonsoftSerializeOptionsPreserve);
            string actual = JsonSerializer.Serialize(root, _serializeOptionsPreserve);

            Assert.Equal(expected, actual);

            MyDictionary rootCopy = JsonSerializer.Deserialize<MyDictionary>(actual, _serializeOptionsPreserve);
            Assert.Same(rootCopy, rootCopy["Self1"]);
            Assert.Same(rootCopy, rootCopy["Self2"]);
        }

        [Fact]
        public static void DictionaryObjectLoop()
        {
            Dictionary<string, Employee> root = new Dictionary<string, Employee>();
            root["Angela"] = new Employee() { Name = "Angela", Contacts = root };

            string expected = JsonConvert.SerializeObject(root, _newtonsoftSerializeOptionsPreserve);
            string actual = JsonSerializer.Serialize(root, _serializeOptionsPreserve);

            Assert.Equal(expected, actual);

            Dictionary<string, Employee> rootCopy = JsonSerializer.Deserialize<Dictionary<string, Employee>>(actual, _serializeOptionsPreserve);
            Assert.Same(rootCopy, rootCopy["Angela"].Contacts);
        }

        private class MyDictionaryArrayValues : Dictionary<string, List<MyDictionaryArrayValues>> { }

        [Fact]
        public static void DictionaryArrayLoop()
        {
            MyDictionaryArrayValues root = new MyDictionaryArrayValues();
            root["ArrayWithSelf"] = new List<MyDictionaryArrayValues> { root };

            string expected = JsonConvert.SerializeObject(root, _newtonsoftSerializeOptionsPreserve);
            string actual = JsonSerializer.Serialize(root, _serializeOptionsPreserve);

            Assert.Equal(expected, actual);

            MyDictionaryArrayValues rootCopy = JsonSerializer.Deserialize<MyDictionaryArrayValues>(actual, _serializeOptionsPreserve);
            Assert.Same(rootCopy, rootCopy["ArrayWithSelf"][0]);
        }

        [Fact]
        public static void DictionaryPreserveDuplicateArrays()
        {
            MyDictionaryArrayValues root = new MyDictionaryArrayValues();
            root["Array1"] = new List<MyDictionaryArrayValues> { root };
            root["Array2"] = root["Array1"];

            string expected = JsonConvert.SerializeObject(root, _newtonsoftSerializeOptionsPreserve);
            string actual = JsonSerializer.Serialize(root, _serializeOptionsPreserve);

            Assert.Equal(expected, actual);

            MyDictionaryArrayValues rootCopy = JsonSerializer.Deserialize<MyDictionaryArrayValues>(actual, _serializeOptionsPreserve);
            Assert.Same(rootCopy, rootCopy["Array1"][0]);
            Assert.Same(rootCopy["Array2"], rootCopy["Array1"]);
        }

        [Fact]
        public static void DictionaryPreserveDuplicateObjects()
        {
            Dictionary<string, Employee> root = new Dictionary<string, Employee>();
            root["Employee1"] = new Employee { Name = "Angela" };
            root["Employee2"] = root["Employee1"];

            string expected = JsonConvert.SerializeObject(root, _newtonsoftSerializeOptionsPreserve);
            string actual = JsonSerializer.Serialize(root, _serializeOptionsPreserve);

            Assert.Equal(expected, actual);

            Dictionary<string, Employee> rootCopy = JsonSerializer.Deserialize<Dictionary<string, Employee>>(actual, _serializeOptionsPreserve);
            Assert.Same(rootCopy["Employee1"], rootCopy["Employee2"]);
        }
        #endregion

        #region Root Array
        private class MyList : List<MyList> { }

        [Fact]
        public static void ArrayLoop()
        {
            MyList root = new MyList();
            root.Add(root);

            string expected = JsonConvert.SerializeObject(root, _newtonsoftSerializeOptionsPreserve);
            string actual = JsonSerializer.Serialize(root, _serializeOptionsPreserve);

            Assert.Equal(expected, actual);

            MyList rootCopy = JsonSerializer.Deserialize<MyList>(actual, _serializeOptionsPreserve);
            Assert.Same(rootCopy, rootCopy[0]);

            // Duplicate reference
            root = new MyList();
            root.Add(root);
            root.Add(root);
            root.Add(root);

            expected = JsonConvert.SerializeObject(root, _newtonsoftSerializeOptionsPreserve);
            actual = JsonSerializer.Serialize(root, _serializeOptionsPreserve);

            Assert.Equal(expected, actual);

            rootCopy = JsonSerializer.Deserialize<MyList>(actual, _serializeOptionsPreserve);
            Assert.Same(rootCopy, rootCopy[0]);
            Assert.Same(rootCopy, rootCopy[1]);
            Assert.Same(rootCopy, rootCopy[2]);
        }

        [Fact]
        public static void ArrayObjectLoop()
        {
            List<Employee> root = new List<Employee>();
            root.Add(new Employee() { Name = "Angela", Subordinates = root });

            string expected = JsonConvert.SerializeObject(root, _newtonsoftSerializeOptionsPreserve);
            string actual = JsonSerializer.Serialize(root, _serializeOptionsPreserve);

            Assert.Equal(expected, actual);

            List<Employee> rootCopy = JsonSerializer.Deserialize<List<Employee>>(actual, _serializeOptionsPreserve);
            Assert.Same(rootCopy, rootCopy[0].Subordinates);
        }

        [Fact]
        public static void ArrayPreserveDuplicateObjects()
        {
            List<Employee> root = new List<Employee>();
            root.Add(new Employee { Name = "Angela" });
            root.Add(root[0]);

            string expected = JsonConvert.SerializeObject(root, _newtonsoftSerializeOptionsPreserve);
            string actual = JsonSerializer.Serialize(root, _serializeOptionsPreserve);

            Assert.Equal(expected, actual);

            List<Employee> rootCopy = JsonSerializer.Deserialize<List<Employee>>(actual, _serializeOptionsPreserve);
            Assert.Same(rootCopy[0], rootCopy[1]);
        }

        private class MyListDictionaryValues : List<Dictionary<string, MyListDictionaryValues>> { }

        [Fact]
        public static void ArrayDictionaryLoop()
        {
            MyListDictionaryValues root = new MyListDictionaryValues();
            root.Add(new Dictionary<string, MyListDictionaryValues> { { "Root", root } });

            string expected = JsonConvert.SerializeObject(root, _newtonsoftSerializeOptionsPreserve);
            string actual = JsonSerializer.Serialize(root, _serializeOptionsPreserve);

            Assert.Equal(expected, actual);

            MyListDictionaryValues rootCopy = JsonSerializer.Deserialize<MyListDictionaryValues>(actual, _serializeOptionsPreserve);
            Assert.Same(rootCopy, rootCopy[0]["Root"]);
        }

        [Fact]
        public static void ArrayPreserveDuplicateDictionaries()
        {
            MyListDictionaryValues root = new MyListDictionaryValues();
            root.Add(new Dictionary<string, MyListDictionaryValues>());
            root.Add(root[0]);

            string expected = JsonConvert.SerializeObject(root, _newtonsoftSerializeOptionsPreserve);
            string actual = JsonSerializer.Serialize(root, _serializeOptionsPreserve);

            Assert.Equal(expected, actual);

            MyListDictionaryValues rootCopy = JsonSerializer.Deserialize<MyListDictionaryValues>(actual, _serializeOptionsPreserve);
            Assert.Same(rootCopy[0], rootCopy[1]);
        }
        #endregion
    }
}
