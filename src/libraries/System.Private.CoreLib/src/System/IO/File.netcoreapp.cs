// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.IO
{
    public static partial class File
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="System.IO.FileStream" /> class with the specified path, creation mode, read/write and sharing permission, the access other FileStreams can have to the same file, the buffer size, additional file options and the allocation size.
        /// </summary>
        /// <remarks><see cref="System.IO.FileStream(string,System.IO.FileStreamOptions)"/> for information about exceptions.</remarks>
        public static FileStream Open(string path, FileStreamOptions options) => new FileStream(path, options);
    }
}
