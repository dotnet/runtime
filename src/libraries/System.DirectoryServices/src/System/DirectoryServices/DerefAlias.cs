// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.DirectoryServices
{
    /// <include file='doc\DerefAlias.uex' path='docs/doc[@for="DerefAlias"]/*' />
    /// <devdoc>
    ///    <para>
    ///       Specifies the behavior in which aliases are dereferenced.
    ///    </para>
    /// </devdoc>
    public enum DereferenceAlias
    {
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
