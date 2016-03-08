// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
//

//
// Provides VM-specific AppX utility code.

#ifndef vm_AppXUtil_h
#define vm_AppXUtil_h

#include "../inc/appxutil.h"

namespace AppX
{
#if defined(FEATURE_APPX) && !defined(CROSSGEN_COMPILE)
    //-----------------------------------------------------------------------------------
    // Returns true if running in an AppX process with Designer Mode enabled.
    bool IsAppXDesignMode();

    // Return Application.Id
    HRESULT GetApplicationId(LPCWSTR& rString);
#else // FEATURE_APPX
    inline bool IsAppXDesignMode()
    {
        return false;
    }
#endif // FEATURE_APPX && !CROSSGEN_COMPILE
}

#endif // vm_AppXUtil_h
