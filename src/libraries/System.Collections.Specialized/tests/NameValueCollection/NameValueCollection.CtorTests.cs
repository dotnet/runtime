// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections;
using System.Collections.Generic;
using Xunit;

namespace System.Collections.Specialized.Tests
{
    public class NameValueCollectionCtorTests
    {
        [Fact]
        public void Ctor_Empty()
        {
            NameValueCollection nameValueCollection = new NameValueCollection();
            Assert.Equal(0, nameValueCollection.Count);
            Assert.Equal(0, nameValueCollection.Keys.Count);
            Assert.Equal(0, nameValueCollection.AllKeys.Length);

            Assert.False(((ICollection)nameValueCollection).IsSynchronized);
        }

        [Fact]
        public void Ctor_Provider_Comparer()
        {
#pragma warning disable CS0618 // Type or member is obsolete
            NameValueCollection nameValueCollection = new NameValueCollection(CaseInsensitiveHashCodeProvider.Default, CaseInsensitiveComparer.Default);
#pragma warning restore CS0618 // Type or member is obsolete
            Assert.Equal(0, nameValueCollection.Count);
            Assert.Equal(0, nameValueCollection.Keys.Count);
            Assert.Equal(0, nameValueCollection.AllKeys.Length);

            Assert.False(((ICollection)nameValueCollection).IsSynchronized);
        }

        [Fact]
        public void Ctor_Int_Provider_Comparer()
        {
#pragma warning disable CS0618 // Type or member is obsolete
            NameValueCollection nameValueCollection = new NameValueCollection(5, CaseInsensitiveHashCodeProvider.Default, CaseInsensitiveComparer.Default);
#pragma warning restore CS0618 // Type or member is obsolete
            Assert.Equal(0, nameValueCollection.Count);
            Assert.Equal(0, nameValueCollection.Keys.Count);
            Assert.Equal(0, nameValueCollection.AllKeys.Length);

            Assert.False(((ICollection)nameValueCollection).IsSynchronized);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(5)]
        public void Ctor_Int(int capacity)
        {
            NameValueCollection nameValueCollection = new NameValueCollection(capacity);
            Assert.Equal(0, nameValueCollection.Count);
            Assert.Equal(0, nameValueCollection.Keys.Count);
            Assert.Equal(0, nameValueCollection.AllKeys.Length);

            Assert.False(((ICollection)nameValueCollection).IsSynchronized);

            int newCount = capacity + 10;
            for (int i = 0; i < newCount; i++)
            {
                nameValueCollection.Add("Name_" + i, "Value_" + i);
            }
            Assert.Equal(newCount, nameValueCollection.Count);
        }

        [Fact]
        public void Ctor_NegativeCapacity_ThrowsArgumentOutOfRangeException()
        {
            AssertExtensions.Throws<ArgumentOutOfRangeException>("capacity", () => new NameValueCollection(-1));
            AssertExtensions.Throws<ArgumentOutOfRangeException>("capacity", () => new NameValueCollection(-1, new NameValueCollection()));
            AssertExtensions.Throws<ArgumentOutOfRangeException>("capacity", () => new NameValueCollection(-1, (IEqualityComparer)null
                ));

            Assert.Throws<OutOfMemoryException>(() => new NameValueCollection(int.MaxValue));
        }

        public static IEnumerable<object[]> Ctor_NameValueCollection_TestData()
        {
            yield return new object[] { new NameValueCollection(10) };
            yield return new object[] { new NameValueCollection() };
            yield return new object[] { Helpers.CreateNameValueCollection(10) };
        }

        [Theory]
        [MemberData(nameof(Ctor_NameValueCollection_TestData))]
        public void Ctor_NameValueCollection(NameValueCollection nameValueCollection1)
        {
            NameValueCollection nameValueCollection2 = new NameValueCollection(nameValueCollection1);

            Assert.Equal(nameValueCollection1.Count, nameValueCollection2.Count);
            Assert.Equal(nameValueCollection1.Keys, nameValueCollection2.Keys);
            Assert.Equal(nameValueCollection1.AllKeys, nameValueCollection2.AllKeys);

            Assert.False(((ICollection)nameValueCollection2).IsSynchronized);

            // Modify nameValueCollection1 does not affect nameValueCollection2
            string previous = nameValueCollection1["Name_1"];
            nameValueCollection1["Name_1"] = "newvalue";
            Assert.Equal(previous, nameValueCollection2["Name_1"]);

            nameValueCollection1.Remove("Name_1");
            Assert.Equal(previous, nameValueCollection2["Name_1"]);
        }

        [Fact]
        public void Ctor_NullNameValueCollection_ThrowsArgumentNullException()
        {
            AssertExtensions.Throws<ArgumentNullException>("c", () => new NameValueCollection((NameValueCollection)null));
            AssertExtensions.Throws<ArgumentNullException>("col", () => new NameValueCollection(0, (NameValueCollection)null));
        }
        
        public static IEnumerable<object[]> Ctor_Int_NameValueCollection_TestData()
        {
            yield return new object[] { 0, new NameValueCollection() };

            yield return new object[] { 10, new NameValueCollection(10) };
            yield return new object[] { 5, new NameValueCollection(10) };
            yield return new object[] { 15, new NameValueCollection(10) };

            yield return new object[] { 10, Helpers.CreateNameValueCollection(10) };
            yield return new object[] { 5, Helpers.CreateNameValueCollection(10) };
            yield return new object[] { 15, Helpers.CreateNameValueCollection(10) };
        }

        [Theory]
        [MemberData(nameof(Ctor_Int_NameValueCollection_TestData))]
        public void Ctor_Int_NameValueCollection(int capacity, NameValueCollection nameValueCollection1)
        {
            NameValueCollection nameValueCollection2 = new NameValueCollection(capacity, nameValueCollection1);

            Assert.Equal(nameValueCollection1.Count, nameValueCollection2.Count);
            Assert.Equal(nameValueCollection1.Keys, nameValueCollection2.Keys);
            Assert.Equal(nameValueCollection1.AllKeys, nameValueCollection2.AllKeys);

            Assert.False(((ICollection)nameValueCollection2).IsSynchronized);

            // Modify nameValueCollection1 does not affect nameValueCollection2
            string previous = nameValueCollection1["Name_1"];
            nameValueCollection1["Name_1"] = "newvalue";
            Assert.Equal(previous, nameValueCollection2["Name_1"]);

            nameValueCollection1.Remove("Name_1");
            Assert.Equal(previous, nameValueCollection2["Name_1"]);
        }

        public static IEnumerable<object[]> Ctor_Int_IEqualityComparer_TestData()
        {
            yield return new object[] { 0, new IdiotComparer() };
            yield return new object[] { 10, new IdiotComparer() };
            yield return new object[] { 1000, new IdiotComparer() };
            yield return new object[] { 0, null };
            yield return new object[] { 10, null };
        }

        [Theory]
        [MemberData(nameof(Ctor_Int_IEqualityComparer_TestData))]
        public void Ctor_Int_IEqualityComparer(int capacity, IEqualityComparer equalityComparer)
        {
            NameValueCollection nameValueCollection = new NameValueCollection(capacity, equalityComparer);
            VerifyCtor_IEqualityComparer(nameValueCollection, equalityComparer, capacity + 10);
        }

        public static IEnumerable<object[]> Ctor_IEqualityComparer_TestData()
        {
            yield return new object[] { new IdiotComparer() };
            yield return new object[] { null };
        }

        [Theory]
        [MemberData(nameof(Ctor_IEqualityComparer_TestData))]
        public void Ctor_IEqualityComparer(IEqualityComparer equalityComparer)
        {
            NameValueCollection nameValueCollection = new NameValueCollection(equalityComparer);
            VerifyCtor_IEqualityComparer(nameValueCollection, equalityComparer, 10);
        }
        
        public void VerifyCtor_IEqualityComparer(NameValueCollection nameValueCollection, IEqualityComparer equalityComparer, int newCount)
        {
            Assert.Equal(0, nameValueCollection.Count);
            Assert.Equal(0, nameValueCollection.Keys.Count);
            Assert.Equal(0, nameValueCollection.AllKeys.Length);

            Assert.False(((ICollection)nameValueCollection).IsSynchronized);
            
            string[] values = new string[newCount];
            for (int i = 0; i < newCount; i++)
            {
                string value = "Value_" + i;
                nameValueCollection.Add("Name_" + i, value);
                values[i] = value;
            }
            if (equalityComparer?.GetType() == typeof(IdiotComparer))
            {
                Assert.Equal(1, nameValueCollection.Count);
                string expectedValues = string.Join(",", values);
                Assert.Equal(expectedValues, nameValueCollection["Name_1"]);
                Assert.Equal(expectedValues, nameValueCollection["any-name"]);

                nameValueCollection.Remove("any-name");
                Assert.Equal(0, nameValueCollection.Count);
            }
            else
            {
                Assert.Equal(newCount, nameValueCollection.Count);
                nameValueCollection.Remove("any-name");
                Assert.Equal(newCount, nameValueCollection.Count);
            }
        }

        public class IdiotComparer : IEqualityComparer
        {
            public new bool Equals(object x, object y) => true;
            public int GetHashCode(object obj) => 0;
        }
    }
}
