// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Xunit;

namespace System.Text.Json.Serialization.Tests
{
    public class SimpleTestClass : ITestClass
    {
        public short MyInt16 { get; set; }
        public int MyInt32 { get; set; }
        public long MyInt64 { get; set; }
        public ushort MyUInt16 { get; set; }
        public uint MyUInt32 { get; set; }
        public ulong MyUInt64 { get; set; }
        public byte MyByte { get; set; }
        public sbyte MySByte { get; set; }
        public char MyChar { get; set; }
        public string MyString { get; set; }
        public decimal MyDecimal { get; set; }
        public bool MyBooleanTrue { get; set; }
        public bool MyBooleanFalse { get; set; }
        public float MySingle { get; set; }
        public double MyDouble { get; set; }
        public DateTime MyDateTime { get; set; }
        public DateTimeOffset MyDateTimeOffset { get; set; }
        public SampleEnum MyEnum { get; set; }
        public SampleInt64Enum MyInt64Enum { get; set; }
        public SampleUInt64Enum MyUInt64Enum { get; set; }
        public short[] MyInt16Array { get; set; }
        public int[] MyInt32Array { get; set; }
        public long[] MyInt64Array { get; set; }
        public ushort[] MyUInt16Array { get; set; }
        public uint[] MyUInt32Array { get; set; }
        public ulong[] MyUInt64Array { get; set; }
        public byte[] MyByteArray { get; set; }
        public sbyte[] MySByteArray { get; set; }
        public char[] MyCharArray { get; set; }
        public string[] MyStringArray { get; set; }
        public decimal[] MyDecimalArray { get; set; }
        public bool[] MyBooleanTrueArray { get; set; }
        public bool[] MyBooleanFalseArray { get; set; }
        public float[] MySingleArray { get; set; }
        public double[] MyDoubleArray { get; set; }
        public DateTime[] MyDateTimeArray { get; set; }
        public DateTimeOffset[] MyDateTimeOffsetArray { get; set; }
        public SampleEnum[] MyEnumArray { get; set; }
        public int[][] MyInt16TwoDimensionArray { get; set; }
        public List<List<int>> MyInt16TwoDimensionList { get; set; }
        public int[][][] MyInt16ThreeDimensionArray { get; set; }
        public List<List<List<int>>> MyInt16ThreeDimensionList { get; set; }
        public List<string> MyStringList { get; set; }
        public IEnumerable<string> MyStringIEnumerableT { get; set; }
        public IList<string> MyStringIListT { get; set; }
        public ICollection<string> MyStringICollectionT { get; set; }
        public IReadOnlyCollection<string> MyStringIReadOnlyCollectionT { get; set; }
        public IReadOnlyList<string> MyStringIReadOnlyListT { get; set; }
        public Dictionary<string, string> MyStringToStringDict { get; set; }
        public IDictionary<string, string> MyStringToStringIDict { get; set; }
        public IReadOnlyDictionary<string, string> MyStringToStringIReadOnlyDict { get; set; }
        public ImmutableDictionary<string, string> MyStringToStringImmutableDict { get; set; }
        public IImmutableDictionary<string, string> MyStringToStringIImmutableDict { get; set; }
        public ImmutableSortedDictionary<string, string> MyStringToStringImmutableSortedDict { get; set; }
        public Stack<string> MyStringStackT { get; set; }
        public Queue<string> MyStringQueueT { get; set; }
        public HashSet<string> MyStringHashSetT { get; set; }
        public LinkedList<string> MyStringLinkedListT { get; set; }
        public SortedSet<string> MyStringSortedSetT { get; set; }
        public IImmutableList<string> MyStringIImmutableListT { get; set; }
        public IImmutableStack<string> MyStringIImmutableStackT { get; set; }
        public IImmutableQueue<string> MyStringIImmutableQueueT { get; set; }
        public IImmutableSet<string> MyStringIImmutableSetT { get; set; }
        public ImmutableHashSet<string> MyStringImmutableHashSetT { get; set; }
        public ImmutableList<string> MyStringImmutableListT { get; set; }
        public ImmutableStack<string> MyStringImmutableStackT { get; set; }
        public ImmutableQueue<string> MyStringImmutablQueueT { get; set; }
        public ImmutableSortedSet<string> MyStringImmutableSortedSetT { get; set; }
        public List<string> MyListOfNullString { get; set; }

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
                @"""MyEnum"" : 2," + // int by default
                @"""MyStringToStringDict"" : {""key"" : ""value""}," +
                @"""MyStringToStringIDict"" : {""key"" : ""value""}," +
                @"""MyStringToStringIReadOnlyDict"" : {""key"" : ""value""}," +
                @"""MyStringToStringImmutableDict"" : {""key"" : ""value""}," +
                @"""MyStringToStringIImmutableDict"" : {""key"" : ""value""}," +
                @"""MyStringToStringImmutableSortedDict"" : {""key"" : ""value""}";

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
                @"""MyEnumArray"" : [2]," + // int by default
                @"""MyInt16TwoDimensionArray"" : [[10, 11],[20, 21]]," +
                @"""MyInt16TwoDimensionList"" : [[10, 11],[20, 21]]," +
                @"""MyInt16ThreeDimensionArray"" : [[[11, 12],[13, 14]],[[21,22],[23,24]]]," +
                @"""MyInt16ThreeDimensionList"" : [[[11, 12],[13, 14]],[[21,22],[23,24]]]," +
                @"""MyStringList"" : [""Hello""]," +
                @"""MyStringIEnumerableT"" : [""Hello""]," +
                @"""MyStringIListT"" : [""Hello""]," +
                @"""MyStringICollectionT"" : [""Hello""]," +
                @"""MyStringIReadOnlyCollectionT"" : [""Hello""]," +
                @"""MyStringIReadOnlyListT"" : [""Hello""]," +
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
            MyEnum = SampleEnum.Two;

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
            MyEnumArray = new SampleEnum[] { SampleEnum.Two };

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
            MyStringIEnumerableT = new string[] { "Hello" };
            MyStringIListT = new string[] { "Hello" };
            MyStringICollectionT = new string[] { "Hello" };
            MyStringIReadOnlyCollectionT = new string[] { "Hello" };
            MyStringIReadOnlyListT = new string[] { "Hello" };

            MyStringToStringDict = new Dictionary<string, string> { { "key", "value" } };
            MyStringToStringIDict = new Dictionary<string, string> { { "key", "value" } };
            MyStringToStringIReadOnlyDict = new Dictionary<string, string> { { "key", "value" } };

            MyStringToStringImmutableDict = ImmutableDictionary.CreateRange(MyStringToStringDict);
            MyStringToStringIImmutableDict = ImmutableDictionary.CreateRange(MyStringToStringDict);
            MyStringToStringImmutableSortedDict = ImmutableSortedDictionary.CreateRange(MyStringToStringDict);

            MyStringStackT = new Stack<string>(new List<string>() { "Hello", "World" } );
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
            Assert.Equal(false, MyBooleanFalse);
            Assert.Equal(true, MyBooleanTrue);
            Assert.Equal(1.1f, MySingle);
            Assert.Equal(2.2d, MyDouble);
            Assert.Equal(new DateTime(2019, 1, 30, 12, 1, 2, DateTimeKind.Utc), MyDateTime);
            Assert.Equal(new DateTimeOffset(2019, 1, 30, 12, 1, 2, new TimeSpan(1, 0, 0)), MyDateTimeOffset);
            Assert.Equal(SampleEnum.Two, MyEnum);

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
            Assert.Equal(false, MyBooleanFalseArray[0]);
            Assert.Equal(true, MyBooleanTrueArray[0]);
            Assert.Equal(1.1f, MySingleArray[0]);
            Assert.Equal(2.2d, MyDoubleArray[0]);
            Assert.Equal(new DateTime(2019, 1, 30, 12, 1, 2, DateTimeKind.Utc), MyDateTimeArray[0]);
            Assert.Equal(new DateTimeOffset(2019, 1, 30, 12, 1, 2, new TimeSpan(1, 0, 0)), MyDateTimeOffsetArray[0]);
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
            Assert.Equal("Hello", MyStringIEnumerableT.First());
            Assert.Equal("Hello", MyStringIListT[0]);
            Assert.Equal("Hello", MyStringICollectionT.First());
            Assert.Equal("Hello", MyStringIReadOnlyCollectionT.First());
            Assert.Equal("Hello", MyStringIReadOnlyListT[0]);

            Assert.Equal("value", MyStringToStringDict["key"]);
            Assert.Equal("value", MyStringToStringIDict["key"]);
            Assert.Equal("value", MyStringToStringIReadOnlyDict["key"]);

            Assert.Equal("value", MyStringToStringImmutableDict["key"]);
            Assert.Equal("value", MyStringToStringIImmutableDict["key"]);
            Assert.Equal("value", MyStringToStringImmutableSortedDict["key"]);

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
