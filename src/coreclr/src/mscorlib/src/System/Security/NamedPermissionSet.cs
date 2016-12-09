// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

// 
//
//  Extends PermissionSet to allow an associated name and description
//

namespace System.Security
{
    using System;
    using System.Security.Permissions;

    [Serializable]
    [System.Runtime.InteropServices.ComVisible(true)]
    public sealed class NamedPermissionSet : PermissionSet
    {
        internal static PermissionSet GetBuiltInSet(string name)
        {
            // Used by PermissionSetAttribute to create one of the built-in,
            // immutable permission sets.
            if (name == null)
                return null;
            else if (name.Equals("FullTrust"))
                return CreateFullTrustSet();
            else if (name.Equals("Nothing"))
                return CreateNothingSet();
            else if (name.Equals("Execution"))
                return CreateExecutionSet();
            else if (name.Equals("SkipVerification"))
                return CreateSkipVerificationSet();
            else if (name.Equals("Internet"))
                return CreateInternetSet();
            else
                return null;
        }

        private static PermissionSet CreateFullTrustSet() {
            return new PermissionSet(PermissionState.Unrestricted);
        }

        private static PermissionSet CreateNothingSet() {
            return new PermissionSet(PermissionState.None);
        }

        private static PermissionSet CreateExecutionSet() {
            PermissionSet permSet = new PermissionSet(PermissionState.None);
#pragma warning disable 618
            permSet.AddPermission(new SecurityPermission(SecurityPermissionFlag.Execution));
#pragma warning restore 618
            return permSet;
        }

        private static PermissionSet CreateSkipVerificationSet() {
            PermissionSet permSet = new PermissionSet(PermissionState.None);
#pragma warning disable 618
            permSet.AddPermission(new SecurityPermission(SecurityPermissionFlag.SkipVerification));
#pragma warning restore 618
            return permSet;
        }

        private static PermissionSet CreateInternetSet() {
            PermissionSet permSet = new PermissionSet(PermissionState.None);
            permSet.AddPermission(new FileDialogPermission(FileDialogPermissionAccess.Open));
#pragma warning disable 618
            permSet.AddPermission(new SecurityPermission(SecurityPermissionFlag.Execution));
#pragma warning restore 618
            permSet.AddPermission(new UIPermission(UIPermissionWindow.SafeTopLevelWindows, UIPermissionClipboard.OwnClipboard));
            return permSet;
            

        }
    }
}
