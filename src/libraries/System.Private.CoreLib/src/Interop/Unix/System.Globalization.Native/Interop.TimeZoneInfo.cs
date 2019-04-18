// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable
using System.Runtime.InteropServices;
using System.Text;

internal static partial class Interop
{
    internal static partial class Globalization
    {
        // needs to be kept in sync with TimeZoneDisplayNameType in System.Globalization.Native
        internal enum TimeZoneDisplayNameType
        {
            Generic = 0,
            Standard = 1,
            DaylightSavings = 2,
        }

        [DllImport(Libraries.GlobalizationNative, CharSet = CharSet.Unicode, EntryPoint = "GlobalizationNative_GetTimeZoneDisplayName")]
        internal static extern unsafe ResultCode GetTimeZoneDisplayName(
            string localeName, 
            string timeZoneId, 
            TimeZoneDisplayNameType type, 
            char* result, 
            int resultLength);
    }
}
