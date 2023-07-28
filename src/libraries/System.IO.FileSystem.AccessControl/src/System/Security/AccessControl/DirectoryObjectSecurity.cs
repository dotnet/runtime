// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Security.Principal;

namespace System.Security.AccessControl
{
    /// <summary>
    /// Managed ACL wrapper for directories. Base for System.DirectoryServices.ActiveDirectorySecurity.
    /// </summary>
    public abstract class DirectoryObjectSecurity : ObjectSecurity
    {
        protected DirectoryObjectSecurity()
            : base(true, true)
        {
            return;
        }

        protected DirectoryObjectSecurity(CommonSecurityDescriptor securityDescriptor)
            : base(securityDescriptor)
        {
            ArgumentNullException.ThrowIfNull(securityDescriptor);

        }

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

                CommonAcl? acl = null;

                if (access)
                {
                    if ((SecurityDescriptor.ControlFlags & ControlFlags.DiscretionaryAclPresent) != 0)
                    {
                        acl = SecurityDescriptor.DiscretionaryAcl;
                    }
                }
                else // !access == audit
                {
                    if ((SecurityDescriptor.ControlFlags & ControlFlags.SystemAclPresent) != 0)
                    {
                        acl = SecurityDescriptor.SystemAcl;
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
                        QualifiedAce? ace = acl[i] as QualifiedAce;

                        if (ace == null)
                        {
                            //
                            // Only consider qualified ACEs
                            //
                            continue;
                        }

                        if (ace.IsCallback)
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
                    QualifiedAce? ace = acl[i] as CommonAce;

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

                    if (ace.IsCallback)
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
                        IdentityReference iref = (targetType == typeof(SecurityIdentifier)) ? ace.SecurityIdentifier : irTarget![i];

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

                            if (ace is ObjectAce objectAce)
                            {
                                result.AddRule(AccessRuleFactory(iref, objectAce.AccessMask, objectAce.IsInherited, objectAce.InheritanceFlags, objectAce.PropagationFlags, type, objectAce.ObjectAceType, objectAce.InheritedObjectAceType));
                            }
                            else
                            {
                                CommonAce? commonAce = ace as CommonAce;

                                if (commonAce == null)
                                {
                                    continue;
                                }

                                result.AddRule(AccessRuleFactory(iref, commonAce.AccessMask, commonAce.IsInherited, commonAce.InheritanceFlags, commonAce.PropagationFlags, type));
                            }
                        }
                        else
                        {
                            if (ace is ObjectAce objectAce)
                            {
                                result.AddRule(AuditRuleFactory(iref, objectAce.AccessMask, objectAce.IsInherited, objectAce.InheritanceFlags, objectAce.PropagationFlags, objectAce.AuditFlags, objectAce.ObjectAceType, objectAce.InheritedObjectAceType));
                            }
                            else
                            {
                                CommonAce? commonAce = ace as CommonAce;

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

            if (SecurityDescriptor.DiscretionaryAcl == null)
            {
                if (modification == AccessControlModification.Remove || modification == AccessControlModification.RemoveAll || modification == AccessControlModification.RemoveSpecific)
                {
                    modified = false;
                    return result;
                }

                //_securityDescriptor.DiscretionaryAcl = new DiscretionaryAcl(IsContainer, IsDS, GenericAcl.AclRevisionDS, 1);
                //_securityDescriptor.AddControlFlags(ControlFlags.DiscretionaryAclPresent);
                SecurityDescriptor.AddDiscretionaryAcl(GenericAcl.AclRevisionDS, 1);
            }
            else if ((modification == AccessControlModification.Add || modification == AccessControlModification.Set || modification == AccessControlModification.Reset) &&
                        (rule.ObjectFlags != ObjectAceFlags.None))
            {
                //
                // This will result in an object ace being added to the dacl, so the dacl revision must be AclRevisionDS
                //
                if (SecurityDescriptor.DiscretionaryAcl.Revision < GenericAcl.AclRevisionDS)
                {
                    //
                    // we need to create a new dacl with the same aces as the existing one but the revision should be AclRevisionDS
                    //
                    byte[] binaryForm = new byte[SecurityDescriptor.DiscretionaryAcl.BinaryLength];
                    SecurityDescriptor.DiscretionaryAcl.GetBinaryForm(binaryForm, 0);
                    binaryForm[0] = GenericAcl.AclRevisionDS; // revision is the first byte of the binary form

                    SecurityDescriptor.DiscretionaryAcl = new DiscretionaryAcl(IsContainer, IsDS, new RawAcl(binaryForm, 0));
                }
            }

            SecurityIdentifier sid = (SecurityIdentifier)rule.IdentityReference.Translate(typeof(SecurityIdentifier));

            Debug.Assert(SecurityDescriptor.DiscretionaryAcl != null);
            if (rule.AccessControlType == AccessControlType.Allow)
            {
                switch (modification)
                {
                    case AccessControlModification.Add:
                        //_securityDescriptor.DiscretionaryAcl.AddAccess(AccessControlType.Allow, sid, rule.AccessMask, rule.InheritanceFlags, rule.PropagationFlags, rule.ObjectFlags, rule.ObjectType, rule.InheritedObjectType);
                        SecurityDescriptor.DiscretionaryAcl.AddAccess(AccessControlType.Allow, sid, rule);
                        break;

                    case AccessControlModification.Set:
                        //_securityDescriptor.DiscretionaryAcl.SetAccess(AccessControlType.Allow, sid, rule.AccessMask, rule.InheritanceFlags, rule.PropagationFlags, rule.ObjectFlags, rule.ObjectType, rule.InheritedObjectType);
                        SecurityDescriptor.DiscretionaryAcl.SetAccess(AccessControlType.Allow, sid, rule);
                        break;

                    case AccessControlModification.Reset:
                        SecurityDescriptor.DiscretionaryAcl.RemoveAccess(AccessControlType.Deny, sid, -1, InheritanceFlags.ContainerInherit, 0, ObjectAceFlags.None, Guid.Empty, Guid.Empty);
                        //_securityDescriptor.DiscretionaryAcl.SetAccess(AccessControlType.Allow, sid, rule.AccessMask, rule.InheritanceFlags, rule.PropagationFlags, rule.ObjectFlags, rule.ObjectType, rule.InheritedObjectType);
                        SecurityDescriptor.DiscretionaryAcl.SetAccess(AccessControlType.Allow, sid, rule);
                        break;

                    case AccessControlModification.Remove:
                        //result = _securityDescriptor.DiscretionaryAcl.RemoveAccess(AccessControlType.Allow, sid, rule.AccessMask, rule.InheritanceFlags, rule.PropagationFlags, rule.ObjectFlags, rule.ObjectType, rule.InheritedObjectType);
                        result = SecurityDescriptor.DiscretionaryAcl.RemoveAccess(AccessControlType.Allow, sid, rule);
                        break;

                    case AccessControlModification.RemoveAll:
                        result = SecurityDescriptor.DiscretionaryAcl.RemoveAccess(AccessControlType.Allow, sid, -1, InheritanceFlags.ContainerInherit, 0, ObjectAceFlags.None, Guid.Empty, Guid.Empty);
                        if (result == false)
                        {
                            throw new InvalidOperationException(SR.InvalidOperation_RemoveFail);
                        }

                        break;

                    case AccessControlModification.RemoveSpecific:
                        //_securityDescriptor.DiscretionaryAcl.RemoveAccessSpecific(AccessControlType.Allow, sid, rule.AccessMask, rule.InheritanceFlags, rule.PropagationFlags, rule.ObjectFlags, rule.ObjectType, rule.InheritedObjectType);
                        SecurityDescriptor.DiscretionaryAcl.RemoveAccessSpecific(AccessControlType.Allow, sid, rule);
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
                        SecurityDescriptor.DiscretionaryAcl.AddAccess(AccessControlType.Deny, sid, rule);
                        break;

                    case AccessControlModification.Set:
                        //_securityDescriptor.DiscretionaryAcl.SetAccess(AccessControlType.Deny, sid, rule.AccessMask, rule.InheritanceFlags, rule.PropagationFlags, rule.ObjectFlags, rule.ObjectType, rule.InheritedObjectType);
                        SecurityDescriptor.DiscretionaryAcl.SetAccess(AccessControlType.Deny, sid, rule);
                        break;

                    case AccessControlModification.Reset:
                        SecurityDescriptor.DiscretionaryAcl.RemoveAccess(AccessControlType.Allow, sid, -1, InheritanceFlags.ContainerInherit, 0, ObjectAceFlags.None, Guid.Empty, Guid.Empty);
                        //_securityDescriptor.DiscretionaryAcl.SetAccess(AccessControlType.Deny, sid, rule.AccessMask, rule.InheritanceFlags, rule.PropagationFlags, rule.ObjectFlags, rule.ObjectType, rule.InheritedObjectType);
                        SecurityDescriptor.DiscretionaryAcl.SetAccess(AccessControlType.Deny, sid, rule);
                        break;

                    case AccessControlModification.Remove:
                        //result = _securityDescriptor.DiscretionaryAcl.RemoveAccess(AccessControlType.Deny, sid, rule.AccessMask, rule.InheritanceFlags, rule.PropagationFlags, rule.ObjectFlags, rule.ObjectType, rule.InheritedObjectType);
                        result = SecurityDescriptor.DiscretionaryAcl.RemoveAccess(AccessControlType.Deny, sid, rule);
                        break;

                    case AccessControlModification.RemoveAll:
                        result = SecurityDescriptor.DiscretionaryAcl.RemoveAccess(AccessControlType.Deny, sid, -1, InheritanceFlags.ContainerInherit, 0, ObjectAceFlags.None, Guid.Empty, Guid.Empty);
                        if (result == false)
                        {
                            throw new InvalidOperationException(SR.InvalidOperation_RemoveFail);
                        }

                        break;

                    case AccessControlModification.RemoveSpecific:
                        //_securityDescriptor.DiscretionaryAcl.RemoveAccessSpecific(AccessControlType.Deny, sid, rule.AccessMask, rule.InheritanceFlags, rule.PropagationFlags, rule.ObjectFlags, rule.ObjectType, rule.InheritedObjectType);
                        SecurityDescriptor.DiscretionaryAcl.RemoveAccessSpecific(AccessControlType.Deny, sid, rule);
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

            if (SecurityDescriptor.SystemAcl == null)
            {
                if (modification == AccessControlModification.Remove || modification == AccessControlModification.RemoveAll || modification == AccessControlModification.RemoveSpecific)
                {
                    modified = false;
                    return result;
                }

                //_securityDescriptor.SystemAcl = new SystemAcl(IsContainer, IsDS, GenericAcl.AclRevisionDS, 1);
                //_securityDescriptor.AddControlFlags(ControlFlags.SystemAclPresent);
                SecurityDescriptor.AddSystemAcl(GenericAcl.AclRevisionDS, 1);
            }
            else if ((modification == AccessControlModification.Add || modification == AccessControlModification.Set || modification == AccessControlModification.Reset) &&
                        (rule.ObjectFlags != ObjectAceFlags.None))
            {
                //
                // This will result in an object ace being added to the sacl, so the sacl revision must be AclRevisionDS
                //
                if (SecurityDescriptor.SystemAcl.Revision < GenericAcl.AclRevisionDS)
                {
                    //
                    // we need to create a new sacl with the same aces as the existing one but the revision should be AclRevisionDS
                    //
                    byte[] binaryForm = new byte[SecurityDescriptor.SystemAcl.BinaryLength];
                    SecurityDescriptor.SystemAcl.GetBinaryForm(binaryForm, 0);
                    binaryForm[0] = GenericAcl.AclRevisionDS; // revision is the first byte of the binary form

                    SecurityDescriptor.SystemAcl = new SystemAcl(IsContainer, IsDS, new RawAcl(binaryForm, 0));
                }
            }

            SecurityIdentifier sid = (SecurityIdentifier)rule.IdentityReference.Translate(typeof(SecurityIdentifier));

            Debug.Assert(SecurityDescriptor.SystemAcl != null);
            switch (modification)
            {
                case AccessControlModification.Add:
                    //_securityDescriptor.SystemAcl.AddAudit(rule.AuditFlags, sid, rule.AccessMask, rule.InheritanceFlags, rule.PropagationFlags, rule.ObjectFlags, rule.ObjectType, rule.InheritedObjectType);
                    SecurityDescriptor.SystemAcl.AddAudit(sid, rule);
                    break;

                case AccessControlModification.Set:
                    //_securityDescriptor.SystemAcl.SetAudit(rule.AuditFlags, sid, rule.AccessMask, rule.InheritanceFlags, rule.PropagationFlags, rule.ObjectFlags, rule.ObjectType, rule.InheritedObjectType);
                    SecurityDescriptor.SystemAcl.SetAudit(sid, rule);
                    break;

                case AccessControlModification.Reset:
                    SecurityDescriptor.SystemAcl.RemoveAudit(AuditFlags.Failure | AuditFlags.Success, sid, -1, InheritanceFlags.ContainerInherit, 0, ObjectAceFlags.None, Guid.Empty, Guid.Empty);
                    //_securityDescriptor.SystemAcl.SetAudit(rule.AuditFlags, sid, rule.AccessMask, rule.InheritanceFlags, rule.PropagationFlags, rule.ObjectFlags, rule.ObjectType, rule.InheritedObjectType);
                    SecurityDescriptor.SystemAcl.SetAudit(sid, rule);
                    break;

                case AccessControlModification.Remove:
                    //result = _securityDescriptor.SystemAcl.RemoveAudit(rule.AuditFlags, sid, rule.AccessMask, rule.InheritanceFlags, rule.PropagationFlags, rule.ObjectFlags, rule.ObjectType, rule.InheritedObjectType);
                    result = SecurityDescriptor.SystemAcl.RemoveAudit(sid, rule);
                    break;

                case AccessControlModification.RemoveAll:
                    result = SecurityDescriptor.SystemAcl.RemoveAudit(AuditFlags.Failure | AuditFlags.Success, sid, -1, InheritanceFlags.ContainerInherit, 0, ObjectAceFlags.None, Guid.Empty, Guid.Empty);
                    if (result == false)
                    {
                        throw new InvalidOperationException(SR.InvalidOperation_RemoveFail);
                    }

                    break;

                case AccessControlModification.RemoveSpecific:
                    //_securityDescriptor.SystemAcl.RemoveAuditSpecific(rule.AuditFlags, sid, rule.AccessMask, rule.InheritanceFlags, rule.PropagationFlags, rule.ObjectFlags, rule.ObjectType, rule.InheritedObjectType);
                    SecurityDescriptor.SystemAcl.RemoveAuditSpecific(sid, rule);
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
            ////if (this.AccessRuleType.IsAssignableFrom(rule.GetType().GetTypeInfo()))
            ////if (!TypeExtensions.IsAssignableFrom(this.AccessRuleType, rule.GetType()))
            //{
            //    throw new ArgumentException(
            //        SR.AccessControl_InvalidAccessRuleType,
            //        "rule");
            //}
            return ModifyAccess(modification, (ObjectAccessRule)rule, out modified);
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
            return ModifyAudit(modification, (ObjectAuditRule)rule, out modified);
        }
#endregion

#region Public Methods

        protected void AddAccessRule(ObjectAccessRule rule)
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

            return;
        }

        protected void SetAccessRule(ObjectAccessRule rule)
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

        protected void ResetAccessRule(ObjectAccessRule rule)
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
        }

        protected bool RemoveAccessRule(ObjectAccessRule rule)
        {
            ArgumentNullException.ThrowIfNull(rule);

            WriteLock();

            try
            {
                if (SecurityDescriptor == null)
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

        protected void RemoveAccessRuleAll(ObjectAccessRule rule)
        {
            ArgumentNullException.ThrowIfNull(rule);

            WriteLock();

            try
            {
                if (SecurityDescriptor == null)
                {
                    return;
                }

                ModifyAccess(AccessControlModification.RemoveAll, rule, out _);
            }
            finally
            {
                WriteUnlock();
            }
        }

        protected void RemoveAccessRuleSpecific(ObjectAccessRule rule)
        {
            ArgumentNullException.ThrowIfNull(rule);

            if (SecurityDescriptor == null)
            {
                return;
            }

            WriteLock();

            try
            {
                ModifyAccess(AccessControlModification.RemoveSpecific, rule, out _);
            }
            finally
            {
                WriteUnlock();
            }
        }

        protected void AddAuditRule(ObjectAuditRule rule)
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

        protected void SetAuditRule(ObjectAuditRule rule)
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

        protected bool RemoveAuditRule(ObjectAuditRule rule)
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

        protected void RemoveAuditRuleAll(ObjectAuditRule rule)
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

        protected void RemoveAuditRuleSpecific(ObjectAuditRule rule)
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
