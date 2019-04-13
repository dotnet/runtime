// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using Xunit;

namespace System.Text.Json.Serialization.Tests
{
    public static partial class ArrayTests
    {
        [Fact]
        public static void ReadEmpty()
        {
            SimpleTestClass[] arr = JsonSerializer.Parse<SimpleTestClass[]>("[]");
            Assert.Equal(0, arr.Length);

            List<SimpleTestClass> list = JsonSerializer.Parse<List<SimpleTestClass>>("[]");
            Assert.Equal(0, list.Count);
        }

        [Fact]
        public static void ReadClassWithStringArray()
        {
            TestClassWithStringArray obj = JsonSerializer.Parse<TestClassWithStringArray>(TestClassWithStringArray.s_data);
            obj.Verify();
        }

        [Fact]
        public static void ReadClassWithObjectList()
        {
            TestClassWithObjectList obj = JsonSerializer.Parse<TestClassWithObjectList>(TestClassWithObjectList.s_data);
            obj.Verify();
        }

        [Fact]
        public static void ReadClassWithObjectArray()
        {
            TestClassWithObjectArray obj = JsonSerializer.Parse<TestClassWithObjectArray>(TestClassWithObjectArray.s_data);
            obj.Verify();
        }

        [Fact]
        public static void ReadClassWithGenericList()
        {
            TestClassWithGenericList obj = JsonSerializer.Parse<TestClassWithGenericList>(TestClassWithGenericList.s_data);
            obj.Verify();
        }

        [Fact]
        public static void ReadClassWithObjectIEnumerableT()
        {
            TestClassWithObjectIEnumerableT obj = JsonSerializer.Parse<TestClassWithObjectIEnumerableT>(TestClassWithObjectIEnumerableT.s_data);
            obj.Verify();
        }

        [Fact]
        public static void ReadClassWithObjectIListT()
        {
            TestClassWithObjectIListT obj = JsonSerializer.Parse<TestClassWithObjectIListT>(TestClassWithObjectIListT.s_data);
            obj.Verify();
        }

        [Fact]
        public static void ReadClassWithObjectICollectionT()
        {
            TestClassWithObjectICollectionT obj = JsonSerializer.Parse<TestClassWithObjectICollectionT>(TestClassWithObjectICollectionT.s_data);
            obj.Verify();
        }

        [Fact]
        public static void ReadClassWithObjectIReadOnlyCollectionT()
        {
            TestClassWithObjectIReadOnlyCollectionT obj = JsonSerializer.Parse<TestClassWithObjectIReadOnlyCollectionT>(TestClassWithObjectIReadOnlyCollectionT.s_data);
            obj.Verify();
        }

        [Fact]
        public static void ReadClassWithObjectIReadOnlyListT()
        {
            TestClassWithObjectIReadOnlyListT obj = JsonSerializer.Parse<TestClassWithObjectIReadOnlyListT>(TestClassWithObjectIReadOnlyListT.s_data);
            obj.Verify();
        }

        [Fact]
        public static void ReadClassWithGenericIEnumerableT()
        {
            TestClassWithGenericIEnumerableT obj = JsonSerializer.Parse<TestClassWithGenericIEnumerableT>(TestClassWithGenericIEnumerableT.s_data);
            obj.Verify();
        }

        [Fact]
        public static void ReadClassWithGenericIListT()
        {
            TestClassWithGenericIListT obj = JsonSerializer.Parse<TestClassWithGenericIListT>(TestClassWithGenericIListT.s_data);
            obj.Verify();
        }

        [Fact]
        public static void ReadClassWithGenericICollectionT()
        {
            TestClassWithGenericICollectionT obj = JsonSerializer.Parse<TestClassWithGenericICollectionT>(TestClassWithGenericICollectionT.s_data);
            obj.Verify();
        }

        [Fact]
        public static void ReadClassWithGenericIReadOnlyCollectionT()
        {
            TestClassWithGenericIReadOnlyCollectionT obj = JsonSerializer.Parse<TestClassWithGenericIReadOnlyCollectionT>(TestClassWithGenericIReadOnlyCollectionT.s_data);
            obj.Verify();
        }

        [Fact]
        public static void ReadClassWithGenericIReadOnlyListT()
        {
            TestClassWithGenericIReadOnlyListT obj = JsonSerializer.Parse<TestClassWithGenericIReadOnlyListT>(TestClassWithGenericIReadOnlyListT.s_data);
            obj.Verify();
        }
    }
}
