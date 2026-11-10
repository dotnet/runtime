// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "datadescriptor.h"
#include "contractconfiguration.h"

extern "C"
{
    // without an extern declaration, clang does not emit this global into the object file
    extern constexpr const void* POINTER_DATA_NAME[] = {
        (void*)0, // placeholder
    #define CDAC_GLOBAL_POINTER(name,value) (void*)(value),
    #define CDAC_GLOBAL_SUB_DESCRIPTOR(name,value) (void*)(value),
    #include "wrappeddatadescriptor.inc"
    };
};
