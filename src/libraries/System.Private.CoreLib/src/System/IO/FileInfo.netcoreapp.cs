// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.IO
{
    public sealed partial class FileInfo : FileSystemInfo
    {
        public FileStream Open(FileStreamOptions options) => File.Open(NormalizedPath, options);
    }
}
