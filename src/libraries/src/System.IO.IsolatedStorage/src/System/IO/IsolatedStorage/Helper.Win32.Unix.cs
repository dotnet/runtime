// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Reflection;

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
            else if (IsRoaming(scope))
            {
                // SpecialFolder.ApplicationData -> C:\Users\Joe\AppData\Roaming
                dataDirectory = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            }
            else
            {
                // SpecialFolder.LocalApplicationData -> C:\Users\Joe\AppData\Local
                dataDirectory = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
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
                // TODO: https://github.com/dotnet/corefx/issues/11124
                // Machine scope, we need to ACL
                throw new NotImplementedException();
            }
        }

        internal static void GetDefaultIdentityAndHash(ref object identity, ref string hash, char separator)
        {
            // NetFX (desktop CLR) IsolatedStorage uses identity from System.Security.Policy.Evidence to build
            // the folder structure on disk. It would use the "best" available evidence in this order:
            //
            //  1. Publisher (Authenticode)
            //  2. StrongName
            //  3. Url (CodeBase)
            //  4. Site
            //  5. Zone
            //
            // For CoreFx StrongName and Url are the only relevant types. By default evidence for the Domain comes
            // from the Assembly which comes from the EntryAssembly(). We'll emulate the legacy default behavior
            // by pulling directly from EntryAssembly.

            Assembly assembly = Assembly.GetEntryAssembly();
            AssemblyName assemblyName = assembly.GetName();
            Uri codeBase = new Uri(assembly.CodeBase);

            hash = GetNormalizedStrongNameHash(assemblyName);
            if (hash != null)
            {
                hash = "StrongName" + separator + hash;
                identity = assemblyName;
            }
            else
            {
                hash = GetNormalizedUriHash(codeBase);
                hash = "Url" + separator + hash;
                identity = codeBase;
            }
        }
    }
}
