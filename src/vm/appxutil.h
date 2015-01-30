//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//
//

//
// Provides VM-specific AppX utility code.

#ifndef vm_AppXUtil_h
#define vm_AppXUtil_h

#include "../inc/appxutil.h"

namespace AppX
{
#if defined(FEATURE_APPX) && !defined(CROSSGEN_COMPILE) && !defined(CLR_STANDALONE_BINDER)
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
