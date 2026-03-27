// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#ifndef APPHOST_HOSTFXR_RESOLVER_H
#define APPHOST_HOSTFXR_RESOLVER_H

#include <stdbool.h>
#include <stdint.h>
#include "hostfxr.h"
#include "error_codes.h"

// Opaque hostfxr resolver handle
typedef struct hostfxr_resolver
{
    void* hostfxr_dll;
    char dotnet_root[4096];
    char fxr_path[4096];
    int status_code; // StatusCode enum value
} hostfxr_resolver_t;

// Initialize the resolver: find and load hostfxr.
void hostfxr_resolver_init(hostfxr_resolver_t* resolver, const char* app_root);

// Clean up the resolver: unload hostfxr if loaded.
void hostfxr_resolver_cleanup(hostfxr_resolver_t* resolver);

// Resolve function pointers from the loaded hostfxr.
hostfxr_main_bundle_startupinfo_fn hostfxr_resolver_resolve_main_bundle_startupinfo(const hostfxr_resolver_t* resolver);
hostfxr_set_error_writer_fn hostfxr_resolver_resolve_set_error_writer(const hostfxr_resolver_t* resolver);
hostfxr_main_startupinfo_fn hostfxr_resolver_resolve_main_startupinfo(const hostfxr_resolver_t* resolver);
hostfxr_main_fn hostfxr_resolver_resolve_main_v1(const hostfxr_resolver_t* resolver);

#endif // APPHOST_HOSTFXR_RESOLVER_H
