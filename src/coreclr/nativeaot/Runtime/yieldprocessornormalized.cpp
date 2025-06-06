// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "common.h"
#include "gcenv.h"
#include "gcheaputilities.h"
#include "CommonTypes.h"
#include "CommonMacros.h"
#include "daccess.h"
#include "DebugMacrosExt.h"
#include "PalLimitedContext.h"
#include "Pal.h"
#include "rhassert.h"
#include "slist.h"
#include "volatile.h"
#include "yieldprocessornormalized.h"
#include "minipal/time.h"

#include "../../utilcode/yieldprocessornormalized.cpp"

#include "../../vm/yieldprocessornormalizedshared.cpp"
