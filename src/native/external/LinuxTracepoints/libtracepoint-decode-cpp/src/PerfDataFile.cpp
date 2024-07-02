// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

#include <tracepoint/PerfDataFile.h>
#include <tracepoint/PerfEventAbi.h>
#include <tracepoint/PerfEventMetadata.h>
#include <tracepoint/PerfEventInfo.h>

#include <assert.h>
#include <errno.h>
#include <stdlib.h>
#include <string.h>
#include <optional>

using namespace std::string_view_literals;
using namespace tracepoint_decode;

#ifdef _WIN32
#include <io.h>     // _setmode
#include <fcntl.h>  // _O_BINARY
#define FSEEK64(file, offset, origin)   _fseeki64(file, offset, origin)
#define FTELL64(file)                   _ftelli64(file)
#define FOPEN(path, mode)               _fsopen(path, mode, _SH_DENYWR)
#define bswap_64(n) _byteswap_uint64(n)
#else // _WIN32
#define FSEEK64(file, offset, origin)   fseeko64(file, offset, origin)
#define FTELL64(file)                   ftello64(file)
#define FOPEN(path, mode)               fopen(path, mode)
#include <byteswap.h>
#endif // _WIN32

#ifndef _Inout_
#define _Inout_
#endif
#ifndef _In_reads_bytes_
#define _In_reads_bytes_(cbAttr)
#endif

#define IF_EQUAL_GOTO_ERROR(index, count) \
    if (index == count) \
    { \
        error = EINVAL; \
        goto Error; \
    } \

// From: perf.data-file-format.txt
struct PerfDataFile::perf_file_section {
    uint64_t offset; // offset from start of file
    uint64_t size;   // size of the section

    // Reverse the endian order of all fields in this struct.
    void ByteSwap() noexcept
    {
        offset = bswap_64(offset);
        size = bswap_64(size);
    }
};

struct PerfDataFile::perf_pipe_header {
    static constexpr uint64_t Magic2 = 0x32454C4946524550; // "PERFILE2"

    uint64_t magic; // If correctly byte-swapped, this will be equal to Magic2.
    uint64_t size;  // size of the header

    // Return true if we should initialize the ByteReader as big-endian.
    bool CannotBeLittleEndian() const noexcept
    {
        // Little-endian file starts with "PERFILE2".
        // If it doesn't start with 'P' it can't be little-endian.
        return 'P' != *reinterpret_cast<char const*>(this);
    }

    // Reverse the endian order of all fields in this struct.
    void ByteSwap() noexcept
    {
        magic = bswap_64(magic);
        size = bswap_64(size);
    }
};

// From: perf.data-file-format.txt
struct PerfDataFile::perf_file_header {
    perf_pipe_header pipe_header;
    uint64_t attr_size;    // size of (perf_event_attrs + perf_file_section) in attrs.
    perf_file_section attrs;
    perf_file_section data;
    perf_file_section event_types; // Not used.

    // 256-bit bitmap based on HEADER_BITS
    uint64_t flags[4];

    // Reverse the endian order of all fields in this struct.
    void ByteSwap() noexcept
    {
        pipe_header.ByteSwap();
        attr_size = bswap_64(attr_size);
        attrs.ByteSwap();
        data.ByteSwap();
        event_types.ByteSwap();
        for (auto& flag : flags)
        {
            flag = bswap_64(flag);
        }
    }
};

template<class T, unsigned N>
static constexpr unsigned
ArrayCount(T(&)[N]) noexcept
{
    return N;
}

PerfDataFile::~PerfDataFile() noexcept
{
    if (m_file)
    {
        fclose(m_file);
    }
}

PerfDataFile::PerfDataFile() noexcept
    : m_filePos(0)
    , m_fileLen(0)
    , m_dataBeginFilePos(0)
    , m_dataEndFilePos(0)
    , m_file(0)
    , m_eventData()
    , m_headers()
    , m_eventDescList()
    , m_eventDescById()
    , m_sessionInfo()
    , m_byteReader()
    , m_sampleIdOffset(-1)
    , m_nonSampleIdOffset(-1)
    , m_commonTypeOffset(-1)
    , m_commonTypeSize(0)
    , m_parsedHeaderEventDesc(0)
    , m_parsedTracingData(0)
    , m_tracingDataLongSize(0)
    , m_tracingDataPageSize(0)
{
    return;
}

bool
PerfDataFile::FileBigEndian() const noexcept
{
    return m_byteReader.BigEndian();
}

PerfByteReader
PerfDataFile::ByteReader() const noexcept
{
    return m_byteReader;
}

uint64_t
PerfDataFile::FilePos() const noexcept
{
    return m_filePos;
}

uint64_t
PerfDataFile::DataBeginFilePos() const noexcept
{
    return m_dataBeginFilePos;
}

uint64_t
PerfDataFile::DataEndFilePos() const noexcept
{
    return m_dataEndFilePos;
}

uintptr_t
PerfDataFile::EventDescCount() const noexcept
{
    return m_eventDescList.size();
}

PerfEventDesc const&
PerfDataFile::EventDesc(uintptr_t eventDescIndex) const noexcept
{
    assert(eventDescIndex < m_eventDescList.size());
    return m_eventDescList[eventDescIndex];
}

_Ret_opt_ PerfEventDesc const*
PerfDataFile::FindEventDescById(uint64_t sampleId) const noexcept
{
    auto const it = m_eventDescById.find(sampleId);
    return it != m_eventDescById.end()
        ? &m_eventDescList[it->second]
        : nullptr;
}

std::string_view
PerfDataFile::Header(PerfHeaderIndex headerIndex) const noexcept
{
    return headerIndex < ArrayCount(m_headers)
        ? std::string_view(m_headers[headerIndex].data(), m_headers[headerIndex].size())
        : std::string_view();
}

uint8_t
PerfDataFile::TracingDataLongSize() const noexcept
{
    return m_tracingDataLongSize;
}

uint32_t
PerfDataFile::TracingDataPageSize() const noexcept
{
    return m_tracingDataPageSize;
}

std::string_view
PerfDataFile::TracingDataHeaderPage() const noexcept
{
    return m_headerPage;
}

std::string_view
PerfDataFile::TracingDataHeaderEvent() const noexcept
{
    return m_headerEvent;
}

std::string_view const*
PerfDataFile::TracingDataFtraces() const noexcept
{
    return m_ftraces.data();
}

uint32_t
PerfDataFile::TracingDataFtraceCount() const noexcept
{
    return static_cast<uint32_t>(m_ftraces.size());
}

std::string_view
PerfDataFile::TracingDataKallsyms() const noexcept
{
    return m_kallsyms;
}

std::string_view
PerfDataFile::TracingDataPrintk() const noexcept
{
    return m_printk;
}

std::string_view
PerfDataFile::TracingDataSavedCmdLine() const noexcept
{
    return m_cmdline;
}

void
PerfDataFile::Close() noexcept
{
    m_filePos = 0;
    m_fileLen = 0;
    m_dataBeginFilePos = 0;
    m_dataEndFilePos = 0;

    if (m_file != nullptr && m_file != stdin)
    {
        fclose(m_file);
    }

    m_file = nullptr;

    for (auto& header : m_headers)
    {
        header.clear();
    }

    m_eventDescList.clear();
    m_eventDescById.clear();
    m_byteReader = PerfByteReader();
    m_sampleIdOffset = -1;
    m_nonSampleIdOffset = -1;
    m_commonTypeOffset = -1;
    m_commonTypeSize = 0;
    m_parsedHeaderEventDesc = false;

    // HEADER_TRACING_DATA
    m_parsedTracingData = false;
    m_tracingDataLongSize = 0;
    m_tracingDataPageSize = 0;
    m_headerPage = {};
    m_headerEvent = {};
    m_ftraces.clear();
    m_metadataById.clear();
    m_kallsyms = {};
    m_printk = {};
    m_cmdline = {};
}

_Success_(return == 0) int
PerfDataFile::Open(_In_z_ char const* filePath) noexcept
{
    int error;
    perf_file_header header;

    Close();
    m_file = FOPEN(filePath, "rb");
    if (!m_file)
    {
        error = errno;
    }
    else if (0 != (error = FileRead(&header.pipe_header, sizeof(header.pipe_header))))
    {
        // error already set.
    }
    else
    {
        m_byteReader = PerfByteReader(header.pipe_header.CannotBeLittleEndian());
        auto const headerMagic = m_byteReader.ReadAsU64(&header.pipe_header.magic);
        auto const headerSize = m_byteReader.ReadAsU64(&header.pipe_header.size);
        if (headerMagic != header.pipe_header.Magic2)
        {
            error = EINVAL; // Bad magic
        }
        else if (!EnsureEventDataSize(0x10000))
        {
            error = ENOMEM;
        }
        else if (headerSize == sizeof(header.pipe_header))
        {
            // Pipe mode, no metadata section, no seeking needed.
            assert(m_filePos == sizeof(header.pipe_header));
            m_dataBeginFilePos = sizeof(header.pipe_header);
            m_dataEndFilePos = UINT64_MAX;
            error = 0;
        }
        else if (headerSize < sizeof(header))
        {
            error = EINVAL;
        }
        else if (
            0 != (error = UpdateFileLen()) ||
            0 != (error = FileSeekAndRead(
                sizeof(header.pipe_header),
                &header.pipe_header + 1,
                sizeof(header) - sizeof(header.pipe_header))))
        {
            // error already set.
        }
        else
        {
            if (m_byteReader.ByteSwapNeeded())
            {
                header.ByteSwap();
            }

            if (!SectionValid(header.attrs) ||
                !SectionValid(header.data) ||
                !SectionValid(header.event_types))
            {
                error = EINVAL;
            }
            else if (
                0 != (error = LoadAttrs(header.attrs, header.attr_size)) ||
                0 != (error = LoadHeaders(header.data, header.flags[0])) ||
                0 != (error = FileSeek(header.data.offset)))
            {
                // error already set.
            }
            else
            {
                assert(m_filePos == header.data.offset);
                m_dataBeginFilePos = header.data.offset;
                m_dataEndFilePos = header.data.offset + header.data.size;
                ParseHeaderClockid();
                ParseHeaderClockData();
                ParseHeaderEventDesc();
                ParseTracingData();
                error = 0;
            }
        }
    }

    return error;
}

_Success_(return == 0) int
PerfDataFile::OpenStdin() noexcept
{
    int error;
    perf_file_header header;

    Close();

#ifdef _WIN32

    if (_setmode(_fileno(stdin), _O_BINARY) < 0)
    {
        return errno;
    }

#endif

    m_file = stdin;
    if (0 != (error = FileRead(&header.pipe_header, sizeof(header.pipe_header))))
    {
        // error already set.
    }
    else
    {
        m_byteReader = PerfByteReader(header.pipe_header.CannotBeLittleEndian());
        auto const headerMagic = m_byteReader.ReadAsU64(&header.pipe_header.magic);
        auto const headerSize = m_byteReader.ReadAsU64(&header.pipe_header.size);
        if (headerMagic != header.pipe_header.Magic2)
        {
            error = EINVAL; // Bad magic
        }
        else if (!EnsureEventDataSize(0x10000))
        {
            error = ENOMEM;
        }
        else if (headerSize == sizeof(header.pipe_header))
        {
            // Pipe mode, no metadata section, no seeking needed.
            assert(m_filePos == sizeof(header.pipe_header));
            m_dataBeginFilePos = sizeof(header.pipe_header);
            m_dataEndFilePos = UINT64_MAX;
            error = 0;
        }
        else
        {
            // Don't try to seek on stdin.
            error = ENOTSUP;
        }
    }

    return error;
}

template<class SizeType>
_Success_(return == 0) int
PerfDataFile::ReadPostEventData(uint16_t eventSizeFromHeader) noexcept
{
    if (eventSizeFromHeader < sizeof(perf_event_header) + sizeof(SizeType))
    {
        return EINVAL;
    }

    auto const specialDataSize = m_byteReader.ReadAs<SizeType>(
        m_eventData.data() + sizeof(perf_event_header));
    if (specialDataSize > 0x80000000 || 0 != (specialDataSize & 7u))
    {
        return EINVAL;
    }
    else if (auto const specialDataSize32 = static_cast<uint32_t>(specialDataSize);
        !EnsureEventDataSize(eventSizeFromHeader + specialDataSize32))
    {
        return ENOMEM;
    }
    else if (specialDataSize32 > m_dataEndFilePos - m_filePos)
    {
        return EINVAL;
    }
    else
    {
        return FileRead(m_eventData.data() + eventSizeFromHeader, specialDataSize32);
    }
}

_Success_(return == 0) int
PerfDataFile::ReadEvent(_Outptr_result_maybenull_ perf_event_header const** ppEventHeader) noexcept
{
    int error;
    
    try
    {
        auto const eventStartFilePos = m_filePos;

        if (eventStartFilePos >= m_dataEndFilePos)
        {
            error = m_filePos == m_dataEndFilePos && m_filePos != UINT64_MAX
                ? 0 // normal-mode has reached EOF.
                : EPIPE; // Calling ReadEvent after error/EOF.
            goto ErrorOrEof;
        }

        assert(m_eventData.size() >= 0x10000); // Should be resized during Open().
        if (sizeof(perf_event_header) > m_dataEndFilePos - m_filePos)
        {
            error = EINVAL;
            goto ErrorOrEof;
        }

        if (0 != (error = FileRead(m_eventData.data(), sizeof(perf_event_header))))
        {
            if (error == EPIPE &&
                m_filePos == eventStartFilePos &&
                m_filePos != UINT64_MAX &&
                m_dataEndFilePos == UINT64_MAX)
            {
                error = 0; // pipe-mode has reached EOF.
            }
            goto ErrorOrEof;
        }

        if (m_byteReader.ByteSwapNeeded())
        {
            reinterpret_cast<perf_event_header*>(m_eventData.data())->ByteSwap();
        }

        auto const eventHeaderSize = reinterpret_cast<perf_event_header*>(m_eventData.data())->size;
        auto const eventHeaderType = reinterpret_cast<perf_event_header*>(m_eventData.data())->type;

        if (eventHeaderSize < sizeof(perf_event_header))
        {
            error = EINVAL;
            goto ErrorOrEof;
        }

        auto const cbEventData = static_cast<uint32_t>(eventHeaderSize - sizeof(perf_event_header));
        if (cbEventData > m_dataEndFilePos - m_filePos)
        {
            error = EINVAL;
            goto ErrorOrEof;
        }

        if (0 != (error = FileRead(m_eventData.data() + sizeof(perf_event_header), cbEventData)))
        {
            goto ErrorOrEof;
        }

        // Successfully read the basic event data.
        // Check for any special cases based on the type.
        switch (eventHeaderType)
        {
        case PERF_RECORD_HEADER_ATTR:
        {
            if (cbEventData >= PERF_ATTR_SIZE_VER0)
            {
                auto const pbEventData = m_eventData.data() + sizeof(perf_event_header);
                auto const attrSize = m_byteReader.Read(&reinterpret_cast<perf_event_attr const*>(pbEventData)->size);
                if (attrSize > cbEventData)
                {
                    error = EINVAL;
                    goto ErrorOrEof;
                }

                auto const cbAttrInEvent = static_cast<uint32_t>(attrSize);
                auto const cbAttrToCopy = std::min<uint32_t>(cbAttrInEvent, sizeof(perf_event_attr));

                auto pAttr = std::make_unique<perf_event_attr>();
                memcpy(pAttr.get(), pbEventData, cbAttrToCopy);
                error = AddAttr(
                    std::move(pAttr), cbAttrToCopy, "", pbEventData + cbAttrInEvent, cbEventData - cbAttrInEvent);
                if (error)
                {
                    goto ErrorOrEof;
                }
            }
            break;
        }
        case PERF_RECORD_HEADER_TRACING_DATA:
        {
            // Note: ReadPostEventData may cause m_eventData to reallocate.
            if (0 != (error = ReadPostEventData<uint32_t>(eventHeaderSize)))
            {
                goto ErrorOrEof;
            }
            else if (!m_parsedTracingData)
            {
                auto const pbEventData = m_eventData.data() + sizeof(perf_event_header);
                auto const len = m_byteReader.ReadAsU32(pbEventData);

                // ReadPostEventData ensures this.
                assert(sizeof(perf_event_header) + sizeof(uint32_t) + len <= m_eventData.size());

                auto& header = m_headers[PERF_HEADER_TRACING_DATA];
                header.resize(len);
                memcpy(header.data(), pbEventData + sizeof(uint32_t), len);
                ParseTracingData();
            }
            break;
        }
        case PERF_RECORD_HEADER_BUILD_ID:
        {
            auto const pbEventData = m_eventData.data() + sizeof(perf_event_header);
            auto& header = m_headers[PERF_HEADER_BUILD_ID];
            header.resize(cbEventData);
            memcpy(header.data(), pbEventData, cbEventData);
            break;
        }
        case PERF_RECORD_AUXTRACE:
        {
            // Note: ReadPostEventData may cause m_eventData to reallocate.
            if (0 != (error = ReadPostEventData<uint64_t>(eventHeaderSize)))
            {
                goto ErrorOrEof;
            }
            break;
        }
        case PERF_RECORD_HEADER_FEATURE:
        {
            if (cbEventData >= sizeof(uint64_t))
            {
                auto const pbEventData = m_eventData.data() + sizeof(perf_event_header);
                auto const bit = m_byteReader.ReadAsU64(pbEventData);
                if (bit < ArrayCount(m_headers))
                {
                    auto& header = m_headers[static_cast<size_t>(bit)];
                    header.resize(cbEventData - sizeof(uint64_t));
                    memcpy(
                        header.data(),
                        pbEventData + sizeof(uint64_t),
                        cbEventData - sizeof(uint64_t));

                    switch (bit)
                    {
                    case PERF_HEADER_CLOCKID:
                        ParseHeaderClockid();
                        break;
                    case PERF_HEADER_CLOCK_DATA:
                        ParseHeaderClockData();
                        break;
                    }
                }
            }
            break;
        }
        case PERF_RECORD_FINISHED_INIT:
        {
            ParseHeaderEventDesc();
            break;
        }
        default:
            break;
        }

        if (m_filePos > m_dataEndFilePos)
        {
            error = EINVAL;
            goto ErrorOrEof;
        }

        *ppEventHeader = reinterpret_cast<perf_event_header*>(m_eventData.data());
        return 0;
    }
    catch (std::bad_alloc const&)
    {
        error = ENOMEM;
    }

ErrorOrEof:

    m_filePos = UINT64_MAX; // Subsequent ReadEvent should get EPIPE.
    *ppEventHeader = nullptr;
    return error;
}

uint32_t
PerfDataFile::EventDataSize(perf_event_header const* pEventHeader) noexcept
{
    uint32_t size;

    // A few event types have data beyond what the header defines.
    switch (pEventHeader->type)
    {
    default:
        assert(pEventHeader->size >= sizeof(perf_event_header)); // ReadEvent would have failed otherwise.
        size = pEventHeader->size;
        break;

    case PERF_RECORD_HEADER_TRACING_DATA:
        assert(pEventHeader->size >= sizeof(perf_event_header) + sizeof(uint64_t));
        size = static_cast<uint32_t>(pEventHeader->size + m_byteReader.ReadAs<uint64_t>(pEventHeader + 1));
        break;

    case PERF_RECORD_AUXTRACE:
        assert(pEventHeader->size >= sizeof(perf_event_header) + sizeof(uint32_t));
        size = static_cast<uint32_t>(pEventHeader->size + m_byteReader.ReadAs<uint32_t>(pEventHeader + 1));
        break;
    }

    return size;
}

_Success_(return == 0) int
PerfDataFile::GetSampleEventInfo(
    _In_ perf_event_header const* pEventHeader,
    _Out_ PerfSampleEventInfo* pInfo) const noexcept
{
    auto const SupportedSampleTypes = 0
        | PERF_SAMPLE_IDENTIFIER
        | PERF_SAMPLE_IP
        | PERF_SAMPLE_TID
        | PERF_SAMPLE_TIME
        | PERF_SAMPLE_ADDR
        | PERF_SAMPLE_ID
        | PERF_SAMPLE_STREAM_ID
        | PERF_SAMPLE_CPU
        | PERF_SAMPLE_PERIOD
        | PERF_SAMPLE_READ
        | PERF_SAMPLE_CALLCHAIN
        | PERF_SAMPLE_RAW;

    int error;
    uint64_t id;

    error = GetSampleEventId(pEventHeader, &id);
    if (!error)
    {
        auto eventDescIt = m_eventDescById.find(id);
        if (eventDescIt == m_eventDescById.end())
        {
            error = ENOENT;
            goto Error;
        }

        auto const& eventDesc = m_eventDescList[eventDescIt->second];
        auto const infoSampleTypes = eventDesc.attr->sample_type & SupportedSampleTypes;
        char const* infoRawData = nullptr;
        uint32_t infoRawDataSize = 0;

        auto const pArray = reinterpret_cast<uint64_t const*>(pEventHeader);
        auto const cArray = pEventHeader->size / sizeof(uint64_t);
        assert(cArray >= 2); // Otherwise GetSampleEventId would have failed.
        size_t iArray = 1;

        if (infoSampleTypes & PERF_SAMPLE_IDENTIFIER)
        {
            iArray += 1;
        }

        if (infoSampleTypes & PERF_SAMPLE_IP)
        {
            IF_EQUAL_GOTO_ERROR(iArray, cArray);
            pInfo->ip = m_byteReader.Read(&pArray[iArray]);
            iArray += 1;
        }

        if (infoSampleTypes & PERF_SAMPLE_TID)
        {
            IF_EQUAL_GOTO_ERROR(iArray, cArray);
            auto const* p32 = reinterpret_cast<uint32_t const*>(&pArray[iArray]);
            pInfo->pid = m_byteReader.Read(&p32[0]);
            pInfo->tid = m_byteReader.Read(&p32[1]);
            iArray += 1;
        }

        if (infoSampleTypes & PERF_SAMPLE_TIME)
        {
            IF_EQUAL_GOTO_ERROR(iArray, cArray);
            pInfo->time = m_byteReader.Read(&pArray[iArray]);
            iArray += 1;
        }

        if (infoSampleTypes & PERF_SAMPLE_ADDR)
        {
            IF_EQUAL_GOTO_ERROR(iArray, cArray);
            pInfo->addr = m_byteReader.Read(&pArray[iArray]);
            iArray += 1;
        }

        if (infoSampleTypes & PERF_SAMPLE_ID)
        {
            IF_EQUAL_GOTO_ERROR(iArray, cArray);
            iArray += 1;
        }

        if (infoSampleTypes & PERF_SAMPLE_STREAM_ID)
        {
            IF_EQUAL_GOTO_ERROR(iArray, cArray);
            pInfo->stream_id = m_byteReader.Read(&pArray[iArray]);
            iArray += 1;
        }

        if (infoSampleTypes & PERF_SAMPLE_CPU)
        {
            IF_EQUAL_GOTO_ERROR(iArray, cArray);
            auto const* p32 = reinterpret_cast<uint32_t const*>(&pArray[iArray]);
            pInfo->cpu = m_byteReader.Read(&p32[0]);
            pInfo->cpu_reserved = m_byteReader.Read(&p32[1]);
            iArray += 1;
        }

        if (infoSampleTypes & PERF_SAMPLE_PERIOD)
        {
            IF_EQUAL_GOTO_ERROR(iArray, cArray);
            pInfo->period = m_byteReader.Read(&pArray[iArray]);
            iArray += 1;
        }

        if (infoSampleTypes & PERF_SAMPLE_READ)
        {
            IF_EQUAL_GOTO_ERROR(iArray, cArray);
            pInfo->read_values = &pArray[iArray];

            auto const SupportedReadFormats = 0
                | PERF_FORMAT_TOTAL_TIME_ENABLED
                | PERF_FORMAT_TOTAL_TIME_RUNNING
                | PERF_FORMAT_ID
                | PERF_FORMAT_GROUP
                | PERF_FORMAT_LOST;
            auto const attrReadFormat = eventDesc.attr->read_format;
            if (attrReadFormat & ~SupportedReadFormats)
            {
                error = ENOTSUP;
                goto Error;
            }
            else if (attrReadFormat & PERF_FORMAT_GROUP)
            {
                IF_EQUAL_GOTO_ERROR(iArray, cArray);
                auto const cValues = m_byteReader.Read(&pArray[iArray]);

                auto const cStaticItems = 1u // cValues
                    + (0 != (attrReadFormat & PERF_FORMAT_TOTAL_TIME_ENABLED))
                    + (0 != (attrReadFormat & PERF_FORMAT_TOTAL_TIME_RUNNING));
                auto const cPerValue = 1u // value
                    + (0 != (attrReadFormat & PERF_FORMAT_ID))
                    + (0 != (attrReadFormat & PERF_FORMAT_LOST));
                auto const cItems = cStaticItems + static_cast<uint32_t>(cValues) * cPerValue;
                if (cValues >= 0x10000 / sizeof(uint64_t) ||
                    cArray - iArray < cItems)
                {
                    error = EINVAL;
                    goto Error;
                }

                iArray += cItems;
            }
            else
            {
                auto const cItems = 1u // value
                    + (0 != (attrReadFormat & PERF_FORMAT_TOTAL_TIME_ENABLED))
                    + (0 != (attrReadFormat & PERF_FORMAT_TOTAL_TIME_RUNNING))
                    + (0 != (attrReadFormat & PERF_FORMAT_ID))
                    + (0 != (attrReadFormat & PERF_FORMAT_LOST));
                if (cArray - iArray < cItems)
                {
                    error = EINVAL;
                    goto Error;
                }

                iArray += cItems;
            }
        }

        if (infoSampleTypes & PERF_SAMPLE_CALLCHAIN)
        {
            IF_EQUAL_GOTO_ERROR(iArray, cArray);
            auto const infoCallchain = &pArray[iArray];
            pInfo->callchain = infoCallchain;
            auto const count = m_byteReader.Read(infoCallchain);
            iArray += 1;

            if (cArray - iArray < count)
            {
                error = EINVAL;
                goto Error;
            }
            iArray += static_cast<size_t>(count);
        }

        if (infoSampleTypes & PERF_SAMPLE_RAW)
        {
            IF_EQUAL_GOTO_ERROR(iArray, cArray);
            auto const* p32 = reinterpret_cast<uint32_t const*>(&pArray[iArray]);
            infoRawDataSize = m_byteReader.Read(&p32[0]);
            infoRawData = reinterpret_cast<char const*>(p32 + 1);
            if ((cArray - iArray) * sizeof(uint64_t) - sizeof(uint32_t) < infoRawDataSize)
            {
                error = EINVAL;
                goto Error;
            }

            iArray += (sizeof(uint32_t) + infoRawDataSize + sizeof(uint64_t) - 1) / sizeof(uint64_t);
        }

        assert(iArray <= cArray);
        pInfo->event_desc = &eventDesc;
        pInfo->session_info = &m_sessionInfo;
        pInfo->header = pEventHeader;
        pInfo->id = id;
        pInfo->raw_data = infoRawData;
        pInfo->raw_data_size = infoRawDataSize;
        return 0;
    }

Error:

    pInfo->event_desc = {};
    pInfo->session_info = {};
    pInfo->header = {};
    pInfo->id = {};
    pInfo->raw_data = {};
    pInfo->raw_data_size = {};
    return error;
}

_Success_(return == 0) int
PerfDataFile::GetNonSampleEventInfo(
    _In_ perf_event_header const* pEventHeader,
    _Out_ PerfNonSampleEventInfo* pInfo) const noexcept
{
    auto const SupportedSampleTypes = 0
        | PERF_SAMPLE_TID
        | PERF_SAMPLE_TIME
        | PERF_SAMPLE_ID
        | PERF_SAMPLE_STREAM_ID
        | PERF_SAMPLE_CPU
        | PERF_SAMPLE_IDENTIFIER;

    int error;
    uint64_t id;

    error = GetNonSampleEventId(pEventHeader, &id);
    if (!error)
    {
        auto eventDescIt = m_eventDescById.find(id);
        if (eventDescIt == m_eventDescById.end())
        {
            error = ENOENT;
            goto Error;
        }

        auto const& eventDesc = m_eventDescList[eventDescIt->second];
        auto const infoSampleTypes = eventDesc.attr->sample_type & SupportedSampleTypes;

        auto const pArray = reinterpret_cast<uint64_t const*>(pEventHeader);
        auto const cArray = pEventHeader->size / sizeof(uint64_t);
        assert(cArray >= 2); // Otherwise GetNonSampleEventId would have failed.
        auto iArray = cArray;

        if (infoSampleTypes & PERF_SAMPLE_IDENTIFIER)
        {
            iArray -= 1;
        }

        if (infoSampleTypes & PERF_SAMPLE_CPU)
        {
            iArray -= 1;
            IF_EQUAL_GOTO_ERROR(iArray, 0);
            auto const* p32 = reinterpret_cast<uint32_t const*>(&pArray[iArray]);
            pInfo->cpu = m_byteReader.Read(&p32[0]);
            pInfo->cpu_reserved = m_byteReader.Read(&p32[1]);
        }

        if (infoSampleTypes & PERF_SAMPLE_STREAM_ID)
        {
            iArray -= 1;
            IF_EQUAL_GOTO_ERROR(iArray, 0);
            pInfo->stream_id = m_byteReader.Read(&pArray[iArray]);
        }

        if (infoSampleTypes & PERF_SAMPLE_ID)
        {
            iArray -= 1;
            IF_EQUAL_GOTO_ERROR(iArray, 0);
        }

        if (infoSampleTypes & PERF_SAMPLE_TIME)
        {
            iArray -= 1;
            IF_EQUAL_GOTO_ERROR(iArray, 0);
            pInfo->time = m_byteReader.Read(&pArray[iArray]);
        }

        if (infoSampleTypes & PERF_SAMPLE_TID)
        {
            iArray -= 1;
            IF_EQUAL_GOTO_ERROR(iArray, 0);
            auto const* p32 = reinterpret_cast<uint32_t const*>(&pArray[iArray]);
            pInfo->pid = m_byteReader.Read(&p32[0]);
            pInfo->tid = m_byteReader.Read(&p32[1]);
        }

        assert(iArray > 0);
        assert(iArray < 0x10000 / sizeof(uint64_t));
        pInfo->event_desc = &eventDesc;
        pInfo->session_info = &m_sessionInfo;
        pInfo->header = pEventHeader;
        pInfo->id = id;
        return 0;
    }

Error:

    pInfo->event_desc = {};
    pInfo->session_info = {};
    pInfo->header = {};
    pInfo->id = {};
    return error;
}

_Success_(return == 0) int
PerfDataFile::LoadAttrs(perf_file_section const& attrs, uint64_t cbAttrAndIdSection64) noexcept
{
    int error;

    if (attrs.size > 0x80000000 ||
        cbAttrAndIdSection64 < PERF_ATTR_SIZE_VER0 + sizeof(perf_file_section) ||
        cbAttrAndIdSection64 > 0x10000)
    {
        error = EINVAL;
    }
    else try
    {
        error = 0;

        auto const cbAttrAndIdSection = static_cast<uint32_t>(cbAttrAndIdSection64);
        uint32_t const cbAttrInFile = cbAttrAndIdSection - sizeof(perf_file_section);
        auto const cbAttrToCopy = std::min<uint32_t>(cbAttrInFile, sizeof(perf_event_attr));

        m_eventDescList.reserve(m_eventDescList.size() + static_cast<uint32_t>(attrs.size) / cbAttrAndIdSection);

        auto const attrFilePosEnd = attrs.offset + attrs.size;
        for (auto attrFilePos = attrs.offset; attrFilePos < attrFilePosEnd;)
        {
            auto pAttr = std::make_unique<perf_event_attr>();
            error = FileSeekAndRead(attrFilePos, pAttr.get(), cbAttrToCopy);
            attrFilePos += cbAttrInFile;
            if (error)
            {
                break;
            }

            perf_file_section section;
            error = FileSeekAndRead(attrFilePos, &section, sizeof(section));
            attrFilePos += sizeof(section);
            if (error)
            {
                break;
            }

            if (!SectionValid(section) ||
                0 != (section.size & 7) ||
                section.size > 0x80000000)
            {
                error = EINVAL;
                break;
            }

            auto const sectionSize = static_cast<uint32_t>(section.size);

            if (!EnsureEventDataSize(sectionSize))
            {
                error = ENOMEM;
                break;
            }

            error = FileSeekAndRead(section.offset, m_eventData.data(), sectionSize);
            if (error)
            {
                goto Done;
            }

            error = AddAttr(std::move(pAttr), cbAttrToCopy, "", m_eventData.data(), sectionSize);
            if (error)
            {
                goto Done;
            }
        }
    }
    catch (std::bad_alloc const&)
    {
        error = ENOMEM;
    }

Done:

    return error;
}

_Success_(return == 0) int
PerfDataFile::LoadHeaders(perf_file_section const& data, uint64_t flags) noexcept
{
    int error;

    try
    {
        error = 0;

        auto filePos = data.offset + data.size;
        uint64_t mask = 1;
        for (auto& header : m_headers)
        {
            if (flags & mask)
            {
                perf_file_section section;
                error = FileSeekAndRead(filePos, &section, sizeof(section));
                filePos += sizeof(section);
                if (error)
                {
                    break;
                }

                if (!SectionValid(section) ||
                    section.size > 0x80000000)
                {
                    error = EINVAL;
                    break;
                }

                header.resize(static_cast<size_t>(section.size));
                error = FileSeekAndRead(section.offset, header.data(), header.size());
                if (error)
                {
                    break;
                }
            }

            mask <<= 1;
        }
    }
    catch (std::bad_alloc const&)
    {
        error = ENOMEM;
    }

    return error;
}

// Returns pEnd - p.
static constexpr size_t
Remaining(char const* p, char const* pEnd) noexcept
{
    assert(p <= pEnd);
    return static_cast<size_t>(pEnd - p);
}

// Expects NUL-terminated string.
// Sets sv = string value, not including NUL.
// On success, returns pointer to after the NUL.
// On failure, returns nullptr.
static char const*
ReadSz(char const* p, char const* pEnd, _Out_ std::string_view* sv) noexcept
{
    auto const max = Remaining(p, pEnd);
    auto const len = strnlen(p, max);
    if (len == max)
    {
        *sv = {};
        return nullptr;
    }
    else
    {
        *sv = { p, len };
        return p + len + 1;
    }
}

// Expects SizeType size + char value[size].
// Sets sv = value.
// On success, returns pointer to after the value.
// On failure, returns nullptr.
template<class SizeType>
static char const*
ReadSection(
    PerfByteReader byteReader,
    char const* p,
    char const* pEnd,
    _Out_ std::string_view* sv) noexcept
{
    if (Remaining(p, pEnd) < sizeof(SizeType))
    {
        *sv = {};
        return nullptr;
    }

    SizeType fullSize = byteReader.ReadAs<SizeType>(p);
    p += sizeof(SizeType);

    if (Remaining(p, pEnd) < fullSize)
    {
        *sv = {};
        return nullptr;
    }

    auto const size = static_cast<size_t>(fullSize);
    *sv = { p, size };
    return p + size;
}

// Expects expectedName + NUL + SizeType size + char value[size].
// On success, sets sv = value and returns pointer to after value.
// If expectedName not present, sets sv = {} and returns p.
// On error, returns nullptr.
template<class SizeType>
static char const*
ReadNamedSection(
    PerfByteReader byteReader,
    char const* p,
    char const* pEnd,
    std::string_view expectedName,
    _Out_ std::string_view* sv) noexcept
{
    auto const cchExpectedName = expectedName.size() + 1;
    if (Remaining(p, pEnd) < cchExpectedName ||
        0 != memcmp(p, expectedName.data(), cchExpectedName))
    {
        *sv = {};
        return p;
    }

    return ReadSection<SizeType>(byteReader, p + cchExpectedName, pEnd, sv);
}

void
PerfDataFile::ParseTracingData() noexcept
{
    try
    {
        auto const& tracingData = m_headers[PERF_HEADER_TRACING_DATA];
        auto constexpr TracingSignature = "\x17\x08\x44tracing"sv;
        if (tracingData.size() >= 10 &&
            0 == memcmp(tracingData.data(), TracingSignature.data(), TracingSignature.size()))
        {
            m_parsedTracingData = true;

            std::string_view version;

            auto p = tracingData.data();
            auto const pEnd = p + tracingData.size();
            p += TracingSignature.size();

            // Version

            p = ReadSz(p, pEnd, &version);
            if (!p)
            {
                return; // Unexpected.
            }

            auto const tracingDataVersion = strtod(version.data(), nullptr);

            // Big Endian, LongSize, PageSize

            if (Remaining(p, pEnd) < 1 + 1 + sizeof(uint32_t))
            {
                return; // Unexpected.
            }

            PerfByteReader byteReader(*p != 0);
            p += 1;

            m_tracingDataLongSize = *p;
            p += 1;

            m_tracingDataPageSize = byteReader.ReadAsU32(p);
            p += sizeof(uint32_t);

            // header_page

            p = ReadNamedSection<uint64_t>(byteReader, p, pEnd, "header_page"sv, &m_headerPage);
            if (!p)
            {
                return; // Unexpected.
            }

            // header_event (not really used anymore)

            p = ReadNamedSection<uint64_t>(byteReader, p, pEnd, "header_event"sv, &m_headerEvent);
            if (!p)
            {
                return; // Unexpected.
            }

            // ftraces

            if (Remaining(p, pEnd) < sizeof(uint32_t))
            {
                return; // Unexpected.
            }

            auto const ftraceCount = byteReader.ReadAsU32(p);
            p += sizeof(uint32_t);
            m_ftraces.reserve(ftraceCount);
            for (uint32_t ftraceIndex = 0; ftraceIndex != ftraceCount; ftraceIndex += 1)
            {
                std::string_view ftrace;
                p = ReadSection<uint64_t>(byteReader, p, pEnd, &ftrace);
                if (!p)
                {
                    return; // Unexpected.
                }

                m_ftraces.push_back(ftrace);
            }

            // systems (and events)

            if (Remaining(p, pEnd) < sizeof(uint32_t))
            {
                return; // Unexpected.
            }

            auto const systemCount = byteReader.ReadAsU32(p);
            p += sizeof(uint32_t);
            for (uint32_t systemIndex = 0; systemIndex != systemCount; systemIndex += 1)
            {
                std::string_view systemName;
                p = ReadSz(p, pEnd, &systemName);
                if (!p)
                {
                    return; // Unexpected.
                }

                if (Remaining(p, pEnd) < sizeof(uint32_t))
                {
                    return; // Unexpected.
                }

                auto const eventCount = byteReader.ReadAsU32(p);
                p += sizeof(uint32_t);
                for (uint32_t eventIndex = 0; eventIndex != eventCount; eventIndex += 1)
                {
                    std::string_view formatFileContents;
                    p = ReadSection<uint64_t>(byteReader, p, pEnd, &formatFileContents);
                    if (!p)
                    {
                        return; // Unexpected.
                    }

                    PerfEventMetadata eventMetadata;
                    auto longSize64 = m_tracingDataLongSize == 0
                        ? sizeof(uintptr_t) == 8
                        : m_tracingDataLongSize == 8;
                    if (eventMetadata.Parse(longSize64, systemName, formatFileContents))
                    {
                        int8_t commonTypeOffset = -1;
                        uint8_t commonTypeSize = 0;
                        for (uint32_t i = 0; i != eventMetadata.CommonFieldCount(); i += 1)
                        {
                            auto const& field = eventMetadata.Fields()[i];
                            if (field.Name() == "common_type"sv)
                            {
                                if (field.Offset() < 128 &&
                                    (field.Size() == 1 || field.Size() == 2 || field.Size() == 4) &&
                                    field.Array() == PerfFieldArrayNone)
                                {
                                    commonTypeOffset = static_cast<int8_t>(field.Offset());
                                    commonTypeSize = static_cast<uint8_t>(field.Size());
                                }
                                break;
                            }
                        }

                        if (commonTypeOffset == -1)
                        {
                            // Unexpected: did not find a usable "common_type" field.
                            continue;
                        }
                        else if (m_commonTypeOffset == -1)
                        {
                            // First event to be parsed. Use its "common_type" field.
                            m_commonTypeOffset = commonTypeOffset;
                            m_commonTypeSize = commonTypeSize;
                        }
                        else if (
                            m_commonTypeOffset != commonTypeOffset ||
                            m_commonTypeSize != commonTypeSize)
                        {
                            // Unexpected: found a different "common_type" field.
                            continue;
                        }

                        m_metadataById.try_emplace(eventMetadata.Id(), std::move(eventMetadata));
                    }
                }
            }

            // Update EventDesc with the new metadata.
            for (auto& desc : m_eventDescList)
            {
                if (desc.metadata == nullptr && desc.attr->type == PERF_TYPE_TRACEPOINT)
                {
                    auto it = m_metadataById.find(static_cast<uint32_t>(desc.attr->config));
                    if (it != m_metadataById.end())
                    {
                        desc.metadata = &it->second;
                    }
                }
            }

            // kallsyms

            p = ReadSection<uint32_t>(byteReader, p, pEnd, &m_kallsyms);
            if (!p)
            {
                return; // Unexpected.
            }

            // printk

            p = ReadSection<uint32_t>(byteReader, p, pEnd, &m_printk);
            if (!p)
            {
                return; // Unexpected.
            }

            // saved_cmdline

            if (tracingDataVersion >= 0.6)
            {
                p = ReadSection<uint64_t>(byteReader, p, pEnd, &m_cmdline);
                if (!p)
                {
                    return; // Unexpected.
                }
            }
        }
    }
    catch (std::bad_alloc const&)
    {
        return;
    }
}

void
PerfDataFile::ParseHeaderClockid() noexcept
{
    struct Clockid
    {
        uint64_t clockid;
    };

    auto const& data = m_headers[PERF_HEADER_CLOCKID];
    if (data.size() >= sizeof(Clockid))
    {
        auto const pClockid = reinterpret_cast<Clockid const*>(data.data());
        m_sessionInfo.SetClockid(static_cast<uint32_t>(m_byteReader.Read(&pClockid->clockid)));
    }
}

void
PerfDataFile::ParseHeaderClockData() noexcept
{
    struct ClockData
    {
        uint32_t version;
        uint32_t clockid;
        uint64_t wall_clock_ns;
        uint64_t clockid_time_ns;
    };

    auto const& data = m_headers[PERF_HEADER_CLOCK_DATA];
    if (data.size() >= sizeof(ClockData))
    {
        auto const pClockData = reinterpret_cast<ClockData const*>(data.data());
        if (1 <= m_byteReader.Read(&pClockData->version))
        {
            m_sessionInfo.SetClockData(
                m_byteReader.Read(&pClockData->clockid),
                m_byteReader.Read(&pClockData->wall_clock_ns),
                m_byteReader.Read(&pClockData->clockid_time_ns));
        }
    }
}

void
PerfDataFile::ParseHeaderEventDesc() noexcept
{
    if (m_parsedHeaderEventDesc)
    {
        return;
    }

    try
    {
        auto const& eventDesc = m_headers[PERF_HEADER_EVENT_DESC];
        if (eventDesc.size() >= sizeof(uint32_t) + sizeof(uint32_t))
        {
            m_parsedHeaderEventDesc = true;

            auto p = eventDesc.data();
            auto const pEnd = p + eventDesc.size();
            auto const cEvents = m_byteReader.ReadAsU32(p);
            p += sizeof(uint32_t);
            auto const cbAttrInHeader = m_byteReader.ReadAsU32(p);
            p += sizeof(uint32_t);
            if (cbAttrInHeader < PERF_ATTR_SIZE_VER0 || cbAttrInHeader > 0x10000)
            {
                return; // Unexpected.
            }

            for (uint32_t iEvent = 0; iEvent != cEvents; iEvent += 1)
            {
                if (Remaining(p, pEnd) < cbAttrInHeader + sizeof(uint32_t) + sizeof(uint32_t))
                {
                    return; // Unexpected.
                }

                auto const pAttrInHeader = reinterpret_cast<perf_event_attr const*>(p);
                p += cbAttrInHeader;
                auto const cIds = m_byteReader.ReadAsU32(p);
                p += sizeof(uint32_t);
                auto const cbString = m_byteReader.ReadAsU32(p);
                p += sizeof(uint32_t);

                if (m_byteReader.Read(&pAttrInHeader->size) != cbAttrInHeader ||
                    cIds > 0x10000 ||
                    cbString > 0x10000 ||
                    Remaining(p, pEnd) < cbString + cIds * sizeof(uint64_t) ||
                    strnlen(p, cbString) >= cbString)
                {
                    return; // Unexpected.
                }

                auto const pString = p;
                p += cbString;

                auto const cbAttrToCopy = std::min<uint32_t>(cbAttrInHeader, sizeof(perf_event_attr));

                auto pAttr = std::make_unique<perf_event_attr>();
                memcpy(pAttr.get(), pAttrInHeader, cbAttrToCopy);

                (void)AddAttr(std::move(pAttr), cbAttrToCopy, pString, p, cIds * sizeof(uint64_t));

                p += cIds * sizeof(uint64_t);
                assert(p <= pEnd);
            }
        }
    }
    catch (std::bad_alloc const&)
    {
        return;
    }
}

_Success_(return == 0) int
PerfDataFile::GetSampleEventId(_In_ perf_event_header const* pEventHeader, _Out_ uint64_t* pId) const noexcept
{
    int error;
    uint64_t id;

    if (m_sampleIdOffset < 0)
    {
        id = 0;
        error = ENODATA;
    }
    else if (pEventHeader->size < sizeof(perf_event_header) + sizeof(uint64_t) + m_sampleIdOffset * sizeof(uint64_t))
    {
        id = 0;
        error = EINVAL;
    }
    else
    {
        auto const pArray = reinterpret_cast<uint64_t const*>(pEventHeader + 1);
        id = m_byteReader.Read(&pArray[m_sampleIdOffset]);
        error = 0;
    }

    *pId = id;
    return error;
}

_Success_(return == 0) int
PerfDataFile::GetNonSampleEventId(_In_ perf_event_header const* pEventHeader, _Out_ uint64_t *pId) const noexcept
{
    int error;
    uint64_t id;

    if (m_nonSampleIdOffset < 0)
    {
        id = 0;
        error = ENODATA;
    }
    else if (pEventHeader->size < sizeof(perf_event_header) + sizeof(uint64_t) + m_nonSampleIdOffset * sizeof(uint64_t))
    {
        id = 0;
        error = EINVAL;
    }
    else
    {
        auto const pArrayLast = reinterpret_cast<uint64_t const*>(pEventHeader) - 1 +
            (pEventHeader->size / sizeof(uint64_t));
        id = m_byteReader.Read(&pArrayLast[-m_nonSampleIdOffset]);
        error = 0;
    }

    *pId = id;
    return error;
}

_Success_(return == 0) int
PerfDataFile::AddAttr(
    std::unique_ptr<perf_event_attr> pAttr,
    uint32_t cbAttrCopied,
    _In_z_ char const* pName,
    _In_reads_bytes_(cbIdsFileEndian) void const* pbIdsFileEndian,
    uintptr_t cbIdsFileEndian) noexcept(false)
{
    assert(cbAttrCopied <= sizeof(perf_event_attr));

    if (m_byteReader.ByteSwapNeeded())
    {
        pAttr->ByteSwap();
    }

    auto const sampleType = pAttr->sample_type;

    int8_t sampleIdOffset;
    int8_t nonSampleIdOffset;
    if (0 != (sampleType & PERF_SAMPLE_IDENTIFIER))
    {
        // ID is at a fixed offset.
        sampleIdOffset = 0;
        nonSampleIdOffset = 0;
    }
    else if (0 == (sampleType & PERF_SAMPLE_ID))
    {
        // ID is not available.
        sampleIdOffset = -2;
        nonSampleIdOffset = -2;
    }
    else
    {
        // ID offset not fixed.
        sampleIdOffset = 0 +
            (0 != (sampleType & PERF_SAMPLE_IP)) +
            (0 != (sampleType & PERF_SAMPLE_TID)) +
            (0 != (sampleType & PERF_SAMPLE_TIME)) +
            (0 != (sampleType & PERF_SAMPLE_ADDR));
        nonSampleIdOffset = 0 +
            (0 != (sampleType & PERF_SAMPLE_CPU)) +
            (0 != (sampleType & PERF_SAMPLE_STREAM_ID));
    }

    if (!pAttr->sample_id_all)
    {
        // ID is not available for non-sample events.
        nonSampleIdOffset = -2;
    }

    if (sampleIdOffset != m_sampleIdOffset)
    {
        if (m_sampleIdOffset != -1)
        {
            // Unexpected: Inconsistent sampleIdOffset across the attrs in the trace.
            return EINVAL;
        }

        m_sampleIdOffset = sampleIdOffset;
    }

    if (nonSampleIdOffset != m_nonSampleIdOffset)
    {
        if (m_nonSampleIdOffset != -1)
        {
            // Unexpected: Inconsistent nonSampleIdOffset across the attrs in the trace.
            return EINVAL;
        }

        m_nonSampleIdOffset = nonSampleIdOffset;
    }

    auto const cIds = static_cast<uint32_t>(cbIdsFileEndian / sizeof(uint64_t));
    auto const pIdsFileEndian = static_cast<uint64_t const*>(pbIdsFileEndian);
    auto pIds = std::make_unique<uint64_t[]>(cIds);
    for (uint32_t i = 0; i != cIds; i += 1)
    {
        pIds[i] = m_byteReader.Read(&pIdsFileEndian[i]);
    }

    pAttr->size = static_cast<perf_event_attr_size>(cbAttrCopied);
    auto const eventDescIndex = m_eventDescList.size();
    auto const pEventListIds = pIds.get(); // To access ids after we move-from pIds.

    PerfEventMetadata const* pMetadata = nullptr;
    if (pAttr->type == PERF_TYPE_TRACEPOINT)
    {
        auto itMetadata = m_metadataById.find(static_cast<uint32_t>(pAttr->config));
        if (itMetadata != m_metadataById.end())
        {
            pMetadata = &itMetadata->second;
        }
    }

    m_eventDescList.push_back({
        { pAttr.get(), pName, pMetadata, pIds.get(), cIds},
        std::move(pAttr),
        std::move(pIds)
    });

    for (uint32_t i = 0; i != cIds; i += 1)
    {
        m_eventDescById[pEventListIds[i]] = eventDescIndex;
    }

    return 0;
}

bool
PerfDataFile::EnsureEventDataSize(uint32_t minSize) noexcept
{
    if (m_eventData.size() < minSize)
    {
        try
        {
            m_eventData.reserve(minSize);
            m_eventData.resize(m_eventData.capacity());
        }
        catch (std::bad_alloc const&)
        {
            return false;
        }
    }

    return true;
}

_Success_(return == 0) int
PerfDataFile::UpdateFileLen() noexcept
{
    int error;

    if (FSEEK64(m_file, 0, SEEK_END))
    {
        error = errno; 
    }
    else if (auto const fileLen = FTELL64(m_file);
        fileLen < 0)
    {
        error = errno;
    }
    else
    {
        m_filePos = fileLen;
        m_fileLen = fileLen;
        error = 0;
    }

    return error;
}

bool
PerfDataFile::SectionValid(perf_file_section const& section) const noexcept
{
    auto const endOffset = section.offset + section.size;
    return endOffset >= section.offset && endOffset <= m_fileLen;
}

_Success_(return == 0) int
PerfDataFile::FileRead(_Out_writes_bytes_all_(cb) void* p, uintptr_t cb) noexcept
{
    auto pLeft = static_cast<uint8_t*>(p);
    auto cLeft = cb;

    for (;;)
    {
        auto const cRead = fread(pLeft, 1, cLeft, m_file);
        m_filePos += cRead;

        if (cRead == cLeft)
        {
            return 0;
        }
        else if (ferror(m_file))
        {
            return EIO;
        }
        else if (cRead == 0)
        {
            return EPIPE;
        }

        pLeft += cRead;
        cLeft -= cRead;
    }
}

_Success_(return == 0) int
PerfDataFile::FileSeek(uint64_t filePos) noexcept
{
    int error;

    if (filePos == m_filePos)
    {
        error = 0;
    }
    else if (FSEEK64(m_file, filePos, SEEK_SET))
    {
        error = errno;
    }
    else
    {
        m_filePos = filePos;
        error = 0;
    }

    return error;
}

_Success_(return == 0) int
PerfDataFile::FileSeekAndRead(
    uint64_t filePos,
    _Out_writes_bytes_all_(cb) void* p,
    uintptr_t cb) noexcept
{
    int error = FileSeek(filePos);
    return error ? error : FileRead(p, cb);
}
