// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

#include <tracepoint/PerfDataFileWriter.h>
#include <tracepoint/PerfEventAbi.h>
#include <tracepoint/PerfEventMetadata.h>
#include <tracepoint/PerfEventSessionInfo.h>

#include <assert.h>
#include <errno.h>
#include <stdlib.h>
#include <string.h>
#include <fcntl.h>  // _O_BINARY

#ifdef _WIN32
#include <io.h>
static bool constexpr HostIsBigEndian = false;
#define CLOSE(file)                     _close(file)
#define LSEEK64(file, offset, origin)   _lseeki64(file, offset, origin)
#define WRITE(file, data, size)         _write(file, data, size)
#define WRITE_SIZE(size)                static_cast<unsigned>(std::min<size_t>(0x80000000, size))
#define GETPAGESIZE()                   4096 // PAGE_SIZE
#define O_CLOEXEC                       0
#else // _WIN32
#include <unistd.h>
#include <sys/utsname.h>
#include <sys/uio.h>
#include <endian.h>
static bool constexpr HostIsBigEndian = __BYTE_ORDER == __BIG_ENDIAN;
#define CLOSE(file)                     close(file)
#define LSEEK64(file, offset, origin)   lseek64(file, offset, origin)
#define WRITE(file, data, size)         write(file, data, size)
#define WRITE_SIZE(size)                (size)
#define GETPAGESIZE()                   sysconf(_SC_PAGESIZE)
#endif // _WIN32

#ifndef _Inout_
#define _Inout_
#endif

using namespace tracepoint_decode;

static constexpr auto InvalidFilePos = static_cast<uint64_t>(-1);
static constexpr char Zero64[8] = {};

_Success_(return == 0) static int
open_s(_Out_ int* pFile, _In_z_ char const* path, int oflags, int pmode)
{
#ifdef _WIN32
    return _sopen_s(pFile, path, oflags | _O_BINARY, _SH_DENYRW, pmode & (_S_IREAD | _S_IWRITE));
#else
    auto const file = open(path, oflags, pmode);
    *pFile = file;
    return file < 0 ? errno : 0;
#endif
}

template<class T>
static void
AppendValue(_Inout_ std::vector<char>* pHeader, T const& value)
{
    auto const pbValue = reinterpret_cast<char const*>(&value);
    pHeader->insert(pHeader->end(), pbValue, pbValue + sizeof(T));
}

static void
AppendStringZ(_Inout_ std::vector<char>* pHeader, std::string_view value)
{
    pHeader->insert(pHeader->end(), value.data(), value.data() + value.size());
    pHeader->push_back(0);
}

static void
AppendSection32(
    _Inout_ std::vector<char>* pHeader,
    _In_reads_bytes_(cb) char const* pb,
    uint32_t cb)
{
    AppendValue<uint32_t>(pHeader, cb);
    pHeader->insert(pHeader->end(), pb, pb + cb);
}

static void
AppendSection64(
    _Inout_ std::vector<char>* pHeader,
    _In_reads_bytes_(cb) char const* pb,
    size_t cb)
{
    AppendValue<uint64_t>(pHeader, cb);
    pHeader->insert(pHeader->end(), pb, pb + cb);
}

static void
AppendNamedSection64(
    _Inout_ std::vector<char>* pHeader,
    std::string_view name,
    std::string_view defaultValue,
    std::vector<char> const& setValue)
{
    auto value = setValue.data() == nullptr
        ? defaultValue
        : std::string_view(setValue.data(), setValue.size());
    AppendStringZ(pHeader, name);
    AppendSection64(pHeader, value.data(), value.size());
}

// From: perf.data-file-format.txt
struct PerfDataFileWriter::perf_file_section {
    uint64_t offset; // offset from start of file
    uint64_t size;   // size of the section
};

struct PerfDataFileWriter::perf_file_header
{
    static constexpr uint64_t Magic2 = 0x32454C4946524550; // "PERFILE2"

    uint64_t magic; // If correctly byte-swapped, this will be equal to Magic2.
    uint64_t size;  // size of the header
    uint64_t attr_size; // size of an attribute in attrs
    perf_file_section attrs;
    perf_file_section data;
    perf_file_section event_types; // Not used - superceded by PERF_HEADER_TRACING_DATA.

    // 256-bit bitmap based on HEADER_BITS
    uint64_t flags[4];
};

struct PerfDataFileWriter::EventDesc
{
    static uint32_t const NameMaxSize = 65535u;
    static uint32_t const SampleIdsMaxSize = 0xFFFFFFFF;

    std::vector<char> name; // does not include nul-termination, max size is NameMaxSize.
    std::vector<uint64_t> sampleIds; // max size is SampleIdsMaxSize.
    perf_event_attr attr;

    EventDesc(PerfEventDesc const& desc, uint32_t nameLen)
        : name(desc.name, desc.name + nameLen)
        , sampleIds(desc.ids, desc.ids + desc.ids_count)
    {
        if (sizeof(attr) <= desc.attr->size)
        {
            memcpy(&attr, desc.attr, sizeof(attr));
            attr.size = static_cast<perf_event_attr_size>(sizeof(attr));
        }
        else
        {
            memset(&attr, 0, sizeof(attr));
            memcpy(&attr, desc.attr, desc.attr->size);
        }
    }
};

struct PerfDataFileWriter::TracepointInfo
{
    PerfEventMetadata const& metadata;
    size_t eventDescIndex;

    TracepointInfo(PerfEventMetadata const& _metadata, size_t _eventDescIndex)
        : metadata(_metadata)
        , eventDescIndex(_eventDescIndex)
    {
        return;
    }
};

PerfDataFileWriter::~PerfDataFileWriter()
{
    CloseNoFinalize();
}

PerfDataFileWriter::PerfDataFileWriter() noexcept(false)
    : m_filePos(InvalidFilePos)
    , m_file(-1)
    , m_eventDescs()
    , m_tracepointInfoByCommonType()
    , m_headers()
    , m_tracingDataPageSize(0)
    , m_tracingDataLongSize(sizeof(size_t))
{
    return;
}

void
PerfDataFileWriter::CloseNoFinalize() noexcept
{
    assert(ValidFilePos());
    if (m_file >= 0)
    {
        CLOSE(m_file);
    }

    m_file = -1;
    m_filePos = InvalidFilePos;
}

_Success_(return == 0) int
PerfDataFileWriter::FinalizeAndClose() noexcept
{
    int error = 0;

    try
    {
        if (m_headers[PERF_HEADER_TRACING_DATA].empty())
        {
            SynthesizeTracingData();
        }

        if (m_headers[PERF_HEADER_EVENT_DESC].empty())
        {
            SynthesizeEventDesc();
        }

        perf_file_header fileHeader = {};
        fileHeader.magic = perf_file_header::Magic2;
        fileHeader.size = sizeof(perf_file_header);
        fileHeader.attr_size = sizeof(perf_event_attr) + sizeof(perf_file_section);

        // Current implementation starts the data section immediately after the file header.
        // It ends wherever we are now.
        fileHeader.data.offset = sizeof(perf_file_header);
        fileHeader.data.size = m_filePos - sizeof(perf_file_header);

        // The sections for the perf headers must go immediately after the data section.
        // Current implementation puts the data for the perf headers right after that.
        error = WriteHeaders(&fileHeader.flags[0]);
        if (error != 0)
        {
            goto Done;
        }

        // The attr section contains a sequence of attr+idSection blocks.
        // Current implementation puts the id data right after that.
        error = WriteAttrs(&fileHeader.attrs);
        if (error != 0)
        {
            goto Done;
        }

        // Finalize the file header.

        assert(ValidFilePos());

        auto const seekResult = LSEEK64(m_file, 0, SEEK_SET);
        m_filePos = 0;
        if (seekResult != 0)
        {
            error = seekResult < 0 ? errno : EDOM;
            goto Done;
        }

        error = WriteData(&fileHeader, sizeof(fileHeader));
    }
    catch (...)
    {
        error = ENOMEM;
    }

Done:

    CloseNoFinalize();
    return error;
}

_Success_(return == 0) int
PerfDataFileWriter::Create(_In_z_ char const* filePath, int mode) noexcept
{
    int error;

    CloseNoFinalize();
    m_eventDescs.clear();
    m_tracepointInfoByCommonType.clear();
    for (auto& header : m_headers)
    {
        header.clear();
    }

    error = open_s(&m_file, filePath, O_CREAT | O_WRONLY | O_TRUNC | O_CLOEXEC, mode);
    if (m_file < 0)
    {
        error = errno;
    }
    else
    {
        m_filePos = 0;
        static constexpr perf_file_header zeroHeader = {};
        error = WriteData(&zeroHeader, sizeof(zeroHeader));
        if (error != 0)
        {
            CloseNoFinalize();
        }
    }

    return error;
}

uint64_t
PerfDataFileWriter::FilePos() const noexcept
{
    assert(ValidFilePos());
    return m_filePos;
}

_Success_(return == 0) int
PerfDataFileWriter::WriteEventData(
    _In_reads_bytes_(dataSize) void const* data,
    size_t dataSize) noexcept
{
    int error = WriteData(data, dataSize);
    return error;
}

#ifndef _WIN32

_Success_(return >= 0) ptrdiff_t
PerfDataFileWriter::WriteEventDataIovecs(
    _In_reads_(iovecsCount) struct iovec const* iovecs,
    int iovecsCount) noexcept
{
    auto const writeResult = writev(m_file, iovecs, iovecsCount);
    if (writeResult >= 0)
    {
        m_filePos += writeResult;
    }

    return writeResult;
}

#endif // !_WIN32

_Success_(return == 0) int
PerfDataFileWriter::WriteFinishedInit() noexcept
{
    static perf_event_header const finishedInit = {
        PERF_RECORD_FINISHED_INIT, 0, sizeof(perf_event_header) };
    return WriteEventData(&finishedInit, sizeof(finishedInit));
}

_Success_(return == 0) int
PerfDataFileWriter::WriteFinishedRound() noexcept
{
    static perf_event_header const finishedRound = {
        PERF_RECORD_FINISHED_ROUND, 0, sizeof(perf_event_header) };
    return WriteEventData(&finishedRound, sizeof(finishedRound));
}

size_t
PerfDataFileWriter::GetHeaderSize(PerfHeaderIndex index) const noexcept
{
    return index >= PERF_HEADER_LAST_FEATURE
        ? 0u
        : m_headers[index].size();
}

void const*
PerfDataFileWriter::GetHeaderData(PerfHeaderIndex index) const noexcept
{
    return index >= PERF_HEADER_LAST_FEATURE
        ? 0
        : m_headers[index].data();
}

_Success_(return == 0) int
PerfDataFileWriter::SetHeader(
    PerfHeaderIndex index,
    _In_reads_bytes_(dataSize) void const* data,
    size_t dataSize) noexcept
{
    int error;

    try
    {
        if (index >= PERF_HEADER_LAST_FEATURE)
        {
            error = EINVAL;
        }
        else
        {
            auto const pData = static_cast<char const*>(data);
            m_headers[index].assign(&pData[0], &pData[dataSize]);
            error = 0;
        }
    }
    catch (...)
    {
        error = ENOMEM;
    }

    return error;
}

_Success_(return == 0) int
PerfDataFileWriter::SetStringHeader(
    PerfHeaderIndex index,
    _In_z_ char const* str) noexcept
{
    int error;

    try
    {
        if (index >= PERF_HEADER_LAST_FEATURE)
        {
            error = EINVAL;
        }
        else
        {
            auto const strLen = strlen(str);
            if (strLen >= 0xFFFFFFFF)
            {
                error = E2BIG;
            }
            else
            {
                auto const cbStr = strLen + 1;
                auto const headerLen = (sizeof(uint32_t) + cbStr + 7) & ~size_t(7);

                /*
                struct perf_header_string {
                       uint32_t len;
                       char string[len]; // zero terminated
                };
                */
                auto& header = m_headers[index];
                header.clear();
                header.reserve(headerLen);
                AppendValue(&header, static_cast<uint32_t>(cbStr)); // uint32_t len;
                header.insert(header.end(), &str[0], &str[cbStr]);  // char string[len];
                header.resize(headerLen); // NUL-terminate, pad to a multiple of 8 bytes.
                error = 0;
            }
        }
    }
    catch (...)
    {
        error = ENOMEM;
    }

    return error;
}

_Success_(return == 0) int
PerfDataFileWriter::SetNrCpusHeader(uint32_t available, uint32_t online) noexcept
{
    struct {
        uint32_t available;
        uint32_t online;
    } const nrcpus = {
        available,
        online,
    };
    return SetHeader(PERF_HEADER_NRCPUS, &nrcpus, sizeof(nrcpus));
}

_Success_(return == 0) int
PerfDataFileWriter::SetSampleTimeHeader(uint64_t first, uint64_t last) noexcept
{
    struct {
        uint64_t first;
        uint64_t last;
    } const times = {
        first,
        last,
    };
    return SetHeader(PERF_HEADER_SAMPLE_TIME, &times, sizeof(times));
}

_Success_(return == 0) int
PerfDataFileWriter::SetClockidHeader(uint32_t clockid) noexcept
{
    uint64_t const clockid64 = clockid;
    return SetHeader(PERF_HEADER_CLOCKID, &clockid64, sizeof(clockid64));
}

_Success_(return == 0) int
PerfDataFileWriter::SetClockDataHeader(uint32_t clockid, uint64_t wallClockNS, uint64_t clockidTimeNS) noexcept
{
    struct {
        uint32_t version;
        uint32_t clockid;
        uint64_t wallClockNS;
        uint64_t clockidTimeNS;
    } const clockData = {
        1,
        clockid,
        wallClockNS,
        clockidTimeNS,
    };
    return SetHeader(PERF_HEADER_CLOCK_DATA, &clockData, sizeof(clockData));
}

_Success_(return == 0) int
PerfDataFileWriter::SetSessionInfoHeaders(PerfEventSessionInfo const& sessionInfo) noexcept
{
    int error = 0;

    auto const clockid = sessionInfo.Clockid();
    if (clockid == 0xFFFFFFFF)
    {
        m_headers[PERF_HEADER_CLOCKID].clear();
    }
    else
    {
        error = SetClockidHeader(clockid);
        if (error != 0)
        {
            goto Done;
        }
    }

    if (!sessionInfo.ClockOffsetKnown())
    {
        m_headers[PERF_HEADER_CLOCK_DATA].clear();
    }
    else
    {
        uint64_t wallClockNS, clockidTimeNS;
        sessionInfo.GetClockData(&wallClockNS, &clockidTimeNS);
        error = SetClockDataHeader(clockid, wallClockNS, clockidTimeNS);
        if (error != 0)
        {
            goto Done;
        }
    }

Done:

    return error;
}

#ifndef _WIN32

_Success_(return == 0) int
PerfDataFileWriter::SetUtsNameHeaders(utsname const& uts) noexcept
{
    int error;

    error = SetStringHeader(PERF_HEADER_HOSTNAME, uts.nodename);
    if (error != 0)
    {
        goto Done;
    }

    error = SetStringHeader(PERF_HEADER_OSRELEASE, uts.release);
    if (error != 0)
    {
        goto Done;
    }

    error = SetStringHeader(PERF_HEADER_ARCH, uts.machine);
    if (error != 0)
    {
        goto Done;
    }

Done:

    return error;
}

#endif // !_WIN32

_Success_(return == 0) int
PerfDataFileWriter::SetTracingData(
    uint8_t longSize,
    uint32_t pageSize,
    std::string_view headerPage,
    std::string_view headerEvent,
    _In_reads_(ftraceCount) std::string_view const* ftraces,
    uint32_t ftraceCount,
    std::string_view kallsyms,
    std::string_view printk,
    std::string_view savedCmdLine) noexcept
{
    int error;

    if (kallsyms.size() > 0x80000000 ||
        printk.size() > 0x80000000)
    {
        error = E2BIG;
    }
    else try
    {
        if (longSize != 0)
        {
            m_tracingDataLongSize = longSize;
        }

        if (pageSize != 0)
        {
            m_tracingDataPageSize = pageSize;
        }

        if (headerPage.data())
        {
            m_tracingDataHeaderPage.assign(headerPage.data(), headerPage.data() + headerPage.size());
        }

        if (headerEvent.data())
        {
            m_tracingDataHeaderEvent.assign(headerEvent.data(), headerEvent.data() + headerEvent.size());
        }

        if (ftraces)
        {
            m_tracingDataFtraces.clear();
            m_tracingDataFtraces.reserve(ftraceCount);
            for (uint32_t i = 0; i != ftraceCount; i += 1)
            {
                auto ftrace = ftraces[i];
                m_tracingDataFtraces.emplace_back(ftrace.data(), ftrace.data() + ftrace.size());
            }
        }

        if (kallsyms.data())
        {
            m_tracingDataKallsyms.assign(kallsyms.data(), kallsyms.data() + kallsyms.size());
        }

        if (printk.data())
        {
            m_tracingDataPrintk.assign(printk.data(), printk.data() + printk.size());
        }

        if (savedCmdLine.data())
        {
            m_tracingDataSavedCmdline.assign(savedCmdLine.data(), savedCmdLine.data() + savedCmdLine.size());
        }

        error = 0;
    }
    catch (...)
    {
        error = ENOMEM;
    }

    return error;
}

_Success_(return == 0) int
PerfDataFileWriter::AddEventDesc(PerfEventDesc const& desc) noexcept
{
    int error;

    assert(desc.attr != nullptr); // Precondition
    assert(desc.name != nullptr); // Precondition
    if (m_eventDescs.size() == 0xFFFFFFFF)
    {
        error = E2BIG;
    }
    else try
    {
        auto const nameLen = strlen(desc.name);
        if (nameLen > EventDesc::NameMaxSize)
        {
            error = E2BIG;
        }
        else
        {
            m_eventDescs.emplace_back(desc, static_cast<uint32_t>(nameLen));
            error = 0;
        }
    }
    catch (...)
    {
        error = ENOMEM;
    }

    return error;
}

bool
PerfDataFileWriter::HasTracepointEventDesc(uint32_t common_type) const noexcept
{
    return 0 != m_tracepointInfoByCommonType.count(common_type);
}

_Success_(return == 0) int
PerfDataFileWriter::AddTracepointEventDesc(PerfEventDesc const& desc) noexcept
{
    int error;

    assert(desc.attr != nullptr); // Precondition
    assert(desc.name != nullptr); // Precondition
    assert(desc.metadata != nullptr); // Precondition
    try
    {
        auto [it, inserted] = m_tracepointInfoByCommonType.try_emplace(
            desc.metadata->Id(),
            *desc.metadata,
            m_eventDescs.size());
        if (!inserted)
        {
            error = EEXIST;
        }
        else
        {
            // Added new metadata so add new EventDesc too.
            error = AddEventDesc(desc);
            if (error != 0)
            {
                m_tracepointInfoByCommonType.erase(it);
            }
        }
    }
    catch (...)
    {
        error = ENOMEM;
    }

    return error;
}

bool
PerfDataFileWriter::ValidFilePos() const noexcept
{
    if (m_file < 0)
    {
        // File is closed, m_filePos should be -1.
        return m_filePos == InvalidFilePos;
    }

    auto const seekResult = LSEEK64(m_file, 0, SEEK_CUR);
    if (seekResult < 0)
    {
        // File is in error condition, m_filePos is unknown.
        return true;
    }

    return m_filePos == static_cast<uint64_t>(seekResult);
}

_Success_(return == 0) int
PerfDataFileWriter::WriteData(
    _In_reads_bytes_(dataSize) void const* data,
    size_t dataSize) noexcept
{
    int error = 0;

    for (size_t i = 0; i < dataSize;)
    {
        auto const writeSize = WRITE_SIZE(dataSize - i);
        auto const writeResult = WRITE(
            m_file,
            static_cast<char const*>(data) + i,
            writeSize);
        if (writeResult < 0)
        {
            error = errno;
            break;
        }

        m_filePos += writeResult;
        i += writeResult;
    }

    return error;
}

void
PerfDataFileWriter::SynthesizeTracingData()
{
    using namespace std::string_view_literals;

    static constexpr auto DefaultHeaderPage = 
        "\tfield: u64 timestamp;\toffset:0;\tsize:8;\tsigned:0;\n"
        "\tfield: local_t commit;\toffset:8;\tsize:8;\tsigned:1;\n"
        "\tfield: int overwrite;\toffset:8;\tsize:1;\tsigned:1;\n"
        "\tfield: char data;\toffset:16;\tsize:4080;\tsigned:0;\n"
        ""sv;
    static constexpr auto DefaultHeaderEvent =
        "# compressed entry header\n"
        "\ttype_len    :    5 bits\n"
        "\ttime_delta  :   27 bits\n"
        "\tarray       :   32 bits\n"
        "\n"
        "\tpadding     : type == 29\n"
        "\ttime_extend : type == 30\n"
        "\ttime_stamp : type == 31\n"
        "\tdata max type_len  == 28\n"
        ""sv;

    auto& header = m_headers[PERF_HEADER_TRACING_DATA];
    header.clear();

    if (m_tracingDataPageSize == 0)
    {
        m_tracingDataPageSize = GETPAGESIZE();
    }

    AppendStringZ(&header, "\x17\x08\x44tracing0.6"sv);
    AppendValue<uint8_t>(&header, HostIsBigEndian);
    AppendValue<uint8_t>(&header, m_tracingDataLongSize);
    AppendValue<uint32_t>(&header, m_tracingDataPageSize);
    AppendNamedSection64(&header, "header_page"sv, DefaultHeaderPage, m_tracingDataHeaderPage);
    AppendNamedSection64(&header, "header_event"sv, DefaultHeaderEvent, m_tracingDataHeaderEvent);

    // ftraces
    assert(m_tracingDataFtraces.size() <= 0xFFFFFFFF);
    AppendValue<uint32_t>(&header, static_cast<uint32_t>(m_tracingDataFtraces.size()));
    for (auto const& ftrace : m_tracingDataFtraces)
    {
        AppendSection64(&header, ftrace.data(), ftrace.size());
    }

    // systems (and events)

    // Group events by system.
    using InfoIt = decltype(m_tracepointInfoByCommonType.cbegin());
    std::map<std::string_view, std::vector<InfoIt>> commonTypesBySystem;
    for (auto it = m_tracepointInfoByCommonType.cbegin(); it != m_tracepointInfoByCommonType.cend(); ++it)
    {
        auto const& meta = it->second.metadata;
        commonTypesBySystem[meta.SystemName()].push_back(it);
    }

    // SystemCount
    AppendValue<uint32_t>(
        &header,
        static_cast<uint32_t>(commonTypesBySystem.size()));

    // Systems
    for (auto& ctbs : commonTypesBySystem)
    {
        // SystemName
        AppendStringZ(&header, ctbs.first);

        // EventCount
        AppendValue<uint32_t>(&header, static_cast<uint32_t>(ctbs.second.size()));

        // Events
        for (auto it : ctbs.second)
        {
            auto const& meta = it->second.metadata;
            auto const format = meta.FormatFileContents();
            AppendSection64(&header, format.data(), format.size());
        }
    }

    // Other stuff.
    AppendSection32(
        &header,
        m_tracingDataKallsyms.data(),
        static_cast<uint32_t>(m_tracingDataKallsyms.size()));
    AppendSection32(
        &header,
        m_tracingDataPrintk.data(),
        static_cast<uint32_t>(m_tracingDataPrintk.size()));
    AppendSection64(
        &header,
        m_tracingDataSavedCmdline.data(),
        m_tracingDataSavedCmdline.size());
}

void
PerfDataFileWriter::SynthesizeEventDesc()
{
    auto& header = m_headers[PERF_HEADER_EVENT_DESC];
    header.clear();

    /*
    From perf.data-file-format.txt:
    struct {
           uint32_t nr; // number of events
           uint32_t attr_size; //size of each perf_event_attr
           struct {
	          struct perf_event_attr attr; // size of attr_size
	          uint32_t nr_ids;
	          struct perf_header_string event_string;
	          uint64_t ids[nr_ids];
           } events[nr]; // Variable length records
    };
    */

    assert(m_eventDescs.size() <= 0xFFFFFFFF);
    auto const nr = static_cast<uint32_t>(m_eventDescs.size());

    AppendValue<uint32_t>(&header, nr);                         // nr
    AppendValue<uint32_t>(&header, sizeof(perf_event_attr));    // attr_size
    for (auto& desc : m_eventDescs)                             // events
    {
        auto const name = desc.name.data();

        static_assert(EventDesc::NameMaxSize < static_cast<uint32_t>(EventDesc::NameMaxSize + 8u),
            "Bad NameMaxSize (nameSize + namePad can overflow)");
        assert(desc.name.size() <= EventDesc::NameMaxSize);
        auto const nameSize = static_cast<uint32_t>(desc.name.size());
        auto const namePad = 8u - (nameSize & 7); // 1 to 8 bytes of '\0'.

        assert(desc.sampleIds.size() <= EventDesc::SampleIdsMaxSize);
        auto const nr_ids = static_cast<uint32_t>(desc.sampleIds.size());

        AppendValue<perf_event_attr>(&header, desc.attr);       // attr
        AppendValue<uint32_t>(&header, nr_ids);                 // nr_ids
        AppendValue<uint32_t>(&header, nameSize + namePad);     // event_string.len
        header.insert(header.end(), &name[0], &name[nameSize]); // event_string.string
        header.insert(header.end(), &Zero64[0], &Zero64[namePad]);// NUL + pad to x8
        header.insert(                                          // ids
            header.end(),
            reinterpret_cast<char const*>(desc.sampleIds.data()),
            reinterpret_cast<char const*>(desc.sampleIds.data() + desc.sampleIds.size()));
    }
}

_Success_(return == 0) int
PerfDataFileWriter::WriteHeaders(_Out_ uint64_t* pFlags0) noexcept
{
    int error = 0;
    uint64_t flags0 = 0;

    // Update the flags and compute where the first perf header will go.
    auto firstPerfHeaderOffset = m_filePos;
    for (uint32_t i = 0; i < PERF_HEADER_LAST_FEATURE; i += 1)
    {
        auto const& header = m_headers[i];
        if (!header.empty())
        {
            flags0 |= static_cast<uint64_t>(1) << i;
            firstPerfHeaderOffset += sizeof(perf_file_section);
        }
    }

    // Store perf_file_section for each perf header.
    auto perfHeaderOffset = firstPerfHeaderOffset;
    for (uint32_t i = 0; i < PERF_HEADER_LAST_FEATURE; i += 1)
    {
        auto& header = m_headers[i];
        if (!header.empty())
        {
            auto headerSize = header.size();

            perf_file_section headerSection = {};
            headerSection.offset = perfHeaderOffset;
            headerSection.size = headerSize;
            perfHeaderOffset += headerSize;

            error = WriteData(&headerSection, sizeof(headerSection));
            if (error != 0)
            {
                goto Done;
            }
        }
    }

    // Store data for each perf header that is present.
    for (uint32_t i = 0; i < PERF_HEADER_LAST_FEATURE; i += 1)
    {
        auto const& header = m_headers[i];
        if (!header.empty())
        {
            error = WriteData(header.data(), header.size());
            if (error != 0)
            {
                goto Done;
            }
        }
    }

    assert(m_filePos == perfHeaderOffset);

Done:

    *pFlags0 = flags0;
    return error;
}

_Success_(return == 0) int
PerfDataFileWriter::WriteAttrs(_Out_ perf_file_section* pAttrsSection) noexcept
{
    int error = 0;

    uint64_t const PerfAttrEntrySize = sizeof(perf_event_attr) + sizeof(perf_file_section);
    auto const perfAttrSectionSize = PerfAttrEntrySize * m_eventDescs.size();
    pAttrsSection->offset = m_filePos;
    pAttrsSection->size = perfAttrSectionSize;

    auto const firstAttrIdsOffset = m_filePos + perfAttrSectionSize;
    auto attrIdsOffset = firstAttrIdsOffset;

    for (auto const& desc : m_eventDescs)
    {
        error = WriteData(&desc.attr, sizeof(perf_event_attr));
        if (error != 0)
        {
            goto Done;
        }

        perf_file_section idsSection = {};
        idsSection.offset = attrIdsOffset;
        idsSection.size = desc.sampleIds.size() * sizeof(desc.sampleIds[0]);
        attrIdsOffset += idsSection.size;

        error = WriteData(&idsSection, sizeof(idsSection));
        if (error != 0)
        {
            goto Done;
        }
    }

    for (auto const& desc : m_eventDescs)
    {
        error = WriteData(
            desc.sampleIds.data(),
            desc.sampleIds.size() * sizeof(desc.sampleIds[0]));
        if (error != 0)
        {
            goto Done;
        }
    }

Done:

    return error;
}
