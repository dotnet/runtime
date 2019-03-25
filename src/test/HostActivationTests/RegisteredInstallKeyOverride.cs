// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.Win32;
using System;

namespace Microsoft.DotNet.CoreSetup.Test.HostActivation
{
    public class RegisteredInstallKeyOverride : IDisposable
    {
        public string KeyPath { get; }

        private readonly RegistryKey parentKey;
        private readonly RegistryKey key;
        private readonly string keyName;

        public RegisteredInstallKeyOverride()
        {
            // To test registered installs, we need a registry key which is:
            // - writable without admin access - so that the tests don't require admin to run
            // - redirected in WOW64 - so that there are both 32-bit and 64-bit versions of the key
            //   This is because the product stores the info in the 32-bit hive only and even 64-bit
            //   product must look into the 32-bit hive.
            //   Without the redirection we would not be able to test that the product always looks
            //   into 32-bit only.
            // Per this page https://docs.microsoft.com/en-us/windows/desktop/WinProg64/shared-registry-keys
            // a user writable redirected key is for example HKCU\Software\Classes\Interface
            // so we're going to use that one - it's not super clean as the key stores COM interfaces,
            // but we should not corrupt anything by adding a special subkey even if it's left behind.
            //
            // Note: If you want to inspect the values written by the test and/or modify them manually
            //   you have to navigate to HKCU\Software\Classes\Wow6432Node\Interface on a 64-bit OS.
            using (RegistryKey hkcu = RegistryKey.OpenBaseKey(RegistryHive.CurrentUser, RegistryView.Registry32))
            {
                parentKey = hkcu.CreateSubKey(@"Software\Classes\Interface");
                keyName = "_DOTNET_Test" + System.Diagnostics.Process.GetCurrentProcess().Id.ToString();
                key = parentKey.CreateSubKey(keyName);
                KeyPath = key.Name;
            }
        }

        public void SetInstallLocation(string installLocation, string architecture)
        {
            using (RegistryKey dotnetLocationKey = key.CreateSubKey($@"Setup\InstalledVersions\{architecture}"))
            {
                dotnetLocationKey.SetValue("InstallLocation", installLocation);
            }
        }

        public void Dispose()
        {
            parentKey.DeleteSubKeyTree(keyName, throwOnMissingSubKey: false);
            key.Dispose();
            parentKey.Dispose();
        }
    }
}
