// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include <cassert>
#include <error_codes.h>
#include <utils.h>
#include "coreclr_resolver.h"
#include <pal.h>
#include <trace.h>

extern "C"
{
    pal::hresult_t STDMETHODCALLTYPE coreclr_initialize(
        const char* exePath,
        const char* appDomainFriendlyName,
        int propertyCount,
        const char** propertyKeys,
        const char** propertyValues,
        host_handle_t* hostHandle,
        unsigned int* domainId);

    pal::hresult_t STDMETHODCALLTYPE coreclr_shutdown_2(
        host_handle_t hostHandle,
        unsigned int domainId,
        int* latchedExitCode);

    pal::hresult_t STDMETHODCALLTYPE coreclr_execute_assembly(
        host_handle_t hostHandle,
        unsigned int domainId,
        int argc,
        const char** argv,
        const char* managedAssemblyPath,
        unsigned int* exitCode);

    pal::hresult_t STDMETHODCALLTYPE coreclr_create_delegate(
        host_handle_t hostHandle,
        unsigned int domainId,
        const char* entryPointAssemblyName,
        const char* entryPointTypeName,
        const char* entryPointMethodName,
        void** delegate);
}


bool coreclr_resolver_t::resolve_coreclr(const pal::string_t& libcoreclr_path, coreclr_resolver_contract_t& coreclr_resolver_contract)
{
    coreclr_resolver_contract.coreclr = nullptr;
    coreclr_resolver_contract.coreclr_initialize = reinterpret_cast<coreclr_initialize_fn>(coreclr_initialize);
    coreclr_resolver_contract.coreclr_shutdown = reinterpret_cast<coreclr_shutdown_fn>(coreclr_shutdown_2);
    coreclr_resolver_contract.coreclr_execute_assembly = reinterpret_cast<coreclr_execute_assembly_fn>(coreclr_execute_assembly);
    coreclr_resolver_contract.coreclr_create_delegate = reinterpret_cast<coreclr_create_delegate_fn>(coreclr_create_delegate);

    return true;
}
