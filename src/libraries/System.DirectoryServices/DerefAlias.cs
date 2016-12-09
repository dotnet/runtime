//------------------------------------------------------------------------------
// <copyright file="DerefAlias.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>                                                                
//------------------------------------------------------------------------------

namespace System.DirectoryServices {    

    /// <include file='doc\DerefAlias.uex' path='docs/doc[@for="DerefAlias"]/*' />
    /// <devdoc>
    ///    <para>
    ///       Specifies the behavior in which aliases are dereferenced.
    ///    </para>
    /// </devdoc>
    public enum DereferenceAlias {
    
        /// <include file='doc\DerefAlias.uex' path='docs/doc[@for="DerefAlias.Never"]/*' />
    	Never = 0,

        /// <include file='doc\DerefAlias.uex' path='docs/doc[@for="DerefAlias.Searching"]/*' />
    	InSearching = 1,

        /// <include file='doc\DerefAlias.uex' path='docs/doc[@for="DerefAlias.Finding"]/*' />
        FindingBaseObject = 2,

        /// <include file='doc\DerefAlias.uex' path='docs/doc[@for="DerefAlias.Always"]/*' />
        Always = 3
    }
    
}
