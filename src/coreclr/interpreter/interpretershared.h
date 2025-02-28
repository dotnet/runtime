// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// This header contains definitions needed by this compiler library and also by
// the interpreter executor in the main coreclr library
#ifndef _INTERPRETERSHARED_H_
#define _INTERPRETERSHARED_H_

#include "intopsshared.h"

#define INTERP_STACK_SLOT_SIZE 8    // Alignment of each var offset on the interpreter stack
#define INTERP_STACK_ALIGNMENT 16   // Alignment of interpreter stack at the start of a frame

#define INTERP_METHOD_DESC_TAG 4 // Tag of a MethodDesc in the interp method dataItems

struct InterpMethod
{
    CORINFO_METHOD_HANDLE methodHnd;
    int32_t allocaSize;
    void** pDataItems;

    InterpMethod(CORINFO_METHOD_HANDLE methodHnd, int32_t allocaSize, void** pDataItems)
    {
        this->methodHnd = methodHnd;
        this->allocaSize = allocaSize;
        this->pDataItems = pDataItems;
    }
};

#endif
