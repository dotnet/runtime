// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// This header contains definitions needed by this compiler library and also by
// the interpreter executor in the main coreclr library
#ifndef _INTERPRETERSHARED_H_
#define _INTERPRETERSHARED_H_

#define INTERP_STACK_SLOT_SIZE 8    // Alignment of each var offset on the interpreter stack
#define INTERP_STACK_ALIGNMENT 16   // Alignment of interpreter stack at the start of a frame

#define OPDEF(a,b,c,d,e,f) a,
typedef enum
{
#include "intops.def"
    INTOP_LAST
} InterpOpcode;
#undef OPDEF

struct InterpMethod
{
    CORINFO_METHOD_HANDLE methodHnd;
    int32_t allocaSize;

    InterpMethod(CORINFO_METHOD_HANDLE methodHnd, int32_t allocaSize)
    {
        this->methodHnd = methodHnd;
        this->allocaSize = allocaSize;
    }
};

#endif
