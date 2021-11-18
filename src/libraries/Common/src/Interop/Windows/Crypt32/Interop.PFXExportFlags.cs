// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

internal static partial class Interop
{
    internal static partial class Crypt32
    {
        [Flags]
        internal enum PFXExportFlags : int
        {
            REPORT_NO_PRIVATE_KEY                 = 0x00000001,
            REPORT_NOT_ABLE_TO_EXPORT_PRIVATE_KEY = 0x00000002,
            EXPORT_PRIVATE_KEYS                   = 0x00000004,
            None                                  = 0x00000000,
        }
    }
}
