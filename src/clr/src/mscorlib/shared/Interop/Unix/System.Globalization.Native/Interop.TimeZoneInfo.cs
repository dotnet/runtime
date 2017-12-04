// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Runtime.InteropServices;
using System.Text;

internal static partial class Interop
{
    internal static partial class GlobalizationInterop
    {
        // needs to be kept in sync with TimeZoneDisplayNameType in System.Globalization.Native
        internal enum TimeZoneDisplayNameType
        {
            Generic = 0,
            Standard = 1,
            DaylightSavings = 2,
        }

        [DllImport(Libraries.GlobalizationInterop, CharSet = CharSet.Unicode, EntryPoint = "GlobalizationNative_GetTimeZoneDisplayName")]
        internal static extern ResultCode GetTimeZoneDisplayName(
            string localeName, 
            string timeZoneId, 
            TimeZoneDisplayNameType type, 
            [Out] StringBuilder result, 
            int resultLength);
    }
}
