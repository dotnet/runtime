// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Xunit;

namespace System.Tests
{
    public static partial class StringComparerTests
    {
        public static readonly object[][] FromComparison_TestData =
        {
            //             StringComparison                             StringComparer
            new object[] { StringComparison.CurrentCulture,             StringComparer.CurrentCulture },
            new object[] { StringComparison.CurrentCultureIgnoreCase,   StringComparer.CurrentCultureIgnoreCase },
            new object[] { StringComparison.InvariantCulture,           StringComparer.InvariantCulture },
            new object[] { StringComparison.InvariantCultureIgnoreCase, StringComparer.InvariantCultureIgnoreCase },
            new object[] { StringComparison.Ordinal,                    StringComparer.Ordinal },
            new object[] { StringComparison.OrdinalIgnoreCase,          StringComparer.OrdinalIgnoreCase },
        };

        [Theory]
        [MemberData(nameof(FromComparison_TestData))]
        public static void FromComparisonTest(StringComparison comparison, StringComparer comparer)
        {
            Assert.Equal(comparer, StringComparer.FromComparison(comparison));
        }

        [Fact]
        public static void FromComparisonInvalidTest()
        {
            StringComparison minInvalid = Enum.GetValues(typeof(StringComparison)).Cast<StringComparison>().Min() - 1;
            StringComparison maxInvalid = Enum.GetValues(typeof(StringComparison)).Cast<StringComparison>().Max() + 1;

            AssertExtensions.Throws<ArgumentException>("comparisonType", () => StringComparer.FromComparison(minInvalid));
            AssertExtensions.Throws<ArgumentException>("comparisonType", () => StringComparer.FromComparison(maxInvalid));
        }
    }
}
