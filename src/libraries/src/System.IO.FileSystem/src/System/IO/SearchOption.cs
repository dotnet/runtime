// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;

namespace System.IO
{
    /// <devdoc>
    ///   Enum describing whether the search operation should
    ///   retrieve files/directories from the current directory alone
    ///   or should include all the subdirectories also.
    /// </devdoc>
    [Serializable]
    public enum SearchOption
    {
        /// <devdoc>
        ///   Include only the current directory in the search operation
        /// </devdoc>
        TopDirectoryOnly,

        /// <devdoc>
        ///   Include the current directory and all the sub-directories
        ///   underneath it including reparse points in the search operation. 
        ///   This will traverse reparse points (i.e, mounted points and symbolic links)
        ///   recursively. If the directory structure searched contains a loop
        ///   because of hard links, the search operation will go on for ever.
        /// </devdoc>
        AllDirectories,
    }
}
