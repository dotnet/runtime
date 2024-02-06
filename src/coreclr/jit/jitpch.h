// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include <stdint.h>
#include <windows.h>
#include <wchar.h>
#include <stdio.h>
#include <stddef.h>
#include <stdlib.h>
#include <limits.h>
#include <string.h>
#include <float.h>
#include <cstdlib>
#include <intrin.h>
#include "netintrinsics.h"

// Don't allow using the windows.h #defines for the BitScan* APIs. Using the #defines means our
// `BitOperations::BitScan*` functions have their name mapped, which is confusing and messes up
// Visual Studio source browsing.
#ifdef BitScanForward
#undef BitScanForward
#endif
#ifdef BitScanReverse
#undef BitScanReverse
#endif
#ifdef BitScanForward64
#undef BitScanForward64
#endif
#ifdef BitScanReverse64
#undef BitScanReverse64
#endif

#include "jitconfig.h"
#include "jit.h"
#include "iallocator.h"
#include "hashbv.h"
#include "compiler.h"
#include "dataflow.h"
#include "block.h"
#include "jiteh.h"
#include "rationalize.h"
#include "jitstd.h"
#include "ssaconfig.h"
#include "blockset.h"
#include "bitvec.h"
#include "inline.h"
#include "objectalloc.h"
