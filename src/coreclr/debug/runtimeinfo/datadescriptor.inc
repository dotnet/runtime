// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "common.h"

#include <stdint.h>
#include <stddef.h>

#include "static_assert.h"

#include <sospriv.h>
#include "cdacplatformmetadata.hpp"
#include "methodtable.h"
#include "threads.h"
#include "vars.hpp"
#include "exinfo.h"

#include "configure.h"

#include "../debug/ee/debugger.h"

#ifdef HAVE_GCCOVER
#include "gccover.h"
#endif // HAVE_GCCOVER
