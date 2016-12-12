// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.DirectoryServices
{
    /// <include file='doc\SearchScope.uex' path='docs/doc[@for="SearchScope"]/*' />
    /// <devdoc>
    ///    <para>Specifies the scope of a directory search.</para>
    /// </devdoc>
    public enum SearchScope
    {
        /// <include file='doc\SearchScope.uex' path='docs/doc[@for="SearchScope.Base"]/*' />
        /// <devdoc>
        ///    <para>Limits the search to the base object. The result contains at most one object. </para>
        /// </devdoc>
        Base = 0,
        /// <include file='doc\SearchScope.uex' path='docs/doc[@for="SearchScope.OneLevel"]/*' />
        /// <devdoc>
        ///    <para>Searched one level of the immediate children, excluding the base object.</para>
        /// </devdoc>
        OneLevel = 1,
        /// <include file='doc\SearchScope.uex' path='docs/doc[@for="SearchScope.Subtree"]/*' />
        /// <devdoc>
        ///    <para>Searches the whole subtree, including all the children and the base object 
        ///       itself.</para>
        /// </devdoc>
        Subtree = 2
    }
}
