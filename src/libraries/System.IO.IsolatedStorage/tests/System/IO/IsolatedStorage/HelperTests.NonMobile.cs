// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Xunit;

namespace System.IO.IsolatedStorage
{
    public partial class HelperTests
    {        
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
