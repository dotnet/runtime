// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using Xunit;

namespace System.Text.Json.Serialization.Tests
{
    public static class CyclicTests
    {
        [Fact]
        public static void WriteCyclicFail()
        {
            TestClassWithCycle obj = new TestClassWithCycle();
            obj.Parent = obj;

            // We don't allow graph cycles; we throw InvalidOperation instead of an unrecoverable StackOverflow.
            Assert.Throws<InvalidOperationException>(() => JsonSerializer.ToString(obj));
        }

        [Fact]
        public static void SimpleTypeCycle()
        {
            TestClassWithArrayOfElementsOfTheSameClass obj = new TestClassWithArrayOfElementsOfTheSameClass();

            // A cycle in just Types (not data) is allowed.
            string json = JsonSerializer.ToString(obj);
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
            string json = JsonSerializer.ToString(root);

            // Round-trip the JSON.
            TestClassWithCycle rootCopy = JsonSerializer.Parse<TestClassWithCycle>(json);
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
    }
}
