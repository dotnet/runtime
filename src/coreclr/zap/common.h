// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
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
#include <stddef.h>
#include <winwrap.h>
#include <windows.h>
#include <stdlib.h>
#include <objbase.h>
#include <float.h>
#include <limits.h>

#if !defined(TARGET_X86) || defined(TARGET_UNIX)
#ifndef FEATURE_EH_FUNCLETS
#define FEATURE_EH_FUNCLETS
#endif
#endif // !TARGET_X86 || TARGET_UNIX

#ifdef TARGET_64BIT
typedef unsigned __int64 TARGET_POINTER_TYPE;
#else
typedef unsigned int TARGET_POINTER_TYPE;
#endif

#include "utilcode.h"
#include "corjit.h"
#include "corcompile.h"
#include "iceefilegen.h"
#include "corpriv.h"
#include "gcinfotypes.h"

#include "holder.h"
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
#include "clrversion.h"

#include "loaderheap.h"

#include "zapper.h"
#include "zapwriter.h"
#include "zapimage.h"

#include "zapperstats.h"

#endif  // __COMMON_H__
