// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#ifndef __HOSTPOLICY_H__
#define __HOSTPOLICY_H__

#include "host_interface.h"
#include <pal.h>

#if defined(_WIN32)
    #define HOSTPOLICY_CALLTYPE __cdecl
#else
    #define HOSTPOLICY_CALLTYPE
#endif

struct corehost_initialize_request_t;
struct corehost_context_contract;

typedef int(HOSTPOLICY_CALLTYPE *corehost_load_fn) (const host_interface_t *init);
typedef int(HOSTPOLICY_CALLTYPE *corehost_unload_fn) ();

typedef int(HOSTPOLICY_CALLTYPE *corehost_main_fn) (
    const int argc,
    const pal::char_t **argv);
typedef int(HOSTPOLICY_CALLTYPE *corehost_main_with_output_buffer_fn) (
    const int argc,
    const pal::char_t **argv,
    pal::char_t *buffer,
    int32_t buffer_size,
    int32_t *required_buffer_size);

typedef void(HOSTPOLICY_CALLTYPE *corehost_error_writer_fn) (const pal::char_t *message);
typedef corehost_error_writer_fn(HOSTPOLICY_CALLTYPE *corehost_set_error_writer_fn) (corehost_error_writer_fn error_writer);

typedef int(HOSTPOLICY_CALLTYPE *corehost_initialize_fn)(
    const corehost_initialize_request_t *init_request,
    uint32_t options,
    corehost_context_contract *handle);

#endif //__HOSTPOLICY_H__
