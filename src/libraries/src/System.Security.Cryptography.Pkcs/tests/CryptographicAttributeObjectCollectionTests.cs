// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.IO;
using System.Linq;
using System.Globalization;
using System.Collections;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Runtime.InteropServices;
using System.Text;
using System.Security.Cryptography.Pkcs;
using System.Security.Cryptography.Xml;
using System.Security.Cryptography.X509Certificates;
using Xunit;

using Test.Cryptography;

namespace System.Security.Cryptography.Pkcs.Tests
{
    public static class CryptographicAttributeObjectCollectionTests
    {
        [Fact]
        public static void Nullary()
        {
            CryptographicAttributeObjectCollection c = new CryptographicAttributeObjectCollection();
            AssertEquals(c, Array.Empty<CryptographicAttributeObject>());
        }

        [Fact]
        public static void Oneary()
        {
            CryptographicAttributeObject a0 = s_ca0;
            CryptographicAttributeObjectCollection c = new CryptographicAttributeObjectCollection(a0);
            AssertEquals(c, new CryptographicAttributeObject[] { a0 });
        }

        [Fact]
        public static void Add()
        {
            CryptographicAttributeObject a0 = s_ca0;
            CryptographicAttributeObject a1 = s_ca1;
            CryptographicAttributeObject a2 = s_ca2;

            CryptographicAttributeObjectCollection c = new CryptographicAttributeObjectCollection();
            int index;
            index = c.Add(a0);
            Assert.Equal(0, index);
            index = c.Add(a1);
            Assert.Equal(1, index);
            index = c.Add(a2);
            Assert.Equal(2, index);

            AssertEquals(c, new CryptographicAttributeObject[] { a0, a1, a2 });
        }

        [Fact]
        public static void AddFold()
        {
            AsnEncodedData dd1 = new Pkcs9DocumentDescription("My Description 1");
            AsnEncodedData dd2 = new Pkcs9DocumentDescription("My Description 2");

            CryptographicAttributeObjectCollection c = new CryptographicAttributeObjectCollection();
            int index;
            index = c.Add(dd1);
            Assert.Equal(0, index);
            index = c.Add(dd2);
            Assert.Equal(0, index);

            AsnEncodedDataCollection expectedValues = new AsnEncodedDataCollection();
            expectedValues.Add(dd1);
            expectedValues.Add(dd2);
            CryptographicAttributeObject expected = new CryptographicAttributeObject(dd1.Oid, expectedValues);
            AssertEquals(c, new CryptographicAttributeObject[] { expected });
        }

        [Fact]
        public static void Remove()
        {
            CryptographicAttributeObject a0 = s_ca0;
            CryptographicAttributeObject a1 = s_ca1;
            CryptographicAttributeObject a2 = s_ca2;

            CryptographicAttributeObjectCollection c = new CryptographicAttributeObjectCollection();
            int index;
            index = c.Add(a0);
            Assert.Equal(0, index);
            index = c.Add(a1);
            Assert.Equal(1, index);
            index = c.Add(a2);
            Assert.Equal(2, index);

            c.Remove(a1);

            AssertEquals(c, new CryptographicAttributeObject[] { a0, a2 });
        }

        [Fact]
        public static void AddNegative()
        {
            CryptographicAttributeObjectCollection c = new CryptographicAttributeObjectCollection();
            Assert.Throws<ArgumentNullException>(() => c.Add((CryptographicAttributeObject)null));
            Assert.Throws<ArgumentNullException>(() => c.Add((AsnEncodedData)null));
        }

        [Fact]
        public static void RemoveNegative()
        {
            CryptographicAttributeObjectCollection c = new CryptographicAttributeObjectCollection();
            Assert.Throws<ArgumentNullException>(() => c.Remove(null));
        }

        [Fact]
        public static void RemoveNonExistent()
        {
            CryptographicAttributeObjectCollection c = new CryptographicAttributeObjectCollection();
            CryptographicAttributeObject a0 = s_ca0;
            c.Remove(a0);  // You can "remove" items that aren't in the collection - this is defined as a NOP.
        }

        [Fact]
        public static void IndexOutOfBounds()
        {
            CryptographicAttributeObject a0 = s_ca0;
            CryptographicAttributeObject a1 = s_ca1;
            CryptographicAttributeObject a2 = s_ca2;

            CryptographicAttributeObjectCollection c = new CryptographicAttributeObjectCollection();
            c.Add(a0);
            c.Add(a1);
            c.Add(a2);

            object ignore = null;
            Assert.Throws<ArgumentOutOfRangeException>(() => ignore = c[-1]);
            Assert.Throws<ArgumentOutOfRangeException>(() => ignore = c[3]);
        }

        [Fact]
        public static void CopyExceptions()
        {
            CryptographicAttributeObject a0 = s_ca0;
            CryptographicAttributeObject a1 = s_ca1;
            CryptographicAttributeObject a2 = s_ca2;

            CryptographicAttributeObjectCollection c = new CryptographicAttributeObjectCollection();
            c.Add(a0);
            c.Add(a1);
            c.Add(a2);

            CryptographicAttributeObject[] a = new CryptographicAttributeObject[3];
            Assert.Throws<ArgumentNullException>(() => c.CopyTo(null, 0));
            Assert.Throws<ArgumentOutOfRangeException>(() => c.CopyTo(a, -1));
            Assert.Throws<ArgumentOutOfRangeException>(() => c.CopyTo(a, 3));
            AssertExtensions.Throws<ArgumentException>(null, () => c.CopyTo(a, 1));

            ICollection ic = c;
            Assert.Throws<ArgumentNullException>(() => ic.CopyTo(null, 0));
            Assert.Throws<ArgumentOutOfRangeException>(() => ic.CopyTo(a, -1));
            Assert.Throws<ArgumentOutOfRangeException>(() => ic.CopyTo(a, 3));
            AssertExtensions.Throws<ArgumentException>(null, () => ic.CopyTo(a, 1));
            AssertExtensions.Throws<ArgumentException>(null, () => ic.CopyTo(new CryptographicAttributeObject[2, 2], 0));
            Assert.Throws<InvalidCastException>(() => ic.CopyTo(new int[10], 0));

            if (PlatformDetection.IsNonZeroLowerBoundArraySupported)
            {
                // Array has non-zero lower bound
                Array array = Array.CreateInstance(typeof(object), new int[] { 10 }, new int[] { 10 });
                Assert.Throws<IndexOutOfRangeException>(() => ic.CopyTo(array, 0));
            }
        }

        private static void AssertEquals(CryptographicAttributeObjectCollection c, IList<CryptographicAttributeObject> expected)
        {
            Assert.Equal(expected.Count, c.Count);

            for (int i = 0; i < c.Count; i++)
            {
                Assert.Equal(expected[i], c[i], s_CryptographicAttributeObjectComparer);
            }

            int index = 0;
            foreach (CryptographicAttributeObject a in c)
            {
                Assert.Equal(expected[index++], a, s_CryptographicAttributeObjectComparer);
            }
            Assert.Equal(c.Count, index);

            ValidateEnumerator(c.GetEnumerator(), expected);
            ValidateEnumerator(((ICollection)c).GetEnumerator(), expected);

            {
                CryptographicAttributeObject[] dumped = new CryptographicAttributeObject[c.Count + 3];
                c.CopyTo(dumped, 2);
                Assert.Equal(null, dumped[0]);
                Assert.Equal(null, dumped[1]);
                Assert.Equal(null, dumped[dumped.Length - 1]);
                for (int i = 0; i < expected.Count; i++)
                {
                    Assert.Equal(expected[i], dumped[i + 2], s_CryptographicAttributeObjectComparer);
                }
            }
            {
                CryptographicAttributeObject[] dumped = new CryptographicAttributeObject[c.Count + 3];
                ((ICollection)c).CopyTo(dumped, 2);
                Assert.Equal(null, dumped[0]);
                Assert.Equal(null, dumped[1]);
                Assert.Equal(null, dumped[dumped.Length - 1]);
                for (int i = 0; i < expected.Count; i++)
                {
                    Assert.Equal(expected[i], dumped[i + 2], s_CryptographicAttributeObjectComparer);
                }
            }
        }

        private static void ValidateEnumerator(IEnumerator enumerator, IList<CryptographicAttributeObject> expected)
        {
            foreach (CryptographicAttributeObject e in expected)
            {
                Assert.True(enumerator.MoveNext());
                CryptographicAttributeObject actual = (CryptographicAttributeObject)(enumerator.Current);
                Assert.Equal(e, actual, s_CryptographicAttributeObjectComparer);
            }
            Assert.False(enumerator.MoveNext());
        }

        private sealed class CryptographicEqualityComparer : IEqualityComparer<CryptographicAttributeObject>
        {
            public bool Equals(CryptographicAttributeObject x, CryptographicAttributeObject y)
            {
                if (x.Oid.Value != y.Oid.Value)
                    return false;

                AsnEncodedDataCollection xv = x.Values;
                AsnEncodedDataCollection yv = y.Values;
                if (xv.Count != yv.Count)
                    return false;

                for (int i = 0; i < xv.Count; i++)
                {
                    AsnEncodedData xa = xv[i];
                    AsnEncodedData ya = yv[i];
                    if (xa.Oid.Value != ya.Oid.Value)
                        return false;
                    if (!xa.RawData.SequenceEqual(ya.RawData))
                        return false;
                }

                return true;
            }

            public int GetHashCode(CryptographicAttributeObject obj)
            {
                return 1;
            }
        }

        private static readonly CryptographicAttributeObject s_ca0 = new CryptographicAttributeObject(new Oid(Oids.DocumentName), new AsnEncodedDataCollection(new Pkcs9DocumentName("My Name")));
        private static readonly CryptographicAttributeObject s_ca1 = new CryptographicAttributeObject(new Oid(Oids.DocumentDescription), new AsnEncodedDataCollection(new Pkcs9DocumentDescription("My Description")));
        private static readonly CryptographicAttributeObject s_ca2 = new CryptographicAttributeObject(new Oid(Oids.SigningTime), new AsnEncodedDataCollection(new Pkcs9SigningTime(new DateTime(2015, 4, 1, 12, 30, 20))));

        private static readonly CryptographicEqualityComparer s_CryptographicAttributeObjectComparer = new CryptographicEqualityComparer();
    }
}


