// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Xunit;

namespace System.IO.Tests
{
    public static class GetFullyQualifiedPathTests
    {
        [Fact]
        public static void IsPathFullyQualified_NullArgument()
        {
            Assert.Throws<ArgumentNullException>(() => Path.IsPathFullyQualified(null));
        }

        [Fact]
        public static void IsPathFullyQualified_Empty()
        {
            Assert.False(Path.IsPathFullyQualified(""));
            Assert.False(Path.IsPathFullyQualified(ReadOnlySpan<char>.Empty));
        }

        [PlatformSpecific(TestPlatforms.Windows)]
        [Theory]
        [InlineData("/")]
        [InlineData(@"\")]
        [InlineData(".")]
        [InlineData("C:")]
        [InlineData("C:foo.txt")]
        public static void IsPathFullyQualified_Windows_Invalid(string path)
        {
            Assert.False(Path.IsPathFullyQualified(path));
            Assert.False(Path.IsPathFullyQualified(path.AsSpan()));
        }

        [PlatformSpecific(TestPlatforms.Windows)]
        [Theory]
        [InlineData(@"\\")]
        [InlineData(@"\\\")]
        [InlineData(@"\\Server")]
        [InlineData(@"\\Server\Foo.txt")]
        [InlineData(@"\\Server\Share\Foo.txt")]
        [InlineData(@"\\Server\Share\Test\Foo.txt")]
        [InlineData(@"C:\")]
        [InlineData(@"C:\foo1")]
        [InlineData(@"C:\\")]
        [InlineData(@"C:\\foo2")]
        [InlineData(@"C:/")]
        [InlineData(@"C:/foo1")]
        [InlineData(@"C://")]
        [InlineData(@"C://foo2")]
        public static void IsPathFullyQualified_Windows_Valid(string path)
        {
            Assert.True(Path.IsPathFullyQualified(path));
            Assert.True(Path.IsPathFullyQualified(path.AsSpan()));
        }

        [PlatformSpecific(TestPlatforms.AnyUnix)]
        [Theory]
        [InlineData(@"\")]
        [InlineData(@"\\")]
        [InlineData(".")]
        [InlineData("./foo.txt")]
        [InlineData("..")]
        [InlineData("../foo.txt")]
        [InlineData(@"C:")]
        [InlineData(@"C:/")]
        [InlineData(@"C://")]
        public static void IsPathFullyQualified_Unix_Invalid(string path)
        {
            Assert.False(Path.IsPathFullyQualified(path));
            Assert.False(Path.IsPathFullyQualified(path.AsSpan()));
        }

        [PlatformSpecific(TestPlatforms.AnyUnix)]
        [Theory]
        [InlineData("/")]
        [InlineData("/foo.txt")]
        [InlineData("/..")]
        [InlineData("//")]
        [InlineData("//foo.txt")]
        [InlineData("//..")]
        public static void IsPathFullyQualified_Unix_Valid(string path)
        {
            Assert.True(Path.IsPathFullyQualified(path));
            Assert.True(Path.IsPathFullyQualified(path.AsSpan()));
        }
    }
}
