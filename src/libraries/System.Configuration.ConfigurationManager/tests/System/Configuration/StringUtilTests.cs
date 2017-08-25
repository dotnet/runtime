// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Configuration;
using Xunit;

namespace System.ConfigurationTests
{
    public class StringUtilTests
    {
        [Theory,
            InlineData(null, null, true),
            InlineData(@"", null, true),
            InlineData(null, @"", true),
            InlineData(@"", @"", true),
            InlineData(@"a", null, false),
            InlineData(null, @"a", false),
            InlineData(@"a", @"", false),
            InlineData(@"", @"a", false),
            InlineData(@"A", @"a", false),
            InlineData(@"a", @"a", true)
            ]
        public void EqualsOrBothNullOrEmpty(string s1, string s2, bool expected)
        {
            Assert.Equal(expected, StringUtil.EqualsOrBothNullOrEmpty(s1, s2));
        }

        [Theory,
            InlineData(null, null, true),
            InlineData(@"", null, false),
            InlineData(null, @"", false),
            InlineData(@"", @"", true),
            InlineData(@"a", null, false),
            InlineData(null, @"a", false),
            InlineData(@"a", @"", false),
            InlineData(@"", @"a", false),
            InlineData(@"A", @"a", true),
            InlineData(@"a", @"a", true)
            ]
        public void EqualsIgnoreCase(string s1, string s2, bool expected)
        {
            Assert.Equal(expected, StringUtil.EqualsIgnoreCase(s1, s2));
        }

        [Theory,
            InlineData(null, null, false),
            InlineData(@"", null, false),
            InlineData(null, @"", false),
            InlineData(@"", @"", true),
            InlineData(@"a", null, false),
            InlineData(null, @"a", false),
            InlineData(@"a", @"", true),
            InlineData(@"", @"a", false),
            InlineData(@"A", @"a", false),
            InlineData(@"a", @"a", true),
            InlineData(@"a", @"abba", false),
            InlineData(@"abba", @"ab", true),
            InlineData(@"abba", @"abba", true),
            InlineData(@"ABBA", @"abba", false)
            ]
        public void StartsWithOrdinal(string s1, string s2, bool expected)
        {
            Assert.Equal(expected, StringUtil.StartsWithOrdinal(s1, s2));
        }

        [Theory,
            InlineData(null, null, false),
            InlineData(@"", null, false),
            InlineData(null, @"", false),
            InlineData(@"", @"", true),
            InlineData(@"a", null, false),
            InlineData(null, @"a", false),
            InlineData(@"a", @"", true),
            InlineData(@"", @"a", false),
            InlineData(@"A", @"a", true),
            InlineData(@"a", @"a", true),
            InlineData(@"a", @"abba", false),
            InlineData(@"abba", @"ab", true),
            InlineData(@"abba", @"abba", true),
            InlineData(@"ABBA", @"abba", true)
            ]
        public void StartsWithOrdinalIgnoreCase(string s1, string s2, bool expected)
        {
            Assert.Equal(expected, StringUtil.StartsWithOrdinalIgnoreCase(s1, s2));
        }

        [Theory,
            InlineData(new object[] { "1", "2", "3" }, new string[] { "1", "2", "3" } )
            ]
        public void ObjectArrayToStringArray(object[] objectArray, string[] expected)
        {
            Assert.Equal(expected, StringUtil.ObjectArrayToStringArray(objectArray));
        }
    }
}
