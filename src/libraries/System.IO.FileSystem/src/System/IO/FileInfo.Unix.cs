// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.IO
{
    partial class FileInfo
    {
        internal static unsafe FileInfo Create(string fullPath, string fileName, ref FileStatus fileStatus)
        {
            FileInfo info = new FileInfo(fullPath, fileName: fileName, isNormalized: true);
            info.Init(ref fileStatus);
            return info;
        }
    }
}
