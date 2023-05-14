// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.IO
{
    internal static partial class FileSystem
    {
        public static partial void CopyFile(string sourceFullPath, string destFullPath, bool overwrite)
        {
            using StartedCopyFileState startedCopyFile = StartCopyFile(sourceFullPath, destFullPath, overwrite);

            // Copy the file using the standard unix implementation
            StandardCopyFile(startedCopyFile);
        }
    }
}
