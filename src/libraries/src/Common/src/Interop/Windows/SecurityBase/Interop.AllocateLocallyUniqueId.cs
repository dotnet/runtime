// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Runtime.InteropServices;

internal partial class Interop
{
    internal partial class SecurityBase
    {
        [DllImport(Libraries.SecurityBase)]
        internal static extern bool AllocateLocallyUniqueId(out LUID Luid);
    }
}

