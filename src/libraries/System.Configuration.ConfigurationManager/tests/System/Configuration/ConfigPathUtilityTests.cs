// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Configuration;
using Xunit;

namespace System.ConfigurationTests
{
    public class ConfigPathUtilityTests
    {
        [Theory,
            InlineData(null, false),
            InlineData(@"", false),
            InlineData(@"\", false),
            InlineData(@"/", false),
            InlineData(@".", false),
            InlineData(@"..", false),
            InlineData(@"a", true),
            InlineData(@"a\", false),
            InlineData(@"\a", false),
            InlineData(@"/a", false),
            InlineData(@"a/", false),
            InlineData(@"a/b", true),
            InlineData(@"a//c", false),
            InlineData(@"a\b", false),
            InlineData(@"a/b/c", true),
            InlineData(@"a/b./c", true),
            InlineData(@"a/b../c", true),
            InlineData(@"a/../c", false),
            InlineData(@"a/./c", false)
            ]
        public void IsValid(string configPath, bool expected)
        {
            Assert.Equal(expected, ConfigPathUtility.IsValid(configPath));
        }

        [Theory,
            InlineData(@"a", @"b", @"a/b")
            ]
        public void Combine(string parentConfigPath, string childConfigPath, string expected)
        {
            Assert.Equal(expected, ConfigPathUtility.Combine(parentConfigPath, childConfigPath));
        }

        [Theory,
            InlineData(@"a", new string[] { "a" }),
            InlineData(@"a/b", new string[] { "a", "b" })
            ]
        public void GetParts(string configPath, string[] expected)
        {
            Assert.Equal(expected, ConfigPathUtility.GetParts(configPath));
        }

        [Theory,
            InlineData(@"a", @"a"),
            InlineData(@"ab", @"ab"),
            InlineData(@"a/b", @"b"),
            InlineData(@"a/b/c", @"c")
            ]
        public void GetName(string configPath, string expected)
        {
            Assert.Equal(expected, ConfigPathUtility.GetName(configPath));
        }
    }
}
