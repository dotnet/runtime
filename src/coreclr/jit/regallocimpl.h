// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#pragma once

//
// Include this header to access the RegAllocImpl type for the target platform.
//
#if HAS_FIXED_REGISTER_SET
#include "lsra.h"
#endif

#ifdef TARGET_WASM
#include "regallocwasm.h"
#endif
