// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
//


#pragma once

#ifdef FEATURE_APPX

#include "clrtypes.h"
#include "appmodel.h"
#include "fusionsetup.h"

#define PACKAGE_FILTER_CLR_DEFAULT (PACKAGE_FILTER_HEAD|PACKAGE_FILTER_DIRECT)


typedef PACKAGE_INFO *              PPACKAGE_INFO;
typedef PACKAGE_INFO const *        PCPACKAGE_INFO;

//---------------------------------------------------------------------------------------------
// Forward declarations
template <typename T>
class NewArrayHolder;
BOOL WinRTSupported();


namespace AppX
{
    // Returns true if process is immersive (or if running in mockup environment).
    bool IsAppXProcess();

    // On CoreCLR, the host is in charge of determining whether the process is AppX or not.
    void SetIsAppXProcess(bool);

#ifdef DACCESS_COMPILE
        bool DacIsAppXProcess();
#endif // DACCESS_COMPILE
};


#else // FEATURE_APPX

namespace AppX
{
    inline bool IsAppXProcess()
    {
        return false;
    }
}

#endif // FEATURE_APPX
