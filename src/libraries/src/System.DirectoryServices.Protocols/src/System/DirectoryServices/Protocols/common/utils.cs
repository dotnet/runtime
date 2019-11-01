// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Runtime.InteropServices;

namespace System.DirectoryServices.Protocols
{
    internal class Utility
    {
        internal static bool IsLdapError(LdapError error)
        {
            if (error == LdapError.IsLeaf || error == LdapError.InvalidCredentials || error == LdapError.SendTimeOut)
            {
                return true;
            }

            return (error >= LdapError.ServerDown && error <= LdapError.ReferralLimitExceeded);
        }

        internal static bool IsResultCode(ResultCode code)
        {
            if (code >= ResultCode.Success && code <= ResultCode.SaslBindInProgress)
            {
                return true;
            }

            if (code >= ResultCode.NoSuchAttribute && code <= ResultCode.InvalidAttributeSyntax)
            {
                return true;
            }

            if (code >= ResultCode.NoSuchObject && code <= ResultCode.InvalidDNSyntax)
            {
                return true;
            }

            if (code >= ResultCode.InsufficientAccessRights && code <= ResultCode.LoopDetect)
            {
                return true;
            }

            if (code >= ResultCode.NamingViolation && code <= ResultCode.AffectsMultipleDsas)
            {
                return true;
            }

            return (code == ResultCode.AliasDereferencingProblem || code == ResultCode.InappropriateAuthentication || code == ResultCode.SortControlMissing || code == ResultCode.OffsetRangeError || code == ResultCode.VirtualListViewError || code == ResultCode.Other);
        }

        internal static IntPtr AllocHGlobalIntPtrArray(int size)
        {
            checked
            {
                IntPtr intPtrArray = Marshal.AllocHGlobal(Marshal.SizeOf(typeof(IntPtr)) * size);
                for (int i = 0; i < size; i++)
                {
                    IntPtr tempPtr = (IntPtr)((long)intPtrArray + Marshal.SizeOf(typeof(IntPtr)) * i);
                    Marshal.WriteIntPtr(tempPtr, IntPtr.Zero);
                }
                return intPtrArray;
            }
        }
    }
}
