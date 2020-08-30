// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using Xunit;

namespace System.Security.Cryptography.Encoding.Tests
{
    public static class OidTests
    {
        [Fact]
        public static void EmptyOid()
        {
            Oid oid = new Oid("");
            Assert.Equal("", oid.Value);
            Assert.Null(oid.FriendlyName);

            oid = new Oid();
            Assert.Null(oid.Value);
            Assert.Null(oid.FriendlyName);
        }

        [Theory]
        [ActiveIssue("https://github.com/mono/mono/issues/16686", TestRuntimes.Mono)]
        [MemberData(nameof(ValidOidFriendlyNamePairs))]
        public static void LookupOidByValue_Ctor(string oidValue, string friendlyName)
        {
            Oid oid = new Oid(oidValue);

            Assert.Equal(oidValue, oid.Value);
            Assert.Equal(friendlyName, oid.FriendlyName);
        }

        [Theory]
        [ActiveIssue("https://github.com/mono/mono/issues/16686", TestRuntimes.Mono)]
        [MemberData(nameof(ValidOidFriendlyNamePairs))]
        public static void LookupOidByFriendlyName_Ctor(string oidValue, string friendlyName)
        {
            Oid oid = new Oid(friendlyName);

            Assert.Equal(oidValue, oid.Value);
            Assert.Equal(friendlyName, oid.FriendlyName);
        }

        [Fact]
        public static void LookupNullOid()
        {
            Assert.Throws<ArgumentNullException>(() => new Oid((string)null));
        }

        [Fact]
        public static void LookupUnknownOid()
        {
            Oid oid = new Oid(Bogus_Name);

            Assert.Equal(Bogus_Name, oid.Value);
            Assert.Null(oid.FriendlyName);
        }

        [Fact]
        public static void Oid_StringString_BothNull()
        {
            // No validation at all.
            Oid oid = new Oid(null, null);

            Assert.Null(oid.Value);
            Assert.Null(oid.FriendlyName);
        }

        [Theory]
        [ActiveIssue("https://github.com/mono/mono/issues/16686", TestRuntimes.Mono)]
        [MemberData(nameof(ValidOidFriendlyNamePairs))]
        public static void Oid_StringString_NullFriendlyName(string oidValue, string expectedFriendlyName)
        {
            // Can omit friendly-name - FriendlyName property demand-computes it.
            Oid oid = new Oid(oidValue, null);

            Assert.Equal(oidValue, oid.Value);
            Assert.Equal(expectedFriendlyName, oid.FriendlyName);
        }

        [Fact]
        public static void Oid_StringString_ExplicitNullFriendlyName()
        {
            Oid oid = new Oid(SHA1_Oid, null) { FriendlyName = null };
            Assert.Throws<PlatformNotSupportedException>(() => oid.FriendlyName = SHA1_Name);
        }

        [Theory]
        [InlineData(SHA1_Name)]
        [InlineData(SHA256_Name)]
        [InlineData(Bogus_Name)]
        public static void Oid_StringString_NullValue(string friendlyName)
        {
            // Can omit oid, Value property does no on-demand conversion.
            Oid oid = new Oid(null, friendlyName);

            Assert.Null(oid.Value);
            Assert.Equal(friendlyName, oid.FriendlyName);
        }

        [Theory]
        [InlineData(SHA1_Oid, SHA256_Name)]
        [InlineData(SHA256_Oid, SHA1_Name)]
        [InlineData(SHA256_Name, SHA1_Name)]
        [InlineData(SHA256_Name, Bogus_Name)]
        [InlineData(Bogus_Name, SHA256_Oid)]
        public static void Oid_StringString_BothSpecified(string oidValue, string friendlyName)
        {
            // The values are taken as true, not verified at all.
            // The data for this test series should be mismatched OID-FriendlyName pairs, and
            // sometimes the OID isn't a legal OID.
            Oid oid = new Oid(oidValue, friendlyName);

            Assert.Equal(oidValue, oid.Value);
            Assert.Equal(friendlyName, oid.FriendlyName);
        }

        [Fact]
        public static void TestValueProperty()
        {
            Oid oid = new Oid(null, null);

            // Should allow setting null
            oid.Value = null;

            // Value property permits initializing once if null, or to its current value

            oid.Value = "BOGUS";
            Assert.Equal("BOGUS", oid.Value);

            Assert.Throws<PlatformNotSupportedException>(() => oid.Value = null);
            Assert.Throws<PlatformNotSupportedException>(() => oid.Value = "SUGOB");
            Assert.Throws<PlatformNotSupportedException>(() => oid.Value = "bogus");

            oid.Value = "BOGUS";
            Assert.Equal("BOGUS", oid.Value);
        }

        [Fact]
        public static void TestFriendlyNameProperty()
        {
            // Setting the FriendlyName can be initialized once, and may update
            // the value as well. If the value updates, it must match the current
            // value, if any.

            Oid oid = new Oid
            {
                FriendlyName = SHA1_Name
            };
            Assert.Equal(SHA1_Name, oid.FriendlyName);
            Assert.Equal(SHA1_Oid, oid.Value);

            Assert.Throws<PlatformNotSupportedException>(() => oid.FriendlyName = "BOGUS");

            oid.FriendlyName = SHA1_Name;
            Assert.Equal(SHA1_Name, oid.FriendlyName);
        }

        [Fact]
        public static void TestFriendlyNameWithExistingValue()
        {
            Oid oid = new Oid(SHA1_Oid, null);
            Assert.Equal(SHA1_Oid, oid.Value);
            // Do not assert the friendly name here since the getter will populate it

            // FriendlyName will attempt to set the Value if the FriendlyName is known.
            // If the new Value does not match, do not permit mutating the value.
            Assert.Throws<PlatformNotSupportedException>(() => oid.FriendlyName = SHA256_Name);
            Assert.Equal(SHA1_Oid, oid.Value);
            Assert.Equal(SHA1_Name, oid.FriendlyName);
        }

        [Fact]
        public static void TestFriendlyNameGetInitializes()
        {
            Oid oid = new Oid(SHA1_Oid, null);
            Assert.Equal(SHA1_Oid, oid.Value);

            // Getter should initialize and lock the friendly name
            Assert.Equal(SHA1_Name, oid.FriendlyName);

            // Set to a friendly name that does not map to an OID
            Assert.Throws<PlatformNotSupportedException>(() => oid.FriendlyName = Bogus_Name);
        }

        [Fact]
        public static void TestFriendlyNameWithMismatchedValue()
        {
            Oid oid = new Oid(SHA1_Oid, SHA256_Name);
            Assert.Equal(SHA1_Oid, oid.Value);
            
            oid.FriendlyName = SHA256_Name;
            
            Assert.Equal(SHA256_Name, oid.FriendlyName);
            Assert.Equal(SHA1_Oid, oid.Value);
        }

        [Theory]
        [MemberData(nameof(ValidOidFriendlyNameHashAlgorithmPairs))]
        public static void LookupOidByValue_Method_HashAlgorithm(string oidValue, string friendlyName)
        {
            Oid oid = Oid.FromOidValue(oidValue, OidGroup.HashAlgorithm);

            Assert.Equal(oidValue, oid.Value);
            Assert.Equal(friendlyName, oid.FriendlyName);
        }

        [Theory]
        [MemberData(nameof(ValidOidFriendlyNameEncryptionAlgorithmPairs))]
        public static void LookupOidByValue_Method_EncryptionAlgorithm(string oidValue, string friendlyName)
        {
            Oid oid = Oid.FromOidValue(oidValue, OidGroup.EncryptionAlgorithm);

            Assert.Equal(oidValue, oid.Value);
            Assert.Equal(friendlyName, oid.FriendlyName);
        }

        [Theory]
        [MemberData(nameof(ValidOidFriendlyNameHashAlgorithmPairs))]
        [PlatformSpecific(TestPlatforms.Windows)]  // Uses P/Invokes to get the  Oid lookup table
        public static void LookupOidByValue_Method_WrongGroup(string oidValue, string friendlyName)
        {
            // Oid group is implemented strictly - no fallback to OidGroup.All as with many other parts of Crypto.
            _ = friendlyName;
            Assert.Throws<CryptographicException>(() => Oid.FromOidValue(oidValue, OidGroup.EncryptionAlgorithm));
        }

        [Fact]
        public static void LookupOidByValue_Method_NullInput()
        {
            Assert.Throws<ArgumentNullException>(() => Oid.FromOidValue(null, OidGroup.HashAlgorithm));
        }

        [Theory]
        [InlineData(SHA1_Name)] // Friendly names are not coerced into OID values from the method.
        [InlineData(Bogus_Name)]
        public static void LookupOidByValue_Method_BadInput(string badInput)
        {
            Assert.Throws<CryptographicException>(() => Oid.FromOidValue(badInput, OidGroup.HashAlgorithm));
        }

        [Theory]
        [MemberData(nameof(ValidOidFriendlyNameHashAlgorithmPairs))]
        public static void LookupOidByFriendlyName_Method_HashAlgorithm(string oidValue, string friendlyName)
        {
            Oid oid = Oid.FromFriendlyName(friendlyName, OidGroup.HashAlgorithm);

            Assert.Equal(oidValue, oid.Value);
            Assert.Equal(friendlyName, oid.FriendlyName);
        }

        [Theory]
        [MemberData(nameof(ValidOidFriendlyNameEncryptionAlgorithmPairs))]
        public static void LookupOidByFriendlyName_Method_EncryptionAlgorithm(string oidValue, string friendlyName)
        {
            Oid oid = Oid.FromFriendlyName(friendlyName, OidGroup.EncryptionAlgorithm);

            Assert.Equal(oidValue, oid.Value);
            Assert.Equal(friendlyName, oid.FriendlyName);
        }

        [Theory]
        [ActiveIssue("https://github.com/mono/mono/issues/16686", TestRuntimes.Mono)]
        [MemberData(nameof(ValidOidFriendlyNamePairs))]
        public static void LookupOidByFriendlyName_Method_InverseCase(string oidValue, string friendlyName)
        {
            // Note that oid lookup is case-insensitive, and we store the name in the form it was
            // input to the constructor (rather than "normalizing" it to the official casing.)
            string inverseCasedName = InvertCase(friendlyName);
            Oid oid = Oid.FromFriendlyName(inverseCasedName, OidGroup.All);

            Assert.Equal(oidValue, oid.Value);
            Assert.Equal(inverseCasedName, oid.FriendlyName);
        }

        [Theory]
        [MemberData(nameof(ValidOidFriendlyNameHashAlgorithmPairs))]
        [PlatformSpecific(TestPlatforms.Windows)]  // Uses P/Invokes to get the  Oid lookup table
        public static void LookupOidByFriendlyName_Method_WrongGroup(string oidValue, string friendlyName)
        {
            // Oid group is implemented strictly - no fallback to OidGroup.All as with many other parts of Crypto.
            _ = oidValue;
            Assert.Throws<CryptographicException>(() => Oid.FromFriendlyName(friendlyName, OidGroup.EncryptionAlgorithm));
        }

        [Fact]
        public static void LookupOidByFriendlyName_Method_NullInput()
        {
            Assert.Throws<ArgumentNullException>(() => Oid.FromFriendlyName(null, OidGroup.HashAlgorithm));
        }

        [Theory]
        [InlineData(SHA1_Oid)] // OIDs are not coerced into friendly names values from the method.
        [InlineData(Bogus_Name)]
        public static void LookupOidByFriendlyName_Method_BadInput(string badInput)
        {
            Assert.Throws<CryptographicException>(() => Oid.FromFriendlyName(badInput, OidGroup.HashAlgorithm));
        }

        [Fact]
        [PlatformSpecific(TestPlatforms.AnyUnix)]  // Uses P/Invokes to search Oid in the lookup table
        public static void LookupOidByValue_Method_UnixOnly()
        {
            // This needs to be an OID not in the static lookup table.  The purpose is to verify the
            // NativeOidToFriendlyName fallback for Unix.  For Windows this is accomplished by
            // using FromOidValue with an OidGroup other than OidGroup.All.

            Oid oid;

            try
            {
                oid = Oid.FromOidValue(ObsoleteSmime3desWrap_Oid, OidGroup.All);
            }
            catch (CryptographicException)
            {
                bool isMac = OperatingSystem.IsMacOS();

                Assert.True(isMac, "Exception is only raised on macOS");

                if (isMac)
                {
                    return;
                }
                else
                {
                    throw;
                }
            }

            Assert.Equal(ObsoleteSmime3desWrap_Oid, oid.Value);
            Assert.Equal(ObsoleteSmime3desWrap_Name, oid.FriendlyName);
        }

        [Fact]
        [PlatformSpecific(TestPlatforms.AnyUnix)]  // Uses P/Invokes to search Oid in the lookup table
        public static void LookupOidByFriendlyName_Method_UnixOnly()
        {
            // This needs to be a name not in the static lookup table.  The purpose is to verify the
            // NativeFriendlyNameToOid fallback for Unix.  For Windows this is accomplished by
            // using FromOidValue with an OidGroup other than OidGroup.All.
            Oid oid;

            try
            {
                oid = Oid.FromFriendlyName(ObsoleteSmime3desWrap_Name, OidGroup.All);
            }
            catch (CryptographicException)
            {
                bool isMac = OperatingSystem.IsMacOS();

                Assert.True(isMac, "Exception is only raised on macOS");

                if (isMac)
                {
                    return;
                }
                else
                {
                    throw;
                }
            }

            Assert.Equal(ObsoleteSmime3desWrap_Oid, oid.Value);
            Assert.Equal(ObsoleteSmime3desWrap_Name, oid.FriendlyName);
        }

        [Theory]
        [InlineData("nistP256", "1.2.840.10045.3.1.7")]
        [InlineData("secP256r1", "1.2.840.10045.3.1.7")]
        [InlineData("x962P256v1", "1.2.840.10045.3.1.7")]
        [InlineData("nistP384", "1.3.132.0.34")]
        [InlineData("secP384r1", "1.3.132.0.34")]
        [InlineData("nistP521", "1.3.132.0.35")]
        [InlineData("secP521r1", "1.3.132.0.35")]
        public static void LookupOidByFriendlyName_AdditionalNames(string friendlyName, string expectedOid)
        {
            Oid oid = Oid.FromFriendlyName(friendlyName, OidGroup.All);
            Assert.Equal(friendlyName, oid.FriendlyName);
            Assert.Equal(expectedOid, oid.Value);
        }

        public static IEnumerable<object[]> ValidOidFriendlyNamePairs
        {
            get
            {
                List<object[]> data = new List<object[]>(ValidOidFriendlyNameHashAlgorithmPairs);
                data.AddRange(ValidOidFriendlyNameEncryptionAlgorithmPairs);

                return data;
            }
        }

        public static IEnumerable<object[]> ValidOidFriendlyNameHashAlgorithmPairs
        {
            get
            {
                return new[]
                {
                    new[] { SHA1_Oid, SHA1_Name },
                    new[] { SHA256_Oid, SHA256_Name },
                    new[] { "1.2.840.113549.2.5", "md5" },
                    new[] { "2.16.840.1.101.3.4.2.2", "sha384" },
                    new[] { "2.16.840.1.101.3.4.2.3", "sha512" },
                };
            }
        }

        public static IEnumerable<object[]> ValidOidFriendlyNameEncryptionAlgorithmPairs
        {
            get
            {
                return new[]
                {
                    new[] { "1.2.840.113549.3.7", "3des" },
                };
            }
        }

        private static string InvertCase(string existing)
        {
            char[] chars = existing.ToCharArray();

            for (int i = 0; i < chars.Length; i++)
            {
                if (char.IsUpper(chars[i]))
                {
                    chars[i] = char.ToLowerInvariant(chars[i]);
                }
                else if (char.IsLower(chars[i]))
                {
                    chars[i] = char.ToUpperInvariant(chars[i]);
                }
            }

            return new string(chars);
        }

        private const string SHA1_Name = "sha1";
        private const string SHA1_Oid = "1.3.14.3.2.26";

        private const string SHA256_Name = "sha256";
        private const string SHA256_Oid = "2.16.840.1.101.3.4.2.1";

        private const string Bogus_Name = "BOGUS_BOGUS_BOGUS_BOGUS";

        private const string ObsoleteSmime3desWrap_Oid = "1.2.840.113549.1.9.16.3.3";
        private const string ObsoleteSmime3desWrap_Name = "id-smime-alg-3DESwrap";
    }
}
