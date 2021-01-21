// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Threading.Tasks;
using Xunit;

namespace System.Text.Json.Serialization.Tests
{
    public class ReferenceHandlerTests_IgnoreCycle
    {
        private static readonly JsonSerializerOptions s_optionsIgnoreCycles =
            new JsonSerializerOptions { ReferenceHandler = ReferenceHandler.IgnoreCycle };

        [Fact]
        public async Task IgnoreCycles_OnObject()
        {
            var root = new Node();
            root.Next = root;

            await Test_Serialize_And_SerializeAsync(root, @"{""Next"":null}", s_optionsIgnoreCycles);
        }

        [Fact]
        public async Task IgnoreCycles_OnObject_NonProperty()
        {
            var node = new Node();
            var rootList = new List<object>() { node };
            node.Next = rootList;

            await Test_Serialize_And_SerializeAsync(rootList, @"[{""Next"":null}]", s_optionsIgnoreCycles);
        }

        [Theory]
        [InlineData(typeof(Dictionary<string, object>))]
        [InlineData(typeof(GenericIDictionaryWrapper<string, object>))]
        public async Task IgnoreCycles_OnDictionary(Type typeToSerialize)
        {
            var root = (ICollection<KeyValuePair<string, object>>)Activator.CreateInstance(typeToSerialize);
            root.Add(new KeyValuePair<string, object>("self", root));

            await Test_Serialize_And_SerializeAsync(root, @"{""self"":null}", s_optionsIgnoreCycles);
        }

        [Fact]
        public async Task IgnoreCycles_OnReadOnlyDictionary()
        {
            var innerDictionary = new Dictionary<string, object>();
            var root = new ReadOnlyDictionary<string, object>(innerDictionary);

            innerDictionary.Add("self", root);
            await Test_Serialize_And_SerializeAsync(root, @"{""self"":null}", s_optionsIgnoreCycles);
        }

        [Fact]
        public async Task IgnoreCycles_OnIDictionary()
        {
            var root = new WrapperForIDictionary();
            root.Add("self", root);
            await Test_Serialize_And_SerializeAsync(root, @"{""self"":null}", s_optionsIgnoreCycles);
        }

        [Fact]
        public async Task IgnoreCycles_OnArray()
        {
            var root = new object[1];
            root[0] = root;
            await Test_Serialize_And_SerializeAsync(root, "[null]", s_optionsIgnoreCycles);
        }

        [Theory]
        [InlineData(typeof(List<object>))]
        [InlineData(typeof(GenericIListWrapper<object>))]
        public async Task IgnoreCycles_OnLists(Type typeToSerialize)
        {
            var root = (IList<object>)Activator.CreateInstance(typeToSerialize);
            root.Add(root);
            await Test_Serialize_And_SerializeAsync(root, "[null]", s_optionsIgnoreCycles);
        }

        [Theory]
        [InlineData(typeof(GenericISetWrapper<object>))]
        [InlineData(typeof(GenericICollectionWrapper<object>))]
        public async Task IgnoreCycles_OnCollections(Type typeToSerialize)
        {
            var root = (ICollection<object>)Activator.CreateInstance(typeToSerialize);
            root.Add(root);
            await Test_Serialize_And_SerializeAsync(root, "[null]", s_optionsIgnoreCycles);
        }

        [Fact]
        public async Task IgnoreCycles_OnCollections_WithoutAddMethod()
        {
            var root = new Stack<object>();
            root.Push(root);
            await Test_Serialize_And_SerializeAsync(root, "[null]", s_optionsIgnoreCycles);

            var root2 = new Queue<object>();
            root2.Enqueue(root2);
            await Test_Serialize_And_SerializeAsync(root2, "[null]", s_optionsIgnoreCycles);

            var root3 = new ConcurrentStack<object>();
            root3.Push(root3);
            await Test_Serialize_And_SerializeAsync(root3, "[null]", s_optionsIgnoreCycles);

            var root4 = new ConcurrentQueue<object>();
            root4.Enqueue(root4);
            await Test_Serialize_And_SerializeAsync(root4, "[null]", s_optionsIgnoreCycles);

            var root5 = new Stack();
            root5.Push(root5);
            await Test_Serialize_And_SerializeAsync(root5, "[null]", s_optionsIgnoreCycles);

            var root6 = new Queue();
            root6.Enqueue(root6);
            await Test_Serialize_And_SerializeAsync(root6, "[null]", s_optionsIgnoreCycles);
        }

        [Fact]
        public async Task IgnoreCycles_OnExtensionData()
        {
            var root = new EmptyClassWithExtensionProperty();
            root.MyOverflow.Add("root", root);
            await Test_Serialize_And_SerializeAsync(root, @"{""root"":null}", s_optionsIgnoreCycles);
        }

        [Fact]
        public async Task IgnoreCycles_OnBoxedValueTypes()
        {
            IValueNode root = new ValueNode();
            root.Next = root;

            await Test_Serialize_And_SerializeAsync(root, @"{""Next"":null}", s_optionsIgnoreCycles);
        }

        [Fact]
        public async Task IgnoreCycles_OnBoxedValueTypes_AsProperty()
        {
            IValueNode node = new ValueNode();
            node.Next = node;

            var root = new Node();
            root.Next = node;

            await Test_Serialize_And_SerializeAsync(root, @"{""Next"":{""Next"":null}}", s_optionsIgnoreCycles);
        }

        [Fact]
        public async Task IgnoreCycles_DoesNotSupportPreserveSemantics()
        {
            // Object
            var node = new NodeWithExtensionData();
            node.Next = node;
            string json = SerializeWithPreserve(node);

            node = JsonSerializer.Deserialize<NodeWithExtensionData>(json, s_optionsIgnoreCycles);
            Assert.True(node.MyOverflow.ContainsKey("$id"));
            Assert.True(node.Next.MyOverflow.ContainsKey("$ref"));

            using var ms = new MemoryStream(Encoding.UTF8.GetBytes(json));
            node = await JsonSerializer.DeserializeAsync<NodeWithExtensionData>(ms, s_optionsIgnoreCycles);
            Assert.True(node.MyOverflow.ContainsKey("$id"));
            Assert.True(node.Next.MyOverflow.ContainsKey("$ref"));

            // Dictionary
            var dictionary = new RecursiveDictionary();
            dictionary.Add("self", dictionary);
            json = SerializeWithPreserve(dictionary);

            Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<RecursiveDictionary>(json, s_optionsIgnoreCycles));
            using var ms2 = new MemoryStream(Encoding.UTF8.GetBytes(json));
            await Assert.ThrowsAsync<JsonException>(() => JsonSerializer.DeserializeAsync<RecursiveDictionary>(ms2, s_optionsIgnoreCycles).AsTask());

            // List
            var list = new RecursiveList();
            list.Add(list);
            json = SerializeWithPreserve(list);

            Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<RecursiveList>(json, s_optionsIgnoreCycles));
            using var ms3 = new MemoryStream(Encoding.UTF8.GetBytes(json));
            await Assert.ThrowsAsync<JsonException>(() => JsonSerializer.DeserializeAsync<RecursiveList>(ms3, s_optionsIgnoreCycles).AsTask());
        }

        [Fact]
        public async Task IgnoreCycles_DoesNotSupportPreserveSemantics_Polymorphic()
        {
            // Object
            var node = new Node();
            node.Next = node;
            string json = SerializeWithPreserve(node);

            node = JsonSerializer.Deserialize<Node>(json, s_optionsIgnoreCycles);
            JsonElement nodeAsJsonElement = Assert.IsType<JsonElement>(node.Next);
            Assert.True(nodeAsJsonElement.GetProperty("$ref").GetString() == "1");

            using var ms = new MemoryStream(Encoding.UTF8.GetBytes(json));
            node = await JsonSerializer.DeserializeAsync<Node>(ms, s_optionsIgnoreCycles);
            nodeAsJsonElement = Assert.IsType<JsonElement>(node.Next);
            Assert.True(nodeAsJsonElement.GetProperty("$ref").GetString() == "1");

            // Dictionary
            var dictionary = new Dictionary<string, object>();
            dictionary.Add("self", dictionary);
            json = SerializeWithPreserve(dictionary);

            dictionary = JsonSerializer.Deserialize<Dictionary<string, object>>(json, s_optionsIgnoreCycles);
        }

        private string SerializeWithPreserve<T>(T value)
        {
            var opts = new JsonSerializerOptions { ReferenceHandler = ReferenceHandler.Preserve };
            return JsonSerializer.Serialize(value, opts);
        }

        // Test for correctness when the object reference is found on a sibling branch.
        [Theory]
        [InlineData(typeof(EmptyClass), "{}")]
        [InlineData(typeof(EmptyStruct), "{}")]
        [InlineData(typeof(object), "{}")]
        [InlineData(typeof(Dictionary<string,object>), "{}")]
        [InlineData(typeof(List<string>), "[]")]
        public async Task AlreadySeenInstance_ShouldNotBeIgnoredOnSecondBranch(Type objectType, string objectPayload)
        {
            object obj = Activator.CreateInstance(objectType);
            var root = new TreeNode();
            root.Left = obj;
            root.Right = obj;

            await Test_Serialize_And_SerializeAsync(root, $@"{{""Left"":{objectPayload},""Right"":{objectPayload}}}", s_optionsIgnoreCycles);
        }

        [Fact]
        public async Task IgnoreCycles_WhenWritingNull()
        {
            var opts = new JsonSerializerOptions
            {
                ReferenceHandler = ReferenceHandler.IgnoreCycle,
                DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
            };

            // Reference cycles are treated as null, hence the JsonIgnoreCondition can be used to actually ignore the property.
            var rootObj = new Node();
            rootObj.Next = rootObj;

            await Test_Serialize_And_SerializeAsync(rootObj, "{}", opts);

            // JsonIgnoreCondition does not ignore nulls in collections, hence a reference loop should not omit writing it.
            // This also helps us to avoid changing the length of a collection when the loop is detected in one of the elements.
            var rootList = new List<object>();
            rootList.Add(rootList);

            await Test_Serialize_And_SerializeAsync(rootList, "[null]", opts);

            var rootDictionary = new Dictionary<string, object>();
            rootDictionary.Add("self", rootDictionary);

            await Test_Serialize_And_SerializeAsync(rootDictionary, @"{""self"":null}", opts);
        }

        private async Task Test_Serialize_And_SerializeAsync(object obj, string expected, JsonSerializerOptions options)
        {
            string json = JsonSerializer.Serialize(obj, options);
            Assert.Equal(expected, json);

            using var ms = new MemoryStream();
            await JsonSerializer.SerializeAsync(ms, obj, options).ConfigureAwait(false);
            json = Encoding.UTF8.GetString(ms.ToArray());
            Assert.Equal(expected, json);
        }

        private class Employee
        {
            public string Name { get; set; }
            public Employee Manager { get; set; }
            public List<Employee> Subordinates { get; set; }
            public Dictionary<string, Employee> Colleagues { get; set; }
        }

        private class Node
        {
            public object Next { get; set; }
        }

        private class TreeNode
        {
            public object Left { get; set; }
            public object Right { get; set; }
        }

        private struct ValueNode : IValueNode
        {
            public object Next { get; set; }
        }

        interface IValueNode
        {
            public object Next { get; set; }
        }

        private class EmptyClass { }
        private struct EmptyStruct { }

        private class EmptyClassWithExtensionProperty
        {
            [JsonExtensionData]
            public Dictionary<string, object> MyOverflow { get; set; } = new Dictionary<string, object>();
        }

        private class NodeWithExtensionData
        {
            [JsonExtensionData]
            public Dictionary<string, object> MyOverflow { get; set; } = new Dictionary<string, object>();
            public NodeWithExtensionData Next { get; set; }
        }

        private class RecursiveDictionary : Dictionary<string, RecursiveDictionary> { }

        private class RecursiveList : List<RecursiveList> { }
    }
}
