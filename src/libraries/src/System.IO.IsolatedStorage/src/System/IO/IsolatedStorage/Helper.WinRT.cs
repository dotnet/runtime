// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Windows.Storage;

namespace System.IO.IsolatedStorage
{
    internal static partial class Helper
    {
        internal static string GetDataDirectory(IsolatedStorageScope scope)
        {
            // This is the relevant special folder for the given scope plus "IsolatedStorage".
            // It is meant to replicate the behavior of the VM ComIsolatedStorage::GetRootDir().

            string dataDirectory = null;

            if (IsMachine(scope))
            {
                // TODO:
                // Windows 10 introduced a way to share across multiple users:
                // dataDirectory = dataDirectory = ApplicationData.Current.SharedLocalFolder.Path;
                throw new PlatformNotSupportedException();
            }
            else
            {
                if (!IsRoaming(scope))
                {
                    dataDirectory = ApplicationData.Current.LocalFolder.Path;
                }
                else
                {
                    dataDirectory = ApplicationData.Current.RoamingFolder.Path;
                }
            }

            dataDirectory = Path.Combine(dataDirectory, IsolatedStorageDirectoryName);
            CreateDirectory(dataDirectory, scope);
            return dataDirectory;
        }

        internal static void CreateDirectory(string path, IsolatedStorageScope scope)
        {
            // ACL'ing isn't an issue in WinRT, just create it
            Directory.CreateDirectory(path);
        }
    }
}
