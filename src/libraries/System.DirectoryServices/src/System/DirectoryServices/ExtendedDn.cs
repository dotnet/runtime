//------------------------------------------------------------------------------
// <copyright file="ExtendedDn.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>                                                                
//------------------------------------------------------------------------------

namespace System.DirectoryServices {

    /// <include file='doc\ExtendedDN.uex' path='docs/doc[@for="ExtendedDn"]/*' />
    /// <devdoc>
    ///    <para>
    ///       Specifies the possible representations of the distinguished name.
    ///    </para>
    /// </devdoc>
    public enum ExtendedDN {

        /// <include file='doc\ExtendedDn.uex' path='docs/doc[@for="ExtendedDn.None"]/*' />
    	None = -1,

        /// <include file='doc\ExtendedDn.uex' path='docs/doc[@for="ExtendedDn.HexString"]/*' />
    	HexString = 0,

        /// <include file='doc\ExtendedDn.uex' path='docs/doc[@for="ExtendedDn.Standard"]/*' />
    	Standard = 1
    }
}
