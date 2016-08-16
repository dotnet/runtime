// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.


namespace System.IO.IsolatedStorage
{
    internal static partial class Helper
    {
        internal static string GetDataDirectory(IsolatedStorageScope scope)
        {
            // This is the relevant special folder for the given scope plus "IsolatedStorage".
            // It is meant to replicate the behavior of the VM ComIsolatedStorage::GetRootDir().

            // (note that Silverlight used "CoreIsolatedStorage" for a directory name and did not support machine scope)

            string dataDirectory = null;

            if (IsMachine(scope))
            {
                // SpecialFolder.CommonApplicationData -> C:\ProgramData
                dataDirectory = Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData);
            }
            else
            {
                if (!IsRoaming(scope))
                {
                    // SpecialFolder.LocalApplicationData -> C:\Users\Joe\AppData\Local
                    dataDirectory = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
                }
                else
                {
                    // SpecialFolder.ApplicationData -> C:\Users\Joe\AppData\Roaming
                    dataDirectory = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
                }
            }

            dataDirectory = Path.Combine(dataDirectory, IsolatedStorageDirectoryName);
            CreateDirectory(dataDirectory, scope);
            return dataDirectory;
        }

        internal static void CreateDirectory(string path, IsolatedStorageScope scope)
        {
            if (!IsMachine(scope))
            {
                Directory.CreateDirectory(path);
            }
            else
            {
                // TODO:
                // Machine scope, we need to ACL
                throw new NotImplementedException();
            }
        }
    }
}
