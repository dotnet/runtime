// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

#pragma once
#ifndef _included_eventheader_tracepoint_h
#define _included_eventheader_tracepoint_h 1

#include <tracepoint/tracepoint-state.h>
#include <sys/uio.h> // struct iovec
#include <eventheader/eventheader.h>

/*
Read-only event info, typically a const static/global for each event.
Caller must provide one for each event, initialized to metadata for the
event and with a pointer to the state for the event.
*/
typedef struct eventheader_tracepoint {
    tracepoint_state* state;
    eventheader_extension const* metadata;
    eventheader header;
    uint64_t keyword;
} eventheader_tracepoint;

/*
Read-only provider info, typically a const static/global for each provider.
Caller must provide one for each provider, initialized to metadata for the
provider and with a pointer to the state for the provider.
*/
typedef struct eventheader_provider {
    tracepoint_provider_state* state;

    /*
    Nul-terminated provider options string. May be "" or NULL if none.

    The options string contains 0 or more options, ordered by option type
    (e.g. option "Gvalue" would come before option "Hvalue").

    Each option is an uppercase ASCII letter (option type) followed by 0 or
    more ASCII digits or **lowercase** ASCII letters (option value), e.g.
    "Gmygroupname".

    Recognized option types:
    - 'G' = provider group name, e.g. "Gmygroupname".

    Total strlen(name + "_" + level + keyword + options) must be less than
    EVENTHEADER_NAME_MAX (256).
    */
    char const* options;

    /*
    Nul-terminated provider name string.

    Provider Name may not contain ' ' or ':' characters.

    Some decoding tools (e.g. tracefs) might impose additional restrictions on
    provider name. For best compatibility, use only ASCII identifier characters
    [A-Za-z0-9_] in provider names.

    Total strlen(name + "_" + level + keyword + options) must be less than
    EVENTHEADER_NAME_MAX (256).
    */
    char const* name;

} eventheader_provider;

enum {
    // 1. For use by tracepoint_write
    // 2. header + activity_id
    EVENTHEADER_PREFIX_DATAVEC_COUNT_NO_METADATA = 2,

    // 1. For use by tracepoint_write
    // 2. header + activity_id
    // 3. header extension block (metadata)
    EVENTHEADER_PREFIX_DATAVEC_COUNT = 3
};

#ifdef __cplusplus
extern "C" {
#endif // __cplusplus

    /*
    Closes the specified provider. Calling Close on an already-closed provider
    is a safe no-op.
    */
    void
    eventheader_close_provider(
        eventheader_provider const* pProvider);

    /*
    Opens the specified provider.

    - Returns 0 for success, errno otherwise. Result is primarily for
      debugging/diagnostics and is usually ignored for production code.

    PRECONDITION:

    - It is an error to call Open(pProvider) while Open or Close for that
      pProvider is running on another thread.
    - It is an error to call Open(pProvider) on a pProvider that is already
      open.
    */
    int
    eventheader_open_provider(
        eventheader_provider const* pProvider);

    /*
    Opens the specified provider and connects the specified events.

    - Returns 0 for success, errno otherwise. Result is primarily for
      debugging/diagnostics and is usually ignored for production code.
    - This function is intended for use when the caller is using
      __attribute__(section) to generate the event list. It sorts and
      de-duplicates the list (moving NULLs to the end), then initializes events
      starting at pEventsStart and stopping at pEventsStop or NULL, whichever
      comes first.

    PRECONDITION:

    - It is an error to call Open(pProvider) while Open or Close for that
      pProvider is running on another thread.
    - It is an error to call Open(pProvider) on a pProvider that is already
      open.
    */
    int
    eventheader_open_provider_with_events(
        eventheader_provider const* pProvider,
        eventheader_tracepoint const** pEventsStart,
        eventheader_tracepoint const** pEventsStop);

    /*
    Opens the specified event and associates it with the specified provider.
    Returns 0 for success, errno for failure. In case of failure, the event
    state is unchanged.

    Call eventheader_connect(pEvent, NULL) to disconnect an event.
    */
    int
    eventheader_connect(
        eventheader_tracepoint const* pEvent,
        eventheader_provider const* pProvider);

    /*
    Writes an event. Safe no-op if event is disabled.

    - Returns 0 for success, EBADF if event is disabled, errno for error.
      Result is primarily for debugging/diagnostics and is usually ignored
      for production code.
    - If pActivityId is not NULL, event will have an activity_id extension.
    - If pEvent->metadata is not NULL, event will have a extension with the
      data from pEvent->metadata.

    PRECONDITION:

    - The ActivityId parameters must either be NULL or must point at 16-byte
      IDs.
    - If pActivityId is NULL then pRelatedActivityId must be NULL.
    - If pEvent->metadata is not NULL then the first
      EVENTHEADER_PREFIX_DATAVEC_COUNT iovecs will be used for event headers,
      so dataCount must be >= EVENTHEADER_PREFIX_DATAVEC_COUNT. Event payload
      (if any) should start at dataVecs[EVENTHEADER_PREFIX_DATAVEC_COUNT].
    - If pEvent->metadata is NULL then the first
      EVENTHEADER_PREFIX_DATAVEC_COUNT_NO_METADATA iovecs will be used for
      event headers, so dataCount must be >=
      EVENTHEADER_PREFIX_DATAVEC_COUNT_NO_METADATA. Event payload (if any)
      should start at dataVecs[EVENTHEADER_PREFIX_DATAVEC_COUNT_NO_METADATA].

    Implementation details:

    - Always: dataVecs[0] will be populated with the event's write_index.
    - Always: dataVecs[1] will be populated with headers.
      - Always: eventheader.
      - If pActivityId != NULL: eventheader_extension + activity ids.
    - If pEvent->metadata != NULL: dataVecs[2] will be populated with the
      header extension block from pEvent->metadata.
    - Remaining dataVecs (if any) are populated by caller (event payload).
    - If you have header extensions in the payload:
      - If pEvent->metadata == NULL: set pEvent->header.flags's extension bit.
      - If pEvent->metadata != NULL: set pEvent->metadata.kind's chain bit.
    */
    int
    eventheader_write(
        eventheader_tracepoint const* pEvent,
        void const* pActivityId,
        void const* pRelatedActivityId,
        uint32_t dataCount,
        struct iovec* dataVecs);

#ifdef __cplusplus
} // extern "C"
#endif // __cplusplus

#endif // _included_eventheader_tracepoint_h
