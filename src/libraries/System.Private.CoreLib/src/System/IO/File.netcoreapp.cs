// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.IO
{
    public static partial class File
    {
        public static FileStream Open(string path, FileStreamOptions options) => new FileStream(path, options);
    }
}
