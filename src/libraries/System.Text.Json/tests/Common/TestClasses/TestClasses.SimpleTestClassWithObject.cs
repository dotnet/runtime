// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Xunit;

namespace System.Text.Json.Serialization.Tests
{
    public class SimpleTestClassWithObject : SimpleTestClassWithSimpleObject
    {
        public object MyInt16Array { get; set; }
        public object MyInt32Array { get; set; }
        public object MyInt64Array { get; set; }
        public object MyUInt16Array { get; set; }
        public object MyUInt32Array { get; set; }
        public object MyUInt64Array { get; set; }
        public object MyByteArray { get; set; }
        public object MySByteArray { get; set; }
        public object MyCharArray { get; set; }
        public object MyStringArray { get; set; }
        public object MyDecimalArray { get; set; }
        public object MyBooleanTrueArray { get; set; }
        public object MyBooleanFalseArray { get; set; }
        public object MySingleArray { get; set; }
        public object MyDoubleArray { get; set; }
        public object MyDateTimeArray { get; set; }
        public object MyEnumArray { get; set; }
        public object MyStringList { get; set; }
        public object MyStringIEnumerable { get; set; }
        public object MyStringIList { get; set; }
        public object MyStringICollection { get; set; }
        public object MyStringIEnumerableT { get; set; }
        public object MyStringIListT { get; set; }
        public object MyStringICollectionT { get; set; }
        public object MyStringIReadOnlyCollectionT { get; set; }
        public object MyStringIReadOnlyListT { get; set; }
        public object MyStringISetT { get; set; }
        public object MyStringToStringKeyValuePair { get; set; }
        public object MyStringToStringIDict { get; set; }
        public object MyStringToStringGenericDict { get; set; }
        public object MyStringToStringGenericIDict { get; set; }
        public object MyStringToStringGenericIReadOnlyDict { get; set; }
        public object MyStringToStringImmutableDict { get; set; }
        public object MyStringToStringIImmutableDict { get; set; }
        public object MyStringToStringImmutableSortedDict { get; set; }
        public object MyStringStackT { get; set; }
        public object MyStringQueueT { get; set; }
        public object MyStringHashSetT { get; set; }
        public object MyStringLinkedListT { get; set; }
        public object MyStringSortedSetT { get; set; }
        public object MyStringIImmutableListT { get; set; }
        public object MyStringIImmutableStackT { get; set; }
        public object MyStringIImmutableQueueT { get; set; }
        public object MyStringIImmutableSetT { get; set; }
        public object MyStringImmutableHashSetT { get; set; }
        public object MyStringImmutableArray { get; set; }
        public object MyStringImmutableListT { get; set; }
        public object MyStringImmutableStackT { get; set; }
        public object MyStringImmutablQueueT { get; set; }
        public object MyStringImmutableSortedSetT { get; set; }

        public static new readonly string s_json =
                @"{" +
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
                @"""MyEnum"" : 2," + // int by default
                @"""MyInt16Array"" : [1]," +
                @"""MyInt32Array"" : [2]," +
                @"""MyInt64Array"" : [3]," +
                @"""MyUInt16Array"" : [4]," +
                @"""MyUInt32Array"" : [5]," +
                @"""MyUInt64Array"" : [6]," +
                @"""MyByteArray"" : [7]," +
                @"""MySByteArray"" : [8]," +
                @"""MyCharArray"" : [""a""]," +
                @"""MyStringArray"" : [""Hello""]," +
                @"""MyBooleanTrueArray"" : [true]," +
                @"""MyBooleanFalseArray"" : [false]," +
                @"""MySingleArray"" : [1.1]," +
                @"""MyDoubleArray"" : [2.2]," +
                @"""MyDecimalArray"" : [3.3]," +
                @"""MyDateTimeArray"" : [""2019-01-30T12:01:02.0000000Z""]," +
                @"""MyEnumArray"" : [2]," + // int by default
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
                @"""MyStringToStringKeyValuePair"" : {""Key"" : ""myKey"", ""Value"" : ""myValue""}," +
                @"""MyStringToStringIDict"" : {""key"" : ""value""}," +
                @"""MyStringToStringGenericDict"" : {""key"" : ""value""}," +
                @"""MyStringToStringGenericIDict"" : {""key"" : ""value""}," +
                @"""MyStringToStringGenericIReadOnlyDict"" : {""key"" : ""value""}," +
                @"""MyStringToStringImmutableDict"" : {""key"" : ""value""}," +
                @"""MyStringToStringIImmutableDict"" : {""key"" : ""value""}," +
                @"""MyStringToStringImmutableSortedDict"" : {""key"" : ""value""}," +
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
                @"""MyStringImmutableSortedSetT"" : [""Hello""]" +
                @"}";

        public static new readonly byte[] s_data = Encoding.UTF8.GetBytes(s_json);

        public override void Initialize()
        {
            base.Initialize();

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
            MyEnumArray = new SampleEnum[] { SampleEnum.Two };

            MyStringList = new List<string>() { "Hello" };

            MyStringIEnumerable = new string[] { "Hello" };
            MyStringIList = new string[] { "Hello" };
            MyStringICollection = new string[] { "Hello" };

            MyStringIEnumerableT = new string[] { "Hello" };
            MyStringIListT = new string[] { "Hello" };
            MyStringICollectionT = new string[] { "Hello" };
            MyStringIReadOnlyCollectionT = new string[] { "Hello" };
            MyStringIReadOnlyListT = new string[] { "Hello" };
            MyStringIReadOnlyListT = new HashSet<string> { "Hello" };

            MyStringISetT = new HashSet<string> { "Hello" };

            MyStringToStringKeyValuePair = new KeyValuePair<string, string>("myKey", "myValue");
            MyStringToStringIDict = new Dictionary<string, string> { { "key", "value" } };

            MyStringToStringGenericDict = new Dictionary<string, string> { { "key", "value" } };
            MyStringToStringGenericIDict = new Dictionary<string, string> { { "key", "value" } };
            MyStringToStringGenericIReadOnlyDict = new Dictionary<string, string> { { "key", "value" } };

            MyStringToStringImmutableDict = ImmutableDictionary.CreateRange((Dictionary<string, string>)MyStringToStringGenericDict);
            MyStringToStringIImmutableDict = ImmutableDictionary.CreateRange((Dictionary<string, string>)MyStringToStringGenericDict);
            MyStringToStringImmutableSortedDict = ImmutableSortedDictionary.CreateRange((Dictionary<string, string>)MyStringToStringGenericDict);

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
        }

        public override void Verify()
        {
            base.Verify();

            Assert.Equal((short)1, ((short[])MyInt16Array)[0]);
            Assert.Equal((int)2, ((int[])MyInt32Array)[0]);
            Assert.Equal((long)3, ((long[])MyInt64Array)[0]);
            Assert.Equal((ushort)4, ((ushort[])MyUInt16Array)[0]);
            Assert.Equal((uint)5, ((uint[])MyUInt32Array)[0]);
            Assert.Equal((ulong)6, ((ulong[])MyUInt64Array)[0]);
            Assert.Equal((byte)7, ((byte[])MyByteArray)[0]);
            Assert.Equal((sbyte)8, ((sbyte[])MySByteArray)[0]);
            Assert.Equal('a', ((char[])MyCharArray)[0]);
            Assert.Equal("Hello", ((string[])MyStringArray)[0]);
            Assert.Equal(3.3m, ((decimal[])MyDecimalArray)[0]);
            Assert.False(((bool[])MyBooleanFalseArray)[0]);
            Assert.True(((bool[])MyBooleanTrueArray)[0]);
            Assert.Equal(1.1f, ((float[])MySingleArray)[0]);
            Assert.Equal(2.2d, ((double[])MyDoubleArray)[0]);
            Assert.Equal(new DateTime(2019, 1, 30, 12, 1, 2, DateTimeKind.Utc), ((DateTime[])MyDateTimeArray)[0]);
            Assert.Equal(SampleEnum.Two, ((SampleEnum[])MyEnumArray)[0]);

            Assert.Equal("Hello", ((List<string>)MyStringList)[0]);

            ((IEnumerable)MyStringIEnumerable).GetEnumerator().MoveNext();
            Assert.Equal("Hello", (string)((IEnumerable)MyStringIEnumerable).GetEnumerator().Current);

            Assert.Equal("Hello", (string)((IList)MyStringIList)[0]);

            ((ICollection)MyStringICollection).GetEnumerator().MoveNext();
            Assert.Equal("Hello", (string)((ICollection)MyStringICollection).GetEnumerator().Current);

            Assert.Equal("Hello", ((IEnumerable<string>)MyStringIEnumerableT).First());
            Assert.Equal("Hello", ((IList<string>)MyStringIListT)[0]);
            Assert.Equal("Hello", ((ICollection<string>)MyStringICollectionT).First());
            Assert.Equal("Hello", ((IReadOnlyCollection<string>)MyStringIReadOnlyCollectionT).First());
            Assert.Equal("Hello", ((IReadOnlyList<string>)MyStringIReadOnlyListT)[0]);
            Assert.Equal("Hello", ((ISet<string>)MyStringISetT).First());


            IEnumerator enumerator = ((IDictionary)MyStringToStringIDict).GetEnumerator();
            enumerator.MoveNext();

            DictionaryEntry entry = (DictionaryEntry)(enumerator.Current);
            Assert.Equal("key", entry.Key);
            Assert.Equal("value", entry.Value);

            Assert.Equal("value", ((Dictionary<string, string>)MyStringToStringGenericDict)["key"]);
            Assert.Equal("value", ((IDictionary<string, string>)MyStringToStringGenericIDict)["key"]);
            Assert.Equal("value", ((IReadOnlyDictionary<string, string>)MyStringToStringGenericIReadOnlyDict)["key"]);

            Assert.Equal("value", ((ImmutableDictionary<string, string>)MyStringToStringImmutableDict)["key"]);
            Assert.Equal("value", ((IImmutableDictionary<string, string>)MyStringToStringIImmutableDict)["key"]);
            Assert.Equal("value", ((ImmutableSortedDictionary<string, string>)MyStringToStringImmutableSortedDict)["key"]);

            Assert.Equal("myKey", ((KeyValuePair<string, string>)MyStringToStringKeyValuePair).Key);
            Assert.Equal("myValue", ((KeyValuePair<string, string>)MyStringToStringKeyValuePair).Value);

            Assert.Equal(2, ((Stack<string>)MyStringStackT).Count);
            Assert.True(((Stack<string>)MyStringStackT).Contains("Hello"));
            Assert.True(((Stack<string>)MyStringStackT).Contains("World"));

            string[] expectedQueue = { "Hello", "World" };
            int i = 0;
            foreach (string item in ((Queue<string>)MyStringQueueT))
            {
                Assert.Equal(expectedQueue[i], item);
                i++;
            }

            Assert.Equal("Hello", ((HashSet<string>)MyStringHashSetT).First());
            Assert.Equal("Hello", ((LinkedList<string>)MyStringLinkedListT).First());
            Assert.Equal("Hello", ((SortedSet<string>)MyStringSortedSetT).First());

            Assert.Equal("Hello", ((IImmutableList<string>)MyStringIImmutableListT)[0]);
            Assert.Equal("Hello", ((IImmutableStack<string>)MyStringIImmutableStackT).First());
            Assert.Equal("Hello", ((IImmutableQueue<string>)MyStringIImmutableQueueT).First());
            Assert.Equal("Hello", ((IImmutableSet<string>)MyStringIImmutableSetT).First());
            Assert.Equal("Hello", ((ImmutableHashSet<string>)MyStringImmutableHashSetT).First());
            Assert.Equal("Hello", ((ImmutableList<string>)MyStringImmutableListT)[0]);
            Assert.Equal("Hello", ((ImmutableStack<string>)MyStringImmutableStackT).First());
            Assert.Equal("Hello", ((ImmutableQueue<string>)MyStringImmutablQueueT).First());
            Assert.Equal("Hello", ((ImmutableSortedSet<string>)MyStringImmutableSortedSetT).First());
        }
    }
}
