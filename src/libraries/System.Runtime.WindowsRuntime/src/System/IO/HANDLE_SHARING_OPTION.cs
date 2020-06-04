// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.IO
{
    [Flags]
    internal enum HANDLE_SHARING_OPTIONS : uint
    {
        HSO_SHARE_NONE = 0,
        HSO_SHARE_READ = 0x1,
        HSO_SHARE_WRITE = 0x2,
        HSO_SHARE_DELETE = 0x4
    }
}
