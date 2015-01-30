// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

/*============================================================
**
**
**
** Purpose: Defines flags for supported platforms for use
**          with the PlatformsSupportedAttribute
**
**
===========================================================*/
#if FEATURE_CORECLR
namespace System
{

[Serializable]
    [Flags]
    public enum Platforms
    {
        Win32S = 1 << PlatformID.Win32S,
        Win32Windows = 1 << PlatformID.Win32Windows,
        Win32NT = 1 << PlatformID.Win32NT,
        WinCE = 1 << PlatformID.WinCE,
        Unix = 1 << PlatformID.Unix,
        Xbox = 1 << PlatformID.Xbox,
#if !FEATURE_LEGACYNETCF
        MacOSX = 1 << PlatformID.MacOSX,
#else // FEATURE_LEGACYNETCF
        NokiaS60 = 1 << PlatformID.NokiaS60,
#endif // FEATURE_LEGACYNETCF

        All = Win32S | Win32Windows | Win32NT | WinCE | Unix | Xbox 
#if !FEATURE_LEGACYNETCF
| MacOSX
#else // FEATURE_LEGACYNETCF
| NokiaS60
#endif // FEATURE_LEGACYNETCF
    }

}
#endif // FEATURE_CORECLR