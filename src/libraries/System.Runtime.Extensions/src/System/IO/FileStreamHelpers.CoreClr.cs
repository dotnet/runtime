// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.IO
{
    internal static class FileStreamHelpers
    {
        public static Stream CreateFileStream(string path, bool write, bool append) =>
            new FileStream(
                path,
                write ? (append ? FileMode.Append : FileMode.Create) : FileMode.Open,
                write ? FileAccess.Write : FileAccess.Read,
                FileShare.Read,
                StreamReader.DefaultFileStreamBufferSize,
                FileOptions.SequentialScan);
    }
}
