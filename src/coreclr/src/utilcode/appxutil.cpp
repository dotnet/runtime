// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
//


#include "stdafx.h"

#include <strsafe.h>

#include "utilcode.h"
#include "holder.h"
#include "volatile.h"
#include "clr/fs.h"
#include "clr/str.h"

#include "appxutil.h"
#include "ex.h"

#include "shlwapi.h"    // Path manipulation APIs


GVAL_IMPL(bool, g_fAppX);
INDEBUG(bool g_fIsAppXAsked;)

namespace AppX
{
#ifdef DACCESS_COMPILE
    bool DacIsAppXProcess()
    {
        return g_fAppX;
    }
#else

    // Returns true if host has deemed the process to be appx
    bool IsAppXProcess()
    {
        INDEBUG(g_fIsAppXAsked = true;)
        return g_fAppX;
    }


    void SetIsAppXProcess(bool value)
    {
        _ASSERTE(!g_fIsAppXAsked);
        g_fAppX = value;
    }
#endif
};

