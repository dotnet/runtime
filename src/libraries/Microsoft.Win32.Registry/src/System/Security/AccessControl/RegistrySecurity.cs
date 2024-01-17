// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.IO;
using System.Runtime.InteropServices;
using System.Security.Principal;
using Microsoft.Win32.SafeHandles;

namespace System.Security.AccessControl
{
    public sealed class RegistryAccessRule : AccessRule
    {
        // Constructor for creating access rules for registry objects
        public RegistryAccessRule(IdentityReference identity, RegistryRights registryRights, AccessControlType type)
            : this(identity, (int)registryRights, false, InheritanceFlags.None, PropagationFlags.None, type)
        {
        }

        public RegistryAccessRule(string identity, RegistryRights registryRights, AccessControlType type)
            : this(new NTAccount(identity), (int)registryRights, false, InheritanceFlags.None, PropagationFlags.None, type)
        {
        }

        public RegistryAccessRule(IdentityReference identity, RegistryRights registryRights, InheritanceFlags inheritanceFlags, PropagationFlags propagationFlags, AccessControlType type)
            : this(identity, (int)registryRights, false, inheritanceFlags, propagationFlags, type)
        {
        }

        public RegistryAccessRule(string identity, RegistryRights registryRights, InheritanceFlags inheritanceFlags, PropagationFlags propagationFlags, AccessControlType type)
            : this(new NTAccount(identity), (int)registryRights, false, inheritanceFlags, propagationFlags, type)
        {
        }

        //
        // Internal constructor to be called by public constructors
        // and the access rule factory methods of {File|Folder}Security
        //
        internal RegistryAccessRule(
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

        public RegistryRights RegistryRights
        {
            get { return (RegistryRights)AccessMask; }
        }
    }


    public sealed class RegistryAuditRule : AuditRule
    {
        public RegistryAuditRule(IdentityReference identity, RegistryRights registryRights, InheritanceFlags inheritanceFlags, PropagationFlags propagationFlags, AuditFlags flags)
            : this(identity, (int)registryRights, false, inheritanceFlags, propagationFlags, flags)
        {
        }

        public RegistryAuditRule(string identity, RegistryRights registryRights, InheritanceFlags inheritanceFlags, PropagationFlags propagationFlags, AuditFlags flags)
            : this(new NTAccount(identity), (int)registryRights, false, inheritanceFlags, propagationFlags, flags)
        {
        }

        internal RegistryAuditRule(IdentityReference identity, int accessMask, bool isInherited, InheritanceFlags inheritanceFlags, PropagationFlags propagationFlags, AuditFlags flags)
            : base(identity, accessMask, isInherited, inheritanceFlags, propagationFlags, flags)
        {
        }

        public RegistryRights RegistryRights
        {
            get { return (RegistryRights)AccessMask; }
        }
    }


    public sealed partial class RegistrySecurity : NativeObjectSecurity
    {
        public RegistrySecurity()
            : base(true, ResourceType.RegistryKey)
        {
        }

        internal RegistrySecurity(SafeRegistryHandle hKey, AccessControlSections includeSections)
            : base(true, ResourceType.RegistryKey, hKey, includeSections, _HandleErrorCode, null)
        {
        }

        private static Exception? _HandleErrorCode(int errorCode, string? name, SafeHandle? handle, object? context)
        {
            Exception? exception = null;

            switch (errorCode)
            {
                case Interop.Errors.ERROR_FILE_NOT_FOUND:
                    exception = new IOException(SR.Format(SR.Arg_RegKeyNotFound, errorCode));
                    break;

                case Interop.Errors.ERROR_INVALID_NAME:
                    exception = new ArgumentException(SR.Arg_RegInvalidKeyName, nameof(name));
                    break;

                case Interop.Errors.ERROR_INVALID_HANDLE:
                    exception = new ArgumentException(SR.AccessControl_InvalidHandle);
                    break;

                default:
                    break;
            }

            return exception;
        }

        public override AccessRule AccessRuleFactory(IdentityReference identityReference, int accessMask, bool isInherited, InheritanceFlags inheritanceFlags, PropagationFlags propagationFlags, AccessControlType type)
        {
            return new RegistryAccessRule(identityReference, accessMask, isInherited, inheritanceFlags, propagationFlags, type);
        }

        public override AuditRule AuditRuleFactory(IdentityReference identityReference, int accessMask, bool isInherited, InheritanceFlags inheritanceFlags, PropagationFlags propagationFlags, AuditFlags flags)
        {
            return new RegistryAuditRule(identityReference, accessMask, isInherited, inheritanceFlags, propagationFlags, flags);
        }

        internal AccessControlSections GetAccessControlSectionsFromChanges()
        {
            AccessControlSections persistRules = AccessControlSections.None;
            if (AccessRulesModified)
            {
                persistRules = AccessControlSections.Access;
            }

            if (AuditRulesModified)
            {
                persistRules |= AccessControlSections.Audit;
            }

            if (OwnerModified)
            {
                persistRules |= AccessControlSections.Owner;
            }

            if (GroupModified)
            {
                persistRules |= AccessControlSections.Group;
            }

            return persistRules;
        }

        internal void Persist(SafeRegistryHandle hKey)
        {
            WriteLock();

            try
            {
                AccessControlSections persistRules = GetAccessControlSectionsFromChanges();
                if (persistRules == AccessControlSections.None)
                {
                    return;  // Don't need to persist anything.
                }

                Persist(hKey, persistRules);
                OwnerModified = GroupModified = AuditRulesModified = AccessRulesModified = false;
            }
            finally
            {
                WriteUnlock();
            }
        }

        public void AddAccessRule(RegistryAccessRule rule)
        {
            base.AddAccessRule(rule);
        }

        public void SetAccessRule(RegistryAccessRule rule)
        {
            base.SetAccessRule(rule);
        }

        public void ResetAccessRule(RegistryAccessRule rule)
        {
            base.ResetAccessRule(rule);
        }

        public bool RemoveAccessRule(RegistryAccessRule rule)
        {
            return base.RemoveAccessRule(rule);
        }

        public void RemoveAccessRuleAll(RegistryAccessRule rule)
        {
            base.RemoveAccessRuleAll(rule);
        }

        public void RemoveAccessRuleSpecific(RegistryAccessRule rule)
        {
            base.RemoveAccessRuleSpecific(rule);
        }

        public void AddAuditRule(RegistryAuditRule rule)
        {
            base.AddAuditRule(rule);
        }

        public void SetAuditRule(RegistryAuditRule rule)
        {
            base.SetAuditRule(rule);
        }

        public bool RemoveAuditRule(RegistryAuditRule rule)
        {
            return base.RemoveAuditRule(rule);
        }

        public void RemoveAuditRuleAll(RegistryAuditRule rule)
        {
            base.RemoveAuditRuleAll(rule);
        }

        public void RemoveAuditRuleSpecific(RegistryAuditRule rule)
        {
            base.RemoveAuditRuleSpecific(rule);
        }

        public override Type AccessRightType
        {
            get { return typeof(RegistryRights); }
        }

        public override Type AccessRuleType
        {
            get { return typeof(RegistryAccessRule); }
        }

        public override Type AuditRuleType
        {
            get { return typeof(RegistryAuditRule); }
        }
    }
}
