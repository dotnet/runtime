// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Security.Principal;

namespace System.Security.AccessControl
{
    public sealed class FileSystemAuditRule : AuditRule
    {
        public FileSystemAuditRule(
            IdentityReference identity,
            FileSystemRights fileSystemRights,
            AuditFlags flags)
            : this(
                identity,
                fileSystemRights,
                InheritanceFlags.None,
                PropagationFlags.None,
                flags)
        {
        }

        public FileSystemAuditRule(
            IdentityReference identity,
            FileSystemRights fileSystemRights,
            InheritanceFlags inheritanceFlags,
            PropagationFlags propagationFlags,
            AuditFlags flags)
            : this(
                identity,
                AccessMaskFromRights(fileSystemRights),
                false,
                inheritanceFlags,
                propagationFlags,
                flags)
        {
        }

        public FileSystemAuditRule(
            string identity,
            FileSystemRights fileSystemRights,
            AuditFlags flags)
            : this(
                new NTAccount(identity),
                fileSystemRights,
                InheritanceFlags.None,
                PropagationFlags.None,
                flags)
        {
        }

        public FileSystemAuditRule(
            string identity,
            FileSystemRights fileSystemRights,
            InheritanceFlags inheritanceFlags,
            PropagationFlags propagationFlags,
            AuditFlags flags)
            : this(
                new NTAccount(identity),
                AccessMaskFromRights(fileSystemRights),
                false,
                inheritanceFlags,
                propagationFlags,
                flags)
        {
        }

        internal FileSystemAuditRule(
            IdentityReference identity,
            int accessMask,
            bool isInherited,
            InheritanceFlags inheritanceFlags,
            PropagationFlags propagationFlags,
            AuditFlags flags)
            : base(
                identity,
                accessMask,
                isInherited,
                inheritanceFlags,
                propagationFlags,
                flags)
        {
        }

        private static int AccessMaskFromRights(FileSystemRights fileSystemRights)
        {
            if (fileSystemRights < 0 || fileSystemRights > FileSystemRights.FullControl)
                throw new ArgumentOutOfRangeException(nameof(fileSystemRights), SR.Format(SR.Argument_InvalidEnumValue, fileSystemRights, nameof(AccessControl.FileSystemRights)));

            return (int)fileSystemRights;
        }

        public FileSystemRights FileSystemRights
        {
            get { return FileSystemAccessRule.RightsFromAccessMask(AccessMask); }
        }
    }
}
