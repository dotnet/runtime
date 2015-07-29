// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Xunit;

namespace System.IO.FileSystem.Tests
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
