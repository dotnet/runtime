// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.DirectoryServices
{
    /// <devdoc>
    /// Specifies the scope of a directory search.
    /// </devdoc>
    public enum SearchScope
    {
        /// <devdoc>
        /// Limits the search to the base object. The result contains at most one object.
        /// </devdoc>
        Base = 0,

        /// <devdoc>
        /// Searched one level of the immediate children, excluding the base object.
        /// </devdoc>
        OneLevel = 1,

        /// <devdoc>
        /// Searches the whole subtree, including all the children and the base object itself.
        /// </devdoc>
        Subtree = 2
    }
}
