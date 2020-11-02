// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Xunit;

namespace Microsoft.Extensions.FileSystemGlobbing.Tests
{
    public class FilePatternMatchTests
    {
        [Fact]
        public void TestGetHashCode()
        {
            FilePatternMatch match1 = new FilePatternMatch("sub/sub2/bar/baz/three.txt", "sub2/bar/baz/three.txt");
            FilePatternMatch match2 = new FilePatternMatch("sub/sub2/bar/baz/three.txt", "sub2/bar/baz/three.txt");
            FilePatternMatch match3 = new FilePatternMatch("sub/sub2/bar/baz/one.txt", "sub2/bar/baz/three.txt");
            FilePatternMatch match4 = new FilePatternMatch("sub/sub2/bar/baz/three.txt", "sub2/bar/baz/one.txt");

            Assert.Equal(match1.GetHashCode(), match2.GetHashCode());
            Assert.NotEqual(match1.GetHashCode(), match3.GetHashCode());
            Assert.NotEqual(match1.GetHashCode(), match4.GetHashCode());

            // FilePatternMatch is case insensitive
            FilePatternMatch matchCase1 = new FilePatternMatch("Sub/Sub2/bar/baz/three.txt", "sub2/bar/baz/three.txt");
            FilePatternMatch matchCase2 = new FilePatternMatch("sub/sub2/bar/baz/three.txt", "Sub2/bar/baz/thrEE.txt");
            Assert.Equal(matchCase1.GetHashCode(), matchCase2.GetHashCode());
        }

        [Fact]
        public void TestGetHashCodeWithNull()
        {
            FilePatternMatch match = new FilePatternMatch(null, null);
            Assert.Equal(0, match.GetHashCode());

            int hash1 = new FilePatternMatch("non null", null).GetHashCode();
            int hash2 = new FilePatternMatch(null, "non null").GetHashCode();
            Assert.NotEqual(0, hash1);
            Assert.NotEqual(0, hash2);
            Assert.NotEqual(hash1, hash2);
        }
    }
}
