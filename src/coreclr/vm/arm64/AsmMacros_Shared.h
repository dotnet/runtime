// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// This file is used to allow sharing of assembly code between NativeAOT and CoreCLR, which have different conventions about how to ensure that constants offsets are accessible

#ifdef TARGET_WINDOWS
#include "ksarm64.h"
#include "asmconstants.h"
#include "asmmacros.h"
#else
#include "asmconstants.h"
#include "unixasmmacros.inc"
#endif
