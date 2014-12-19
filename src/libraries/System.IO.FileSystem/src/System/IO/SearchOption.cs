// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

namespace System.IO
{
    /// <devdoc>
    ///   <para>Enum describing whether the search operation should
    ///   retrieve files/directories from the current directory alone
    ///   or should include all the subdirectories also.</para>
    /// </devdoc>
    [System.Runtime.InteropServices.ComVisible(true)]
    public enum SearchOption
    {
        /// <devdoc>
        ///   <para>Include only the current directory in the search operation</para>
        /// </devdoc>
        TopDirectoryOnly,

        /// <devdoc>
        ///   <para>Include the current directory and all the sub-directories
        ///   underneath it including reparse points in the search operation. 
        ///   This will traverse reparse points (i.e, mounted points and symbolic links)
        ///   recursively. If the directory structure searched contains a loop
        ///   because of hard links, the search operation will go on for ever.</para>
        /// </devdoc>
        AllDirectories,
    }
}
