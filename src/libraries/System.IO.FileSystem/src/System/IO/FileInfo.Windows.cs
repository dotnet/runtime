// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;

namespace System.IO
{
    partial class FileInfo
    {
        internal unsafe FileInfo(string fullPath, string fileName, ref RawFindData findData)
            : this(fullPath, fileName: fileName, isNormalized: true)
        {
            Debug.Assert(fileName.Equals(Path.GetFileName(fullPath)));
            Init(findData._info);
        }
    }
}
