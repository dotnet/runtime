// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

/*
Type definitions and macros for the tracepoint interface.

These definitions are separated from tracepoint.h to minimize namespace
pollution - these types and macros are needed in some scenarios where the
function prototypes from tracepoint.h do not need to be defined.
*/

#pragma once
#ifndef _included_tracepoint_state_h
#define _included_tracepoint_state_h 1

/*
Implementation detail - for use by the tracepoint implementation.
*/
typedef struct tracepoint_list_node tracepoint_list_node;
struct tracepoint_list_node
{
    tracepoint_list_node* next;
    tracepoint_list_node* prev;
};

/*
Macro returns a non-zero value if any consumer is listening to the specified
tracepoint.
*/
#define TRACEPOINT_ENABLED(tp_state) __atomic_load_n(&(tp_state)->status_word, __ATOMIC_RELAXED)

/*
Opaque provider state, typically a non-const static/global for each provider.

Basic usage:

- Allocate memory for use by the provider (usually a static or global).
- Initialize with TRACEPOINT_PROVIDER_STATE_INIT before use.
- Do not directly modify any fields while in use.
- Use with the tracepoint_close_provider, tracepoint_open_provider, or
  tracepoint_connect APIs.
- Close the provider with tracepoint_close_provider before deallocating it.
- Normally you'll want to make sure no tracepoints are connected to a provider
  before deallocating it. However, it's ok to deallocate a *closed* provider
  with connected tracepoints as long as you deallocate all connected
  tracepoints at the same time (e.g. if the provider and tracepoints are static
  or global).

More precise description of usage requirements:

- Caller should make no assumptions about the semantics of any of the fields.
  All fields should be considered opaque.
- The struct may become "valid" only when all of its fields have the values
  specified in TRACEPOINT_PROVIDER_STATE_INIT.
- The struct becomes invalid when the caller modifies fields or deallocates the
  memory of the struct.
- Only valid provider structs may be used in calls to tracepoint_close_provider,
  tracepoint_open_provider, or tracepoint_connect.
- A provider struct that is used in a call to tracepoint_close_provider,
  tracepoint_open_provider, or tracepoint_connect must remain valid until the
  call returns.
- A provider struct that is open must remain valid until it is closed.
- Invalidating a provider also invalidates all tracepoints that are connected
  to that provider.
*/
typedef struct tracepoint_provider_state {
    int data_file;                             // Opaque. Initial value must be -1.
    unsigned ref_count;                        // Opaque. Initial value must be 0.
    tracepoint_list_node tracepoint_list_head; // Opaque. Initial value must be {NULL,NULL}.
} tracepoint_provider_state;

/*
Initializer for tracepoint_provider_state.
Usage: tracepoint_provider_state my_provider = TRACEPOINT_PROVIDER_STATE_INIT;
*/
#define TRACEPOINT_PROVIDER_STATE_INIT { -1,  0,  { (tracepoint_list_node*)0, (tracepoint_list_node*)0 } }

/*
Partially-opaque tracepoint state, typically a non-const static/global for each
tracepoint.

Basic usage:

- Allocate memory for use by the tracepoint (usually a static or global).
- Initialize with TRACEPOINT_STATE_INIT before use.
- Do not directly modify any fields while in use.
- Use with the tracepoint_connect, tracepoint_write, or TRACEPOINT_ENABLED
  APIs.
- Disconnect the tracepoint before deallocating it.
- It is ok to deallocate a tracepoint that is connected to a *closed* provider
  if you also deallocate the connected provider and all of the other connected
  tracepoints at the same time (e.g. if the provider and tracepoints are static
  or global).

More precise description of usage requirements:

- Caller may assume that a call to tracepoint_write will be a no-op if
  0 == (status_mask & *status_byte).
- Caller may assume that provider_state is a pointer to the provider state of
  the connected provider, or NULL if the tracepoint is disconnected.
- Caller should make no other assumptions about the semantics of any of the
  fields. All fields should be considered opaque. 
- The struct may become "valid" only when all of its fields have the values
  specified in TRACEPOINT_STATE_INIT.
- The struct becomes invalid when the caller modifies fields or deallocates the
  memory of the struct.
- Only valid tracepoint structs may be used in calls to tracepoint_connect,
  tracepoint_write, or TRACEPOINT_ENABLED.
- A tracepoint struct that is used in a call to tracepoint_connect,
  tracepoint_write, or TRACEPOINT_ENABLED must remain valid until the call
  returns.
- A tracepoint struct that is connected to a provider may only be invalidated
  by invalidating the provider.
*/
typedef struct tracepoint_state {
    unsigned status_word;                            // Initial value must be 0.
    int write_index;                                 // Opaque. Initial value must be -1.
    tracepoint_provider_state const* provider_state; // Initial value must be NULL.
    tracepoint_list_node tracepoint_list_link;       // Opaque. Initial value must be {NULL,NULL}.
} tracepoint_state;

/*
Initializer for tracepoint_state.
Usage: tracepoint_state my_tracepoint = TRACEPOINT_STATE_INIT;
*/
#define TRACEPOINT_STATE_INIT { \
    0, \
    -1, \
    (tracepoint_provider_state const*)0, \
    { (tracepoint_list_node*)0, (tracepoint_list_node*)0 } \
} \

#endif // _included_tracepoint_state_h 
