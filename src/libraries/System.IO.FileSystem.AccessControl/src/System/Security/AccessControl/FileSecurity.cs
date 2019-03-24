// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

/*============================================================
**
**
**
** Purpose: Managed ACL wrapper for files & directories.
**
**
===========================================================*/

using Microsoft.Win32.SafeHandles;
using Microsoft.Win32;
using System.Collections;
using System.IO;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using System.Security.AccessControl;
using System.Security.Principal;
using System;

namespace System.Security.AccessControl
{
    // Constants from winnt.h - search for FILE_WRITE_DATA, etc.
    [Flags]
    public enum FileSystemRights
    {
        // No None field - An ACE with the value 0 cannot grant nor deny.
        ReadData = 0x000001,
        ListDirectory = ReadData,     // For directories
        WriteData = 0x000002,
        CreateFiles = WriteData,    // For directories
        AppendData = 0x000004,
        CreateDirectories = AppendData,   // For directories
        ReadExtendedAttributes = 0x000008,
        WriteExtendedAttributes = 0x000010,
        ExecuteFile = 0x000020,     // For files
        Traverse = ExecuteFile,  // For directories
        // DeleteSubdirectoriesAndFiles only makes sense on directories, but
        // the shell explicitly sets it for files in its UI.  So we'll include
        // it in FullControl.
        DeleteSubdirectoriesAndFiles = 0x000040,
        ReadAttributes = 0x000080,
        WriteAttributes = 0x000100,
        Delete = 0x010000,
        ReadPermissions = 0x020000,
        ChangePermissions = 0x040000,
        TakeOwnership = 0x080000,
        // From the Core File Services team, CreateFile always requires
        // SYNCHRONIZE access.  Very tricksy, CreateFile is.
        Synchronize = 0x100000,  // Can we wait on the handle?
        FullControl = 0x1F01FF,

        // These map to what Explorer sets, and are what most users want.
        // However, an ACL editor will also want to set the Synchronize
        // bit when allowing access, and exclude the synchronize bit when
        // denying access.
        Read = ReadData | ReadExtendedAttributes | ReadAttributes | ReadPermissions,
        ReadAndExecute = Read | ExecuteFile,
        Write = WriteData | AppendData | WriteExtendedAttributes | WriteAttributes,
        Modify = ReadAndExecute | Write | Delete,
    }


    public sealed class FileSystemAccessRule : AccessRule
    {
        #region Constructors

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

        #endregion

        #region Public properties

        public FileSystemRights FileSystemRights
        {
            get { return RightsFromAccessMask(base.AccessMask); }
        }

        #endregion

        #region Access mask to rights translation

        // ACL's on files have a SYNCHRONIZE bit, and CreateFile ALWAYS
        // asks for it.  So for allows, let's always include this bit,
        // and for denies, let's never include this bit unless we're denying
        // full control.  This is the right thing for users, even if it does
        // make the model look asymmetrical from a purist point of view.
        internal static int AccessMaskFromRights(FileSystemRights fileSystemRights, AccessControlType controlType)
        {
            if (fileSystemRights < (FileSystemRights)0 || fileSystemRights > FileSystemRights.FullControl)
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
        #endregion
    }


    public sealed class FileSystemAuditRule : AuditRule
    {
        #region Constructors

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

        #endregion

        #region Private methods

        private static int AccessMaskFromRights(FileSystemRights fileSystemRights)
        {
            if (fileSystemRights < (FileSystemRights)0 || fileSystemRights > FileSystemRights.FullControl)
                throw new ArgumentOutOfRangeException(nameof(fileSystemRights), SR.Format(SR.Argument_InvalidEnumValue, fileSystemRights, nameof(AccessControl.FileSystemRights)));

            return (int)fileSystemRights;
        }

        #endregion

        #region Public properties

        public FileSystemRights FileSystemRights
        {
            get { return FileSystemAccessRule.RightsFromAccessMask(base.AccessMask); }
        }
        #endregion
    }


    public abstract class FileSystemSecurity : NativeObjectSecurity
    {
        #region Member variables

        private const ResourceType s_ResourceType = ResourceType.FileObject;

        #endregion

        internal FileSystemSecurity(bool isContainer)
            : base(isContainer, s_ResourceType, _HandleErrorCode, isContainer)
        {
        }

        internal FileSystemSecurity(bool isContainer, string name, AccessControlSections includeSections, bool isDirectory)
            : base(isContainer, s_ResourceType, name, includeSections, _HandleErrorCode, isDirectory)
        {
        }

        internal FileSystemSecurity(bool isContainer, SafeFileHandle handle, AccessControlSections includeSections, bool isDirectory)
            : base(isContainer, s_ResourceType, handle, includeSections, _HandleErrorCode, isDirectory)
        {
        }

        private static Exception _HandleErrorCode(int errorCode, string name, SafeHandle handle, object context)
        {
            System.Exception exception = null;

            switch (errorCode)
            {
                case Interop.Errors.ERROR_INVALID_NAME:
                    exception = new ArgumentException(SR.Argument_InvalidName, nameof(name));
                    break;

                case Interop.Errors.ERROR_INVALID_HANDLE:
                    exception = new ArgumentException(SR.AccessControl_InvalidHandle);
                    break;

                case Interop.Errors.ERROR_FILE_NOT_FOUND:
                    if ((context != null) && (context is bool) && ((bool)context))
                    { // DirectorySecurity
                        if ((name != null) && (name.Length != 0))
                            exception = new DirectoryNotFoundException(name);
                        else
                            exception = new DirectoryNotFoundException();
                    }
                    else
                    {
                        if ((name != null) && (name.Length != 0))
                            exception = new FileNotFoundException(name);
                        else
                            exception = new FileNotFoundException();
                    }
                    break;

                default:
                    break;
            }

            return exception;
        }

        #region Factories

        public sealed override AccessRule AccessRuleFactory(
            IdentityReference identityReference,
            int accessMask,
            bool isInherited,
            InheritanceFlags inheritanceFlags,
            PropagationFlags propagationFlags,
            AccessControlType type)
        {
            return new FileSystemAccessRule(
                identityReference,
                accessMask,
                isInherited,
                inheritanceFlags,
                propagationFlags,
                type);
        }

        public sealed override AuditRule AuditRuleFactory(
            IdentityReference identityReference,
            int accessMask,
            bool isInherited,
            InheritanceFlags inheritanceFlags,
            PropagationFlags propagationFlags,
            AuditFlags flags)
        {
            return new FileSystemAuditRule(
                identityReference,
                accessMask,
                isInherited,
                inheritanceFlags,
                propagationFlags,
                flags);
        }

        #endregion

        #region Internal Methods

        internal AccessControlSections GetAccessControlSectionsFromChanges()
        {
            AccessControlSections persistRules = AccessControlSections.None;
            if (AccessRulesModified)
                persistRules = AccessControlSections.Access;
            if (AuditRulesModified)
                persistRules |= AccessControlSections.Audit;
            if (OwnerModified)
                persistRules |= AccessControlSections.Owner;
            if (GroupModified)
                persistRules |= AccessControlSections.Group;
            return persistRules;
        }

        internal void Persist(string fullPath)
        {
            WriteLock();

            try
            {
                AccessControlSections persistRules = GetAccessControlSectionsFromChanges();
                base.Persist(fullPath, persistRules);
                OwnerModified = GroupModified = AuditRulesModified = AccessRulesModified = false;
            }
            finally
            {
                WriteUnlock();
            }
        }

        internal void Persist(SafeFileHandle handle, string fullPath)
        {
            WriteLock();

            try
            {
                AccessControlSections persistRules = GetAccessControlSectionsFromChanges();
                base.Persist(handle, persistRules);
                OwnerModified = GroupModified = AuditRulesModified = AccessRulesModified = false;
            }
            finally
            {
                WriteUnlock();
            }
        }

        #endregion

        #region Public Methods

        public void AddAccessRule(FileSystemAccessRule rule)
        {
            base.AddAccessRule(rule);
            //PersistIfPossible();
        }

        public void SetAccessRule(FileSystemAccessRule rule)
        {
            base.SetAccessRule(rule);
        }

        public void ResetAccessRule(FileSystemAccessRule rule)
        {
            base.ResetAccessRule(rule);
        }

        public bool RemoveAccessRule(FileSystemAccessRule rule)
        {
            if (rule == null)
                throw new ArgumentNullException(nameof(rule));

            // If the rule to be removed matches what is there currently then
            // remove it unaltered. That is, don't mask off the Synchronize bit.
            // This is to avoid dangling synchronize bit

            AuthorizationRuleCollection rules = GetAccessRules(true, true, rule.IdentityReference.GetType());

            for (int i = 0; i < rules.Count; i++)
            {
                FileSystemAccessRule fsrule = rules[i] as FileSystemAccessRule;

                if ((fsrule != null) && (fsrule.FileSystemRights == rule.FileSystemRights)
                    && (fsrule.IdentityReference == rule.IdentityReference)
                    && (fsrule.AccessControlType == rule.AccessControlType))
                {
                    return base.RemoveAccessRule(rule);
                }
            }

            // Mask off the synchronize bit (that is automatically added for Allow)
            // before removing the ACL. The logic here should be same as Deny and hence
            // fake a call to AccessMaskFromRights as though the ACL is for Deny

            FileSystemAccessRule ruleNew = new FileSystemAccessRule(
                                                    rule.IdentityReference,
                                                    FileSystemAccessRule.AccessMaskFromRights(rule.FileSystemRights, AccessControlType.Deny),
                                                    rule.IsInherited,
                                                    rule.InheritanceFlags,
                                                    rule.PropagationFlags,
                                                    rule.AccessControlType);

            return base.RemoveAccessRule(ruleNew);
        }

        public void RemoveAccessRuleAll(FileSystemAccessRule rule)
        {
            // We don't need to worry about the synchronize bit here
            // AccessMask is ignored anyways in a RemoveAll call

            base.RemoveAccessRuleAll(rule);
        }

        public void RemoveAccessRuleSpecific(FileSystemAccessRule rule)
        {
            if (rule == null)
                throw new ArgumentNullException(nameof(rule));

            // If the rule to be removed matches what is there currently then
            // remove it unaltered. That is, don't mask off the Synchronize bit
            // This is to avoid dangling synchronize bit

            AuthorizationRuleCollection rules = GetAccessRules(true, true, rule.IdentityReference.GetType());

            for (int i = 0; i < rules.Count; i++)
            {
                FileSystemAccessRule fsrule = rules[i] as FileSystemAccessRule;

                if ((fsrule != null) && (fsrule.FileSystemRights == rule.FileSystemRights)
                    && (fsrule.IdentityReference == rule.IdentityReference)
                    && (fsrule.AccessControlType == rule.AccessControlType))
                {
                    base.RemoveAccessRuleSpecific(rule);
                    return;
                }
            }

            // Mask off the synchronize bit (that is automatically added for Allow)
            // before removing the ACL. The logic here should be same as Deny and hence
            // fake a call to AccessMaskFromRights as though the ACL is for Deny

            FileSystemAccessRule ruleNew = new FileSystemAccessRule(
                                                    rule.IdentityReference,
                                                    FileSystemAccessRule.AccessMaskFromRights(rule.FileSystemRights, AccessControlType.Deny),
                                                    rule.IsInherited,
                                                    rule.InheritanceFlags,
                                                    rule.PropagationFlags,
                                                    rule.AccessControlType);

            base.RemoveAccessRuleSpecific(ruleNew);
        }

        public void AddAuditRule(FileSystemAuditRule rule)
        {
            base.AddAuditRule(rule);
        }

        public void SetAuditRule(FileSystemAuditRule rule)
        {
            base.SetAuditRule(rule);
        }

        public bool RemoveAuditRule(FileSystemAuditRule rule)
        {
            return base.RemoveAuditRule(rule);
        }

        public void RemoveAuditRuleAll(FileSystemAuditRule rule)
        {
            base.RemoveAuditRuleAll(rule);
        }

        public void RemoveAuditRuleSpecific(FileSystemAuditRule rule)
        {
            base.RemoveAuditRuleSpecific(rule);
        }
        #endregion

        #region some overrides
        public override Type AccessRightType
        {
            get { return typeof(System.Security.AccessControl.FileSystemRights); }
        }

        public override Type AccessRuleType
        {
            get { return typeof(System.Security.AccessControl.FileSystemAccessRule); }
        }

        public override Type AuditRuleType
        {
            get { return typeof(System.Security.AccessControl.FileSystemAuditRule); }
        }
        #endregion
    }


    public sealed class FileSecurity : FileSystemSecurity
    {
        #region Constructors

        public FileSecurity()
            : base(false)
        {
        }

        public FileSecurity(string fileName, AccessControlSections includeSections)
            : base(false, fileName, includeSections, false)
        {
            string fullPath = Path.GetFullPath(fileName);
        }

        // Warning!  Be exceedingly careful with this constructor.  Do not make
        // it public.  We don't want to get into a situation where someone can
        // pass in the string foo.txt and a handle to bar.exe, and we do a
        // demand on the wrong file name.
        internal FileSecurity(SafeFileHandle handle, string fullPath, AccessControlSections includeSections)
            : base(false, handle, includeSections, false)
        {
        }
        #endregion
    }


    public sealed class DirectorySecurity : FileSystemSecurity
    {
        #region Constructors

        public DirectorySecurity()
            : base(true)
        {
        }

        public DirectorySecurity(string name, AccessControlSections includeSections)
            : base(true, name, includeSections, true)
        {
            string fullPath = Path.GetFullPath(name);
        }
        #endregion
    }
}

