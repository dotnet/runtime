// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;

namespace System.IO
{
    /// <devdoc>
    ///   Defines constants for read, write, or read/write
    ///   access to a file.
    /// </devdoc>
    [Flags]
    public enum FileAccess
    {
        /// <devdoc>
        ///   Specifies read access to the file.  Data can be read from the file and
        ///   the file pointer can be moved.  Combine with WRITE for read-write access.
        /// </devdoc>
        Read = 1,

        /// <devdoc>
        ///   Specifies write access to the file.  Data can be written to the file and
        ///   the file pointer can be moved.  Combine with READ for read-write access.
        /// </devdoc>
        Write = 2,

        /// <devdoc>
        ///   Specifies read and write access to the file.  Data can be written to the
        ///   file and the file pointer can be moved.  Data can also be read from the
        ///   file.
        /// </devdoc>
        ReadWrite = 3
    }
}
