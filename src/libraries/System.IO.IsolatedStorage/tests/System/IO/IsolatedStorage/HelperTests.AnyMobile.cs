// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Xunit;
using System.Reflection;
using System.Text;

namespace System.IO.IsolatedStorage
{
    public partial class HelperTests
    {                
        [Theory, InlineData(IsolatedStorageScope.User)]
        public void GetRandomDirectory(IsolatedStorageScope scope)
        {
            using (var temp = new TempDirectory())
            {
                string randomDir = Helper.GetRandomDirectory(temp.Path, scope);
                Assert.True(Directory.Exists(randomDir.Replace(Helper.IsolatedStorageDirectoryName, "")));
            }
        }

        [Theory,
            InlineData(IsolatedStorageScope.Assembly),
            InlineData(IsolatedStorageScope.Assembly | IsolatedStorageScope.Roaming),
            InlineData(IsolatedStorageScope.User)
            ]
        public void GetRandomDirectoryWithExistingDir(IsolatedStorageScope scope)
        {
            using (var temp = new TempDirectory())
            {
                Assert.Null(Helper.GetExistingRandomDirectory(temp.Path));

                string randomPath = Path.Combine(temp.Path, Path.GetRandomFileName(), Path.GetRandomFileName());
                Directory.CreateDirectory(randomPath);
                Assert.Equal(randomPath, Helper.GetExistingRandomDirectory(temp.Path));
                Assert.Equal(Helper.GetRandomDirectory(temp.Path, scope), Helper.GetExistingRandomDirectory(temp.Path));
            }
        }

        [Theory,
            InlineData(IsolatedStorageScope.Assembly),
            InlineData(IsolatedStorageScope.Assembly | IsolatedStorageScope.Roaming),
            InlineData(IsolatedStorageScope.User)
            ]
        public void GetRandomDirectoryWithNotExistingDir(IsolatedStorageScope scope)
        {
            using (var temp = new TempDirectory())
            {
                Assert.Null(Helper.GetExistingRandomDirectory(temp.Path));  
                Assert.Equal(Helper.GetRandomDirectory(Helper.GetDataDirectory(scope), scope), Helper.GetDataDirectory(scope));
            }
        }        

        [Fact]
        public void GetUserStoreForApplicationPath()
        {
            TestHelper.WipeStores();

            using (var isf = IsolatedStorageFile.GetUserStoreForApplication())
            {
                string root = isf.GetUserRootDirectory();
                Assert.EndsWith("/.config/.isolated-storage", root);
            }
        }

        [Fact]
        public void GetUserStoreForAssemblyPath()
        {
            TestHelper.WipeStores();

            using (var isf = IsolatedStorageFile.GetUserStoreForAssembly())
            {
                string root = isf.GetUserRootDirectory();
                Assert.EndsWith("/.config/.isolated-storage", root);               
            }
        }

        [Fact]
        public void GetUserStoreForDomainPath()
        {
            TestHelper.WipeStores();

            using (var isf = IsolatedStorageFile.GetUserStoreForDomain())
            {
                string root = isf.GetUserRootDirectory();
                Assert.EndsWith("/.config/.isolated-storage", root);                
            }
        }
    }
}
