// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using Xunit;

namespace System.Tests
{
    public partial class VersionTests
    {
        [Fact]
        public static void Ctor()
        {
            Version version = new Version();
            Assert.Equal(0, version.Major);
            Assert.Equal(0, version.Minor);
        }

        [Theory]
        [MemberData(nameof(CompareTo_TestData))]
        public static void CompareTo_Object(Version version1, Version version2, int expectedSign)
        {
            Assert.Equal(expectedSign, Math.Sign(version1.CompareTo(version2)));
        }

        [Theory]
        [MemberData(nameof(Parse_Valid_TestData))]
        public static void Clone_EqualsOriginal(string input, Version expected)
        {
            var actual = new Version(input);
            Assert.Equal(expected, actual);

            var clone = (Version)actual.Clone();
            Assert.Equal(expected, clone);
        }
    }
}
