// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using Xunit;

public class VersionTests
{
    [Theory]
    [MemberData(nameof(Parse_Valid_TestData))]
    public static void TestCtor_String(string input, Version expected)
    {
        Assert.Equal(expected, new Version(input));
    }

    [Theory]
    [MemberData(nameof(Parse_Invalid_TestData))]
    public static void TestCtor_String_Invalid(string input, Type exceptionType)
    {
        Assert.Throws(exceptionType, () => new Version(input)); // Input is invalid
    }

    [Theory]
    [InlineData(0, 0)]
    [InlineData(2, 3)]
    [InlineData(int.MaxValue, int.MaxValue)]
    public static void TestCtor_Int_Int(int major, int minor)
    {
        VerifyVersion(new Version(major, minor), major, minor, -1, -1);
    }

    [Fact]
    public static void TestCtor_Int_Int_Invalid()
    {
        Assert.Throws<ArgumentOutOfRangeException>("major", () => new Version(-1, 0)); // Major < 0
        Assert.Throws<ArgumentOutOfRangeException>("minor", () => new Version(0, -1)); // Minor < 0
    }

    [Theory]
    [InlineData(0, 0, 0)]
    [InlineData(2, 3, 4)]
    [InlineData(int.MaxValue, int.MaxValue, int.MaxValue)]
    public static void TestCtor_Int_Int_Int(int major, int minor, int build)
    {
        VerifyVersion(new Version(major, minor, build), major, minor, build, -1);
    }

    [Fact]
    public static void TestCtor_Int_Int_Int_Invalid()
    {
        Assert.Throws<ArgumentOutOfRangeException>("major", () => new Version(-1, 0, 0)); // Major < 0
        Assert.Throws<ArgumentOutOfRangeException>("minor", () => new Version(0, -1, 0)); // Minor < 0
        Assert.Throws<ArgumentOutOfRangeException>("build", () => new Version(0, 0, -1)); // Build < 0
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
    public static void TestCtor_Int_Int_Int_Int(int major, int minor, int build, int revision)
    {
        VerifyVersion(new Version(major, minor, build, revision), major, minor, build, revision);
    }

    [Fact]
    public static void TestCtor_Int_Int_Int_Int_Invalid()
    {
        Assert.Throws<ArgumentOutOfRangeException>("major", () => new Version(-1, 0, 0, 0)); // Major < 0
        Assert.Throws<ArgumentOutOfRangeException>("minor", () => new Version(0, -1, 0, 0)); // Minor < 0
        Assert.Throws<ArgumentOutOfRangeException>("build", () => new Version(0, 0, -1, 0)); // Build < 0
        Assert.Throws<ArgumentOutOfRangeException>("revision", () => new Version(0, 0, 0, -1)); // Revision < 0
    }

    public static IEnumerable<object[]> CompareToTestData()
    {
        yield return new object[] { new Version(1, 2), null, 1 };
        yield return new object[] { new Version(1, 2), new Version(1, 2), 0 };
        yield return new object[] { new Version(1, 2), new Version(1, 3), -1 };
        yield return new object[] { new Version(1, 2), new Version(1, 1), 1 };
        yield return new object[] { new Version(1, 2), new Version(2, 0), -1 };
        yield return new object[] { new Version(1, 2), new Version(1, 2, 1), -1 };
        yield return new object[] { new Version(1, 2), new Version(1, 2, 0, 1), -1 };
        yield return new object[] { new Version(1, 2), new Version(1, 0), 1 };
        yield return new object[] { new Version(1, 2), new Version(1, 0, 1), 1 };
        yield return new object[] { new Version(1, 2), new Version(1, 0, 0, 1), 1 };

        yield return new object[] { new Version(1, 2, 3, 4), new Version(1, 2, 3, 4), 0 };
        yield return new object[] { new Version(1, 2, 3, 4), new Version(1, 2, 3, 5), -1 };
        yield return new object[] { new Version(1, 2, 3, 4), new Version(1, 2, 3, 3), 1 };
    }

    [Theory]
    [MemberData(nameof(CompareToTestData))]
    public static void TestCompareTo(Version version1, Version obj, int expectedSign)
    {
        Version version2 = obj as Version;

        Assert.Equal(expectedSign, Math.Sign(version1.CompareTo(version2)));
        if (version1 != null && version2 != null)
        {
            if (expectedSign >= 0)
            {
                Assert.True(version1 >= version2);
                Assert.False(version1 < version2);
            }
            if (expectedSign > 0)
            {
                Assert.True(version1 > version2);
                Assert.False(version1 <= version2);
            }
            if (expectedSign <= 0)
            {
                Assert.True(version1 <= version2);
                Assert.False(version1 > version2);
            }
            if (expectedSign < 0)
            {
                Assert.True(version1 < version2);
                Assert.False(version1 >= version2);
            }
        }

        IComparable comparable = version1;
        Assert.Equal(expectedSign, Math.Sign(comparable.CompareTo(obj)));
    }

    [Fact]
    public static void TestCompareTo_Invalid()
    {
        IComparable comparable = new Version(1, 1);
        Assert.Throws<ArgumentException>(null, () => comparable.CompareTo(1)); // Obj is not a version
        Assert.Throws<ArgumentException>(null, () => comparable.CompareTo("1.1")); // Obj is not a version

        Version nullVersion = null;
        Version testVersion = new Version(1, 2);
        Assert.Throws<ArgumentNullException>("v1", () => testVersion >= nullVersion); // V2 is null
        Assert.Throws<ArgumentNullException>("v1", () => testVersion > nullVersion); // V2 is null
        Assert.Throws<ArgumentNullException>("v1", () => nullVersion < testVersion); // V1 is null
        Assert.Throws<ArgumentNullException>("v1", () => nullVersion <= testVersion); // V1 is null
    }

    private static IEnumerable<object[]> EqualsTestData()
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

        yield return new object[] { new Version(2, 3, 4, 5), null, false };
    }

    [Theory]
    [MemberData(nameof(EqualsTestData))]
    public static void TestEquals(Version version1, object obj, bool expected)
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

    private static IEnumerable<object[]> Parse_Valid_TestData()
    {
        yield return new object[] { "1.2", new Version(1, 2) };
        yield return new object[] { "1.2.3", new Version(1, 2, 3) };
        yield return new object[] { "1.2.3.4", new Version(1, 2, 3, 4) };
        yield return new object[] { "2  .3.    4.  \t\r\n15  ", new Version(2, 3, 4, 15) };
    }

    [Theory]
    [MemberData(nameof(Parse_Valid_TestData))]
    public static void TestParse(string input, Version expected)
    {
        Assert.Equal(expected, Version.Parse(input));

        Version version;
        Assert.True(Version.TryParse(input, out version));
        Assert.Equal(expected, version);
    }

    private static IEnumerable<object[]> Parse_Invalid_TestData()
    {
        yield return new object[] { null, typeof(ArgumentNullException) }; // Input is null

        yield return new object[] { "", typeof(ArgumentException) }; // Input is empty
        yield return new object[] { "1,2,3,4", typeof(ArgumentException) }; // Input has fewer than 4 version components
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
    }

    [Theory]
    [MemberData(nameof(Parse_Invalid_TestData))]
    public static void TestParse_Invalid(string input, Type exceptionType)
    {
        Assert.Throws(exceptionType, () => Version.Parse(input));
        Version version;
        Assert.False(Version.TryParse(input, out version));
    }

    public static IEnumerable<object[]> ToStringTestData()
    {
        yield return new object[] { new Version(1, 2), new string[] { "", "1", "1.2" } };
        yield return new object[] { new Version(1, 2, 3), new string[] { "", "1", "1.2", "1.2.3" } };
        yield return new object[] { new Version(1, 2, 3, 4), new string[] { "", "1", "1.2", "1.2.3", "1.2.3.4" } };
    }

    [Theory]
    [MemberData(nameof(ToStringTestData))]
    public static void TestToString(Version version, string[] expected)
    {
        for (int i = 0; i < expected.Length; i++)
        {
            Assert.Equal(expected[i], version.ToString(i));
        }

        int maxFieldCount = expected.Length - 1;
        Assert.Equal(expected[maxFieldCount], version.ToString());

        Assert.Throws<ArgumentException>("fieldCount", () => version.ToString(-1)); // Index < 0
        Assert.Throws<ArgumentException>("fieldCount", () => version.ToString(maxFieldCount + 1)); // Index > version.fieldCount
    }

    private static void VerifyVersion(Version version, int major, int minor, int build, int revision)
    {
        Assert.Equal(major, version.Major);
        Assert.Equal(minor, version.Minor);
        Assert.Equal(build, version.Build);
        Assert.Equal(revision, version.Revision);
        Assert.Equal((short)(revision >> 16), version.MajorRevision);
        Assert.Equal((short)(revision & 0xFFFF), version.MinorRevision);
    }
}
