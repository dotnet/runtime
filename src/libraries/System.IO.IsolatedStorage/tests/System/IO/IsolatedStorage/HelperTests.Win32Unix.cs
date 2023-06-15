// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Xunit;

namespace System.IO.IsolatedStorage
{
    public partial class HelperTests
    {
        [Fact]
        public void GetExistingRandomDirectory()
        {
            using (var temp = new TempDirectory())
            {
                Assert.Null(Helper.GetExistingRandomDirectory(temp.Path));

                string randomPath = Path.Combine(temp.Path, Path.GetRandomFileName(), Path.GetRandomFileName());
                Directory.CreateDirectory(randomPath);
                Assert.Equal(randomPath, Helper.GetExistingRandomDirectory(temp.Path));
            }
        }
    }
}
