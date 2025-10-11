// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#ifndef HOST_WINDOWS
#include "../pal/inc/pal.h"
#include "../pal/inc/rt/ntimage.h"
#endif // HOST_WINDOWS
#include "common.h"

#include <stdint.h>
#include <stddef.h>

#include <sospriv.h>
#include "cdacplatformmetadata.hpp"
#include "interoplibinterface_comwrappers.h"
#include "methodtable.h"
#include "threads.h"
#include "vars.hpp"
#include "exinfo.h"

#include "configure.h"

#include "../debug/ee/debugger.h"
#include "patchpointinfo.h"
#ifdef FEATURE_COMWRAPPERS
#include "../interop/comwrappers.hpp"
#endif // FEATURE_COMWRAPPERS

#ifdef HAVE_GCCOVER
#include "gccover.h"
#endif // HAVE_GCCOVER
