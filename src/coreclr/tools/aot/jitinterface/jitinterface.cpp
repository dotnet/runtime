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

int JitInterfaceWrapper::FilterException(struct _EXCEPTION_POINTERS* pExceptionPointers)
{
    NotImplemented();
    return 1; // EXCEPTION_EXECUTE_HANDLER
}

bool JitInterfaceWrapper::runWithErrorTrap(ICorJitInfo::errorTrapFunction function, void* parameter)
{
    try
    {
        (*function)(parameter);
    }
    catch (CorInfoExceptionClass *)
    {
        return false;
    }
    return true;
}

bool JitInterfaceWrapper::runWithSPMIErrorTrap(ICorJitInfo::errorTrapFunction function, void* parameter)
{
    try
    {
        (*function)(parameter);
    }
    catch (CorInfoExceptionClass *)
    {
        return false;
    }
    return true;
}
