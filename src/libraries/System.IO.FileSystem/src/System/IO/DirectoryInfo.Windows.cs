// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;
using System.Security;

namespace System.IO
{
    partial class DirectoryInfo
    {
        [SecurityCritical]
        internal DirectoryInfo(string fullPath, ref Interop.Kernel32.WIN32_FIND_DATA findData)
            : this(fullPath, findData.cFileName)
        {
            Debug.Assert(string.Equals(findData.cFileName, Path.GetFileName(fullPath), StringComparison.Ordinal));
            Init(ref findData);
        }
    }
}
