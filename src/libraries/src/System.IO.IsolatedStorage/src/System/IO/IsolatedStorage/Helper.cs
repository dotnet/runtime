// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.IO.IsolatedStorage
{
    internal static partial class Helper
    {
        private const string IsolatedStorageDirectoryName = "IsolatedStorage";

        private static string s_machineRootDirectory;
        private static string s_roamingUserRootDirectory;
        private static string s_userRootDirectory;

        /// <summary>
        /// The full root directory is the relevant special folder from Environment.GetFolderPath() plus "IsolatedStorage"
        /// and a set of random directory names if not roaming. (The random directories aren't created for WinRT as
        /// the FolderPath locations for WinRT are app isolated already.)
        /// 
        /// Examples:
        /// 
        ///     User: @"C:\Users\jerem\AppData\Local\IsolatedStorage\10v31ho4.bo2\eeolfu22.f2w\"
        ///     User|Roaming: @"C:\Users\jerem\AppData\Roaming\IsolatedStorage\"
        ///     Machine: @"C:\ProgramData\IsolatedStorage\nin03cyc.wr0\o3j0urs3.0sn\"
        /// 
        /// Identity for the current store gets tacked on after this.
        /// </summary>
        internal static string GetRootDirectory(IsolatedStorageScope scope)
        {
            if (IsRoaming(scope))
            {
                if (string.IsNullOrEmpty(s_roamingUserRootDirectory))
                {
                    s_roamingUserRootDirectory = GetDataDirectory(scope);
                }
                return s_roamingUserRootDirectory;
            }

            if (IsMachine(scope))
            {
                if (string.IsNullOrEmpty(s_machineRootDirectory))
                {
                    s_machineRootDirectory = GetRandomDirectory(GetDataDirectory(scope), scope);
                }
                return s_machineRootDirectory;
            }

            if (string.IsNullOrEmpty(s_userRootDirectory))
                s_userRootDirectory = GetRandomDirectory(GetDataDirectory(scope), scope);

            return s_userRootDirectory;
        }

        internal static bool IsMachine(IsolatedStorageScope scope) => ((scope & IsolatedStorageScope.Machine) != 0);
        internal static bool IsAssembly(IsolatedStorageScope scope) => ((scope & IsolatedStorageScope.Assembly) != 0);
        internal static bool IsApplication(IsolatedStorageScope scope) => ((scope & IsolatedStorageScope.Application) != 0);
        internal static bool IsRoaming(IsolatedStorageScope scope) => ((scope & IsolatedStorageScope.Roaming) != 0);
        internal static bool IsDomain(IsolatedStorageScope scope) => ((scope & IsolatedStorageScope.Domain) != 0);
    }
}
