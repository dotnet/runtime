// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.IO
{
    /// <devdoc>
    ///    Changes that may occur to a file or directory.
    /// </devdoc>
    [Flags()]
    public enum WatcherChangeTypes
    {
        /// <devdoc>
        ///    The creation of a file or folder.
        /// </devdoc>
        Created = 1,
        /// <devdoc>
        ///    The deletion of a file or folder.
        /// </devdoc>
        Deleted = 2,
        /// <devdoc>
        ///    The change of a file or folder.
        /// </devdoc>
        Changed = 4,
        /// <devdoc>
        ///    The renaming of a file or folder.
        /// </devdoc>
        Renamed = 8,
        // all of the above
        /// <devdoc>
        ///    [To be supplied.]
        /// </devdoc>
        All = Created | Deleted | Changed | Renamed
    }
}
