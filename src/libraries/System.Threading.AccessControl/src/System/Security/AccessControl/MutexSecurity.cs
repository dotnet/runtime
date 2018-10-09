// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

/*============================================================
**
**
**
** Purpose: Managed ACL wrapper for Win32 mutexes.
**
**
===========================================================*/

using System;
using System.Collections;
using System.Runtime.InteropServices;
using System.Security.Principal;
using System.Threading;
using Microsoft.Win32;
using Microsoft.Win32.SafeHandles;

namespace System.Security.AccessControl
{
    // Derive this list of values from winnt.h and MSDN docs:
    // https://docs.microsoft.com/en-us/windows/desktop/sync/synchronization-object-security-and-access-rights

    // In order to call ReleaseMutex, you must have an ACL granting you
    // MUTEX_MODIFY_STATE rights (0x0001).  The other interesting value
    // in a Mutex's ACL is MUTEX_ALL_ACCESS (0x1F0001).
    // You need SYNCHRONIZE to be able to open a handle to a mutex.
    [Flags]
    public enum MutexRights
    {
        Modify = 0x000001,
        Delete = 0x010000,
        ReadPermissions = 0x020000,
        ChangePermissions = 0x040000,
        TakeOwnership = 0x080000,
        Synchronize = 0x100000,  // SYNCHRONIZE
        FullControl = 0x1F0001
    }


    public sealed class MutexAccessRule : AccessRule
    {
        // Constructor for creating access rules for registry objects

        public MutexAccessRule(IdentityReference identity, MutexRights eventRights, AccessControlType type)
            : this(identity, (int)eventRights, false, InheritanceFlags.None, PropagationFlags.None, type)
        {
        }

        public MutexAccessRule(string identity, MutexRights eventRights, AccessControlType type)
            : this(new NTAccount(identity), (int)eventRights, false, InheritanceFlags.None, PropagationFlags.None, type)
        {
        }

        //
        // Internal constructor to be called by public constructors
        // and the access rule factory methods of {File|Folder}Security
        //
        internal MutexAccessRule(
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

        public MutexRights MutexRights
        {
            get { return (MutexRights)base.AccessMask; }
        }
    }


    public sealed class MutexAuditRule : AuditRule
    {
        public MutexAuditRule(IdentityReference identity, MutexRights eventRights, AuditFlags flags)
            : this(identity, (int)eventRights, false, InheritanceFlags.None, PropagationFlags.None, flags)
        {
        }

        internal MutexAuditRule(IdentityReference identity, int accessMask, bool isInherited, InheritanceFlags inheritanceFlags, PropagationFlags propagationFlags, AuditFlags flags)
            : base(identity, accessMask, isInherited, inheritanceFlags, propagationFlags, flags)
        {
        }

        public MutexRights MutexRights
        {
            get { return (MutexRights)base.AccessMask; }
        }
    }


    public sealed class MutexSecurity : NativeObjectSecurity
    {
        public MutexSecurity()
            : base(true, ResourceType.KernelObject)
        {
        }

        public MutexSecurity(string name, AccessControlSections includeSections)
            : base(true, ResourceType.KernelObject, name, includeSections, HandleErrorCode, null)
        {
            // Let the underlying ACL API's demand unmanaged code permission.
        }

        internal MutexSecurity(SafeWaitHandle handle, AccessControlSections includeSections)
            : base(true, ResourceType.KernelObject, handle, includeSections, HandleErrorCode, null)
        {
            // Let the underlying ACL API's demand unmanaged code permission.
        }

        private static Exception HandleErrorCode(int errorCode, string name, SafeHandle handle, object context)
        {
            System.Exception exception = null;

            switch (errorCode)
            {
                case Interop.Errors.ERROR_INVALID_NAME:
                case Interop.Errors.ERROR_INVALID_HANDLE:
                case Interop.Errors.ERROR_FILE_NOT_FOUND:
                    if ((name != null) && (name.Length != 0))
                        exception = new WaitHandleCannotBeOpenedException(SR.Format(SR.WaitHandleCannotBeOpenedException_InvalidHandle, name));
                    else
                        exception = new WaitHandleCannotBeOpenedException();
                    break;
            }

            return exception;
        }

        public override AccessRule AccessRuleFactory(IdentityReference identityReference, int accessMask, bool isInherited, InheritanceFlags inheritanceFlags, PropagationFlags propagationFlags, AccessControlType type)
        {
            return new MutexAccessRule(identityReference, accessMask, isInherited, inheritanceFlags, propagationFlags, type);
        }

        public override AuditRule AuditRuleFactory(IdentityReference identityReference, int accessMask, bool isInherited, InheritanceFlags inheritanceFlags, PropagationFlags propagationFlags, AuditFlags flags)
        {
            return new MutexAuditRule(identityReference, accessMask, isInherited, inheritanceFlags, propagationFlags, flags);
        }

        internal AccessControlSections GetAccessControlSectionsFromChanges()
        {
            AccessControlSections persistRules = AccessControlSections.None;
            if (AccessRulesModified)
                persistRules |= AccessControlSections.Access;
            if (AuditRulesModified)
                persistRules |= AccessControlSections.Audit;
            if (OwnerModified)
                persistRules |= AccessControlSections.Owner;
            if (GroupModified)
                persistRules |= AccessControlSections.Group;
            return persistRules;
        }

        internal void Persist(SafeWaitHandle handle)
        {
            // Let the underlying ACL API's demand unmanaged code.

            WriteLock();

            try
            {
                AccessControlSections persistSections = GetAccessControlSectionsFromChanges();
                if (persistSections == AccessControlSections.None)
                    return;  // Don't need to persist anything.

                base.Persist(handle, persistSections);
                OwnerModified = GroupModified = AuditRulesModified = AccessRulesModified = false;
            }
            finally
            {
                WriteUnlock();
            }
        }

        public void AddAccessRule(MutexAccessRule rule)
        {
            base.AddAccessRule(rule);
        }

        public void SetAccessRule(MutexAccessRule rule)
        {
            base.SetAccessRule(rule);
        }

        public void ResetAccessRule(MutexAccessRule rule)
        {
            base.ResetAccessRule(rule);
        }

        public bool RemoveAccessRule(MutexAccessRule rule)
        {
            return base.RemoveAccessRule(rule);
        }

        public void RemoveAccessRuleAll(MutexAccessRule rule)
        {
            base.RemoveAccessRuleAll(rule);
        }

        public void RemoveAccessRuleSpecific(MutexAccessRule rule)
        {
            base.RemoveAccessRuleSpecific(rule);
        }

        public void AddAuditRule(MutexAuditRule rule)
        {
            base.AddAuditRule(rule);
        }

        public void SetAuditRule(MutexAuditRule rule)
        {
            base.SetAuditRule(rule);
        }

        public bool RemoveAuditRule(MutexAuditRule rule)
        {
            return base.RemoveAuditRule(rule);
        }

        public void RemoveAuditRuleAll(MutexAuditRule rule)
        {
            base.RemoveAuditRuleAll(rule);
        }

        public void RemoveAuditRuleSpecific(MutexAuditRule rule)
        {
            base.RemoveAuditRuleSpecific(rule);
        }

        public override Type AccessRightType
        {
            get { return typeof(MutexRights); }
        }

        public override Type AccessRuleType
        {
            get { return typeof(MutexAccessRule); }
        }

        public override Type AuditRuleType
        {
            get { return typeof(MutexAuditRule); }
        }
    }
}
