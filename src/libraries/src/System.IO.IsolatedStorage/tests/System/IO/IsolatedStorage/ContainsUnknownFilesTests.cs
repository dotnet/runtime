// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Reflection;
using Xunit;

namespace System.IO.IsolatedStorage
{
    public class ContainsUnknownFilesTests : IsoStorageTest
    {
        private static MethodInfo s_containsUnknownFilesMethod
            = typeof(IsolatedStorageFile).GetMethod("ContainsUnknownFiles", BindingFlags.NonPublic | BindingFlags.Instance);

        [Theory, MemberData(nameof(ValidStores))]
        public void ContainsUnknownFiles_CleanStore(PresetScopes scope)
        {
            TestHelper.WipeStores();

            using (var isf = GetPresetScope(scope))
            {
                Assert.False((bool)s_containsUnknownFilesMethod.Invoke(isf, new object[] { isf.GetUserRootDirectory() }));
            }
        }

        [Theory, MemberData(nameof(ValidStores))]
        public void ContainsUnknownFiles_OkFiles(PresetScopes scope)
        {
            TestHelper.WipeStores();

            using (var isf = GetPresetScope(scope))
            {
                string rootDirectory = isf.GetIdentityRootDirectory();
                string identityFile = Path.Combine(rootDirectory, "identity.dat");
                string quotaFile = Path.Combine(rootDirectory, "info.dat");
                using (File.OpenWrite(identityFile)) { }
                Assert.False((bool)s_containsUnknownFilesMethod.Invoke(isf, new object[] { rootDirectory }), "identity ok");
                File.Move(identityFile, quotaFile);
                Assert.False((bool)s_containsUnknownFilesMethod.Invoke(isf, new object[] { rootDirectory }), "quota ok");
                using (File.OpenWrite(identityFile)) { }
                Assert.False((bool)s_containsUnknownFilesMethod.Invoke(isf, new object[] { rootDirectory }), "both ok");
            }
        }

        [Theory, MemberData(nameof(ValidStores))]
        public void ContainsUnknownFiles_NotOkFiles(PresetScopes scope)
        {
            TestHelper.WipeStores();

            using (var isf = GetPresetScope(scope))
            {
                string rootDirectory = isf.GetIdentityRootDirectory();
                string otherFile = Path.Combine(rootDirectory, "ContainsUnknownFiles_NotOkFiles");
                string identityFile = Path.Combine(rootDirectory, "identity.dat");
                string quotaFile = Path.Combine(rootDirectory, "info.dat");
                using (File.OpenWrite(otherFile)) { }
                Assert.True((bool)s_containsUnknownFilesMethod.Invoke(isf, new object[] { rootDirectory }), "other file not ok");
                using (File.OpenWrite(identityFile)) { }
                Assert.True((bool)s_containsUnknownFilesMethod.Invoke(isf, new object[] { rootDirectory }), "other file with identity not ok");
                File.Move(identityFile, quotaFile);
                Assert.True((bool)s_containsUnknownFilesMethod.Invoke(isf, new object[] { rootDirectory }), "other file with quota not ok");
                using (File.OpenWrite(identityFile)) { }
                Assert.True((bool)s_containsUnknownFilesMethod.Invoke(isf, new object[] { rootDirectory }), "too many files not ok");
            }
        }

        [Theory, MemberData(nameof(ValidStores))]
        public void ContainsUnknownFiles_NotOkDirectory(PresetScopes scope)
        {
            TestHelper.WipeStores();

            using (var isf = GetPresetScope(scope))
            {
                string rootDirectory = isf.GetIdentityRootDirectory();
                string otherDirectory = Path.Combine(rootDirectory, "ContainsUnknownFiles_NotOkDirectory");
                Directory.CreateDirectory(otherDirectory);
                Assert.True((bool)s_containsUnknownFilesMethod.Invoke(isf, new object[] { rootDirectory }), "other directory not ok");
            }
        }
    }
}
