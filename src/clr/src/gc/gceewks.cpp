//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//



#include "common.h"

#include "gcenv.h"

#include "gc.h"
#include "gcscan.h"

#ifdef SERVER_GC
#undef SERVER_GC
#endif

namespace WKS { 
#include "gcimpl.h"
#include "gcee.cpp"
}

