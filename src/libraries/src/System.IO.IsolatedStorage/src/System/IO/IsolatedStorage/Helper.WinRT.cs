// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Security;
using Windows.ApplicationModel;
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
                // Getting the shared local folder isn't possible if the policy for
                // "Allow a Windows app to share application data between users".
                dataDirectory = ApplicationData.Current.SharedLocalFolder?.Path;

                if (dataDirectory == null)
                {
                    throw new IsolatedStorageException(SR.IsolatedStorage_Scope_Invalid);
                }
            }
            if (!IsRoaming(scope))
            {
                dataDirectory = ApplicationData.Current.LocalFolder.Path;
            }
            else
            {
                dataDirectory = ApplicationData.Current.RoamingFolder.Path;
            }

            dataDirectory = Path.Combine(dataDirectory, IsolatedStorageDirectoryName);

            return dataDirectory;
        }

        internal static void CreateDirectory(string path, IsolatedStorageScope scope)
        {
            // ACL'ing isn't an issue in WinRT, just create it
            Directory.CreateDirectory(path);
        }

        internal static void GetDefaultIdentityAndHash(out object identity, out string hash, char separator)
        {
            // WinRT creates an ApplicationSecurityInfo off of the AppDomain.CurrentDomain.ActivationContext.
            // Evidence is built as follows:
            //
            //   StrongName <- ApplicationId.PublicKeyToken/Name/Version
            //   Url <- ApplicationContext.Identity.CodeBase
            //   Zone <- Zone.CreateFromUrl(Url)
            //   Site <- Site.CreateFromUrl(Url) *if* not file://

            // TODO: https://github.com/dotnet/corefx/issues/11123
            // When we have Assembly.GetEntryAssembly() we can utilize it to get the AssemblyName and
            // Codebase to unify the logic. For now we'll use installed location from the package.
            Uri codeBase = new Uri(Package.Current.InstalledLocation.Path);

            hash = IdentityHelper.GetNormalizedUriHash(codeBase);
            hash = "Url" + separator + hash;
            identity = codeBase;
        }

        internal static string GetRandomDirectory(string rootDirectory, IsolatedStorageScope scope)
        {
            // We didn't create random directories for UAP/UWP in the past. As the root locations are
            // scoped beneath app isolated folders we don't need the extra layer of obfuscation.
            return rootDirectory;
        }
    }
}
