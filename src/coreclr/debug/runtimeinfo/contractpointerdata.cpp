// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "common.h"

#include <stddef.h>
#include <stdint.h>

#include "threads.h"

extern "C"
{
  
// without an extern declaration, clang does not emit this global into the object file
extern const uintptr_t contractDescriptorPointerData[];

const uintptr_t contractDescriptorPointerData[] = {
    (uintptr_t)0, // placeholder
#define CDAC_GLOBAL_POINTER(name,value) (uintptr_t)(value),
#include "datadescriptor.h"
};

}
