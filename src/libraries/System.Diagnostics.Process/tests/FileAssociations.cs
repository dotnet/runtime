// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Win32;

namespace System.Diagnostics.Tests
{
    internal static class FileAssociations
    {
        private const int SHCNE_ASSOCCHANGED = 0x8000000;
        private const int SHCNF_FLUSH = 0x1000;

        internal static void EnsureAssociationSet(string extension, string fileTypeDescription, string exePath, string programId)
        {
            bool commit = false;

            commit |= SetCurrentUserRegistryKey(@"Software\Classes\" + extension, programId);
            commit |= SetCurrentUserRegistryKey(@"Software\Classes\" + programId, fileTypeDescription);
            commit |= SetCurrentUserRegistryKey($@"Software\Classes\{programId}\shell\open\command", "\"" + exePath + "\" \"%1\"");

            if (commit)
            {
                Interop.SHChangeNotify(SHCNE_ASSOCCHANGED, SHCNF_FLUSH, IntPtr.Zero, IntPtr.Zero);
            }

            static bool SetCurrentUserRegistryKey(string keyPath, string value)
            {
                // using CurrentUser should avoid the need of using Admin account
                using (var key = Registry.CurrentUser.CreateSubKey(keyPath))
                {
                    if (key.GetValue(null) as string != value)
                    {
                        key.SetValue(null, value);
                        return true;
                    }
                }

                return false;
            }
        }
    }
}
