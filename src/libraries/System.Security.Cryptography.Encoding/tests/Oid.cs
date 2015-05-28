// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.IO;
using System.Text;
using Xunit;

namespace System.Security.Cryptography.Encoding.Tests
{
    public class OidTests
    {
        [Fact]
        public static void TestStrConstructor()
        {
            Oid oid;

            Assert.Throws<ArgumentNullException>(() => oid = new Oid((string)null));

            oid = new Oid(SHA1_Oid);
            Assert.Equal(SHA1_Name, oid.FriendlyName);
            Assert.Equal(SHA1_Oid, oid.Value);

            // Though the parameter is supposed to be an OID, the constructor will also accept a friendly name.
            oid = new Oid(SHA1_Name);
            Assert.Equal(SHA1_Name, oid.FriendlyName);
            Assert.Equal(SHA1_Oid, oid.Value);

            // No validation done on OID (other than the null check.) 
            oid = new Oid(Bogus_Name);
            Assert.Equal(null, oid.FriendlyName);
            Assert.Equal(Bogus_Name, oid.Value);

            return;
        }

        [Fact]
        public static void TestStrStrConstructor()
        {
            Oid oid;

            // No validation at all.
            oid = new Oid((string)null, (string)null);
            Assert.Equal(null, oid.FriendlyName);
            Assert.Equal(null, oid.Value);

            // Can omit friendly-name - FriendlyName property demand-computes it.
            oid = new Oid(SHA1_Oid, (string)null);
            Assert.Equal(SHA1_Name, oid.FriendlyName);
            Assert.Equal(SHA1_Oid, oid.Value);

            oid = new Oid(SHA1_Oid, "BOGUS-NAME");
            Assert.Equal("BOGUS-NAME", oid.FriendlyName);
            Assert.Equal(SHA1_Oid, oid.Value);

            // Can omit oid, Value property does no on-demand conversion.
            oid = new Oid((string)null, SHA1_Name);
            Assert.Equal(SHA1_Name, oid.FriendlyName);
            Assert.Equal(null, oid.Value);

            oid = new Oid("BOGUS-OID", SHA1_Name);
            Assert.Equal(SHA1_Name, oid.FriendlyName);
            Assert.Equal("BOGUS-OID", oid.Value);


            return;
        }

        [Fact]
        public static void TestValueProperty()
        {
            Oid oid = new Oid(null, null);

            // Value property is just a field exposed as a property - no extra policy at all.

            oid.Value = "BOGUS";
            Assert.Equal("BOGUS", oid.Value);

            oid.Value = null;
            Assert.Equal(null, oid.Value);

            return;
        }

        [Fact]
        public static void TestFriendlyNameProperty()
        {
            Oid oid;

            oid = new Oid(null, null);

            // Friendly name property can initialize itself from the Value (but only
            // if it was originally null.)

            oid.Value = SHA1_Oid;
            Assert.Equal(SHA1_Name, oid.FriendlyName);

            oid.Value = SHA256_Oid;
            Assert.Equal(SHA1_Name, oid.FriendlyName);

            oid.Value = null;
            Assert.Equal(SHA1_Name, oid.FriendlyName);

            oid.Value = Bogus_Name;
            Assert.Equal(SHA1_Name, oid.FriendlyName);

            // Setting the FriendlyName can also updates the value if there a valid OID for the new name.
            oid.FriendlyName = Bogus_Name;
            Assert.Equal(Bogus_Name, oid.FriendlyName);
            Assert.Equal(Bogus_Name, oid.Value);

            oid.FriendlyName = SHA1_Name;
            Assert.Equal(SHA1_Name, oid.FriendlyName);
            Assert.Equal(SHA1_Oid, oid.Value);

            oid.FriendlyName = SHA256_Name;
            Assert.Equal(SHA256_Name, oid.FriendlyName);
            Assert.Equal(SHA256_Oid, oid.Value);
        }

        [Fact]
        [ActiveIssue(1863, PlatformID.AnyUnix)]
        public static void TestFromFriendlyName()
        {
            Oid oid;

            oid = Oid.FromFriendlyName(SHA1_Name, OidGroup.HashAlgorithm);
            Assert.Equal(SHA1_Name, oid.FriendlyName);
            Assert.Equal(SHA1_Oid, oid.Value);

            oid = Oid.FromFriendlyName(SHA256_Name, OidGroup.HashAlgorithm);
            Assert.Equal(SHA256_Name, oid.FriendlyName);
            Assert.Equal(SHA256_Oid, oid.Value);

            Assert.Throws<ArgumentNullException>(() => Oid.FromFriendlyName(null, OidGroup.HashAlgorithm));
            Assert.Throws<CryptographicException>(() => Oid.FromFriendlyName(Bogus_Name, OidGroup.HashAlgorithm));

            // Oid group is implemented strictly - no fallback to OidGroup.All as with many other parts of Crypto.
            Assert.Throws<CryptographicException>(() => Oid.FromFriendlyName(SHA1_Name, OidGroup.Policy));
        }

        [Fact]
        [ActiveIssue(1863, PlatformID.AnyUnix)]
        public static void TestFromOidValue()
        {
            Oid oid;

            oid = Oid.FromOidValue(SHA1_Oid, OidGroup.HashAlgorithm);
            Assert.Equal(SHA1_Name, oid.FriendlyName);
            Assert.Equal(SHA1_Oid, oid.Value);

            oid = Oid.FromOidValue(SHA256_Oid, OidGroup.HashAlgorithm);
            Assert.Equal(SHA256_Name, oid.FriendlyName);
            Assert.Equal(SHA256_Oid, oid.Value);

            Assert.Throws<ArgumentNullException>(() => Oid.FromOidValue(null, OidGroup.HashAlgorithm));
            Assert.Throws<CryptographicException>(() => Oid.FromOidValue(Bogus_Name, OidGroup.HashAlgorithm));

            // Oid group is implemented strictly - no fallback to OidGroup.All as with many other parts of Crypto.
            Assert.Throws<CryptographicException>(() => Oid.FromOidValue(SHA1_Oid, OidGroup.Policy));
        }

        [Fact]
        [ActiveIssue(1863, PlatformID.AnyUnix)]
        public static void TestKnownValues()
        {
            Oid oid;
            oid = Oid.FromFriendlyName(SHA1_Name, OidGroup.All);
            Assert.Equal(SHA1_Name, oid.FriendlyName);
            Assert.Equal(SHA1_Oid, oid.Value);

            oid = Oid.FromFriendlyName(SHA256_Name, OidGroup.All);
            Assert.Equal(SHA256_Name, oid.FriendlyName);
            Assert.Equal(SHA256_Oid, oid.Value);

            // Note that oid lookup is case-insensitive, and we store the name in the form it was input to the constructor (rather than "normalizing" it 
            // to the official casing.)
            oid = Oid.FromFriendlyName("MD5", OidGroup.All);
            Assert.Equal("MD5", oid.FriendlyName);
            Assert.Equal("1.2.840.113549.2.5", oid.Value);

            oid = Oid.FromFriendlyName("sha384", OidGroup.All);
            Assert.Equal("sha384", oid.FriendlyName);
            Assert.Equal("2.16.840.1.101.3.4.2.2", oid.Value);

            oid = Oid.FromFriendlyName("sha512", OidGroup.All);
            Assert.Equal("sha512", oid.FriendlyName);
            Assert.Equal("2.16.840.1.101.3.4.2.3", oid.Value);

            oid = Oid.FromFriendlyName("3des", OidGroup.All);
            Assert.Equal("3des", oid.FriendlyName);
            Assert.Equal("1.2.840.113549.3.7", oid.Value);
        }

        private const string SHA1_Name = "sha1";
        private const string SHA1_Oid = "1.3.14.3.2.26";

        private const string SHA256_Name = "sha256";
        private const string SHA256_Oid = "2.16.840.1.101.3.4.2.1";

        private const string Bogus_Name = "BOGUS_BOGUS_BOGUS_BOGUS";
    }
}