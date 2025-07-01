// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

/*
PerfDataFile class - Reads perf.data files.
*/

#pragma once
#ifndef _included_PerfDataFile_h
#define _included_PerfDataFile_h

#include "PerfByteReader.h"
#include "PerfDataFileDefs.h"
#include "PerfEventSessionInfo.h"
#include <stdint.h>
#include <stdio.h> // FILE
#include <map>
#include <memory>
#include <string_view>
#include <vector>

#ifdef _WIN32
#include <sal.h>
#endif
#ifndef _In_
#define _In_
#endif
#ifndef _In_z_
#define _In_z_
#endif
#ifndef _In_reads_bytes_
#define _In_reads_bytes_(cb)
#endif
#ifndef _Out_
#define _Out_
#endif
#ifndef _Outptr_result_maybenull_
#define _Outptr_result_maybenull_
#endif
#ifndef _Out_writes_bytes_all_
#define _Out_writes_bytes_all_(size)
#endif
#ifndef _Success_
#define _Success_(condition)
#endif
#ifndef _Ret_opt_
#define _Ret_opt_
#endif

namespace tracepoint_decode
{
    // Forward declarations from PerfEventInfo.h:
    struct PerfSampleEventInfo;
    struct PerfNonSampleEventInfo;

    // Forward declaration from PerfEventMetadata.h:
    class PerfEventMetadata;

    /*
    PerfDataFile class - Reads perf.data files.
    */
    class PerfDataFile
    {
        struct perf_file_section;
        struct perf_pipe_header;
        struct perf_file_header;

        struct EventDesc : PerfEventDesc
        {
            std::unique_ptr<perf_event_attr> attrStorage;
            std::unique_ptr<uint64_t[]> idsStorage;
        };

        uint64_t m_filePos;
        uint64_t m_fileLen;
        uint64_t m_dataBeginFilePos;
        uint64_t m_dataEndFilePos;
        FILE* m_file;
        std::vector<uint8_t> m_eventData;
        std::vector<char> m_headers[PERF_HEADER_LAST_FEATURE]; // Stored file-endian.
        std::vector<EventDesc> m_eventDescList; // Stored host-endian. Name points into m_headers.
        std::map<uint64_t, size_t> m_eventDescById; // Index into m_eventDescList.
        PerfEventSessionInfo m_sessionInfo;
        PerfByteReader m_byteReader;
        int8_t m_sampleIdOffset; // -1 = unset, -2 = no id.
        int8_t m_nonSampleIdOffset; // -1 = unset, -2 = no id.
        int8_t m_commonTypeOffset; // -1 = unset, -2 = not available.
        uint8_t m_commonTypeSize;
        bool m_parsedHeaderEventDesc;

        // HEADER_TRACING_DATA
        bool m_parsedTracingData;
        uint8_t m_tracingDataLongSize;
        uint32_t m_tracingDataPageSize;
        std::string_view m_headerPage; // Points into m_headers.
        std::string_view m_headerEvent; // Points into m_headers.
        std::vector<std::string_view> m_ftraces; // Points into m_headers.
        std::map<uint32_t, PerfEventMetadata> m_metadataById; // Points into m_headers.
        std::string_view m_kallsyms; // Points into m_headers.
        std::string_view m_printk; // Points into m_headers.
        std::string_view m_cmdline; // Points into m_headers.

    public:

        PerfDataFile(PerfDataFile const&) = delete;
        void operator=(PerfDataFile const&) = delete;
        ~PerfDataFile() noexcept;
        PerfDataFile() noexcept;

        // Returns true if the currently-opened file is big-endian.
        bool
        FileBigEndian() const noexcept;

        // Returns PerfByteReader(FileBigEndian()).
        PerfByteReader
        ByteReader() const noexcept;

        // Returns the position within the input file of the event that will be
        // read by the next call to ReadEvent().
        // Returns UINT64_MAX after end-of-file or file error.
        uint64_t
        FilePos() const noexcept;

        // Returns the position within the input file of the first event.
        uint64_t
        DataBeginFilePos() const noexcept;

        // If the input file was recorded in pipe mode, returns UINT64_MAX.
        // Otherwise, returns the position within the input file immediately after
        // the last event.
        uint64_t
        DataEndFilePos() const noexcept;

        // Returns the number of attribute records available from EventDesc().
        uintptr_t
        EventDescCount() const noexcept;

        // Combined data from perf_file_header::attrs and PERF_RECORD_HEADER_ATTR.
        // Requires: eventDescIndex < EventDescCount().
        PerfEventDesc const&
        EventDesc(uintptr_t eventDescIndex) const noexcept;

        // Combined data from perf_file_header::attrs, PERF_RECORD_HEADER_ATTR,
        // and HEADER_EVENT_DESC. Returns NULL if sampleId is not known.
        _Ret_opt_ PerfEventDesc const*
        FindEventDescById(uint64_t sampleId) const noexcept;

        // Returns the raw data from the specified header (file-endian, use ByteReader()
        // to do byte-swapping as appropriate).
        // Returns empty if the requested header was not loaded from the file.
        std::string_view
        Header(PerfHeaderIndex headerIndex) const noexcept;

        // Returns the LongSize parsed from a PERF_HEADER_TRACING_DATA header,
        // or 0 if no PERF_HEADER_TRACING_DATA has been parsed.
        uint8_t
        TracingDataLongSize() const noexcept;

        // Returns the PageSize parsed from a PERF_HEADER_TRACING_DATA header,
        // or 0 if no PERF_HEADER_TRACING_DATA has been parsed.
        uint32_t
        TracingDataPageSize() const noexcept;

        // Returns the header_page parsed from a PERF_HEADER_TRACING_DATA header,
        // or {} if no PERF_HEADER_TRACING_DATA has been parsed.
        std::string_view
        TracingDataHeaderPage() const noexcept;

        // Returns the header_event parsed from a PERF_HEADER_TRACING_DATA header,
        // or {} if no PERF_HEADER_TRACING_DATA has been parsed.
        std::string_view
        TracingDataHeaderEvent() const noexcept;

        // Returns the ftraces parsed from a PERF_HEADER_TRACING_DATA header,
        // or NULL if no PERF_HEADER_TRACING_DATA has been parsed.
        std::string_view const*
        TracingDataFtraces() const noexcept;

        // Returns the count of ftraces parsed from a PERF_HEADER_TRACING_DATA header,
        // or 0 if no PERF_HEADER_TRACING_DATA has been parsed.
        uint32_t
        TracingDataFtraceCount() const noexcept;

        // Returns the kallsyms parsed from a PERF_HEADER_TRACING_DATA header,
        // or {} if no PERF_HEADER_TRACING_DATA has been parsed.
        std::string_view
        TracingDataKallsyms() const noexcept;

        // Returns the printk parsed from a PERF_HEADER_TRACING_DATA header,
        // or {} if no PERF_HEADER_TRACING_DATA has been parsed.
        std::string_view
        TracingDataPrintk() const noexcept;

        // Returns the saved_cmdline parsed from a PERF_HEADER_TRACING_DATA header,
        // or {} if no PERF_HEADER_TRACING_DATA has been parsed.
        std::string_view
        TracingDataSavedCmdLine() const noexcept;

        // Closes the input file, if any.
        void
        Close() noexcept;

        // Closes the current input file (if any), then opens the specified
        // perf.data file using fopen and reads the file header.
        // If not a pipe-mode file, loads metadata. If a pipe-mode file, metadata
        // will be loaded as the metadata events are encountered by ReadEvent.
        // On successful return, the file will be positioned before the first event.
        _Success_(return == 0) int
        Open(_In_z_ char const* filePath) noexcept;

        // Closes the current input file (if any), then switches stdin to binary
        // mode (Windows-only), then reads the file header from stdin. If stdin is
        // not a pipe-mode file, returns an error. Metadata will be loaded as the
        // metadata events are encountered by ReadEvent.
        // On successful return, the file will be positioned before the first event.
        _Success_(return == 0) int
        OpenStdin() noexcept;

        // Returns the event header (host-endian) followed by the raw data from the
        // file (file-endian, use ByteReader() to do byte-swapping as appropriate).
        //
        // On success, sets *ppEventHeader to the event and returns 0.
        // The returned pointer is valid until the next call to ReadEvent.
        // 
        // On end-of-file, sets *ppEventHeader to NULL and returns 0.
        // 
        // On error, sets *ppEventHeader to NULL and returns errno.
        //
        // Note that for PERF_RECORD_HEADER_TRACING_DATA and PERF_RECORD_AUXTRACE,
        // there will be extra data immediately after the event. Use EventDataSize to
        // get the actual event size.
        _Success_(return == 0) int
        ReadEvent(_Outptr_result_maybenull_ perf_event_header const** ppEventHeader) noexcept;

        // Given a pEventHeader that was returned from ReadEvent, returns the actual
        // size of the specified event.
        //
        // For most event types, this returns pEventHeader->size.
        //
        // For PERF_RECORD_HEADER_TRACING_DATA and PERF_RECORD_AUXTRACE, there is
        // extra data after the event, and this will return a size that includes
        // that extra data.
        uint32_t
        EventDataSize(perf_event_header const* pEventHeader) noexcept;

        // Tries to get event information from the event's prefix. The prefix is
        // usually present only for sample events. If the event prefix is not
        // present, this function may return an error or it may succeed but return
        // incorrect information. In general, only use this on events where
        // pEventHeader->type == PERF_RECORD_SAMPLE.
        _Success_(return == 0) int
        GetSampleEventInfo(
            _In_ perf_event_header const* pEventHeader,
            _Out_ PerfSampleEventInfo* pInfo) const noexcept;

        // Tries to get event information from the event's suffix. The event suffix
        // is usually present only for non-sample kernel-generated events.
        // If the event suffix is not present, this function may return an error or
        // it may succeed but return incorrect information. In general:
        // - Only use this on events where pEventHeader->type != PERF_RECORD_SAMPLE
        //   and pEventHeader->type < PERF_RECORD_USER_TYPE_START.
        // - Only use this on events that come after the PERF_RECORD_FINISHED_INIT
        //   event.
        _Success_(return == 0) int
        GetNonSampleEventInfo(
            _In_ perf_event_header const* pEventHeader,
            _Out_ PerfNonSampleEventInfo* pInfo) const noexcept;

    private:

        _Success_(return == 0) int
        LoadAttrs(perf_file_section const& attrs, uint64_t cbAttrAndIdSection64) noexcept;

        _Success_(return == 0) int
        LoadHeaders(perf_file_section const& data, uint64_t flags) noexcept;

        void
        ParseTracingData() noexcept;

        void
        ParseHeaderClockid() noexcept;

        void
        ParseHeaderClockData() noexcept;

        void
        ParseHeaderEventDesc() noexcept;

        _Success_(return == 0) int
        GetSampleEventId(_In_ perf_event_header const* pEventHeader, _Out_ uint64_t* pId) const noexcept;

        _Success_(return == 0) int
        GetNonSampleEventId(_In_ perf_event_header const* pEventHeader, _Out_ uint64_t* pId) const noexcept;

        _Success_(return == 0) int
        AddAttr(
            std::unique_ptr<perf_event_attr> pAttr,
            uint32_t cbAttrCopied,
            _In_z_ char const* pName,
            _In_reads_bytes_(cbIdsFileEndian) void const* pbIdsFileEndian,
            uintptr_t cbIdsFileEndian) noexcept(false);

        template<class SizeType>
        _Success_(return == 0) int
        ReadPostEventData(uint16_t eventSizeFromHeader) noexcept;

        bool
        EnsureEventDataSize(uint32_t minSize) noexcept;

        // Note: leaves filePos at EOF.
        _Success_(return == 0) int
        UpdateFileLen() noexcept;

        bool
        SectionValid(perf_file_section const& section) const noexcept;

        // Returns 0 (success), EIO (fread error), or EPIPE (eof).
        _Success_(return == 0) int
        FileRead(_Out_writes_bytes_all_(cb) void* p, uintptr_t cb) noexcept;

        _Success_(return == 0) int
        FileSeek(uint64_t filePos) noexcept;

        // Returns 0 (success), EIO (fread error), EPIPE (eof), or others.
        _Success_(return == 0) int
        FileSeekAndRead(uint64_t filePos, _Out_writes_bytes_all_(cb) void* p, uintptr_t cb) noexcept;
    };
}
// namespace tracepoint_decode

#endif // _included_PerfDataFile_h
