// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace System.PrivateUri.Tests
{
    public static class UncTest
    {
        [Fact]
        public static void Uri_WithTwoForwardSlashes_IsUnc()
        {
            // Will be recognized as UNC - even on Unix
            Assert.True(new Uri("//foo").IsUnc);
        }

        [Theory]
        // / and \ can be mixed and will be recognized the same on all platforms
        [InlineData(@"\\host")]
        [InlineData(@"//host")]
        [InlineData(@"\/host")]
        [InlineData(@"/\host")]
        public static void Uri_RecognizesUncPaths(string uriString)
        {
            var uri = new Uri(uriString);
            Assert.True(uri.IsUnc);
            Assert.Equal("host", uri.Host);
            Assert.Equal("file://host/", uri.AbsoluteUri);
            Assert.Equal(@"\\host", uri.LocalPath);

            // Same for explicit file form
            uri = new Uri("file://" + uriString);
            Assert.True(uri.IsUnc);
            Assert.Equal("host", uri.Host);
            Assert.Equal("file://host/", uri.AbsoluteUri);
            Assert.Equal(@"\\host", uri.LocalPath);
        }

        [Theory]
        [InlineData(@"\\host/share")]
        [InlineData(@"\\host\share")]
        [InlineData(@"//host/share")]
        [InlineData(@"//host\share")]
        [InlineData(@"\/host/share")]
        [InlineData(@"\/host\share")]
        [InlineData(@"/\host/share")]
        [InlineData(@"/\host\share")]
        public static void Uri_UncLocalPath_ConvertsForwardSlashes(string uriString)
        {
            var uri = new Uri(uriString);
            Assert.True(uri.IsUnc);
            Assert.Equal("host", uri.Host);
            Assert.Equal("file://host/share", uri.AbsoluteUri);
            Assert.Equal(@"\\host\share", uri.LocalPath);
            Assert.Equal(@"/share", uri.AbsolutePath);

            // Same for explicit file form
            uri = new Uri("file://" + uriString);
            Assert.True(uri.IsUnc);
            Assert.Equal("host", uri.Host);
            Assert.Equal("file://host/share", uri.AbsoluteUri);
            Assert.Equal(@"\\host\share", uri.LocalPath);
            Assert.Equal(@"/share", uri.AbsolutePath);
        }

        [Theory]
        [InlineData(@"\\host\..")]
        [InlineData(@"\\host/..")]
        [InlineData(@"//host\..")]
        [InlineData(@"//host/..")]
        [InlineData(@"\\host\foo\..\..")]
        [InlineData(@"//host\foo\..\..")]
        [InlineData(@"\/host\foo\..\..")]
        [InlineData(@"/\host\foo\..\..")]
        [InlineData(@"\\host/foo/../..")]
        [InlineData(@"\\host/foo/../../")]
        public static void Uri_UncPathEscaping_FixesOnHost(string uriString)
        {
            Uri uri = new Uri(uriString);
            Assert.True(uri.IsUnc);
            Assert.Equal("host", uri.Host);
            Assert.Equal(@"\\host\", uri.LocalPath);
            Assert.Equal("/", uri.AbsolutePath);

            // Same for explicit file form
            uri = new Uri("file://" + uriString);
            Assert.True(uri.IsUnc);
            Assert.Equal("host", uri.Host);
            Assert.Equal(@"\\host\", uri.LocalPath);
            Assert.Equal("/", uri.AbsolutePath);
        }
    }
}
