// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#ifndef __CACHEDINTERFACEDISPATCHPAL_H__
#define __CACHEDINTERFACEDISPATCHPAL_H__

#include "CommonTypes.h"
#include "CommonMacros.h"
#include "daccess.h"
#include "DebugMacrosExt.h"
#include "PalRedhawkCommon.h"
#include "PalRedhawk.h"
#include "rhassert.h"
#include "slist.h"
#include "holder.h"
#include "Crst.h"
#include "RedhawkWarnings.h"
#include "TargetPtrs.h"
#include "MethodTable.h"
#include "Range.h"
#include "allocheap.h"
#include "rhbinder.h"
#include "ObjectLayout.h"
#include "shash.h"
#include "TypeManager.h"
#include "RuntimeInstance.h"
#include "MethodTable.inl"
#include "CommonMacros.inl"

bool InterfaceDispatch_InitializePal();

// Allocate memory aligned at sizeof(void*)*2 boundaries
void *InterfaceDispatch_AllocDoublePointerAligned(size_t size);
// Allocate memory aligned at sizeof(void*) boundaries

void *InterfaceDispatch_AllocPointerAligned(size_t size);

#endif // __CACHEDINTERFACEDISPATCHPAL_H__