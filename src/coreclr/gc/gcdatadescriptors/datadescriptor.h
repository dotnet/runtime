// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#ifdef IN_PROCESS_GC

// #include "../gc/env/common.h"
// #include "../gc/env/gcenv.h"
#include "static_assert.h"

#include <stdint.h>
#include <stddef.h>

// #include "../gc/env/common.h"
// #include "../gc/env/gcenv.h"
// #include "../gc/gc.h"
// #include "../gc/gcscan.h"

#else // IN_PROCESS_GC

#include <stdint.h>
#include <stddef.h>

#include "../gc/env/common.h"
#include "../gc/env/gcenv.h"
// #include "gc.h"
// #include "gcscan.h"

#endif // IN_PROCESS_GC
