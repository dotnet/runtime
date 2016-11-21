// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace System.IO.Tests
{
    public static partial class PathTests
    {
        [Fact]
        public static void InvalidPathChars_MatchesGetInvalidPathChars()
        {
#pragma warning disable 0618
            Assert.NotNull(Path.InvalidPathChars);
            Assert.Equal(Path.GetInvalidPathChars(), Path.InvalidPathChars);
            Assert.Same(Path.InvalidPathChars, Path.InvalidPathChars);
#pragma warning restore 0618
        }
    }
}
