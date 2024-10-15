// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

#include <eventheader/eventheader-tracepoint.h>
#include <tracepoint/tracepoint.h>
#include <tracepoint/tracepoint-impl.h>

#include <stdlib.h>
#include <assert.h>
#include <errno.h>
#include <inttypes.h>
#include <stdio.h>
#include <string.h>

#ifndef _uehp_FUNC_ATTRIBUTES
#define _uehp_FUNC_ATTRIBUTES //__attribute__((weak, visibility("hidden")))
#endif // _uehp_FUNC_ATTRIBUTES

#ifdef __cplusplus
extern "C" {
#endif // __cplusplus

    int
    eventheader_open_provider(
        eventheader_provider const* pProvider) _uehp_FUNC_ATTRIBUTES;
    int
    eventheader_open_provider(
        eventheader_provider const* pProvider)
    {
        assert(pProvider->state);
        assert(pProvider->name);
        assert(NULL == strchr(pProvider->name, ' '));
        assert(NULL == strchr(pProvider->name, ':'));

        if (pProvider->options != NULL)
        {
            assert(NULL == strchr(pProvider->options, ' '));
            assert(NULL == strchr(pProvider->options, ':'));
            assert(NULL == strchr(pProvider->options, '_'));
        }

        return tracepoint_open_provider(pProvider->state);
    }

    int
    eventheader_open_provider_with_events(
        eventheader_provider const* pProvider,
        eventheader_tracepoint const** pEventsStart,
        eventheader_tracepoint const** pEventsStop) _uehp_FUNC_ATTRIBUTES;
    int
    eventheader_open_provider_with_events(
        eventheader_provider const* pProvider,
        eventheader_tracepoint const** pEventsStart,
        eventheader_tracepoint const** pEventsStop)
    {
        int err = eventheader_open_provider(pProvider);
        if (err != 0)
        {
            return err;
        }

        eventheader_tracepoint const** adjustedEventPtrsStop =
            tracepoint_fix_array((void const**)pEventsStart, (void const**)pEventsStop);

        int const eventCount = (int)(adjustedEventPtrsStop - pEventsStart);
        for (int i = 0; i < eventCount; i += 1)
        {
            eventheader_tracepoint const* const pEvent = pEventsStart[i];

            assert(0 == __atomic_load_n(&pEvent->state->status_word, __ATOMIC_RELAXED));
            assert(-1 == __atomic_load_n(&pEvent->state->write_index, __ATOMIC_RELAXED));
            assert(NULL == __atomic_load_n(&pEvent->state->provider_state, __ATOMIC_RELAXED));

            (void)eventheader_connect(pEvent, pProvider);
        }

        return 0;
    }

    void
    eventheader_close_provider(
        eventheader_provider const* pProvider) _uehp_FUNC_ATTRIBUTES;
    void
    eventheader_close_provider(
        eventheader_provider const* pProvider)
    {
        tracepoint_close_provider(pProvider->state);
    }

    int
    eventheader_connect(
        eventheader_tracepoint const* pEvent,
        eventheader_provider const* pProvider) _uehp_FUNC_ATTRIBUTES;
    int
    eventheader_connect(
        eventheader_tracepoint const* pEvent,
        eventheader_provider const* pProvider)
    {
        int err;

        char command[EVENTHEADER_COMMAND_MAX];
        if (pProvider == NULL)
        {
            err = tracepoint_connect(pEvent->state, NULL, NULL);
        }
        else if (EVENTHEADER_COMMAND_MAX <= (unsigned)EVENTHEADER_FORMAT_COMMAND(
            command, sizeof(command),
            pProvider->name, pEvent->header.level, pEvent->keyword, pProvider->options))
        {
            assert(!"Full name too long");
            err = E2BIG;
        }
        else
        {
            err = tracepoint_connect(pEvent->state, pProvider->state, command);
        }

        return err;
    }

    int
    eventheader_write(
        eventheader_tracepoint const* pEvent,
        void const* pActivityId,
        void const* pRelatedActivityId,
        uint32_t dataCount,
        struct iovec* dataVecs) _uehp_FUNC_ATTRIBUTES;
    int
    eventheader_write(
        eventheader_tracepoint const* pEvent,
        void const* pActivityId,
        void const* pRelatedActivityId,
        uint32_t dataCount,
        struct iovec* dataVecs)
    {
        uint8_t headers[0
            + sizeof(eventheader)
            + sizeof(eventheader_extension) + 32]; // ActivityId + RelatedActivityId
        size_t iHeaders = 0;

        eventheader* pHeader = (eventheader*)&headers[iHeaders];
        iHeaders += sizeof(eventheader);
        *pHeader = pEvent->header;

        if (pActivityId == NULL)
        {
            assert(pRelatedActivityId == NULL);
        }
        else
        {
            pHeader->flags |= eventheader_flag_extension;

            eventheader_extension* pExt = (eventheader_extension*)&headers[iHeaders];
            iHeaders += sizeof(eventheader_extension);
            pExt->kind = pEvent->metadata || (pEvent->header.flags & eventheader_flag_extension)
                ? (eventheader_extension_kind_activity_id | eventheader_extension_kind_chain_flag)
                : (eventheader_extension_kind_activity_id);

            pExt->size = 16;
            memcpy(&headers[iHeaders], pActivityId, 16);
            iHeaders += 16;

            if (pRelatedActivityId != NULL)
            {
                pExt->size = 32;
                memcpy(&headers[iHeaders], pRelatedActivityId, 16);
                iHeaders += 16;
            }
        }

        assert(iHeaders <= sizeof(headers));

        assert(dataVecs != NULL);
        assert((int)dataCount >= EVENTHEADER_PREFIX_DATAVEC_COUNT_NO_METADATA);
        dataVecs[0].iov_len = 0;
        dataVecs[1].iov_base = headers;
        dataVecs[1].iov_len = iHeaders;

        if (pEvent->metadata != NULL)
        {
            pHeader->flags |= eventheader_flag_extension;

            assert((int)dataCount >= EVENTHEADER_PREFIX_DATAVEC_COUNT);
            dataVecs[2].iov_base = (void*)pEvent->metadata;
            dataVecs[2].iov_len = pEvent->metadata->size + sizeof(eventheader_extension);
        }

        return tracepoint_write(pEvent->state, dataCount, dataVecs);
    }

#ifdef __cplusplus
} // extern "C"
#endif // __cplusplus
