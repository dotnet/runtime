// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

namespace System.IO
{
    /// <devdoc>
    ///   <para>Defines constants for read, write, or read/write
    ///   access to a file.</para>
    /// <devdoc>
    [Flags]
    [System.Runtime.InteropServices.ComVisible(true)]
    public enum FileAccess
    {
        /// <devdoc>
        ///   <paraSpecifies read access to the file.  Data can be read from the file and
        ///   the file pointer can be moved.  Combine with WRITE for read-write access.<para>
        /// </devdoc>
        Read = 1,

        /// <devdoc>
        ///   <para>Specifies write access to the file.  Data can be written to the file and
        ///   the file pointer can be moved.  Combine with READ for read-write access.<para>
        /// </devdoc>
        Write = 2,

        /// <devdoc>
        ///   <para>Specifies read and write access to the file.  Data can be written to the
        ///   file and the file pointer can be moved.  Data can also be read from the
        ///   file.<para>
        /// </devdoc>
        ReadWrite = 3
    }
}