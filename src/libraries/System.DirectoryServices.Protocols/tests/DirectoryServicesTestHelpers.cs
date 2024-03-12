// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.InteropServices;
using Xunit;

[assembly: ActiveIssue("https://github.com/dotnet/runtime/issues/35912", TestRuntimes.Mono)]

namespace System.DirectoryServices.Protocols.Tests
{
    public static class DirectoryServicesTestHelpers
    {
        public static bool IsWindowsOrLibLdapIsInstalled => RuntimeInformation.IsOSPlatform(OSPlatform.Windows) || IsLibLdapInstalled;

        // Cache the check once we have performed it once
        private static bool? _isLibLdapInstalled = null;

        /// <summary>
        /// Returns true if able to PInvoke into Linux or OSX, false otherwise
        /// </summary>
        public static bool IsLibLdapInstalled
        {
            get
            {
#if NETCOREAPP
                if (!_isLibLdapInstalled.HasValue)
                {
                    if (PlatformDetection.IsApplePlatform)
                    {
                        _isLibLdapInstalled = NativeLibrary.TryLoad("libldap.dylib", out _);
                    }
                    else
                    {
                        _isLibLdapInstalled = NativeLibrary.TryLoad("libldap-2.4.so.2", out _);
                    }
                }
                return _isLibLdapInstalled.Value;
#else
                _isLibLdapInstalled = true; // In .NET Framework ldap is always installed.
                return _isLibLdapInstalled.Value;
#endif
            }
        }
    }
}
