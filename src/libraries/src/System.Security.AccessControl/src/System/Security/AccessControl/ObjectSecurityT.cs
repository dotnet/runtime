// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

/*============================================================
**
** Class:  ObjectSecurity
**
** Purpose: Generic Managed ACL wrapper
** 
** Date:  February 7, 2007
**
===========================================================*/

using System;
using System.Runtime.InteropServices;
using System.Security.AccessControl;
using System.Security.Principal;
using Microsoft.Win32.SafeHandles;

namespace System.Security.AccessControl
{
    public class AccessRule<T> : AccessRule where T : struct
    {
        #region Constructors
        //
        // Constructors for creating access rules for file objects
        //

        public AccessRule(
            IdentityReference identity,
            T rights,
            AccessControlType type)
            : this(
                identity,
                (int)(object)rights,
                false,
                InheritanceFlags.None,
                PropagationFlags.None,
                type)
        { }

        public AccessRule(
            string identity,
            T rights,
            AccessControlType type)
            : this(
                new NTAccount(identity),
                (int)(object)rights,
                false,
                InheritanceFlags.None,
                PropagationFlags.None,
                type)
        { }

        //
        // Constructor for creating access rules for folder objects
        //

        public AccessRule(
            IdentityReference identity,
            T rights,
            InheritanceFlags inheritanceFlags,
            PropagationFlags propagationFlags,
            AccessControlType type)
            : this(
                identity,
                (int)(object)rights,
                false,
                inheritanceFlags,
                propagationFlags,
                type)
        { }

        public AccessRule(
            string identity,
            T rights,
            InheritanceFlags inheritanceFlags,
            PropagationFlags propagationFlags,
            AccessControlType type)
            : this(
                new NTAccount(identity),
                (int)(object)rights,
                false,
                inheritanceFlags,
                propagationFlags,
                type)
        { }

        //
        // Internal constructor to be called by public constructors
        // and the access rule factory methods of ObjectSecurity
        //

        internal AccessRule(
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
        { }

        #endregion

        #region Public properties

        public T Rights
        {
            get { return (T)(object)base.AccessMask; }
        }
        #endregion
    }


    public class AuditRule<T> : AuditRule where T : struct
    {
        #region Constructors

        public AuditRule(
            IdentityReference identity,
            T rights,
            AuditFlags flags)
            : this(
                identity,
                rights,
                InheritanceFlags.None,
                PropagationFlags.None,
                flags)
        {
        }

        public AuditRule(
            IdentityReference identity,
            T rights,
            InheritanceFlags inheritanceFlags,
            PropagationFlags propagationFlags,
            AuditFlags flags)
            : this(
                identity,
                (int)(object)rights,
                false,
                inheritanceFlags,
                propagationFlags,
                flags)
        {
        }

        public AuditRule(
            string identity,
            T rights,
            AuditFlags flags)
            : this(
                new NTAccount(identity),
                rights,
                InheritanceFlags.None,
                PropagationFlags.None,
                flags)
        {
        }

        public AuditRule(
            string identity,
            T rights,
            InheritanceFlags inheritanceFlags,
            PropagationFlags propagationFlags,
            AuditFlags flags)
            : this(
                new NTAccount(identity),
                (int)(object)rights,
                false,
                inheritanceFlags,
                propagationFlags,
                flags)
        {
        }

        internal AuditRule(
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

        #region Public properties

        public T Rights
        {
            get { return (T)(object)base.AccessMask; }
        }
        #endregion
    }


    public abstract class ObjectSecurity<T> : NativeObjectSecurity where T : struct
    {
        #region Constructors

        protected ObjectSecurity(bool isContainer, ResourceType resourceType)
            : base(isContainer, resourceType, null, null)
        { }

        protected ObjectSecurity(bool isContainer, ResourceType resourceType, string name, AccessControlSections includeSections)
            : base(isContainer, resourceType, name, includeSections, null, null)
        { }

        protected ObjectSecurity(bool isContainer, ResourceType resourceType, string name, AccessControlSections includeSections, ExceptionFromErrorCode exceptionFromErrorCode, object exceptionContext)
            : base(isContainer, resourceType, name, includeSections, exceptionFromErrorCode, exceptionContext)
        { }

        protected ObjectSecurity(bool isContainer, ResourceType resourceType, SafeHandle safeHandle, AccessControlSections includeSections)
            : base(isContainer, resourceType, safeHandle, includeSections, null, null)
        { }

        protected ObjectSecurity(bool isContainer, ResourceType resourceType, SafeHandle safeHandle, AccessControlSections includeSections, ExceptionFromErrorCode exceptionFromErrorCode, object exceptionContext)
            : base(isContainer, resourceType, safeHandle, includeSections, exceptionFromErrorCode, exceptionContext)
        { }

        #endregion
        #region Factories

        public override AccessRule AccessRuleFactory(
            IdentityReference identityReference,
            int accessMask,
            bool isInherited,
            InheritanceFlags inheritanceFlags,
            PropagationFlags propagationFlags,
            AccessControlType type)
        {
            return new AccessRule<T>(
                identityReference,
                accessMask,
                isInherited,
                inheritanceFlags,
                propagationFlags,
                type);
        }

        public override AuditRule AuditRuleFactory(
            IdentityReference identityReference,
            int accessMask,
            bool isInherited,
            InheritanceFlags inheritanceFlags,
            PropagationFlags propagationFlags,
            AuditFlags flags)
        {
            return new AuditRule<T>(
                identityReference,
                accessMask,
                isInherited,
                inheritanceFlags,
                propagationFlags,
                flags);
        }

        #endregion
        #region Private Methods

        private AccessControlSections GetAccessControlSectionsFromChanges()
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

        #endregion
        #region Protected Methods

        // Use this in your own Persist after you have demanded any appropriate CAS permissions.
        // Note that you will want your version to be internal and use a specialized Safe Handle. 
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

        // Use this in your own Persist after you have demanded any appropriate CAS permissions.
        // Note that you will want your version to be internal. 
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

        #endregion
        #region Public Methods

        // Override these if you need to do some custom bit remapping to hide any 
        // complexity from the user. 
        public virtual void AddAccessRule(AccessRule<T> rule)
        {
            base.AddAccessRule(rule);
        }

        public virtual void SetAccessRule(AccessRule<T> rule)
        {
            base.SetAccessRule(rule);
        }

        public virtual void ResetAccessRule(AccessRule<T> rule)
        {
            base.ResetAccessRule(rule);
        }

        public virtual bool RemoveAccessRule(AccessRule<T> rule)
        {
            return base.RemoveAccessRule(rule);
        }

        public virtual void RemoveAccessRuleAll(AccessRule<T> rule)
        {
            base.RemoveAccessRuleAll(rule);
        }

        public virtual void RemoveAccessRuleSpecific(AccessRule<T> rule)
        {
            base.RemoveAccessRuleSpecific(rule);
        }

        public virtual void AddAuditRule(AuditRule<T> rule)
        {
            base.AddAuditRule(rule);
        }

        public virtual void SetAuditRule(AuditRule<T> rule)
        {
            base.SetAuditRule(rule);
        }

        public virtual bool RemoveAuditRule(AuditRule<T> rule)
        {
            return base.RemoveAuditRule(rule);
        }

        public virtual void RemoveAuditRuleAll(AuditRule<T> rule)
        {
            base.RemoveAuditRuleAll(rule);
        }

        public virtual void RemoveAuditRuleSpecific(AuditRule<T> rule)
        {
            base.RemoveAuditRuleSpecific(rule);
        }

        #endregion
        #region some overrides

        public override Type AccessRightType
        {
            get { return typeof(T); }
        }

        public override Type AccessRuleType
        {
            get { return typeof(AccessRule<T>); }
        }

        public override Type AuditRuleType
        {
            get { return typeof(AuditRule<T>); }
        }
        #endregion
    }
}
