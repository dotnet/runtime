// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.IO
{
    public sealed class FileStreamOptions
    {
        /// <summary>
        /// One of the enumeration values that determines how to open or create the file.
        /// </summary>
        public FileMode Mode { get; set; }
        /// <summary>
        /// A bitwise combination of the enumeration values that determines how the file can be accessed by the <see cref="FileStream" /> object. This also determines the values returned by the <see cref="System.IO.FileStream.CanRead" /> and <see cref="System.IO.FileStream.CanWrite" /> properties of the <see cref="FileStream" /> object.
        /// </summary>
        public FileAccess Access { get; set; } = FileAccess.Read;
        /// <summary>
        /// A bitwise combination of the enumeration values that determines how the file will be shared by processes. The default value is <see cref="System.IO.FileShare.Read" />.
        /// </summary>
        public FileShare Share { get; set; } = FileStream.DefaultShare;
        /// <summary>
        /// A bitwise combination of the enumeration values that specifies additional file options. The default value is <see cref="System.IO.FileOptions.None" />, which indicates synchronous IO.
        /// </summary>
        public FileOptions Options { get; set; }
        /// <summary>
        /// The initial allocation size in bytes for the file. A positive value is effective only when a regular file is being created, overwritten, or replaced.
        /// When the value is negative, the <see cref="FileStream" /> constructor throws an <see cref="ArgumentOutOfRangeException" />.
        /// In other cases (including the default 0 value), it's ignored.
        /// </summary>
        public long PreallocationSize { get; set; }
        /// <summary>
        /// The size of the buffer used by <see cref="FileStream" /> for buffering. The default buffer size is 4096.
        /// 0 or 1 means that buffering should be disabled. Negative values are not allowed.
        /// </summary>
        public int BufferSize { get; set; } = FileStream.DefaultBufferSize;
    }
}
