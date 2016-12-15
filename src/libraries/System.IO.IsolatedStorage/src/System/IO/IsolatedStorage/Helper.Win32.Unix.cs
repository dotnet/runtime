// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Reflection;
using System.Runtime.InteropServices;
using System.Security;
using System.Security.AccessControl;
using System.Security.Principal;

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

            return dataDirectory;
        }

        internal static void CreateDirectory(string path, IsolatedStorageScope scope)
        {
            if (Directory.Exists(path))
                return;

            DirectoryInfo info = Directory.CreateDirectory(path);

            if (IsMachine(scope) && RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                // Need to emulate COMIsolatedStorage::CreateDirectoryWithDacl(), which gives the following rights:
                //
                //  World / Everyone (S-1-1-0 / SECURITY_WORLD_RID) -> (FILE_GENERIC_WRITE | FILE_GENERIC_READ) & (~WRITE_DAC)
                //  Creator Owner (S-1-3-0 / SECURITY_CREATOR_OWNER_RID) -> FILE_ALL_ACCESS
                //  Local Admins (S-1-5-32 / SECURITY_BUILTIN_DOMAIN_RID & DOMAIN_ALIAS_RID_ADMINS) -> FILE_ALL_ACCESS
                // 
                // When looking at rights through the GUI it looks like this:
                //
                //  "Everyone" -> Read, Write
                //  "Administrators" -> Full control
                //  "CREATOR OWNER" -> Full control
                //
                // With rights applying to "This folder, subfolders, and files". No inheritance from the parent folder.
                //
                // Note that trying to reset the rules for CREATOR OWNER leaves the current directory with the actual creator's SID.
                // (But applies CREATOR OWNER as expected for items and subdirectories.) Setting up front when creating the directory
                // doesn't exhibit this behavior, but as we can't currently do that we'll take the rough equivalent for now.

                DirectorySecurity security = new DirectorySecurity();

                // Don't inherit the existing rules
                security.SetAccessRuleProtection(isProtected: true, preserveInheritance: false);
                security.AddAccessRule(new FileSystemAccessRule(
                    identity: new SecurityIdentifier(WellKnownSidType.WorldSid, null),
                    fileSystemRights: FileSystemRights.Read | FileSystemRights.Write,
                    inheritanceFlags: InheritanceFlags.ContainerInherit | InheritanceFlags.ObjectInherit,
                    propagationFlags: PropagationFlags.None,
                    type: AccessControlType.Allow));

                security.AddAccessRule(new FileSystemAccessRule(
                    identity: new SecurityIdentifier(WellKnownSidType.BuiltinAdministratorsSid, null),
                    fileSystemRights: FileSystemRights.FullControl,
                    inheritanceFlags: InheritanceFlags.ContainerInherit | InheritanceFlags.ObjectInherit,
                    propagationFlags: PropagationFlags.None,
                    type: AccessControlType.Allow));

                security.AddAccessRule(new FileSystemAccessRule(
                    identity: new SecurityIdentifier(WellKnownSidType.CreatorOwnerSid, null),
                    fileSystemRights: FileSystemRights.FullControl,
                    inheritanceFlags: InheritanceFlags.ContainerInherit | InheritanceFlags.ObjectInherit,
                    propagationFlags: PropagationFlags.None,
                    type: AccessControlType.Allow));

                info.SetAccessControl(security);
            }
        }

        internal static void GetDefaultIdentityAndHash(out object identity, out string hash, char separator)
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
            // For CoreFX StrongName and Url are the only relevant types. By default evidence for the Domain comes
            // from the Assembly which comes from the EntryAssembly(). We'll emulate the legacy default behavior
            // by pulling directly from EntryAssembly.
            //
            // Note that it is possible that there won't be an EntryAssembly, which is something NetFX doesn't
            // have to deal with and shouldn't be likely on CoreFX due to a single AppDomain. Without Evidence
            // to pull from we'd have to dig into the use case to try and find a reasonable solution should we
            // run into this in the wild.

            Assembly assembly = Assembly.GetEntryAssembly();

            if (assembly == null)
                throw new IsolatedStorageException(SR.IsolatedStorage_Init);

            AssemblyName assemblyName = assembly.GetName();
            Uri codeBase = new Uri(assembly.CodeBase);

            hash = IdentityHelper.GetNormalizedStrongNameHash(assemblyName);
            if (hash != null)
            {
                hash = "StrongName" + separator + hash;
                identity = assemblyName;
            }
            else
            {
                hash = IdentityHelper.GetNormalizedUriHash(codeBase);
                hash = "Url" + separator + hash;
                identity = codeBase;
            }
        }
    }
}
