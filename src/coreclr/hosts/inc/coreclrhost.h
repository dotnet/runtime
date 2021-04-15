// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

//
// APIs for hosting CoreCLR
//

#ifndef __CORECLR_HOST_H__
#define __CORECLR_HOST_H__

#if defined(_WIN32) && defined(_M_IX86)
#define CORECLR_CALLING_CONVENTION __stdcall
#else
#define CORECLR_CALLING_CONVENTION
#endif

#include <stdint.h>

// For each hosting API, we define a function prototype and a function pointer
// The prototype is useful for implicit linking against the dynamic coreclr
// library and the pointer for explicit dynamic loading (dlopen, LoadLibrary)
#define CORECLR_HOSTING_API(function, ...) \
    extern "C" int CORECLR_CALLING_CONVENTION function(__VA_ARGS__); \
    typedef int (CORECLR_CALLING_CONVENTION *function##_ptr)(__VA_ARGS__)

//
// Initialize the CoreCLR. Creates and starts CoreCLR host and creates an app domain
//
// Parameters:
//  exePath                 - Absolute path of the executable that invoked the ExecuteAssembly (the native host application)
//  appDomainFriendlyName   - Friendly name of the app domain that will be created to execute the assembly
//  propertyCount           - Number of properties (elements of the following two arguments)
//  propertyKeys            - Keys of properties of the app domain
//  propertyValues          - Values of properties of the app domain
//  hostHandle              - Output parameter, handle of the created host
//  domainId                - Output parameter, id of the created app domain
//
// Returns:
//  HRESULT indicating status of the operation. S_OK if the assembly was successfully executed
//
CORECLR_HOSTING_API(coreclr_initialize,
            const char* exePath,
            const char* appDomainFriendlyName,
            int propertyCount,
            const char** propertyKeys,
            const char** propertyValues,
            void** hostHandle,
            unsigned int* domainId);

//
// Shutdown CoreCLR. It unloads the app domain and stops the CoreCLR host.
//
// Parameters:
//  hostHandle              - Handle of the host
//  domainId                - Id of the domain
//
// Returns:
//  HRESULT indicating status of the operation. S_OK if the assembly was successfully executed
//
CORECLR_HOSTING_API(coreclr_shutdown,
            void* hostHandle,
            unsigned int domainId);

//
// Shutdown CoreCLR. It unloads the app domain and stops the CoreCLR host.
//
// Parameters:
//  hostHandle              - Handle of the host
//  domainId                - Id of the domain
//  latchedExitCode         - Latched exit code after domain unloaded
//
// Returns:
//  HRESULT indicating status of the operation. S_OK if the assembly was successfully executed
//
CORECLR_HOSTING_API(coreclr_shutdown_2,
            void* hostHandle,
            unsigned int domainId,
            int* latchedExitCode);

//
// Create a native callable function pointer for a managed method.
//
// Parameters:
//  hostHandle              - Handle of the host
//  domainId                - Id of the domain
//  entryPointAssemblyName  - Name of the assembly which holds the custom entry point
//  entryPointTypeName      - Name of the type which holds the custom entry point
//  entryPointMethodName    - Name of the method which is the custom entry point
//  delegate                - Output parameter, the function stores a native callable function pointer to the delegate at the specified address
//
// Returns:
//  HRESULT indicating status of the operation. S_OK if the assembly was successfully executed
//
CORECLR_HOSTING_API(coreclr_create_delegate,
            void* hostHandle,
            unsigned int domainId,
            const char* entryPointAssemblyName,
            const char* entryPointTypeName,
            const char* entryPointMethodName,
            void** delegate);

//
// Execute a managed assembly with given arguments
//
// Parameters:
//  hostHandle              - Handle of the host
//  domainId                - Id of the domain
//  argc                    - Number of arguments passed to the executed assembly
//  argv                    - Array of arguments passed to the executed assembly
//  managedAssemblyPath     - Path of the managed assembly to execute (or NULL if using a custom entrypoint).
//  exitCode                - Exit code returned by the executed assembly
//
// Returns:
//  HRESULT indicating status of the operation. S_OK if the assembly was successfully executed
//
CORECLR_HOSTING_API(coreclr_execute_assembly,
            void* hostHandle,
            unsigned int domainId,
            int argc,
            const char** argv,
            const char* managedAssemblyPath,
            unsigned int* exitCode);

#undef CORECLR_HOSTING_API

//
// Callback types used by the hosts
//
typedef bool(CORECLR_CALLING_CONVENTION BundleProbeFn)(const char* path, int64_t* offset, int64_t* size, int64_t* compressedSize);
typedef const void* (CORECLR_CALLING_CONVENTION PInvokeOverrideFn)(const char* libraryName, const char* entrypointName);


#endif // __CORECLR_HOST_H__
