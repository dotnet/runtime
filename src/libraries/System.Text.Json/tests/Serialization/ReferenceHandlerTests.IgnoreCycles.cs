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
    public class ReferenceHandlerTests_IgnoreCycles
    {
        private static readonly JsonSerializerOptions s_optionsIgnoreCycles =
            new JsonSerializerOptions { ReferenceHandler = ReferenceHandler.IgnoreCycles };

        [Fact]
        public async Task IgnoreCycles_OnObject()
        {
            await Verify<NodeWithNodeProperty>();
            await Verify<NodeWithObjectProperty>();

            async Task Verify<T>() where T : class, new()
            {
                T root = new T();
                SetNextProperty(typeof(T), root, root);

                await Test_Serialize_And_SerializeAsync(root, @"{""Next"":null}", s_optionsIgnoreCycles);

                // Verify that object property is not mutated on serialization.
                object rootNext = GetNextProperty(typeof(T), root);
                Assert.NotNull(rootNext);
                Assert.Same(rootNext, root);
            }
        }

        [Fact]
        public async Task IgnoreCycles_OnObject_AsProperty()
        {
            await Verify<NodeWithNodeProperty>();
            await Verify<NodeWithObjectProperty>();

            async Task Verify<T>() where T : class, new()
            {
                var node = new T();
                SetNextProperty(typeof(T), node, node);

                var root = new ClassWithGenericProperty<T>();
                root.Foo = node;
                await Test_Serialize_And_SerializeAsync(root, expected: @"{""Foo"":{""Next"":null}}", s_optionsIgnoreCycles);

                object nodeNext = GetNextProperty(typeof(T), node);
                Assert.NotNull(nodeNext);
                Assert.Same(nodeNext, node);

                var rootWithObjProperty = new ClassWithGenericProperty<object>();
                rootWithObjProperty.Foo = node;
                await Test_Serialize_And_SerializeAsync(rootWithObjProperty, expected: @"{""Foo"":{""Next"":null}}", s_optionsIgnoreCycles);

                nodeNext = GetNextProperty(typeof(T), node);
                Assert.NotNull(nodeNext);
                Assert.Same(nodeNext, node);
            }
        }

        [Fact]
        public async Task IgnoreCycles_OnBoxedValueType()
        {
            await Verify<ValueNodeWithIValueNodeProperty>();
            await Verify<ValueNodeWithObjectProperty>();

            async Task Verify<T>() where T : new()
            {
                object root = new T();
                SetNextProperty(typeof(T), root, root);
                await Test_Serialize_And_SerializeAsync(root, expected: @"{""Next"":null}", s_optionsIgnoreCycles);

                object rootNext = GetNextProperty(typeof(T), root);
                Assert.NotNull(rootNext);
                Assert.Same(rootNext, root);
            }
        }

        [Fact]
        public async Task IgnoreCycles_OnBoxedValueType_Interface()
        {
            IValueNodeWithIValueNodeProperty root = new ValueNodeWithIValueNodeProperty();
            root.Next = root;
            await Test_Serialize_And_SerializeAsync(root, expected: @"{""Next"":null}", s_optionsIgnoreCycles);

            IValueNodeWithObjectProperty root2 = new ValueNodeWithObjectProperty();
            root2.Next = root2;
            await Test_Serialize_And_SerializeAsync(root2, expected: @"{""Next"":null}", s_optionsIgnoreCycles);
        }

        [Fact]
        public async Task IgnoreCycles_OnBoxedValueType_AsProperty()
        {
            await Verify<ValueNodeWithIValueNodeProperty>();
            await Verify<ValueNodeWithObjectProperty>();

            async Task Verify<T>() where T : new()
            {
                object node = new T();
                SetNextProperty(typeof(T), node, node);

                var rootWithObjProperty = new ClassWithGenericProperty<object>();
                rootWithObjProperty.Foo = node;
                await Test_Serialize_And_SerializeAsync(rootWithObjProperty, expected: @"{""Foo"":{""Next"":null}}", s_optionsIgnoreCycles);

                object nodeNext = GetNextProperty(typeof(T), node);
                Assert.NotNull(nodeNext);
                Assert.Same(nodeNext, node);
            }
        }

        [Theory]
        [InlineData(typeof(Dictionary<string, object>))]
        [InlineData(typeof(GenericIDictionaryWrapper<string, object>))]
        public async Task IgnoreCycles_OnDictionary(Type typeToSerialize)
        {
            var root = (IDictionary<string, object>)Activator.CreateInstance(typeToSerialize);
            root.Add("self", root);

            await Test_Serialize_And_SerializeAsync(root, @"{""self"":null}", s_optionsIgnoreCycles);
        }

        [Fact]
        public async Task IgnoreCycles_OnRecursiveDictionary()
        {
            var root = new RecursiveDictionary();
            root.Add("self", root);

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
        public async Task IgnoreCycles_OnList(Type typeToSerialize)
        {
            var root = (IList<object>)Activator.CreateInstance(typeToSerialize);
            root.Add(root);
            await Test_Serialize_And_SerializeAsync(root, "[null]", s_optionsIgnoreCycles);
        }

        [Fact]
        public async Task IgnoreCycles_OnRecursiveList()
        {
            var root = new RecursiveList();
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
            var node = new NodeWithObjectProperty();
            node.Next = node;
            string json = SerializeWithPreserve(node);

            node = JsonSerializer.Deserialize<NodeWithObjectProperty>(json, s_optionsIgnoreCycles);
            JsonElement nodeAsJsonElement = Assert.IsType<JsonElement>(node.Next);
            Assert.True(nodeAsJsonElement.GetProperty("$ref").GetString() == "1");

            using var ms = new MemoryStream(Encoding.UTF8.GetBytes(json));
            node = await JsonSerializer.DeserializeAsync<NodeWithObjectProperty>(ms, s_optionsIgnoreCycles);
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

        [Fact]
        public async Task AlreadySeenInstance_ShouldNotBeIgnoredOnSiblingBranch()
        {
            await Verify<EmptyClass>(expectedPayload: "{}");
            await Verify<EmptyStruct>(expectedPayload: "{}");
            await Verify<object>(expectedPayload: "{}");
            await Verify<Dictionary<string, object>>(expectedPayload: "{}");
            await Verify<List<string>>(expectedPayload: "[]");

            async Task Verify<T>(string expectedPayload) where T : new()
            {
                T value = new();
                var root = new TreeNode<T> { Left = value, Right = value };
                await Test_Serialize_And_SerializeAsync(root, $@"{{""Left"":{expectedPayload},""Right"":{expectedPayload}}}", s_optionsIgnoreCycles);

                var rootWithObjectProperties = new TreeNode<object> { Left = value, Right = value };
                await Test_Serialize_And_SerializeAsync(rootWithObjectProperties, $@"{{""Left"":{expectedPayload},""Right"":{expectedPayload}}}", s_optionsIgnoreCycles);
            }
        }

        [Fact]
        public async Task IgnoreCycles_WhenWritingNull()
        {
            var opts = new JsonSerializerOptions
            {
                ReferenceHandler = ReferenceHandler.IgnoreCycles,
                DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
            };

            // Reference cycles are treated as null, hence the JsonIgnoreCondition can be used to actually ignore the property.
            var rootObj = new NodeWithObjectProperty();
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

        private async Task Test_Serialize_And_SerializeAsync<T>(T obj, string expected, JsonSerializerOptions options)
        {
            string json;
            Type objType = typeof(T);

            if (objType != typeof(object))
            {
                json = JsonSerializer.Serialize(obj, options);
                Assert.Equal(expected, json);

                using var ms1 = new MemoryStream();
                await JsonSerializer.SerializeAsync(ms1, obj, options).ConfigureAwait(false);
                json = Encoding.UTF8.GetString(ms1.ToArray());
                Assert.Equal(expected, json);
            }

            json = JsonSerializer.Serialize(obj, objType, options);
            Assert.Equal(expected, json);

            using var ms2 = new MemoryStream();
            await JsonSerializer.SerializeAsync(ms2, obj, objType, options).ConfigureAwait(false);
            json = Encoding.UTF8.GetString(ms2.ToArray());
            Assert.Equal(expected, json);
        }

        private const string Next = nameof(Next);
        private void SetNextProperty(Type type, object obj, object value)
        {
            type.GetProperty(Next).SetValue(obj, value);
        }

        private object GetNextProperty(Type type, object obj)
        {
            return type.GetProperty(Next).GetValue(obj);
        }

        private class NodeWithObjectProperty
        {
            public object Next { get; set; }
        }

        private class NodeWithNodeProperty
        {
            public NodeWithNodeProperty Next { get; set; }
        }

        private class ClassWithGenericProperty<T>
        {
            public T Foo { get; set; }
        }

        private class TreeNode<T>
        {
            public T Left { get; set; }
            public T Right { get; set; }
        }

        interface IValueNodeWithObjectProperty
        {
            public object Next { get; set; }
        }

        private struct ValueNodeWithObjectProperty : IValueNodeWithObjectProperty
        {
            public object Next { get; set; }
        }

        interface IValueNodeWithIValueNodeProperty
        {
            public IValueNodeWithIValueNodeProperty Next { get; set; }
        }

        private struct ValueNodeWithIValueNodeProperty : IValueNodeWithIValueNodeProperty
        {
            public IValueNodeWithIValueNodeProperty Next { get; set; }
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
