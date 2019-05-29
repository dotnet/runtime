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
        public static void ReadObjectArray()
        {
            string data =
                "[" +
                SimpleTestClass.s_json +
                "," +
                SimpleTestClass.s_json +
                "]";

            SimpleTestClass[] i = JsonSerializer.Parse<SimpleTestClass[]>(Encoding.UTF8.GetBytes(data));

            i[0].Verify();
            i[1].Verify();
        }

        [Fact]
        public static void DeserializeObjectArray_36167()
        {
            // https://github.com/dotnet/corefx/issues/36167
            object[] data = JsonSerializer.Parse<object[]>("[1]");
            Assert.Equal(1, data.Length);
            Assert.IsType<JsonElement>(data[0]);
            Assert.Equal(1, ((JsonElement)data[0]).GetInt32());
        }

        [Fact]
        public static void ReadEmptyObjectArray()
        {
            SimpleTestClass[] data = JsonSerializer.Parse<SimpleTestClass[]>("[{}]");
            Assert.Equal(1, data.Length);
            Assert.NotNull(data[0]);
        }

        [Fact]
        public static void ReadPrimitiveJagged2dArray()
        {
            int[][] i = JsonSerializer.Parse<int[][]>(Encoding.UTF8.GetBytes(@"[[1,2],[3,4]]"));
            Assert.Equal(1, i[0][0]);
            Assert.Equal(2, i[0][1]);
            Assert.Equal(3, i[1][0]);
            Assert.Equal(4, i[1][1]);
        }

        [Fact]
        public static void ReadPrimitiveJagged3dArray()
        {
            int[][][] i = JsonSerializer.Parse<int[][][]>(Encoding.UTF8.GetBytes(@"[[[11,12],[13,14]], [[21,22],[23,24]]]"));
            Assert.Equal(11, i[0][0][0]);
            Assert.Equal(12, i[0][0][1]);
            Assert.Equal(13, i[0][1][0]);
            Assert.Equal(14, i[0][1][1]);

            Assert.Equal(21, i[1][0][0]);
            Assert.Equal(22, i[1][0][1]);
            Assert.Equal(23, i[1][1][0]);
            Assert.Equal(24, i[1][1][1]);
        }

        [Fact]
        public static void ReadArrayWithInterleavedComments()
        {
            var options = new JsonSerializerOptions();
            options.ReadCommentHandling = JsonCommentHandling.Skip;

            int[][] i = JsonSerializer.Parse<int[][]>(Encoding.UTF8.GetBytes("[[1,2] // Inline [\n,[3, /* Multi\n]] Line*/4]]"), options);
            Assert.Equal(1, i[0][0]);
            Assert.Equal(2, i[0][1]);
            Assert.Equal(3, i[1][0]);
            Assert.Equal(4, i[1][1]);
        }

        [Fact]
        public static void ReadEmpty()
        {
            SimpleTestClass[] arr = JsonSerializer.Parse<SimpleTestClass[]>("[]");
            Assert.Equal(0, arr.Length);

            List<SimpleTestClass> list = JsonSerializer.Parse<List<SimpleTestClass>>("[]");
            Assert.Equal(0, list.Count);
        }

        [Fact]
        public static void ReadPrimitiveArray()
        {
            int[] i = JsonSerializer.Parse<int[]>(Encoding.UTF8.GetBytes(@"[1,2]"));
            Assert.Equal(1, i[0]);
            Assert.Equal(2, i[1]);

            i = JsonSerializer.Parse<int[]>(Encoding.UTF8.GetBytes(@"[]"));
            Assert.Equal(0, i.Length);
        }

        [Fact]
        public static void ReadArrayWithEnums()
        {
            SampleEnum[] i = JsonSerializer.Parse<SampleEnum[]>(Encoding.UTF8.GetBytes(@"[1,2]"));
            Assert.Equal(SampleEnum.One, i[0]);
            Assert.Equal(SampleEnum.Two, i[1]);
        }

        [Fact]
        public static void ReadPrimitiveArrayFail()
        {
            // Invalid data
            Assert.Throws<JsonException>(() => JsonSerializer.Parse<int[]>(Encoding.UTF8.GetBytes(@"[1,""a""]")));

            // Invalid data
            Assert.Throws<JsonException>(() => JsonSerializer.Parse<List<int?>>(Encoding.UTF8.GetBytes(@"[1,""a""]")));

            // Multidimensional arrays currently not supported
            Assert.Throws<JsonException>(() => JsonSerializer.Parse<int[,]>(Encoding.UTF8.GetBytes(@"[[1,2],[3,4]]")));
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

            static void VerifyReadNull(SimpleTestClass obj, bool isNull)
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

        public static void ClassWithNoSetter()
        {
            string json = @"{""MyList"":[1]}";
            ClassWithListButNoSetter obj = JsonSerializer.Parse<ClassWithListButNoSetter>(json);
            Assert.Equal(1, obj.MyList[0]);
        }

        public class ClassWithListButNoSetter
        {
            public List<int> MyList { get; } = new List<int>();
        }
    }
}
