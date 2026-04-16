// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// This header provides the includes needed for datadescriptor.inc to use
// offsetof() on NativeAOT runtime data structures.
//
// Note: Some NativeAOT types have private members that offsetof() cannot access
// from this compilation unit. For those types, we use known offset constants
// validated at build time by AsmOffsetsVerify.cpp and DebugHeader.cpp static_asserts.

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

// ILC emits a ContractDescriptor named "DotNetManagedContractDescriptor" with
// managed type layouts. We take its address so datadescriptor.inc can reference
// it as a sub-descriptor via CDAC_GLOBAL_SUB_DESCRIPTOR.
struct ContractDescriptor;
extern "C" ContractDescriptor DotNetManagedContractDescriptor;
static const void* g_pManagedContractDescriptor = &DotNetManagedContractDescriptor;
