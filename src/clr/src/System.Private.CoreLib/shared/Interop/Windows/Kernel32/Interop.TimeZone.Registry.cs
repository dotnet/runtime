// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable
using System;
using System.Runtime.InteropServices;

internal static partial class Interop
{
    internal static partial class Kernel32
    {
        [StructLayout(LayoutKind.Sequential)]
        internal struct REG_TZI_FORMAT
        {
            internal int Bias;
            internal int StandardBias;
            internal int DaylightBias;
            internal SYSTEMTIME StandardDate;
            internal SYSTEMTIME DaylightDate;

            internal REG_TZI_FORMAT(in TIME_ZONE_INFORMATION tzi)
            {
                Bias = tzi.Bias;
                StandardDate = tzi.StandardDate;
                StandardBias = tzi.StandardBias;
                DaylightDate = tzi.DaylightDate;
                DaylightBias = tzi.DaylightBias;
            }
        }
    }
}
