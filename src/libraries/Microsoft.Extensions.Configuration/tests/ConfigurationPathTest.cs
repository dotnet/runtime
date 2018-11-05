// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Xunit;

namespace Microsoft.Extensions.Configuration.Test
{
    public class ConfigurationPathTest
    {
        [Fact]
        public void CombineWithEmptySegmentLeavesDelimiter()
        {
            Assert.Equal("parent:", ConfigurationPath.Combine("parent", ""));
            Assert.Equal("parent::", ConfigurationPath.Combine("parent", "", ""));
            Assert.Equal("parent:::key", ConfigurationPath.Combine("parent", "", "", "key"));
        }

        [Fact]
        public void GetLastSegmenGetSectionKeyTests()
        {
            Assert.Null(ConfigurationPath.GetSectionKey(null));
            Assert.Equal("", ConfigurationPath.GetSectionKey(""));
            Assert.Equal("", ConfigurationPath.GetSectionKey(":::"));
            Assert.Equal("c", ConfigurationPath.GetSectionKey("a::b:::c"));
            Assert.Equal("", ConfigurationPath.GetSectionKey("a:::b:"));
            Assert.Equal("key", ConfigurationPath.GetSectionKey("key"));
            Assert.Equal("key", ConfigurationPath.GetSectionKey(":key"));
            Assert.Equal("key", ConfigurationPath.GetSectionKey("::key"));
            Assert.Equal("key", ConfigurationPath.GetSectionKey("parent:key"));
        }

        [Fact]
        public void GetParentPathTests()
        {
            Assert.Null(ConfigurationPath.GetParentPath(null));
            Assert.Null(ConfigurationPath.GetParentPath(""));
            Assert.Equal("::", ConfigurationPath.GetParentPath(":::"));
            Assert.Equal("a::b::", ConfigurationPath.GetParentPath("a::b:::c"));
            Assert.Equal("a:::b", ConfigurationPath.GetParentPath("a:::b:"));
            Assert.Null(ConfigurationPath.GetParentPath("key"));
            Assert.Equal("", ConfigurationPath.GetParentPath(":key"));
            Assert.Equal(":", ConfigurationPath.GetParentPath("::key"));
            Assert.Equal("parent", ConfigurationPath.GetParentPath("parent:key"));
        }

    }
}
