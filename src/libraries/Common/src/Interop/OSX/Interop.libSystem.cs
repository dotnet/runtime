// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.InteropServices;

internal static partial class Interop
{
    internal static partial class libSystem
    {
        [LibraryImport(Interop.Libraries.libSystem)]
        public static unsafe partial int mach_timebase_info(mach_timebase_info_data_t* info);
        public struct mach_timebase_info_data_t
        {
            public uint numer;
            public uint denom;
        }
    }
}
