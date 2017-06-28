// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections;
using System.Linq;

using Xunit;

namespace System.Data.SqlClient.Tests
{
    public partial class SqlConnectionTest
    {
        private static readonly string[] s_retrieveStatisticsKeys =
        {
            "BuffersReceived",
            "BuffersSent",
            "BytesReceived",
            "BytesSent",
            "CursorOpens",
            "IduCount",
            "IduRows",
            "PreparedExecs",
            "Prepares",
            "SelectCount",
            "SelectRows",
            "ServerRoundtrips",
            "SumResultSets",
            "Transactions",
            "UnpreparedExecs",
            "ConnectionTime",
            "ExecutionTime",
            "NetworkServerTime"
        };

        [Fact]
        public void RetrieveStatistics_Success()
        {
            var connection = new SqlConnection();
            IDictionary d = connection.RetrieveStatistics();
            Assert.NotNull(d);
            Assert.NotSame(d, connection.RetrieveStatistics());
        }

        [Fact]
        public void RetrieveStatistics_ExpectedKeysInDictionary_Success()
        {
            IDictionary d = new SqlConnection().RetrieveStatistics();

            Assert.NotEmpty(d);
            Assert.Equal(s_retrieveStatisticsKeys.Length, d.Count);

            Assert.NotEmpty(d.Keys);
            Assert.Equal(s_retrieveStatisticsKeys.Length, d.Keys.Count);

            Assert.NotEmpty(d.Values);
            Assert.Equal(s_retrieveStatisticsKeys.Length, d.Values.Count);

            foreach (string key in s_retrieveStatisticsKeys)
            {
                Assert.True(d.Contains(key));

                object value = d[key];
                Assert.NotNull(value);
                Assert.IsType<long>(value);
                Assert.Equal(0L, value);
            }
        }

        [Fact]
        public void RetrieveStatistics_UnexpectedKeysNotInDictionary_Success()
        {
            IDictionary d = new SqlConnection().RetrieveStatistics();
            Assert.False(d.Contains("Foo"));
            Assert.Null(d["Foo"]);
        }

        [Fact]
        public void RetrieveStatistics_IsSynchronized_Success()
        {
            IDictionary d = new SqlConnection().RetrieveStatistics();
            Assert.False(d.IsSynchronized);
        }

        [Fact]
        public void RetrieveStatistics_SyncRoot_Success()
        {
            IDictionary d = new SqlConnection().RetrieveStatistics();
            Assert.NotNull(d.SyncRoot);
            Assert.Same(d.SyncRoot, d.SyncRoot);
        }

        [Fact]
        public void RetrieveStatistics_IsFixedSize_Success()
        {
            IDictionary d = new SqlConnection().RetrieveStatistics();
            Assert.False(d.IsFixedSize);
        }

        [Fact]
        public void RetrieveStatistics_IsReadOnly_Success()
        {
            IDictionary d = new SqlConnection().RetrieveStatistics();
            Assert.False(d.IsReadOnly);
        }

        public static readonly object[][] RetrieveStatisticsKeyValueData =
        {
            new object[] { "Foo", 100L },
            new object[] { "Foo", null },
            new object[] { "Blah", "Blah" },
            new object[] { 100, "Value" }
        };

        [Theory]
        [MemberData(nameof(RetrieveStatisticsKeyValueData))]
        public void RetrieveStatistics_Add_Success(object key, object value)
        {
            IDictionary d = new SqlConnection().RetrieveStatistics();

            d.Add(key, value);

            Assert.True(d.Contains(key));

            object v = d[key];
            Assert.Same(value, v);
        }

        [Fact]
        public void RetrieveStatistics_Add_ExistingKey_Throws()
        {
            IDictionary d = new SqlConnection().RetrieveStatistics();
            string key = s_retrieveStatisticsKeys[0];
            AssertExtensions.Throws<ArgumentException>(null, () => d.Add(key, 100L));
        }

        [Fact]
        public void RetrieveStatistics_Add_NullKey_Throws()
        {
            IDictionary d = new SqlConnection().RetrieveStatistics();
            AssertExtensions.Throws<ArgumentNullException>("key", () => d.Add(null, 100L));
        }

        [Theory]
        [MemberData(nameof(RetrieveStatisticsKeyValueData))]
        public void RetrieveStatistics_Setter_Success(object key, object value)
        {
            IDictionary d = new SqlConnection().RetrieveStatistics();

            d[key] = value;

            Assert.True(d.Contains(key));

            object v = d[key];
            Assert.Same(value, v);
        }

        [Fact]
        public void RetrieveStatistics_Setter_ExistingKey_Success()
        {
            IDictionary d = new SqlConnection().RetrieveStatistics();
            string key = s_retrieveStatisticsKeys[0];

            d[key] = 100L;
            Assert.Equal(100L, d[key]);
        }

        [Fact]
        public void RetrieveStatistics_Setter_NullKey_Throws()
        {
            IDictionary d = new SqlConnection().RetrieveStatistics();
            AssertExtensions.Throws<ArgumentNullException>("key", () => d[null] = 100L);
        }

        [Fact]
        public void RetrieveStatistics_Clear_Success()
        {
            IDictionary d = new SqlConnection().RetrieveStatistics();

            d.Clear();

            Assert.Empty(d);
            Assert.Equal(0, d.Count);

            Assert.Empty(d.Keys);
            Assert.Equal(0, d.Keys.Count);

            Assert.Empty(d.Values);
            Assert.Equal(0, d.Values.Count);
        }

        [Fact]
        public void RetrieveStatistics_Remove_ExistingKey_Success()
        {
            IDictionary d = new SqlConnection().RetrieveStatistics();

            string key = s_retrieveStatisticsKeys[0];

            Assert.Equal(s_retrieveStatisticsKeys.Length, d.Count);
            Assert.Equal(s_retrieveStatisticsKeys.Length, d.Keys.Count);
            Assert.Equal(s_retrieveStatisticsKeys.Length, d.Values.Count);
            Assert.True(d.Contains(key));
            Assert.NotNull(d[key]);

            d.Remove(key);

            Assert.Equal(s_retrieveStatisticsKeys.Length - 1, d.Count);
            Assert.Equal(s_retrieveStatisticsKeys.Length - 1, d.Keys.Count);
            Assert.Equal(s_retrieveStatisticsKeys.Length - 1, d.Values.Count);
            Assert.False(d.Contains(key));
            Assert.Null(d[key]);
        }

        [Fact]
        public void RetrieveStatistics_Remove_NonExistentKey_Success()
        {
            IDictionary d = new SqlConnection().RetrieveStatistics();

            const string key = "Foo";

            Assert.Equal(s_retrieveStatisticsKeys.Length, d.Count);
            Assert.Equal(s_retrieveStatisticsKeys.Length, d.Keys.Count);
            Assert.Equal(s_retrieveStatisticsKeys.Length, d.Values.Count);
            Assert.False(d.Contains(key));
            Assert.Null(d[key]);

            d.Remove(key);

            Assert.Equal(s_retrieveStatisticsKeys.Length, d.Count);
            Assert.Equal(s_retrieveStatisticsKeys.Length, d.Keys.Count);
            Assert.Equal(s_retrieveStatisticsKeys.Length, d.Values.Count);
            Assert.False(d.Contains(key));
            Assert.Null(d[key]);
        }

        [Fact]
        public void RetrieveStatistics_Remove_NullKey_Throws()
        {
            IDictionary d = new SqlConnection().RetrieveStatistics();
            AssertExtensions.Throws<ArgumentNullException>("key", () => d.Remove(null));
        }

        [Fact]
        public void RetrieveStatistics_Contains_NullKey_Throws()
        {
            IDictionary d = new SqlConnection().RetrieveStatistics();
            AssertExtensions.Throws<ArgumentNullException>("key", () => d.Contains(null));
        }

        [Fact]
        public void RetrieveStatistics_CopyTo_Success()
        {
            IDictionary d = new SqlConnection().RetrieveStatistics();
            DictionaryEntry[] destination = new DictionaryEntry[d.Count];

            d.CopyTo(destination, 0);

            int i = 0;
            foreach (DictionaryEntry entry in d)
            {
                Assert.Equal(entry, destination[i]);
                i++;
            }
        }

        [Fact]
        public void RetrieveStatistics_CopyTo_Throws()
        {
            IDictionary d = new SqlConnection().RetrieveStatistics();

            AssertExtensions.Throws<ArgumentNullException>("array", () => d.CopyTo(null, 0));
            AssertExtensions.Throws<ArgumentNullException>("array", () => d.CopyTo(null, -1));
            AssertExtensions.Throws<ArgumentOutOfRangeException>("arrayIndex", () => d.CopyTo(new DictionaryEntry[20], -1));
            AssertExtensions.Throws<ArgumentException>(null, () => d.CopyTo(new DictionaryEntry[20], 18));
            AssertExtensions.Throws<ArgumentException>(null, () => d.CopyTo(new DictionaryEntry[20], 1000));
            AssertExtensions.Throws<ArgumentException>(null, () => d.CopyTo(new DictionaryEntry[4, 3], 0));
            Assert.Throws<InvalidCastException>(() => d.CopyTo(new string[20], 0));
        }

        [Fact]
        public void RetrieveStatistics_IDictionary_GetEnumerator_Success()
        {
            IDictionary d = new SqlConnection().RetrieveStatistics();

            IDictionaryEnumerator e = d.GetEnumerator();

            Assert.NotNull(e);
            Assert.NotSame(e, d.GetEnumerator());

            for (int i = 0; i < 2; i++)
            {
                Assert.Throws<InvalidOperationException>(() => e.Current);

                foreach (string ignored in s_retrieveStatisticsKeys)
                {
                    Assert.True(e.MoveNext());

                    Assert.NotNull(e.Current);
                    Assert.IsType<DictionaryEntry>(e.Current);

                    Assert.NotNull(e.Entry.Key);
                    Assert.IsType<string>(e.Entry.Key);
                    Assert.NotNull(e.Entry.Value);
                    Assert.IsType<long>(e.Entry.Value);

                    Assert.Equal(e.Current, e.Entry);
                    Assert.Same(e.Key, e.Entry.Key);
                    Assert.Same(e.Value, e.Entry.Value);

                    Assert.True(s_retrieveStatisticsKeys.Contains(e.Entry.Key));
                }

                Assert.False(e.MoveNext());
                Assert.False(e.MoveNext());
                Assert.False(e.MoveNext());

                Assert.Throws<InvalidOperationException>(() => e.Current);

                e.Reset();
            }
        }

        [Fact]
        public void RetrieveStatistics_IEnumerable_GetEnumerator_Success()
        {
            // Treat the result as IEnumerable instead of IDictionary.
            IEnumerable d = new SqlConnection().RetrieveStatistics();

            IEnumerator e = d.GetEnumerator();

            Assert.NotNull(e);
            Assert.NotSame(e, d.GetEnumerator());

            for (int i = 0; i < 2; i++)
            {
                Assert.Throws<InvalidOperationException>(() => e.Current);

                foreach (string ignored in s_retrieveStatisticsKeys)
                {
                    Assert.True(e.MoveNext());

                    Assert.NotNull(e.Current);

                    // Verify the IEnumerable.GetEnumerator enumerator is yielding DictionaryEntry entries,
                    // not KeyValuePair entries.
                    Assert.IsType<DictionaryEntry>(e.Current);

                    DictionaryEntry entry = (DictionaryEntry)e.Current;

                    Assert.NotNull(entry.Key);
                    Assert.IsType<string>(entry.Key);
                    Assert.NotNull(entry.Value);
                    Assert.IsType<long>(entry.Value);

                    Assert.True(s_retrieveStatisticsKeys.Contains(entry.Key));
                }

                Assert.False(e.MoveNext());
                Assert.False(e.MoveNext());
                Assert.False(e.MoveNext());

                Assert.Throws<InvalidOperationException>(() => e.Current);

                e.Reset();
            }
        }

        [Fact]
        public void RetrieveStatistics_GetEnumerator_ModifyCollection_Throws()
        {
            IDictionary d = new SqlConnection().RetrieveStatistics();

            IDictionaryEnumerator e = d.GetEnumerator();

            d.Add("Foo", 0L);

            Assert.Throws<InvalidOperationException>(() => e.MoveNext());
            Assert.Throws<InvalidOperationException>(() => e.Reset());
        }

        [Fact]
        public void RetrieveStatistics_Keys_Success()
        {
            IDictionary d = new SqlConnection().RetrieveStatistics();
            Assert.NotNull(d.Keys);
            Assert.Same(d.Keys, d.Keys);
        }

        [Fact]
        public void RetrieveStatistics_Keys_IsSynchronized_Success()
        {
            IDictionary d = new SqlConnection().RetrieveStatistics();
            ICollection c = d.Keys;
            Assert.False(c.IsSynchronized);
        }

        [Fact]
        public void RetrieveStatistics_Keys_SyncRoot_Success()
        {
            IDictionary d = new SqlConnection().RetrieveStatistics();
            ICollection c = d.Keys;
            Assert.NotNull(c.SyncRoot);
            Assert.Same(c.SyncRoot, c.SyncRoot);
        }

        [Fact]
        public void RetrieveStatistics_Keys_CopyTo_ObjectArray_Success()
        {
            IDictionary d = new SqlConnection().RetrieveStatistics();
            ICollection c = d.Keys;
            object[] destination = new object[c.Count];

            c.CopyTo(destination, 0);

            Assert.Equal(c.Cast<object>().ToArray(), destination);
        }

        [Fact]
        public void RetrieveStatistics_Keys_CopyTo_StringArray_Success()
        {
            IDictionary d = new SqlConnection().RetrieveStatistics();
            ICollection c = d.Keys;
            string[] destination = new string[c.Count];

            c.CopyTo(destination, 0);

            Assert.Equal(c.Cast<string>().ToArray(), destination);
        }

        [Fact]
        public void RetrieveStatistics_Keys_CopyTo_Throws()
        {
            IDictionary d = new SqlConnection().RetrieveStatistics();
            ICollection c = d.Keys;

            AssertExtensions.Throws<ArgumentNullException>("array", () => c.CopyTo(null, 0));
            AssertExtensions.Throws<ArgumentNullException>("array", () => c.CopyTo(null, -1));
            AssertExtensions.Throws<ArgumentOutOfRangeException>("arrayIndex", () => c.CopyTo(new string[20], -1));
            AssertExtensions.Throws<ArgumentException>(null, () => c.CopyTo(new string[20], 18));
            AssertExtensions.Throws<ArgumentException>(null, () => c.CopyTo(new string[20], 1000));
            AssertExtensions.Throws<ArgumentException>(null, () => c.CopyTo(new string[4, 3], 0));
            Assert.Throws<InvalidCastException>(() => c.CopyTo(new Version[20], 0));
        }

        [Fact]
        public void RetrieveStatistics_Keys_GetEnumerator_Success()
        {
            IDictionary d = new SqlConnection().RetrieveStatistics();
            ICollection c = d.Keys;

            IEnumerator e = c.GetEnumerator();

            Assert.NotNull(e);
            Assert.NotSame(e, c.GetEnumerator());

            for (int i = 0; i < 2; i++)
            {
                Assert.Throws<InvalidOperationException>(() => e.Current);

                foreach (string ignored in s_retrieveStatisticsKeys)
                {
                    Assert.True(e.MoveNext());
                    Assert.NotNull(e.Current);
                    Assert.True(s_retrieveStatisticsKeys.Contains(e.Current));
                }

                Assert.False(e.MoveNext());
                Assert.False(e.MoveNext());
                Assert.False(e.MoveNext());

                Assert.Throws<InvalidOperationException>(() => e.Current);

                e.Reset();
            }
        }

        [Fact]
        public void RetrieveStatistics_Keys_GetEnumerator_ModifyCollection_Throws()
        {
            IDictionary d = new SqlConnection().RetrieveStatistics();
            ICollection c = d.Keys;

            IEnumerator e = c.GetEnumerator();

            d.Add("Foo", 0L);

            Assert.Throws<InvalidOperationException>(() => e.MoveNext());
            Assert.Throws<InvalidOperationException>(() => e.Reset());
        }

        [Fact]
        public void RetrieveStatistics_Values_Success()
        {
            IDictionary d = new SqlConnection().RetrieveStatistics();
            Assert.NotNull(d.Values);
            Assert.Same(d.Values, d.Values);
        }

        [Fact]
        public void RetrieveStatistics_Values_IsSynchronized_Success()
        {
            IDictionary d = new SqlConnection().RetrieveStatistics();
            ICollection c = d.Values;
            Assert.False(c.IsSynchronized);
        }

        [Fact]
        public void RetrieveStatistics_Values_SyncRoot_Success()
        {
            IDictionary d = new SqlConnection().RetrieveStatistics();
            ICollection c = d.Values;
            Assert.NotNull(c.SyncRoot);
            Assert.Same(c.SyncRoot, c.SyncRoot);
        }

        [Fact]
        public void RetrieveStatistics_Values_CopyTo_ObjectArray_Success()
        {
            IDictionary d = new SqlConnection().RetrieveStatistics();
            ICollection c = d.Values;
            object[] destination = new object[c.Count];

            c.CopyTo(destination, 0);

            Assert.Equal(c.Cast<object>().ToArray(), destination);
        }

        [Fact]
        public void RetrieveStatistics_Values_CopyTo_Int64Array_Success()
        {
            IDictionary d = new SqlConnection().RetrieveStatistics();
            ICollection c = d.Values;
            long[] destination = new long[c.Count];

            c.CopyTo(destination, 0);

            Assert.Equal(c.Cast<long>().ToArray(), destination);
        }

        [Fact]
        public void RetrieveStatistics_Values_CopyTo_Throws()
        {
            IDictionary d = new SqlConnection().RetrieveStatistics();
            ICollection c = d.Values;

            AssertExtensions.Throws<ArgumentNullException>("array", () => c.CopyTo(null, 0));
            AssertExtensions.Throws<ArgumentNullException>("array", () => c.CopyTo(null, -1));
            AssertExtensions.Throws<ArgumentOutOfRangeException>("arrayIndex", () => c.CopyTo(new long[20], -1));
            AssertExtensions.Throws<ArgumentException>(null, () => c.CopyTo(new long[20], 18));
            AssertExtensions.Throws<ArgumentException>(null, () => c.CopyTo(new long[20], 1000));
            AssertExtensions.Throws<ArgumentException>(null, () => c.CopyTo(new long[4, 3], 0));
            Assert.Throws<InvalidCastException>(() => c.CopyTo(new Version[20], 0));
        }

        [Fact]
        public void RetrieveStatistics_Values_GetEnumerator_Success()
        {
            IDictionary d = new SqlConnection().RetrieveStatistics();
            ICollection c = d.Values;

            IEnumerator e = c.GetEnumerator();

            Assert.NotNull(e);
            Assert.NotSame(e, c.GetEnumerator());

            for (int i = 0; i < 2; i++)
            {
                Assert.Throws<InvalidOperationException>(() => e.Current);

                foreach (string ignored in s_retrieveStatisticsKeys)
                {
                    Assert.True(e.MoveNext());
                    Assert.NotNull(e.Current);
                    Assert.Equal(0L, e.Current);
                }

                Assert.False(e.MoveNext());
                Assert.False(e.MoveNext());
                Assert.False(e.MoveNext());

                Assert.Throws<InvalidOperationException>(() => e.Current);

                e.Reset();
            }
        }

        [Fact]
        public void RetrieveStatistics_Values_GetEnumerator_ModifyCollection_Throws()
        {
            IDictionary d = new SqlConnection().RetrieveStatistics();
            ICollection c = d.Values;

            IEnumerator e = c.GetEnumerator();

            d.Add("Foo", 0L);

            Assert.Throws<InvalidOperationException>(() => e.MoveNext());
            Assert.Throws<InvalidOperationException>(() => e.Reset());
        }
    }
}
