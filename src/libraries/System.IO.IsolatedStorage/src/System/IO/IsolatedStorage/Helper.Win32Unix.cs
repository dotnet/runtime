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
        internal static string GetDataDirectory(IsolatedStorageScope scope)
        {
            // This is the relevant special folder for the given scope plus "IsolatedStorage".
            // It is meant to replicate the behavior of the VM ComIsolatedStorage::GetRootDir().

            // (note that Silverlight used "CoreIsolatedStorage" for a directory name and did not support machine scope)

            Environment.SpecialFolder specialFolder =
            IsMachine(scope) ? Environment.SpecialFolder.CommonApplicationData : // e.g. C:\ProgramData
            IsRoaming(scope) ? Environment.SpecialFolder.ApplicationData : // e.g. C:\Users\Joe\AppData\Roaming
            Environment.SpecialFolder.LocalApplicationData; // e.g. C:\Users\Joe\AppData\Local

            string dataDirectory = Environment.GetFolderPath(specialFolder, Environment.SpecialFolderOption.Create);
            dataDirectory = Path.Combine(dataDirectory, IsolatedStorageDirectoryName);

            return dataDirectory;
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
            // have to deal with and shouldn't be likely on .NET Core due to a single AppDomain. Without Evidence
            // to pull from we'd have to dig into the use case to try and find a reasonable solution should we
            // run into this in the wild.

            Assembly? assembly = Assembly.GetEntryAssembly();

            if (assembly == null)
                throw new IsolatedStorageException(SR.IsolatedStorage_Init);

            AssemblyName assemblyName = assembly.GetName();

            hash = IdentityHelper.GetNormalizedStrongNameHash(assemblyName)!;
            if (hash != null)
            {
                hash = "StrongName" + separator + hash;
                identity = assemblyName;
            }
            else
            {
                string? location = assembly.Location;
                // In case of SingleFile deployment, Assembly.Location is empty.
                if (location == string.Empty)
                    location = Environment.ProcessPath;
                if (string.IsNullOrEmpty(location))
                    throw new IsolatedStorageException(SR.IsolatedStorage_Init);
                Uri locationUri = new Uri(location);
                hash = "Url" + separator + IdentityHelper.GetNormalizedUriHash(locationUri);
                identity = locationUri;
            }
        }

        internal static string GetRandomDirectory(string rootDirectory, IsolatedStorageScope scope)
        {
            string? randomDirectory = GetExistingRandomDirectory(rootDirectory);
            if (string.IsNullOrEmpty(randomDirectory))
            {
                using (Mutex m = CreateMutexNotOwned(rootDirectory))
                {
                    if (!m.WaitOne())
                    {
                        throw new IsolatedStorageException(SR.IsolatedStorage_Init);
                    }

                    try
                    {
                        randomDirectory = GetExistingRandomDirectory(rootDirectory);
                        if (string.IsNullOrEmpty(randomDirectory))
                        {
                            // Someone else hasn't created the directory before we took the lock
                            randomDirectory = Path.Combine(rootDirectory, Path.GetRandomFileName(), Path.GetRandomFileName());
                            CreateDirectory(randomDirectory, scope);
                        }
                    }
                    finally
                    {
                        m.ReleaseMutex();
                    }
                }
            }

            return randomDirectory;
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

        private static Mutex CreateMutexNotOwned(string pathName)
        {
            return new Mutex(initiallyOwned: false, name: @"Global\" + IdentityHelper.GetStrongHashSuitableForObjectName(pathName));
        }
    }
}
