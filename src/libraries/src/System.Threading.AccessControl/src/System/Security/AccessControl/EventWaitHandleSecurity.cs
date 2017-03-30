// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

/*============================================================
**
**
**
** Purpose: Managed ACL wrapper for Win32 events.
**
**
===========================================================*/

using System;
using System.Collections;
using System.Security.Principal;
using Microsoft.Win32;
using Microsoft.Win32.SafeHandles;
using System.Runtime.InteropServices;
using System.Threading;

namespace System.Security.AccessControl
{
    // Derive this list of values from winnt.h and MSDN docs:
    // http://msdn.microsoft.com/library/default.asp?url=/library/en-us/dllproc/base/synchronization_object_security_and_access_rights.asp

    // Win32's interesting values are EVENT_MODIFY_STATE (0x0002) and
    // EVENT_ALL_ACCESS (0x1F0003).  I don't know what 0x1 is, but Windows
    // includes it in EVENT_ALL_ACCESS.
    [Flags]
    public enum EventWaitHandleRights
    {
        Modify = 0x000002,
        Delete = 0x010000,
        ReadPermissions = 0x020000,
        ChangePermissions = 0x040000,
        TakeOwnership = 0x080000,
        Synchronize = 0x100000,  // SYNCHRONIZE
        FullControl = 0x1F0003
    }


    public sealed class EventWaitHandleAccessRule : AccessRule
    {
        // Constructor for creating access rules for registry objects

        public EventWaitHandleAccessRule(IdentityReference identity, EventWaitHandleRights eventRights, AccessControlType type)
            : this(identity, (int)eventRights, false, InheritanceFlags.None, PropagationFlags.None, type)
        {
        }

        public EventWaitHandleAccessRule(String identity, EventWaitHandleRights eventRights, AccessControlType type)
            : this(new NTAccount(identity), (int)eventRights, false, InheritanceFlags.None, PropagationFlags.None, type)
        {
        }

        //
        // Internal constructor to be called by public constructors
        // and the access rule factory methods of {File|Folder}Security
        //
        internal EventWaitHandleAccessRule(
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

        public EventWaitHandleRights EventWaitHandleRights
        {
            get { return (EventWaitHandleRights)base.AccessMask; }
        }
    }


    public sealed class EventWaitHandleAuditRule : AuditRule
    {
        public EventWaitHandleAuditRule(IdentityReference identity, EventWaitHandleRights eventRights, AuditFlags flags)
            : this(identity, (int)eventRights, false, InheritanceFlags.None, PropagationFlags.None, flags)
        {
        }

        /* // Not in the spec
        public EventWaitHandleAuditRule(string identity, EventWaitHandleRights eventRights, AuditFlags flags)
            : this(new NTAccount(identity), (int) eventRights, false, InheritanceFlags.None, PropagationFlags.None, flags)
        {
        }
        */

        internal EventWaitHandleAuditRule(IdentityReference identity, int accessMask, bool isInherited, InheritanceFlags inheritanceFlags, PropagationFlags propagationFlags, AuditFlags flags)
            : base(identity, accessMask, isInherited, inheritanceFlags, propagationFlags, flags)
        {
        }

        public EventWaitHandleRights EventWaitHandleRights
        {
            get { return (EventWaitHandleRights)base.AccessMask; }
        }
    }


    public sealed class EventWaitHandleSecurity : NativeObjectSecurity
    {
        public EventWaitHandleSecurity()
            : base(true, ResourceType.KernelObject)
        {
        }

        [System.Security.SecurityCritical]  // auto-generated
        internal EventWaitHandleSecurity(String name, AccessControlSections includeSections)
            : base(true, ResourceType.KernelObject, name, includeSections, _HandleErrorCode, null)
        {
            // Let the underlying ACL API's demand unmanaged code permission.
        }

        [System.Security.SecurityCritical]  // auto-generated
        internal EventWaitHandleSecurity(SafeWaitHandle handle, AccessControlSections includeSections)
            : base(true, ResourceType.KernelObject, handle, includeSections, _HandleErrorCode, null)
        {
            // Let the underlying ACL API's demand unmanaged code permission.
        }

        [System.Security.SecurityCritical]  // auto-generated
        private static Exception _HandleErrorCode(int errorCode, string name, SafeHandle handle, object context)
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

                default:
                    break;
            }

            return exception;
        }

        public override AccessRule AccessRuleFactory(IdentityReference identityReference, int accessMask, bool isInherited, InheritanceFlags inheritanceFlags, PropagationFlags propagationFlags, AccessControlType type)
        {
            return new EventWaitHandleAccessRule(identityReference, accessMask, isInherited, inheritanceFlags, propagationFlags, type);
        }

        public override AuditRule AuditRuleFactory(IdentityReference identityReference, int accessMask, bool isInherited, InheritanceFlags inheritanceFlags, PropagationFlags propagationFlags, AuditFlags flags)
        {
            return new EventWaitHandleAuditRule(identityReference, accessMask, isInherited, inheritanceFlags, propagationFlags, flags);
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

        [System.Security.SecurityCritical]  // auto-generated
        internal void Persist(SafeWaitHandle handle)
        {
            //
            // Let the underlying ACL API's demand unmanaged code.
            //

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

        public void AddAccessRule(EventWaitHandleAccessRule rule)
        {
            base.AddAccessRule(rule);
        }

        public void SetAccessRule(EventWaitHandleAccessRule rule)
        {
            base.SetAccessRule(rule);
        }

        public void ResetAccessRule(EventWaitHandleAccessRule rule)
        {
            base.ResetAccessRule(rule);
        }

        public bool RemoveAccessRule(EventWaitHandleAccessRule rule)
        {
            return base.RemoveAccessRule(rule);
        }

        public void RemoveAccessRuleAll(EventWaitHandleAccessRule rule)
        {
            base.RemoveAccessRuleAll(rule);
        }

        public void RemoveAccessRuleSpecific(EventWaitHandleAccessRule rule)
        {
            base.RemoveAccessRuleSpecific(rule);
        }

        public void AddAuditRule(EventWaitHandleAuditRule rule)
        {
            base.AddAuditRule(rule);
        }

        public void SetAuditRule(EventWaitHandleAuditRule rule)
        {
            base.SetAuditRule(rule);
        }

        public bool RemoveAuditRule(EventWaitHandleAuditRule rule)
        {
            return base.RemoveAuditRule(rule);
        }

        public void RemoveAuditRuleAll(EventWaitHandleAuditRule rule)
        {
            base.RemoveAuditRuleAll(rule);
        }

        public void RemoveAuditRuleSpecific(EventWaitHandleAuditRule rule)
        {
            base.RemoveAuditRuleSpecific(rule);
        }

        public override Type AccessRightType
        {
            get { return typeof(EventWaitHandleRights); }
        }

        public override Type AccessRuleType
        {
            get { return typeof(EventWaitHandleAccessRule); }
        }

        public override Type AuditRuleType
        {
            get { return typeof(EventWaitHandleAuditRule); }
        }
    }
}
