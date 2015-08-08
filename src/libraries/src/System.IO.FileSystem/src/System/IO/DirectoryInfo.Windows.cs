// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics;
using System.Security;

namespace System.IO
{
    partial class DirectoryInfo
    {
        [SecurityCritical]
        internal DirectoryInfo(string fullPath, ref Interop.mincore.WIN32_FIND_DATA findData)
            : this(fullPath, findData.cFileName)
        {
            Debug.Assert(string.Equals(findData.cFileName, Path.GetFileName(fullPath), StringComparison.Ordinal));
            Init(ref findData);
        }
    }
}
