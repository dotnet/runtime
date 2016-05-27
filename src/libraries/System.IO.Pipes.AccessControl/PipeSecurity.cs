// ==++==
// 
//   Copyright (c) Microsoft Corporation.  All rights reserved.
// 
// ==--==
/*============================================================
**
** Class:  PipeSecurity
**
**
** Purpose: Managed ACL wrapper for Pipes.
**
**
===========================================================*/

using System;
using System.Collections;
using System.Security.AccessControl;
using System.Security.Permissions;
using System.Security.Principal;
using Microsoft.Win32;
using Microsoft.Win32.SafeHandles;
using System.Runtime.InteropServices;
using System.IO;
using System.Runtime.Versioning;

namespace System.IO.Pipes {

    [Flags]
    public enum PipeAccessRights {
        // No None field - An ACE with the value 0 cannot grant nor deny.
        ReadData = 0x000001,
        WriteData = 0x000002,

        // Not that all client named pipes require ReadAttributes access even if the user does not specify it.
        // (This is because CreateFile slaps on the requirement before calling NTCreateFile (at least in WinXP SP2)).
        ReadAttributes = 0x000080,
        WriteAttributes = 0x000100,

        // These aren't really needed since there is no operation that requires this access, but they are left here
        // so that people can specify ACLs that others can open by specifying a PipeDirection rather than a
        // PipeAccessRights (PipeDirection.In/Out maps to GENERIC_READ/WRITE access).
        ReadExtendedAttributes = 0x000008,
        WriteExtendedAttributes = 0x000010,

        CreateNewInstance = 0x000004, // AppendData

        // Again, this is not needed but it should be here so that our FullControl matches windows.
        Delete = 0x010000,

        ReadPermissions = 0x020000,
        ChangePermissions = 0x040000,
        TakeOwnership = 0x080000,
        Synchronize = 0x100000,

        FullControl = ReadData | WriteData | ReadAttributes | ReadExtendedAttributes |
                                       WriteAttributes | WriteExtendedAttributes | CreateNewInstance |
                                       Delete | ReadPermissions | ChangePermissions | TakeOwnership |
                                       Synchronize,

        Read = ReadData | ReadAttributes | ReadExtendedAttributes | ReadPermissions,
        Write = WriteData | WriteAttributes | WriteExtendedAttributes, // | CreateNewInstance, For security, I really don't this CreateNewInstance belongs here.   
        ReadWrite = Read | Write,

        // These are somewhat similar to what you get if you use PipeDirection:
        //In                           = ReadData | ReadAttributes | ReadExtendedAttributes | ReadPermissions, 
        //Out                          = WriteData | WriteAttributes | WriteExtendedAttributes | ChangePermissions | CreateNewInstance | ReadAttributes, // NOTE: Not sure if ReadAttributes should really be here 
        //InOut                        = In | Out,

        AccessSystemSecurity = 0x01000000, // Allow changes to SACL. 
    }


    [System.Security.Permissions.HostProtection(MayLeakOnAbort = true)]
    public sealed class PipeAccessRule : AccessRule {
        #region Constructors

        //
        // Constructor for creating access rules for pipe objects
        //

        public PipeAccessRule(
            String identity,
            PipeAccessRights rights,
            AccessControlType type)
            : this(
                new NTAccount(identity),
                AccessMaskFromRights(rights, type),
                false,
                type) {
        }

        public PipeAccessRule(
            IdentityReference identity,
            PipeAccessRights rights,
            AccessControlType type)
            : this(
                identity,
                AccessMaskFromRights(rights, type),
                false,
                type) {
        }

        //
        // Internal constructor to be called by public constructors
        // and the access rights factory methods
        //
        internal PipeAccessRule(
            IdentityReference identity,
            int accessMask,
            bool isInherited,
            AccessControlType type)
            : base(
                identity,
                accessMask,
                isInherited,
                InheritanceFlags.None,  // these do not apply to pipes
                PropagationFlags.None,  // these do not apply to pipes
                type) {
        }
        
        #endregion

        #region Public properties

        public PipeAccessRights PipeAccessRights {
            get { 
                return RightsFromAccessMask(base.AccessMask); 
            }
        }

        #endregion

        #region Access mask to rights translation

        // ACL's on pipes have a SYNCHRONIZE bit, and CreateFile ALWAYS asks for it.  
        // So for allows, let's always include this bit, and for denies, let's never
        // include this bit unless we're denying full control.  This is the right 
        // thing for users, even if it does make the model look asymmetrical from a
        // purist point of view.
        internal static int AccessMaskFromRights(PipeAccessRights rights, AccessControlType controlType) {
            if (rights < (PipeAccessRights)0 || rights > (PipeAccessRights.FullControl | PipeAccessRights.AccessSystemSecurity))
                throw new ArgumentOutOfRangeException("rights", SR.GetString(SR.ArgumentOutOfRange_NeedValidPipeAccessRights));

            if (controlType == AccessControlType.Allow) {
                rights |= PipeAccessRights.Synchronize;
            }
            else if (controlType == AccessControlType.Deny) {
                if (rights != PipeAccessRights.FullControl) {
                    rights &= ~PipeAccessRights.Synchronize;
                }
            }

            return (int)rights;
        }

        internal static PipeAccessRights RightsFromAccessMask(int accessMask) {
            return (PipeAccessRights)accessMask;
        }

        #endregion
    }


    [System.Security.Permissions.HostProtection(MayLeakOnAbort = true)]
    public sealed class PipeAuditRule : AuditRule {
        #region Constructors

        public PipeAuditRule(
            IdentityReference identity,
            PipeAccessRights rights,
            AuditFlags flags)
            : this(
                identity,
                AccessMaskFromRights(rights),
                false,
                flags) {
        }

        public PipeAuditRule(
            String identity,
            PipeAccessRights rights,
            AuditFlags flags)
            : this(
                new NTAccount(identity),
                AccessMaskFromRights(rights),
                false,
                flags) {
        }

        internal PipeAuditRule(
            IdentityReference identity,
            int accessMask,
            bool isInherited,
            AuditFlags flags)
            : base(
                identity,
                accessMask,
                isInherited,
                InheritanceFlags.None,
                PropagationFlags.None,
                flags) {
        }
        #endregion

        #region Private methods

        private static int AccessMaskFromRights(PipeAccessRights rights) {
            if (rights < (PipeAccessRights)0 || rights > (PipeAccessRights.FullControl | PipeAccessRights.AccessSystemSecurity)) {
                throw new ArgumentOutOfRangeException("rights", SR.GetString(SR.ArgumentOutOfRange_NeedValidPipeAccessRights));
            }

            return (int)rights;
        }

        #endregion

        #region Public properties

        public PipeAccessRights PipeAccessRights {
            get { 
                return PipeAccessRule.RightsFromAccessMask(base.AccessMask); 
            }
        }

        #endregion
    }


    [System.Security.Permissions.HostProtection(MayLeakOnAbort = true)]
    public class PipeSecurity : NativeObjectSecurity {
        public PipeSecurity()
            : base(false, ResourceType.KernelObject) { }

        // Used by PipeStream.GetAccessControl
        [System.Security.SecuritySafeCritical]
        internal PipeSecurity(SafePipeHandle safeHandle, AccessControlSections includeSections)
            : base(false, ResourceType.KernelObject, safeHandle, includeSections) { }

        public void AddAccessRule(PipeAccessRule rule) {
            if (rule == null)
                throw new ArgumentNullException("rule");

            base.AddAccessRule(rule);
        }

        public void SetAccessRule(PipeAccessRule rule) {
            if (rule == null)
                throw new ArgumentNullException("rule");

            base.SetAccessRule(rule);
        }

        public void ResetAccessRule(PipeAccessRule rule) {
            if (rule == null)
                throw new ArgumentNullException("rule");

            base.ResetAccessRule(rule);
        }

        public bool RemoveAccessRule(PipeAccessRule rule) {
            if (rule == null) {
                throw new ArgumentNullException("rule");
            }

            // If the rule to be removed matches what is there currently then 
            // remove it unaltered. That is, don't mask off the Synchronize bit.
            AuthorizationRuleCollection rules = GetAccessRules(true, true,
                    rule.IdentityReference.GetType());

            for (int i = 0; i < rules.Count; i++) {
                PipeAccessRule fsrule = rules[i] as PipeAccessRule;

                if ((fsrule != null) && (fsrule.PipeAccessRights == rule.PipeAccessRights)
                        && (fsrule.IdentityReference == rule.IdentityReference)
                        && (fsrule.AccessControlType == rule.AccessControlType)) {
                    return base.RemoveAccessRule(rule);
                }
            }

            // It didn't exactly match any of the current rules so remove this way:
            // mask off the synchronize bit (that is automatically added for Allow)
            // before removing the ACL. The logic here should be same as Deny and hence
            // fake a call to AccessMaskFromRights as though the ACL is for Deny
            if (rule.PipeAccessRights != PipeAccessRights.FullControl) {
                return base.RemoveAccessRule(new PipeAccessRule(
                            rule.IdentityReference,
                            PipeAccessRule.AccessMaskFromRights(rule.PipeAccessRights, AccessControlType.Deny),
                            false,
                            rule.AccessControlType));
            }
            else {
                return base.RemoveAccessRule(rule);
            }
        }

        public void RemoveAccessRuleSpecific(PipeAccessRule rule) {
            if (rule == null) {
                throw new ArgumentNullException("rule");
            }

            // If the rule to be removed matches what is there currently then 
            // remove it unaltered. That is, don't mask off the Synchronize bit
            AuthorizationRuleCollection rules = GetAccessRules(true, true,
                    rule.IdentityReference.GetType());

            for (int i = 0; i < rules.Count; i++) {
                PipeAccessRule fsrule = rules[i] as PipeAccessRule;

                if ((fsrule != null) && (fsrule.PipeAccessRights == rule.PipeAccessRights)
                    && (fsrule.IdentityReference == rule.IdentityReference)
                    && (fsrule.AccessControlType == rule.AccessControlType)) {
                    base.RemoveAccessRuleSpecific(rule);
                    return;
                }
            }

            // It wasn't an exact match so try masking the sychronize bit (that is 
            // automatically added for Allow) before removing the ACL. The logic 
            // here should be same as Deny and hence fake a call to 
            // AccessMaskFromRights as though the ACL is for Deny
            if (rule.PipeAccessRights != PipeAccessRights.FullControl) {
                base.RemoveAccessRuleSpecific(new PipeAccessRule(rule.IdentityReference,
                    PipeAccessRule.AccessMaskFromRights(rule.PipeAccessRights, AccessControlType.Deny),
                    false,
                    rule.AccessControlType));
            }
            else {
                base.RemoveAccessRuleSpecific(rule);
            }
        }

        public void AddAuditRule(PipeAuditRule rule) {
            base.AddAuditRule(rule);
        }

        public void SetAuditRule(PipeAuditRule rule) {
            base.SetAuditRule(rule);
        }

        public bool RemoveAuditRule(PipeAuditRule rule) {
            return base.RemoveAuditRule(rule);
        }

        public void RemoveAuditRuleAll(PipeAuditRule rule) {
            base.RemoveAuditRuleAll(rule);
        }

        public void RemoveAuditRuleSpecific(PipeAuditRule rule) {
            base.RemoveAuditRuleSpecific(rule);
        }

        public override AccessRule AccessRuleFactory(IdentityReference identityReference,
                int accessMask, bool isInherited, InheritanceFlags inheritanceFlags,
                PropagationFlags propagationFlags, AccessControlType type) {
            // Throw if inheritance flags or propagation flags set. Have to include in signature
            // since this is an override
            if (inheritanceFlags != InheritanceFlags.None) {
                throw new ArgumentException(SR.GetString(SR.Argument_NonContainerInvalidAnyFlag), "inheritanceFlags");
            }
            if (propagationFlags != PropagationFlags.None) {
                throw new ArgumentException(SR.GetString(SR.Argument_NonContainerInvalidAnyFlag),  "propagationFlags");
            }

            return new PipeAccessRule(
                identityReference,
                accessMask,
                isInherited,
                type);

        }


        public sealed override AuditRule AuditRuleFactory(
            IdentityReference identityReference,
            int accessMask,
            bool isInherited,
            InheritanceFlags inheritanceFlags,
            PropagationFlags propagationFlags,
            AuditFlags flags) {

            // Throw if inheritance flags or propagation flags set. Have to include in signature
            // since this is an override
            if (inheritanceFlags != InheritanceFlags.None) {
                throw new ArgumentException(SR.GetString(SR.Argument_NonContainerInvalidAnyFlag), "inheritanceFlags");
            }
            if (propagationFlags != PropagationFlags.None) {
                throw new ArgumentException(SR.GetString(SR.Argument_NonContainerInvalidAnyFlag), "propagationFlags");
            }

            return new PipeAuditRule(
                identityReference,
                accessMask,
                isInherited,
                flags);
        }

        #region Private Methods

        private AccessControlSections GetAccessControlSectionsFromChanges() {
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

        #endregion
        #region Protected Methods

        // Use this in your own Persist after you have demanded any appropriate CAS permissions.
        // Note that you will want your version to be internal and use a specialized Safe Handle. 
        [System.Security.SecurityCritical]
        [SecurityPermission(SecurityAction.Assert, UnmanagedCode = true)]
        protected internal void Persist(SafeHandle handle) {
            WriteLock();

            try {
                AccessControlSections persistRules = GetAccessControlSectionsFromChanges();
                base.Persist(handle, persistRules);
                OwnerModified = GroupModified = AuditRulesModified = AccessRulesModified = false;
            }
            finally {
                WriteUnlock();
            }
        }

        // Use this in your own Persist after you have demanded any appropriate CAS permissions.
        // Note that you will want your version to be internal. 
        [System.Security.SecurityCritical]
        [SecurityPermission(SecurityAction.Assert, UnmanagedCode = true)]
        protected internal void Persist(String name) {
            WriteLock();

            try {
                AccessControlSections persistRules = GetAccessControlSectionsFromChanges();
                base.Persist(name, persistRules);
                OwnerModified = GroupModified = AuditRulesModified = AccessRulesModified = false;
            }
            finally {
                WriteUnlock();
            }
        }

        #endregion

        #region some overrides
        public override Type AccessRightType {
            get { 
                return typeof(PipeAccessRights); 
            }
        }

        public override Type AccessRuleType {
            get { 
                return typeof(PipeAccessRule); 
            }
        }

        public override Type AuditRuleType {
            get { 
                return typeof(PipeAuditRule); 
            }
        }
        #endregion
    }
}

