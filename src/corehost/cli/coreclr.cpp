// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#include <cassert>

#include "coreclr.h"
#include "utils.h"

static pal::dll_t g_coreclr = nullptr;

// Prototype of the coreclr_initialize function from coreclr.dll
typedef pal::hresult_t(STDMETHODCALLTYPE *coreclr_initialize_fn)(
    const char* exePath,
    const char* appDomainFriendlyName,
    int propertyCount,
    const char** propertyKeys,
    const char** propertyValues,
    coreclr::host_handle_t* hostHandle,
    unsigned int* domainId);

// Prototype of the coreclr_shutdown function from coreclr.dll
typedef pal::hresult_t(STDMETHODCALLTYPE *coreclr_shutdown_fn)(
    coreclr::host_handle_t hostHandle,
    unsigned int domainId,
    int* latchedExitCode);

// Prototype of the coreclr_execute_assembly function from coreclr.dll
typedef pal::hresult_t(STDMETHODCALLTYPE *coreclr_execute_assembly_fn)(
    coreclr::host_handle_t hostHandle,
    unsigned int domainId,
    int argc,
    const char** argv,
    const char* managedAssemblyPath,
    unsigned int* exitCode);

static coreclr_shutdown_fn coreclr_shutdown = nullptr;
static coreclr_initialize_fn coreclr_initialize = nullptr;
static coreclr_execute_assembly_fn coreclr_execute_assembly = nullptr;

bool coreclr::bind(const pal::string_t& libcoreclr_path)
{
    assert(g_coreclr == nullptr);

    pal::string_t coreclr_dll_path(libcoreclr_path);
    append_path(&coreclr_dll_path, LIBCORECLR_NAME);

    if (!pal::load_library(coreclr_dll_path.c_str(), &g_coreclr))
    {
        return false;
    }

    coreclr_initialize = (coreclr_initialize_fn)pal::get_symbol(g_coreclr, "coreclr_initialize");
    coreclr_shutdown = (coreclr_shutdown_fn)pal::get_symbol(g_coreclr, "coreclr_shutdown_2");
    coreclr_execute_assembly = (coreclr_execute_assembly_fn)pal::get_symbol(g_coreclr, "coreclr_execute_assembly");

    return true;
}

void coreclr::unload()
{
    assert(g_coreclr != nullptr && coreclr_initialize != nullptr);

    pal::unload_library(g_coreclr);
}

pal::hresult_t coreclr::initialize(
    const char* exe_path,
    const char* app_domain_friendly_name,
    const char** property_keys,
    const char** property_values,
    int property_count,
    host_handle_t* host_handle,
    domain_id_t* domain_id)
{
    assert(g_coreclr != nullptr && coreclr_initialize != nullptr);

    return coreclr_initialize(
        exe_path,
        app_domain_friendly_name,
        property_count,
        property_keys,
        property_values,
        host_handle,
        domain_id);
}

pal::hresult_t coreclr::shutdown(host_handle_t host_handle, domain_id_t domain_id, int* latchedExitCode)
{
    assert(g_coreclr != nullptr && coreclr_shutdown != nullptr);

    return coreclr_shutdown(host_handle, domain_id, latchedExitCode);
}

pal::hresult_t coreclr::execute_assembly(
    host_handle_t host_handle,
    domain_id_t domain_id,
    int argc,
    const char** argv,
    const char* managed_assembly_path,
    unsigned int* exit_code)
{
    assert(g_coreclr != nullptr && coreclr_execute_assembly != nullptr);

    return coreclr_execute_assembly(
        host_handle,
        domain_id,
        argc,
        argv,
        managed_assembly_path,
        exit_code);
}
