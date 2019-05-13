// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Reflection;
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


        public static IEnumerable<object[]> ReadNullJson
        {
            get
            {
                yield return new object[] { $"[null, null, null]", true, true, true };
                yield return new object[] { $"[null, null, {SimpleTestClass.s_json}]", true, true, false };
                yield return new object[] { $"[null, {SimpleTestClass.s_json}, null]", true, false, true };
                yield return new object[] { $"[null, {SimpleTestClass.s_json}, {SimpleTestClass.s_json}]", true, false, false };
                yield return new object[] { $"[{SimpleTestClass.s_json}, {SimpleTestClass.s_json}, {SimpleTestClass.s_json}]", false, false, false };
                yield return new object[] { $"[{SimpleTestClass.s_json}, {SimpleTestClass.s_json}, null]", false, false, true };
                yield return new object[] { $"[{SimpleTestClass.s_json}, null, {SimpleTestClass.s_json}]", false, true, false };
                yield return new object[] { $"[{SimpleTestClass.s_json}, null, null]", false, true, true };
            }
        }

        private static void VerifyReadNull(SimpleTestClass obj, bool isNull)
        {
            if (isNull)
            {
                Assert.Null(obj);
            }
            else
            {
                obj.Verify();
            }
        }

        [Theory]
        [MemberData(nameof(ReadNullJson))]
        public static void ReadNull(string json, bool element0Null, bool element1Null, bool element2Null)
        {
            SimpleTestClass[] arr = JsonSerializer.Parse<SimpleTestClass[]>(json);
            Assert.Equal(3, arr.Length);
            VerifyReadNull(arr[0], element0Null);
            VerifyReadNull(arr[1], element1Null);
            VerifyReadNull(arr[2], element2Null);

            List<SimpleTestClass> list = JsonSerializer.Parse<List<SimpleTestClass>>(json);
            Assert.Equal(3, list.Count);
            VerifyReadNull(list[0], element0Null);
            VerifyReadNull(list[1], element1Null);
            VerifyReadNull(list[2], element2Null);
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

        [Fact]
        public static void ReadClassWithObjectIEnumerableConstructibleTypes()
        {
            TestClassWithObjectIEnumerableConstructibleTypes obj = JsonSerializer.Parse<TestClassWithObjectIEnumerableConstructibleTypes>(TestClassWithObjectIEnumerableConstructibleTypes.s_data);
            obj.Verify();
        }

        [Fact]
        public static void ReadClassWithObjectImmutableTypes()
        {
            TestClassWithObjectImmutableTypes obj = JsonSerializer.Parse<TestClassWithObjectImmutableTypes>(TestClassWithObjectImmutableTypes.s_data);
            obj.Verify();
        }
    }
}
