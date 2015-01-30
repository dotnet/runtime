//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//



#include "common.h"

#if defined(FEATURE_SVR_GC)

#include "gcenv.h"

#include "gc.h"
#include "gcscan.h"
#include "gcdesc.h"

#define SERVER_GC 1

namespace SVR { 
#include "gcimpl.h"
#include "gc.cpp"
}

#endif // defined(FEATURE_SVR_GC)
