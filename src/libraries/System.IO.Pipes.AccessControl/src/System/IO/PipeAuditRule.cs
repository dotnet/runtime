// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Security.AccessControl;
using System.Security.Principal;

namespace System.IO.Pipes
{
    //[System.Security.Permissions.HostProtection(MayLeakOnAbort = true)]
    public sealed class PipeAuditRule : AuditRule
    {
        public PipeAuditRule(
            IdentityReference identity,
            PipeAccessRights rights,
            AuditFlags flags)
            : this(
                identity,
                AccessMaskFromRights(rights),
                false,
                flags)
        {
        }

        public PipeAuditRule(
            String identity,
            PipeAccessRights rights,
            AuditFlags flags)
            : this(
                new NTAccount(identity),
                AccessMaskFromRights(rights),
                false,
                flags)
        {
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
                flags)
        {
        }

        private static int AccessMaskFromRights(PipeAccessRights rights)
        {
            if (rights < (PipeAccessRights)0 || rights > (PipeAccessRights.FullControl | PipeAccessRights.AccessSystemSecurity))
            {
                throw new ArgumentOutOfRangeException(nameof(rights), SR.ArgumentOutOfRange_NeedValidPipeAccessRights);
            }

            return (int)rights;
        }

        public PipeAccessRights PipeAccessRights
        {
            get
            {
                return PipeAccessRule.RightsFromAccessMask(base.AccessMask);
            }
        }
    }
}

