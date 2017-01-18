// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.DirectoryServices
{
    /// <include file='doc\ReferalChasingOption.uex' path='docs/doc[@for="ReferralChasingOption"]/*' />
    /// <devdoc>
    ///    <para>Specifies if and how referral chasing is pursued.</para>
    /// </devdoc>
    public enum ReferralChasingOption
    {
        /// <include file='doc\ReferalChasingOption.uex' path='docs/doc[@for="ReferralChasingOption.None"]/*' />
        /// <devdoc>
        ///    <para> Never chase the referred-to server. Setthing this option 
        ///       prevents a client from contacting other servers in a referral process.</para>
        /// </devdoc>
        None = 0,
        /// <include file='doc\ReferalChasingOption.uex' path='docs/doc[@for="ReferralChasingOption.Subordinate"]/*' />
        /// <devdoc>
        ///    <para>Chase only subordinate referrals which are a subordinate naming context in a 
        ///       directory tree. The ADSI LDAP provider always turns off this flag for paged
        ///       searches.</para>
        /// </devdoc>
        Subordinate = 0x20,
        /// <include file='doc\ReferalChasingOption.uex' path='docs/doc[@for="ReferralChasingOption.External"]/*' />
        /// <devdoc>
        ///    <para>Chase external referrals.</para>
        /// </devdoc>
        External = 0x40,
        /// <include file='doc\ReferalChasingOption.uex' path='docs/doc[@for="ReferralChasingOption.All"]/*' />
        /// <devdoc>
        ///    <para>Chase referrals of either the subordinate or external type.</para>
        /// </devdoc>
        All = Subordinate | External
    }
}
