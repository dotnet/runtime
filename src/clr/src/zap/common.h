//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//
//*****************************************************************************
// common.h
//

//
// Precompiled headers.
//
//*****************************************************************************
#ifndef __COMMON_H__
#define __COMMON_H__

#include <stdint.h>
#include <winwrap.h>
#include <windows.h>
#include <stdlib.h>
#include <objbase.h>
#include <stddef.h>
#include <float.h>
#include <limits.h>

#if defined(_WIN64) || defined(_TARGET_ARM_)
#ifndef WIN64EXCEPTIONS
#define WIN64EXCEPTIONS
#endif
#endif // _WIN64 || _TARGET_ARM_

#include "utilcode.h"
#include "corjit.h"
#include "corcompile.h"
#include "iceefilegen.h"
#ifdef FEATURE_FUSION
#include "fusionbind.h"
#endif
#include "corpriv.h"

#include "holder.h"
#include "strongname.h"
#include "ex.h"
#include "corbbtprof.h"
#include "clrnt.h"
#include "contract.h"
#include "psapi.h"
#include "log.h"
#include "ngen.h"
#include "pedecoder.h"
#include "guidfromname.h"
#include "../dlls/mscorrc/resource.h"
#include "zaplog.h"
#ifndef FEATURE_CORECLR
#include "eventmsg.h"
#endif // FEATURE_CORECLR
#include "ndpversion.h"

#include "loaderheap.h"

#include "zapper.h"
#include "zapwriter.h"
#include "zapimage.h"

#include "zapperstats.h"

#endif  // __COMMON_H__
