// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
// APIs for hosting CoreCLR
//

#ifndef __CORECLR_HOST_H__
#define __CORECLR_HOST_H__

// For each hosting API, we define a function prototype and a function pointer
// The prototype is useful for implicit linking against the dynamic coreclr
// library and the pointer for explicit dynamic loading (dlopen, LoadLibrary)
#define CORECLR_HOSTING_API(function, ...) \
    extern "C" int function(__VA_ARGS__); \
    typedef int (*function##_ptr)(__VA_ARGS__)
    
CORECLR_HOSTING_API(coreclr_initialize,
            const char* exePath,
            const char* appDomainFriendlyName,
            int propertyCount,
            const char** propertyKeys,
            const char** propertyValues,
            void** hostHandle,
            unsigned int* domainId);

CORECLR_HOSTING_API(coreclr_shutdown,
            void* hostHandle,
            unsigned int domainId);

CORECLR_HOSTING_API(coreclr_shutdown_2,
            void* hostHandle,
            unsigned int domainId,
            int* latchedExitCode);

CORECLR_HOSTING_API(coreclr_create_delegate,
            void* hostHandle,
            unsigned int domainId,
            const char* entryPointAssemblyName,
            const char* entryPointTypeName,
            const char* entryPointMethodName,
            void** delegate);

CORECLR_HOSTING_API(coreclr_execute_assembly,
            void* hostHandle,
            unsigned int domainId,
            int argc,
            const char** argv,
            const char* managedAssemblyPath,
            unsigned int* exitCode);

#undef CORECLR_HOSTING_API
                      
#endif // __CORECLR_HOST_H__
