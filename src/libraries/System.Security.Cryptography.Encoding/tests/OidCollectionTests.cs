// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections;
using System.Linq;
using Xunit;

namespace System.Security.Cryptography.Encoding.Tests
{
    public class OidCollectionTests
    {
        private const string Sha1Name = "sha1";
        private const string Sha1Oid = "1.3.14.3.2.26";

        private const string Sha256Name = "sha256";
        private const string Sha256Oid = "2.16.840.1.101.3.4.2.1";

        [Fact]
        public void TestOidCollection()
        {
            var c = new OidCollection();
            Assert.Equal(0, c.Count);

            var o0 = new Oid(Sha1Oid, Sha1Name);
            Assert.Equal(0, c.Add(o0));

            var o1 = new Oid(Sha256Oid, Sha256Name);
            Assert.Equal(1, c.Add(o1));

            Assert.Equal(2, c.Count);

            Assert.Same(o0, c[0]);
            Assert.Same(o1, c[1]);
            Assert.Throws<ArgumentOutOfRangeException>(() => c[-1]);
            Assert.Throws<ArgumentOutOfRangeException>(() => c[c.Count]);

            var o2 = new Oid(Sha1Oid, Sha1Name);
            Assert.Equal(2, c.Add(o2));

            // If there multiple matches, the one with the lowest index wins.
            Assert.Same(o0, c[Sha1Name]);
            Assert.Same(o0, c[Sha1Oid]);

            Assert.Same(o1, c[Sha256Name]);
            Assert.Same(o1, c[Sha256Oid]);

            var o3 = new Oid(null, null);
            Assert.Equal(3, c.Add(o3));
            Assert.Throws<ArgumentNullException>(() => c[null]);

            Assert.Null(c["BOGUSBOGUS"]);
        }

        [Fact]
        public void CopyTo_Success()
        {
            ValidateCopyTo((collection, array, index) => collection.CopyTo(array, index));
        }

        [Fact]
        public void CopyTo_NonGeneric_Success()
        {
            ValidateCopyTo((collection, array, index) => ((ICollection)collection).CopyTo(array, index));
        }

        private static void ValidateCopyTo(Action<OidCollection, Oid[], int> copyTo)
        {
            var item1 = new Oid(Sha1Oid, Sha1Name);
            var item2 = new Oid(Sha256Oid, Sha256Name);
            var item3 = new Oid(Sha1Oid, Sha1Name);
            var item4 = new Oid(null, null);

            var c = new OidCollection { item1, item2, item3, item4 };

            Oid[] a = Enumerable.Range(0, 10).Select(i => new Oid(null, null)).ToArray();
            Oid[] destination = (Oid[])(a.Clone());

            copyTo(c, destination, 3);

            Assert.Same(a[0], destination[0]);
            Assert.Same(a[1], destination[1]);
            Assert.Same(a[2], destination[2]);
            Assert.Same(item1, destination[3]);
            Assert.Same(item2, destination[4]);
            Assert.Same(item3, destination[5]);
            Assert.Same(item4, destination[6]);
            Assert.Same(a[7], destination[7]);
            Assert.Same(a[8], destination[8]);
            Assert.Same(a[9], destination[9]);
        }

        [Fact]
        public void CopyTo_Throws()
        {
            ValidateCopyToThrows((collection, array, index) => collection.CopyTo(array, index), paramName: "destinationArray");
        }

        [Fact]
        public void CopyTo_NonGeneric_Throws()
        {
            ValidateCopyToThrows((collection, array, index) => ((ICollection)collection).CopyTo(array, index), c =>
            {
                ICollection ic = c;
                AssertExtensions.Throws<ArgumentException>(null, () => ic.CopyTo(new Oid[4, 3], 0));
                Assert.Throws<InvalidCastException>(() => ic.CopyTo(new string[10], 0));
            }, paramName: null);
        }

        private static void ValidateCopyToThrows(
            Action<OidCollection, Oid[], int> copyTo,
            Action<OidCollection> additionalValidation = null,
            string paramName = null)
        {
            var item1 = new Oid(Sha1Oid, Sha1Name);
            var item2 = new Oid(Sha256Oid, Sha256Name);
            var item3 = new Oid(Sha1Oid, Sha1Name);
            var item4 = new Oid(null, null);

            var c = new OidCollection { item1, item2, item3, item4 };

            Assert.Throws<ArgumentNullException>(() => copyTo(c, null, 0));
            Assert.Throws<ArgumentNullException>(() => copyTo(c, null, -1));
            Assert.Throws<ArgumentOutOfRangeException>(() => copyTo(c, new Oid[10], -1));
            AssertExtensions.Throws<ArgumentException>(paramName, null, () => copyTo(c, new Oid[10], 7));
            Assert.Throws<ArgumentOutOfRangeException>(() => copyTo(c, new Oid[10], 1000));

            additionalValidation?.Invoke(c);
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsNonZeroLowerBoundArraySupported))]
        public void CopyTo_NonZeroLowerBound_ThrowsIndexOutOfRangeException()
        {
            Oid item = new Oid(Sha1Oid, Sha1Name);
            ICollection ic = new OidCollection { item };
            Array array = Array.CreateInstance(typeof(object), new int[] { 10 }, new int[] { 10 });
            Assert.Throws<IndexOutOfRangeException>(() => ic.CopyTo(array, 0));
        }

        [Fact]
        public void GetEnumerator_Success()
        {
            ValidateEnumerator(c => c.GetEnumerator(), e => e.Current);
        }

        [Fact]
        public void GetEnumerator_NonGeneric_Success()
        {
            ValidateEnumerator(c => ((IEnumerable)c).GetEnumerator(), e => e.Current);
        }

        private static void ValidateEnumerator<TEnumerator, TCurrent>(
            Func<OidCollection, TEnumerator> getEnumerator,
            Func<TEnumerator, TCurrent> getCurrent) where TEnumerator : IEnumerator
        {
            var item1 = new Oid(Sha1Oid, Sha1Name);
            var item2 = new Oid(Sha256Oid, Sha256Name);
            var item3 = new Oid(Sha1Oid, Sha1Name);

            var c = new OidCollection { item1, item2, item3 };

            TEnumerator e = getEnumerator(c);

            for (int i = 0; i < 2; i++)
            {
                Assert.True(e.MoveNext());
                Assert.Same(item1, getCurrent(e));

                Assert.True(e.MoveNext());
                Assert.Same(item2, getCurrent(e));

                Assert.True(e.MoveNext());
                Assert.Same(item3, getCurrent(e));

                Assert.False(e.MoveNext());
                Assert.False(e.MoveNext());
                Assert.False(e.MoveNext());

                e.Reset();
            }
        }

        [Fact]
        public void IsSynchronized_Success()
        {
            ICollection c = new OidCollection();
            Assert.False(c.IsSynchronized);
        }

        [Fact]
        public void SyncRoot_Success()
        {
            ICollection c = new OidCollection();
            Assert.Same(c, c.SyncRoot);
        }
    }
}
