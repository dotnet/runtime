// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace System.Text.Json.Serialization.Tests
{
    public static class CyclicTests
    {
        [Fact]
        public static void WriteCyclicFailDefault()
        {
            TestClassWithCycle obj = new TestClassWithCycle();
            obj.Parent = obj;

            // We don't allow graph cycles; we throw JsonException instead of an unrecoverable StackOverflow.
            Assert.Throws<JsonException>(() => JsonSerializer.Serialize(obj));
        }

        [Theory]
        [InlineData(1, 2)]
        [InlineData(2, 3)]
        [InlineData(10, 11)]
        [InlineData(70, 71)]
        [InlineData(10, 0)] // 0 (or default) max depth is treated as 64
        public static void WriteCyclic(int objectHierarchyDepth, int maxDepth)
        {
            var rootObj = new TestClassWithCycle("root");
            CreateObjectHierarchy(1, objectHierarchyDepth, rootObj);

            var options = new JsonSerializerOptions();
            options.MaxDepth = maxDepth;

            string json = JsonSerializer.Serialize(rootObj, options);
            Assert.False(string.IsNullOrEmpty(json));
        }

        [Theory]
        [InlineData(1, 1, 0)]
        [InlineData(2, 2, 1)]
        [InlineData(10, 10, 9)]
        [InlineData(70, 70, 69)]
        [InlineData(70, 0, 63)] // 0 (or default) max depth is treated as 64
        public static void WriteCyclicFail(int objectHierarchyDepth, int maxDepth, int expectedPathDepth)
        {
            int effectiveMaxDepth = maxDepth == 0 ? 64 : maxDepth;
            var rootObj = new TestClassWithCycle("root");
            CreateObjectHierarchy(1, objectHierarchyDepth, rootObj);

            var options = new JsonSerializerOptions();
            options.MaxDepth = maxDepth;
            JsonException ex = Assert.Throws<JsonException>(() => JsonSerializer.Serialize(rootObj, options));

            // Exception should contain the path and MaxDepth.
            // Since the last Parent property is null, the serializer moves onto the Children property.
            string expectedPath = "$" + string.Concat(Enumerable.Repeat(".Parent", expectedPathDepth));
            Assert.Contains(expectedPath, ex.Path);
            Assert.Contains(effectiveMaxDepth.ToString(), ex.Message);
        }

        private static TestClassWithCycle CreateObjectHierarchy(int i, int max, TestClassWithCycle previous)
        {
            if (i == max)
            {
                return null;
            }

            var obj = new TestClassWithCycle(i.ToString());
            previous.Parent = obj;
            return CreateObjectHierarchy(++i, max, obj);
        }

        [Fact]
        public static void SimpleTypeCycle()
        {
            TestClassWithArrayOfElementsOfTheSameClass obj = new TestClassWithArrayOfElementsOfTheSameClass();

            // A cycle in just Types (not data) is allowed.
            string json = JsonSerializer.Serialize(obj);
            Assert.Equal(@"{""Array"":null}", json);
        }

        [Fact]
        public static void DeepTypeCycleWithRoundTrip()
        {
            TestClassWithCycle root = new TestClassWithCycle("root");
            TestClassWithCycle parent = new TestClassWithCycle("parent");
            root.Parent = parent;
            root.Children.Add(new TestClassWithCycle("child1"));
            root.Children.Add(new TestClassWithCycle("child2"));

            // A cycle in just Types (not data) is allowed.
            string json = JsonSerializer.Serialize(root);

            // Round-trip the JSON.
            TestClassWithCycle rootCopy = JsonSerializer.Deserialize<TestClassWithCycle>(json);
            Assert.Equal("root", rootCopy.Name);
            Assert.Equal(2, rootCopy.Children.Count);

            Assert.Equal("parent", rootCopy.Parent.Name);
            Assert.Equal(0, rootCopy.Parent.Children.Count);
            Assert.Null(rootCopy.Parent.Parent);

            Assert.Equal("child1", rootCopy.Children[0].Name);
            Assert.Equal(0, rootCopy.Children[0].Children.Count);
            Assert.Null(rootCopy.Children[0].Parent);

            Assert.Equal("child2", rootCopy.Children[1].Name);
            Assert.Equal(0, rootCopy.Children[1].Children.Count);
            Assert.Null(rootCopy.Children[1].Parent);
        }

        public class TestClassWithCycle
        {
            public TestClassWithCycle() { }

            public TestClassWithCycle(string name)
            {
                Name = name;
            }

            public TestClassWithCycle Parent { get; set; }
            public List<TestClassWithCycle> Children { get; set; } = new List<TestClassWithCycle>();
            public string Name { get; set; }
        }

        public class TestClassWithArrayOfElementsOfTheSameClass
        {
            public TestClassWithArrayOfElementsOfTheSameClass[] Array { get; set; }
        }

        public class CycleRoot
        {
            public Child1 Child1 { get; set; }
        }

        public class Child1
        {
            public IList<Child2> Child2IList { get; set; } = new List<Child2>();
            public List<Child2> Child2List { get; set; } = new List<Child2>();
            public Dictionary<string, Child2> Child2Dictionary { get; set; } = new Dictionary<string, Child2>();
            public Child2 Child2 { get; set; }
        }

        public class Child2
        {
            // Add properties that cause a cycle (Root->Child1->Child2->Child1)
            public Child1 Child1 { get; set; }
            public IList<Child1> Child1IList { get; set; }
            public IList<Child1> Child1List { get; set; }
            public Dictionary<string, Child1> Child1Dictionary { get; set; }
        }

        [Fact]
        public static void MultiClassCycle()
        {
            CycleRoot root = new CycleRoot();
            root.Child1 = new Child1();
            root.Child1.Child2IList.Add(new Child2());
            root.Child1.Child2List.Add(new Child2());
            root.Child1.Child2Dictionary.Add("0", new Child2());
            root.Child1.Child2 = new Child2();
            root.Child1.Child2.Child1 = new Child1();

            // A cycle in just Types (not data) is allowed.
            string json = JsonSerializer.Serialize(root);

            root = JsonSerializer.Deserialize<CycleRoot>(json);
            Assert.NotNull(root.Child1);
            Assert.NotNull(root.Child1.Child2IList[0]);
            Assert.NotNull(root.Child1.Child2List[0]);
            Assert.NotNull(root.Child1.Child2Dictionary["0"]);
            Assert.NotNull(root.Child1.Child2);
            Assert.NotNull(root.Child1.Child2.Child1);

            // Round-trip
            string jsonRoundTrip = JsonSerializer.Serialize(root);
            Assert.Equal(json, jsonRoundTrip);
        }
    }
}
