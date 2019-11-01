// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#include <stdarg.h>
#include <stdlib.h>
#include <stdint.h>

#include "dllexport.h"
#include "jitinterface.h"

static void NotImplemented()
{
    abort();
}

enum CORINFO_RUNTIME_LOOKUP_KIND { };
struct CORINFO_LOOKUP_KIND
{
    bool                        needsRuntimeLookup;
    CORINFO_RUNTIME_LOOKUP_KIND runtimeLookupKind;
    unsigned short              runtimeLookupFlags;
    void*                       runtimeLookupArgs;
};

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
    catch (CorInfoException *)
    {
        return false;
    }
    return true;
}

CORINFO_LOOKUP_KIND JitInterfaceWrapper::getLocationOfThisType(void* context)
{
    CorInfoException* pException = nullptr;
    CORINFO_LOOKUP_KIND _ret;
    _callbacks->getLocationOfThisType(_thisHandle, &pException, &_ret, context);
    if (pException != nullptr)
    {
        throw pException;
    }
    return _ret;
}

void* JitInterfaceWrapper::getMemoryManager()
{
    NotImplemented();
    return nullptr;
}
