// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

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
    }
}
