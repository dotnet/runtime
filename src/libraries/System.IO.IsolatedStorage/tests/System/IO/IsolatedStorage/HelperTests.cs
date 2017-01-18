// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Reflection;
using Xunit;

namespace System.IO.IsolatedStorage.Tests
{
    public class HelperTests
    {
        [Fact]
        public void GetDefaultIdentityAndHash()
        {
            object identity;
            string hash;
            Helper.GetDefaultIdentityAndHash(out identity, out hash, '.');

            Assert.NotNull(identity);
            Assert.NotNull(hash);

            // We lie about the identity type when creating the folder structure as we're emulating the Evidence types
            // we don't have available in .NET Standard. We don't serialize the actual identity object, so the desktop
            // implementation will work with locations built off the hash.
            if (identity.GetType() == typeof(Uri))
            {
                Assert.StartsWith(@"Url.", hash);
            }
            else
            {
                Assert.IsType<AssemblyName>(identity);
                Assert.StartsWith(@"StrongName.", hash);
            }
        }

        [Theory
            InlineData(IsolatedStorageScope.Assembly)
            InlineData(IsolatedStorageScope.Assembly | IsolatedStorageScope.Roaming)
            InlineData(IsolatedStorageScope.Machine)
            ]
        public void GetDataDirectory(IsolatedStorageScope scope)
        {
            string path = Helper.GetDataDirectory(scope);
            Assert.Equal("IsolatedStorage", Path.GetFileName(path));
        }

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

        [Theory
            InlineData(IsolatedStorageScope.User)
            InlineData(IsolatedStorageScope.Machine)
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
