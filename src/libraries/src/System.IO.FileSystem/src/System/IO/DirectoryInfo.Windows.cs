// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.IO.Enumeration;

namespace System.IO
{
    partial class DirectoryInfo
    {
        internal static unsafe DirectoryInfo Create(string fullPath, ref FileSystemEntry findData)
        {
            DirectoryInfo info = new DirectoryInfo(fullPath, fileName: findData.FileName.GetStringFromFixedBuffer(), isNormalized: true);
            info.Init(findData._info);
            return info;
        }
    }
}
