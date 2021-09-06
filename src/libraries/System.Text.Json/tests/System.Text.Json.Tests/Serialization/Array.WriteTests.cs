// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using Xunit;

namespace System.Text.Json.Serialization.Tests
{
    public static partial class ArrayTests
    {
        [Fact]
        public static void WritePrimitiveArray()
        {
            var input = new int[] { 0, 1 };
            string json = JsonSerializer.Serialize(input);
            Assert.Equal("[0,1]", json);
        }

        [Fact]
        public static void WriteArrayWithEnums()
        {
            var input = new SampleEnum[] { SampleEnum.One, SampleEnum.Two };
            string json = JsonSerializer.Serialize(input);
            Assert.Equal("[1,2]", json);
        }

        [Fact]
        public static void WriteNullByteArray()
        {
            byte[] input = null;
            string json = JsonSerializer.Serialize(input);
            Assert.Equal($"null", json);
        }

        [Fact]
        public static void WriteEmptyByteArray()
        {
            var input = new byte[] {};
            string json = JsonSerializer.Serialize(input);
            Assert.Equal(@"""""", json);
        }

        [Theory]
        [InlineData(null, "null")]
        [InlineData(new byte[] { }, "\"\"")]
        [InlineData(new byte[] { 1, 2 }, "\"AQI=\"")]
        public static void WriteByteArray(byte[]? input, string expectedEncoding)
        {
            // root-level serialization
            string json = JsonSerializer.Serialize(input);
            Assert.Equal(expectedEncoding, json);

            // object property
            json = JsonSerializer.Serialize(new { Property = input });
            Assert.Equal(@$"{{""Property"":{expectedEncoding}}}", json);

            // array element
            json = JsonSerializer.Serialize(new[] { input });
            Assert.Equal($"[{expectedEncoding}]", json);

            // dictionary entry
            json = JsonSerializer.Serialize(new Dictionary<string, byte[]> { ["key"] = input });
            Assert.Equal(@$"{{""key"":{expectedEncoding}}}", json);
        }

        [Fact]
        public static void WriteTwo2dByteArray()
        {
            var inner = new byte[] { 1, 2 };
            var outer = new byte[2][] { inner, inner };
            string json = JsonSerializer.Serialize(outer);
            Assert.Equal($"[\"{Convert.ToBase64String(inner)}\",\"{Convert.ToBase64String(inner)}\"]", json);
        }

        [Fact]
        public static void WriteObjectArray()
        {
            string json;

            {
                SimpleTestClass[] input = new SimpleTestClass[] { new SimpleTestClass(), new SimpleTestClass() };
                input[0].Initialize();
                input[0].Verify();

                input[1].Initialize();
                input[1].Verify();

                json = JsonSerializer.Serialize(input);
            }

            {
                SimpleTestClass[] output = JsonSerializer.Deserialize<SimpleTestClass[]>(json);
                Assert.Equal(2, output.Length);
                output[0].Verify();
                output[1].Verify();
            }
        }

        [Fact]
        public static void WriteEmptyObjectArray()
        {
            object[] arr = new object[] { new object() };

            string json = JsonSerializer.Serialize(arr);
            Assert.Equal("[{}]", json);
        }

        [Fact]
        public static void WritePrimitiveJaggedArray()
        {
            var input = new int[2][];
            input[0] = new int[] { 1, 2 };
            input[1] = new int[] { 3, 4 };

            string json = JsonSerializer.Serialize(input);
            Assert.Equal("[[1,2],[3,4]]", json);
        }

        [Fact]
        public static void WriteEmpty()
        {
            string json = JsonSerializer.Serialize(new SimpleTestClass[] { });
            Assert.Equal("[]", json);

            json = JsonSerializer.Serialize(new List<SimpleTestClass>());
            Assert.Equal("[]", json);
        }

        [Fact]
        public static void WriteClassWithStringArray()
        {
            string json;

            {
                TestClassWithStringArray obj = new TestClassWithStringArray();
                obj.Initialize();
                obj.Verify();
                json = JsonSerializer.Serialize(obj);
            }

            {
                TestClassWithStringArray obj = JsonSerializer.Deserialize<TestClassWithStringArray>(json);
                obj.Verify();
            }

            {
                TestClassWithStringArray obj = JsonSerializer.Deserialize<TestClassWithStringArray>(TestClassWithStringArray.s_data);
                obj.Verify();
            }
        }

        [Fact]
        public static void WriteClassWithObjectArray()
        {
            string json;

            {
                TestClassWithObjectList obj = new TestClassWithObjectList();
                obj.Initialize();
                obj.Verify();
                json = JsonSerializer.Serialize(obj);
            }

            {
                TestClassWithObjectList obj = JsonSerializer.Deserialize<TestClassWithObjectList>(json);
                obj.Verify();
            }

            {
                TestClassWithObjectList obj = JsonSerializer.Deserialize<TestClassWithObjectList>(TestClassWithObjectList.s_data);
                obj.Verify();
            }
        }

        [Fact]
        public static void WriteClassWithGenericList()
        {
            string json;

            {
                TestClassWithGenericList obj = new TestClassWithGenericList();
                obj.Initialize();
                obj.Verify();
                json = JsonSerializer.Serialize(obj);
            }

            {
                TestClassWithGenericList obj = JsonSerializer.Deserialize<TestClassWithGenericList>(json);
                obj.Verify();
            }

            {
                TestClassWithGenericList obj = JsonSerializer.Deserialize<TestClassWithGenericList>(TestClassWithGenericList.s_data);
                obj.Verify();
            }
        }

        [Fact]
        public static void WriteClassWithGenericIEnumerableT()
        {
            string json;

            {
                TestClassWithGenericIEnumerableT obj = new TestClassWithGenericIEnumerableT();
                obj.Initialize();
                obj.Verify();
                json = JsonSerializer.Serialize(obj);
            }

            {
                TestClassWithGenericIEnumerableT obj = JsonSerializer.Deserialize<TestClassWithGenericIEnumerableT>(json);
                obj.Verify();
            }

            {
                TestClassWithGenericIEnumerableT obj = JsonSerializer.Deserialize<TestClassWithGenericIEnumerableT>(TestClassWithGenericIEnumerableT.s_data);
                obj.Verify();
            }
        }

        [Fact]
        public static void WriteClassWithGenericIListT()
        {
            string json;

            {
                TestClassWithGenericIListT obj = new TestClassWithGenericIListT();
                obj.Initialize();
                obj.Verify();
                json = JsonSerializer.Serialize(obj);
            }

            {
                TestClassWithGenericIListT obj = JsonSerializer.Deserialize<TestClassWithGenericIListT>(json);
                obj.Verify();
            }

            {
                TestClassWithGenericIListT obj = JsonSerializer.Deserialize<TestClassWithGenericIListT>(TestClassWithGenericIListT.s_data);
                obj.Verify();
            }
        }

        [Fact]
        public static void WriteClassWithGenericICollectionT()
        {
            string json;

            {
                TestClassWithGenericICollectionT obj = new TestClassWithGenericICollectionT();
                obj.Initialize();
                obj.Verify();
                json = JsonSerializer.Serialize(obj);
            }

            {
                TestClassWithGenericICollectionT obj = JsonSerializer.Deserialize<TestClassWithGenericICollectionT>(json);
                obj.Verify();
            }

            {
                TestClassWithGenericICollectionT obj = JsonSerializer.Deserialize<TestClassWithGenericICollectionT>(TestClassWithGenericICollectionT.s_data);
                obj.Verify();
            }
        }

        [Fact]
        public static void WriteClassWithGenericIReadOnlyCollectionT()
        {
            string json;

            {
                TestClassWithGenericIReadOnlyCollectionT obj = new TestClassWithGenericIReadOnlyCollectionT();
                obj.Initialize();
                obj.Verify();
                json = JsonSerializer.Serialize(obj);
            }

            {
                TestClassWithGenericIReadOnlyCollectionT obj = JsonSerializer.Deserialize<TestClassWithGenericIReadOnlyCollectionT>(json);
                obj.Verify();
            }

            {
                TestClassWithGenericIReadOnlyCollectionT obj = JsonSerializer.Deserialize<TestClassWithGenericIReadOnlyCollectionT>(TestClassWithGenericIReadOnlyCollectionT.s_data);
                obj.Verify();
            }
        }

        [Fact]
        public static void WriteClassWithGenericIReadOnlyListT()
        {
            string json;

            {
                TestClassWithGenericIReadOnlyListT obj = new TestClassWithGenericIReadOnlyListT();
                obj.Initialize();
                obj.Verify();
                json = JsonSerializer.Serialize(obj);
            }

            {
                TestClassWithGenericIReadOnlyListT obj = JsonSerializer.Deserialize<TestClassWithGenericIReadOnlyListT>(json);
                obj.Verify();
            }

            {
                TestClassWithGenericIReadOnlyListT obj = JsonSerializer.Deserialize<TestClassWithGenericIReadOnlyListT>(TestClassWithGenericIEnumerableT.s_data);
                obj.Verify();
            }
        }

        [Fact]
        public static void WriteClassWithObjectIEnumerableT()
        {
            string json;

            {
                TestClassWithObjectIEnumerableT obj = new TestClassWithObjectIEnumerableT();
                obj.Initialize();
                obj.Verify();
                json = JsonSerializer.Serialize(obj);
            }

            {
                TestClassWithObjectIEnumerableT obj = JsonSerializer.Deserialize<TestClassWithObjectIEnumerableT>(json);
                obj.Verify();
            }

            {
                TestClassWithObjectIEnumerableT obj = JsonSerializer.Deserialize<TestClassWithObjectIEnumerableT>(TestClassWithObjectIEnumerableT.s_data);
                obj.Verify();
            }
        }

        [Fact]
        public static void WriteClassWithObjectIListT()
        {
            string json;

            {
                TestClassWithObjectIListT obj = new TestClassWithObjectIListT();
                obj.Initialize();
                obj.Verify();
                json = JsonSerializer.Serialize(obj);
            }

            {
                TestClassWithObjectIListT obj = JsonSerializer.Deserialize<TestClassWithObjectIListT>(json);
                obj.Verify();
            }

            {
                TestClassWithObjectIListT obj = JsonSerializer.Deserialize<TestClassWithObjectIListT>(TestClassWithObjectIListT.s_data);
                obj.Verify();
            }
        }

        [Fact]
        public static void WriteClassWithObjectICollectionT()
        {
            string json;

            {
                TestClassWithObjectICollectionT obj = new TestClassWithObjectICollectionT();
                obj.Initialize();
                obj.Verify();
                json = JsonSerializer.Serialize(obj);
            }

            {
                TestClassWithObjectICollectionT obj = JsonSerializer.Deserialize<TestClassWithObjectICollectionT>(json);
                obj.Verify();
            }

            {
                TestClassWithObjectICollectionT obj = JsonSerializer.Deserialize<TestClassWithObjectICollectionT>(TestClassWithObjectICollectionT.s_data);
                obj.Verify();
            }
        }

        [Fact]
        public static void WriteClassWithObjectIReadOnlyCollectionT()
        {
            string json;

            {
                TestClassWithObjectIReadOnlyCollectionT obj = new TestClassWithObjectIReadOnlyCollectionT();
                obj.Initialize();
                obj.Verify();
                json = JsonSerializer.Serialize(obj);
            }

            {
                TestClassWithObjectIReadOnlyCollectionT obj = JsonSerializer.Deserialize<TestClassWithObjectIReadOnlyCollectionT>(json);
                obj.Verify();
            }

            {
                TestClassWithObjectIReadOnlyCollectionT obj = JsonSerializer.Deserialize<TestClassWithObjectIReadOnlyCollectionT>(TestClassWithObjectIReadOnlyCollectionT.s_data);
                obj.Verify();
            }
        }

        [Fact]
        public static void WriteClassWithObjectIReadOnlyListT()
        {
            string json;

            {
                TestClassWithObjectIReadOnlyListT obj = new TestClassWithObjectIReadOnlyListT();
                obj.Initialize();
                obj.Verify();
                json = JsonSerializer.Serialize(obj);
            }

            {
                TestClassWithObjectIReadOnlyListT obj = JsonSerializer.Deserialize<TestClassWithObjectIReadOnlyListT>(json);
                obj.Verify();
            }

            {
                TestClassWithObjectIReadOnlyListT obj = JsonSerializer.Deserialize<TestClassWithObjectIReadOnlyListT>(TestClassWithObjectIEnumerableT.s_data);
                obj.Verify();
            }
        }

        [Fact]
        public static void WriteClassWithObjectIEnumerableConstructibleTypes()
        {
            string json;

            {
                TestClassWithObjectIEnumerableConstructibleTypes obj = new TestClassWithObjectIEnumerableConstructibleTypes();
                obj.Initialize();
                obj.Verify();
                json = JsonSerializer.Serialize(obj);
            }

            {
                TestClassWithObjectIEnumerableConstructibleTypes obj = JsonSerializer.Deserialize<TestClassWithObjectIEnumerableConstructibleTypes>(json);
                obj.Verify();
            }

            {
                TestClassWithObjectIEnumerableConstructibleTypes obj = JsonSerializer.Deserialize<TestClassWithObjectIEnumerableConstructibleTypes>(TestClassWithObjectIEnumerableConstructibleTypes.s_data);
                obj.Verify();
            }
        }

        [Fact]
        public static void WriteClassWithObjectImmutableTypes()
        {
            string json;

            {
                TestClassWithObjectImmutableTypes obj = new TestClassWithObjectImmutableTypes();
                obj.Initialize();
                obj.Verify();
                json = JsonSerializer.Serialize(obj);
            }

            {
                TestClassWithObjectImmutableTypes obj = JsonSerializer.Deserialize<TestClassWithObjectImmutableTypes>(json);
                obj.Verify();
            }

            {
                TestClassWithObjectImmutableTypes obj = JsonSerializer.Deserialize<TestClassWithObjectImmutableTypes>(TestClassWithObjectImmutableTypes.s_data);
                obj.Verify();
            }
        }
    }
}
