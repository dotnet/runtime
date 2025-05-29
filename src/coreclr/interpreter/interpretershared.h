// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// This header contains definitions needed by this compiler library and also by
// the interpreter executor in the main coreclr library
#ifndef _INTERPRETERSHARED_H_
#define _INTERPRETERSHARED_H_

#include "intopsshared.h"

#ifdef _MSC_VER
#define INTERP_API
#else
#define INTERP_API __attribute__ ((visibility ("default")))
#endif // _MSC_VER

#define INTERP_STACK_SLOT_SIZE 8    // Alignment of each var offset on the interpreter stack
#define INTERP_STACK_ALIGNMENT 16   // Alignment of interpreter stack at the start of a frame

#define INTERP_METHOD_HANDLE_TAG 4 // Tag of a MethodDesc in the interp method dataItems
#define INTERP_INDIRECT_HELPER_TAG 1 // When a helper ftn's address is indirect we tag it with this tag bit

struct InterpMethod
{
#if DEBUG
    InterpMethod *self;
#endif
    CORINFO_METHOD_HANDLE methodHnd;
    int32_t allocaSize;
    void** pDataItems;
    bool initLocals;

    InterpMethod(CORINFO_METHOD_HANDLE methodHnd, int32_t allocaSize, void** pDataItems, bool initLocals)
    {
#if DEBUG
        this->self = this;
#endif
        this->methodHnd = methodHnd;
        this->allocaSize = allocaSize;
        this->pDataItems = pDataItems;
        this->initLocals = initLocals;
    }

    bool CheckIntegrity()
    {
#if DEBUG
        return this->self == this;
#else
        return true;
#endif
    }
};

#endif
