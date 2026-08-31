// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Security.Principal;

namespace System.Security.AccessControl
{
    public sealed class FileSystemAccessRule : AccessRule
    {
        //
        // Constructor for creating access rules for file objects
        //

        public FileSystemAccessRule(
            IdentityReference identity,
            FileSystemRights fileSystemRights,
            AccessControlType type)
            : this(
                identity,
                AccessMaskFromRights(fileSystemRights, type),
                false,
                InheritanceFlags.None,
                PropagationFlags.None,
                type)
        {
        }

        public FileSystemAccessRule(
            string identity,
            FileSystemRights fileSystemRights,
            AccessControlType type)
            : this(
                new NTAccount(identity),
                AccessMaskFromRights(fileSystemRights, type),
                false,
                InheritanceFlags.None,
                PropagationFlags.None,
                type)
        {
        }

        //
        // Constructor for creating access rules for folder objects
        //

        public FileSystemAccessRule(
            IdentityReference identity,
            FileSystemRights fileSystemRights,
            InheritanceFlags inheritanceFlags,
            PropagationFlags propagationFlags,
            AccessControlType type)
            : this(
                identity,
                AccessMaskFromRights(fileSystemRights, type),
                false,
                inheritanceFlags,
                propagationFlags,
                type)
        {
        }

        public FileSystemAccessRule(
            string identity,
            FileSystemRights fileSystemRights,
            InheritanceFlags inheritanceFlags,
            PropagationFlags propagationFlags,
            AccessControlType type)
            : this(
                new NTAccount(identity),
                AccessMaskFromRights(fileSystemRights, type),
                false,
                inheritanceFlags,
                propagationFlags,
                type)
        {
        }

        //
        // Internal constructor to be called by public constructors
        // and the access rule factory methods of {File|Folder}Security
        //

        internal FileSystemAccessRule(
            IdentityReference identity,
            int accessMask,
            bool isInherited,
            InheritanceFlags inheritanceFlags,
            PropagationFlags propagationFlags,
            AccessControlType type)
            : base(
                identity,
                accessMask,
                isInherited,
                inheritanceFlags,
                propagationFlags,
                type)
        {
        }

        public FileSystemRights FileSystemRights
        {
            get { return RightsFromAccessMask(AccessMask); }
        }

        // ACL's on files have a SYNCHRONIZE bit, and CreateFile ALWAYS
        // asks for it.  So for allows, let's always include this bit,
        // and for denies, let's never include this bit unless we're denying
        // full control.  This is the right thing for users, even if it does
        // make the model look asymmetrical from a purist point of view.
        internal static int AccessMaskFromRights(FileSystemRights fileSystemRights, AccessControlType controlType)
        {
            if (fileSystemRights < 0 || fileSystemRights > FileSystemRights.FullControl)
                throw new ArgumentOutOfRangeException(nameof(fileSystemRights), SR.Format(SR.Argument_InvalidEnumValue, fileSystemRights, nameof(AccessControl.FileSystemRights)));

            if (controlType == AccessControlType.Allow)
            {
                fileSystemRights |= FileSystemRights.Synchronize;
            }
            else if (controlType == AccessControlType.Deny)
            {
                if (fileSystemRights != FileSystemRights.FullControl &&
                    fileSystemRights != (FileSystemRights.FullControl & ~FileSystemRights.DeleteSubdirectoriesAndFiles))
                    fileSystemRights &= ~FileSystemRights.Synchronize;
            }

            return (int)fileSystemRights;
        }

        internal static FileSystemRights RightsFromAccessMask(int accessMask)
        {
            return (FileSystemRights)accessMask;
        }
    }
}
