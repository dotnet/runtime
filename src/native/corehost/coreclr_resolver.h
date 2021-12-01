// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#ifndef __CORECLR_RESOLVER_H__
#define __CORECLR_RESOLVER_H__

#include "pal.h"
#include "error_codes.h"

using host_handle_t = void*;

// Prototype of the coreclr_initialize function from coreclr.dll
using coreclr_initialize_fn = pal::hresult_t(STDMETHODCALLTYPE*)(
    const char* exePath,
    const char* appDomainFriendlyName,
    int propertyCount,
    const char** propertyKeys,
    const char** propertyValues,
    host_handle_t* hostHandle,
    unsigned int* domainId);

// Prototype of the coreclr_shutdown function from coreclr.dll
using coreclr_shutdown_fn = pal::hresult_t(STDMETHODCALLTYPE*)(
    host_handle_t hostHandle,
    unsigned int domainId,
    int* latchedExitCode);

// Prototype of the coreclr_execute_assembly function from coreclr.dll
using coreclr_execute_assembly_fn = pal::hresult_t(STDMETHODCALLTYPE*)(
    host_handle_t hostHandle,
    unsigned int domainId,
    int argc,
    const char** argv,
    const char* managedAssemblyPath,
    unsigned int* exitCode);

// Prototype of the coreclr_create_delegate function from coreclr.dll
using coreclr_create_delegate_fn = pal::hresult_t(STDMETHODCALLTYPE*)(
    host_handle_t hostHandle,
    unsigned int domainId,
    const char* entryPointAssemblyName,
    const char* entryPointTypeName,
    const char* entryPointMethodName,
    void** delegate);

struct coreclr_resolver_contract_t
{
    pal::dll_t coreclr;
    coreclr_shutdown_fn coreclr_shutdown;
    coreclr_initialize_fn coreclr_initialize;
    coreclr_execute_assembly_fn coreclr_execute_assembly;
    coreclr_create_delegate_fn coreclr_create_delegate;
};

class coreclr_resolver_t
{
    public:
        static bool resolve_coreclr(const pal::string_t& libcoreclr_path, coreclr_resolver_contract_t& coreclr_resolver_contract);
};

#endif // __CORECLR_RESOLVER_H__
