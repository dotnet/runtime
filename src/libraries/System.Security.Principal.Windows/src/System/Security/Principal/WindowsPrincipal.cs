// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.Win32.SafeHandles;
using System.Collections.Generic;
using System.ComponentModel;
using System.Security.Claims;

namespace System.Security.Principal
{
    public enum WindowsBuiltInRole
    {
        Administrator = 0x220,
        User = 0x221,
        Guest = 0x222,
        PowerUser = 0x223,
        AccountOperator = 0x224,
        SystemOperator = 0x225,
        PrintOperator = 0x226,
        BackupOperator = 0x227,
        Replicator = 0x228
    }

    public class WindowsPrincipal : ClaimsPrincipal
    {
        private WindowsIdentity _identity = null;

        //
        // Constructors.
        //

        private WindowsPrincipal() { }

        public WindowsPrincipal(WindowsIdentity ntIdentity)
            : base(ntIdentity)
        {
            if (ntIdentity == null)
                throw new ArgumentNullException(nameof(ntIdentity));

            _identity = ntIdentity;
        }
        
        //
        // Properties.
        //
        public override IIdentity Identity
        {
            get
            {
                return _identity;
            }
        }

        //
        // Public methods.
        //


        public override bool IsInRole(string role)
        {
            if (role == null || role.Length == 0)
                return false;

            NTAccount ntAccount = new NTAccount(role);
            IdentityReferenceCollection source = new IdentityReferenceCollection(1);
            source.Add(ntAccount);
            IdentityReferenceCollection target = NTAccount.Translate(source, typeof(SecurityIdentifier), false);

            SecurityIdentifier sid = target[0] as SecurityIdentifier;

            if (sid != null)
            {
                if (IsInRole(sid))
                {
                    return true;
                }
            }

            // possible that identity has other role claims that match
            return base.IsInRole(role);
        }

        // <summary
        // Returns all of the claims from all of the identities that are windows user claims
        // found in the NT token.
        // </summary>
        public virtual IEnumerable<Claim> UserClaims
        {
            get
            {
                foreach (ClaimsIdentity identity in Identities)
                {
                    WindowsIdentity wi = identity as WindowsIdentity;
                    if (wi != null)
                    {
                        foreach (Claim claim in wi.UserClaims)
                        {
                            yield return claim;
                        }
                    }
                }
            }
        }

        // <summary
        // Returns all of the claims from all of the identities that are windows device claims
        // found in the NT token.
        // </summary>
        public virtual IEnumerable<Claim> DeviceClaims
        {
            get
            {
                foreach (ClaimsIdentity identity in Identities)
                {
                    WindowsIdentity wi = identity as WindowsIdentity;
                    if (wi != null)
                    {
                        foreach (Claim claim in wi.DeviceClaims)
                        {
                            yield return claim;
                        }
                    }
                }
            }
        }

        public virtual bool IsInRole(WindowsBuiltInRole role)
        {
            if (role < WindowsBuiltInRole.Administrator || role > WindowsBuiltInRole.Replicator)
                throw new ArgumentException(SR.Format(SR.Arg_EnumIllegalVal, (int)role), nameof(role));

            return IsInRole((int)role);
        }

        public virtual bool IsInRole(int rid)
        {
            SecurityIdentifier sid = new SecurityIdentifier(IdentifierAuthority.NTAuthority,
                                                            new int[] { Interop.SecurityIdentifier.SECURITY_BUILTIN_DOMAIN_RID, rid });

            return IsInRole(sid);
        }

        // This method (with a SID parameter) is more general than the 2 overloads that accept a WindowsBuiltInRole or
        // a rid (as an int). It is also better from a performance standpoint than the overload that accepts a string.
        // The aformentioned overloads remain in this class since we do not want to introduce a
        // breaking change. However, this method should be used in all new applications.
        
        public virtual bool IsInRole(SecurityIdentifier sid)
        {
            if (sid == null)
                throw new ArgumentNullException(nameof(sid));

            // special case the anonymous identity.
            if (_identity.AccessToken.IsInvalid)
                return false;

            // CheckTokenMembership expects an impersonation token
            SafeAccessTokenHandle token = SafeAccessTokenHandle.InvalidHandle;
            if (_identity.ImpersonationLevel == TokenImpersonationLevel.None)
            {
                if (!Interop.Advapi32.DuplicateTokenEx(_identity.AccessToken,
                                                  (uint)TokenAccessLevels.Query,
                                                  IntPtr.Zero,
                                                  (uint)TokenImpersonationLevel.Identification,
                                                  (uint)TokenType.TokenImpersonation,
                                                  ref token))
                    throw new SecurityException(new Win32Exception().Message);
            }

            bool isMember = false;

            // CheckTokenMembership will check if the SID is both present and enabled in the access token.
#if uap
            if (!Interop.Kernel32.CheckTokenMembershipEx((_identity.ImpersonationLevel != TokenImpersonationLevel.None ? _identity.AccessToken : token),
                                                  sid.BinaryForm,
                                                  Interop.Kernel32.CTMF_INCLUDE_APPCONTAINER,
                                                  ref isMember))
                throw new SecurityException(new Win32Exception().Message);
#else
            if (!Interop.Advapi32.CheckTokenMembership((_identity.ImpersonationLevel != TokenImpersonationLevel.None ? _identity.AccessToken : token),
                                                  sid.BinaryForm,
                                                  ref isMember))
                throw new SecurityException(new Win32Exception().Message);
#endif

            token.Dispose();
            return isMember;
        }
    }
}
