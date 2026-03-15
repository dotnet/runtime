// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.CompilerServices;
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
        internal struct TIME_DYNAMIC_ZONE_INFORMATION
        {
            [InlineArray(32)]
            internal struct NameBuffer { private char _element0; }

            [InlineArray(128)]
            internal struct TimeZoneKeyNameBuffer { private char _element0; }

            internal int Bias;
            internal NameBuffer StandardName;
            internal SYSTEMTIME StandardDate;
            internal int StandardBias;
            internal NameBuffer DaylightName;
            internal SYSTEMTIME DaylightDate;
            internal int DaylightBias;
            internal TimeZoneKeyNameBuffer TimeZoneKeyName;
            internal byte DynamicDaylightTimeDisabled;

            internal unsafe string GetTimeZoneKeyName()
            {
                fixed (TimeZoneKeyNameBuffer* p = &TimeZoneKeyName)
                    return new string((char*)p);
            }
        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        internal struct TIME_ZONE_INFORMATION
        {
            [InlineArray(32)]
            internal struct NameBuffer { private char _element0; }

            internal int Bias;
            internal NameBuffer StandardName;
            internal SYSTEMTIME StandardDate;
            internal int StandardBias;
            internal NameBuffer DaylightName;
            internal SYSTEMTIME DaylightDate;
            internal int DaylightBias;

            internal unsafe TIME_ZONE_INFORMATION(in TIME_DYNAMIC_ZONE_INFORMATION dtzi)
            {
                // The start of TIME_DYNAMIC_ZONE_INFORMATION has identical layout as TIME_ZONE_INFORMATION
                fixed (TIME_ZONE_INFORMATION* pTo = &this)
                fixed (TIME_DYNAMIC_ZONE_INFORMATION* pFrom = &dtzi)
                    *pTo = *(TIME_ZONE_INFORMATION*)pFrom;
            }

            internal unsafe string GetStandardName()
            {
                fixed (NameBuffer* p = &StandardName)
                    return new string((char*)p);
            }

            internal unsafe string GetDaylightName()
            {
                fixed (NameBuffer* p = &DaylightName)
                    return new string((char*)p);
            }
        }

        internal const uint TIME_ZONE_ID_INVALID = unchecked((uint)-1);

        [LibraryImport(Libraries.Kernel32, SetLastError = true)]
        internal static partial uint GetDynamicTimeZoneInformation(out TIME_DYNAMIC_ZONE_INFORMATION pTimeZoneInformation);

        [LibraryImport(Libraries.Kernel32, SetLastError = true)]
        internal static partial uint GetTimeZoneInformation(out TIME_ZONE_INFORMATION lpTimeZoneInformation);
    }
}
