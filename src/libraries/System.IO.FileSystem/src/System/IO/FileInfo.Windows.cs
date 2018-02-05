// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.IO.Enumeration;

namespace System.IO
{
    partial class FileInfo
    {
        internal static unsafe FileInfo Create(string fullPath, ref FileSystemEntry findData)
        {
            FileInfo info = new FileInfo(fullPath, fileName: findData.FileName.GetStringFromFixedBuffer(), isNormalized: true);
            info.Init(findData._info);
            return info;
        }
    }
}
