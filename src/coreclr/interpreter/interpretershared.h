// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// This header contains definitions needed by this compiler library and also by
// the interpreter executor in the main coreclr library
#ifndef _INTERPRETERSHARED_H_
#define _INTERPRETERSHARED_H_

struct InterpMethod
{
    CORINFO_METHOD_HANDLE methodHnd;
    int32_t *pCode;
    int32_t allocaSize;

    volatile bool compiled;

    InterpMethod()
    {
        pCode = NULL;
        compiled = false;
    }
};

#endif
