// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

/*============================================================
**
**
**
** Purpose: Defines IDs for supported platforms
**
**
===========================================================*/
namespace System {

    [Serializable]
[System.Runtime.InteropServices.ComVisible(true)]
    public enum PlatformID
    {
        Win32S        = 0,
        Win32Windows  = 1,
        Win32NT       = 2,
        WinCE         = 3,      
        Unix          = 4,
        Xbox          = 5,
#if !FEATURE_LEGACYNETCF
        MacOSX        = 6
#else // FEATURE_LEGACYNETCF
        NokiaS60      = 6
#endif // FEATURE_LEGACYNETCF
    }

}
