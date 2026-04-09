// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.


#include "common.h"

#define FPO_INTERRUPTIBLE 0

/* Precompiled header nonsense requires that we do it this way  */

/* GCDecoder.cpp is a common source file bewtween VM and JIT/IL */

// we need only one copy when embedding
#if !defined(CORECLR_EMBEDDED)
#include "gcdecoder.cpp"
#endif
