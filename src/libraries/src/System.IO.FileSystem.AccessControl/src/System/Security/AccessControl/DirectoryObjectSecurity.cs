// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//
// Managed ACL wrapper for directories


using Microsoft.Win32.SafeHandles;
using Microsoft.Win32;
using System.Collections;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using System.Security.AccessControl;
using System.Security.Principal;
using System;

namespace System.Security.AccessControl
{
    public abstract class DirectoryObjectSecurity : ObjectSecurity
    {
        #region Private Members

        internal CommonSecurityDescriptor _securityDescriptor;

        #endregion

        #region Constructors

        protected DirectoryObjectSecurity()
            : base(true, true)
        {
            return;
        }

        protected DirectoryObjectSecurity(CommonSecurityDescriptor securityDescriptor)
            : base(securityDescriptor)
        {
            if (securityDescriptor == null)
            {
                throw new ArgumentNullException(nameof(securityDescriptor));
            }

            _securityDescriptor = securityDescriptor;
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
                    throw new ArgumentException(SR.Arg_MustBeIdentityReferenceType, nameof(targetType));
                }

                CommonAcl acl = null;

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

                IdentityReferenceCollection irTarget = null;

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
                        QualifiedAce ace = acl[i] as QualifiedAce;

                        if (ace == null)
                        {
                            //
                            // Only consider qualified ACEs
                            //
                            continue;
                        }

                        if (ace.IsCallback == true)
                        {
                            //
                            // Ignore callback ACEs
                            //
                            continue;
                        }

                        if (access)
                        {
                            if (ace.AceQualifier != AceQualifier.AccessAllowed && ace.AceQualifier != AceQualifier.AccessDenied)
                            {
                                continue;
                            }
                        }
                        else
                        {
                            if (ace.AceQualifier != AceQualifier.SystemAudit)
                            {
                                continue;
                            }
                        }

                        irSource.Add(ace.SecurityIdentifier);
                    }

                    irTarget = irSource.Translate(targetType);
                }

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
                    QualifiedAce ace = acl[i] as CommonAce;

                    if (ace == null)
                    {
                        ace = acl[i] as ObjectAce;
                        if (ace == null)
                        {
                            //
                            // Only consider common or object ACEs
                            //
                            continue;
                        }
                    }

                    if (ace.IsCallback == true)
                    {
                        //
                        // Ignore callback ACEs
                        //
                        continue;
                    }

                    if (access)
                    {
                        if (ace.AceQualifier != AceQualifier.AccessAllowed && ace.AceQualifier != AceQualifier.AccessDenied)
                        {
                            continue;
                        }
                    }
                    else
                    {
                        if (ace.AceQualifier != AceQualifier.SystemAudit)
                        {
                            continue;
                        }
                    }

                    if ((includeExplicit && ((ace.AceFlags & AceFlags.Inherited) == 0)) || (includeInherited && ((ace.AceFlags & AceFlags.Inherited) != 0)))
                    {
                        IdentityReference iref = (targetType == typeof(SecurityIdentifier)) ? ace.SecurityIdentifier : irTarget[i];

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

                            if (ace is ObjectAce)
                            {
                                ObjectAce objectAce = ace as ObjectAce;

                                result.AddRule(AccessRuleFactory(iref, objectAce.AccessMask, objectAce.IsInherited, objectAce.InheritanceFlags, objectAce.PropagationFlags, type, objectAce.ObjectAceType, objectAce.InheritedObjectAceType));
                            }
                            else
                            {
                                CommonAce commonAce = ace as CommonAce;

                                if (commonAce == null)
                                {
                                    continue;
                                }

                                result.AddRule(AccessRuleFactory(iref, commonAce.AccessMask, commonAce.IsInherited, commonAce.InheritanceFlags, commonAce.PropagationFlags, type));
                            }
                        }
                        else
                        {
                            if (ace is ObjectAce)
                            {
                                ObjectAce objectAce = ace as ObjectAce;

                                result.AddRule(AuditRuleFactory(iref, objectAce.AccessMask, objectAce.IsInherited, objectAce.InheritanceFlags, objectAce.PropagationFlags, objectAce.AuditFlags, objectAce.ObjectAceType, objectAce.InheritedObjectAceType));
                            }
                            else
                            {
                                CommonAce commonAce = ace as CommonAce;

                                if (commonAce == null)
                                {
                                    continue;
                                }

                                result.AddRule(AuditRuleFactory(iref, commonAce.AccessMask, commonAce.IsInherited, commonAce.InheritanceFlags, commonAce.PropagationFlags, commonAce.AuditFlags));
                            }
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

        //
        // Modifies the DACL
        //
        private bool ModifyAccess(AccessControlModification modification, ObjectAccessRule rule, out bool modified)
        {
            bool result = true;

            if (_securityDescriptor.DiscretionaryAcl == null)
            {
                if (modification == AccessControlModification.Remove || modification == AccessControlModification.RemoveAll || modification == AccessControlModification.RemoveSpecific)
                {
                    modified = false;
                    return result;
                }

                //_securityDescriptor.DiscretionaryAcl = new DiscretionaryAcl(IsContainer, IsDS, GenericAcl.AclRevisionDS, 1);
                //_securityDescriptor.AddControlFlags(ControlFlags.DiscretionaryAclPresent);
                _securityDescriptor.AddDiscretionaryAcl(GenericAcl.AclRevisionDS, 1);
            }
            else if ((modification == AccessControlModification.Add || modification == AccessControlModification.Set || modification == AccessControlModification.Reset) &&
                        (rule.ObjectFlags != ObjectAceFlags.None))
            {
                //
                // This will result in an object ace being added to the dacl, so the dacl revision must be AclRevisionDS
                //
                if (_securityDescriptor.DiscretionaryAcl.Revision < GenericAcl.AclRevisionDS)
                {
                    //
                    // we need to create a new dacl with the same aces as the existing one but the revision should be AclRevisionDS
                    //
                    byte[] binaryForm = new byte[_securityDescriptor.DiscretionaryAcl.BinaryLength];
                    _securityDescriptor.DiscretionaryAcl.GetBinaryForm(binaryForm, 0);
                    binaryForm[0] = GenericAcl.AclRevisionDS; // revision is the first byte of the binary form

                    _securityDescriptor.DiscretionaryAcl = new DiscretionaryAcl(IsContainer, IsDS, new RawAcl(binaryForm, 0));
                }
            }

            SecurityIdentifier sid = rule.IdentityReference.Translate(typeof(SecurityIdentifier)) as SecurityIdentifier;

            if (rule.AccessControlType == AccessControlType.Allow)
            {
                switch (modification)
                {
                    case AccessControlModification.Add:
                        //_securityDescriptor.DiscretionaryAcl.AddAccess(AccessControlType.Allow, sid, rule.AccessMask, rule.InheritanceFlags, rule.PropagationFlags, rule.ObjectFlags, rule.ObjectType, rule.InheritedObjectType);
                        _securityDescriptor.DiscretionaryAcl.AddAccess(AccessControlType.Allow, sid, rule);
                        break;

                    case AccessControlModification.Set:
                        //_securityDescriptor.DiscretionaryAcl.SetAccess(AccessControlType.Allow, sid, rule.AccessMask, rule.InheritanceFlags, rule.PropagationFlags, rule.ObjectFlags, rule.ObjectType, rule.InheritedObjectType);
                        _securityDescriptor.DiscretionaryAcl.SetAccess(AccessControlType.Allow, sid, rule);
                        break;

                    case AccessControlModification.Reset:
                        _securityDescriptor.DiscretionaryAcl.RemoveAccess(AccessControlType.Deny, sid, -1, InheritanceFlags.ContainerInherit, 0, ObjectAceFlags.None, Guid.Empty, Guid.Empty);
                        //_securityDescriptor.DiscretionaryAcl.SetAccess(AccessControlType.Allow, sid, rule.AccessMask, rule.InheritanceFlags, rule.PropagationFlags, rule.ObjectFlags, rule.ObjectType, rule.InheritedObjectType);
                        _securityDescriptor.DiscretionaryAcl.SetAccess(AccessControlType.Allow, sid, rule);
                        break;

                    case AccessControlModification.Remove:
                        //result = _securityDescriptor.DiscretionaryAcl.RemoveAccess(AccessControlType.Allow, sid, rule.AccessMask, rule.InheritanceFlags, rule.PropagationFlags, rule.ObjectFlags, rule.ObjectType, rule.InheritedObjectType);
                        result = _securityDescriptor.DiscretionaryAcl.RemoveAccess(AccessControlType.Allow, sid, rule);
                        break;

                    case AccessControlModification.RemoveAll:
                        result = _securityDescriptor.DiscretionaryAcl.RemoveAccess(AccessControlType.Allow, sid, -1, InheritanceFlags.ContainerInherit, 0, ObjectAceFlags.None, Guid.Empty, Guid.Empty);
                        if (result == false)
                        {                            
                            throw new InvalidOperationException(SR.InvalidOperation_RemoveFail);
                        }

                        break;

                    case AccessControlModification.RemoveSpecific:
                        //_securityDescriptor.DiscretionaryAcl.RemoveAccessSpecific(AccessControlType.Allow, sid, rule.AccessMask, rule.InheritanceFlags, rule.PropagationFlags, rule.ObjectFlags, rule.ObjectType, rule.InheritedObjectType);
                        _securityDescriptor.DiscretionaryAcl.RemoveAccessSpecific(AccessControlType.Allow, sid, rule);
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
                        //_securityDescriptor.DiscretionaryAcl.AddAccess(AccessControlType.Deny, sid, rule.AccessMask, rule.InheritanceFlags, rule.PropagationFlags, rule.ObjectFlags, rule.ObjectType, rule.InheritedObjectType);
                        _securityDescriptor.DiscretionaryAcl.AddAccess(AccessControlType.Deny, sid, rule);
                        break;

                    case AccessControlModification.Set:
                        //_securityDescriptor.DiscretionaryAcl.SetAccess(AccessControlType.Deny, sid, rule.AccessMask, rule.InheritanceFlags, rule.PropagationFlags, rule.ObjectFlags, rule.ObjectType, rule.InheritedObjectType);
                        _securityDescriptor.DiscretionaryAcl.SetAccess(AccessControlType.Deny, sid, rule);
                        break;

                    case AccessControlModification.Reset:
                        _securityDescriptor.DiscretionaryAcl.RemoveAccess(AccessControlType.Allow, sid, -1, InheritanceFlags.ContainerInherit, 0, ObjectAceFlags.None, Guid.Empty, Guid.Empty);
                        //_securityDescriptor.DiscretionaryAcl.SetAccess(AccessControlType.Deny, sid, rule.AccessMask, rule.InheritanceFlags, rule.PropagationFlags, rule.ObjectFlags, rule.ObjectType, rule.InheritedObjectType);
                        _securityDescriptor.DiscretionaryAcl.SetAccess(AccessControlType.Deny, sid, rule);
                        break;

                    case AccessControlModification.Remove:
                        //result = _securityDescriptor.DiscretionaryAcl.RemoveAccess(AccessControlType.Deny, sid, rule.AccessMask, rule.InheritanceFlags, rule.PropagationFlags, rule.ObjectFlags, rule.ObjectType, rule.InheritedObjectType);
                        result = _securityDescriptor.DiscretionaryAcl.RemoveAccess(AccessControlType.Deny, sid, rule);
                        break;

                    case AccessControlModification.RemoveAll:
                        result = _securityDescriptor.DiscretionaryAcl.RemoveAccess(AccessControlType.Deny, sid, -1, InheritanceFlags.ContainerInherit, 0, ObjectAceFlags.None, Guid.Empty, Guid.Empty);
                        if (result == false)
                        {                            
                            throw new InvalidOperationException(SR.InvalidOperation_RemoveFail);
                        }

                        break;

                    case AccessControlModification.RemoveSpecific:
                        //_securityDescriptor.DiscretionaryAcl.RemoveAccessSpecific(AccessControlType.Deny, sid, rule.AccessMask, rule.InheritanceFlags, rule.PropagationFlags, rule.ObjectFlags, rule.ObjectType, rule.InheritedObjectType);
                        _securityDescriptor.DiscretionaryAcl.RemoveAccessSpecific(AccessControlType.Deny, sid, rule);
                        break;

                    default:
                        throw new ArgumentOutOfRangeException(
nameof(modification),
                            SR.ArgumentOutOfRange_Enum);
                }
            }
            else
            {                
                throw new ArgumentException(SR.Format(SR.TypeUnrecognized_AccessControl, rule.AccessControlType));
            }

            modified = result;
            AccessRulesModified |= modified;
            return result;
        }

        //
        // Modifies the SACL
        //
        private bool ModifyAudit(AccessControlModification modification, ObjectAuditRule rule, out bool modified)
        {
            bool result = true;

            if (_securityDescriptor.SystemAcl == null)
            {
                if (modification == AccessControlModification.Remove || modification == AccessControlModification.RemoveAll || modification == AccessControlModification.RemoveSpecific)
                {
                    modified = false;
                    return result;
                }

                //_securityDescriptor.SystemAcl = new SystemAcl(IsContainer, IsDS, GenericAcl.AclRevisionDS, 1);
                //_securityDescriptor.AddControlFlags(ControlFlags.SystemAclPresent);
                _securityDescriptor.AddSystemAcl(GenericAcl.AclRevisionDS, 1);
            }
            else if ((modification == AccessControlModification.Add || modification == AccessControlModification.Set || modification == AccessControlModification.Reset) &&
                        (rule.ObjectFlags != ObjectAceFlags.None))
            {
                //
                // This will result in an object ace being added to the sacl, so the sacl revision must be AclRevisionDS
                //
                if (_securityDescriptor.SystemAcl.Revision < GenericAcl.AclRevisionDS)
                {
                    //
                    // we need to create a new sacl with the same aces as the existing one but the revision should be AclRevisionDS
                    //
                    byte[] binaryForm = new byte[_securityDescriptor.SystemAcl.BinaryLength];
                    _securityDescriptor.SystemAcl.GetBinaryForm(binaryForm, 0);
                    binaryForm[0] = GenericAcl.AclRevisionDS; // revision is the first byte of the binary form

                    _securityDescriptor.SystemAcl = new SystemAcl(IsContainer, IsDS, new RawAcl(binaryForm, 0));
                }
            }

            SecurityIdentifier sid = rule.IdentityReference.Translate(typeof(SecurityIdentifier)) as SecurityIdentifier;

            switch (modification)
            {
                case AccessControlModification.Add:
                    //_securityDescriptor.SystemAcl.AddAudit(rule.AuditFlags, sid, rule.AccessMask, rule.InheritanceFlags, rule.PropagationFlags, rule.ObjectFlags, rule.ObjectType, rule.InheritedObjectType);
                    _securityDescriptor.SystemAcl.AddAudit(sid, rule);
                    break;

                case AccessControlModification.Set:
                    //_securityDescriptor.SystemAcl.SetAudit(rule.AuditFlags, sid, rule.AccessMask, rule.InheritanceFlags, rule.PropagationFlags, rule.ObjectFlags, rule.ObjectType, rule.InheritedObjectType);
                    _securityDescriptor.SystemAcl.SetAudit(sid, rule);
                    break;

                case AccessControlModification.Reset:
                    _securityDescriptor.SystemAcl.RemoveAudit(AuditFlags.Failure | AuditFlags.Success, sid, -1, InheritanceFlags.ContainerInherit, 0, ObjectAceFlags.None, Guid.Empty, Guid.Empty);
                    //_securityDescriptor.SystemAcl.SetAudit(rule.AuditFlags, sid, rule.AccessMask, rule.InheritanceFlags, rule.PropagationFlags, rule.ObjectFlags, rule.ObjectType, rule.InheritedObjectType);
                    _securityDescriptor.SystemAcl.SetAudit(sid, rule);
                    break;

                case AccessControlModification.Remove:
                    //result = _securityDescriptor.SystemAcl.RemoveAudit(rule.AuditFlags, sid, rule.AccessMask, rule.InheritanceFlags, rule.PropagationFlags, rule.ObjectFlags, rule.ObjectType, rule.InheritedObjectType);
                    result = _securityDescriptor.SystemAcl.RemoveAudit(sid, rule);
                    break;

                case AccessControlModification.RemoveAll:
                    result = _securityDescriptor.SystemAcl.RemoveAudit(AuditFlags.Failure | AuditFlags.Success, sid, -1, InheritanceFlags.ContainerInherit, 0, ObjectAceFlags.None, Guid.Empty, Guid.Empty);
                    if (result == false)
                    {                        
                        throw new InvalidOperationException(SR.InvalidOperation_RemoveFail);
                    }

                    break;

                case AccessControlModification.RemoveSpecific:
                    //_securityDescriptor.SystemAcl.RemoveAuditSpecific(rule.AuditFlags, sid, rule.AccessMask, rule.InheritanceFlags, rule.PropagationFlags, rule.ObjectFlags, rule.ObjectType, rule.InheritedObjectType);
                    _securityDescriptor.SystemAcl.RemoveAuditSpecific(sid, rule);
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

        #endregion

        #region public Methods

        public virtual AccessRule AccessRuleFactory(IdentityReference identityReference, int accessMask, bool isInherited, InheritanceFlags inheritanceFlags, PropagationFlags propagationFlags, AccessControlType type, Guid objectType, Guid inheritedObjectType)
        {
            throw NotImplemented.ByDesign;
        }

        public virtual AuditRule AuditRuleFactory(IdentityReference identityReference, int accessMask, bool isInherited, InheritanceFlags inheritanceFlags, PropagationFlags propagationFlags, AuditFlags flags, Guid objectType, Guid inheritedObjectType)
        {
            throw NotImplemented.ByDesign;
        }

        protected override bool ModifyAccess(AccessControlModification modification, AccessRule rule, out bool modified)
        {
            ////if(this.AccessRuleType.IsAssignableFrom(rule.GetType().GetTypeInfo()))
            ////if (!TypeExtensions.IsAssignableFrom(this.AccessRuleType, rule.GetType()))
            //{
            //    throw new ArgumentException(
            //        SR.AccessControl_InvalidAccessRuleType,
            //        "rule");
            //}
            return ModifyAccess(modification, rule as ObjectAccessRule, out modified);
        }

        protected override bool ModifyAudit(AccessControlModification modification, AuditRule rule, out bool modified)
        {
            //if (this.AccessRuleType.IsAssignableFrom(rule.GetType().GetTypeInfo()))
            ////if (!TypeExtensions.IsAssignableFrom(this.AuditRuleType, rule.GetType()))
            //{
            //    throw new ArgumentException(
            //        SR.AccessControl_InvalidAuditRuleType,
            //        "rule");
            //}
            return ModifyAudit(modification, rule as ObjectAuditRule, out modified);
        }
        #endregion

        #region Public Methods

        protected void AddAccessRule(ObjectAccessRule rule)
        {
            if (rule == null)
            {
                throw new ArgumentNullException(nameof(rule));
            }

            WriteLock();

            try
            {
                bool modified;
                ModifyAccess(AccessControlModification.Add, rule, out modified);
            }
            finally
            {
                WriteUnlock();
            }

            return;
        }

        protected void SetAccessRule(ObjectAccessRule rule)
        {
            if (rule == null)
            {
                throw new ArgumentNullException(nameof(rule));
            }

            WriteLock();

            try
            {
                bool modified;
                ModifyAccess(AccessControlModification.Set, rule, out modified);
            }
            finally
            {
                WriteUnlock();
            }
        }

        protected void ResetAccessRule(ObjectAccessRule rule)
        {
            if (rule == null)
            {
                throw new ArgumentNullException(nameof(rule));
            }

            WriteLock();

            try
            {
                bool modified;
                ModifyAccess(AccessControlModification.Reset, rule, out modified);
            }
            finally
            {
                WriteUnlock();
            }
        }

        protected bool RemoveAccessRule(ObjectAccessRule rule)
        {
            if (rule == null)
            {
                throw new ArgumentNullException(nameof(rule));
            }

            WriteLock();

            try
            {
                if (_securityDescriptor == null)
                {
                    return true;
                }

                bool modified;
                return ModifyAccess(AccessControlModification.Remove, rule, out modified);
            }
            finally
            {
                WriteUnlock();
            }
        }

        protected void RemoveAccessRuleAll(ObjectAccessRule rule)
        {
            if (rule == null)
            {
                throw new ArgumentNullException(nameof(rule));
            }

            WriteLock();

            try
            {
                if (_securityDescriptor == null)
                {
                    return;
                }

                bool modified;
                ModifyAccess(AccessControlModification.RemoveAll, rule, out modified);
            }
            finally
            {
                WriteUnlock();
            }
        }

        protected void RemoveAccessRuleSpecific(ObjectAccessRule rule)
        {
            if (rule == null)
            {
                throw new ArgumentNullException(nameof(rule));
            }

            if (_securityDescriptor == null)
            {
                return;
            }

            WriteLock();

            try
            {
                bool modified;
                ModifyAccess(AccessControlModification.RemoveSpecific, rule, out modified);
            }
            finally
            {
                WriteUnlock();
            }
        }

        protected void AddAuditRule(ObjectAuditRule rule)
        {
            if (rule == null)
            {
                throw new ArgumentNullException(nameof(rule));
            }

            WriteLock();

            try
            {
                bool modified;
                ModifyAudit(AccessControlModification.Add, rule, out modified);
            }
            finally
            {
                WriteUnlock();
            }
        }

        protected void SetAuditRule(ObjectAuditRule rule)
        {
            if (rule == null)
            {
                throw new ArgumentNullException(nameof(rule));
            }

            WriteLock();

            try
            {
                bool modified;
                ModifyAudit(AccessControlModification.Set, rule, out modified);
            }
            finally
            {
                WriteUnlock();
            }
        }

        protected bool RemoveAuditRule(ObjectAuditRule rule)
        {
            if (rule == null)
            {
                throw new ArgumentNullException(nameof(rule));
            }

            WriteLock();

            try
            {
                bool modified;
                return ModifyAudit(AccessControlModification.Remove, rule, out modified);
            }
            finally
            {
                WriteUnlock();
            }
        }

        protected void RemoveAuditRuleAll(ObjectAuditRule rule)
        {
            if (rule == null)
            {
                throw new ArgumentNullException(nameof(rule));
            }

            WriteLock();

            try
            {
                bool modified;
                ModifyAudit(AccessControlModification.RemoveAll, rule, out modified);
            }
            finally
            {
                WriteUnlock();
            }
        }

        protected void RemoveAuditRuleSpecific(ObjectAuditRule rule)
        {
            if (rule == null)
            {
                throw new ArgumentNullException(nameof(rule));
            }

            WriteLock();

            try
            {
                bool modified;
                ModifyAudit(AccessControlModification.RemoveSpecific, rule, out modified);
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
