// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.



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
