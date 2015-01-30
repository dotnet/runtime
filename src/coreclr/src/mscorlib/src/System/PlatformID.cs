// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

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
