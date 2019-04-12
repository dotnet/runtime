// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using Xunit;

namespace System.Tests
{
    public partial class VersionTests
    {
        [Fact]
        public void Ctor_Default()
        {
            VerifyVersion(new Version(), 0, 0, -1, -1);
        }

        [Theory]
        [MemberData(nameof(Parse_Valid_TestData))]
        public static void Ctor_String(string input, Version expected)
        {
            Assert.Equal(expected, new Version(input));
        }

        [Theory]
        [MemberData(nameof(Parse_Invalid_TestData))]
        public static void CtorInvalidVerionString_ThrowsException(string input, Type exceptionType)
        {
            Assert.Throws(exceptionType, () => new Version(input));
        }

        [Theory]
        [InlineData(0, 0)]
        [InlineData(2, 3)]
        [InlineData(int.MaxValue, int.MaxValue)]
        public static void Ctor_Int_Int(int major, int minor)
        {
            VerifyVersion(new Version(major, minor), major, minor, -1, -1);
        }

        [Theory]
        [InlineData(0, 0, 0)]
        [InlineData(2, 3, 4)]
        [InlineData(int.MaxValue, int.MaxValue, int.MaxValue)]
        public static void Ctor_Int_Int_Int(int major, int minor, int build)
        {
            VerifyVersion(new Version(major, minor, build), major, minor, build, -1);
        }

        [Theory]
        [InlineData(0, 0, 0, 0)]
        [InlineData(2, 3, 4, 7)]
        [InlineData(2, 3, 4, 32767)]
        [InlineData(2, 3, 4, 32768)]
        [InlineData(2, 3, 4, 65535)]
        [InlineData(2, 3, 4, 65536)]
        [InlineData(2, 3, 4, 2147483647)]
        [InlineData(2, 3, 4, 2147450879)]
        [InlineData(2, 3, 4, 2147418112)]
        [InlineData(int.MaxValue, int.MaxValue, int.MaxValue, int.MaxValue)]
        public static void Ctor_Int_Int_Int_Int(int major, int minor, int build, int revision)
        {
            VerifyVersion(new Version(major, minor, build, revision), major, minor, build, revision);
        }

        [Fact]
        public void Ctor_NegativeMajor_ThrowsArgumentOutOfRangeException()
        {
            AssertExtensions.Throws<ArgumentOutOfRangeException>("major", () => new Version(-1, 0));
            AssertExtensions.Throws<ArgumentOutOfRangeException>("major", () => new Version(-1, 0, 0));
            AssertExtensions.Throws<ArgumentOutOfRangeException>("major", () => new Version(-1, 0, 0, 0));
        }

        [Fact]
        public void Ctor_NegativeMinor_ThrowsArgumentOutOfRangeException()
        {
            AssertExtensions.Throws<ArgumentOutOfRangeException>("minor", () => new Version(0, -1));
            AssertExtensions.Throws<ArgumentOutOfRangeException>("minor", () => new Version(0, -1, 0));
            AssertExtensions.Throws<ArgumentOutOfRangeException>("minor", () => new Version(0, -1, 0, 0));
        }

        [Fact]
        public void Ctor_NegativeBuild_ThrowsArgumentOutOfRangeException()
        {
            AssertExtensions.Throws<ArgumentOutOfRangeException>("build", () => new Version(0, 0, -1));
            AssertExtensions.Throws<ArgumentOutOfRangeException>("build", () => new Version(0, 0, -1, 0));
        }

        [Fact]
        public void Ctor_NegativeRevision_ThrowsArgumentOutOfRangeException()
        {
            AssertExtensions.Throws<ArgumentOutOfRangeException>("revision", () => new Version(0, 0, 0, -1));
        }

        public static IEnumerable<object[]> Comparison_TestData()
        {
            foreach (var input in new (Version v1, Version v2, int expectedSign)[]
            {
                (null, null, 0),

                (new Version(1, 2), null, 1),
                (new Version(1, 2), new Version(1, 2), 0),
                (new Version(1, 2), new Version(1, 3), -1),
                (new Version(1, 2), new Version(1, 1), 1),
                (new Version(1, 2), new Version(2, 0), -1),
                (new Version(1, 2), new Version(1, 2, 1), -1),
                (new Version(1, 2), new Version(1, 2, 0, 1), -1),
                (new Version(1, 2), new Version(1, 0), 1),
                (new Version(1, 2), new Version(1, 0, 1), 1),
                (new Version(1, 2), new Version(1, 0, 0, 1), 1),

                (new Version(3, 2, 1), null, 1),
                (new Version(3, 2, 1), new Version(2, 2, 1), 1),
                (new Version(3, 2, 1), new Version(3, 1, 1), 1),
                (new Version(3, 2, 1), new Version(3, 2, 0), 1),

                (new Version(1, 2, 3, 4), null, 1),
                (new Version(1, 2, 3, 4), new Version(1, 2, 3, 4), 0),
                (new Version(1, 2, 3, 4), new Version(1, 2, 3, 5), -1),
                (new Version(1, 2, 3, 4), new Version(1, 2, 3, 3), 1)
            })
            {
                yield return new object[] { input.v1, input.v2, input.expectedSign };
                yield return new object[] { input.v2, input.v1, input.expectedSign * -1 };
            }
        }

        [Theory]
        [MemberData(nameof(Comparison_TestData))]
        public void CompareTo_ReturnsExpected(Version version1, Version version2, int expectedSign)
        {
            Assert.Equal(expectedSign, Comparer<Version>.Default.Compare(version1, version2));
            if (version1 != null)
            {
                Assert.Equal(expectedSign, Math.Sign(((IComparable)version1).CompareTo(version2)));
                Assert.Equal(expectedSign, Math.Sign(version1.CompareTo((object)version2)));
                Assert.Equal(expectedSign, Math.Sign(version1.CompareTo(version2)));
            }
        }

        [ActiveIssue("https://github.com/dotnet/coreclr/pull/23898")]
        [SkipOnTargetFramework(TargetFrameworkMonikers.NetFramework, "https://github.com/dotnet/coreclr/pull/23898")]
        [Theory]
        [MemberData(nameof(Comparison_TestData))]
        public void ComparisonOperators_ReturnExpected(Version version1, Version version2, int expectedSign)
        {
            if (expectedSign < 0)
            {
                Assert.True(version1 < version2);
                Assert.True(version1 <= version2);
                Assert.False(version1 == version2);
                Assert.False(version1 >= version2);
                Assert.False(version1 > version2);
                Assert.True(version1 != version2);
            }
            else if (expectedSign == 0)
            {
                Assert.False(version1 < version2);
                Assert.True(version1 <= version2);
                Assert.True(version1 == version2);
                Assert.True(version1 >= version2);
                Assert.False(version1 > version2);
                Assert.False(version1 != version2);
            }
            else
            {
                Assert.False(version1 < version2);
                Assert.False(version1 <= version2);
                Assert.False(version1 == version2);
                Assert.True(version1 >= version2);
                Assert.True(version1 > version2);
                Assert.True(version1 != version2);
            }
        }

        [Theory]
        [InlineData(1)]
        [InlineData("1.1")]
        public void CompareTo_ObjectNotAVersion_ThrowsArgumentException(object other)
        {
            var version = new Version(1, 1);
            AssertExtensions.Throws<ArgumentException>(null, () => version.CompareTo(other));
            AssertExtensions.Throws<ArgumentException>(null, () => ((IComparable)version).CompareTo(other));
        }

        public static IEnumerable<object[]> Equals_TestData()
        {
            yield return new object[] { new Version(2, 3), new Version(2, 3), true };
            yield return new object[] { new Version(2, 3), new Version(2, 4), false };
            yield return new object[] { new Version(2, 3), new Version(3, 3), false };

            yield return new object[] { new Version(2, 3, 4), new Version(2, 3, 4), true };
            yield return new object[] { new Version(2, 3, 4), new Version(2, 3, 5), false };
            yield return new object[] { new Version(2, 3, 4), new Version(2, 3), false };

            yield return new object[] { new Version(2, 3, 4, 5), new Version(2, 3, 4, 5), true };
            yield return new object[] { new Version(2, 3, 4, 5), new Version(2, 3, 4, 6), false };
            yield return new object[] { new Version(2, 3, 4, 5), new Version(2, 3), false };
            yield return new object[] { new Version(2, 3, 4, 5), new Version(2, 3, 4), false };

            yield return new object[] { new Version(2, 3, 0), new Version(2, 3), false };
            yield return new object[] { new Version(2, 3, 4, 0), new Version(2, 3, 4), false };

            yield return new object[] { new Version(2, 3, 4, 5), new TimeSpan(), false };
            yield return new object[] { new Version(2, 3, 4, 5), null, false };
        }

        [Theory]
        [MemberData(nameof(Equals_TestData))]
        public static void Equals_Other_ReturnsExpected(Version version1, object obj, bool expected)
        {
            Version version2 = obj as Version;

            Assert.Equal(expected, version1.Equals(version2));
            Assert.Equal(expected, version1.Equals(obj));

            Assert.Equal(expected, version1 == version2);
            Assert.Equal(!expected, version1 != version2);

            if (version2 != null)
            {
                Assert.Equal(expected, version1.GetHashCode().Equals(version2.GetHashCode()));
            }
        }

        public static IEnumerable<object[]> Parse_Valid_TestData()
        {
            yield return new object[] { "1.2", new Version(1, 2) };
            yield return new object[] { "1.2.3", new Version(1, 2, 3) };
            yield return new object[] { "1.2.3.4", new Version(1, 2, 3, 4) };
            yield return new object[] { "2  .3.    4.  \t\r\n15  ", new Version(2, 3, 4, 15) };
            yield return new object[] { "   2  .3.    4.  \t\r\n15  ", new Version(2, 3, 4, 15) };
            yield return new object[] { "+1.+2.+3.+4", new Version(1, 2, 3, 4) };
        }

        [Theory]
        [MemberData(nameof(Parse_Valid_TestData))]
        public static void Parse_ValidInput_ReturnsExpected(string input, Version expected)
        {
            Assert.Equal(expected, Version.Parse(input));

            Assert.True(Version.TryParse(input, out Version version));
            Assert.Equal(expected, version);
        }

        public static IEnumerable<object[]> Parse_Invalid_TestData()
        {
            yield return new object[] { null, typeof(ArgumentNullException) }; // Input is null

            yield return new object[] { "", typeof(ArgumentException) }; // Input is empty
            yield return new object[] { "1,2,3,4", typeof(ArgumentException) }; // Input contains invalid separator
            yield return new object[] { "1", typeof(ArgumentException) }; // Input has fewer than 2 version components
            yield return new object[] { "1.2.3.4.5", typeof(ArgumentException) }; // Input has more than 4 version components

            yield return new object[] { "-1.2.3.4", typeof(ArgumentOutOfRangeException) }; // Input contains negative value
            yield return new object[] { "1.-2.3.4", typeof(ArgumentOutOfRangeException) }; // Input contains negative value
            yield return new object[] { "1.2.-3.4", typeof(ArgumentOutOfRangeException) }; // Input contains negative value
            yield return new object[] { "1.2.3.-4", typeof(ArgumentOutOfRangeException) }; // Input contains negative value

            yield return new object[] { "b.2.3.4", typeof(FormatException) }; // Input contains non-numeric value
            yield return new object[] { "1.b.3.4", typeof(FormatException) }; // Input contains non-numeric value
            yield return new object[] { "1.2.b.4", typeof(FormatException) }; // Input contains non-numeric value
            yield return new object[] { "1.2.3.b", typeof(FormatException) }; // Input contains non-numeric value

            yield return new object[] { "2147483648.2.3.4", typeof(OverflowException) }; // Input contains a value > int.MaxValue
            yield return new object[] { "1.2147483648.3.4", typeof(OverflowException) }; // Input contains a value > int.MaxValue
            yield return new object[] { "1.2.2147483648.4", typeof(OverflowException) }; // Input contains a value > int.MaxValue
            yield return new object[] { "1.2.3.2147483648", typeof(OverflowException) }; // Input contains a value > int.MaxValue

            // Input contains a value < 0
            yield return new object[] { "-1.2.3.4", typeof(ArgumentOutOfRangeException) };
            yield return new object[] { "1.-2.3.4", typeof(ArgumentOutOfRangeException) };
            yield return new object[] { "1.2.-3.4", typeof(ArgumentOutOfRangeException) };
            yield return new object[] { "1.2.3.-4", typeof(ArgumentOutOfRangeException) };
        }

        [Theory]
        [MemberData(nameof(Parse_Invalid_TestData))]
        public static void Parse_InvalidInput_ThrowsException(string input, Type exceptionType)
        {
            Assert.Throws(exceptionType, () => Version.Parse(input));

            Assert.False(Version.TryParse(input, out Version version));
            Assert.Null(version);
        }

        public static IEnumerable<object[]> ToString_TestData()
        {
            yield return new object[] { new Version(1, 2), new string[] { "", "1", "1.2" } };
            yield return new object[] { new Version(1, 2, 3), new string[] { "", "1", "1.2", "1.2.3" } };
            yield return new object[] { new Version(1, 2, 3, 4), new string[] { "", "1", "1.2", "1.2.3", "1.2.3.4" } };
        }

        [Theory]
        [MemberData(nameof(ToString_TestData))]
        public static void ToString_Invoke_ReturnsExpected(Version version, string[] expected)
        {
            for (int i = 0; i < expected.Length; i++)
            {
                Assert.Equal(expected[i], version.ToString(i));
            }

            int maxFieldCount = expected.Length - 1;
            Assert.Equal(expected[maxFieldCount], version.ToString());

            AssertExtensions.Throws<ArgumentException>("fieldCount", () => version.ToString(-1)); // Index < 0
            AssertExtensions.Throws<ArgumentException>("fieldCount", () => version.ToString(maxFieldCount + 1)); // Index > version.fieldCount
        }

        private static void VerifyVersion(Version version, int major, int minor, int build, int revision)
        {
            Assert.Equal(major, version.Major);
            Assert.Equal(minor, version.Minor);
            Assert.Equal(build, version.Build);
            Assert.Equal(revision, version.Revision);
            Assert.Equal((short)(revision >> 16), version.MajorRevision);
            Assert.Equal(unchecked((short)(revision & 0xFFFF)), version.MinorRevision);

            Version clone = Assert.IsType<Version>(version.Clone());
            Assert.NotSame(version, clone);
            Assert.Equal(version.Major, clone.Major);
            Assert.Equal(version.Minor, clone.Minor);
            Assert.Equal(version.Build, clone.Build);
            Assert.Equal(version.Revision, clone.Revision);
        }
    }
}
