// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using Xunit;

namespace Microsoft.Win32.RegistryTests
{
    public abstract class RegistryTestsBase : IDisposable
    {
        protected string TestRegistryKeyName { get; private set; }
        protected RegistryKey TestRegistryKey { get; private set; }

        protected RegistryTestsBase()
        {
            // Create a unique name for this test class
            TestRegistryKeyName = CreateUniqueKeyName();

            // Cleanup the key in case a previous run of this test crashed and left
            // the key behind.  The key name is specific enough to corefx that we don't
            // need to worry about it being a real key on the user's system used
            // for another purpose.
            RemoveKeyIfExists(TestRegistryKeyName);

            // Then create the key.
            TestRegistryKey = Registry.CurrentUser.CreateSubKey(TestRegistryKeyName);
            Assert.NotNull(TestRegistryKey);
        }

        public void Dispose()
        {
            TestRegistryKey.Dispose();
            RemoveKeyIfExists(TestRegistryKeyName);
        }

        private static void RemoveKeyIfExists(string keyName)
        {
            RegistryKey rk = Registry.CurrentUser;
            if (rk.OpenSubKey(keyName) != null)
            {
                rk.DeleteSubKeyTree(keyName);
                Assert.Null(rk.OpenSubKey(keyName));
            }
        }

        private string CreateUniqueKeyName()
        {
            // Create a name to use for this class of tests. The name includes:
            // - A "corefxtest" prefix to help make it clear to anyone looking at the registry
            //   that these keys are test-only and can be deleted, in case the tests crash and 
            //   we end up leaving some keys behind.
            // - The name of this test class, so as to avoid problems with tests on different test 
            //   classes running concurrently
            return "corefxtest_" + GetType().Name;
        }
    }
}
