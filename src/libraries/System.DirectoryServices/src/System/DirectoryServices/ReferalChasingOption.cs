// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.DirectoryServices
{
    /// <devdoc>
    /// Specifies if and how referral chasing is pursued.
    /// </devdoc>
    public enum ReferralChasingOption
    {
        /// <devdoc>
        /// Never chase the referred-to server. Setting this option 
        /// prevents a client from contacting other servers in a referral process.
        /// </devdoc>
        None = 0,

        /// <devdoc>
        /// Chase only subordinate referrals which are a subordinate naming context in a 
        /// directory tree. The ADSI LDAP provider always turns off this flag for paged
        /// searches.
        /// </devdoc>
        Subordinate = 0x20,

        /// <devdoc>
        /// Chase external referrals.
        /// </devdoc>
        External = 0x40,

        /// <devdoc>
        /// Chase referrals of either the subordinate or external type.
        /// </devdoc>
        All = Subordinate | External
    }
}
