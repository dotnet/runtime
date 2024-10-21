// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

/*============================================================
**
** Classes:  Common Object Security class
**
**
===========================================================*/

using System;
using System.Collections;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;
using System.Security.Principal;
using Microsoft.Win32;

namespace System.Security.AccessControl
{
    public abstract class CommonObjectSecurity : ObjectSecurity
    {
        #region Constructors

        protected CommonObjectSecurity(bool isContainer)
            : base(isContainer, false)
        {
        }

        internal CommonObjectSecurity(CommonSecurityDescriptor securityDescriptor)
            : base(securityDescriptor)
        {
        }

        #endregion

        #region Private Methods
        // Ported from NDP\clr\src\BCL\System\Security\Principal\SID.cs since we can't access System.Security.Principal.IdentityReference's internals
        private static bool IsValidTargetTypeStatic(Type targetType)
        {
            if (targetType == typeof(NTAccount))
            {
                return true;
            }
            else if (targetType == typeof(SecurityIdentifier))
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        private AuthorizationRuleCollection GetRules(bool access, bool includeExplicit, bool includeInherited, System.Type targetType)
        {
            ReadLock();

            try
            {
                AuthorizationRuleCollection result = new AuthorizationRuleCollection();

                if (!IsValidTargetTypeStatic(targetType))
                {
                    throw new ArgumentException(
                        SR.Arg_MustBeIdentityReferenceType,
                        nameof(targetType));
                }

                CommonAcl? acl = null;

                if (access)
                {
                    if ((_securityDescriptor.ControlFlags & ControlFlags.DiscretionaryAclPresent) != 0)
                    {
                        acl = _securityDescriptor.DiscretionaryAcl;
                    }
                }
                else // !access == audit
                {
                    if ((_securityDescriptor.ControlFlags & ControlFlags.SystemAclPresent) != 0)
                    {
                        acl = _securityDescriptor.SystemAcl;
                    }
                }

                if (acl == null)
                {
                    //
                    // The required ACL was not present; return an empty collection.
                    //
                    return result;
                }

                IdentityReferenceCollection? irTarget = null;

                if (targetType != typeof(SecurityIdentifier))
                {
                    IdentityReferenceCollection irSource = new IdentityReferenceCollection(acl.Count);

                    for (int i = 0; i < acl.Count; i++)
                    {
                        //
                        // Calling the indexer on a common ACL results in cloning,
                        // (which would not be the case if we were to use the internal RawAcl property)
                        // but also ensures that the resulting order of ACEs is proper
                        // However, this is a big price to pay - cloning all the ACEs just so that
                        // the canonical order could be ascertained just once.
                        // A better way would be to have an internal method that would canonicalize the ACL
                        // and call it once, then use the RawAcl.
                        //
                        CommonAce? ace = acl[i] as CommonAce;
                        if (AceNeedsTranslation(ace, access, includeExplicit, includeInherited))
                        {
                            irSource.Add(ace.SecurityIdentifier);
                        }
                    }

                    irTarget = irSource.Translate(targetType);
                }

                int targetIndex = 0;
                for (int i = 0; i < acl.Count; i++)
                {
                    //
                    // Calling the indexer on a common ACL results in cloning,
                    // (which would not be the case if we were to use the internal RawAcl property)
                    // but also ensures that the resulting order of ACEs is proper
                    // However, this is a big price to pay - cloning all the ACEs just so that
                    // the canonical order could be ascertained just once.
                    // A better way would be to have an internal method that would canonicalize the ACL
                    // and call it once, then use the RawAcl.
                    //

                    CommonAce? ace = acl[i] as CommonAce;
                    if (AceNeedsTranslation(ace, access, includeExplicit, includeInherited))
                    {
                        IdentityReference iref = (targetType == typeof(SecurityIdentifier)) ? ace.SecurityIdentifier : irTarget![targetIndex++];

                        if (access)
                        {
                            AccessControlType type;

                            if (ace.AceQualifier == AceQualifier.AccessAllowed)
                            {
                                type = AccessControlType.Allow;
                            }
                            else
                            {
                                type = AccessControlType.Deny;
                            }

                            result.AddRule(
                                AccessRuleFactory(
                                    iref,
                                    ace.AccessMask,
                                    ace.IsInherited,
                                    ace.InheritanceFlags,
                                    ace.PropagationFlags,
                                    type));
                        }
                        else
                        {
                            result.AddRule(
                                AuditRuleFactory(
                                    iref,
                                    ace.AccessMask,
                                    ace.IsInherited,
                                    ace.InheritanceFlags,
                                    ace.PropagationFlags,
                                    ace.AuditFlags));
                        }
                    }
                }

                return result;
            }
            finally
            {
                ReadUnlock();
            }
        }

        private static bool AceNeedsTranslation([NotNullWhen(true)] CommonAce? ace, bool isAccessAce, bool includeExplicit, bool includeInherited)
        {
            if (ace == null)
            {
                //
                // Only consider common ACEs
                //

                return false;
            }

            if (isAccessAce)
            {
                if (ace.AceQualifier != AceQualifier.AccessAllowed &&
                    ace.AceQualifier != AceQualifier.AccessDenied)
                {
                    return false;
                }
            }
            else
            {
                if (ace.AceQualifier != AceQualifier.SystemAudit)
                {
                    return false;
                }
            }

            if ((includeExplicit &&
                ((ace.AceFlags & AceFlags.Inherited) == 0)) ||
                (includeInherited &&
                ((ace.AceFlags & AceFlags.Inherited) != 0)))
            {
                return true;
            }

            return false;
        }

        //
        // Modifies the DACL
        //
        protected override bool ModifyAccess(AccessControlModification modification, AccessRule rule, out bool modified)
        {
            ArgumentNullException.ThrowIfNull(rule);

            WriteLock();
            try
            {
                bool result = true;

                if (_securityDescriptor.DiscretionaryAcl == null)
                {
                    if (modification == AccessControlModification.Remove ||
                        modification == AccessControlModification.RemoveAll ||
                        modification == AccessControlModification.RemoveSpecific)
                    {
                        modified = false;
                        return result;
                    }

                    _securityDescriptor.DiscretionaryAcl = new DiscretionaryAcl(IsContainer, IsDS, GenericAcl.AclRevision, 1);
                    _securityDescriptor.AddControlFlags(ControlFlags.DiscretionaryAclPresent);
                }

                SecurityIdentifier sid = (SecurityIdentifier)rule.IdentityReference.Translate(typeof(SecurityIdentifier));

                if (rule.AccessControlType == AccessControlType.Allow)
                {
                    switch (modification)
                    {
                        case AccessControlModification.Add:
                            _securityDescriptor.DiscretionaryAcl.AddAccess(AccessControlType.Allow, sid, rule.AccessMask, rule.InheritanceFlags, rule.PropagationFlags);
                            break;

                        case AccessControlModification.Set:
                            _securityDescriptor.DiscretionaryAcl.SetAccess(AccessControlType.Allow, sid, rule.AccessMask, rule.InheritanceFlags, rule.PropagationFlags);
                            break;

                        case AccessControlModification.Reset:
                            _securityDescriptor.DiscretionaryAcl.RemoveAccess(AccessControlType.Deny, sid, -1, InheritanceFlags.ContainerInherit | InheritanceFlags.ObjectInherit, 0);
                            _securityDescriptor.DiscretionaryAcl.SetAccess(AccessControlType.Allow, sid, rule.AccessMask, rule.InheritanceFlags, rule.PropagationFlags);
                            break;

                        case AccessControlModification.Remove:
                            result = _securityDescriptor.DiscretionaryAcl.RemoveAccess(AccessControlType.Allow, sid, rule.AccessMask, rule.InheritanceFlags, rule.PropagationFlags);
                            break;

                        case AccessControlModification.RemoveAll:
                            result = _securityDescriptor.DiscretionaryAcl.RemoveAccess(AccessControlType.Allow, sid, -1, InheritanceFlags.ContainerInherit | InheritanceFlags.ObjectInherit, 0);
                            if (result == false)
                            {
                                Debug.Fail("Invalid operation");
                                throw new InvalidOperationException();
                            }

                            break;

                        case AccessControlModification.RemoveSpecific:
                            _securityDescriptor.DiscretionaryAcl.RemoveAccessSpecific(AccessControlType.Allow, sid, rule.AccessMask, rule.InheritanceFlags, rule.PropagationFlags);
                            break;

                        default:
                            throw new ArgumentOutOfRangeException(
                                nameof(modification),
                                SR.ArgumentOutOfRange_Enum);
                    }
                }
                else if (rule.AccessControlType == AccessControlType.Deny)
                {
                    switch (modification)
                    {
                        case AccessControlModification.Add:
                            _securityDescriptor.DiscretionaryAcl.AddAccess(AccessControlType.Deny, sid, rule.AccessMask, rule.InheritanceFlags, rule.PropagationFlags);
                            break;

                        case AccessControlModification.Set:
                            _securityDescriptor.DiscretionaryAcl.SetAccess(AccessControlType.Deny, sid, rule.AccessMask, rule.InheritanceFlags, rule.PropagationFlags);
                            break;

                        case AccessControlModification.Reset:
                            _securityDescriptor.DiscretionaryAcl.RemoveAccess(AccessControlType.Allow, sid, -1, InheritanceFlags.ContainerInherit | InheritanceFlags.ObjectInherit, 0);
                            _securityDescriptor.DiscretionaryAcl.SetAccess(AccessControlType.Deny, sid, rule.AccessMask, rule.InheritanceFlags, rule.PropagationFlags);
                            break;

                        case AccessControlModification.Remove:
                            result = _securityDescriptor.DiscretionaryAcl.RemoveAccess(AccessControlType.Deny, sid, rule.AccessMask, rule.InheritanceFlags, rule.PropagationFlags);
                            break;

                        case AccessControlModification.RemoveAll:
                            result = _securityDescriptor.DiscretionaryAcl.RemoveAccess(AccessControlType.Deny, sid, -1, InheritanceFlags.ContainerInherit | InheritanceFlags.ObjectInherit, 0);
                            if (result == false)
                            {
                                Debug.Fail("Invalid operation");
                                throw new InvalidOperationException();
                            }

                            break;

                        case AccessControlModification.RemoveSpecific:
                            _securityDescriptor.DiscretionaryAcl.RemoveAccessSpecific(AccessControlType.Deny, sid, rule.AccessMask, rule.InheritanceFlags, rule.PropagationFlags);
                            break;

                        default:
                            throw new ArgumentOutOfRangeException(
                                nameof(modification),
                                SR.ArgumentOutOfRange_Enum);
                    }
                }
                else
                {
                    Debug.Fail("rule.AccessControlType unrecognized");
                    throw new ArgumentException(SR.Format(SR.Arg_EnumIllegalVal, (int)rule.AccessControlType), nameof(rule));
                }

                modified = result;
                AccessRulesModified |= modified;
                return result;
            }
            finally
            {
                WriteUnlock();
            }
        }

        //
        // Modifies the SACL
        //

        protected override bool ModifyAudit(AccessControlModification modification, AuditRule rule, out bool modified)
        {
            ArgumentNullException.ThrowIfNull(rule);

            WriteLock();
            try
            {
                bool result = true;

                if (_securityDescriptor.SystemAcl == null)
                {
                    if (modification == AccessControlModification.Remove ||
                        modification == AccessControlModification.RemoveAll ||
                        modification == AccessControlModification.RemoveSpecific)
                    {
                        modified = false;
                        return result;
                    }

                    _securityDescriptor.SystemAcl = new SystemAcl(IsContainer, IsDS, GenericAcl.AclRevision, 1);
                    _securityDescriptor.AddControlFlags(ControlFlags.SystemAclPresent);
                }

                SecurityIdentifier sid = (SecurityIdentifier)rule.IdentityReference.Translate(typeof(SecurityIdentifier));

                switch (modification)
                {
                    case AccessControlModification.Add:
                        _securityDescriptor.SystemAcl.AddAudit(rule.AuditFlags, sid, rule.AccessMask, rule.InheritanceFlags, rule.PropagationFlags);
                        break;

                    case AccessControlModification.Set:
                        _securityDescriptor.SystemAcl.SetAudit(rule.AuditFlags, sid, rule.AccessMask, rule.InheritanceFlags, rule.PropagationFlags);
                        break;

                    case AccessControlModification.Reset:
                        _securityDescriptor.SystemAcl.SetAudit(rule.AuditFlags, sid, rule.AccessMask, rule.InheritanceFlags, rule.PropagationFlags);
                        break;

                    case AccessControlModification.Remove:
                        result = _securityDescriptor.SystemAcl.RemoveAudit(rule.AuditFlags, sid, rule.AccessMask, rule.InheritanceFlags, rule.PropagationFlags);
                        break;

                    case AccessControlModification.RemoveAll:
                        result = _securityDescriptor.SystemAcl.RemoveAudit(AuditFlags.Failure | AuditFlags.Success, sid, -1, InheritanceFlags.ContainerInherit | InheritanceFlags.ObjectInherit, 0);
                        if (result == false)
                        {
                            throw new InvalidOperationException();
                        }

                        break;

                    case AccessControlModification.RemoveSpecific:
                        _securityDescriptor.SystemAcl.RemoveAuditSpecific(rule.AuditFlags, sid, rule.AccessMask, rule.InheritanceFlags, rule.PropagationFlags);
                        break;

                    default:
                        throw new ArgumentOutOfRangeException(
                            nameof(modification),
                            SR.ArgumentOutOfRange_Enum);
                }

                modified = result;
                AuditRulesModified |= modified;
                return result;
            }
            finally
            {
                WriteUnlock();
            }
        }

        #endregion

        #region Protected Methods

        #endregion

        #region Public Methods

        protected void AddAccessRule(AccessRule rule)
        {
            ArgumentNullException.ThrowIfNull(rule);

            WriteLock();

            try
            {
                ModifyAccess(AccessControlModification.Add, rule, out _);
            }
            finally
            {
                WriteUnlock();
            }
        }

        protected void SetAccessRule(AccessRule rule)
        {
            ArgumentNullException.ThrowIfNull(rule);

            WriteLock();

            try
            {
                ModifyAccess(AccessControlModification.Set, rule, out _);
            }
            finally
            {
                WriteUnlock();
            }
        }

        protected void ResetAccessRule(AccessRule rule)
        {
            ArgumentNullException.ThrowIfNull(rule);

            WriteLock();

            try
            {
                ModifyAccess(AccessControlModification.Reset, rule, out _);
            }
            finally
            {
                WriteUnlock();
            }

            return;
        }

        protected bool RemoveAccessRule(AccessRule rule)
        {
            ArgumentNullException.ThrowIfNull(rule);

            WriteLock();

            try
            {
                if (_securityDescriptor == null)
                {
                    return true;
                }

                return ModifyAccess(AccessControlModification.Remove, rule, out _);
            }
            finally
            {
                WriteUnlock();
            }
        }

        protected void RemoveAccessRuleAll(AccessRule rule)
        {
            ArgumentNullException.ThrowIfNull(rule);

            WriteLock();

            try
            {
                if (_securityDescriptor == null)
                {
                    return;
                }

                ModifyAccess(AccessControlModification.RemoveAll, rule, out _);
            }
            finally
            {
                WriteUnlock();
            }

            return;
        }

        protected void RemoveAccessRuleSpecific(AccessRule rule)
        {
            ArgumentNullException.ThrowIfNull(rule);

            WriteLock();

            try
            {
                if (_securityDescriptor == null)
                {
                    return;
                }

                ModifyAccess(AccessControlModification.RemoveSpecific, rule, out _);
            }
            finally
            {
                WriteUnlock();
            }
        }

        protected void AddAuditRule(AuditRule rule)
        {
            ArgumentNullException.ThrowIfNull(rule);

            WriteLock();

            try
            {
                ModifyAudit(AccessControlModification.Add, rule, out _);
            }
            finally
            {
                WriteUnlock();
            }
        }

        protected void SetAuditRule(AuditRule rule)
        {
            ArgumentNullException.ThrowIfNull(rule);

            WriteLock();

            try
            {
                ModifyAudit(AccessControlModification.Set, rule, out _);
            }
            finally
            {
                WriteUnlock();
            }
        }

        protected bool RemoveAuditRule(AuditRule rule)
        {
            ArgumentNullException.ThrowIfNull(rule);

            WriteLock();

            try
            {
                return ModifyAudit(AccessControlModification.Remove, rule, out _);
            }
            finally
            {
                WriteUnlock();
            }
        }

        protected void RemoveAuditRuleAll(AuditRule rule)
        {
            ArgumentNullException.ThrowIfNull(rule);

            WriteLock();

            try
            {
                ModifyAudit(AccessControlModification.RemoveAll, rule, out _);
            }
            finally
            {
                WriteUnlock();
            }
        }

        protected void RemoveAuditRuleSpecific(AuditRule rule)
        {
            ArgumentNullException.ThrowIfNull(rule);

            WriteLock();

            try
            {
                ModifyAudit(AccessControlModification.RemoveSpecific, rule, out _);
            }
            finally
            {
                WriteUnlock();
            }
        }

        public AuthorizationRuleCollection GetAccessRules(bool includeExplicit, bool includeInherited, System.Type targetType)
        {
            return GetRules(true, includeExplicit, includeInherited, targetType);
        }

        public AuthorizationRuleCollection GetAuditRules(bool includeExplicit, bool includeInherited, System.Type targetType)
        {
            return GetRules(false, includeExplicit, includeInherited, targetType);
        }
        #endregion
    }
}
