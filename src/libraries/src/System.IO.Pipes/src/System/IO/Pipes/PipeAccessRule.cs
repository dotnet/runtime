// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Security.AccessControl;
using System.Security.Principal;

namespace System.IO.Pipes
{
    public sealed class PipeAccessRule : AccessRule
    {
        //
        // Constructor for creating access rules for pipe objects
        //
        public PipeAccessRule( string identity, PipeAccessRights rights, AccessControlType type)
            : this( new NTAccount(identity), AccessMaskFromRights(rights, type), false, type)
        {
        }

        public PipeAccessRule( IdentityReference identity, PipeAccessRights rights, AccessControlType type)
            : this(identity, AccessMaskFromRights(rights, type), false, type)
        {
        }

        //
        // Internal constructor to be called by public constructors
        // and the access rights factory methods
        //
        internal PipeAccessRule( IdentityReference identity, int accessMask, bool isInherited, AccessControlType type)
            : base( identity, accessMask, isInherited, InheritanceFlags.None, PropagationFlags.None, type)
        {
        }

        public PipeAccessRights PipeAccessRights
        {
            get
            {
                return RightsFromAccessMask(base.AccessMask);
            }
        }

        // ACL's on pipes have a SYNCHRONIZE bit, and CreateFile ALWAYS asks for it.  
        // So for allows, let's always include this bit, and for denies, let's never
        // include this bit unless we're denying full control.  This is the right 
        // thing for users, even if it does make the model look asymmetrical from a
        // purist point of view.
        internal static int AccessMaskFromRights(PipeAccessRights rights, AccessControlType controlType)
        {
            if (rights < (PipeAccessRights)0 || rights > (PipeAccessRights.FullControl | PipeAccessRights.AccessSystemSecurity))
                throw new ArgumentOutOfRangeException(nameof(rights), SR.ArgumentOutOfRange_NeedValidPipeAccessRights);

            if (controlType == AccessControlType.Allow)
            {
                rights |= PipeAccessRights.Synchronize;
            }
            else if (controlType == AccessControlType.Deny)
            {
                if (rights != PipeAccessRights.FullControl)
                {
                    rights &= ~PipeAccessRights.Synchronize;
                }
            }

            return (int)rights;
        }

        internal static PipeAccessRights RightsFromAccessMask(int accessMask)
        {
            return (PipeAccessRights)accessMask;
        }
    }
}

