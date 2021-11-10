// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
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

            SimpleTestClass[] i = JsonSerializer.Deserialize<SimpleTestClass[]>(Encoding.UTF8.GetBytes(data));

            i[0].Verify();
            i[1].Verify();
        }

        [Fact]
        public static void ReadNullByteArray()
        {
            byte[] arr = JsonSerializer.Deserialize<byte[]>("null");
            Assert.Null(arr);

            PocoWithByteArrayProperty poco = JsonSerializer.Deserialize<PocoWithByteArrayProperty>(@"{""Value"":null}");
            Assert.Null(poco.Value);

            byte[][] jaggedArr = JsonSerializer.Deserialize<byte[][]>(@"[null]");
            Assert.Null(jaggedArr[0]);

            Dictionary<string, byte[]> dict = JsonSerializer.Deserialize<Dictionary<string, byte[]>>(@"{""key"":null}");
            Assert.Null(dict["key"]);
        }

        public class PocoWithByteArrayProperty
        {
            public byte[] Value { get; set; }
        }

        [Fact]
        public static void ReadEmptyByteArray()
        {
            string json = @"""""";
            byte[] arr = JsonSerializer.Deserialize<byte[]>(json);
            Assert.Equal(0, arr.Length);
        }

        [Fact]
        public static void ReadByteArray()
        {
            string json = $"\"{Convert.ToBase64String(new byte[] { 1, 2 })}\"";
            byte[] arr = JsonSerializer.Deserialize<byte[]>(json);

            Assert.Equal(2, arr.Length);
            Assert.Equal(1, arr[0]);
            Assert.Equal(2, arr[1]);
        }

        [Fact]
        public static void Read2dByteArray()
        {
            // Baseline for comparison.
            Assert.Equal("AQI=", Convert.ToBase64String(new byte[] { 1, 2 }));

            string json = "[\"AQI=\",\"AQI=\"]";
            byte[][] arr = JsonSerializer.Deserialize<byte[][]>(json);
            Assert.Equal(2, arr.Length);

            Assert.Equal(2, arr[0].Length);
            Assert.Equal(1, arr[0][0]);
            Assert.Equal(2, arr[0][1]);

            Assert.Equal(2, arr[1].Length);
            Assert.Equal(1, arr[1][0]);
            Assert.Equal(2, arr[1][1]);
        }

        [Theory]
        [InlineData(@"""1""")]
        [InlineData(@"""A===""")]
        [InlineData(@"[1, 2]")]  // Currently not support deserializing JSON arrays as byte[] - only Base64 string.
        public static void ReadByteArrayFail(string json)
        {
            Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<byte[]>(json));
        }

        [Fact]
        public static void ReadByteListAsJsonArray()
        {
            string json = $"[1, 2]";
            List<byte> list = JsonSerializer.Deserialize<List<byte>>(json);

            Assert.Equal(2, list.Count);
            Assert.Equal(1, list[0]);
            Assert.Equal(2, list[1]);
        }

        [Fact]
        public static void DeserializeObjectArray()
        {
            // https://github.com/dotnet/runtime/issues/29019
            object[] data = JsonSerializer.Deserialize<object[]>("[1]");
            Assert.Equal(1, data.Length);
            Assert.IsType<JsonElement>(data[0]);
            Assert.Equal(1, ((JsonElement)data[0]).GetInt32());
        }

        [Fact]
        public static void ReadEmptyObjectArray()
        {
            SimpleTestClass[] data = JsonSerializer.Deserialize<SimpleTestClass[]>("[{}]");
            Assert.Equal(1, data.Length);
            Assert.NotNull(data[0]);
        }

        [Fact]
        public static void ReadPrimitiveJagged2dArray()
        {
            int[][] i = JsonSerializer.Deserialize<int[][]>(Encoding.UTF8.GetBytes(@"[[1,2],[3,4]]"));
            Assert.Equal(1, i[0][0]);
            Assert.Equal(2, i[0][1]);
            Assert.Equal(3, i[1][0]);
            Assert.Equal(4, i[1][1]);
        }

        [Fact]
        public static void ReadPrimitiveJagged3dArray()
        {
            int[][][] i = JsonSerializer.Deserialize<int[][][]>(Encoding.UTF8.GetBytes(@"[[[11,12],[13,14]], [[21,22],[23,24]]]"));
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

            int[][] i = JsonSerializer.Deserialize<int[][]>(Encoding.UTF8.GetBytes("[[1,2] // Inline [\n,[3, /* Multi\n]] Line*/4]]"), options);
            Assert.Equal(1, i[0][0]);
            Assert.Equal(2, i[0][1]);
            Assert.Equal(3, i[1][0]);
            Assert.Equal(4, i[1][1]);
        }

        [Fact]
        public static void ReadEmpty()
        {
            SimpleTestClass[] arr = JsonSerializer.Deserialize<SimpleTestClass[]>("[]");
            Assert.Equal(0, arr.Length);

            List<SimpleTestClass> list = JsonSerializer.Deserialize<List<SimpleTestClass>>("[]");
            Assert.Equal(0, list.Count);
        }

        [Fact]
        public static void ReadPrimitiveArray()
        {
            int[] i = JsonSerializer.Deserialize<int[]>(Encoding.UTF8.GetBytes(@"[1,2]"));
            Assert.Equal(1, i[0]);
            Assert.Equal(2, i[1]);

            i = JsonSerializer.Deserialize<int[]>(Encoding.UTF8.GetBytes(@"[]"));
            Assert.Equal(0, i.Length);
        }

        [Fact]
        public static void ReadInitializedArrayTest()
        {
            string serialized = "{\"Values\":[1,2,3]}";
            TestClassWithInitializedArray testClassWithInitializedArray = JsonSerializer.Deserialize<TestClassWithInitializedArray>(serialized);

            Assert.Equal(1, testClassWithInitializedArray.Values[0]);
            Assert.Equal(2, testClassWithInitializedArray.Values[1]);
            Assert.Equal(3, testClassWithInitializedArray.Values[2]);
        }

        [Fact]
        public static void ReadArrayWithEnums()
        {
            SampleEnum[] i = JsonSerializer.Deserialize<SampleEnum[]>(Encoding.UTF8.GetBytes(@"[1,2]"));
            Assert.Equal(SampleEnum.One, i[0]);
            Assert.Equal(SampleEnum.Two, i[1]);
        }

        [Fact]
        public static void ReadPrimitiveArrayFail()
        {
            // Invalid data
            Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<int[]>(Encoding.UTF8.GetBytes(@"[1,""a""]")));

            // Invalid data
            Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<List<int?>>(Encoding.UTF8.GetBytes(@"[1,""a""]")));

            // Multidimensional arrays currently not supported
            Assert.Throws<NotSupportedException>(() => JsonSerializer.Deserialize<int[,]>(Encoding.UTF8.GetBytes(@"[[1,2],[3,4]]")));
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
            SimpleTestClass[] arr = JsonSerializer.Deserialize<SimpleTestClass[]>(json);
            Assert.Equal(3, arr.Length);
            VerifyReadNull(arr[0], element0Null);
            VerifyReadNull(arr[1], element1Null);
            VerifyReadNull(arr[2], element2Null);

            List<SimpleTestClass> list = JsonSerializer.Deserialize<List<SimpleTestClass>>(json);
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
            TestClassWithStringArray obj = JsonSerializer.Deserialize<TestClassWithStringArray>(TestClassWithStringArray.s_data);
            obj.Verify();
        }

        [Fact]
        public static void ReadClassWithObjectList()
        {
            TestClassWithObjectList obj = JsonSerializer.Deserialize<TestClassWithObjectList>(TestClassWithObjectList.s_data);
            obj.Verify();
        }

        [Fact]
        public static void ReadClassWithObjectArray()
        {
            TestClassWithObjectArray obj = JsonSerializer.Deserialize<TestClassWithObjectArray>(TestClassWithObjectArray.s_data);
            obj.Verify();
        }

        [Fact]
        public static void ReadClassWithGenericList()
        {
            TestClassWithGenericList obj = JsonSerializer.Deserialize<TestClassWithGenericList>(TestClassWithGenericList.s_data);
            obj.Verify();
        }

        [Fact]
        public static void ReadClassWithObjectIEnumerable()
        {
            TestClassWithObjectIEnumerable obj = JsonSerializer.Deserialize<TestClassWithObjectIEnumerable>(TestClassWithObjectIEnumerable.s_data);
            obj.Verify();
        }

        [Fact]
        public static void ReadClassWithObjectIList()
        {
            TestClassWithObjectIList obj = JsonSerializer.Deserialize<TestClassWithObjectIList>(TestClassWithObjectIList.s_data);
            obj.Verify();
        }

        [Fact]
        public static void ReadClassWithObjectICollection()
        {
            TestClassWithObjectICollection obj = JsonSerializer.Deserialize<TestClassWithObjectICollection>(TestClassWithObjectICollection.s_data);
            obj.Verify();
        }

        [Fact]
        public static void ReadClassWithObjectIEnumerableT()
        {
            TestClassWithObjectIEnumerableT obj = JsonSerializer.Deserialize<TestClassWithObjectIEnumerableT>(TestClassWithObjectIEnumerableT.s_data);
            obj.Verify();
        }

        [Fact]
        public static void ReadClassWithObjectIListT()
        {
            TestClassWithObjectIListT obj = JsonSerializer.Deserialize<TestClassWithObjectIListT>(TestClassWithObjectIListT.s_data);
            obj.Verify();
        }

        [Fact]
        public static void ReadClassWithObjectICollectionT()
        {
            TestClassWithObjectICollectionT obj = JsonSerializer.Deserialize<TestClassWithObjectICollectionT>(TestClassWithObjectICollectionT.s_data);
            obj.Verify();
        }

        [Fact]
        public static void ReadClassWithObjectIReadOnlyCollectionT()
        {
            TestClassWithObjectIReadOnlyCollectionT obj = JsonSerializer.Deserialize<TestClassWithObjectIReadOnlyCollectionT>(TestClassWithObjectIReadOnlyCollectionT.s_data);
            obj.Verify();
        }

        [Fact]
        public static void ReadClassWithObjectIReadOnlyListT()
        {
            TestClassWithObjectIReadOnlyListT obj = JsonSerializer.Deserialize<TestClassWithObjectIReadOnlyListT>(TestClassWithObjectIReadOnlyListT.s_data);
            obj.Verify();
        }

        [Fact]
        public static void ReadClassWithGenericIEnumerable()
        {
            TestClassWithGenericIEnumerable obj = JsonSerializer.Deserialize<TestClassWithGenericIEnumerable>(TestClassWithGenericIEnumerable.s_data);
            obj.Verify();
        }

        [Fact]
        public static void ReadClassWithGenericIList()
        {
            TestClassWithGenericIList obj = JsonSerializer.Deserialize<TestClassWithGenericIList>(TestClassWithGenericIList.s_data);
            obj.Verify();
        }

        [Fact]
        public static void ReadClassWithGenericICollection()
        {
            TestClassWithGenericICollection obj = JsonSerializer.Deserialize<TestClassWithGenericICollection>(TestClassWithGenericICollection.s_data);
        }

        [Fact]
        public static void ReadClassWithObjectISetT()
        {
            TestClassWithObjectISetT obj = JsonSerializer.Deserialize<TestClassWithObjectISetT>(TestClassWithObjectISetT.s_data);
            obj.Verify();
        }

        [Fact]
        public static void ReadClassWithGenericIEnumerableT()
        {
            TestClassWithGenericIEnumerableT obj = JsonSerializer.Deserialize<TestClassWithGenericIEnumerableT>(TestClassWithGenericIEnumerableT.s_data);
            obj.Verify();
        }

        [Fact]
        public static void ReadClassWithGenericIListT()
        {
            TestClassWithGenericIListT obj = JsonSerializer.Deserialize<TestClassWithGenericIListT>(TestClassWithGenericIListT.s_data);
            obj.Verify();
        }

        [Fact]
        public static void ReadClassWithGenericICollectionT()
        {
            TestClassWithGenericICollectionT obj = JsonSerializer.Deserialize<TestClassWithGenericICollectionT>(TestClassWithGenericICollectionT.s_data);
            obj.Verify();
        }

        [Fact]
        public static void ReadClassWithGenericIReadOnlyCollectionT()
        {
            TestClassWithGenericIReadOnlyCollectionT obj = JsonSerializer.Deserialize<TestClassWithGenericIReadOnlyCollectionT>(TestClassWithGenericIReadOnlyCollectionT.s_data);
            obj.Verify();
        }

        [Fact]
        public static void ReadClassWithGenericIReadOnlyListT()
        {
            TestClassWithGenericIReadOnlyListT obj = JsonSerializer.Deserialize<TestClassWithGenericIReadOnlyListT>(TestClassWithGenericIReadOnlyListT.s_data);
            obj.Verify();
        }

        [Fact]
        public static void ReadClassWithGenericISetT()
        {
            TestClassWithGenericISetT obj = JsonSerializer.Deserialize<TestClassWithGenericISetT>(TestClassWithGenericISetT.s_data);
            obj.Verify();
        }

        [Fact]
        public static void ReadClassWithObjectIEnumerableConstructibleTypes()
        {
            TestClassWithObjectIEnumerableConstructibleTypes obj = JsonSerializer.Deserialize<TestClassWithObjectIEnumerableConstructibleTypes>(TestClassWithObjectIEnumerableConstructibleTypes.s_data);
            obj.Verify();
        }

        [Fact]
        public static void ReadClassWithObjectImmutableTypes()
        {
            TestClassWithObjectImmutableTypes obj = JsonSerializer.Deserialize<TestClassWithObjectImmutableTypes>(TestClassWithObjectImmutableTypes.s_data);
            obj.Verify();
        }

        public class ClassWithPopulatedListAndNoSetter
        {
            public List<int> MyList { get; } = new List<int>() { 1 };
        }

        [Fact]
        public static void ClassWithNoSetter()
        {
            // We replace the contents of this collection; we don't attempt to add items to the existing collection instance.
            string json = @"{""MyList"":[1,2]}";
            ClassWithPopulatedListAndNoSetter obj = JsonSerializer.Deserialize<ClassWithPopulatedListAndNoSetter>(json);
            Assert.Equal(1, obj.MyList.Count);
        }

        public class ClassWithPopulatedListAndSetter
        {
            public List<int> MyList { get; set; } = new List<int>() { 1 };
        }

        [Fact]
        public static void ClassWithPopulatedList()
        {
            // We replace the contents of this collection; we don't attempt to add items to the existing collection instance.
            string json = @"{""MyList"":[2,3]}";
            ClassWithPopulatedListAndSetter obj = JsonSerializer.Deserialize<ClassWithPopulatedListAndSetter>(json);
            Assert.Equal(2, obj.MyList.Count);
        }

        public class ClassWithMixedSetters
        {
            public List<int> SkippedChild1 { get; }
            public List<int> ParsedChild1 { get; set; }
            public IEnumerable<int> SkippedChild2 { get; }
            public IEnumerable<int> ParsedChild2 { get; set; }
            [JsonIgnore] public IEnumerable<int> SkippedChild3 { get; set; } // Note this has a setter.
            public IEnumerable<int> ParsedChild3 { get; set; }
        }

        [Theory]
        [InlineData(@"{
                ""SkippedChild1"": {},
                ""ParsedChild1"": [1],
                ""UnmatchedProp"": null,
                ""SkippedChild2"": [{""DrainProp1"":{}, ""DrainProp2"":{""SubProp"":0}}],
                ""SkippedChild2"": {},
                ""ParsedChild2"": [2,2],
                ""SkippedChild3"": {},
                ""ParsedChild3"": [3,3]}")]
        [InlineData(@"{
                ""SkippedChild1"": null,
                ""ParsedChild1"": [1],
                ""UnmatchedProp"": null,
                ""SkippedChild2"": [],
                ""SkippedChild2"": null,
                ""ParsedChild2"": [2,2],
                ""SkippedChild3"": null,
                ""ParsedChild3"": [3,3]}")]
        public static void ClassWithMixedSettersIsParsed(string json)
        {
            ClassWithMixedSetters parsedObject = JsonSerializer.Deserialize<ClassWithMixedSetters>(json);

            Assert.Null(parsedObject.SkippedChild1);

            Assert.NotNull(parsedObject.ParsedChild1);
            Assert.Equal(1, parsedObject.ParsedChild1.Count);
            Assert.Equal(1, parsedObject.ParsedChild1[0]);

            Assert.Null(parsedObject.SkippedChild2);

            Assert.NotNull(parsedObject.ParsedChild2);
            Assert.True(parsedObject.ParsedChild2.SequenceEqual(new int[] { 2, 2 }));

            Assert.NotNull(parsedObject.ParsedChild3);
            Assert.True(parsedObject.ParsedChild3.SequenceEqual(new int[] { 3, 3 }));
        }

        public class ClassWithNonNullEnumerableGetters
        {
            private string[] _array = null;
            private List<string> _list = null;
            private StringListWrapper _listWrapper = null;
            // Immutable array is a struct.
            private ImmutableArray<string> _immutableArray = default;
            private ImmutableList<string> _immutableList = null;

            public string[] Array
            {
                get => _array ?? new string[] { "-1" };
                set { _array = value; }
            }

            public List<string> List
            {
                get => _list ?? new List<string> { "-1" };
                set { _list = value; }
            }

            public StringListWrapper ListWrapper
            {
                get => _listWrapper ?? new StringListWrapper { "-1" };
                set { _listWrapper = value; }
            }

            public ImmutableArray<string> MyImmutableArray
            {
                get => _immutableArray.IsDefault ? ImmutableArray.CreateRange(new List<string> { "-1" }) : _immutableArray;
                set { _immutableArray = value; }
            }

            public ImmutableList<string> MyImmutableList
            {
                get => _immutableList ?? ImmutableList.CreateRange(new List<string> { "-1" });
                set { _immutableList = value; }
            }

            internal object GetRawArray => _array;
            internal object GetRawList => _list;
            internal object GetRawListWrapper => _listWrapper;
            internal object GetRawImmutableArray => _immutableArray;
            internal object GetRawImmutableList => _immutableList;
        }

        [Fact]
        public static void ClassWithNonNullEnumerableGettersIsParsed()
        {
            static void TestRoundTrip(ClassWithNonNullEnumerableGetters obj)
            {
                ClassWithNonNullEnumerableGetters roundtrip = JsonSerializer.Deserialize<ClassWithNonNullEnumerableGetters>(JsonSerializer.Serialize(obj));

                if (obj.Array != null)
                {
                    Assert.Equal(obj.Array.Length, roundtrip.Array.Length);
                    Assert.Equal(obj.List.Count, roundtrip.List.Count);
                    Assert.Equal(obj.ListWrapper.Count, roundtrip.ListWrapper.Count);
                    Assert.Equal(obj.MyImmutableArray.Length, roundtrip.MyImmutableArray.Length);
                    Assert.Equal(obj.MyImmutableList.Count, roundtrip.MyImmutableList.Count);

                    if (obj.Array.Length > 0)
                    {
                        Assert.Equal(obj.Array[0], roundtrip.Array[0]);
                        Assert.Equal(obj.List[0], roundtrip.List[0]);
                        Assert.Equal(obj.ListWrapper[0], roundtrip.ListWrapper[0]);
                        Assert.Equal(obj.MyImmutableArray[0], roundtrip.MyImmutableArray[0]);
                        Assert.Equal(obj.MyImmutableList[0], roundtrip.MyImmutableList[0]);
                    }
                }
                else
                {
                    Assert.Null(obj.GetRawArray);
                    Assert.Null(obj.GetRawList);
                    Assert.Null(obj.GetRawListWrapper);
                    Assert.Null(obj.GetRawImmutableList);
                    Assert.Null(roundtrip.GetRawArray);
                    Assert.Null(roundtrip.GetRawList);
                    Assert.Null(roundtrip.GetRawListWrapper);
                    Assert.Null(roundtrip.GetRawImmutableList);
                    Assert.Equal(obj.GetRawImmutableArray, roundtrip.GetRawImmutableArray);
                }
            }

            const string inputJsonWithCollectionElements =
                @"{
                    ""Array"":[""1""],
                    ""List"":[""2""],
                    ""ListWrapper"":[""3""],
                    ""MyImmutableArray"":[""4""],
                    ""MyImmutableList"":[""5""]
                }";

            ClassWithNonNullEnumerableGetters obj = JsonSerializer.Deserialize<ClassWithNonNullEnumerableGetters>(inputJsonWithCollectionElements);
            Assert.Equal(1, obj.Array.Length);
            Assert.Equal("1", obj.Array[0]);

            Assert.Equal(1, obj.List.Count);
            Assert.Equal("2", obj.List[0]);

            Assert.Equal(1, obj.ListWrapper.Count);
            Assert.Equal("3", obj.ListWrapper[0]);

            Assert.Equal(1, obj.MyImmutableArray.Length);
            Assert.Equal("4", obj.MyImmutableArray[0]);

            Assert.Equal(1, obj.MyImmutableList.Count);
            Assert.Equal("5", obj.MyImmutableList[0]);

            TestRoundTrip(obj);

            const string inputJsonWithoutCollectionElements =
                @"{
                    ""Array"":[],
                    ""List"":[],
                    ""ListWrapper"":[],
                    ""MyImmutableArray"":[],
                    ""MyImmutableList"":[]
                }";

            obj = JsonSerializer.Deserialize<ClassWithNonNullEnumerableGetters>(inputJsonWithoutCollectionElements);
            Assert.Equal(0, obj.Array.Length);
            Assert.Equal(0, obj.List.Count);
            Assert.Equal(0, obj.ListWrapper.Count);
            Assert.Equal(0, obj.MyImmutableArray.Length);
            Assert.Equal(0, obj.MyImmutableList.Count);
            TestRoundTrip(obj);

            string inputJsonWithNullCollections =
                @"{
                    ""Array"":null,
                    ""List"":null,
                    ""ListWrapper"":null,
                    ""MyImmutableList"":null
                }";

            obj = JsonSerializer.Deserialize<ClassWithNonNullEnumerableGetters>(inputJsonWithNullCollections);
            TestRoundTrip(obj);

            // ImmutableArray<T> is a struct and cannot be null.
            inputJsonWithNullCollections = @"{""MyImmutableArray"":null}";
            Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<ClassWithNonNullEnumerableGetters>(inputJsonWithNullCollections));
        }

        [Fact]
        public static void DoNotDependOnPropertyGetterWhenDeserializingCollections()
        {
            Dealer dealer = new Dealer { NetworkCodeList = new List<string> { "Network1", "Network2" } };

            string serialized = JsonSerializer.Serialize(dealer);
            Assert.Equal(@"{""NetworkCodeList"":[""Network1"",""Network2""]}", serialized);

            dealer = JsonSerializer.Deserialize<Dealer>(serialized);

            List<string> expected = new List<string> { "Network1", "Network2" };
            int i = 0;

            foreach (string str in dealer.NetworkCodeList)
            {
                Assert.Equal(expected[i], str);
                i++;
            }

            Assert.Equal("Network1,Network2", dealer.Networks);
        }

        class Dealer
        {
            private string _networks;

            [JsonIgnore]
            public string Networks
            {
                get => _networks;
                set => _networks = value ?? string.Empty;
            }

            public IEnumerable<string> NetworkCodeList
            {
                get => !string.IsNullOrEmpty(Networks) ? Networks?.Split(',') : new string[0];
                set => Networks = (value != null) ? string.Join(",", value) : string.Empty;
            }
        }
    }
}
