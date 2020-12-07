// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include <stdarg.h>
#include <stdlib.h>
#include <stdint.h>

#include "dllexport.h"
#include "jitinterface.h"

static void NotImplemented()
{
    abort();
}

int JitInterfaceWrapper::FilterException(void* pExceptionPointers)
{
    NotImplemented();
    return 1; // EXCEPTION_EXECUTE_HANDLER
}

void JitInterfaceWrapper::HandleException(void* pExceptionPointers)
{
    NotImplemented();
}

bool JitInterfaceWrapper::runWithErrorTrap(void* function, void* parameter)
{
    typedef void(*pfn)(void*);
    try
    {
        (*(pfn)function)(parameter);
    }
    catch (CorInfoExceptionClass *)
    {
        return false;
    }
    return true;
}
