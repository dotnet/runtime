// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Security;
using System.Threading;

namespace System.IO.IsolatedStorage
{
    internal static partial class Helper
    {
        private static string? s_machineRootDirectory;
        private static string? s_roamingUserRootDirectory;
        private static string? s_userRootDirectory;

        /// <summary>
        /// The full root directory is the relevant special folder from Environment.GetFolderPath() plus IsolatedStorageDirectoryName
        /// and a set of random directory names if not roaming. (The random directories aren't created for Android/iOS as
        /// the FolderPath locations for Android/iOS are app isolated already.)
        ///
        /// Examples:
        ///
        ///     User: @"C:\Users\jerem\AppData\Local\IsolatedStorage\10v31ho4.bo2\eeolfu22.f2w\"
        ///     User|Roaming: @"C:\Users\jerem\AppData\Roaming\IsolatedStorage\"
        ///     Machine: @"C:\ProgramData\IsolatedStorage\nin03cyc.wr0\o3j0urs3.0sn\"
        ///     Android path: "/data/user/0/{packageName}/files/.config/.isolated-storage"
        ///     iOS path: "/var/mobile/Containers/Data/Application/A323CBB9-A2B3-4432-9449-48CC20C07A7D/Documents/.config/.isolated-storage"
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

        internal static string? GetExistingRandomDirectory(string rootDirectory)
        {
            // Look for an existing random directory at the given root
            // (a set of nested directories that were created via Path.GetRandomFileName())

            // Older versions of the .NET Framework created longer (24 character) random paths and would
            // migrate them if they could not find the new style directory.

            if (!Directory.Exists(rootDirectory))
                return null;

            foreach (string directory in Directory.GetDirectories(rootDirectory))
            {
                if (Path.GetFileName(directory)?.Length == 12)
                {
                    foreach (string subdirectory in Directory.GetDirectories(directory))
                    {
                        if (Path.GetFileName(subdirectory)?.Length == 12)
                        {
                            return subdirectory;
                        }
                    }
                }
            }

            return null;
        }

        [UnconditionalSuppressMessage("SingleFile", "IL3000:Avoid accessing Assembly file path when publishing as a single file",
            Justification = "Code handles single-file deployment by using the information of the .exe file")]
        internal static void GetDefaultIdentityAndHash(out object identity, out string hash, char separator)
        {
            // In .NET Framework IsolatedStorage uses identity from System.Security.Policy.Evidence to build
            // the folder structure on disk. It would use the "best" available evidence in this order:
            //
            //  1. Publisher (Authenticode)
            //  2. StrongName
            //  3. Url (CodeBase)
            //  4. Site
            //  5. Zone
            //
            // For .NET Core StrongName and Url are the only relevant types. By default evidence for the Domain comes
            // from the Assembly which comes from the EntryAssembly(). We'll emulate the legacy default behavior
            // by pulling directly from EntryAssembly.
            //
            // Note that it is possible that there won't be an EntryAssembly, which is something the .NET Framework doesn't
            // have to deal with and isn't likely on .NET Core due to a single AppDomain. The exception is Android which
            // doesn't set an EntryAssembly.

            Assembly? assembly = Assembly.GetEntryAssembly();
            string? location = null;

            if (assembly != null)
            {
                AssemblyName assemblyName = assembly.GetName();

                hash = IdentityHelper.GetNormalizedStrongNameHash(assemblyName)!;
                if (hash != null)
                {
                    hash = string.Concat("StrongName", new ReadOnlySpan<char>(in separator), hash);
                    identity = assemblyName;
                    return;
                }
                else
                {
                    location = assembly.Location;
                }
            }

            // In case of SingleFile deployment, Assembly.Location is empty. On Android there is no entry assembly.
            if (string.IsNullOrEmpty(location))
                location = Environment.ProcessPath;
            if (string.IsNullOrEmpty(location))
                throw new IsolatedStorageException(SR.IsolatedStorage_Init);
            Uri locationUri = new Uri(location);
            hash = string.Concat("Url", new ReadOnlySpan<char>(in separator), IdentityHelper.GetNormalizedUriHash(locationUri));
            identity = locationUri;
        }

        internal static bool IsMachine(IsolatedStorageScope scope) => ((scope & IsolatedStorageScope.Machine) != 0);
        internal static bool IsAssembly(IsolatedStorageScope scope) => ((scope & IsolatedStorageScope.Assembly) != 0);
        internal static bool IsApplication(IsolatedStorageScope scope) => ((scope & IsolatedStorageScope.Application) != 0);
        internal static bool IsRoaming(IsolatedStorageScope scope) => ((scope & IsolatedStorageScope.Roaming) != 0);
        internal static bool IsDomain(IsolatedStorageScope scope) => ((scope & IsolatedStorageScope.Domain) != 0);
    }
}
