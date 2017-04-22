// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Security.AccessControl;
using System.Security.Principal;

namespace System.IO.IsolatedStorage
{
    internal static partial class Helper
    {
        internal static void CreateDirectory(string path, IsolatedStorageScope scope)
        {
            if (Directory.Exists(path))
                return;

            DirectoryInfo info = Directory.CreateDirectory(path);

            if (IsMachine(scope))
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
    }
}
