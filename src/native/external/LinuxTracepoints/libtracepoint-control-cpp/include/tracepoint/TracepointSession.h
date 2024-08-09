// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

/*
TracepointSession class that manages a tracepoint collection session.
*/

#pragma once
#ifndef _included_TracepointSession_h
#define _included_TracepointSession_h

#include "TracepointName.h"
#include <tracepoint/PerfEventMetadata.h>
#include <tracepoint/PerfEventInfo.h>
#include <tracepoint/PerfEventSessionInfo.h>
#include <tracepoint/PerfDataFileDefs.h>
#include <tracepoint/TracepointCache.h>

#include <unordered_map>
#include <memory>
#include <vector>

#include <signal.h> // sigset_t

#ifndef _In_opt_
#define _In_opt_
#endif
#ifndef _In_z_
#define _In_z_
#endif
#ifndef _In_reads_
#define _In_reads_(size)
#endif
#ifndef _In_reads_opt_
#define _In_reads_opt_(size)
#endif
#ifndef _Inout_
#define _Inout_
#endif
#ifndef _Out_opt_
#define _Out_opt_
#endif
#ifndef _Out_writes_
#define _Out_writes_(size)
#endif

// Forward declarations:
struct pollfd; // From poll.h
struct timespec; // From time.h
namespace tracepoint_decode
{
    class PerfDataFileWriter; // From tracepoint/PerfDataFileWriter.h.
}

namespace tracepoint_control
{
    /*
    Mode to use for a tracepoint collection session:

    - Circular: Used for "flight recorder" scenarios. Events are collected
      into fixed-size buffers (one buffer per CPU). When a buffer is full, new
      events overwrite old events. At any point, you can pause collection,
      enumerate the contents of the buffer, and resume collection. Events
      received while collection is paused will be lost.

      For example, you can record information about what is happening on the
      system into memory, and then if a program crashes, you save the data to
      disk so you can discover what was happening on the system in the period
      leading up to the crash.

    - RealTime: Used for logging/tracing scenarios. Events are collected into
      fixed-size buffers (one buffer per CPU). When a buffer is full, events
      will be lost. At any point, you can enumerate events from the buffer,
      consuming them to make room for new events (no pause required).
    */
    enum class TracepointSessionMode : unsigned char
    {
        /*
        Buffers will be managed as circular:

        - If buffer is full, new events will overwrite old events.
        - Natural event enumeration order is newest-to-oldest (per buffer).
        - Procedure for reading data: pause buffer, enumerate events, unpause.
          (Events arriving while buffer is paused will be lost.)
        - Cannot be notified when data becomes available.
        */
        Circular,

        /*
        Buffers will be managed as realtime:

        - If buffer is full, new events will be lost.
        - Natural event enumeration order is oldest-to-newest (per buffer).
        - Procedure for reading data: enumerate events, marking the events as
          consumed to make room for new events.
        - Can use WaitForWakeup() or poll() to wait for data to become available.
        */
        RealTime,
    };

    /*
    Enablement status of a tracepoint that has been added to a session.
    */
    enum class TracepointEnableState : unsigned char
    {
        /*
        An error occurred while trying to enable/disable the tracepoint.
        Actual status is unknown.
        */
        Unknown,

        /*
        Tracepoint is enabled.
        */
        Enabled,

        /*
        Tracepoint is disabled.
        */
        Disabled,
    };

    /*
    Configuration settings for a tracepoint collection session.

    Required settings are specified as constructor parameters.
    Optional settings are set by calling methods.

    Example:

        TracepointCache cache;
        TracepointSession session(
            cache,
            TracepointSessionOptions(TracepointSessionMode::RealTime, 65536) // Required
                .WakeupWatermark(32768)                                      // Optional
                );
    */
    class TracepointSessionOptions
    {
        friend class TracepointSession;

    public:

        /*
        The flags that are set in the default value of the SampleType property:

        | PERF_SAMPLE_IDENTIFIER
        | PERF_SAMPLE_TID
        | PERF_SAMPLE_TIME
        | PERF_SAMPLE_CPU
        | PERF_SAMPLE_RAW
        */
        static constexpr auto SampleTypeDefault = 0x10486u;

        /*
        The flags that are supported for use with the SampleType property:

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
        | PERF_SAMPLE_RAW
        */
        static constexpr auto SampleTypeSupported = 0x107EFu;

        /*
        Initializes a TracepointSessionOptions to configure a session with the
        specified mode and buffer size.

        - mode: controls whether the buffer is managed as Circular or RealTime.

        - perCpuBufferSize: specifies the size of each buffer in bytes. This value will
          be rounded up to a power of 2 that is equal to or greater than the page size.
          Size may not exceed 2GB.
          Note that the session will allocate one buffer for each CPU.
        */
        constexpr
        TracepointSessionOptions(
            TracepointSessionMode mode,
            uint32_t perCpuBufferSize) noexcept
            : m_cpuBufferSizes(nullptr)
            , m_cpuBufferSizesCount(UINT32_MAX)
            , m_perCpuBufferSize(perCpuBufferSize)
            , m_mode(mode)
            , m_wakeupUseWatermark(true)
            , m_wakeupValue(0)
            , m_sampleType(SampleTypeDefault)
        {
            return;
        }

        /*
        Advanced scenarios: Initializes a TracepointSessionOptions to configure a
        session with the specified mode, using a specified buffer size for each CPU.

        - mode: controls whether the buffer is managed as Circular or RealTime.

        - cpuBufferSizes: Specifies the sizes for the buffers, in bytes. This must not be
          NULL. If a size is 0, no collection will be performed on the corresponding CPU.
          Other values will be rounded up to a power of 2 that is equal to or greater
          than the page size. Size may not exceed 2GB.

        - cpuBufferSizesCount: The number of values provided in cpuBufferSizes. This must
          be greater than 0. If this is less than the number of CPUs, no collection will
          be performed on the remaining CPUs.
        */
        constexpr
        TracepointSessionOptions(
            TracepointSessionMode mode,
            _In_reads_(cpuBufferSizesCount) uint32_t const* cpuBufferSizes,
            uint32_t cpuBufferSizesCount) noexcept
            : m_cpuBufferSizes(cpuBufferSizes)
            , m_cpuBufferSizesCount(cpuBufferSizesCount)
            , m_perCpuBufferSize(0)
            , m_mode(mode)
            , m_wakeupUseWatermark(true)
            , m_wakeupValue(0)
            , m_sampleType(SampleTypeDefault)
        {
            return;
        }

        /*
        Flags indicating what information should be recorded for each tracepoint.

        Flags use the perf_event_sample_format values defined in <linux/perf_event.h>
        or <tracepoint/PerfEventAbi.h>.

        The following flags are enabled by default (SampleTypeDefault):

        | PERF_SAMPLE_IDENTIFIER
        | PERF_SAMPLE_TID
        | PERF_SAMPLE_TIME
        | PERF_SAMPLE_CPU
        | PERF_SAMPLE_RAW

        The following flags are supported (SampleTypeSupported):

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
        | PERF_SAMPLE_RAW

        Note that you'll almost always want to include PERF_SAMPLE_RAW since that
        is the event's raw data (the event field values).
        */
        constexpr TracepointSessionOptions&
        SampleType(uint32_t sampleType) noexcept
        {
            m_sampleType = sampleType & SampleTypeSupported;
            return *this;
        }

        /*
        For realtime sessions only: sets the number of bytes of unconsumed event
        data (counting both SAMPLE and non-SAMPLE events) that a realtime buffer
        must contain to trigger wakeup (see WaitForWakeup).

        The default value is WakeupWatermark(0).

        Note that wakeup conditions are evaluated per-buffer. For example, if 3
        buffers each contain 32760 bytes of pending data, none of them would
        trigger a WakeupWatermark(32768) condition.
        */
        constexpr TracepointSessionOptions&
        WakeupWatermark(uint32_t wakeupWatermark) noexcept
        {
            m_wakeupUseWatermark = true;
            m_wakeupValue = wakeupWatermark;
            return *this;
        }

    private:

        uint32_t const* m_cpuBufferSizes;
        uint32_t const m_cpuBufferSizesCount;
        uint32_t const m_perCpuBufferSize;
        TracepointSessionMode const m_mode;
        bool m_wakeupUseWatermark;
        uint32_t m_wakeupValue;
        uint32_t m_sampleType;
    };

    /*
    Records a range of timestamps. If Last > First, the range is invalid.
    Default-initializes to an invalid range (First = UINT64_MAX, Last = 0).
    */
    struct TracepointTimestampRange
    {
        uint64_t First = UINT64_MAX;
        uint64_t Last = 0;
    };

    /*
    Configuration settings for TracepointSession::SavePerfDataFile.

    Example:

        error = session.SavePerfDataFile(
            "perf.data",
            TracepointSavePerfDataFileOptions().OpenMode(S_IRUSR | S_IWUSR));
    */
    class TracepointSavePerfDataFileOptions
    {
        friend class TracepointSession;

    public:

        /*
        Initializes a TracepointSavePerfDataFileOptions to use the default settings.

        - OpenMode = -1 (use default file permissions based on process umask).
        - TimestampFilter = 0..MAX_UINT64 (no timestamp filtering).
        - TimestampWrittenRange = nullptr (do not return timestamp range).
        */
        constexpr
        TracepointSavePerfDataFileOptions() noexcept
            : m_openMode(-1)
            , m_filterRange{ 0, UINT64_MAX }
            , m_timestampWrittenRange(nullptr)
        {
            return;
        }

        /*
        Sets the permissions mode to use when creating the perf.data file. The file will
        be created as: open(perfDataFileName, O_CREAT|O_WRONLY|O_TRUNC|O_CLOEXEC, OpenMode).

        Default value is -1 (use default file permissions based on process umask).
        This can be one or more of S_IRUSR, S_IWUSR, S_IRGRP, S_IWGRP, etc.
        */
        constexpr TracepointSavePerfDataFileOptions&
        OpenMode(int openMode) noexcept
        {
            m_openMode = openMode;
            return *this;
        }

        /*
        Sets the timestamp filter. Only sample events where
        timeMin <= event.timestamp <= timeMax will be written to the file. (Timestamp on
        non-sample events will be ignored.)

        Default value is 0..UINT64_MAX (no timestamp filter).

        For example, to write only events since the last Save:

        uint64_t lastTimestampWritten = 0;

        // First save does not filter-out any events based on timestamp,
        // and records the timestamp of the last event for use in next save:
        session.SavePerfDataFile("perf.data.0", TracepointSavePerfDataFileOptions()
            .TimestampFilter(lastTimestampWritten) // = 0, so no timestamp filter.
            .TimestampWrittenRange(nullptr, &lastTimestampWritten));

        ...

        // Subsequent saves use last event timestamp from previous save, and
        // update that timestamp for use in subsequent saves:
        session.SavePerfDataFile("perf.data.1", TracepointSavePerfDataFileOptions()
            .TimestampFilter(lastTimestampWritten) // filter out old events
            .TimestampWrittenRange(nullptr, &lastTimestampWritten));

        Note that in this pattern, the last event saved to file N will also be included
        in file N+1. If you want to avoid that, use
        TimestampFilter(lastTimestampWritten + 1), though that risks missing new events
        with timestamp exactly equal to lastTimestampWritten.
        */
        constexpr TracepointSavePerfDataFileOptions&
        TimestampFilter(uint64_t filterMin, uint64_t filterMax = UINT64_MAX) noexcept
        {
            m_filterRange = { filterMin, filterMax };
            return *this;
        }

        /*
        Sets the variable that will receive the timestamp range of the events that were
        written to the file.

        Default value is nullptr (do not return timestamp range).
        */
        constexpr TracepointSavePerfDataFileOptions&
        TimestampWrittenRange(_Out_opt_ TracepointTimestampRange* range = nullptr) noexcept
        {
            m_timestampWrittenRange = range;
            return *this;
        }

    private:

        int m_openMode;
        TracepointTimestampRange m_filterRange;
        TracepointTimestampRange* m_timestampWrittenRange;
    };

    /*
    Information about a tracepoint that has been added to a session.
    */
    class TracepointInfo
    {
        // Note: Implemented as a pimpl.
        // - I want the constructor to be private on the type that the user sees.
        // - Constructor on concrete type needs to be public so that it can be
        //   constructed by a container's emplace method.
        // - Therefore the concrete type needs to be a private type with a public
        //   constructor.

        friend class TracepointSession;

        ~TracepointInfo();
        TracepointInfo() noexcept;

    public:

        TracepointInfo(TracepointInfo const&) = delete;
        void operator=(TracepointInfo const&) = delete;

        tracepoint_decode::PerfEventMetadata const&
        Metadata() const noexcept;

        tracepoint_decode::PerfEventDesc const&
        EventDesc() const noexcept;

        TracepointEnableState
        EnableState() const noexcept;

        _Success_(return == 0) int
        GetEventCount(_Out_ uint64_t* value) const noexcept;
    };

    /*
    Manages a tracepoint collection session.

    Basic usage:

        TracepointCache cache; // May be shared by multiple sessions.
        TracepointSession session(
            cache,                           // The metadata cache to use for this session.
            TracepointSessionMode::RealTime, // Collection mode: RealTime or Circular.
            65536);                          // Size of each buffer (one buffer per CPU).

        error = session.EnableTracepoint(TracepointName("user_events", "MyFirstTracepoint"));
        if (error != 0) abort(); // TODO: handle error.

        error = session.EnableTracepoint(TracepointName("user_events:MySecondTracepoint"));
        if (error != 0) abort(); // TODO: handle error.

        for (;;)
        {
            // Wait until one or more of the buffers reaches 32768 bytes of event data.
            error = session.WaitForWakeup();
            if (error != 0) abort(); // TODO: handle error. (Don't get into a busy loop if waiting fails!)

            error = session.EnumerateSampleEventsUnordered(
                [](PerfSampleEventInfo const& event)
                {
                    // This code will run once for each SAMPLE event.
                    // It should record or process the event's data.
                    return 0; // If we return an error, enumeration will stop.
                });
            if (error != 0) abort(); // TODO: handle error.
        }
    */
    class TracepointSession
    {
        friend class TracepointInfo;

        struct ReadFormat; // Forward declaration

        class unique_fd
        {
            int m_fd;
        public:
            ~unique_fd();
            unique_fd() noexcept;
            explicit unique_fd(int fd) noexcept;
            unique_fd(unique_fd&&) noexcept;
            unique_fd& operator=(unique_fd&&) noexcept;
            explicit operator bool() const noexcept;
            void reset() noexcept;
            void reset(int fd) noexcept;
            int get() const noexcept;
        };

        class unique_mmap
        {
            void* m_addr;
            size_t m_size;
        public:
            ~unique_mmap();
            unique_mmap() noexcept;
            unique_mmap(void* addr, size_t size) noexcept;
            unique_mmap(unique_mmap&&) noexcept;
            unique_mmap& operator=(unique_mmap&&) noexcept;
            explicit operator bool() const noexcept;
            void reset() noexcept;
            void reset(void* addr, size_t size) noexcept;
            void* get() const noexcept;
            size_t get_size() const noexcept;
        };

        struct TracepointInfoImpl : TracepointInfo
        {
            tracepoint_decode::PerfEventDesc const m_eventDesc;
            std::unique_ptr<char unsigned[]> const m_eventDescStorage;
            std::unique_ptr<unique_fd[]> const m_bufferFiles; // size is BufferFilesCount
            unsigned const m_bufferFilesCount;
            TracepointEnableState m_enableState;

            TracepointInfoImpl(TracepointInfoImpl const&) = delete;
            void operator=(TracepointInfoImpl const&) = delete;
            ~TracepointInfoImpl();
            TracepointInfoImpl(
                tracepoint_decode::PerfEventDesc const& eventDesc,
                std::unique_ptr<char unsigned[]> eventDescStorage,
                std::unique_ptr<unique_fd[]> bufferFiles,
                unsigned bufferFilesCount) noexcept;

            // read(m_bufferFiles[i], data, sizeof(ReadFormat)).
            _Success_(return == 0) int
            Read(unsigned index, _Out_ ReadFormat* data) const noexcept;

            // Calls read() on each file, returns sum of the value fields.
            _Success_(return == 0) int
            GetEventCountImpl(_Out_ uint64_t* value) const noexcept;
        };

        struct BufferInfo
        {
            unique_mmap Mmap; // When non-empty: Mmap.get_size() = Size + PAGE_SIZE
            uint32_t Size; // Set whether or not Mmap is empty.
            uint8_t const* Data; // NULL if Mmap is empty, else Mmap.ptr + PAGE_SIZE.
            size_t DataPos;
            size_t DataTail;
            uint64_t DataHead64;

            BufferInfo(BufferInfo const&) = delete;
            void operator=(BufferInfo const&) = delete;
            ~BufferInfo();
            BufferInfo() noexcept;
        };

        struct TracepointBookmark
        {
            uint64_t Timestamp;
            uint16_t BufferIndex;
            uint16_t RecordSize;
            uint32_t RecordBufferPos;

            TracepointBookmark(
                uint64_t timestamp,
                uint16_t bufferIndex,
                uint16_t recordSize,
                uint32_t recordBufferPos) noexcept;
        };

        class UnorderedEnumerator
        {
            TracepointSession& m_session;
            uint32_t const m_bufferIndex;

        public:

            UnorderedEnumerator(UnorderedEnumerator const&) = delete;
            void operator=(UnorderedEnumerator const&) = delete;
            ~UnorderedEnumerator();

            UnorderedEnumerator(
                TracepointSession& session,
                uint32_t bufferIndex) noexcept;

            bool
            MoveNext() noexcept;
        };

        class OrderedEnumerator
        {
            TracepointSession& m_session;
            bool m_needsCleanup;
            size_t m_index;

        public:

            OrderedEnumerator(OrderedEnumerator const&) = delete;
            void operator=(OrderedEnumerator const&) = delete;
            ~OrderedEnumerator();

            explicit
            OrderedEnumerator(TracepointSession& session) noexcept;

            _Success_(return == 0) int
            LoadAndSort() noexcept;

            bool
            MoveNext() noexcept;
        };

    public:

        class TracepointInfoRange; // Forward declaration

        class TracepointInfoIterator
        {
            friend class TracepointSession;
            friend class TracepointInfoRange;
            using InnerItTy = std::unordered_map<unsigned, TracepointInfoImpl>::const_iterator;
            InnerItTy m_it;

            explicit
            TracepointInfoIterator(InnerItTy it) noexcept;

        public:

            using difference_type = std::ptrdiff_t;
            using value_type = TracepointInfo;
            using pointer = TracepointInfo const*;
            using reference = TracepointInfo const&;
            using iterator_category = std::forward_iterator_tag;

            TracepointInfoIterator() noexcept;
            TracepointInfoIterator& operator++() noexcept;
            TracepointInfoIterator operator++(int) noexcept;
            pointer operator->() const noexcept;
            reference operator*() const noexcept;
            bool operator==(TracepointInfoIterator other) const noexcept;
            bool operator!=(TracepointInfoIterator other) const noexcept;
        };

        class TracepointInfoRange
        {
            friend class TracepointSession;
            using RangeTy = std::unordered_map<unsigned, TracepointInfoImpl>;
            RangeTy const& m_range;

            explicit
            TracepointInfoRange(RangeTy const& range) noexcept;

        public:

            TracepointInfoIterator begin() const noexcept;
            TracepointInfoIterator end() const noexcept;
        };

        TracepointSession(TracepointSession const&) = delete;
        void operator=(TracepointSession const&) = delete;
        ~TracepointSession();

        /*
        Constructs a session using defaults for advanced options.
        May throw std::bad_alloc.

        - cache: The TracepointCache that this session will use to locate metadata
          (format) information about tracepoints. Multiple sessions may share a
          cache.

        - mode: controls whether the buffer is managed as Circular or RealTime.

        - perCpuBufferSize: specifies the size of each buffer in bytes. This value will
          be rounded up to a power of 2 that is equal to or greater than the page size.
          Size may not exceed 2GB.
          Note that the session will allocate one buffer for each CPU.

        Example:

            TracepointCache cache;
            TracepointSession session(
                cache,                           // The metadata cache to use for this session.
                TracepointSessionMode::RealTime, // Collection mode: RealTime or Circular.
                65536);                          // Size of each buffer (one buffer per CPU).
        */
        TracepointSession(
            TracepointCache& cache,
            TracepointSessionMode mode,
            uint32_t perCpuBufferSize) noexcept(false);

        /*
        Constructs a session using TracepointSessionOptions to set advanced options.
        May throw std::bad_alloc.

        - cache: The TracepointCache that this session will use to locate metadata
          (format) information about tracepoints. Multiple sessions may share a
          cache.

        - options: Configuration settings that this session will use.

        Example:

            TracepointCache cache;
            TracepointSession session(
                cache, // The metadata cache to use for this session.
                TracepointSessionOptions(TracepointSessionMode::RealTime, 65536) // Required settings
                    .SampleType(PERF_SAMPLE_TIME | PERF_SAMPLE_RAW)              // Optional setting
                    .WakeupWatermark(32768));                                    // Optional setting
        */
        TracepointSession(
            TracepointCache& cache,
            TracepointSessionOptions const& options) noexcept(false);

        /*
        Returns the tracepoint cache associated with this session.
        */
        TracepointCache&
        Cache() const noexcept;

        /*
        Returns the mode that was specified at construction.
        */
        TracepointSessionMode
        Mode() const noexcept;

        /*
        Returns session information, e.g. clockid and clock offset.
        At present, clockid is CLOCK_MONOTONIC_RAW and the timestamp offsets are
        captured when this TracepointSession object is constructed
        */
        tracepoint_decode::PerfEventSessionInfo const&
        SessionInfo() const noexcept;

        /*
        Returns true if Mode() == Realtime, false if Mode() == Circular.
        */
        bool
        IsRealtime() const noexcept;

        /*
        Returns the size (in bytes) of the specified buffer.
        Returns 0 if collection is disabled for the specified buffer.
        Requires: bufferIndex < BufferCount().
        */
        uint32_t
        BufferSize(unsigned bufferIndex = 0) const noexcept;

        /*
        Returns the number of buffers used for the session.
        Usually this is the number of CPUs.
        */
        uint32_t
        BufferCount() const noexcept;

        /*
        Returns the number of SAMPLE events that have been enumerated by this
        session.
        */
        uint64_t
        SampleEventCount() const noexcept;

        /*
        Returns the number of lost events that have been enumerated by this
        session. Events can be lost due to:

        - Memory allocation failure during buffer enumeration.
        - Event received while session is paused (circular mode only).
        - Event received while buffer is full (realtime mode only).
        */
        uint64_t
        LostEventCount() const noexcept;

        /*
        Returns the number of corrupt events that have been enumerated by this
        session. An event is detected as corrupt if the event's size is too
        small for the event's expected SampleType.
        */
        uint64_t
        CorruptEventCount() const noexcept;

        /*
        Returns the number of times buffer corruption has been detected by this
        session. The buffer is detected as corrupt if the buffer header has
        invalid values or if an event's size is invalid. Buffer corruption
        generally causes the buffer's remaining contents to be skipped.
        */
        uint64_t
        CorruptBufferCount() const noexcept;

        /*
        Clears the list of tracepoints we are listening to.
        Frees all buffers.
        */
        void
        Clear() noexcept;

        /*
        Disables collection of the specified tracepoint.

        Note that ID is from the event's common_type field and is not the PERF_SAMPLE_ID
        or PERF_SAMPLE_IDENTIFIER value.

        - Uses Cache().FindById(id) to look up the specified tracepoint.
        - If that succeeds and the specified tracepoint is in the list of session
          tracepoints, disables the tracepoint.

        Note that the tracepoint remains in the list of session tracepoints, but is set
        to the "disabled" state.

        Returns 0 for success, errno for error.

        Errors include but are not limited to:
        - ENOENT: tracefs metadata not found (tracepoint may not be registered yet)
          or tracepoint is not in the list of session tracepoints.
        - ENOTSUP: unable to find tracefs mount point.
        - EPERM: access denied to tracefs metadata.
        - ENODATA: unable to parse tracefs metadata.
        - ENOMEM: memory allocation failed.
        */
        _Success_(return == 0) int
        DisableTracepoint(unsigned id) noexcept;

        /*
        Disables collection of the specified tracepoint.

        - Uses Cache().FindOrAddFromSystem(name) to look up the specified tracepoint.
        - If that succeeds and the specified tracepoint is in the list of session
          tracepoints, disables the tracepoint.

        Note that the tracepoint remains in the list of session tracepoints, but is set
        to the "disabled" state.

        Returns 0 for success, errno for error.

        Errors include but are not limited to:
        - ENOENT: tracefs metadata not found (tracepoint may not be registered yet).
        - ENOTSUP: unable to find tracefs mount point.
        - EPERM: access denied to tracefs metadata.
        - ENODATA: unable to parse tracefs metadata.
        - ENOMEM: memory allocation failed.
        */
        _Success_(return == 0) int
        DisableTracepoint(TracepointName name) noexcept;

        /*
        Enables collection of the specified tracepoint.

        Note that ID is from the event's common_type field and is not the PERF_SAMPLE_ID
        or PERF_SAMPLE_IDENTIFIER value.

        - Uses Cache().FindById(name) to look up the specified tracepoint.
        - If that succeeds, enables the tracepoint (adding it to the list of session
          tracepoints if it is not already in the list).

        Returns 0 for success, errno for error.
        Errors include but are not limited to:
        - ENOENT: tracefs metadata not found (tracepoint may not be registered yet).
        - ENOTSUP: unable to find tracefs mount point.
        - EPERM: access denied to tracefs metadata.
        - ENODATA: unable to parse tracefs metadata.
        - ENOMEM: memory allocation failed.
        */
        _Success_(return == 0) int
        EnableTracepoint(unsigned id) noexcept;

        /*
        Enables collection of the specified tracepoint.

        - Uses Cache().FindOrAddFromSystem(name) to look up the specified tracepoint.
        - If that succeeds, enables the tracepoint (adding it to the list of session
          tracepoints if it is not already in the list).

        Returns 0 for success, errno for error.
        Errors include but are not limited to:
        - ENOENT: tracefs metadata not found (tracepoint may not be registered yet).
        - ENOTSUP: unable to find tracefs mount point.
        - EPERM: access denied to tracefs metadata.
        - ENODATA: unable to parse tracefs metadata.
        - ENOMEM: memory allocation failed.
        */
        _Success_(return == 0) int
        EnableTracepoint(TracepointName name) noexcept;

        /*
        Returns a range for enumerating the tracepoints in the session (includes
        both enabled and disabled tracepoints). Returned range is equivalent to
        TracepointInfoBegin()..TracepointInfoEnd().
        */
        TracepointInfoRange
        TracepointInfos() const noexcept;

        /*
        Returns the begin iterator of a range for enumerating the tracepoints in
        the session.
        */
        TracepointInfoIterator
        TracepointInfosBegin() const noexcept;

        /*
        Returns the end iterator of a range for enumerating the tracepoints in
        the session.
        */
        TracepointInfoIterator
        TracepointInfosEnd() const noexcept;

        /*
        Returns an iterator referencing a tracepoint in this session. Returns
        TracepointInfoEnd() if the specified tracepoint is not in this session.

        Note that ID is from the event's common_type field and is not the PERF_SAMPLE_ID
        or PERF_SAMPLE_IDENTIFIER value.
        */
        TracepointInfoIterator
        FindTracepointInfo(unsigned id) const noexcept;

        /*
        Returns an iterator referencing a tracepoint in this session. Returns
        TracepointInfoEnd() if the specified tracepoint is not in this session.
        */
        TracepointInfoIterator
        FindTracepointInfo(TracepointName name) const noexcept;

        /*
        For realtime sessions only: Waits for the wakeup condition using
        ppoll(bufferFiles, bufferCount, timeout, sigmask). The wakeup condition
        is configured by calling WakeupWatermark on a config before passing the
        config to the session's constructor.

        - timeout: Maximum time to wait. NULL means wait forever.
        - sigmask: Signal mask to apply before waiting. NULL means don't mask.
        - activeCount: On success, receives the number of buffers that meet the
          wakeup condition, or 0 if wait ended due to a timeout or a signal.

        Returns EPERM if the session is not realtime.

        Returns EPERM if the session is inactive. After construction and after
        Clear(), the session will be inactive until a tracepoint is added.

        Note that wakeup conditions are evaluated per-buffer. For example, if 3
        buffers each contain 32760 bytes of pending data, none of them would
        trigger a WakeupWatermark(32768) condition.
        */
        _Success_(return == 0) int
        WaitForWakeup(
            timespec const* timeout = nullptr,
            sigset_t const* sigmask = nullptr,
            _Out_opt_ int* pActiveCount = nullptr) noexcept;

        /*
        Advanced scenarios: Returns the file descriptors used for the buffers
        of the session. The returned file descriptors may be used for poll()
        but should not be read-from, written-to, closed, etc. This may be
        useful if you want to use a single thread to poll for events from
        multiple sessions or to poll for both events and some other condition.

        Returns EPERM if the session is inactive. After construction and after
        Clear(), the session will be inactive until a tracepoint is added.

        Most users should use WaitForWakeup() instead of GetBufferFiles().

        Note that if a buffer is disabled (buffer size was set to 0), the
        corresponding file descriptor will be -1.
        */
        _Success_(return == 0) int
        GetBufferFiles(
            _Out_writes_(BufferCount()) int* pBufferFiles) const noexcept;

        /*
        Creates a perf.data-format file and writes all pending data from the
        current session's buffers to the file. This can be done for all session
        types but is normally only used with circular sessions.

        This method does the following:

        - Open a new file.
        - Write a PERF_RECORD_FINISHED_INIT record to the file.
        - Flush buffers to the file, adding EventDesc records as needed.
        - Set system information headers as if by SetWriterHeaders().
        - Close the file.

        File is created as:

            open(perfDataFileName, O_CREAT|O_WRONLY|O_TRUNC|O_CLOEXEC, options.OpenMode());

        Returns: int error code (errno), or 0 for success.

        *** Circular session flush behavior ***

        For each buffer (usually one per CPU):

        - Pause collection into the buffer.
        - Write buffer's data to the file.
        - Unpause the buffer.

        Note that events are lost if they arrive while the buffer is paused. The lost
        event count indicates how many events were lost during previous pauses that would
        have been part of a enumeration if there had been no pauses. It does not include
        the count of events that were lost due to the current enumeration's pause (those
        will show up after a subsequent enumeration).

        *** Realtime session flush behavior ***

        For each buffer (usually one per CPU):

        - Write buffer's pending (unconsumed) events to the file.
        - Mark the enumerated events as consumed, making room for subsequent events.

        Note that SavePerfDataFile() is not normally used for realtime sessions because
        it only flushes the data in the buffers at the time you call SavePerfDataFile().
        Instead, you would normally want to use FlushToWriter() to flush the buffers
        multiple times over a long collection period.

        Note that events are lost if they arrive while the buffer is full. The lost
        event count indicates how many events were lost during previous periods when
        the buffer was full. It does not include the count of events that were lost
        due to the buffer being full at the start of the current enumeration (those will
        show up after a subsequent enumeration).
        */
        _Success_(return == 0) int
        SavePerfDataFile(
            _In_z_ char const* perfDataFileName,
            TracepointSavePerfDataFileOptions const& options = TracepointSavePerfDataFileOptions()) noexcept;

        /*
        Writes all pending data from the current session's buffers to the specified
        writer. Expands writtenRange to reflect the range of the timestamps seen.

        This can be done for all session types but is normally only used with realtime
        sessions.

        Typical usage:

        - Create a range to track timestamp: TracepointTimestampRange writtenRange{};
        - Create a writer: PerfDataFileWriter writer;
        - Call writer.Create(...) to open the file.
        - Write system-state events, if any (e.g. non-sample events like
          PERF_RECORD_MMAP, PERF_RECORD_COMM, PERF_RECORD_ID_INDEX,
          PERF_RECORD_THREAD_MAP, PERF_RECORD_CPU_MAP).
        - Call writer.WriteFinishedInit() to write a PERF_RECORD_FINISHED_INIT record.
        - Call FlushToWriter(writer, &writtenRange) to write the sample events as they
          arrive (e.g. each time session.WaitForWakeup() returns) and call
          writer.WriteFinishedRound() each time flush is complete.
        - Call SetWriterHeaders(writer, &writtenRange) to write system information headers.
        - Call writer.FinalizeAndClose() to close the file.

        Returns: int error code (errno), or 0 for success.

        *** Circular session behavior ***

        For each buffer (usually one per CPU):

        - Pause collection into the buffer.
        - Write buffer's data to the file.
        - Unpause the buffer.

        Note that events are lost if they arrive while the buffer is paused. The lost
        event count indicates how many events were lost during previous pauses that would
        have been part of a enumeration if there had been no pauses. It does not include
        the count of events that were lost due to the current enumeration's pause (those
        will show up after a subsequent enumeration).

        *** Realtime session behavior ***

        For each buffer (usually one per CPU):

        - Write buffer's pending (unconsumed) events to the file.
        - Mark the enumerated events as consumed, making room for subsequent events.

        Note that events are lost if they arrive while the buffer is full. The lost
        event count indicates how many events were lost during previous periods when
        the buffer was full. It does not include the count of events that were lost
        due to the buffer being full at the start of the current enumeration (those will
        show up after a subsequent enumeration).
        */
        _Success_(return == 0) int
        FlushToWriter(
            tracepoint_decode::PerfDataFileWriter& writer,
            _Inout_ TracepointTimestampRange* writtenRange,
            TracepointTimestampRange filterRange = { 0, UINT64_MAX }) noexcept;

        /*
        Sets the headers in the specified writer based on the session's configuration.
        At present, this sets the following headers:

        - SetUtsNameHeaders() from uname.
        - SetNrCpusHeader() from sysconf _SC_NPROCESSORS_CONF and _SC_NPROCESSORS_ONLN.
        - SetSessionInfoHeaders() using data from SessionInfo().
        - SetSampleTimeHeader() if writtenRange is non-null.
        */
        _Success_(return == 0) int
        SetWriterHeaders(
            tracepoint_decode::PerfDataFileWriter& writer,
            _In_opt_ TracepointTimestampRange const* writtenRange) noexcept;

        /*
        For each PERF_RECORD_SAMPLE record in the session's buffers, in timestamp
        order, invoke:

            int error = eventInfoCallback(eventInfo, args...);

        - eventInfoCallback: Callable object (e.g. a function pointer or a lambda)
          to invoke for each event.

          This callback should return an int (0 for success, errno for error). If
          eventInfoCallback returns a nonzero value then EnumerateEventsUnordered
          will immediately stop and return the specified error value.

          This callback should take a PerfSampleEventInfo const& as its first
          parameter.

          The args... (if any) are from the args... of the call to
          EnumerateEventsUnordered(eventInfoCallback, args...).

        - args...: optional additional parameters to be passed to eventInfoCallback.

        Returns: int error code (errno), or 0 for success.

        Requires:

        - The session's SampleType() must include PERF_SAMPLE_TIME so that the events
          can be sorted based on timestamp.

        Examples:

            // Use a callback function pointer and callback context:
            error = session.EnumerateSampleEvents(functionPointer, functionContext);

            // Use a lambda:
            error = session.EnumerateSampleEvents(
                [&](PerfSampleEventInfo const& event) -> int
                {
                    ...
                    return 0;
                });

        Events will be sorted based on timestamp before invoking the callback. If your
        callback does not need events to be sorted based on timestamp, use
        EnumerateSampleEventsUnordered to avoid the sorting overhead.

        Note that the eventInfo provided to eventInfoCallback will contain pointers
        into the trace buffers. The pointers will become invalid after eventInfoCallback
        returns. Any data that you need to use after that point must be copied.

        Note that this method does not throw any of its own exceptions, but it may
        exit via exception if your eventInfoCallback(...) throws an exception.

        *** Circular session behavior ***

        - Pause collection into all buffers.
        - Scan all buffers to find events.
        - Sort the events based on timestamp.
        - Invoke eventInfoCallback(...) for each event.
        - Unpause all buffers.

        Note that events are lost if they arrive while the buffer is paused. The lost
        event count indicates how many events were lost during previous pauses that would
        have been part of an enumeration if there had been no pauses. It does not include
        the count of events that were lost due to the current enumeration's pause (those
        will show up after a subsequent enumeration).

        *** Realtime session behavior ***

        - Scan all buffers to find events.
        - Sort the events based on timestamp.
        - Invoke eventInfoCallback for each event.
        - Mark the enumerated events as consumed, making room for subsequent events.

        Note that events are lost if they arrive while the buffer is full. The lost
        event count indicates how many events were lost during previous periods when
        the buffer was full. It does not include the count of events that were lost
        due to the buffer being full at the start of the current enumeration (those will
        show up after a subsequent enumeration).

        Note that if eventInfoCallback throws or returns a nonzero value, all events will
        be marked as consumed.
        */
        template<class EventInfoCallbackTy, class... ArgTys>
        _Success_(return == 0) int
        EnumerateSampleEvents(
            EventInfoCallbackTy&& eventInfoCallback, // int eventInfoCallback(PerfSampleEventInfo const&, args...)
            ArgTys&&... args // optional parameters to be passed to eventInfoCallback
        ) noexcept(noexcept(eventInfoCallback( // Throws exceptions if and only if eventInfoCallback throws.
            std::declval<tracepoint_decode::PerfSampleEventInfo const&>(),
            args...)))
        {
            int error = 0;

            if (m_bufferLeaderFiles != nullptr)
            {
                OrderedEnumerator enumerator(*this);
                error = enumerator.LoadAndSort();
                while (error == 0 && enumerator.MoveNext())
                {
                    error = eventInfoCallback(m_enumEventInfo, args...);
                }
            }

            return error;
        }

        /*
        For each PERF_RECORD_SAMPLE record in the session's buffers, in unspecified
        order, invoke:

            int error = eventInfoCallback(eventInfo, args...);

        - eventInfoCallback: Callable object (e.g. a function pointer or a lambda)
          to invoke for each event.

          This callback should return an int (0 for success, errno for error). If
          eventInfoCallback returns a nonzero value then EnumerateEventsUnordered
          will immediately stop and return the specified error value.

          This callback should take a PerfSampleEventInfo const& as its first
          parameter.

          The args... (if any) are from the args... of the call to
          EnumerateEventsUnordered(eventInfoCallback, args...).

        - args...: optional additional parameters to be passed to eventInfoCallback.

        Returns: int error code (errno), or 0 for success.

        Examples:

            // Use a callback function pointer and callback context:
            error = session.EnumerateSampleEventsUnordered(functionPointer, functionContext);

            // Use a lambda:
            error = session.EnumerateSampleEventsUnordered(
                [&](PerfSampleEventInfo const& event) -> int
                {
                    ...
                    return 0;
                });

        For efficiency, events will be provided in a natural enumeration order. This
        is usually not the same as event timestamp order, so you need to be able to
        accept the events out-of-order. If you need the events to be provided in
        timestamp order, use EnumerateSampleEvents.

        Note that the eventInfo provided to eventInfoCallback will contain pointers
        into the trace buffers. The pointers will become invalid after eventInfoCallback
        returns. Any data that you need to use after that point must be copied.

        Note that this method does not throw any of its own exceptions, but it may
        exit via exception if your eventInfoCallback(...) throws an exception.

        *** Circular session behavior ***

        For each buffer (usually one per CPU):

        - Pause collection into the buffer.
        - Invoke eventInfoCallback(...) for each of the buffer's events, newest-to-oldest.
        - Unpause the buffer.

        Note that events are lost if they arrive while the buffer is paused. The lost
        event count indicates how many events were lost during previous pauses that would
        have been part of an enumeration if there had been no pauses. It does not include
        the count of events that were lost due to the current enumeration's pause (those
        will show up after a subsequent enumeration).

        *** Realtime session behavior ***

        For each buffer (usually one per CPU):

        - Invoke eventInfoCallback(...) for each of the buffer's events, oldest-to-newest.
        - Mark the enumerated events as consumed, making room for subsequent events.

        Note that events are lost if they arrive while the buffer is full. The lost
        event count indicates how many events were lost during previous periods when
        the buffer was full. It does not include the count of events that were lost
        due to the buffer being full at the start of the current enumeration (those will
        show up after a subsequent enumeration).

        Note that if eventInfoCallback throws or returns a nonzero value, events will be
        marked consumed up to and including the event for which eventInfoCallback returned
        an error.
        */
        template<class EventInfoCallbackTy, class... ArgTys>
        _Success_(return == 0) int
        EnumerateSampleEventsUnordered(
            EventInfoCallbackTy&& eventInfoCallback, // int eventInfoCallback(PerfSampleEventInfo const&, args...)
            ArgTys&&... args // optional parameters to be passed to eventInfoCallback
        ) noexcept(noexcept(eventInfoCallback( // Throws exceptions if and only if eventInfoCallback throws.
            std::declval<tracepoint_decode::PerfSampleEventInfo const&>(),
            args...)))
        {
            int error = 0;

            if (m_bufferLeaderFiles != nullptr)
            {
                for (uint32_t bufferIndex = 0; bufferIndex != m_bufferCount; bufferIndex += 1)
                {
                    if (m_buffers[bufferIndex].Size == 0)
                    {
                        continue;
                    }

                    UnorderedEnumerator enumerator(*this, bufferIndex);
                    while (enumerator.MoveNext())
                    {
                        error = eventInfoCallback(m_enumEventInfo, args...);
                        if (error != 0)
                        {
                            break;
                        }
                    }

                    if (error != 0)
                    {
                        break;
                    }
                }
            }

            return error;
        }

    private:

        _Success_(return == 0) int
        DisableTracepointImpl(tracepoint_decode::PerfEventMetadata const& metadata) noexcept;

        _Success_(return == 0) int
        EnableTracepointImpl(tracepoint_decode::PerfEventMetadata const& metadata) noexcept;

        _Success_(return == 0) static int
        IoctlForEachFile(
            _In_reads_(filesCount) unique_fd const* files,
            unsigned filesCount,
            unsigned long request,
            _In_reads_opt_(filesCount) unique_fd const* values) noexcept;

        bool
        ParseSample(
            BufferInfo const& buffer,
            uint16_t recordSize,
            uint32_t recordBufferPos) noexcept;

        void
        EnumeratorEnd(uint32_t bufferIndex) const noexcept;

        void
        EnumeratorBegin(uint32_t bufferIndex) noexcept;

        template<class RecordFn>
        bool
        EnumeratorMoveNext(
            uint32_t bufferIndex,
            RecordFn&& recordFn) noexcept(noexcept(recordFn(std::declval<BufferInfo>(), 0u, 0u)));

        _Success_(return == 0) int
        SetTracepointEnableState(
            TracepointInfoImpl& tpi,
            bool enabled) noexcept;

        _Success_(return == 0) int
        AddTracepoint(
            tracepoint_decode::PerfEventMetadata const& metadata,
            TracepointEnableState enableState) noexcept(false);

        static uint32_t
        CalculateBufferCount(TracepointSessionOptions const& options) noexcept;

        static std::unique_ptr<BufferInfo[]>
        MakeBufferInfos(
            uint32_t bufferCount,
            uint32_t pageSize,
            TracepointSessionOptions const& options) noexcept(false);

    private:

        // Constant

        tracepoint_decode::PerfEventSessionInfo const m_sessionInfo;
        TracepointCache& m_cache;
        TracepointSessionMode const m_mode;
        bool const m_wakeupUseWatermark;
        uint32_t const m_wakeupValue;
        uint32_t const m_sampleType;
        uint32_t const m_bufferCount;
        uint32_t const m_pageSize;

        // State

        std::unique_ptr<BufferInfo[]> const m_buffers; // size is m_bufferCount
        std::unordered_map<unsigned, TracepointInfoImpl> m_tracepointInfoByCommonType;
        std::unordered_map<uint64_t, TracepointInfoImpl const*> m_tracepointInfoBySampleId;
        unique_fd const* m_bufferLeaderFiles; // == m_tracepointInfoByCommonType[N].BufferFiles.get() for some N, size is m_bufferCount

        // Statistics

        uint64_t m_sampleEventCount;
        uint64_t m_lostEventCount;
        uint64_t m_corruptEventCount;
        uint64_t m_corruptBufferCount;

        // Transient

        std::vector<uint8_t> m_eventDataBuffer; // Double-buffer for events that wrap.
        std::vector<TracepointBookmark> m_enumeratorBookmarks;
        std::unique_ptr<pollfd[]> m_pollfd;
        tracepoint_decode::PerfSampleEventInfo m_enumEventInfo;
    };

    using TracepointInfoRange = TracepointSession::TracepointInfoRange;
    using TracepointInfoIterator = TracepointSession::TracepointInfoIterator;
}
// namespace tracepoint_control

#endif // _included_TracepointSession_h
