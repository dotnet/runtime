// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Win32.SafeHandles;
using System.Security.AccessControl;
using System.Security.Principal;
using System.Runtime.InteropServices;

namespace System.IO.Pipes
{
    public class PipeSecurity : NativeObjectSecurity
    {
        public PipeSecurity()
            : base(false, ResourceType.KernelObject) { }

        // Used by PipeStream.GetAccessControl
        internal PipeSecurity(SafePipeHandle safeHandle, AccessControlSections includeSections)
            : base(false, ResourceType.KernelObject, safeHandle, includeSections) { }

        public void AddAccessRule(PipeAccessRule rule)
        {
            ArgumentNullException.ThrowIfNull(rule);

            base.AddAccessRule(rule);
        }

        public void SetAccessRule(PipeAccessRule rule)
        {
            ArgumentNullException.ThrowIfNull(rule);

            base.SetAccessRule(rule);
        }

        public void ResetAccessRule(PipeAccessRule rule)
        {
            ArgumentNullException.ThrowIfNull(rule);

            base.ResetAccessRule(rule);
        }

        public bool RemoveAccessRule(PipeAccessRule rule)
        {
            ArgumentNullException.ThrowIfNull(rule);

            // If the rule to be removed matches what is there currently then
            // remove it unaltered. That is, don't mask off the Synchronize bit.
            AuthorizationRuleCollection rules = GetAccessRules(true, true, rule.IdentityReference.GetType());

            for (int i = 0; i < rules.Count; i++)
            {
                PipeAccessRule? fsrule = rules[i] as PipeAccessRule;

                if ((fsrule != null) && (fsrule.PipeAccessRights == rule.PipeAccessRights)
                        && (fsrule.IdentityReference == rule.IdentityReference)
                        && (fsrule.AccessControlType == rule.AccessControlType))
                {
                    return base.RemoveAccessRule(rule);
                }
            }

            // It didn't exactly match any of the current rules so remove this way:
            // mask off the synchronize bit (that is automatically added for Allow)
            // before removing the ACL. The logic here should be same as Deny and hence
            // fake a call to AccessMaskFromRights as though the ACL is for Deny
            if (rule.PipeAccessRights != PipeAccessRights.FullControl)
            {
                return base.RemoveAccessRule(new PipeAccessRule(
                            rule.IdentityReference,
                            PipeAccessRule.AccessMaskFromRights(rule.PipeAccessRights, AccessControlType.Deny),
                            false,
                            rule.AccessControlType));
            }
            else
            {
                return base.RemoveAccessRule(rule);
            }
        }

        public void RemoveAccessRuleSpecific(PipeAccessRule rule)
        {
            ArgumentNullException.ThrowIfNull(rule);

            // If the rule to be removed matches what is there currently then
            // remove it unaltered. That is, don't mask off the Synchronize bit
            AuthorizationRuleCollection rules = GetAccessRules(true, true, rule.IdentityReference.GetType());

            for (int i = 0; i < rules.Count; i++)
            {
                PipeAccessRule? fsrule = rules[i] as PipeAccessRule;

                if ((fsrule != null) && (fsrule.PipeAccessRights == rule.PipeAccessRights)
                    && (fsrule.IdentityReference == rule.IdentityReference)
                    && (fsrule.AccessControlType == rule.AccessControlType))
                {
                    base.RemoveAccessRuleSpecific(rule);
                    return;
                }
            }

            // It wasn't an exact match so try masking the sychronize bit (that is
            // automatically added for Allow) before removing the ACL. The logic
            // here should be same as Deny and hence fake a call to
            // AccessMaskFromRights as though the ACL is for Deny
            if (rule.PipeAccessRights != PipeAccessRights.FullControl)
            {
                base.RemoveAccessRuleSpecific(new PipeAccessRule(rule.IdentityReference,
                    PipeAccessRule.AccessMaskFromRights(rule.PipeAccessRights, AccessControlType.Deny),
                    false,
                    rule.AccessControlType));
            }
            else
            {
                base.RemoveAccessRuleSpecific(rule);
            }
        }

        public void AddAuditRule(PipeAuditRule rule)
        {
            base.AddAuditRule(rule);
        }

        public void SetAuditRule(PipeAuditRule rule)
        {
            base.SetAuditRule(rule);
        }

        public bool RemoveAuditRule(PipeAuditRule rule)
        {
            return base.RemoveAuditRule(rule);
        }

        public void RemoveAuditRuleAll(PipeAuditRule rule)
        {
            base.RemoveAuditRuleAll(rule);
        }

        public void RemoveAuditRuleSpecific(PipeAuditRule rule)
        {
            base.RemoveAuditRuleSpecific(rule);
        }

        public override AccessRule AccessRuleFactory(IdentityReference identityReference,
                int accessMask, bool isInherited, InheritanceFlags inheritanceFlags,
                PropagationFlags propagationFlags, AccessControlType type)
        {
            // Throw if inheritance flags or propagation flags set. Have to include in signature
            // since this is an override
            if (inheritanceFlags != InheritanceFlags.None)
            {
                throw new ArgumentException(SR.Argument_NonContainerInvalidAnyFlag, nameof(inheritanceFlags));
            }
            if (propagationFlags != PropagationFlags.None)
            {
                throw new ArgumentException(SR.Argument_NonContainerInvalidAnyFlag, nameof(propagationFlags));
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
            AuditFlags flags)
        {

            // Throw if inheritance flags or propagation flags set. Have to include in signature
            // since this is an override
            if (inheritanceFlags != InheritanceFlags.None)
            {
                throw new ArgumentException(SR.Argument_NonContainerInvalidAnyFlag, nameof(inheritanceFlags));
            }
            if (propagationFlags != PropagationFlags.None)
            {
                throw new ArgumentException(SR.Argument_NonContainerInvalidAnyFlag, nameof(propagationFlags));
            }

            return new PipeAuditRule(
                identityReference,
                accessMask,
                isInherited,
                flags);
        }

        private AccessControlSections GetAccessControlSectionsFromChanges()
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

        protected internal void Persist(SafeHandle handle)
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

        protected internal void Persist(string name)
        {
            WriteLock();

            try
            {
                AccessControlSections persistRules = GetAccessControlSectionsFromChanges();
                base.Persist(name, persistRules);
                OwnerModified = GroupModified = AuditRulesModified = AccessRulesModified = false;
            }
            finally
            {
                WriteUnlock();
            }
        }

        public override Type AccessRightType
        {
            get
            {
                return typeof(PipeAccessRights);
            }
        }

        public override Type AccessRuleType
        {
            get
            {
                return typeof(PipeAccessRule);
            }
        }

        public override Type AuditRuleType
        {
            get
            {
                return typeof(PipeAuditRule);
            }
        }
    }
}
