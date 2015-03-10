//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//



#include "common.h"

#if defined(FEATURE_SVR_GC)

#include "gcenv.h"

#include "gc.h"
#include "gcscan.h"

#define SERVER_GC 1

namespace SVR { 
#include "gcimpl.h"
#include "gcee.cpp"
}

#if defined(FEATURE_PAL) && !defined(DACCESS_COMPILE)
 
// Initializes the SVR DAC table entries
void DacGlobals::InitializeSVREntries(TADDR baseAddress)
{
#define DEFINE_DACVAR_SVR(id_type, size, id, var)   id = PTR_TO_TADDR(&var) - baseAddress;
#include "dacvars.h"
}

#endif // FEATURE_PAL && !DACCESS_COMPILE

#endif // FEATURE_SVR_GC
