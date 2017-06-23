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
    public static class CmsRecipientCollectionTests
    {
        [Fact]
        public static void Nullary()
        {
            CmsRecipientCollection c = new CmsRecipientCollection();
            AssertEquals(c, Array.Empty<CmsRecipient>());
        }

        [Fact]
        public static void Oneary()
        {
            CmsRecipient a0 = s_cr0;
            CmsRecipientCollection c = new CmsRecipientCollection(a0);
            AssertEquals(c, new CmsRecipient[] { a0 });
        }

        [Fact]
        public static void Twoary()
        {
            CmsRecipient a0 = s_cr0;
            CmsRecipientCollection c = new CmsRecipientCollection(SubjectIdentifierType.IssuerAndSerialNumber, new X509Certificate2Collection(a0.Certificate));
            Assert.Equal(1, c.Count);
            CmsRecipient actual = c[0];
            Assert.Equal(a0.RecipientIdentifierType, actual.RecipientIdentifierType);
            Assert.Equal(a0.Certificate, actual.Certificate);
        }

        [Fact]
        public static void Twoary_Ski()
        {
            CmsRecipient a0 = s_cr0;
            CmsRecipientCollection c = new CmsRecipientCollection(SubjectIdentifierType.SubjectKeyIdentifier, new X509Certificate2Collection(a0.Certificate));
            Assert.Equal(1, c.Count);
            CmsRecipient actual = c[0];
            Assert.Equal(SubjectIdentifierType.SubjectKeyIdentifier, actual.RecipientIdentifierType);
            Assert.Equal(a0.Certificate, actual.Certificate);
        }

        [Fact]
        public static void Twoary_Negative()
        {
            object ignore;
            Assert.Throws<NullReferenceException>(() => ignore = new CmsRecipientCollection(SubjectIdentifierType.IssuerAndSerialNumber, null));
        }

        [Fact]
        public static void Add()
        {
            CmsRecipient a0 = s_cr0;
            CmsRecipient a1 = s_cr1;
            CmsRecipient a2 = s_cr2;

            CmsRecipientCollection c = new CmsRecipientCollection();
            int index;
            index = c.Add(a0);
            Assert.Equal(0, index);
            index = c.Add(a1);
            Assert.Equal(1, index);
            index = c.Add(a2);
            Assert.Equal(2, index);

            AssertEquals(c, new CmsRecipient[] { a0, a1, a2 });
        }

        [Fact]
        public static void Remove()
        {
            CmsRecipient a0 = s_cr0;
            CmsRecipient a1 = s_cr1;
            CmsRecipient a2 = s_cr2;

            CmsRecipientCollection c = new CmsRecipientCollection();
            int index;
            index = c.Add(a0);
            Assert.Equal(0, index);
            index = c.Add(a1);
            Assert.Equal(1, index);
            index = c.Add(a2);
            Assert.Equal(2, index);

            c.Remove(a1);

            AssertEquals(c, new CmsRecipient[] { a0, a2 });
        }

        [Fact]
        public static void AddNegative()
        {
            CmsRecipientCollection c = new CmsRecipientCollection();
            Assert.Throws<ArgumentNullException>(() => c.Add(null));
        }

        [Fact]
        public static void RemoveNegative()
        {
            CmsRecipientCollection c = new CmsRecipientCollection();
            Assert.Throws<ArgumentNullException>(() => c.Remove(null));
        }

        [Fact]
        public static void RemoveNonExistent()
        {
            CmsRecipientCollection c = new CmsRecipientCollection();
            CmsRecipient a0 = s_cr0;
            c.Remove(a0);  // You can "remove" items that aren't in the collection - this is defined as a NOP.
        }

        [Fact]
        public static void IndexOutOfBounds()
        {
            CmsRecipient a0 = s_cr0;
            CmsRecipient a1 = s_cr1;
            CmsRecipient a2 = s_cr2;

            CmsRecipientCollection c = new CmsRecipientCollection();
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
            CmsRecipient a0 = s_cr0;
            CmsRecipient a1 = s_cr1;
            CmsRecipient a2 = s_cr2;

            CmsRecipientCollection c = new CmsRecipientCollection();
            c.Add(a0);
            c.Add(a1);
            c.Add(a2);

            CmsRecipient[] a = new CmsRecipient[3];
            Assert.Throws<ArgumentNullException>(() => c.CopyTo(null, 0));
            Assert.Throws<ArgumentOutOfRangeException>(() => c.CopyTo(a, -1));
            Assert.Throws<ArgumentOutOfRangeException>(() => c.CopyTo(a, 3));
            AssertExtensions.Throws<ArgumentException>(null, () => c.CopyTo(a, 1));

            ICollection ic = c;
            Assert.Throws<ArgumentNullException>(() => ic.CopyTo(null, 0));
            Assert.Throws<ArgumentOutOfRangeException>(() => ic.CopyTo(a, -1));
            Assert.Throws<ArgumentOutOfRangeException>(() => ic.CopyTo(a, 3));
            AssertExtensions.Throws<ArgumentException>(null, () => ic.CopyTo(a, 1));
            AssertExtensions.Throws<ArgumentException>(null, () => ic.CopyTo(new CmsRecipient[2, 2], 1));
            Assert.Throws<InvalidCastException>(() => ic.CopyTo(new int[10], 1));

            if (PlatformDetection.IsNonZeroLowerBoundArraySupported)
            {
                // Array has non-zero lower bound
                Array array = Array.CreateInstance(typeof(object), new int[] { 10 }, new int[] { 10 });
                Assert.Throws<IndexOutOfRangeException>(() => ic.CopyTo(array, 0));
            }
        }


        private static void AssertEquals(CmsRecipientCollection c, IList<CmsRecipient> expected)
        {
            Assert.Equal(expected.Count, c.Count);

            for (int i = 0; i < c.Count; i++)
            {
                Assert.Equal(expected[i], c[i]);
            }

            int index = 0;
            foreach (CmsRecipient a in c)
            {
                Assert.Equal(expected[index++], a);
            }
            Assert.Equal(c.Count, index);

            ValidateEnumerator(c.GetEnumerator(), expected);
            ValidateEnumerator(((ICollection)c).GetEnumerator(), expected);

            {
                CmsRecipient[] dumped = new CmsRecipient[c.Count + 3];
                c.CopyTo(dumped, 2);
                Assert.Equal(null, dumped[0]);
                Assert.Equal(null, dumped[1]);
                Assert.Equal(null, dumped[dumped.Length - 1]);
                Assert.Equal<CmsRecipient>(expected, dumped.Skip(2).Take(c.Count));
            }

            {
                CmsRecipient[] dumped = new CmsRecipient[c.Count + 3];
                ((ICollection)c).CopyTo(dumped, 2);
                Assert.Equal(null, dumped[0]);
                Assert.Equal(null, dumped[1]);
                Assert.Equal(null, dumped[dumped.Length - 1]);
                Assert.Equal<CmsRecipient>(expected, dumped.Skip(2).Take(c.Count));
            }
        }

        private static void ValidateEnumerator(IEnumerator enumerator, IList<CmsRecipient> expected)
        {
            foreach (CmsRecipient e in expected)
            {
                Assert.True(enumerator.MoveNext());
                CmsRecipient actual = (CmsRecipient)(enumerator.Current);
                Assert.Equal(e, actual);
            }
            Assert.False(enumerator.MoveNext());
        }


        private static readonly CmsRecipient s_cr0 = new CmsRecipient(Certificates.RSAKeyTransfer1.GetCertificate());
        private static readonly CmsRecipient s_cr1 = new CmsRecipient(Certificates.RSAKeyTransfer2.GetCertificate());
        private static readonly CmsRecipient s_cr2 = new CmsRecipient(Certificates.RSAKeyTransfer3.GetCertificate());
    }
}


