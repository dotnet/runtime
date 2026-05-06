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
            internal struct NameBuffer
            {
                private char _element0;
            }

            [InlineArray(128)]
            internal struct TimeZoneKeyNameBuffer
            {
                private char _element0;
            }

            internal int Bias;
            internal NameBuffer StandardName;
            internal SYSTEMTIME StandardDate;
            internal int StandardBias;
            internal NameBuffer DaylightName;
            internal SYSTEMTIME DaylightDate;
            internal int DaylightBias;
            internal TimeZoneKeyNameBuffer TimeZoneKeyName;
            internal byte DynamicDaylightTimeDisabled;

            internal string GetTimeZoneKeyName()
            {
                ReadOnlySpan<char> span = TimeZoneKeyName;
                int idx = span.IndexOf('\0');
                return new string(idx >= 0 ? span[..idx] : span);
            }
        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        internal struct TIME_ZONE_INFORMATION
        {
            [InlineArray(32)]
            internal struct NameBuffer
            {
                private char _element0;
            }

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

            internal string GetStandardName()
            {
                ReadOnlySpan<char> span = StandardName;
                int idx = span.IndexOf('\0');
                return new string(idx >= 0 ? span[..idx] : span);
            }

            internal string GetDaylightName()
            {
                ReadOnlySpan<char> span = DaylightName;
                int idx = span.IndexOf('\0');
                return new string(idx >= 0 ? span[..idx] : span);
            }
        }

        internal const uint TIME_ZONE_ID_INVALID = unchecked((uint)-1);

        [LibraryImport(Libraries.Kernel32, SetLastError = true)]
        internal static partial uint GetDynamicTimeZoneInformation(out TIME_DYNAMIC_ZONE_INFORMATION pTimeZoneInformation);

        [LibraryImport(Libraries.Kernel32, SetLastError = true)]
        internal static partial uint GetTimeZoneInformation(out TIME_ZONE_INFORMATION lpTimeZoneInformation);
    }
}
