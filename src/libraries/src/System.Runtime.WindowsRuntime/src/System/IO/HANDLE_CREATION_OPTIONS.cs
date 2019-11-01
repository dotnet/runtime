// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.IO
{
    internal enum HANDLE_CREATION_OPTIONS : uint
    {
        HCO_CREATE_NEW = 0x1,
        HCO_CREATE_ALWAYS = 0x2,
        HCO_OPEN_EXISTING = 0x3,
        HCO_OPEN_ALWAYS = 0x4,
        HCO_TRUNCATE_EXISTING = 0x5,
    }
}
