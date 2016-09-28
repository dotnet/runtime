// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Reflection;
using Xunit;

namespace System.IO.IsolatedStorage
{
    public class GetStoreTests : IsoStorageTest
    {
        private static MethodInfo s_verifyScopeMethod;

        static GetStoreTests()
        {
            s_verifyScopeMethod = typeof(IsolatedStorage).GetMethod("VerifyScope", BindingFlags.NonPublic | BindingFlags.Static);
        }

        [Theory
            MemberData("ValidScopes")
            ]
        public void InitStore_ValidScopes(IsolatedStorageScope scope)
        {
            s_verifyScopeMethod.Invoke(null, new object[] { scope });
        }

        [Theory
            InlineData(IsolatedStorageScope.None)
            InlineData(IsolatedStorageScope.Machine | IsolatedStorageScope.Roaming)
            InlineData(IsolatedStorageScope.Machine | IsolatedStorageScope.User)
            ]
        public void InitStore_InvalidScopes(IsolatedStorageScope scope)
        {
            try
            {
                s_verifyScopeMethod.Invoke(null, new object[] { scope });
            }
            catch (TargetInvocationException e)
            {
                Assert.IsType<ArgumentException>(e.InnerException);
            }
        }

        [Fact]
        public void GetUserStoreForSite_ThrowsNotSupported()
        {
            Assert.Throws<NotSupportedException>(() => IsolatedStorageFile.GetUserStoreForSite());
        }

        [Fact]
        public void GetUserStoreForApplication()
        {
            var isf = IsolatedStorageFile.GetUserStoreForApplication();
            string root = isf.GetRootDirectory();
            Assert.EndsWith(@"AppFiles" + Path.DirectorySeparatorChar, root);
            Assert.True(Directory.Exists(root), "store root folder should exist");
        }

        [Fact]
        public void GetUserStoreForAssembly()
        {
            var isf = IsolatedStorageFile.GetUserStoreForAssembly();
            string root = isf.GetRootDirectory();
            Assert.EndsWith(@"AssemFiles" + Path.DirectorySeparatorChar, root);
            Assert.True(Directory.Exists(root), "store root folder should exist");
        }

        [Fact]
        public void GetUserStoreForDomain()
        {
            var isf = IsolatedStorageFile.GetUserStoreForDomain();
            string root = isf.GetRootDirectory();
            Assert.EndsWith(@"Files" + Path.DirectorySeparatorChar, root);
            Assert.True(Directory.Exists(root), "store root folder should exist");
        }
    }
}