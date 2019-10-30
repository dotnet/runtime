// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.IO;
using Xunit;

namespace Microsoft.Win32.RegistryTests
{
    public class RegistryKey_OpenRemoteBaseKey_rh_str_rv : RegistryTestsBase
    {
        [Fact]
        public void NegativeTests()
        {
            // Should throw if passed machine name is null
            Assert.Throws<ArgumentNullException>(() => RegistryKey.OpenRemoteBaseKey(RegistryHive.LocalMachine, null, RegistryView.Default));

            // Should throw if remote machine does not exist
            Assert.Throws<IOException>(() => RegistryKey.OpenRemoteBaseKey(RegistryHive.LocalMachine, "Non-Existing-Machine", RegistryView.Default));
        }
    }
}
