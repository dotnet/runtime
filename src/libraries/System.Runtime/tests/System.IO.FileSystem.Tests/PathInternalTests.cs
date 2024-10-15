// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Xunit;

namespace System.IO.Tests
{
    public class PathInternalTests : FileSystemTest
    {
        [Fact]
        [OuterLoop]
        public void PathInternalIsCaseSensitiveMatchesProbing()
        {
            string probingDirectory = TestDirectory;
            Assert.Equal(GetIsCaseSensitiveByProbing(probingDirectory), PathInternal.IsCaseSensitive);
        }
    }
}
