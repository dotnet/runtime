// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

/*
Helper functions for use by implementations of the tracepoint interface.
*/

#pragma once
#ifndef _included_tracepoint_impl_h
#define _included_tracepoint_impl_h 1

#include <tracepoint/tracepoint.h>
#include <stddef.h>
#include <stdlib.h> // qsort

#ifndef assert
#include <assert.h>
#endif

#ifdef WIN32
#define __atomic_store_n(ptr, val, order) *(ptr) = (val)
#endif

// Don't warn if user of this header doesn't use every function.
#pragma GCC diagnostic push
#pragma GCC diagnostic ignored "-Wunused-function"

// For use by tracepoint_fix_array.
static int
tracepoint_fix_array_compare(void const* p1, void const* p2)
{
    // Reverse sort so that NULL goes at end.
    void const* v1 = *(void const* const*)p1;
    void const* v2 = *(void const* const*)p2;
    return v1 < v2 ? 1 : v1 == v2 ? 0 : -1;
}

/*
Remove duplicates and NULLs from an array of pointers.
For use by tracepoint_open_provider_with_tracepoints.
Returns the new end of the array (the position after the last non-NULL value).
*/
static void*
tracepoint_fix_array(void const** start_ptr, void const** stop_ptr)
{
    void const** good_ptr = start_ptr;
    void const** next_ptr;

    assert(stop_ptr - start_ptr >= 0);
    assert(stop_ptr - start_ptr <= 0x7fffffff);

    if (start_ptr != stop_ptr)
    {
        // Sort.
        qsort(start_ptr, (size_t)(stop_ptr - start_ptr), sizeof(void*), tracepoint_fix_array_compare);

        // Remove adjacent repeated elements.
        for (; good_ptr + 1 != stop_ptr; good_ptr += 1)
        {
            if (*good_ptr == *(good_ptr + 1))
            {
                void const** next_ptr;
                for (next_ptr = good_ptr + 2; next_ptr != stop_ptr; next_ptr += 1)
                {
                    if (*good_ptr != *next_ptr)
                    {
                        good_ptr += 1;
                        *good_ptr = *next_ptr;
                    }
                }
                break;
            }
        }

        if (*good_ptr != NULL)
        {
            good_ptr += 1;
        }

        // Fill any remaining entries with NULL
        for (next_ptr = good_ptr; next_ptr != stop_ptr; next_ptr += 1)
        {
            if (*next_ptr == NULL)
            {
                break;
            }

            *next_ptr = NULL;
        }
    }

    return good_ptr;
}

/*
Default implementation of tracepoint_open_provider_with_tracepoints.
*/
static int
tracepoint_open_provider_with_tracepoints_impl(
    tracepoint_provider_state* provider_state,
    tracepoint_definition const** tp_definition_start,
    tracepoint_definition const** tp_definition_stop)
{
    int err = tracepoint_open_provider(provider_state);
    if (err != 0)
    {
        return err;
    }

    tracepoint_definition const** adjusted_stop = (tracepoint_definition const**)
        tracepoint_fix_array((void const**)tp_definition_start, (void const**)tp_definition_stop);
    int const count = (int)(adjusted_stop - tp_definition_start);
    for (int i = 0; i < count; i += 1)
    {
        (void)tracepoint_connect(
            tp_definition_start[i]->state,
            provider_state,
            tp_definition_start[i]->tp_name_args);
    }

    return 0;
}

/*
Performs common initialization for an implementation of
tracepoint_open_provider.

This should be called while holding a lock guarding the providers so that the
updates appear atomic.

- If there are any tracepoints connected to this provider, disconnects them and
  resets them to TRACEPOINT_STATE_INIT.
- Sets provider's data_file field to the specified value.
- Sets provider's next and prev fields to &tracepoint_list_head.
*/
static void
tracepoint_open_provider_impl(
    tracepoint_provider_state* provider_state,
    int data_file)
{
    // Reset all tracepoints in our list.
    tracepoint_list_node* node = provider_state->tracepoint_list_head.next;
    if (node != NULL)
    {
        assert(node->prev == &provider_state->tracepoint_list_head);
        while (node != &provider_state->tracepoint_list_head)
        {
            tracepoint_state* tp_state = (tracepoint_state*)((char*)node - offsetof(tracepoint_state, tracepoint_list_link));
            node = node->next;
            assert(node->prev == &tp_state->tracepoint_list_link);

            __atomic_store_n(&tp_state->status_word, 0, __ATOMIC_RELAXED);
            __atomic_store_n(&tp_state->write_index, -1, __ATOMIC_RELAXED);
            __atomic_store_n(&tp_state->provider_state, NULL, __ATOMIC_RELAXED);
            tp_state->tracepoint_list_link.next = NULL;
            tp_state->tracepoint_list_link.prev = NULL;
        }
    }

    __atomic_store_n(&provider_state->data_file, data_file, __ATOMIC_RELAXED);
    __atomic_store_n(&provider_state->ref_count, 0, __ATOMIC_RELAXED);
    provider_state->tracepoint_list_head.next = &provider_state->tracepoint_list_head;
    provider_state->tracepoint_list_head.prev = &provider_state->tracepoint_list_head;
}

/*
Performs common initialization for an implementation of
tracepoint_close_provider.

This should be called while holding a lock guarding the providers so that the
updates appear atomic.

- If there are any tracepoints connected to this provider, disconnects them and
  resets them to TRACEPOINT_STATE_INIT.
- Sets provider's data_file field to -1.
- Sets provider's next and prev fields to &tracepoint_list_head.
*/
static void
tracepoint_close_provider_impl(
    tracepoint_provider_state* provider_state)
{
    tracepoint_open_provider_impl(provider_state, -1);
}

/*
Performs common initialization for an implementation of tracepoint_connect.

This should be called while holding a lock guarding the providers so that the
updates appear atomic.

- Removes tp_state from its current list, if any.
- Initializes tp_state with the specified field values.
- If provider_state is not NULL, adds tp_state to provider_state's list.
*/
static void
tracepoint_connect_impl(
    tracepoint_state* tp_state,
    tracepoint_provider_state* provider_state,
    int write_index)
{
    if (tp_state->provider_state != NULL)
    {
        // Disconnect from existing provider.
        assert(tp_state->tracepoint_list_link.next->prev == &tp_state->tracepoint_list_link);
        tp_state->tracepoint_list_link.next->prev = tp_state->tracepoint_list_link.prev;
        assert(tp_state->tracepoint_list_link.prev->next == &tp_state->tracepoint_list_link);
        tp_state->tracepoint_list_link.prev->next = tp_state->tracepoint_list_link.next;
    }
    else
    {
        assert(tp_state->tracepoint_list_link.next == NULL);
        assert(tp_state->tracepoint_list_link.prev == NULL);
    }

    // Initialize event fields.
    __atomic_store_n(&tp_state->write_index, write_index, __ATOMIC_RELAXED);
    __atomic_store_n(&tp_state->provider_state, provider_state, __ATOMIC_RELAXED);

    if (provider_state == NULL)
    {
        // Leave disconnected
        __atomic_store_n(&tp_state->status_word, 0, __ATOMIC_RELAXED);
        tp_state->tracepoint_list_link.next = NULL;
        tp_state->tracepoint_list_link.prev = NULL;
    }
    else
    {
        // Add to new provider's list.
        if (provider_state->tracepoint_list_head.next == NULL)
        {
            assert(provider_state->tracepoint_list_head.prev == NULL);
            provider_state->tracepoint_list_head.next = &provider_state->tracepoint_list_head;
            provider_state->tracepoint_list_head.prev = &provider_state->tracepoint_list_head;
        }
        else
        {
            assert(provider_state->tracepoint_list_head.prev != NULL);
        }

        tracepoint_list_node* next = provider_state->tracepoint_list_head.next;
        provider_state->tracepoint_list_head.next = &tp_state->tracepoint_list_link;
        next->prev = &tp_state->tracepoint_list_link;
        tp_state->tracepoint_list_link.next = next;
        tp_state->tracepoint_list_link.prev = &provider_state->tracepoint_list_head;
    }
}

#pragma GCC diagnostic pop // "-Wunused-function"

#endif // _included_tracepoint_impl_h
