// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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

        [Theory,
            InlineData(IsolatedStorageScope.User),
            InlineData(IsolatedStorageScope.Machine),
            ]
        public void GetRandomDirectory(IsolatedStorageScope scope)
        {
            using (var temp = new TempDirectory())
            {
                string randomDir = Helper.GetRandomDirectory(temp.Path, scope);
                Assert.True(Directory.Exists(randomDir));
            }
        }
    }
}
