// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.IO;
using System.Text;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using Xunit;

namespace System.Security.Cryptography.Encoding.Tests
{
    public static class AsnEncodedDataCollectionTests
    {
        [Fact]
        public static void Nullary()
        {
            AsnEncodedDataCollection c = new AsnEncodedDataCollection();
            AssertEquals(c, Array.Empty<AsnEncodedData>());
        }

        [Fact]
        public static void Oneary()
        {
            AsnEncodedData a0 = new AsnEncodedData("1.0", Array.Empty<byte>());
            AsnEncodedDataCollection c = new AsnEncodedDataCollection(a0);
            AssertEquals(c, new AsnEncodedData[] { a0 });
        }

        [Fact]
        public static void Add()
        {
            AsnEncodedData a0 = new AsnEncodedData("1.0", Array.Empty<byte>());
            AsnEncodedData a1 = new AsnEncodedData("1.1", Array.Empty<byte>());
            AsnEncodedData a2 = new AsnEncodedData("1.2", Array.Empty<byte>());

            AsnEncodedDataCollection c = new AsnEncodedDataCollection();
            int index;
            index = c.Add(a0);
            Assert.Equal(0, index);
            index = c.Add(a1);
            Assert.Equal(1, index);
            index = c.Add(a2);
            Assert.Equal(2, index);

            AssertEquals(c, new AsnEncodedData[] { a0, a1, a2 });
        }

        [Fact]
        public static void Remove()
        {
            AsnEncodedData a0 = new AsnEncodedData("1.0", Array.Empty<byte>());
            AsnEncodedData a1 = new AsnEncodedData("1.1", Array.Empty<byte>());
            AsnEncodedData a2 = new AsnEncodedData("1.2", Array.Empty<byte>());

            AsnEncodedDataCollection c = new AsnEncodedDataCollection();
            int index;
            index = c.Add(a0);
            Assert.Equal(0, index);
            index = c.Add(a1);
            Assert.Equal(1, index);
            index = c.Add(a2);
            Assert.Equal(2, index);

            c.Remove(a1);

            AssertEquals(c, new AsnEncodedData[] { a0, a2 });
        }

        [Fact]
        public static void AddNegative()
        {
            AsnEncodedDataCollection c = new AsnEncodedDataCollection();
            Assert.Throws<ArgumentNullException>(() => c.Add(null));
        }

        [Fact]
        public static void RemoveNegative()
        {
            AsnEncodedDataCollection c = new AsnEncodedDataCollection();
            Assert.Throws<ArgumentNullException>(() => c.Remove(null));
        }

        [Fact]
        public static void RemoveNonExistent()
        {
            AsnEncodedDataCollection c = new AsnEncodedDataCollection();
            AsnEncodedData a0 = new AsnEncodedData("1.0", Array.Empty<byte>());
            c.Remove(a0);  // You can "remove" items that aren't in the collection - this is defined as a NOP.
        }

        [Fact]
        public static void IndexOutOfBounds()
        {
            AsnEncodedData a0 = new AsnEncodedData("1.0", Array.Empty<byte>());
            AsnEncodedData a1 = new AsnEncodedData("1.1", Array.Empty<byte>());
            AsnEncodedData a2 = new AsnEncodedData("1.2", Array.Empty<byte>());

            AsnEncodedDataCollection c = new AsnEncodedDataCollection();
            c.Add(a0);
            c.Add(a1);
            c.Add(a2);

            Assert.Throws<ArgumentOutOfRangeException>(() => c[-1]);
            Assert.Throws<ArgumentOutOfRangeException>(() => c[3]);
        }

        [Fact]
        public static void CopyExceptions()
        {
            AsnEncodedData a0 = new AsnEncodedData("1.0", Array.Empty<byte>());
            AsnEncodedData a1 = new AsnEncodedData("1.1", Array.Empty<byte>());
            AsnEncodedData a2 = new AsnEncodedData("1.2", Array.Empty<byte>());

            AsnEncodedDataCollection c = new AsnEncodedDataCollection();
            c.Add(a0);
            c.Add(a1);
            c.Add(a2);

            Assert.Throws<ArgumentNullException>(() => c.CopyTo(null, 0));
            AsnEncodedData[] a = new AsnEncodedData[3];
            Assert.Throws<ArgumentOutOfRangeException>(() => c.CopyTo(a, -1));
            Assert.Throws<ArgumentOutOfRangeException>(() => c.CopyTo(a, 3));
            AssertExtensions.Throws<ArgumentException>("destinationArray", null, () => c.CopyTo(a, 1));

            if (PlatformDetection.IsNonZeroLowerBoundArraySupported)
            {
                // Array has non-zero lower bound
                ICollection ic = c;
                Array array = Array.CreateInstance(typeof(object), new int[] { 10 }, new int[] { 10 });
                Assert.Throws<IndexOutOfRangeException>(() => ic.CopyTo(array, 0));
            }
        }


        private static void AssertEquals(AsnEncodedDataCollection c, IList<AsnEncodedData> expected)
        {
            Assert.Equal(expected.Count, c.Count);

            for (int i = 0; i < c.Count; i++)
            {
                Assert.Equal(expected[i], c[i]);
            }

            int index = 0;
            foreach (AsnEncodedData a in c)
            {
                Assert.Equal(expected[index++], a);
            }
            Assert.Equal(c.Count, index);

            ValidateEnumerator(c.GetEnumerator(), expected);
            ValidateEnumerator(((ICollection)c).GetEnumerator(), expected);

            AsnEncodedData[] dumped = new AsnEncodedData[c.Count + 3];
            c.CopyTo(dumped, 2);
            Assert.Equal(null, dumped[0]);
            Assert.Equal(null, dumped[1]);
            Assert.Equal(null, dumped[dumped.Length - 1]);
            Assert.Equal<AsnEncodedData>(expected, dumped.Skip(2).Take(c.Count));
        }

        private static void ValidateEnumerator(IEnumerator enumerator, IList<AsnEncodedData> expected)
        {
            foreach (AsnEncodedData e in expected)
            {
                Assert.True(enumerator.MoveNext());
                AsnEncodedData actual = (AsnEncodedData)(enumerator.Current);
                Assert.Equal(e, actual);
            }
            Assert.False(enumerator.MoveNext());
        }
    }
}
