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
#include "rhbinder.h"

bool InterfaceDispatch_InitializePal();

// Allocate memory aligned at sizeof(void*)*2 boundaries
void *InterfaceDispatch_AllocDoublePointerAligned(size_t size);
// Allocate memory aligned at at least sizeof(void*)
void *InterfaceDispatch_AllocPointerAligned(size_t size);

#endif // __CACHEDINTERFACEDISPATCHPAL_H__