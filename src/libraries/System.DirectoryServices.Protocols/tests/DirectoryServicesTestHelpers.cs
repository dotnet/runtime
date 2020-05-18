// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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
                if (!_isLibLdapInstalled.HasValue)
                {
                    try
                    {
                        // Attempt PInvoking into libldap on Linux
                        IntPtr handle = ber_alloc_Linux(1);
                        ber_free_Linux(handle, 1);
                        _isLibLdapInstalled = true;
                    }
                    catch (Exception)
                    {
                        try
                        {
                            // Attempt PInvoking into libldap on OSX
                            IntPtr handle = ber_alloc_OSX(1);
                            ber_free_OSX(handle, 1);
                            _isLibLdapInstalled = true;
                        }
                        catch (Exception)
                        {
                            _isLibLdapInstalled = false;
                        }
                    }
                }
                return _isLibLdapInstalled.Value;
            }
        }

        internal const string OpenLdapLinux = "libldap-2.4.so.2";
        internal const string OpenLdapOSX = "libldap";

        [DllImport(OpenLdapLinux, EntryPoint = "ber_alloc_t", CharSet = CharSet.Ansi)]
        internal static extern IntPtr ber_alloc_Linux(int option);

        [DllImport(OpenLdapLinux, EntryPoint = "ber_free", CharSet = CharSet.Ansi)]
        public static extern IntPtr ber_free_Linux([In] IntPtr berelement, int option);

        [DllImport(OpenLdapOSX, EntryPoint = "ber_alloc_t", CharSet = CharSet.Ansi)]
        internal static extern IntPtr ber_alloc_OSX(int option);

        [DllImport(OpenLdapOSX, EntryPoint = "ber_free", CharSet = CharSet.Ansi)]
        public static extern IntPtr ber_free_OSX([In] IntPtr berelement, int option);
    }
}
