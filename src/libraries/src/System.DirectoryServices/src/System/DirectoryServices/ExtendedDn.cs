// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.DirectoryServices
{
    /// <include file='doc\ExtendedDN.uex' path='docs/doc[@for="ExtendedDn"]/*' />
    /// <devdoc>
    ///    <para>
    ///       Specifies the possible representations of the distinguished name.
    ///    </para>
    /// </devdoc>
    public enum ExtendedDN
    {
        /// <include file='doc\ExtendedDn.uex' path='docs/doc[@for="ExtendedDn.None"]/*' />
    	None = -1,

        /// <include file='doc\ExtendedDn.uex' path='docs/doc[@for="ExtendedDn.HexString"]/*' />
    	HexString = 0,

        /// <include file='doc\ExtendedDn.uex' path='docs/doc[@for="ExtendedDn.Standard"]/*' />
    	Standard = 1
    }
}
