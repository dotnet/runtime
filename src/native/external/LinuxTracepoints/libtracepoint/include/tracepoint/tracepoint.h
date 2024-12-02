// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

/*
Function prototypes for the tracepoint interface.
*/

#pragma once
#ifndef _included_tracepoint_h
#define _included_tracepoint_h 1

#include "tracepoint-state.h"
#include <sys/uio.h> // struct iovec

/*
Information about a tracepoint.
Used with tracepoint_open_provider_with_tracepoints.
*/
typedef struct tracepoint_definition {
    tracepoint_state* state;
    char const* tp_name_args;
} tracepoint_definition;

#ifdef __cplusplus
extern "C" {
#endif // __cplusplus

    /*
    Closes the specified tracepoint provider. Calling close on an
    already-closed provider is a safe no-op.

    Disconnects any tracepoints that are connected to this provider and resets
    them to TRACEPOINT_STATE_INIT.
    */
    void tracepoint_close_provider(
        tracepoint_provider_state* provider_state);

    /*
    Opens the specified provider. Returns 0 for success, errno for failure.

    On failure, the provider remains closed.

    On success, disconnects any tracepoints that are already connected to this
    provider and resets them to TRACEPOINT_STATE_INIT (this behavior is
    necessary for init-on-first-use scenarios).

    PRECONDITION:
    - It is an error to call tracepoint_open_provider(provider_state) if that
      provider_state is already open.
    - It is an error to call tracepoint_open_provider(provider_state) while
      tracepoint_open_provider or tracepoint_close_provider for that
      provider_state is running on another thread.
    */
    int
    tracepoint_open_provider(
        tracepoint_provider_state* provider_state);

    /*
    Opens the specified provider and connects the specified tracepoints.

    On failure, the provider remains closed.

    On success, disconnects any tracepoints that are already connected to this
    provider and resets them to TRACEPOINT_STATE_INIT (this behavior is
    necessary for init-on-first-use scenarios), then attempts to connect all
    tracepoints in the tp_definition list (ignoring errors).

    This function is intended for use when the caller is using
    __attribute__(section) to generate the tracepoint definition list. It sorts
    and de-duplicates the list (moving NULLs to the end), then connects
    tracepoints starting at tp_state_start and stopping at tp_state_stop or
    NULL, whichever comes first.

    PRECONDITION:
    - It is an error to call tracepoint_open_provider(provider_state) if that
      provider_state is already open.
    - It is an error to call tracepoint_open_provider(provider_state) while
      tracepoint_open_provider or tracepoint_close_provider for that
      provider_state is running on another thread.
    */
    int
    tracepoint_open_provider_with_tracepoints(
        tracepoint_provider_state* provider_state,
        tracepoint_definition const** tp_definition_start,
        tracepoint_definition const** tp_definition_stop);

    /*
    Connects the tracepoint to the specified provider.

    Returns 0 for success or if provider is closed, errno if an error was
    reported during tracepoint registration. Tracepoint's connection will be
    updated in either case.

    Tracepoint will remain connected to the provider until the provider is next
    opened or closed or until another call to tracepoint_connect.

    - tp_state is the tracepoint to be connected. It is ok to specify a
      tracepoint that is already connected to a provider, in which case the
      tracepoint will be disconnected from the old provider (if any) and
      connected to the new one.

    - provider_state is the new provider to connect to the tracepoint. It is ok
      to specify NULL - the tracepoint will be disconnected and reset. It is ok
      to specify a closed provider - the tracepoint will be connected to the
      closed provider until it opens, at which point the tracepoint will be
      disconnected and reset (this behavior is necessary for init-on-first-use
      scenarios).

    - tp_name_args is a nul-terminated string with the tracepoint name, a
      space, and the tracepoint arg spec string e.g.
      "MyTracepoint u16 MyField1; u8 MyField2".
    */
    int
    tracepoint_connect(
        tracepoint_state* tp_state,
        tracepoint_provider_state* provider_state,
        char const* tp_name_args);

    /*
    Writes the specified tracepoint.

    Returns:
    - 0 for success.
    - EBADF if the tracepoint is disconnected or if nobody is listening for
      this tracepoint.
    - Other errno value for failure.

    Calling tracepoint_write on a disconnected tracepoint is a safe no-op.
    Calling tracepoint_write on a tracepoint that is connected to a closed
    provider is a safe no-op.

    For optimal performance, only call this function if
    TRACEPOINT_ENABLED(tp_state) returns non-zero.

    data_vecs is an array of iovec structures that define the data payload.
    data_vecs[0] is reserved for the use of tracepoint_write and must be
    initialized to { NULL, 0 } before calling tracepoint_write. The
    implementation will overwrite the contents of data_vecs[0].

    PRECONDITION:
    - data_count >= 1.
    - data_vecs[0].iov_len == 0.
    */
    int
    tracepoint_write(
        tracepoint_state const* tp_state,
        unsigned data_count,
        struct iovec* data_vecs);

#ifdef __cplusplus
} // extern "C"
#endif // __cplusplus

#endif // _included_tracepoint_h
