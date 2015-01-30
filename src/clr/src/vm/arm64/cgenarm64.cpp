//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//
//
// Various helper routines for generating AMD64 assembly code.
//

// Precompiled Header

#include "common.h"

#include "stublink.h"
#include "cgensys.h"
#include "siginfo.hpp"
#include "excep.h"
#include "ecall.h"
#include "dllimport.h"
#include "dllimportcallback.h"
#include "dbginterface.h"
#include "fcall.h"
#include "array.h"
#include "virtualcallstub.h"

#ifndef DACCESS_COMPILE

// Note: This is only used on server GC on Windows.

DWORD GetLogicalCpuCount()
{
    LIMITED_METHOD_CONTRACT;

    // The contact with any callers of this function is that if we're unable to determine
    // the processor count, or the number of processors is not distributed evenly, then
    // we should return 1.
    return 1;
}

#endif // DACCESS_COMPILE
