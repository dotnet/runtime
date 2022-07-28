// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// The enumeration constants used in CultureInfo.GetCultures().
// On Linux platforms, the only enum values used there is NeutralCultures and SpecificCultures
// the rest are obsolete or not valid on Linux

namespace System.Globalization
{
    [Flags]
    public enum CultureTypes
    {
        NeutralCultures = 0x0001, // Neutral cultures are cultures like "en", "de", "zh", etc, for enumeration this includes ALL neutrals regardless of other flags
        SpecificCultures = 0x0002, // Non-netural cultuers.  Examples are "en-us", "zh-tw", etc., for enumeration this includes ALL specifics regardless of other flags
        InstalledWin32Cultures = 0x0004, // Win32 installed cultures in the system and exists in the framework too., this is effectively all cultures

        AllCultures = NeutralCultures | SpecificCultures | InstalledWin32Cultures,

        UserCustomCulture = 0x0008, // User defined custom culture
        ReplacementCultures = 0x0010, // User defined replacement custom culture.
        [Obsolete("CultureTypes.WindowsOnlyCultures has been deprecated. Use other values in CultureTypes instead.")]
        WindowsOnlyCultures = 0x0020, // this will always return empty list.
        [Obsolete("CultureTypes.FrameworkCultures has been deprecated. Use other values in CultureTypes instead.")]
        FrameworkCultures = 0x0040, // will return only the v2 cultures marked as Framework culture.
    }
}
