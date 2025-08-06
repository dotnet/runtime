// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "datadescriptor.h"
#include "contractconfiguration.h"

extern "C"
{
// without an extern declaration, clang does not emit this global into the object file
extern const uintptr_t POINTER_DATA_NAME[];

const uintptr_t POINTER_DATA_NAME[] = {
    (uintptr_t)0, // placeholder
#define CDAC_GLOBAL_POINTER(name,value) (uintptr_t)(value),
#define CDAC_GLOBAL_SUB_DESCRIPTOR(name,value) (uintptr_t)(value),
#include "wrappeddatadescriptor.inc"
};
}
