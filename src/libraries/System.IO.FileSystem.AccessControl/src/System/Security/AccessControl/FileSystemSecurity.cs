// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.IO;
using System.Runtime.InteropServices;
using System.Security.Principal;
using Microsoft.Win32.SafeHandles;

namespace System.Security.AccessControl
{
    public abstract class FileSystemSecurity : NativeObjectSecurity
    {
        private const ResourceType s_ResourceType = ResourceType.FileObject;

        internal FileSystemSecurity(bool isContainer)
            : base(isContainer, s_ResourceType, _HandleErrorCode, isContainer)
        {
        }

        internal FileSystemSecurity(bool isContainer, string name, AccessControlSections includeSections, bool isDirectory)
            : base(isContainer, s_ResourceType, PathInternal.EnsureExtendedPrefixIfNeeded(Path.GetFullPath(name)), includeSections, _HandleErrorCode, isDirectory)
        {
        }

        internal FileSystemSecurity(bool isContainer, SafeFileHandle? handle, AccessControlSections includeSections, bool isDirectory)
            : base(isContainer, s_ResourceType, handle, includeSections, _HandleErrorCode, isDirectory)
        {
        }

        private static Exception? _HandleErrorCode(int errorCode, string? name, SafeHandle? handle, object? context)
        {
            Exception? exception = null;

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
                    {
                        // DirectorySecurity
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
                base.Persist(PathInternal.EnsureExtendedPrefixIfNeeded(fullPath), persistRules);
                OwnerModified = GroupModified = AuditRulesModified = AccessRulesModified = false;
            }
            finally
            {
                WriteUnlock();
            }
        }

        internal void Persist(SafeFileHandle handle, string _ /*fullPath*/)
        {
            WriteLock();

            try
            {
                AccessControlSections persistRules = GetAccessControlSectionsFromChanges();
                Persist(handle, persistRules);
                OwnerModified = GroupModified = AuditRulesModified = AccessRulesModified = false;
            }
            finally
            {
                WriteUnlock();
            }
        }

        public void AddAccessRule(FileSystemAccessRule rule)
        {
            base.AddAccessRule(rule);
            // PersistIfPossible();
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
            ArgumentNullException.ThrowIfNull(rule);

            // If the rule to be removed matches what is there currently then
            // remove it unaltered. That is, don't mask off the Synchronize bit.
            // This is to avoid dangling synchronize bit

            AuthorizationRuleCollection rules = GetAccessRules(true, true, rule.IdentityReference.GetType());

            for (int i = 0; i < rules.Count; i++)
            {
                if ((rules[i] is FileSystemAccessRule fsrule) && (fsrule.FileSystemRights == rule.FileSystemRights)
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
            ArgumentNullException.ThrowIfNull(rule);

            // If the rule to be removed matches what is there currently then
            // remove it unaltered. That is, don't mask off the Synchronize bit
            // This is to avoid dangling synchronize bit

            AuthorizationRuleCollection rules = GetAccessRules(true, true, rule.IdentityReference.GetType());

            for (int i = 0; i < rules.Count; i++)
            {
                if ((rules[i] is FileSystemAccessRule fsrule) && (fsrule.FileSystemRights == rule.FileSystemRights)
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

        public override Type AccessRightType
        {
            get { return typeof(FileSystemRights); }
        }

        public override Type AccessRuleType
        {
            get { return typeof(FileSystemAccessRule); }
        }

        public override Type AuditRuleType
        {
            get { return typeof(FileSystemAuditRule); }
        }
    }
}
