// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Xunit;

namespace System.Text.Json.Serialization.Tests
{
    public class SimpleTestClassWithFields : ITestClass
    {
        public short MyInt16;
        public int MyInt32;
        public long MyInt64;
        public ushort MyUInt16;
        public uint MyUInt32;
        public ulong MyUInt64;
        public byte MyByte;
        public sbyte MySByte;
        public char MyChar;
        public string MyString;
        public decimal MyDecimal;
        public bool MyBooleanTrue;
        public bool MyBooleanFalse;
        public float MySingle;
        public double MyDouble;
        public DateTime MyDateTime;
        public DateTimeOffset MyDateTimeOffset;
        public Guid MyGuid;
        public Uri MyUri;
        public SampleEnumSByte MySByteEnum;
        public SampleEnumByte MyByteEnum;
        public SampleEnum MyEnum;
        public SampleEnumInt16 MyInt16Enum;
        public SampleEnumInt32 MyInt32Enum;
        public SampleEnumInt64 MyInt64Enum;
        public SampleEnumUInt16 MyUInt16Enum;
        public SampleEnumUInt32 MyUInt32Enum;
        public SampleEnumUInt64 MyUInt64Enum;
        public SimpleStruct MySimpleStruct;
        public SimpleTestStruct MySimpleTestStruct;
        public short[] MyInt16Array;
        public int[] MyInt32Array;
        public long[] MyInt64Array;
        public ushort[] MyUInt16Array;
        public uint[] MyUInt32Array;
        public ulong[] MyUInt64Array;
        public byte[] MyByteArray;
        public sbyte[] MySByteArray;
        public char[] MyCharArray;
        public string[] MyStringArray;
        public decimal[] MyDecimalArray;
        public bool[] MyBooleanTrueArray;
        public bool[] MyBooleanFalseArray;
        public float[] MySingleArray;
        public double[] MyDoubleArray;
        public DateTime[] MyDateTimeArray;
        public DateTimeOffset[] MyDateTimeOffsetArray;
        public Guid[] MyGuidArray;
        public Uri[] MyUriArray;
        public SampleEnum[] MyEnumArray;
        public int[][] MyInt16TwoDimensionArray;
        public List<List<int>> MyInt16TwoDimensionList;
        public int[][][] MyInt16ThreeDimensionArray;
        public List<List<List<int>>> MyInt16ThreeDimensionList;
        public List<string> MyStringList;
        public IEnumerable MyStringIEnumerable;
        public IList MyStringIList;
        public ICollection MyStringICollection;
        public IEnumerable<string> MyStringIEnumerableT;
        public IList<string> MyStringIListT;
        public ICollection<string> MyStringICollectionT;
        public IReadOnlyCollection<string> MyStringIReadOnlyCollectionT;
        public IReadOnlyList<string> MyStringIReadOnlyListT;
        public ISet<string> MyStringISetT;
        public KeyValuePair<string, string> MyStringToStringKeyValuePair;
        public IDictionary MyStringToStringIDict;
        public Dictionary<string, string> MyStringToStringGenericDict;
        public IDictionary<string, string> MyStringToStringGenericIDict;
        public IReadOnlyDictionary<string, string> MyStringToStringGenericIReadOnlyDict;
        public ImmutableDictionary<string, string> MyStringToStringImmutableDict;
        public IImmutableDictionary<string, string> MyStringToStringIImmutableDict;
        public ImmutableSortedDictionary<string, string> MyStringToStringImmutableSortedDict;
        public Stack<string> MyStringStackT;
        public Queue<string> MyStringQueueT;
        public HashSet<string> MyStringHashSetT;
        public LinkedList<string> MyStringLinkedListT;
        public SortedSet<string> MyStringSortedSetT;
        public IImmutableList<string> MyStringIImmutableListT;
        public IImmutableStack<string> MyStringIImmutableStackT;
        public IImmutableQueue<string> MyStringIImmutableQueueT;
        public IImmutableSet<string> MyStringIImmutableSetT;
        public ImmutableHashSet<string> MyStringImmutableHashSetT;
        public ImmutableList<string> MyStringImmutableListT;
        public ImmutableStack<string> MyStringImmutableStackT;
        public ImmutableQueue<string> MyStringImmutablQueueT;
        public ImmutableSortedSet<string> MyStringImmutableSortedSetT;
        public List<string> MyListOfNullString;

        public static readonly string s_json = $"{{{s_partialJsonProperties},{s_partialJsonArrays}}}";
        public static readonly string s_json_flipped = $"{{{s_partialJsonArrays},{s_partialJsonProperties}}}";

        private const string s_partialJsonProperties =
                @"""MyInt16"" : 1," +
                @"""MyInt32"" : 2," +
                @"""MyInt64"" : 3," +
                @"""MyUInt16"" : 4," +
                @"""MyUInt32"" : 5," +
                @"""MyUInt64"" : 6," +
                @"""MyByte"" : 7," +
                @"""MySByte"" : 8," +
                @"""MyChar"" : ""a""," +
                @"""MyString"" : ""Hello""," +
                @"""MyBooleanTrue"" : true," +
                @"""MyBooleanFalse"" : false," +
                @"""MySingle"" : 1.1," +
                @"""MyDouble"" : 2.2," +
                @"""MyDecimal"" : 3.3," +
                @"""MyDateTime"" : ""2019-01-30T12:01:02.0000000Z""," +
                @"""MyDateTimeOffset"" : ""2019-01-30T12:01:02.0000000+01:00""," +
                @"""MyGuid"" : ""1B33498A-7B7D-4DDA-9C13-F6AA4AB449A6""," +
                @"""MyUri"" : ""https://github.com/dotnet/runtime""," +
                @"""MyEnum"" : 2," + // int by default
                @"""MyInt64Enum"" : -9223372036854775808," +
                @"""MyUInt64Enum"" : 18446744073709551615," +
                @"""MyStringToStringKeyValuePair"" : {""Key"" : ""myKey"", ""Value"" : ""myValue""}," +
                @"""MyStringToStringIDict"" : {""key"" : ""value""}," +
                @"""MyStringToStringGenericDict"" : {""key"" : ""value""}," +
                @"""MyStringToStringGenericIDict"" : {""key"" : ""value""}," +
                @"""MyStringToStringGenericIReadOnlyDict"" : {""key"" : ""value""}," +
                @"""MyStringToStringImmutableDict"" : {""key"" : ""value""}," +
                @"""MyStringToStringIImmutableDict"" : {""key"" : ""value""}," +
                @"""MyStringToStringImmutableSortedDict"" : {""key"" : ""value""}," +
                @"""MySimpleStruct"" : {""One"" : 11, ""Two"" : 1.9999, ""Three"" : 33}," +
                @"""MySimpleTestStruct"" : {""MyInt64"" : 64, ""MyString"" :""Hello"", ""MyInt32Array"" : [32]}";

        private const string s_partialJsonArrays =
                @"""MyInt16Array"" : [1]," +
                @"""MyInt32Array"" : [2]," +
                @"""MyInt64Array"" : [3]," +
                @"""MyUInt16Array"" : [4]," +
                @"""MyUInt32Array"" : [5]," +
                @"""MyUInt64Array"" : [6]," +
                @"""MyByteArray"" : ""Bw==""," + // Base64 encoded value of 7
                @"""MySByteArray"" : [8]," +
                @"""MyCharArray"" : [""a""]," +
                @"""MyStringArray"" : [""Hello""]," +
                @"""MyBooleanTrueArray"" : [true]," +
                @"""MyBooleanFalseArray"" : [false]," +
                @"""MySingleArray"" : [1.1]," +
                @"""MyDoubleArray"" : [2.2]," +
                @"""MyDecimalArray"" : [3.3]," +
                @"""MyDateTimeArray"" : [""2019-01-30T12:01:02.0000000Z""]," +
                @"""MyDateTimeOffsetArray"" : [""2019-01-30T12:01:02.0000000+01:00""]," +
                @"""MyGuidArray"" : [""1B33498A-7B7D-4DDA-9C13-F6AA4AB449A6""]," +
                @"""MyUriArray"" : [""https://github.com/dotnet/runtime""]," +
                @"""MyEnumArray"" : [2]," + // int by default
                @"""MyInt16TwoDimensionArray"" : [[10, 11],[20, 21]]," +
                @"""MyInt16TwoDimensionList"" : [[10, 11],[20, 21]]," +
                @"""MyInt16ThreeDimensionArray"" : [[[11, 12],[13, 14]],[[21,22],[23,24]]]," +
                @"""MyInt16ThreeDimensionList"" : [[[11, 12],[13, 14]],[[21,22],[23,24]]]," +
                @"""MyStringList"" : [""Hello""]," +
                @"""MyStringIEnumerable"" : [""Hello""]," +
                @"""MyStringIList"" : [""Hello""]," +
                @"""MyStringICollection"" : [""Hello""]," +
                @"""MyStringIEnumerableT"" : [""Hello""]," +
                @"""MyStringIListT"" : [""Hello""]," +
                @"""MyStringICollectionT"" : [""Hello""]," +
                @"""MyStringIReadOnlyCollectionT"" : [""Hello""]," +
                @"""MyStringIReadOnlyListT"" : [""Hello""]," +
                @"""MyStringISetT"" : [""Hello""]," +
                @"""MyStringStackT"" : [""Hello"", ""World""]," +
                @"""MyStringQueueT"" : [""Hello"", ""World""]," +
                @"""MyStringHashSetT"" : [""Hello""]," +
                @"""MyStringLinkedListT"" : [""Hello""]," +
                @"""MyStringSortedSetT"" : [""Hello""]," +
                @"""MyStringIImmutableListT"" : [""Hello""]," +
                @"""MyStringIImmutableStackT"" : [""Hello""]," +
                @"""MyStringIImmutableQueueT"" : [""Hello""]," +
                @"""MyStringIImmutableSetT"" : [""Hello""]," +
                @"""MyStringImmutableHashSetT"" : [""Hello""]," +
                @"""MyStringImmutableListT"" : [""Hello""]," +
                @"""MyStringImmutableStackT"" : [""Hello""]," +
                @"""MyStringImmutablQueueT"" : [""Hello""]," +
                @"""MyStringImmutableSortedSetT"" : [""Hello""]," +
                @"""MyListOfNullString"" : [null]";

        public static readonly byte[] s_data = Encoding.UTF8.GetBytes(s_json);

        public void Initialize()
        {
            MyInt16 = 1;
            MyInt32 = 2;
            MyInt64 = 3;
            MyUInt16 = 4;
            MyUInt32 = 5;
            MyUInt64 = 6;
            MyByte = 7;
            MySByte = 8;
            MyChar = 'a';
            MyString = "Hello";
            MyBooleanTrue = true;
            MyBooleanFalse = false;
            MySingle = 1.1f;
            MyDouble = 2.2d;
            MyDecimal = 3.3m;
            MyDateTime = new DateTime(2019, 1, 30, 12, 1, 2, DateTimeKind.Utc);
            MyDateTimeOffset = new DateTimeOffset(2019, 1, 30, 12, 1, 2, new TimeSpan(1, 0, 0));
            MyGuid = new Guid("1B33498A-7B7D-4DDA-9C13-F6AA4AB449A6");
            MyUri = new Uri("https://github.com/dotnet/runtime");
            MyEnum = SampleEnum.Two;
            MyInt64Enum = SampleEnumInt64.MinNegative;
            MyUInt64Enum = SampleEnumUInt64.Max;
            MyInt16Array = new short[] { 1 };
            MyInt32Array = new int[] { 2 };
            MyInt64Array = new long[] { 3 };
            MyUInt16Array = new ushort[] { 4 };
            MyUInt32Array = new uint[] { 5 };
            MyUInt64Array = new ulong[] { 6 };
            MyByteArray = new byte[] { 7 };
            MySByteArray = new sbyte[] { 8 };
            MyCharArray = new char[] { 'a' };
            MyStringArray = new string[] { "Hello" };
            MyBooleanTrueArray = new bool[] { true };
            MyBooleanFalseArray = new bool[] { false };
            MySingleArray = new float[] { 1.1f };
            MyDoubleArray = new double[] { 2.2d };
            MyDecimalArray = new decimal[] { 3.3m };
            MyDateTimeArray = new DateTime[] { new DateTime(2019, 1, 30, 12, 1, 2, DateTimeKind.Utc) };
            MyDateTimeOffsetArray = new DateTimeOffset[] { new DateTimeOffset(2019, 1, 30, 12, 1, 2, new TimeSpan(1, 0, 0)) };
            MyGuidArray = new Guid[] { new Guid("1B33498A-7B7D-4DDA-9C13-F6AA4AB449A6") };
            MyUriArray = new Uri[] { new Uri("https://github.com/dotnet/runtime") };
            MyEnumArray = new SampleEnum[] { SampleEnum.Two };
            MySimpleStruct = new SimpleStruct { One = 11, Two = 1.9999 };
            MySimpleTestStruct = new SimpleTestStruct { MyInt64 = 64, MyString = "Hello", MyInt32Array = new int[] { 32 } };

            MyInt16TwoDimensionArray = new int[2][];
            MyInt16TwoDimensionArray[0] = new int[] { 10, 11 };
            MyInt16TwoDimensionArray[1] = new int[] { 20, 21 };

            MyInt16TwoDimensionList = new List<List<int>>();
            MyInt16TwoDimensionList.Add(new List<int> { 10, 11 });
            MyInt16TwoDimensionList.Add(new List<int> { 20, 21 });

            MyInt16ThreeDimensionArray = new int[2][][];
            MyInt16ThreeDimensionArray[0] = new int[2][];
            MyInt16ThreeDimensionArray[1] = new int[2][];
            MyInt16ThreeDimensionArray[0][0] = new int[] { 11, 12 };
            MyInt16ThreeDimensionArray[0][1] = new int[] { 13, 14 };
            MyInt16ThreeDimensionArray[1][0] = new int[] { 21, 22 };
            MyInt16ThreeDimensionArray[1][1] = new int[] { 23, 24 };

            MyInt16ThreeDimensionList = new List<List<List<int>>>();
            var list1 = new List<List<int>>();
            MyInt16ThreeDimensionList.Add(list1);
            list1.Add(new List<int> { 11, 12 });
            list1.Add(new List<int> { 13, 14 });
            var list2 = new List<List<int>>();
            MyInt16ThreeDimensionList.Add(list2);
            list2.Add(new List<int> { 21, 22 });
            list2.Add(new List<int> { 23, 24 });

            MyStringList = new List<string>() { "Hello" };

            MyStringIEnumerable = new string[] { "Hello" };
            MyStringIList = new string[] { "Hello" };
            MyStringICollection = new string[] { "Hello" };

            MyStringIEnumerableT = new string[] { "Hello" };
            MyStringIListT = new string[] { "Hello" };
            MyStringICollectionT = new string[] { "Hello" };
            MyStringIReadOnlyCollectionT = new string[] { "Hello" };
            MyStringIReadOnlyListT = new string[] { "Hello" };
            MyStringISetT = new HashSet<string> { "Hello" };

            MyStringToStringKeyValuePair = new KeyValuePair<string, string>("myKey", "myValue");
            MyStringToStringIDict = new Dictionary<string, string> { { "key", "value" } };

            MyStringToStringGenericDict = new Dictionary<string, string> { { "key", "value" } };
            MyStringToStringGenericIDict = new Dictionary<string, string> { { "key", "value" } };
            MyStringToStringGenericIReadOnlyDict = new Dictionary<string, string> { { "key", "value" } };

            MyStringToStringImmutableDict = ImmutableDictionary.CreateRange(MyStringToStringGenericDict);
            MyStringToStringIImmutableDict = ImmutableDictionary.CreateRange(MyStringToStringGenericDict);
            MyStringToStringImmutableSortedDict = ImmutableSortedDictionary.CreateRange(MyStringToStringGenericDict);

            MyStringStackT = new Stack<string>(new List<string>() { "Hello", "World" });
            MyStringQueueT = new Queue<string>(new List<string>() { "Hello", "World" });
            MyStringHashSetT = new HashSet<string>(new List<string>() { "Hello" });
            MyStringLinkedListT = new LinkedList<string>(new List<string>() { "Hello" });
            MyStringSortedSetT = new SortedSet<string>(new List<string>() { "Hello" });

            MyStringIImmutableListT = ImmutableList.CreateRange(new List<string> { "Hello" });
            MyStringIImmutableStackT = ImmutableStack.CreateRange(new List<string> { "Hello" });
            MyStringIImmutableQueueT = ImmutableQueue.CreateRange(new List<string> { "Hello" });
            MyStringIImmutableSetT = ImmutableHashSet.CreateRange(new List<string> { "Hello" });
            MyStringImmutableHashSetT = ImmutableHashSet.CreateRange(new List<string> { "Hello" });
            MyStringImmutableListT = ImmutableList.CreateRange(new List<string> { "Hello" });
            MyStringImmutableStackT = ImmutableStack.CreateRange(new List<string> { "Hello" });
            MyStringImmutablQueueT = ImmutableQueue.CreateRange(new List<string> { "Hello" });
            MyStringImmutableSortedSetT = ImmutableSortedSet.CreateRange(new List<string> { "Hello" });

            MyListOfNullString = new List<string> { null };
        }

        public void Verify()
        {
            Assert.Equal((short)1, MyInt16);
            Assert.Equal((int)2, MyInt32);
            Assert.Equal((long)3, MyInt64);
            Assert.Equal((ushort)4, MyUInt16);
            Assert.Equal((uint)5, MyUInt32);
            Assert.Equal((ulong)6, MyUInt64);
            Assert.Equal((byte)7, MyByte);
            Assert.Equal((sbyte)8, MySByte);
            Assert.Equal('a', MyChar);
            Assert.Equal("Hello", MyString);
            Assert.Equal(3.3m, MyDecimal);
            Assert.False(MyBooleanFalse);
            Assert.True(MyBooleanTrue);
            Assert.Equal(1.1f, MySingle);
            Assert.Equal(2.2d, MyDouble);
            Assert.Equal(new DateTime(2019, 1, 30, 12, 1, 2, DateTimeKind.Utc), MyDateTime);
            Assert.Equal(new DateTimeOffset(2019, 1, 30, 12, 1, 2, new TimeSpan(1, 0, 0)), MyDateTimeOffset);
            Assert.Equal(new Guid("1B33498A-7B7D-4DDA-9C13-F6AA4AB449A6"), MyGuid);
            Assert.Equal(new Uri("https://github.com/dotnet/runtime"), MyUri);
            Assert.Equal(SampleEnum.Two, MyEnum);
            Assert.Equal(SampleEnumInt64.MinNegative, MyInt64Enum);
            Assert.Equal(SampleEnumUInt64.Max, MyUInt64Enum);
            Assert.Equal(11, MySimpleStruct.One);
            Assert.Equal(1.9999, MySimpleStruct.Two);
            Assert.Equal(64, MySimpleTestStruct.MyInt64);
            Assert.Equal("Hello", MySimpleTestStruct.MyString);
            Assert.Equal(32, MySimpleTestStruct.MyInt32Array[0]);

            Assert.Equal((short)1, MyInt16Array[0]);
            Assert.Equal((int)2, MyInt32Array[0]);
            Assert.Equal((long)3, MyInt64Array[0]);
            Assert.Equal((ushort)4, MyUInt16Array[0]);
            Assert.Equal((uint)5, MyUInt32Array[0]);
            Assert.Equal((ulong)6, MyUInt64Array[0]);
            Assert.Equal((byte)7, MyByteArray[0]);
            Assert.Equal((sbyte)8, MySByteArray[0]);
            Assert.Equal('a', MyCharArray[0]);
            Assert.Equal("Hello", MyStringArray[0]);
            Assert.Equal(3.3m, MyDecimalArray[0]);
            Assert.False(MyBooleanFalseArray[0]);
            Assert.True(MyBooleanTrueArray[0]);
            Assert.Equal(1.1f, MySingleArray[0]);
            Assert.Equal(2.2d, MyDoubleArray[0]);
            Assert.Equal(new DateTime(2019, 1, 30, 12, 1, 2, DateTimeKind.Utc), MyDateTimeArray[0]);
            Assert.Equal(new DateTimeOffset(2019, 1, 30, 12, 1, 2, new TimeSpan(1, 0, 0)), MyDateTimeOffsetArray[0]);
            Assert.Equal(new Guid("1B33498A-7B7D-4DDA-9C13-F6AA4AB449A6"), MyGuidArray[0]);
            Assert.Equal(new Uri("https://github.com/dotnet/runtime"), MyUriArray[0]);
            Assert.Equal(SampleEnum.Two, MyEnumArray[0]);

            Assert.Equal(10, MyInt16TwoDimensionArray[0][0]);
            Assert.Equal(11, MyInt16TwoDimensionArray[0][1]);
            Assert.Equal(20, MyInt16TwoDimensionArray[1][0]);
            Assert.Equal(21, MyInt16TwoDimensionArray[1][1]);

            Assert.Equal(10, MyInt16TwoDimensionList[0][0]);
            Assert.Equal(11, MyInt16TwoDimensionList[0][1]);
            Assert.Equal(20, MyInt16TwoDimensionList[1][0]);
            Assert.Equal(21, MyInt16TwoDimensionList[1][1]);

            Assert.Equal(11, MyInt16ThreeDimensionArray[0][0][0]);
            Assert.Equal(12, MyInt16ThreeDimensionArray[0][0][1]);
            Assert.Equal(13, MyInt16ThreeDimensionArray[0][1][0]);
            Assert.Equal(14, MyInt16ThreeDimensionArray[0][1][1]);
            Assert.Equal(21, MyInt16ThreeDimensionArray[1][0][0]);
            Assert.Equal(22, MyInt16ThreeDimensionArray[1][0][1]);
            Assert.Equal(23, MyInt16ThreeDimensionArray[1][1][0]);
            Assert.Equal(24, MyInt16ThreeDimensionArray[1][1][1]);

            Assert.Equal(11, MyInt16ThreeDimensionList[0][0][0]);
            Assert.Equal(12, MyInt16ThreeDimensionList[0][0][1]);
            Assert.Equal(13, MyInt16ThreeDimensionList[0][1][0]);
            Assert.Equal(14, MyInt16ThreeDimensionList[0][1][1]);
            Assert.Equal(21, MyInt16ThreeDimensionList[1][0][0]);
            Assert.Equal(22, MyInt16ThreeDimensionList[1][0][1]);
            Assert.Equal(23, MyInt16ThreeDimensionList[1][1][0]);
            Assert.Equal(24, MyInt16ThreeDimensionList[1][1][1]);

            Assert.Equal("Hello", MyStringList[0]);

            IEnumerator enumerator = MyStringIEnumerable.GetEnumerator();
            enumerator.MoveNext();
            {
                // Verifying after deserialization.
                if (enumerator.Current is JsonElement currentJsonElement)
                {
                    Assert.Equal("Hello", currentJsonElement.GetString());
                }
                // Verifying test data.
                else
                {
                    Assert.Equal("Hello", enumerator.Current);
                }
            }

            {
                // Verifying after deserialization.
                if (MyStringIList[0] is JsonElement currentJsonElement)
                {
                    Assert.Equal("Hello", currentJsonElement.GetString());
                }
                // Verifying test data.
                else
                {
                    Assert.Equal("Hello", enumerator.Current);
                }
            }

            enumerator = MyStringICollection.GetEnumerator();
            enumerator.MoveNext();
            {
                // Verifying after deserialization.
                if (enumerator.Current is JsonElement currentJsonElement)
                {
                    Assert.Equal("Hello", currentJsonElement.GetString());
                }
                // Verifying test data.
                else
                {
                    Assert.Equal("Hello", enumerator.Current);
                }
            }

            Assert.Equal("Hello", MyStringIEnumerableT.First());
            Assert.Equal("Hello", MyStringIListT[0]);
            Assert.Equal("Hello", MyStringICollectionT.First());
            Assert.Equal("Hello", MyStringIReadOnlyCollectionT.First());
            Assert.Equal("Hello", MyStringIReadOnlyListT[0]);
            Assert.Equal("Hello", MyStringISetT.First());

            enumerator = MyStringToStringIDict.GetEnumerator();
            enumerator.MoveNext();
            {
                // Verifying after deserialization.
                if (enumerator.Current is JsonElement currentJsonElement)
                {
                    IEnumerator jsonEnumerator = currentJsonElement.EnumerateObject();
                    jsonEnumerator.MoveNext();

                    JsonProperty property = (JsonProperty)jsonEnumerator.Current;

                    Assert.Equal("key", property.Name);
                    Assert.Equal("value", property.Value.GetString());
                }
                // Verifying test data.
                else
                {
                    DictionaryEntry entry = (DictionaryEntry)enumerator.Current;
                    Assert.Equal("key", entry.Key);

                    if (entry.Value is JsonElement element)
                    {
                        Assert.Equal("value", element.GetString());
                    }
                    else
                    {
                        Assert.Equal("value", entry.Value);
                    }
                }
            }

            Assert.Equal("value", MyStringToStringGenericDict["key"]);
            Assert.Equal("value", MyStringToStringGenericIDict["key"]);
            Assert.Equal("value", MyStringToStringGenericIReadOnlyDict["key"]);

            Assert.Equal("value", MyStringToStringImmutableDict["key"]);
            Assert.Equal("value", MyStringToStringIImmutableDict["key"]);
            Assert.Equal("value", MyStringToStringImmutableSortedDict["key"]);

            Assert.Equal("myKey", MyStringToStringKeyValuePair.Key);
            Assert.Equal("myValue", MyStringToStringKeyValuePair.Value);

            Assert.Equal(2, MyStringStackT.Count);
            Assert.True(MyStringStackT.Contains("Hello"));
            Assert.True(MyStringStackT.Contains("World"));

            string[] expectedQueue = { "Hello", "World" };
            int i = 0;
            foreach (string item in MyStringQueueT)
            {
                Assert.Equal(expectedQueue[i], item);
                i++;
            }

            Assert.Equal("Hello", MyStringHashSetT.First());
            Assert.Equal("Hello", MyStringLinkedListT.First());
            Assert.Equal("Hello", MyStringSortedSetT.First());

            Assert.Equal("Hello", MyStringIImmutableListT[0]);
            Assert.Equal("Hello", MyStringIImmutableStackT.First());
            Assert.Equal("Hello", MyStringIImmutableQueueT.First());
            Assert.Equal("Hello", MyStringIImmutableSetT.First());
            Assert.Equal("Hello", MyStringImmutableHashSetT.First());
            Assert.Equal("Hello", MyStringImmutableListT[0]);
            Assert.Equal("Hello", MyStringImmutableStackT.First());
            Assert.Equal("Hello", MyStringImmutablQueueT.First());
            Assert.Equal("Hello", MyStringImmutableSortedSetT.First());

            Assert.Null(MyListOfNullString[0]);
        }
    }
}
