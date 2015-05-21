// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.IO;
using System.Text;
using System.Collections;
using Xunit;

namespace System.Security.Cryptography.Encoding.Tests
{
    public class OidCollectionTests
    {
        [Fact]
        public static void TestOidCollection()
        {
            int i;
            OidCollection c = new OidCollection();
            Assert.Equal(0, c.Count);

            Oid o0 = new Oid(SHA1_Oid, SHA1_Name);
            i = c.Add(o0);
            Assert.Equal(0, i);

            Oid o1 = new Oid(SHA256_Oid, SHA256_Name);
            i = c.Add(o1);
            Assert.Equal(1, i);

            Assert.Equal(2, c.Count);

            Assert.True(Object.ReferenceEquals(o0, c[0]));
            Assert.True(Object.ReferenceEquals(o1, c[1]));
            Assert.Throws<ArgumentOutOfRangeException>(() => GC.KeepAlive(c[-1]));
            Assert.Throws<ArgumentOutOfRangeException>(() => GC.KeepAlive(c[c.Count]));

            Oid o2 = new Oid(SHA1_Oid, SHA1_Name);
            i = c.Add(o2);
            Assert.Equal(2, i);

            // If there multiple matches, the one with the lowest index wins.
            Assert.True(Object.ReferenceEquals(o0, c[SHA1_Name]));
            Assert.True(Object.ReferenceEquals(o0, c[SHA1_Oid]));

            Assert.True(Object.ReferenceEquals(o1, c[SHA256_Name]));
            Assert.True(Object.ReferenceEquals(o1, c[SHA256_Oid]));

            Oid o3 = new Oid(null, null);
            i = c.Add(o3);
            Assert.Equal(3, i);
            Assert.Throws<ArgumentNullException>(() => GC.KeepAlive(c[null]));

            Object o = c["BOGUSBOGUS"];
            Assert.Null(c["BOGUSBOGUS"]);

            Oid[] a = new Oid[10];
            for (int j = 0; j < a.Length; j++)
            {
                a[j] = new Oid(null, null);
            }
            Oid[] a2 = (Oid[])(a.Clone());

            c.CopyTo(a2, 3);
            Assert.Equal(a[0], a2[0]);
            Assert.Equal(a[1], a2[1]);
            Assert.Equal(a[2], a2[2]);
            Assert.Equal(o0, a2[3]);
            Assert.Equal(o1, a2[4]);
            Assert.Equal(o2, a2[5]);
            Assert.Equal(o3, a2[6]);
            Assert.Equal(a[7], a2[7]);
            Assert.Equal(a[8], a2[8]);
            Assert.Equal(a[9], a2[9]);

            Assert.Throws<ArgumentNullException>(() => c.CopyTo(null, 0));
            Assert.Throws<ArgumentNullException>(() => c.CopyTo(null, -1));
            Assert.Throws<ArgumentOutOfRangeException>(() => c.CopyTo(a, -1));
            Assert.Throws<ArgumentException>(() => c.CopyTo(a, 7));
            Assert.Throws<ArgumentOutOfRangeException>(() => c.CopyTo(a, 1000));

            ICollection ic = c;
            Assert.Throws<ArgumentException>(() => ic.CopyTo(new Oid[4, 3], 0));
            Assert.Throws<InvalidCastException>(() => ic.CopyTo(new string[100], 0));

            return;
        }

        private const string SHA1_Name = "sha1";
        private const string SHA1_Oid = "1.3.14.3.2.26";

        private const string SHA256_Name = "sha256";
        private const string SHA256_Oid = "2.16.840.1.101.3.4.2.1";
    }
}