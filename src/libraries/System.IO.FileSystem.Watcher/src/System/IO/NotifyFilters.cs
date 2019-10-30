// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.IO
{
    /// <devdoc>
    ///    Specifies the changes to watch for in a file or folder.
    /// </devdoc>
    [Flags]
    public enum NotifyFilters
    {
        /// <devdoc>
        ///    [To be supplied.]
        /// </devdoc>
        FileName = 0x00000001,
        /// <devdoc>
        ///    [To be supplied.]
        /// </devdoc>
        DirectoryName = 0x00000002,
        /// <devdoc>
        ///    The attributes of the file or folder.
        /// </devdoc>
        Attributes = 0x00000004,
        /// <devdoc>
        ///    The size of the file or folder.
        /// </devdoc>
        Size = 0x00000008,
        /// <devdoc>
        ///       The date that the file or folder last had anything written to it.
        /// </devdoc>
        LastWrite = 0x00000010,
        /// <devdoc>
        ///    The date that the file or folder was last opened.
        /// </devdoc>
        LastAccess = 0x00000020,
        /// <devdoc>
        ///    [To be supplied.]
        /// </devdoc>
        CreationTime = 0x00000040,
        /// <devdoc>
        ///    The security settings of the file or folder.
        /// </devdoc>
        Security = 0x00000100,
    }
}
