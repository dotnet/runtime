// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

#include <tracepoint/TracepointSession.h>
#include <tracepoint/PerfDataFileWriter.h>
#include <algorithm>
#include <assert.h>
#include <errno.h>
#include <string.h>
#include <time.h>

#include <unistd.h>
#include <poll.h>
#include <sys/ioctl.h>
#include <sys/mman.h>
#include <sys/uio.h>
#include <sys/utsname.h>
#include <asm/unistd.h> // __NR_perf_event_open
#include <linux/perf_event.h>

#ifdef NDEBUG
#define DEBUG_PRINTF(...) ((void)0)
#else // NDEBUG
#include <stdio.h>
#define DEBUG_PRINTF(...) printf(__VA_ARGS__)
#endif // NDEBUG

using namespace std::string_view_literals;
using namespace tracepoint_control;
using namespace tracepoint_decode;

// Helpers

static long
perf_event_open(
    struct perf_event_attr* pe,
    pid_t pid,
    int cpuIndex,
    int groupFd,
    unsigned long flags) noexcept
{
    return syscall(__NR_perf_event_open, pe, pid, cpuIndex, groupFd, flags);
}

static perf_event_header const*
BufferDataPosToHeader(
    uint8_t const* bufferData,
    uint32_t recordBufferPos) noexcept
{
    assert(0 == (recordBufferPos & 7));
    return reinterpret_cast<perf_event_header const*>(bufferData + recordBufferPos);
}

// Return the smallest power of 2 that is >= pageSize and >= bufferSize.
// Assumes pageSize is a power of 2.
static uint32_t
RoundUpBufferSize(uint32_t pageSize, uint32_t bufferSize) noexcept
{
    static constexpr uint32_t BufferSizeMax = 0x80000000;

    assert(0 != pageSize);
    assert(0 == (pageSize & (pageSize - 1)));
    assert(bufferSize <= BufferSizeMax);

    for (size_t roundedSize = pageSize; roundedSize != 0; roundedSize <<= 1)
    {
        if (roundedSize >= bufferSize)
        {
            return roundedSize;
        }
    }

    return BufferSizeMax;
}

static tracepoint_decode::PerfEventSessionInfo
MakeSessionInfo(uint32_t clockid) noexcept
{
    static const auto Billion = 1000000000u;

    tracepoint_decode::PerfEventSessionInfo sessionInfo;
    sessionInfo.SetClockid(clockid);

    timespec monotonic;
    timespec realtime;
    if (0 == clock_gettime(clockid, &monotonic) &&
        0 == clock_gettime(CLOCK_REALTIME, &realtime))
    {
        uint64_t monotonicNS, realtimeNS;
        if (monotonic.tv_sec < realtime.tv_sec ||
            (monotonic.tv_sec == realtime.tv_sec && monotonic.tv_nsec < realtime.tv_nsec))
        {
            monotonicNS = 0;
            realtimeNS = static_cast<uint64_t>(realtime.tv_sec - monotonic.tv_sec) * Billion
                + realtime.tv_nsec - monotonic.tv_nsec;
        }
        else
        {
            realtimeNS = 0;
            monotonicNS = static_cast<uint64_t>(monotonic.tv_sec - realtime.tv_sec) * Billion
                + monotonic.tv_nsec - realtime.tv_nsec;
        }

        sessionInfo.SetClockData(clockid, realtimeNS, monotonicNS);
    }

    return sessionInfo;
}

namespace
{
    /*
    Manages a scatter-gather list of chunks of memory that need to be written to
    the perf.data file. This allows us to reduce the number of kernel calls.
    Instead of calling write() once per event (or twice if the event wraps), we
    call writev once for every 16 noncontinguous blocks of memory to be written.
    */
    class IovecList
    {
        static constexpr unsigned Max = 16;
        unsigned m_used = 0;
        iovec m_vecs[Max];

    public:

        unsigned
        RoomLeft() const noexcept
        {
            return Max - m_used;
        }

        void
        Add(uint8_t const* p, size_t c) noexcept
        {
            assert(m_used < Max); // Caller is responsible for checking RoomLeft().
            auto const lastVec = m_used - 1;
            if (0 != m_used &&
                p == static_cast<uint8_t*>(m_vecs[lastVec].iov_base) + m_vecs[lastVec].iov_len)
            {
                // This block immediately follows the last block. Merge the blocks.
                m_vecs[lastVec].iov_len += c;
            }
            else
            {
                m_vecs[m_used].iov_base = const_cast<uint8_t*>(p);
                m_vecs[m_used].iov_len = c;
                m_used += 1;
            }
        }

        _Success_(return == 0) int
        Flush(PerfDataFileWriter& output) noexcept
        {
            assert(m_used <= Max);

            int error;
            if (m_used == 0)
            {
                error = 0;
            }
            else
            {
                size_t cbToWrite = 0;
                for (unsigned i = 0; i != m_used; i += 1)
                {
                    cbToWrite += m_vecs[i].iov_len;
                    if (cbToWrite < m_vecs[i].iov_len)
                    {
                        error = ERANGE;
                        goto Done;
                    }
                }

                for (auto skip = 0u;;)
                {
                    auto const cbWritten = output.WriteEventDataIovecs(m_vecs + skip, m_used - skip);
                    if (cbWritten < 0)
                    {
                        error = errno;
                        break;
                    }
                    else if (static_cast<size_t>(cbWritten) == cbToWrite)
                    {
                        error = 0;
                        break;
                    }

                    // Partial write. Skip what was written and try again.

                    auto cbToSkip = static_cast<size_t>(cbWritten);
                    assert(cbToWrite > cbToSkip);
                    cbToWrite -= cbToSkip;

                    while (cbToSkip >= m_vecs[skip].iov_len)
                    {
                        cbToSkip -= m_vecs[skip].iov_len;
                        skip += 1;
                    }

                    assert(skip < m_used);
                    m_vecs[skip].iov_base = static_cast<uint8_t*>(m_vecs[skip].iov_base) + cbToSkip;
                    m_vecs[skip].iov_len = m_vecs[skip].iov_len - cbToSkip;
                }

                m_used = 0;
            }

        Done:

            return error;
        }
    };
}

// TracepointInfo

TracepointInfo::~TracepointInfo()
{
    return;
}

TracepointInfo::TracepointInfo() noexcept
{
    return;
}

tracepoint_decode::PerfEventMetadata const&
TracepointInfo::Metadata() const noexcept
{
    auto& self = *static_cast<TracepointSession::TracepointInfoImpl const*>(this);
    return *self.m_eventDesc.metadata;
}

tracepoint_decode::PerfEventDesc const&
TracepointInfo::EventDesc() const noexcept
{
    auto& self = *static_cast<TracepointSession::TracepointInfoImpl const*>(this);
    return self.m_eventDesc;
}

TracepointEnableState
TracepointInfo::EnableState() const noexcept
{
    auto& self = *static_cast<TracepointSession::TracepointInfoImpl const*>(this);
    return self.m_enableState;
}

_Success_(return == 0) int
TracepointInfo::GetEventCount(_Out_ uint64_t* value) const noexcept
{
    auto& self = *static_cast<TracepointSession::TracepointInfoImpl const*>(this);
    return self.GetEventCountImpl(value);
}

// ReadFormat

struct TracepointSession::ReadFormat
{
    // This needs to match the attr.read_format used for tracepoints.
    uint64_t value;
    uint64_t id;
};

// TracepointInfoImpl

TracepointSession::TracepointInfoImpl::~TracepointInfoImpl()
{
    return;
}

TracepointSession::TracepointInfoImpl::TracepointInfoImpl(
    tracepoint_decode::PerfEventDesc const& eventDesc,
    std::unique_ptr<char unsigned[]> eventDescStorage,
    std::unique_ptr<unique_fd[]> bufferFiles,
    unsigned bufferFilesCount
    ) noexcept
    : m_eventDesc(eventDesc)
    , m_eventDescStorage(std::move(eventDescStorage))
    , m_bufferFiles(std::move(bufferFiles))
    , m_bufferFilesCount(bufferFilesCount)
    , m_enableState(TracepointEnableState::Unknown)
{
    return;
}

_Success_(return == 0) int
TracepointSession::TracepointInfoImpl::Read(unsigned index, _Out_ ReadFormat* data) const noexcept
{
    assert(index < m_bufferFilesCount);
    auto const size = read(m_bufferFiles[index].get(), data, sizeof(data[0]));
    int const error = size == sizeof(data[0])
        ? 0
        : size < 0
        ? errno
        : EPIPE;
    return error;
}

_Success_(return == 0) int
TracepointSession::TracepointInfoImpl::GetEventCountImpl(_Out_ uint64_t* value) const noexcept
{
    int error = 0;
    uint64_t total = 0;

    for (unsigned i = 0; i != m_bufferFilesCount; i += 1)
    {
        if (!m_bufferFiles[i])
        {
            continue;
        }

        ReadFormat data;
        error = Read(i, &data);
        if (error != 0)
        {
            break;
        }

        total += data.value;
    }

    *value = total;
    return error;
}

// BufferInfo

TracepointSession::BufferInfo::~BufferInfo()
{
    return;
}

TracepointSession::BufferInfo::BufferInfo() noexcept
    : Mmap()
    , Size()
    , Data()
    , DataPos()
    , DataTail()
    , DataHead64()
{
    return;
}

// TracepointBookmark

TracepointSession::TracepointBookmark::TracepointBookmark(
    uint64_t timestamp,
    uint16_t bufferIndex,
    uint16_t recordSize,
    uint32_t recordBufferPos) noexcept
    : Timestamp(timestamp)
    , BufferIndex(bufferIndex)
    , RecordSize(recordSize)
    , RecordBufferPos(recordBufferPos)
{
    return;
}

// UnorderedEnumerator

TracepointSession::UnorderedEnumerator::~UnorderedEnumerator()
{
    m_session.EnumeratorEnd(m_bufferIndex);
}

TracepointSession::UnorderedEnumerator::UnorderedEnumerator(
    TracepointSession& session,
    uint32_t bufferIndex) noexcept
    : m_session(session)
    , m_bufferIndex(bufferIndex)
{
    m_session.EnumeratorBegin(bufferIndex);
}

bool
TracepointSession::UnorderedEnumerator::MoveNext() noexcept
{
    auto& session = m_session;
    return session.EnumeratorMoveNext(
        m_bufferIndex,
        [&session](
            BufferInfo const& buffer,
            uint16_t recordSize,
            uint32_t recordBufferPos)
        {
            auto const headerType = BufferDataPosToHeader(buffer.Data, recordBufferPos)->type;
            return headerType == PERF_RECORD_SAMPLE &&
                session.ParseSample(buffer, recordSize, recordBufferPos);
        });
}

//  OrderedEnumerator

TracepointSession::OrderedEnumerator::~OrderedEnumerator()
{
    if (m_needsCleanup)
    {
        for (unsigned bufferIndex = 0; bufferIndex != m_session.m_bufferCount; bufferIndex += 1)
        {
            if (m_session.m_buffers[bufferIndex].Size != 0)
            {
                m_session.EnumeratorEnd(bufferIndex);
            }
        }
    }
}

TracepointSession::OrderedEnumerator::OrderedEnumerator(TracepointSession& session) noexcept
    : m_session(session)
    , m_needsCleanup(false)
    , m_index(0)
{
    return;
}

_Success_(return == 0) int
TracepointSession::OrderedEnumerator::LoadAndSort() noexcept
{
    int error;
    auto& session = m_session;

    if (0 == (session.m_sampleType & PERF_SAMPLE_TIME))
    {
        error = EPERM;
    }
    else try
    {
        auto const sampleType = session.m_sampleType;
        unsigned const bytesBeforeTime = sizeof(uint64_t) * (
            1u + // perf_event_header
            (0 != (sampleType & PERF_SAMPLE_IDENTIFIER)) +
            (0 != (sampleType & PERF_SAMPLE_IP)) +
            (0 != (sampleType & PERF_SAMPLE_TID)));

        for (uint32_t bufferIndex = 0; bufferIndex != session.m_bufferCount; bufferIndex += 1)
        {
            if (session.m_buffers[bufferIndex].Size != 0)
            {
                session.EnumeratorBegin(bufferIndex);
            }
        }

        // Circular: If we throw an exception, we need to unpause during cleanup.
        // Realtime: If we throw an exception, we don't update tail pointers during cleanup.
        m_needsCleanup = !session.IsRealtime();

        session.m_enumeratorBookmarks.clear();
        for (uint32_t bufferIndex = 0; bufferIndex != session.m_bufferCount; bufferIndex += 1)
        {
            if (session.m_buffers[bufferIndex].Size == 0)
            {
                continue;
            }

            auto const startSize = session.m_enumeratorBookmarks.size();

            // Only need to call EnumeratorMoveNext once per buffer - it will loop until a callback
            // returns true or it reaches end of buffer, and our callback never returns true.
            session.EnumeratorMoveNext(
                bufferIndex,
                [bufferIndex, bytesBeforeTime, &session](
                    BufferInfo const& buffer,
                    uint16_t recordSize,
                    uint32_t recordBufferPos)
                {
                    assert(0 == (recordSize & 7));
                    assert(0 == (recordBufferPos & 7));

                    if (PERF_RECORD_SAMPLE != BufferDataPosToHeader(buffer.Data, recordBufferPos)->type)
                    {
                        return false; // Keep going.
                    }

                    if (recordSize <= bytesBeforeTime)
                    {
                        session.m_corruptEventCount += 1;
                        return false;
                    }

                    auto const timePos = (recordBufferPos + bytesBeforeTime) & (buffer.Size - 1);
                    auto const timestamp = *reinterpret_cast<uint64_t const*>(buffer.Data + timePos);
                    session.m_enumeratorBookmarks.emplace_back( // May throw bad_alloc.
                        timestamp,
                        bufferIndex,
                        recordSize,
                        recordBufferPos);
                    return false; // Keep going.
                });

            if (!session.IsRealtime())
            {
                // Circular buffers enumerate in reverse order. Fix that.
                auto const endSize = session.m_enumeratorBookmarks.size();
                auto const bookmarksData = session.m_enumeratorBookmarks.data();
                std::reverse(bookmarksData + startSize, bookmarksData + endSize);
            }
        }

        auto const bookmarksData = session.m_enumeratorBookmarks.data();
        std::stable_sort(
            bookmarksData,
            bookmarksData + session.m_enumeratorBookmarks.size(),
            [](TracepointBookmark const& a, TracepointBookmark const& b) noexcept
            {
                return a.Timestamp < b.Timestamp;
            });

        // From here on, do full cleanup.
        m_needsCleanup = true;
        error = 0;
    }
    catch (...)
    {
        error = ENOMEM;
    }

    return error;
}

bool
TracepointSession::OrderedEnumerator::MoveNext() noexcept
{
    auto& session = m_session;
    auto const buffers = session.m_buffers.get();
    auto const enumeratorBookmarksData = session.m_enumeratorBookmarks.data();
    auto const enumeratorBookmarksSize = session.m_enumeratorBookmarks.size();
    while (m_index < enumeratorBookmarksSize)
    {
        auto const& item = enumeratorBookmarksData[m_index];
        m_index += 1;

        if (session.ParseSample(
            buffers[item.BufferIndex],
            item.RecordSize,
            item.RecordBufferPos))
        {
            return true;
        }
    }

    return false;
}

// TracepointInfoIterator

TracepointInfoIterator::TracepointInfoIterator(InnerItTy it) noexcept
    : m_it(it)
{
    return;
}

TracepointInfoIterator::TracepointInfoIterator() noexcept
    : m_it()
{
    return;
}

TracepointInfoIterator&
TracepointInfoIterator::operator++() noexcept
{
    ++m_it;
    return *this;
}

TracepointInfoIterator
TracepointInfoIterator::operator++(int) noexcept
{
    TracepointInfoIterator old(m_it);
    ++m_it;
    return old;
}

TracepointInfoIterator::pointer
TracepointInfoIterator::operator->() const noexcept
{
    return &m_it->second;
}

TracepointInfoIterator::reference
TracepointInfoIterator::operator*() const noexcept
{
    return m_it->second;
}

bool
TracepointInfoIterator::operator==(TracepointInfoIterator other) const noexcept
{
    return m_it == other.m_it;
}

bool
TracepointInfoIterator::operator!=(TracepointInfoIterator other) const noexcept
{
    return m_it != other.m_it;
}

// TracepointInfoRange

TracepointInfoRange::TracepointInfoRange(RangeTy const& range) noexcept
    : m_range(range)
{
    return;
}

TracepointInfoIterator
TracepointInfoRange::begin() const noexcept
{
    return TracepointInfoIterator(m_range.begin());
}

TracepointInfoIterator
TracepointInfoRange::end() const noexcept
{
    return TracepointInfoIterator(m_range.end());
}

// TracepointSession

TracepointSession::~TracepointSession()
{
    return;
}

TracepointSession::TracepointSession(
    TracepointCache& cache,
    TracepointSessionMode mode,
    uint32_t perCpuBufferSize) noexcept(false)
    : TracepointSession(cache, TracepointSessionOptions(mode, perCpuBufferSize))
{
    return;
}

TracepointSession::TracepointSession(
    TracepointCache& cache,
    TracepointSessionOptions const& options) noexcept(false)
    : m_sessionInfo(MakeSessionInfo(CLOCK_MONOTONIC_RAW))
    , m_cache(cache)
    , m_mode(options.m_mode)
    , m_wakeupUseWatermark(options.m_wakeupUseWatermark)
    , m_wakeupValue(options.m_wakeupValue)
    , m_sampleType(options.m_sampleType)
    , m_bufferCount(CalculateBufferCount(options))
    , m_pageSize(sysconf(_SC_PAGESIZE))
    , m_buffers(MakeBufferInfos(m_bufferCount, m_pageSize, options)) // may throw bad_alloc.
    , m_tracepointInfoByCommonType() // may throw bad_alloc (but probably doesn't).
    , m_tracepointInfoBySampleId() // may throw bad_alloc (but probably doesn't).
    , m_bufferLeaderFiles(nullptr)
    , m_sampleEventCount(0)
    , m_lostEventCount(0)
    , m_corruptEventCount(0)
    , m_corruptBufferCount(0)
    , m_eventDataBuffer()
    , m_enumeratorBookmarks()
    , m_pollfd(nullptr)
    , m_enumEventInfo()
{
    assert(options.m_mode <= TracepointSessionMode::RealTime);
    assert(m_bufferCount > 0 && m_bufferCount < 0x10000000);
    assert(m_pageSize >= sizeof(perf_event_mmap_page) && m_pageSize < 0x10000000);
    assert((m_pageSize & (m_pageSize - 1)) == 0); // power of 2
}

TracepointCache&
TracepointSession::Cache() const noexcept
{
    return m_cache;
}

TracepointSessionMode
TracepointSession::Mode() const noexcept
{
    return m_mode;
}

PerfEventSessionInfo const&
TracepointSession::SessionInfo() const noexcept
{
    return m_sessionInfo;
}

bool
TracepointSession::IsRealtime() const noexcept
{
    return m_mode != TracepointSessionMode::Circular;
}

uint32_t
TracepointSession::BufferSize(unsigned bufferIndex) const noexcept
{
    assert(bufferIndex < BufferCount());
    return m_buffers[bufferIndex].Size;
}

uint32_t
TracepointSession::BufferCount() const noexcept
{
    return m_bufferCount;
}

uint64_t
TracepointSession::SampleEventCount() const noexcept
{
    return m_sampleEventCount;
}

uint64_t
TracepointSession::LostEventCount() const noexcept
{
    return m_lostEventCount;
}

uint64_t
TracepointSession::CorruptEventCount() const noexcept
{
    return m_corruptEventCount;
}

uint64_t
TracepointSession::CorruptBufferCount() const noexcept
{
    return m_corruptBufferCount;
}

void
TracepointSession::Clear() noexcept
{
    for (uint32_t bufferIndex = 0; bufferIndex != m_bufferCount; bufferIndex += 1)
    {
        m_buffers[bufferIndex].Mmap.reset();
        m_buffers[bufferIndex].Data = nullptr;
    }

    m_tracepointInfoByCommonType.clear();
    m_tracepointInfoBySampleId.clear();
    m_bufferLeaderFiles = nullptr;

    m_sampleEventCount = 0;
    m_lostEventCount = 0;
    m_corruptEventCount = 0;
    m_corruptBufferCount = 0;
}

_Success_(return == 0) int
TracepointSession::DisableTracepoint(unsigned id) noexcept
{
    auto const metadata = m_cache.FindById(id);
    auto const error = metadata == nullptr
        ? ENOENT
        : DisableTracepointImpl(*metadata);

    return error;
}

_Success_(return == 0) int
TracepointSession::DisableTracepoint(TracepointName name) noexcept
{
    int error;

    PerfEventMetadata const* metadata;
    error = m_cache.FindOrAddFromSystem(name, &metadata);
    if (error == 0)
    {
        error = DisableTracepointImpl(*metadata);
    }

    return error;
}

_Success_(return == 0) int
TracepointSession::EnableTracepoint(unsigned id) noexcept
{
    auto const metadata = m_cache.FindById(id);
    auto const error = metadata == nullptr
        ? ENOENT
        : EnableTracepointImpl(*metadata);

    return error;
}

_Success_(return == 0) int
TracepointSession::EnableTracepoint(TracepointName name) noexcept
{
    int error;

    PerfEventMetadata const* metadata;
    error = m_cache.FindOrAddFromSystem(name, &metadata);
    if (error == 0)
    {
        error = EnableTracepointImpl(*metadata);
    }

    return error;
}

TracepointInfoRange
TracepointSession::TracepointInfos() const noexcept
{
    return TracepointInfoRange(m_tracepointInfoByCommonType);
}

TracepointInfoIterator
TracepointSession::TracepointInfosBegin() const noexcept
{
    return TracepointInfoIterator(m_tracepointInfoByCommonType.begin());
}

TracepointInfoIterator
TracepointSession::TracepointInfosEnd() const noexcept
{
    return TracepointInfoIterator(m_tracepointInfoByCommonType.end());
}

TracepointInfoIterator
TracepointSession::FindTracepointInfo(unsigned id) const noexcept
{
    return TracepointInfoIterator(m_tracepointInfoByCommonType.find(id));
}

TracepointInfoIterator
TracepointSession::FindTracepointInfo(TracepointName name) const noexcept
{
    auto metadata = m_cache.FindByName(name);
    return TracepointInfoIterator(metadata
        ? m_tracepointInfoByCommonType.find(metadata->Id())
        : m_tracepointInfoByCommonType.end());
}

_Success_(return == 0) int
TracepointSession::WaitForWakeup(
    timespec const* timeout,
    sigset_t const* sigmask,
    _Out_opt_ int* pActiveCount) noexcept
{
    int error;
    int activeCount;

    if (!IsRealtime() || m_bufferLeaderFiles == nullptr)
    {
        activeCount = 0;
        error = EPERM;
    }
    else try
    {
        if (m_pollfd == nullptr)
        {
            m_pollfd = std::make_unique<pollfd[]>(m_bufferCount);
        }

        unsigned pollfdCount = 0;
        for (unsigned i = 0; i != m_bufferCount; i += 1)
        {
            if (m_buffers[i].Size != 0)
            {
                m_pollfd[pollfdCount] = { m_bufferLeaderFiles[i].get(), POLLIN, 0 };
                pollfdCount += 1;
            }
        }

        activeCount = ppoll(m_pollfd.get(), pollfdCount, timeout, sigmask);
        if (activeCount < 0)
        {
            activeCount = 0;
            error = errno;
        }
        else
        {
            error = 0;
        }
    }
    catch (...)
    {
        activeCount = 0;
        error = ENOMEM;
    }

    if (pActiveCount)
    {
        *pActiveCount = activeCount;
    }
    return error;
}

_Success_(return == 0) int
TracepointSession::GetBufferFiles(
    _Out_writes_(BufferCount()) int* pBufferFiles) const noexcept
{
    int error;

    if (m_bufferLeaderFiles == nullptr)
    {
        memset(pBufferFiles, 0, m_bufferCount * sizeof(pBufferFiles[0]));
        error = EPERM;
    }
    else
    {
        for (unsigned i = 0; i != m_bufferCount; i += 1)
        {
            pBufferFiles[i] = m_bufferLeaderFiles[i].get();
        }

        error = 0;
    }

    return error;
}

_Success_(return == 0) int
TracepointSession::SavePerfDataFile(
    _In_z_ char const* perfDataFileName,
    TracepointSavePerfDataFileOptions const& options) noexcept
{
    int error = 0;
    PerfDataFileWriter writer;
    TracepointTimestampRange writtenRange{}; // Start with an invalid range.
    bool timesValid;

    if (options.m_timestampWrittenRange)
    {
        *options.m_timestampWrittenRange = writtenRange;
    }

    error = writer.Create(perfDataFileName, options.m_openMode);
    if (error != 0)
    {
        goto Done;
    }

    // Mark the end of the "synthetic events" section (currently empty).

    error = writer.WriteFinishedInit();
    if (error != 0)
    {
        goto Done;
    }

    // Write event data:

    error = FlushToWriter(writer, &writtenRange, options.m_filterRange);
    if (error != 0)
    {
        goto Done;
    }

    timesValid = (m_sampleType & PERF_SAMPLE_TIME) && writtenRange.First <= writtenRange.Last;

    // Write system information headers:

    error = SetWriterHeaders(writer, timesValid ? &writtenRange : nullptr);
    if (error != 0)
    {
        goto Done;
    }

    // Flush to disk:

    error = writer.FinalizeAndClose();
    if (error != 0)
    {
        goto Done;
    }

    // Update output parameters:

    if (timesValid && options.m_timestampWrittenRange)
    {
        *options.m_timestampWrittenRange = writtenRange;
    }

Done:

    return error;
}

_Success_(return == 0) int
TracepointSession::FlushToWriter(
    tracepoint_decode::PerfDataFileWriter & writer,
    _Inout_ TracepointTimestampRange* writtenRange,
    TracepointTimestampRange filterRange) noexcept
{
    int error = 0;
    IovecList vecList;

    if (m_bufferLeaderFiles != nullptr)
    {
        auto recordFn = [this, &vecList, writtenRange, filterRange](
            BufferInfo const& buffer,
            uint16_t recordSize,
            uint32_t recordBufferPos) noexcept
            {
                // Look up the correct value for m_enumEventInfo.event_desc.

                m_enumEventInfo.event_desc = nullptr;
                if (PERF_RECORD_SAMPLE == BufferDataPosToHeader(buffer.Data, recordBufferPos)->type)
                {
                    // TODO: We don't need a full parse here. Could potentially
                    // save a few cycles by inlining ParseSample and removing the
                    // parts we don't need.

                    // If this succeeds it will set m_enumEventInfo.
                    if (ParseSample(buffer, recordSize, recordBufferPos))
                    {
                        if (filterRange.First > m_enumEventInfo.time ||
                            filterRange.Last < m_enumEventInfo.time)
                        {
                            // TODO: Optimization - in some cases, this means we're going
                            // to skip the rest of the buffer, so perhaps detect those
                            // cases and stop the enumeration?
                            return false; // Skip this event.
                        }

                        if (m_enumEventInfo.time < writtenRange->First)
                        {
                            writtenRange->First = m_enumEventInfo.time;
                        }

                        if (m_enumEventInfo.time > writtenRange->Last)
                        {
                            writtenRange->Last = m_enumEventInfo.time;
                        }
                    }
                }

                // Add event data to vecList.

                auto const unmaskedPosEnd = recordBufferPos + recordSize;
                if (unmaskedPosEnd <= buffer.Size)
                {
                    // Event does not wrap.
                    vecList.Add(buffer.Data + recordBufferPos, recordSize);
                }
                else
                {
                    // Event wraps.
                    vecList.Add(buffer.Data + recordBufferPos, buffer.Size - recordBufferPos);
                    vecList.Add(buffer.Data, unmaskedPosEnd - buffer.Size);
                }

                return true;
            };

        // Pause one buffer at a time.
        for (uint32_t bufferIndex = 0; bufferIndex != m_bufferCount; bufferIndex += 1)
        {
            EnumeratorBegin(bufferIndex);

            while (EnumeratorMoveNext(bufferIndex, recordFn))
            {
                if (m_enumEventInfo.event_desc)
                {
                    error = writer.AddTracepointEventDesc(*m_enumEventInfo.event_desc);
                    if (error != EEXIST && error != 0)
                    {
                        EnumeratorEnd(bufferIndex);
                        goto Done;
                    }
                }

                if (vecList.RoomLeft() < 2) // Next recordFn may need up to 2 calls to Add().
                {
                    error = vecList.Flush(writer);
                    if (error != 0)
                    {
                        EnumeratorEnd(bufferIndex);
                        goto Done;
                    }
                }
            }

            error = vecList.Flush(writer);

            EnumeratorEnd(bufferIndex);

            if (error != 0)
            {
                goto Done;
            }
        }
    }

Done:

    return error;
}

_Success_(return == 0) int
TracepointSession::SetWriterHeaders(
    tracepoint_decode::PerfDataFileWriter& writer,
    _In_opt_ TracepointTimestampRange const* writtenRange) noexcept
{
    int error;

    utsname uts;
    if (0 == uname(&uts))
    {
        // HOSTNAME, OSRELEASE, ARCH
        error = writer.SetUtsNameHeaders(uts);
        if (error != 0)
        {
            goto Done;
        }
    }

    {
        auto const conf = sysconf(_SC_NPROCESSORS_CONF);
        auto const onln = sysconf(_SC_NPROCESSORS_ONLN);
        if (conf > 0 && onln > 0)
        {
            // NRCPUS
            error = writer.SetNrCpusHeader(static_cast<uint32_t>(conf), static_cast<uint32_t>(onln));
            if (error != 0)
            {
                goto Done;
            }
        }
    }

    // CLOCKID, CLOCK_DATA
    error = writer.SetSessionInfoHeaders(m_sessionInfo);
    if (error != 0)
    {
        goto Done;
    }

    if (writtenRange != nullptr)
    {
        // SAMPLE_TIME
        error = writer.SetSampleTimeHeader(writtenRange->First, writtenRange->Last);
        if (error != 0)
        {
            goto Done;
        }
    }

Done:

    return error;
}

_Success_(return == 0) int
TracepointSession::DisableTracepointImpl(PerfEventMetadata const& metadata) noexcept
{
    int error;

    auto const existingIt = m_tracepointInfoByCommonType.find(metadata.Id());
    if (existingIt == m_tracepointInfoByCommonType.end())
    {
        error = ENOENT;
    }
    else
    {
        error = SetTracepointEnableState(existingIt->second, false);
    }

    return error;
}

_Success_(return == 0) int
TracepointSession::EnableTracepointImpl(PerfEventMetadata const& metadata) noexcept
{
    int error;

    auto const existingIt = m_tracepointInfoByCommonType.find(metadata.Id());
    if (existingIt == m_tracepointInfoByCommonType.end())
    {
        error = AddTracepoint(metadata, TracepointEnableState::Enabled);
    }
    else
    {
        error = SetTracepointEnableState(existingIt->second, true);
    }

    return error;
}

_Success_(return == 0) int
TracepointSession::IoctlForEachFile(
    _In_reads_(filesCount) unique_fd const* files,
    unsigned filesCount,
    unsigned long request,
    _In_reads_opt_(filesCount) unique_fd const* values) noexcept
{
    int error = 0;

    for (unsigned i = 0; i != filesCount; i += 1)
    {
        if (!files[i])
        {
            continue;
        }

        errno = 0;
        auto const value = values ? values[i].get() : 0;
        if (-1 == ioctl(files[i].get(), request, value))
        {
            error = errno;
            if (error == 0)
            {
                error = ENODEV;
            }
        }
    }

    return error;
}

bool
TracepointSession::ParseSample(
    BufferInfo const& buffer,
    uint16_t recordSize,
    uint32_t recordBufferPos) noexcept
{
    assert(buffer.Mmap);
    assert(buffer.Mmap.get() == buffer.Data - m_pageSize);
    assert(buffer.Mmap.get_size() == buffer.Size + m_pageSize);
    assert(0 == (recordSize & 7));
    assert(0 == (recordBufferPos & 7));
    assert(recordSize <= buffer.Size);
    assert(recordBufferPos < buffer.Size);

    uint8_t const* p;

    if (recordBufferPos + recordSize <= buffer.Size)
    {
        // Event does not wrap.
        p = buffer.Data + recordBufferPos;
    }
    else
    {
        // Event wraps. We need to double-buffer it.

        if (m_eventDataBuffer.size() < recordSize)
        {
            try
            {
                m_eventDataBuffer.resize(recordSize);
            }
            catch (...)
            {
                m_lostEventCount += 1;
                return false; // out of memory
            }
        }

        auto const afterWrap = recordBufferPos + recordSize - buffer.Size;
        auto const beforeWrap = buffer.Size - recordBufferPos;
        auto const eventDataBuffer = m_eventDataBuffer.data();
        memcpy(eventDataBuffer, buffer.Data + recordBufferPos, beforeWrap);
        memcpy(eventDataBuffer + beforeWrap, buffer.Data, afterWrap);
        p = eventDataBuffer;
    }

    auto const pEnd = p + recordSize;
    auto const infoSampleTypes = m_sampleType;
    uint64_t infoId = 0;
    PerfEventDesc const* infoEventDesc;
    char const* infoRawData = nullptr;
    uint32_t infoRawDataSize = 0;

    perf_event_header const* const infoHeader = reinterpret_cast<perf_event_header const*>(p);
    p += sizeof(perf_event_header);

    auto const SampleTypeSupported = 0u
        | PERF_SAMPLE_IDENTIFIER
        | PERF_SAMPLE_IP
        | PERF_SAMPLE_TID
        | PERF_SAMPLE_TIME
        | PERF_SAMPLE_ADDR
        | PERF_SAMPLE_ID
        | PERF_SAMPLE_STREAM_ID
        | PERF_SAMPLE_CPU
        | PERF_SAMPLE_PERIOD
        | PERF_SAMPLE_CALLCHAIN
        | PERF_SAMPLE_RAW;
    static_assert(
        SampleTypeSupported == TracepointSessionOptions::SampleTypeSupported,
        "SampleTypeSupported out of sync");

    auto const SampleTypeDefault = 0u
        | PERF_SAMPLE_IDENTIFIER
        | PERF_SAMPLE_TID
        | PERF_SAMPLE_TIME
        | PERF_SAMPLE_CPU
        | PERF_SAMPLE_RAW;
    static_assert(
        SampleTypeDefault == TracepointSessionOptions::SampleTypeDefault,
        "SampleTypeDefault out of sync");

    // Fast path for default sample type.
    if (infoSampleTypes == SampleTypeDefault)
    {
        if (recordSize <
            sizeof(perf_event_header) +
            sizeof(uint64_t) + // PERF_SAMPLE_IDENTIFIER
            sizeof(uint64_t) + // PERF_SAMPLE_TID
            sizeof(uint64_t) + // PERF_SAMPLE_TIME
            sizeof(uint64_t) + // PERF_SAMPLE_CPU
            sizeof(uint64_t))  // PERF_SAMPLE_RAW
        {
            goto Error;
        }

        // PERF_SAMPLE_IDENTIFIER
        infoId = *reinterpret_cast<uint64_t const*>(p);
        p += sizeof(uint64_t);

        // PERF_SAMPLE_TID
        auto const pTid = reinterpret_cast<uint32_t const*>(p);
        m_enumEventInfo.pid = pTid[0];
        m_enumEventInfo.tid = pTid[1];
        p += sizeof(uint64_t);

        // PERF_SAMPLE_TIME
        m_enumEventInfo.time = *reinterpret_cast<uint64_t const*>(p);
        p += sizeof(uint64_t);

        // PERF_SAMPLE_CPU
        auto const pCpu = reinterpret_cast<uint32_t const*>(p);
        m_enumEventInfo.cpu = pCpu[0];
        m_enumEventInfo.cpu_reserved = pCpu[1];
        p += sizeof(uint64_t);

        // PERF_SAMPLE_RAW
        goto PerfSampleRaw;
    }

    if (infoSampleTypes & PERF_SAMPLE_IDENTIFIER)
    {
        if (p == pEnd) goto Error;
        infoId = *reinterpret_cast<uint64_t const*>(p);
        p += sizeof(uint64_t);
    }

    if (infoSampleTypes & PERF_SAMPLE_IP)
    {
        if (p == pEnd) goto Error;
        m_enumEventInfo.ip = *reinterpret_cast<uint64_t const*>(p);
        p += sizeof(uint64_t);
    }

    if (infoSampleTypes & PERF_SAMPLE_TID)
    {
        if (p == pEnd) goto Error;
        auto const pTid = reinterpret_cast<uint32_t const*>(p);
        m_enumEventInfo.pid = pTid[0];
        m_enumEventInfo.tid = pTid[1];
        p += sizeof(uint64_t);
    }

    if (infoSampleTypes & PERF_SAMPLE_TIME)
    {
        if (p == pEnd) goto Error;
        m_enumEventInfo.time = *reinterpret_cast<uint64_t const*>(p);
        p += sizeof(uint64_t);
    }

    if (infoSampleTypes & PERF_SAMPLE_ADDR)
    {
        if (p == pEnd) goto Error;
        m_enumEventInfo.addr = *reinterpret_cast<uint64_t const*>(p);
        p += sizeof(uint64_t);
    }

    if (infoSampleTypes & PERF_SAMPLE_ID)
    {
        if (p == pEnd) goto Error;
        infoId = *reinterpret_cast<uint64_t const*>(p);
        p += sizeof(uint64_t);
    }

    if (infoSampleTypes & PERF_SAMPLE_STREAM_ID)
    {
        if (p == pEnd) goto Error;
        m_enumEventInfo.stream_id = *reinterpret_cast<uint64_t const*>(p);
        p += sizeof(uint64_t);
    }

    if (infoSampleTypes & PERF_SAMPLE_CPU)
    {
        if (p == pEnd) goto Error;
        auto const pCpu = reinterpret_cast<uint32_t const*>(p);
        m_enumEventInfo.cpu = pCpu[0];
        m_enumEventInfo.cpu_reserved = pCpu[1];
        p += sizeof(uint64_t);
    }

    if (infoSampleTypes & PERF_SAMPLE_PERIOD)
    {
        if (p == pEnd) goto Error;
        m_enumEventInfo.period = *reinterpret_cast<uint64_t const*>(p);
        p += sizeof(uint64_t);
    }

    if (infoSampleTypes & PERF_SAMPLE_CALLCHAIN)
    {
        if (p == pEnd) goto Error;
        auto const infoCallchain = reinterpret_cast<uint64_t const*>(p);
        m_enumEventInfo.callchain = infoCallchain;
        auto const count = *infoCallchain;
        p += sizeof(uint64_t);

        if ((pEnd - p) / sizeof(uint64_t) < count)
        {
            goto Error;
        }
        p += count * sizeof(uint64_t);
    }

    if (infoSampleTypes & PERF_SAMPLE_RAW)
    {
        if (p == pEnd) goto Error;

    PerfSampleRaw:

        assert(p < pEnd);

        auto const* pRaw = reinterpret_cast<uint32_t const*>(p);
        infoRawDataSize = pRaw[0];
        infoRawData = reinterpret_cast<char const*>(pRaw + 1);
        if ((pEnd - p) - sizeof(uint32_t) < infoRawDataSize)
        {
            goto Error;
        }

        assert(p + sizeof(uint32_t) + infoRawDataSize <= pEnd);

        // Try to look up eventDesc by common type field:

        decltype(m_tracepointInfoByCommonType.end()) infoIt;
        auto const commonTypeOffset = static_cast<uint32_t>(m_cache.CommonTypeOffset());
        auto const commonTypeSize = m_cache.CommonTypeSize();
        if (infoRawDataSize <= commonTypeOffset ||
            infoRawDataSize - commonTypeOffset <= commonTypeSize)
        {
            infoIt = m_tracepointInfoByCommonType.end();
        }
        else if (commonTypeSize == sizeof(uint16_t))
        {
            uint16_t commonType;
            memcpy(&commonType, infoRawData + commonTypeOffset, sizeof(commonType));
            infoIt = m_tracepointInfoByCommonType.find(commonType);
        }
        else if (commonTypeSize == sizeof(uint32_t))
        {
            uint32_t commonType;
            memcpy(&commonType, infoRawData + commonTypeOffset, sizeof(commonType));
            infoIt = m_tracepointInfoByCommonType.find(commonType);
        }
        else
        {
            assert(commonTypeSize == 1);
            uint8_t commonType;
            memcpy(&commonType, infoRawData + commonTypeOffset, sizeof(commonType));
            infoIt = m_tracepointInfoByCommonType.find(commonType);
        }

        if (infoIt != m_tracepointInfoByCommonType.end())
        {
            infoEventDesc = &infoIt->second.m_eventDesc;
            goto Done;
        }
    }
    else
    {
        assert(p <= pEnd);
    }

    if (infoSampleTypes & (PERF_SAMPLE_IDENTIFIER | PERF_SAMPLE_ID))
    {
        // Try to look up eventDesc by sample id:

        auto infoIt = m_tracepointInfoBySampleId.find(infoId);
        if (infoIt != m_tracepointInfoBySampleId.end())
        {
            infoEventDesc = &infoIt->second->m_eventDesc;
            goto Done;
        }
    }

    // Unable to locate eventDesc.
    goto Error;

Done:

    m_enumEventInfo.event_desc = infoEventDesc;
    m_enumEventInfo.session_info = &m_sessionInfo;
    m_enumEventInfo.header = infoHeader;
    m_enumEventInfo.id = infoId;
    m_enumEventInfo.raw_data = infoRawData;
    m_enumEventInfo.raw_data_size = infoRawDataSize;

    m_sampleEventCount += 1;
    return true;

Error:

    assert(p <= pEnd);

    m_enumEventInfo.event_desc = {};
    m_enumEventInfo.session_info = {};
    m_enumEventInfo.header = {};
    m_enumEventInfo.id = {};
    m_enumEventInfo.raw_data = {};
    m_enumEventInfo.raw_data_size = {};

    m_corruptEventCount += 1;
    return false;
}

void
TracepointSession::EnumeratorEnd(uint32_t bufferIndex) const noexcept
{
    auto const& buffer = m_buffers[bufferIndex];
    assert(buffer.Size != 0);

    if (!IsRealtime())
    {
        // Should not change while collection paused.
        assert(buffer.DataHead64 == __atomic_load_n(
            &static_cast<perf_event_mmap_page const*>(buffer.Mmap.get())->data_head,
            __ATOMIC_RELAXED));

        int error = ioctl(m_bufferLeaderFiles[bufferIndex].get(), PERF_EVENT_IOC_PAUSE_OUTPUT, 0);
        if (error != 0)
        {
            DEBUG_PRINTF("CPU%u unpause error %u\n", bufferIndex, error);
        }
    }
    else if (buffer.DataPos != buffer.DataTail)
    {
        // Create a new 64-bit tail value.
        uint64_t newTail64;
        static_assert(sizeof(buffer.DataPos) == 8 || sizeof(buffer.DataPos) == 4);
        if constexpr (sizeof(buffer.DataPos) == 8)
        {
            newTail64 = buffer.DataPos;
        }
        else
        {
            // Convert m_bufferDataPos to a 64-bit value relative to m_bufferDataHead64.
            // Order of operations needs to be careful about 64-bit wrapping, e.g.
            // - DataHead64 = 0x600000000
            // - DataHead32 = 0x000000000
            // - DataPos32  = 0x0FFFFFFF8
            // Correct newTail64 is 0x5FFFFFFF8, not 0x6FFFFFFF8
            newTail64 = buffer.DataHead64 - (static_cast<size_t>(buffer.DataHead64) - buffer.DataPos);
        }

        assert(buffer.DataHead64 - newTail64 <= buffer.Size);

        auto const bufferHeader = static_cast<perf_event_mmap_page*>(buffer.Mmap.get());

        // ATOMIC_RELEASE: perf_events.h recommends smp_mb() here.
        // For future consideration: Ordered enumerator could probably merge barriers.
        // For future consideration: This probably just needs a compiler barrier.
        __atomic_store_n(&bufferHeader->data_tail, newTail64, __ATOMIC_RELEASE);
    }
}

void
TracepointSession::EnumeratorBegin(uint32_t bufferIndex) noexcept
{
    auto const realtime = IsRealtime();
    if (!realtime)
    {
        int error = ioctl(m_bufferLeaderFiles[bufferIndex].get(), PERF_EVENT_IOC_PAUSE_OUTPUT, 1);
        if (error != 0)
        {
            DEBUG_PRINTF("CPU%u pause error %u\n", bufferIndex, error);
        }
    }

    auto& buffer = m_buffers[bufferIndex];
    auto const bufferHeader = static_cast<perf_event_mmap_page const*>(buffer.Mmap.get());

    // ATOMIC_ACQUIRE: perf_events.h recommends smp_rmb() here.
    // For future consideration: Ordered enumerator could probably merge barriers.
    buffer.DataHead64 = __atomic_load_n(&bufferHeader->data_head, __ATOMIC_ACQUIRE);

    if (0 != (buffer.DataHead64 & 7) ||
        m_pageSize != bufferHeader->data_offset ||
        buffer.Size != bufferHeader->data_size)
    {
        // Unexpected - corrupt trace buffer.
        DEBUG_PRINTF("CPU%u bad perf_event_mmap_page: head=%llx offset=%lx size=%lx\n",
            bufferIndex,
            (unsigned long long)buffer.DataHead64,
            (unsigned long)bufferHeader->data_offset,
            (unsigned long)bufferHeader->data_size);
        buffer.DataTail = static_cast<size_t>(buffer.DataHead64) - buffer.Size;
        buffer.DataPos = static_cast<size_t>(buffer.DataHead64);
        m_corruptBufferCount += 1;
    }
    else if (!realtime)
    {
        // Circular: write_backward == 1
        buffer.DataTail = static_cast<size_t>(buffer.DataHead64) - buffer.Size;
        buffer.DataPos = buffer.DataTail;
    }
    else
    {
        // Realtime: write_backward == 0
        auto const bufferDataTail64 = bufferHeader->data_tail;
        buffer.DataTail = static_cast<size_t>(bufferDataTail64);
        if (buffer.DataHead64 - bufferDataTail64 > buffer.Size)
        {
            // Unexpected - assume bad tail pointer.
            DEBUG_PRINTF("CPU%u bad data_tail: head=%llx tail=%llx\n",
                bufferIndex,
                (unsigned long long)buffer.DataHead64,
                (unsigned long long)bufferDataTail64);
            buffer.DataTail = static_cast<size_t>(buffer.DataHead64) - buffer.Size; // Ensure tail gets updated.
            buffer.DataPos = static_cast<size_t>(buffer.DataHead64);
            m_corruptBufferCount += 1;
        }
        else
        {
            buffer.DataPos = buffer.DataTail;
        }
    }
}

template<class RecordFn>
bool
TracepointSession::EnumeratorMoveNext(
    uint32_t bufferIndex,
    RecordFn&& recordFn) noexcept(noexcept(recordFn(std::declval<BufferInfo>(), 0u, 0u)))
{
    auto& buffer = m_buffers[bufferIndex];
    assert(buffer.Mmap);
    assert(buffer.Mmap.get() == buffer.Data - m_pageSize);
    assert(buffer.Mmap.get_size() == buffer.Size + m_pageSize);

    for (;;)
    {
        auto const remaining = static_cast<size_t>(buffer.DataHead64) - buffer.DataPos;
        if (remaining == 0)
        {
            break;
        }

        auto const eventHeaderBufferPos = buffer.DataPos & (buffer.Size - 1);
        auto const eventHeader = *BufferDataPosToHeader(buffer.Data, eventHeaderBufferPos);

        if (eventHeader.size == 0 ||
            eventHeader.size > remaining)
        {
            // - Circular: this is probably not a real problem - it's probably
            //   unused buffer space or a partially-overwritten event.
            // - Realtime: The buffer is corrupt.
            m_corruptBufferCount += IsRealtime();

            // In either case, buffer is done. Mark the buffer's events as consumed.
            buffer.DataPos = static_cast<size_t>(buffer.DataHead64);
            break;
        }

        if (0 != (eventHeader.size & 7))
        {
            // Unexpected - corrupt event header.
            DEBUG_PRINTF("CPU%u unaligned eventHeader.Size at pos %lx: %u\n",
                bufferIndex, (unsigned long)buffer.DataPos, eventHeader.size);

            // The event is corrupt, can't parse beyond it. Mark the buffer's events as consumed.
            m_corruptBufferCount += 1;
            buffer.DataPos = static_cast<size_t>(buffer.DataHead64);
            break;
        }

        buffer.DataPos += eventHeader.size;

        if (eventHeader.type == PERF_RECORD_LOST)
        {
            auto const newEventsLost64 = *reinterpret_cast<uint64_t const*>(
                buffer.Data + ((eventHeaderBufferPos + sizeof(perf_event_header) + sizeof(uint64_t)) & (buffer.Size - 1)));
            m_lostEventCount += newEventsLost64;
        }

        if (recordFn(buffer, eventHeader.size, eventHeaderBufferPos))
        {
            return true;
        }
    }

    return false;
}

_Success_(return == 0) int
TracepointSession::SetTracepointEnableState(
    TracepointInfoImpl & tpi,
    bool enabled) noexcept
{
    int error;

    static auto const UnknownState = TracepointEnableState::Unknown;
    auto const desiredState = enabled
        ? TracepointEnableState::Enabled
        : TracepointEnableState::Disabled;

    if (desiredState == tpi.m_enableState)
    {
        error = 0;
        goto Done;
    }

    tpi.m_enableState = UnknownState;

    error = IoctlForEachFile(
        tpi.m_bufferFiles.get(),
        tpi.m_bufferFilesCount,
        enabled ? PERF_EVENT_IOC_ENABLE : PERF_EVENT_IOC_DISABLE,
        nullptr);
    if (error == 0)
    {
        tpi.m_enableState = desiredState;
    }

Done:

    return error;
}

_Success_(return == 0) int
TracepointSession::AddTracepoint(
    PerfEventMetadata const& metadata,
    TracepointEnableState enableState) noexcept(false)
{
    int error;
    uint32_t cIdsAdded = 0;
    uint64_t* pIds = nullptr;

    try
    {
        auto const systemName = metadata.SystemName();
        auto const eventName = metadata.Name();
        if (systemName.size() > 65535 ||
            eventName.size() > 65535)
        {
            error = E2BIG;
            goto Error;
        }

        uint32_t nonzeroBufferCount = 0;
        for (uint32_t i = 0; i != m_bufferCount; i += 1)
        {
            if (m_buffers[i].Size != 0)
            {
                nonzeroBufferCount += 1;
            }
        }

        // We don't use the fields that were added after v3. Allocate space for
        // the full structure (we expose the structure to users) but don't ask
        // the kernel to look at the new fields.
        unsigned constexpr PerfEventAttrSizeUsed = PERF_ATTR_SIZE_VER3;

        auto const cbEventDescStorage =
            sizeof(perf_event_attr) +
            nonzeroBufferCount * sizeof(uint64_t) +
            systemName.size() + 1 + eventName.size() + 1;
        auto eventDescStorage = std::make_unique<char unsigned[]>(cbEventDescStorage);

        auto const pAttr = reinterpret_cast<perf_event_attr*>(eventDescStorage.get());
        pAttr->type = PERF_TYPE_TRACEPOINT;
        pAttr->size = PerfEventAttrSizeUsed;
        pAttr->config = metadata.Id();
        pAttr->sample_period = 1;
        pAttr->sample_type = m_sampleType;
        pAttr->read_format = PERF_FORMAT_ID; // Must align with the definition of struct ReadFormat.
        pAttr->watermark = m_wakeupUseWatermark;
        pAttr->use_clockid = 1;
        pAttr->write_backward = !IsRealtime();
        pAttr->wakeup_events = m_wakeupValue;
        pAttr->clockid = m_sessionInfo.Clockid();
        static_assert(offsetof(perf_event_attr, clockid) < PerfEventAttrSizeUsed);

        // pIds will be initialized after file handle creation.
        // cIdsAdded tracks initialization.
        pIds = reinterpret_cast<uint64_t*>(pAttr + 1);

        auto const pName = reinterpret_cast<char*>(pIds + nonzeroBufferCount);
        {
            size_t i = 0;
            memcpy(&pName[i], systemName.data(), systemName.size());
            i += systemName.size();
            pName[i] = ':';
            i += 1;
            memcpy(&pName[i], eventName.data(), eventName.size());
            i += eventName.size();
            pName[i] = '\0';
        }

        PerfEventDesc const eventDesc = {
            pAttr,
            pName,
            &metadata,
            pIds,
            nonzeroBufferCount
        };

        auto er = m_tracepointInfoByCommonType.try_emplace(metadata.Id(),
            eventDesc,
            std::move(eventDescStorage),
            std::make_unique<unique_fd[]>(m_bufferCount),
            m_bufferCount);
        assert(er.second);
        auto& tpi = er.first->second;
        tpi.m_enableState = enableState;

        // Starting from here, if there is an error then we must erase(metadata.Id).

        for (uint32_t bufferIndex = 0; bufferIndex != m_bufferCount; bufferIndex += 1)
        {
            if (m_buffers[bufferIndex].Size == 0)
            {
                continue;
            }

            errno = 0;
            tpi.m_bufferFiles[bufferIndex].reset(perf_event_open(pAttr, -1, bufferIndex, -1, PERF_FLAG_FD_CLOEXEC));
            if (!tpi.m_bufferFiles[bufferIndex])
            {
                error = errno;
                if (error == 0)
                {
                    error = ENODEV;
                }

                goto Error;
            }
        }

        if (m_bufferLeaderFiles)
        {
            // Leader already exists. Add this event to the leader's mmaps.
            error = IoctlForEachFile(tpi.m_bufferFiles.get(), m_bufferCount, PERF_EVENT_IOC_SET_OUTPUT, m_bufferLeaderFiles);
            if (error)
            {
                goto Error;
            }
        }
        else
        {
            // This is the first event. Make it the "leader" (the owner of the session buffers).
            auto const prot = IsRealtime()
                ? PROT_READ | PROT_WRITE
                : PROT_READ;
            for (uint32_t bufferIndex = 0; bufferIndex != m_bufferCount; bufferIndex += 1)
            {
                if (m_buffers[bufferIndex].Size == 0)
                {
                    continue;
                }

                auto const mmapSize = m_pageSize + m_buffers[bufferIndex].Size;

                errno = 0;
                auto cpuMap = mmap(nullptr, mmapSize, prot, MAP_SHARED, tpi.m_bufferFiles[bufferIndex].get(), 0);
                if (MAP_FAILED == cpuMap)
                {
                    error = errno;

                    // Clean up any mmaps that we opened.
                    for (uint32_t bufferIndex2 = 0; bufferIndex2 != bufferIndex; bufferIndex2 += 1)
                    {
                        m_buffers[bufferIndex2].Mmap.reset();
                        m_buffers[bufferIndex2].Data = nullptr;
                    }

                    if (error == 0)
                    {
                        error = ENODEV;
                    }

                    goto Error;
                }

                m_buffers[bufferIndex].Mmap.reset(cpuMap, mmapSize);
                m_buffers[bufferIndex].Data = static_cast<uint8_t*>(cpuMap) + m_pageSize;
            }
        }

        // Find the sample_ids for the new tracepoints.
        for (uint32_t i = 0; i != m_bufferCount; i += 1)
        {
            if (!tpi.m_bufferFiles[i])
            {
                continue;
            }

            ReadFormat data;
            error = tpi.Read(i, &data);
            if (error != 0)
            {
                goto Error;
            }

            pIds[cIdsAdded] = data.id;
            cIdsAdded += 1;

            auto const added = m_tracepointInfoBySampleId.emplace(data.id, &tpi).second;
            assert(added);
            (void)added;
        }

        assert(cIdsAdded == nonzeroBufferCount);

        // Success. Commit it. (No exceptions beyond this point.)

        if (!m_bufferLeaderFiles)
        {
            m_bufferLeaderFiles = tpi.m_bufferFiles.get(); // Commit this event as the leader.
        }

        goto Done;
    }
    catch (...)
    {
        error = ENOMEM;
        goto Error;
    }

Error:

    for (uint32_t i = 0; i != cIdsAdded; i += 1)
    {
        if (m_buffers[i].Size == 0)
        {
            continue;
        }

        m_tracepointInfoBySampleId.erase(pIds[i]);
    }

    // May or may not have been added yet. If not, erase does nothing.
    m_tracepointInfoByCommonType.erase(metadata.Id());

Done:

    return error;
}

uint32_t
TracepointSession::CalculateBufferCount(TracepointSessionOptions const& options) noexcept
{
    auto const cpuCount = static_cast<uint32_t>(sysconf(_SC_NPROCESSORS_ONLN));
    assert(cpuCount > 0);
    assert(cpuCount < 0x10000000);

    uint32_t bufferCount;
    if (options.m_cpuBufferSizes == nullptr && options.m_cpuBufferSizesCount == UINT32_MAX)
    {
        // Either they used the perCpuBufferSize constructor or they passed
        // garbage parameters to the cpuBufferSizes constructor.
        // Use one buffer per CPU.
        bufferCount = cpuCount;
    }
    else
    {
        // They used the cpuBufferSizes constructor.
        // Check TracepointSessionOptions constructor preconditions.
        assert(options.m_cpuBufferSizes != nullptr);
        assert(options.m_cpuBufferSizesCount > 0);
        assert(options.m_cpuBufferSizesCount < 0x10000000);

        // Each buffer may have a different size, and some sizes may be zero.
        // Don't waste space tracking zero-sized buffers.
        bufferCount = std::min(cpuCount, options.m_cpuBufferSizesCount);
        while (bufferCount > 1 && options.m_cpuBufferSizes[bufferCount - 1] == 0)
        {
            bufferCount -= 1;
        }
    }

    return bufferCount;
}

std::unique_ptr<TracepointSession::BufferInfo[]>
TracepointSession::MakeBufferInfos(
    uint32_t bufferCount,
    uint32_t pageSize,
    TracepointSessionOptions const& options) noexcept(false)
{
    assert(bufferCount > 0);
    assert(bufferCount < 0x10000000);
    assert(pageSize != 0);
    assert((pageSize & (pageSize - 1)) == 0);

    auto buffers = std::make_unique<BufferInfo[]>(bufferCount);

    if (options.m_cpuBufferSizes == nullptr && options.m_cpuBufferSizesCount == UINT32_MAX)
    {
        // Either they used the perCpuBufferSize constructor or they passed
        // garbage parameters to the cpuBufferSizes constructor.
        // All buffers will have the same non-zero size.
        auto const bufferSize = RoundUpBufferSize(pageSize, options.m_perCpuBufferSize);
        for (auto i = 0u; i != bufferCount; i += 1)
        {
            buffers[i].Size = bufferSize;
        }
    }
    else
    {
        assert(bufferCount <= options.m_cpuBufferSizesCount); // Ensured by CalculateBufferCount.

        // They used the cpuBufferSizes constructor.
        // Each buffer may have a different size, and some sizes may be zero.
        for (auto i = 0u; i != bufferCount; i += 1)
        {
            auto const bufferSize = options.m_cpuBufferSizes[i] != 0
                ? RoundUpBufferSize(pageSize, options.m_cpuBufferSizes[i])
                : 0u;
            buffers[i].Size = bufferSize;
        }
    }

    return buffers;
}
