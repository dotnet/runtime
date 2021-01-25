// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Xunit;

namespace System.IO.Tests
{
    public class DirectoryInfo_Parent : Directory_GetParent
    {
        protected override DirectoryInfo GetParent(string path)
        {
            return new DirectoryInfo(path).Parent;
        }

        [Fact]
        public void TrailingSlashes()
        {
            var test = GetParent(Path.Combine(TestDirectory, "a") + Path.DirectorySeparatorChar);
            Assert.Equal(TestDirectory, test.FullName);
        }
    }
}
