// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "common.h"
#include "gcenv.h"
#include "gcheaputilities.h"
#include "gcinterface.dac.h"
#include "rhassert.h"
#include "TargetPtrs.h"
#include "PalLimitedContext.h"
#include "Pal.h"
#include "holder.h"
#include "RuntimeInstance.h"
#include "regdisplay.h"
#include "StackFrameIterator.h"
#include "thread.h"
#include "threadstore.h"

#include <stdint.h>
#include <stddef.h>

GPTR_DECL(MethodTable, g_pFreeObjectEEType);
GPTR_DECL(StressLog, g_pStressLog);

// ILC emits a ContractDescriptor named "DotNetManagedContractDescriptor" with
// managed type layouts. We take its address so datadescriptor.inc can reference
// it as a sub-descriptor via CDAC_GLOBAL_SUB_DESCRIPTOR.
struct ContractDescriptor;
extern "C" ContractDescriptor DotNetManagedContractDescriptor;
static const void* g_pManagedContractDescriptor = &DotNetManagedContractDescriptor;
