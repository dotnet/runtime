// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "common.h"

#ifdef PROFILING_SUPPORTED
#include "asmconstants.h"
#include "proftoeeinterfaceimpl.h"

UINT_PTR ProfileGetIPFromPlatformSpecificHandle(void* pPlatformSpecificHandle)
{
    _ASSERTE(!"ProfileGetIPFromPlatformSpecificHandle is not implemented on wasm");
    return 0;
}

void ProfileSetFunctionIDInPlatformSpecificHandle(void* pPlatformSpecificHandle, FunctionID functionId)
{
    _ASSERTE(!"ProfileSetFunctionIDInPlatformSpecificHandle is not implemented on wasm");
}

ProfileArgIterator::ProfileArgIterator(MetaSig* pSig, void* pPlatformSpecificHandle)
    : m_argIterator(pSig)
{
    _ASSERTE(!"ProfileArgIterator constructor is not implemented on wasm");
}

ProfileArgIterator::~ProfileArgIterator()
{
    _ASSERTE(!"ProfileArgIterator destructor is not implemented on wasm");
}

LPVOID ProfileArgIterator::GetNextArgAddr()
{
    _ASSERTE(!"GetNextArgAddr is not implemented on wasm");
    return nullptr;
}

LPVOID ProfileArgIterator::GetHiddenArgValue(void)
{
    _ASSERTE(!"GetHiddenArgValue is not implemented on wasm");
    return nullptr;
}

LPVOID ProfileArgIterator::GetThis(void)
{
    _ASSERTE(!"GetThis is not implemented on wasm");
    return nullptr;
}

LPVOID ProfileArgIterator::GetReturnBufferAddr(void)
{
    _ASSERTE(!"GetReturnBufferAddr is not implemented on wasm");
    return nullptr;
}


#endif // PROFILING_SUPPORTED
