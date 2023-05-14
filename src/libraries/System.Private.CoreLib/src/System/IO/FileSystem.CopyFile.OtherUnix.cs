// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.IO
{
    internal static partial class FileSystem
    {
        public static partial void CopyFile(string sourceFullPath, string destFullPath, bool overwrite)
        {
            var (fileLength, _, _, src, dst) = StartCopyFile(sourceFullPath, destFullPath, overwrite);

            try
            {
                // Copy the file using the standard unix implementation
                // dst! because dst is not null if StartCopyFile's openDst is true (which is the default value)
                StandardCopyFile(src, dst!, fileLength);
            }
            finally
            {
                // Dipose relevant file handles
                src.Dispose();
                dst?.Dispose();
            }
        }
    }
}
