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
        internal struct SYSTEMTIME
        {
            internal ushort Year;
            internal ushort Month;
            internal ushort DayOfWeek;
            internal ushort Day;
            internal ushort Hour;
            internal ushort Minute;
            internal ushort Second;
            internal ushort Milliseconds;

            internal bool Equals(in SYSTEMTIME other) =>
                    Year == other.Year &&
                    Month == other.Month &&
                    DayOfWeek == other.DayOfWeek &&
                    Day == other.Day &&
                    Hour == other.Hour &&
                    Minute == other.Minute &&
                    Second == other.Second &&
                    Milliseconds == other.Milliseconds;
        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        internal unsafe struct TIME_DYNAMIC_ZONE_INFORMATION
        {
            internal int Bias;
            internal fixed char StandardName[32];
            internal SYSTEMTIME StandardDate;
            internal int StandardBias;
            internal fixed char DaylightName[32];
            internal SYSTEMTIME DaylightDate;
            internal int DaylightBias;
            internal fixed char TimeZoneKeyName[128];
            internal byte DynamicDaylightTimeDisabled;

            internal string GetTimeZoneKeyName()
            {
                fixed (char* p = TimeZoneKeyName)
                    return new string(p);
            }
        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        internal unsafe struct TIME_ZONE_INFORMATION
        {
            internal int Bias;
            internal fixed char StandardName[32];
            internal SYSTEMTIME StandardDate;
            internal int StandardBias;
            internal fixed char DaylightName[32];
            internal SYSTEMTIME DaylightDate;
            internal int DaylightBias;

            internal TIME_ZONE_INFORMATION(in TIME_DYNAMIC_ZONE_INFORMATION dtzi)
            {
                // The start of TIME_DYNAMIC_ZONE_INFORMATION has identical layout as TIME_ZONE_INFORMATION
                fixed (TIME_ZONE_INFORMATION* pTo = &this)
                fixed (TIME_DYNAMIC_ZONE_INFORMATION* pFrom = &dtzi)
                    *pTo = *(TIME_ZONE_INFORMATION*)pFrom;
            }

            internal string GetStandardName()
            {
                fixed (char* p = StandardName)
                    return new string(p);
            }

            internal string GetDaylightName()
            {
                fixed (char* p = DaylightName)
                    return new string(p);
            }
        }

        internal const uint TIME_ZONE_ID_INVALID = unchecked((uint)-1);

        [DllImport(Libraries.Kernel32, CharSet = CharSet.Unicode, SetLastError = true, ExactSpelling = true)]
        internal static extern uint GetDynamicTimeZoneInformation(out TIME_DYNAMIC_ZONE_INFORMATION pTimeZoneInformation);

        [DllImport(Libraries.Kernel32, CharSet = CharSet.Unicode, SetLastError = true, ExactSpelling = true)]
        internal static extern uint GetTimeZoneInformation(out TIME_ZONE_INFORMATION lpTimeZoneInformation);
    }
}
