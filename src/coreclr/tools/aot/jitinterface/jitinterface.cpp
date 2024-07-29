// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include <stdarg.h>
#include <stdlib.h>
#include <stdint.h>

#include "dllexport.h"
#include "jitinterface_generated.h"

static void NotImplemented()
{
    abort();
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
