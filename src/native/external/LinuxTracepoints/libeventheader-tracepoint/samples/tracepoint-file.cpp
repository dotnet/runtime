// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

/*
Implementation of the tracepoint.h interface that writes events to a file.
*/

#include <tracepoint/tracepoint.h>
#include <tracepoint/tracepoint-impl.h>
#include <eventheader/eventheader.h>

#include <assert.h>
#include <fcntl.h>
#include <string.h>
#include <unistd.h>

#include <string>
#include <string_view>
#include <vector>
#include <shared_mutex>

static uint16_t const WriteIndexMax = 65535;

struct TracepointInfo
{
    std::string Name;
    unsigned* StatusPtr = nullptr;
};

static std::shared_mutex s_eventsMutex; // Also guards access to any tracepoint_provider_state
static std::vector<TracepointInfo> s_eventsByWriteIndex;
static int s_eventsFile = -1;
static unsigned s_eventsFileRefCount = 0;

static char const* const InterceptorFileNameDefault = "Interceptor.dat";
char const* g_interceptorFileName = InterceptorFileNameDefault;

void
tracepoint_close_provider(tracepoint_provider_state* providerState)
{
    int fileToClose = -1;

    // Scope for lock.
    {
        auto lock = std::lock_guard(s_eventsMutex);

        if (providerState->data_file != -1)
        {
            assert(providerState->data_file > -1);

            assert(s_eventsFileRefCount != 0);
            s_eventsFileRefCount -= 1;
            if (s_eventsFileRefCount == 0)
            {
                fileToClose = s_eventsFile;
                s_eventsFile = -1;
                s_eventsByWriteIndex.clear();
            }
        }

        tracepoint_close_provider_impl(providerState);
    }

    if (fileToClose != -1)
    {
        close(fileToClose);
    }
}

int
tracepoint_open_provider(tracepoint_provider_state* providerState)
{
    int err;
    auto lock = std::lock_guard(s_eventsMutex);

    if (providerState->data_file != -1)
    {
        assert(providerState->data_file == -1); // PRECONDITION
        abort(); // PRECONDITION
    }

    if (s_eventsFile == -1)
    {
        assert(s_eventsFileRefCount == 0);

        s_eventsFile = open(g_interceptorFileName,
            O_WRONLY | O_CLOEXEC | O_CREAT | O_APPEND,
            S_IRUSR | S_IWUSR | S_IRGRP | S_IROTH);
        if (s_eventsFile == -1)
        {
            err = errno;
            goto Done;
        }
    }

    s_eventsFileRefCount += 1;
    tracepoint_open_provider_impl(providerState, s_eventsFile);
    err = 0;

Done:

    return err;
}

int
tracepoint_connect(
    tracepoint_state* eventState,
    tracepoint_provider_state* providerState,
    char const* eventNameArgs)
{
    int err;
    int writeIndex = -1;

    auto lock = std::lock_guard(s_eventsMutex);

    if (providerState == NULL)
    {
        auto const wi = static_cast<uint32_t>(__atomic_load_n(&eventState->write_index, __ATOMIC_RELAXED));
        if (wi >= s_eventsByWriteIndex.size())
        {
            err = EINVAL;
        }
        else if (auto& e = s_eventsByWriteIndex[wi];
            e.StatusPtr == nullptr)
        {
            err = EINVAL;
        }
        else
        {
            e.StatusPtr = nullptr;
            e.Name.clear();
            err = 0;
        }
    }
    else try
    {
        // eventNameArgs = "EventName ArgList". We just want EventName.
        auto const eventNameEnd = strchr(eventNameArgs, ' ');
        if (eventNameEnd == nullptr)
        {
            err = EINVAL;
            goto Done;
        }

        auto const eventName = std::string_view(eventNameArgs, eventNameEnd - eventNameArgs);
        if (eventName.size() >= EVENTHEADER_NAME_MAX)
        {
            err = EINVAL;
            goto Done;
        }

        if (s_eventsByWriteIndex.size() > WriteIndexMax)
        {
            err = E2BIG;
            goto Done;
        }

        auto const pStatusWord = &eventState->status_word;
        s_eventsByWriteIndex.push_back({ std::string(eventName), pStatusWord });

        // In this sample, events are always enabled.
        __atomic_store_n(pStatusWord, 1, __ATOMIC_RELAXED);

        writeIndex = static_cast<int>(s_eventsByWriteIndex.size() - 1);
        err = 0;
    }
    catch (std::bad_alloc const&)
    {
        err = ENOMEM;
    }
    catch (...)
    {
        err = EINVAL;
    }

Done:

    tracepoint_connect_impl(eventState, providerState, writeIndex);
    return err;
}

int
tracepoint_open_provider_with_tracepoints(
    tracepoint_provider_state* provider_state,
    tracepoint_definition const** tp_definition_start,
    tracepoint_definition const** tp_definition_stop)
{
    return tracepoint_open_provider_with_tracepoints_impl(
        provider_state,
        tp_definition_start,
        tp_definition_stop);
}

int
tracepoint_write(
    tracepoint_state const* eventState,
    unsigned dataCount,
    struct iovec* dataVecs)
{
    assert((int)dataCount >= 1);
    assert(dataVecs[0].iov_len == 0);

    if (!TRACEPOINT_ENABLED(eventState))
    {
        return EBADF;
    }

    size_t size = 0;
    for (unsigned i = 1; i < dataCount; i += 1)
    {
        size += dataVecs[i].iov_len;
        if (size < dataVecs[i].iov_len)
        {
            return E2BIG;
        }
    }

    auto const providerState = __atomic_load_n(&eventState->provider_state, __ATOMIC_RELAXED);
    if (providerState == NULL)
    {
        return EBADF;
    }

    auto lock = std::shared_lock(s_eventsMutex);

    // Look up our tracking info for this event.
    auto const writeIndex = static_cast<uint32_t>(__atomic_load_n(&eventState->write_index, __ATOMIC_RELAXED));
    if (writeIndex >= s_eventsByWriteIndex.size())
    {
        return EINVAL;
    }

    auto const& tpi = s_eventsByWriteIndex[writeIndex];
    if (tpi.StatusPtr == nullptr)
    {
        return EINVAL;
    }

    auto const headerSize = sizeof(uint32_t) + tpi.Name.size() + 1;
    size += headerSize;
    if (size < headerSize || size != (uint32_t)size)
    {
        return E2BIG;
    }

    struct
    {
        uint32_t LittleEndianRecordSize;
        char TracepointName[EVENTHEADER_NAME_MAX];
    } header;

    header.LittleEndianRecordSize = htole32((uint32_t)size);

    assert(tpi.Name.size() < sizeof(header.TracepointName)); // Was checked in tracepoint_connect.
    memcpy(header.TracepointName, tpi.Name.c_str(), tpi.Name.size() + 1);

    assert(s_eventsFile == providerState->data_file);

    /*
    On-disk record format:
    uint32_t LittleEndianRecordSize;
    char[] NulTerminatedTracepointName;
    char[] EventData;
    */
    dataVecs[0].iov_base = &header;
    dataVecs[0].iov_len = headerSize;
    return 0 <= writev(providerState->data_file, dataVecs, (int)dataCount)
        ? 0
        : errno;
}
