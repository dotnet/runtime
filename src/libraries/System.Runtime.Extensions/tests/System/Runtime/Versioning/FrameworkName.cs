// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace System.Runtime.Versioning.Tests
{
    public static class FrameworkNameTests
    {
        private const string TestIdentifier = "TestFramework";
        private const string TestProfile = "TestProfile";

        private static readonly Version s_testVersion = new Version(1, 2, 3, 4);
        private static readonly string s_testNameString = $"{TestIdentifier},Version=v{s_testVersion},Profile={TestProfile}";
        private static readonly string s_testNameNoProfileString = $"{TestIdentifier},Version=v{s_testVersion}";
        private static readonly FrameworkName s_testName = new FrameworkName(s_testNameString);
        private static readonly FrameworkName s_testNameNoProfile = new FrameworkName(s_testNameNoProfileString);

        [Fact]
        public static void ConstructFromString()
        {
            VerifyConstructor(s_testName, s_testNameString, TestIdentifier, s_testVersion, TestProfile);

            string emptyProfileFrameworkName = $"{TestIdentifier},Version=v{s_testVersion}";
            VerifyConstructor(new FrameworkName(emptyProfileFrameworkName), emptyProfileFrameworkName, TestIdentifier, s_testVersion, string.Empty);
            VerifyConstructor(new FrameworkName(emptyProfileFrameworkName + ",Profile="), emptyProfileFrameworkName, TestIdentifier, s_testVersion, string.Empty);
        }

        [Fact]
        public static void ConstructFromStringWithWhitespace()
        {
            var nameString = string.Format("   \r{0}\r\n, Version = {1}\t,  Profile =  {2} \r\n", TestIdentifier, s_testVersion, TestProfile);
            VerifyConstructor(new FrameworkName(nameString), s_testNameString, TestIdentifier, s_testVersion, TestProfile);
        }

        [Fact]
        public static void ConstructFromStringOmitProfile()
        {
            VerifyConstructor(s_testNameNoProfile, s_testNameNoProfileString, TestIdentifier, s_testVersion, string.Empty);
        }

        [Fact]
        public static void ConstructFromInvalidString()
        {
            Assert.Throws<ArgumentNullException>(() => new FrameworkName(null));
            AssertExtensions.Throws<ArgumentException>("frameworkName", () => new FrameworkName(string.Empty));
            AssertExtensions.Throws<ArgumentException>("frameworkName", () => new FrameworkName(" ,A"));
            AssertExtensions.Throws<ArgumentException>("frameworkName", () => new FrameworkName("A"));
            AssertExtensions.Throws<ArgumentException>("frameworkName", () => new FrameworkName("A,B"));
            AssertExtensions.Throws<ArgumentException>("frameworkName", () => new FrameworkName("A,B,C"));
            AssertExtensions.Throws<ArgumentException>("frameworkName", () => new FrameworkName("A,Version=1.0.0.0,C"));
            AssertExtensions.Throws<ArgumentException>("frameworkName", () => new FrameworkName("A,1.0.0.0,Profile=C"));
            AssertExtensions.Throws<ArgumentException>("frameworkName", () => new FrameworkName("A,Version=1.z.0.0,Profile=C"));
            AssertExtensions.Throws<ArgumentException>("frameworkName", () => new FrameworkName("A,Something=1.z.0.0,Profile=C"));
            AssertExtensions.Throws<ArgumentException>("frameworkName", () => new FrameworkName("A,Profile=C"));
            AssertExtensions.Throws<ArgumentException>("frameworkName", () => new FrameworkName("A,======"));
            AssertExtensions.Throws<ArgumentException>("frameworkName", () => new FrameworkName("A,    =B="));
            AssertExtensions.Throws<ArgumentException>("frameworkName", () => new FrameworkName("A,1  =2=3"));
        }

        [Fact]
        public static void ConstructFromIdentifierVersion()
        {
            VerifyConstructor(new FrameworkName(TestIdentifier, s_testVersion),
                s_testNameNoProfileString, TestIdentifier, s_testVersion, string.Empty);
        }

        [Fact]
        public static void ConstructFromInvalidIdentifierVersion()
        {
            Assert.Throws<ArgumentNullException>(() => new FrameworkName(null, s_testVersion));
            AssertExtensions.Throws<ArgumentException>("identifier", () => new FrameworkName(string.Empty, s_testVersion));
            AssertExtensions.Throws<ArgumentException>("identifier", () => new FrameworkName("   \r\n\t", s_testVersion));

            Assert.Throws<ArgumentNullException>(() => new FrameworkName(TestIdentifier, null));
        }

        [Fact]
        public static void ConstructFromIdentifierVersionProfile()
        {
            VerifyConstructor(new FrameworkName(TestIdentifier, s_testVersion, TestProfile),
                s_testNameString, TestIdentifier, s_testVersion, TestProfile);

            VerifyConstructor(new FrameworkName(TestIdentifier, s_testVersion, null),
                s_testNameNoProfileString, TestIdentifier, s_testVersion, string.Empty);
        }

        [Fact]
        public static void ConstructFromInvalidIdentifierVersionProfile()
        {
            Assert.Throws<ArgumentNullException>(() => new FrameworkName(null, s_testVersion, TestProfile));
            AssertExtensions.Throws<ArgumentException>("identifier", () => new FrameworkName(string.Empty, s_testVersion, TestProfile));
            AssertExtensions.Throws<ArgumentException>("identifier", () => new FrameworkName("   \r\n\t", s_testVersion, TestProfile));

            Assert.Throws<ArgumentNullException>(() => new FrameworkName(TestIdentifier, null, TestProfile));
        }

        [Fact]
        public static void ConstructFromVersionWithoutBuildRevision()
        {
            var majorMinor = new Version(1, 2);
            Assert.Equal(-1, new FrameworkName(s_testName.Identifier, majorMinor).Version.Build);
            Assert.Equal(-1, new FrameworkName(s_testName.Identifier, majorMinor).Version.Revision);

            var majorMinorBuild = new Version(1, 2, 3);
            Assert.Equal(3, new FrameworkName(s_testName.Identifier, majorMinorBuild).Version.Build);
            Assert.Equal(-1, new FrameworkName(s_testName.Identifier, majorMinorBuild).Version.Revision);

            var majorMinorBuildRevision = new Version(1, 2, 3, 4);
            Assert.Equal(3, new FrameworkName(s_testName.Identifier, majorMinorBuildRevision).Version.Build);
            Assert.Equal(4, new FrameworkName(s_testName.Identifier, majorMinorBuildRevision).Version.Revision);
        }

        [Fact]
        public static void Equality()
        {
            VerifyEquality(null, null);
            VerifyEquality(s_testName, s_testName);
            VerifyEquality(s_testName, new FrameworkName(s_testNameString));
            VerifyEquality(s_testName, new FrameworkName(TestIdentifier, s_testVersion, TestProfile));
        }

        [Fact]
        public static void Inequality()
        {
            VerifyInequality(null, s_testName);
            VerifyInequality(s_testName, null);
            VerifyInequality(s_testName, new FrameworkName("NotTheTestIdentifier", s_testVersion, TestProfile));
            VerifyInequality(s_testName, new FrameworkName(TestIdentifier, s_testVersion));
            VerifyInequality(s_testName, new FrameworkName(TestIdentifier, new Version(9, 8, 7, 6), TestProfile));
        }

        private static void VerifyConstructor(FrameworkName name, string expectedName, string expectedIdentifier, Version expectedVersion, string expectedProfile)
        {
            Assert.Equal(expectedName, name.FullName);
            Assert.Equal(expectedName, name.ToString());
            Assert.Equal(expectedIdentifier, name.Identifier);
            Assert.Equal(expectedProfile, name.Profile);
            Assert.Equal(expectedVersion, name.Version);
        }

        private static void VerifyInequality(FrameworkName x, FrameworkName y)
        {
            Assert.True(x != y);
            Assert.False(x == y);
            if (x != null)
            {
                Assert.False(x.Equals(y));
                Assert.False(x.Equals((object)y));
                Assert.False(((IEquatable<FrameworkName>)x).Equals(y));
            }
        }

        private static void VerifyEquality(FrameworkName x, FrameworkName y)
        {
            Assert.True(x == y);
            Assert.False(x != y);
            if (x != null)
            {
                Assert.True(x.Equals(y));
                Assert.True(x.Equals((object)y));
                Assert.True(((IEquatable<FrameworkName>)x).Equals(y));
                Assert.Equal(x.GetHashCode(), y.GetHashCode());
            }
        }
    }
}
