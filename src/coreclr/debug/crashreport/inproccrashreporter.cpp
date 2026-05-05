// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// In-proc crash report generator.
//
// Streams a createdump-shaped JSON skeleton to a crashreport.json file.

#include "inproccrashreporter.h"
#include "signalsafeconsolewriter.h"
#include "signalsafejsonwriter.h"
#include "signalsafeformat.h"

#include "pal.h"

#include <fcntl.h>
#include <errno.h>
#include <unistd.h>
#include <string.h>
#include <time.h>
#include <ucontext.h>
#include <minipal/getexepath.h>
#include <minipal/guid.h>
#include <minipal/thread.h>
#ifdef __APPLE__
#include <mach/mach.h>
#include <sys/sysctl.h>
#endif

extern "C" const char* PROCGetSignalNameAscii(int signal);

static const char CRASHREPORT_PROTOCOL_VERSION[] = "1.0.0";

#if defined(__x86_64__)
static const char CRASHREPORT_ARCHITECTURE_NAME[] = "amd64";
#elif defined(__aarch64__)
static const char CRASHREPORT_ARCHITECTURE_NAME[] = "arm64";
#elif defined(__arm__)
static const char CRASHREPORT_ARCHITECTURE_NAME[] = "arm";
#endif

// Prescribed compact crash report log format. One logical line == one
// __android_log_write entry under tag "DOTNET_CRASH" on Android, one
// '\n'-terminated stderr write elsewhere.
//
//   *** *** *** *** *** *** *** *** *** *** *** *** *** *** *** ***   (EmitConsoleHeader)
//   .NET Crash Report v<protocol>
//   Build: <sccsid>                                                    (omitted if empty)
//   ABI: amd64|arm64|arm
//   Cmdline: <process name>                                            (omitted if empty)
//   pid: <pid>
//   signal <N> (<NAME>)
//                                                                      (blank between sections)
//   --- thread 0xTID [(crashed)] ---                                   (per thread; OnThread)
//     managed exception: <Type> (0x<HRESULT>)                          (only if EE provided one)
//     #NN [M] Class.Method + 0xILOFFSET (token=0xTOKEN)                (managed frame; WriteFrameToConsole)
//     #NN (in <name>) Class.Method + 0xILOFFSET (token=0xTOKEN)        (overflow form: module didn't fit the table)
//     #NN [M] 0xIP (module + 0xOFFSET)                                 (native frame; WriteFrameToConsole)
//     #NN 0xIP (module + 0xOFFSET)                                     (native frame not in module table)
//     (no managed frames)                                              (FinishCurrentThreadCompactBlock)
//                                                                      (blank between threads)
//   modules:                                                           (EmitConsoleModulesAndFooter)
//     [N] <name> {<MVID>}                                              (one per ModuleTable entry)
//   *** *** *** *** *** *** *** *** *** *** *** *** *** *** *** ***   (closing separator)

static SignalSafeConsoleWriter s_consoleWriter;

// Include the .NET version string instead of linking because it is "static".
#if __has_include("_version.c")
#include "_version.c"
#else
static char sccsid[] = "@(#)Version N/A";
#endif

#ifdef __APPLE__
// Query a sysctl by name into a caller-supplied buffer. Called from Initialize, NOT from the
// signal handler -- sysctl/sysctlbyname is not on POSIX's async-signal-safe list, so the
// queried values are cached for use during crash reporting (mirrors the m_hostName /
// gethostname pattern).
static void CacheSysctlString(const char* sysctlName, char* buffer, size_t bufferSize)
{
    buffer[0] = '\0';
    size_t size = bufferSize;
    if (sysctlbyname(sysctlName, buffer, &size, nullptr, 0) == 0 && size > 0)
    {
        size_t terminatorIndex = (size < bufferSize) ? size : bufferSize - 1;
        buffer[terminatorIndex] = '\0';
    }
    else
    {
        buffer[0] = '\0';
    }
}
#endif // __APPLE__

// Bounded module name/GUID table that deduplicates each unique module
// observed during a single crash report. Frames in the compact log refer to
// modules by short ``[N]`` indices instead of repeating the (often verbose)
// filename + GUID on every line; the matching ``modules:`` block at the end
// of the report maps each index back to the full data.
//
// Capacity is fixed at MAX_MODULES_IN_TABLE (no heap on the fatal-signal
// path). A managed frame whose module didn't fit (table full, or empty/null GUID)
// renders the module identity inline as ``(in <name>) `` so the frame stays
// self-describing — overflow is lossless, just less compact for that frame.
//
// Single-instance because CreateReport is one-shot per process (guarded by
// the ``s_generating`` InterlockedCompareExchange in CreateReport).

static constexpr size_t MAX_MODULES_IN_TABLE = 64;

class ModuleTable
{
public:
    int GetOrAddIndex(const char* moduleName, const char* moduleGuid)
    {
        if (moduleName == nullptr || moduleName[0] == '\0' ||
            moduleGuid == nullptr || moduleGuid[0] == '\0')
        {
            return -1;
        }

        for (size_t i = 0; i < m_count; ++i)
        {
            if (strncmp(m_entries[i].guid, moduleGuid, MINIPAL_GUID_BUFFER_LEN) == 0)
            {
                return static_cast<int>(i);
            }
        }

        if (m_count >= MAX_MODULES_IN_TABLE)
        {
            return -1;
        }

        Entry& entry = m_entries[m_count];
        size_t nameLen = strnlen(moduleName, sizeof(entry.name) - 1);
        memcpy(entry.name, moduleName, nameLen);
        entry.name[nameLen] = '\0';
        size_t guidLen = strnlen(moduleGuid, sizeof(entry.guid) - 1);
        memcpy(entry.guid, moduleGuid, guidLen);
        entry.guid[guidLen] = '\0';
        return static_cast<int>(m_count++);
    }

    size_t Count() const { return m_count; }
    const char* Name(size_t i) const { return m_entries[i].name; }
    const char* Guid(size_t i) const { return m_entries[i].guid; }

private:
    struct Entry
    {
        char name[CRASHREPORT_STRING_BUFFER_SIZE];
        char guid[MINIPAL_GUID_BUFFER_LEN];
    };

    Entry m_entries[MAX_MODULES_IN_TABLE];
    size_t m_count = 0;
};

static ModuleTable s_moduleTable;

class ThreadEnumerationContext
{
public:
    ThreadEnumerationContext(
        SignalSafeJsonWriter* writer,
        SignalSafeConsoleWriter* consoleWriter,
        uint64_t crashingTid,
        void* signalContext)
        : m_writer(writer),
          m_consoleWriter(consoleWriter),
          m_signalContext(signalContext),
          m_threadCount(0),
          m_crashingTid(crashingTid),
          m_currentThreadFrameCount(0),
          m_sawCrashThread(false)
    {
    }

    ThreadEnumerationContext(const ThreadEnumerationContext&) = delete;
    ThreadEnumerationContext& operator=(const ThreadEnumerationContext&) = delete;

    size_t ThreadCount() const { return m_threadCount; }
    bool SawCrashThread() const { return m_sawCrashThread; }
    SignalSafeJsonWriter* Writer() const { return m_writer; }
    SignalSafeConsoleWriter* ConsoleWriter() const { return m_consoleWriter; }

    void EnumerateThreads(InProcCrashReportEnumerateThreadsCallback callback);

    static void ThreadCallback(
        uint64_t osThreadId,
        bool isCrashThread,
        const char* exceptionType,
        uint32_t exceptionHResult,
        void* ctx);

    static void FrameCallback(
        uint64_t ip,
        uint64_t stackPointer,
        const char* methodName,
        const char* className,
        const char* moduleName,
        uint32_t nativeOffset,
        uint32_t token,
        uint32_t ilOffset,
        uint32_t moduleTimestamp,
        uint32_t moduleSize,
        const char* moduleGuid,
        void* ctx);

private:
    void OnThread(
        uint64_t osThreadId,
        bool isCrashThread,
        const char* exceptionType,
        uint32_t exceptionHResult);

    void OnFrame(
        uint64_t ip,
        uint64_t stackPointer,
        const char* methodName,
        const char* className,
        const char* moduleName,
        uint32_t nativeOffset,
        uint32_t token,
        uint32_t ilOffset,
        uint32_t moduleTimestamp,
        uint32_t moduleSize,
        const char* moduleGuid);

    void FinishCurrentThreadCompactBlock();

    SignalSafeJsonWriter* m_writer;
    SignalSafeConsoleWriter* m_consoleWriter;
    void* m_signalContext;
    size_t m_threadCount;
    uint64_t m_crashingTid;
    uint32_t m_currentThreadFrameCount;
    bool m_sawCrashThread;
};

class CrashReportOutputContext
{
public:
    explicit CrashReportOutputContext(int fd)
        : m_fd(fd),
          m_writeFailed(false)
    {
    }

    CrashReportOutputContext(const CrashReportOutputContext&) = delete;
    CrashReportOutputContext& operator=(const CrashReportOutputContext&) = delete;

    int Fd() const { return m_fd; }
    bool WriteFailed() const { return m_writeFailed; }

    static bool ChunkCallback(const char* buffer, size_t len, void* ctx);

private:
    bool HandleChunk(const char* buffer, size_t len);

    int m_fd;
    bool m_writeFailed;
};

class CrashReportHelpers
{
public:
    struct FrameSinks
    {
        SignalSafeJsonWriter* writer;
        SignalSafeConsoleWriter* consoleWriter;
        uint32_t* currentThreadFrameCount;
    };

    static void GetVersionString(
        char* buffer,
        size_t bufferSize);

    static bool AppendString(
        char* buffer,
        size_t bufferSize,
        size_t* pos,
        const char* value);

    static void WriteRegistersToJson(
        SignalSafeJsonWriter* writer,
        void* context);

    static uint64_t GetInstructionPointer(
        void* context);

    static uint64_t GetStackPointer(
        void* context);

    static uint64_t GetFramePointer(
        void* context);

    static void WriteCrashSiteFrameToJson(
        SignalSafeJsonWriter* writer,
        void* context);

    static void BuildMethodName(
        char* buffer,
        size_t bufferSize,
        const char* className,
        const char* methodName);

    static const char* GetFilename(
        const char* path);

    static void CopyString(
        char* buffer,
        size_t bufferSize,
        const char* value);

    static void JsonFrameCallback(
        uint64_t ip,
        uint64_t stackPointer,
        const char* methodName,
        const char* className,
        const char* moduleName,
        uint32_t nativeOffset,
        uint32_t token,
        uint32_t ilOffset,
        uint32_t moduleTimestamp,
        uint32_t moduleSize,
        const char* moduleGuid,
        void* ctx);

    static void WriteFrameToJson(
        SignalSafeJsonWriter* writer,
        uint64_t ip,
        uint64_t stackPointer,
        const char* methodName,
        const char* className,
        const char* moduleName,
        uint32_t nativeOffset,
        uint32_t token,
        uint32_t ilOffset,
        uint32_t moduleTimestamp,
        uint32_t moduleSize,
        const char* moduleGuid);

    static void WriteFrameToConsole(
        SignalSafeConsoleWriter* consoleWriter,
        uint32_t frameIndex,
        int moduleIndex,
        uint64_t ip,
        const char* methodName,
        const char* className,
        const char* moduleName,
        uint32_t nativeOffset,
        uint32_t token,
        uint32_t ilOffset);

    static void WriteThreadBlockHeaderToConsole(
        SignalSafeConsoleWriter* consoleWriter,
        uint64_t osThreadId,
        bool isCrashThread);

    static void WriteThreadBlockCloserToConsole(
        SignalSafeConsoleWriter* consoleWriter,
        uint32_t frameCount);

    static void FrameSinkCallback(
        uint64_t ip,
        uint64_t stackPointer,
        const char* methodName,
        const char* className,
        const char* moduleName,
        uint32_t nativeOffset,
        uint32_t token,
        uint32_t ilOffset,
        uint32_t moduleTimestamp,
        uint32_t moduleSize,
        const char* moduleGuid,
        void* ctx);

    static bool WriteToFile(
        int fd,
        const char* buffer,
        size_t len);

    // SignalSafeJsonWriter callback that drops everything: used when the
    // crash report is running in compact-log-only mode (no DbgMiniDumpName)
    // so the JSON formatter still keeps its bookkeeping consistent without
    // emitting bytes anywhere.
    static bool DiscardOutputCallback(const char* buffer, size_t len, void* ctx);

    static bool BuildReportPath(
        char* buffer,
        size_t bufferSize,
        const char* dumpPath,
        const char* processName,
        const char* hostName);

    static size_t ExpandDumpTemplate(
        char* buffer,
        size_t bufferSize,
        const char* pattern,
        const char* processName,
        const char* hostName);
};

void
InProcCrashReporter::CreateReport(
    int signal,
    siginfo_t* siginfo,
    void* context)
{
    static LONG s_generating = 0;
    if (InterlockedCompareExchange(&s_generating, 1, 0) != 0)
    {
        return;
    }

    char reportPath[CRASHREPORT_PATH_BUFFER_SIZE];
    reportPath[0] = '\0';

    // The JSON file sink is only enabled when DbgMiniDumpName supplied a
    // template AND the template expanded to a valid path. Otherwise the
    // crash report runs in compact-log-only mode: the JSON emitter still
    // executes (so it can keep its bookkeeping consistent) but writes go
    // to a no-op DiscardOutputCallback instead of an open fd.
    bool jsonEnabled = m_reportPath[0] != '\0' &&
        CrashReportHelpers::BuildReportPath(reportPath, sizeof(reportPath), m_reportPath, m_processName, m_hostName);

    int fd = -1;
    if (jsonEnabled)
    {
        fd = open(reportPath, O_WRONLY | O_CREAT | O_TRUNC, 0600);
        if (fd == -1)
        {
            jsonEnabled = false;
        }
    }

    (void)siginfo;

    EmitConsoleHeader(signal);

    CrashReportOutputContext outputContext(fd);
    if (jsonEnabled)
    {
        m_jsonWriter.Init(&CrashReportOutputContext::ChunkCallback, &outputContext);
    }
    else
    {
        m_jsonWriter.Init(&CrashReportHelpers::DiscardOutputCallback, nullptr);
    }

    EmitJsonHeader();

    m_jsonWriter.OpenArray("threads");
    if (m_enumerateThreadsCallback != nullptr)
    {
        uint64_t crashingTid = static_cast<uint64_t>(minipal_get_current_thread_id());
        ThreadEnumerationContext threadContext(&m_jsonWriter, &s_consoleWriter, crashingTid, context);

        threadContext.EnumerateThreads(m_enumerateThreadsCallback);

        if (threadContext.ThreadCount() == 0 || !threadContext.SawCrashThread())
        {
            EmitSynthesizedCrashThread(context, /*walkStack*/ false);
        }
    }
    else
    {
        EmitSynthesizedCrashThread(context, /*walkStack*/ true);
    }
    m_jsonWriter.CloseArray(); // threads

    EmitJsonFooter(signal);

    EmitConsoleModulesAndFooter();

    if (jsonEnabled)
    {
        bool writeSucceeded = m_jsonWriter.Finish() &&
            !outputContext.WriteFailed() &&
            CrashReportHelpers::WriteToFile(fd, "\n", 1);

        if (close(fd) != 0 || !writeSucceeded)
        {
            unlink(reportPath);
        }
    }
    else
    {
        (void)m_jsonWriter.Finish();
    }
}

InProcCrashReporter&
InProcCrashReporter::GetInstance()
{
    static InProcCrashReporter s_instance;
    return s_instance;
}

void
InProcCrashReporter::Initialize(
    const InProcCrashReporterSettings& settings)
{
    m_isManagedThreadCallback = settings.isManagedThreadCallback;
    m_walkStackCallback = settings.walkStackCallback;
    m_enumerateThreadsCallback = settings.enumerateThreadsCallback;
    CrashReportHelpers::CopyString(m_reportPath, sizeof(m_reportPath), settings.reportPath);

    m_processName[0] = '\0';
#if defined(__ANDROID__)
    // On Android every app forks from the Zygote, so /proc/self/exe always
    // resolves to /system/bin/app_process64. /proc/self/cmdline holds the
    // package name (set by ActivityThread via PR_SET_NAME / setproctitle),
    // which is what crash diagnostics actually want.
    int cmdlineFd = open("/proc/self/cmdline", O_RDONLY | O_CLOEXEC);
    if (cmdlineFd >= 0)
    {
        char buf[CRASHREPORT_STRING_BUFFER_SIZE];
        ssize_t n = read(cmdlineFd, buf, sizeof(buf) - 1);
        close(cmdlineFd);
        if (n > 0)
        {
            buf[n] = '\0';
            CrashReportHelpers::CopyString(m_processName, sizeof(m_processName), CrashReportHelpers::GetFilename(buf));
        }
    }
#endif
    if (m_processName[0] == '\0')
    {
        if (char* exePath = minipal_getexepath())
        {
            CrashReportHelpers::CopyString(m_processName, sizeof(m_processName), CrashReportHelpers::GetFilename(exePath));
            free(exePath);
        }
    }

    // Cache hostname here because gethostname is not on the POSIX
    // async-signal-safe list; the dump-template expander needs it for %h
    // expansion at crash time.
    m_hostName[0] = '\0';
    if (gethostname(m_hostName, sizeof(m_hostName) - 1) == 0)
    {
        m_hostName[sizeof(m_hostName) - 1] = '\0';
    }
    else
    {
        m_hostName[0] = '\0';
    }

#ifdef __APPLE__
    // Cache sysctl values at Initialize because sysctl/sysctlbyname is not on POSIX's
    // async-signal-safe list; CreateReport reads these from the signal-handler path.
    CacheSysctlString("kern.osproductversion", m_osVersion, sizeof(m_osVersion));
    CacheSysctlString("hw.model", m_systemModel, sizeof(m_systemModel));
#endif
}

void
InProcCrashReportSignalDispatcher(int signal, void* siginfo, void* context)
{
    InProcCrashReporter& reporter = InProcCrashReporter::GetInstance();
    reporter.CreateReport(signal, static_cast<siginfo_t*>(siginfo), context);
}

void
InProcCrashReportInitialize(const InProcCrashReporterSettings& settings)
{
    InProcCrashReporter::GetInstance().Initialize(settings);

    // Register last so PAL only observes the dispatcher after the reporter
    // singleton is fully populated (mirrors the publication ordering used by
    // PAL_SetLogManagedCallstackForSignalCallback).
    PAL_SetInProcCrashReportCallback(&InProcCrashReportSignalDispatcher);
}

bool
CrashReportHelpers::WriteToFile(
    int fd,
    const char* buffer,
    size_t len)
{
    if (fd < 0 || buffer == nullptr)
    {
        return false;
    }

    size_t totalWritten = 0;
    while (totalWritten < len)
    {
        ssize_t written = write(fd, buffer + totalWritten, len - totalWritten);
        if (written > 0)
        {
            totalWritten += static_cast<size_t>(written);
            continue;
        }

        if (written == -1 && errno == EINTR)
        {
            continue;
        }

        return false;
    }

    return true;
}

bool
CrashReportHelpers::DiscardOutputCallback(
    const char* /*buffer*/,
    size_t /*len*/,
    void* /*ctx*/)
{
    return true;
}

bool
CrashReportOutputContext::HandleChunk(
    const char* buffer,
    size_t len)
{
    if (m_fd == -1)
    {
        return false;
    }

    if (!CrashReportHelpers::WriteToFile(m_fd, buffer, len))
    {
        m_writeFailed = true;
        return false;
    }

    return true;
}

bool
CrashReportOutputContext::ChunkCallback(
    const char* buffer,
    size_t len,
    void* ctx)
{
    CrashReportOutputContext* outputContext = reinterpret_cast<CrashReportOutputContext*>(ctx);
    if (outputContext == nullptr)
    {
        return false;
    }

    return outputContext->HandleChunk(buffer, len);
}

// Expand the coredump template patterns supported by createdump's
// FormatDumpName for DOTNET_DbgMiniDumpName: %% %p %d (PID), %e (process
// name, cached at Initialize), %h (hostname, cached at Initialize), and %t
// (current epoch seconds via time(2), POSIX async-signal-safe). Unknown
// specifiers are rejected (return 0) to match createdump and to avoid
// silently producing diverging file names from the same template.
size_t
CrashReportHelpers::ExpandDumpTemplate(
    char* buffer,
    size_t bufferSize,
    const char* pattern,
    const char* processName,
    const char* hostName)
{
    if (buffer == nullptr || bufferSize == 0 || pattern == nullptr)
    {
        return 0;
    }

    size_t pos = 0;
    unsigned pid = static_cast<unsigned>(GetCurrentProcessId());

    while (*pattern != '\0' && pos + 1 < bufferSize)
    {
        if (*pattern != '%')
        {
            buffer[pos++] = *pattern++;
            continue;
        }

        pattern++;
        char specifier = *pattern;

        const char* substitution = nullptr;
        char numberBuf[CRASHREPORT_NUMBER_BUFFER_SIZE];

        switch (specifier)
        {
            case '%':
                if (pos + 1 < bufferSize)
                {
                    buffer[pos++] = '%';
                }
                pattern++;
                continue;

            case 'p':
            case 'd':
                if (SignalSafeFormat::FormatUnsignedDecimal(numberBuf, sizeof(numberBuf), pid) == 0)
                {
                    return 0;
                }
                substitution = numberBuf;
                break;

            case 'e':
                substitution = (processName != nullptr && processName[0] != '\0') ? processName : nullptr;
                break;

            case 'h':
                substitution = (hostName != nullptr && hostName[0] != '\0') ? hostName : nullptr;
                break;

            case 't':
                if (SignalSafeFormat::FormatUnsignedDecimal(
                        numberBuf, sizeof(numberBuf), static_cast<uint64_t>(time(nullptr))) == 0)
                {
                    return 0;
                }
                substitution = numberBuf;
                break;

            default:
                // Unknown / unsupported specifier; fail rather than emit a
                // path with a literal '%X' that would diverge from the file
                // name createdump would produce for the same template.
                return 0;
        }

        if (substitution == nullptr)
        {
            // Required substitution unavailable (e.g. hostname capture failed
            // at Initialize). Fail rather than emit a path missing this
            // component, which could collide with the dump file on disk.
            return 0;
        }

        size_t subLen = strlen(substitution);
        if (pos + subLen >= bufferSize)
        {
            return 0;
        }
        memcpy(buffer + pos, substitution, subLen);
        pos += subLen;

        if (*pattern != '\0')
        {
            pattern++;
        }
    }

    buffer[pos] = '\0';
    if (*pattern != '\0')
    {
        // The output buffer filled before the full template was consumed.
        // Fail rather than returning a truncated path that could collide or
        // unexpectedly change the report location.
        return 0;
    }
    return pos;
}

bool
CrashReportHelpers::BuildReportPath(
    char* buffer,
    size_t bufferSize,
    const char* dumpPath,
    const char* processName,
    const char* hostName)
{
    if (buffer == nullptr || bufferSize == 0 || dumpPath == nullptr || dumpPath[0] == '\0')
    {
        return false;
    }

    char expanded[CRASHREPORT_PATH_BUFFER_SIZE];
    size_t expandedLen = ExpandDumpTemplate(expanded, sizeof(expanded), dumpPath, processName, hostName);
    if (expandedLen == 0)
    {
        return false;
    }

    size_t pos = 0;
    if (!AppendString(buffer, bufferSize, &pos, expanded))
    {
        return false;
    }
    if (!AppendString(buffer, bufferSize, &pos, ".crashreport.json"))
    {
        return false;
    }
    return true;
}

void
CrashReportHelpers::GetVersionString(
    char* buffer,
    size_t bufferSize)
{
    if (buffer == nullptr || bufferSize == 0)
    {
        return;
    }

    if (bufferSize == 1)
    {
        buffer[0] = '\0';
        return;
    }

    buffer[0] = '\0';

    const char* version = sccsid;
    const char versionPrefix[] = "@(#)Version ";
    if (strncmp(version, versionPrefix, sizeof(versionPrefix) - 1) != 0)
    {
        return;
    }

    version += sizeof(versionPrefix) - 1;

    size_t toCopy = strnlen(version, bufferSize - 1);
    if (toCopy != 0)
    {
        memcpy(buffer, version, toCopy);
    }

    buffer[toCopy] = '\0';
}

// Appends |value| to |buffer| at *|pos|, advancing *|pos|, while leaving
// room for a trailing null terminator. Always null-terminates when
// bufferSize > 0. Returns true iff the full value was appended.
// Async-signal-safe.
bool
CrashReportHelpers::AppendString(
    char* buffer,
    size_t bufferSize,
    size_t* pos,
    const char* value)
{
    if (buffer == nullptr || pos == nullptr || value == nullptr || bufferSize == 0)
    {
        return false;
    }

    size_t p = *pos;
    while (*value != '\0' && p + 1 < bufferSize)
    {
        buffer[p++] = *value++;
    }
    buffer[p] = '\0';
    *pos = p;
    return *value == '\0';
}

void
CrashReportHelpers::WriteRegistersToJson(
    SignalSafeJsonWriter* writer,
    void* context)
{
    uint64_t ipValue = GetInstructionPointer(context);
    uint64_t spValue = GetStackPointer(context);
    uint64_t bpValue = GetFramePointer(context);

    writer->OpenObject("ctx");
    writer->WriteHexAsString("IP", ipValue);
    writer->WriteHexAsString("SP", spValue);
    writer->WriteHexAsString("BP", bpValue);
    writer->CloseObject(); // ctx
}

uint64_t
CrashReportHelpers::GetInstructionPointer(
    void* context)
{
    if (context == nullptr)
    {
        return 0;
    }

    ucontext_t* ucontext = reinterpret_cast<ucontext_t*>(context);
#if defined(__APPLE__) && defined(__x86_64__)
    return static_cast<uint64_t>(ucontext->uc_mcontext->__ss.__rip);
#elif defined(__APPLE__) && defined(__aarch64__)
    return reinterpret_cast<uint64_t>(arm_thread_state64_get_pc_fptr(ucontext->uc_mcontext->__ss));
#elif defined(__x86_64__)
    return static_cast<uint64_t>(ucontext->uc_mcontext.gregs[REG_RIP]);
#elif defined(__aarch64__)
    return static_cast<uint64_t>(ucontext->uc_mcontext.pc);
#elif defined(__arm__)
    return static_cast<uint64_t>(ucontext->uc_mcontext.arm_pc);
#else
    return 0;
#endif
}

uint64_t
CrashReportHelpers::GetStackPointer(
    void* context)
{
    if (context == nullptr)
    {
        return 0;
    }

    ucontext_t* ucontext = reinterpret_cast<ucontext_t*>(context);
#if defined(__APPLE__) && defined(__x86_64__)
    return static_cast<uint64_t>(ucontext->uc_mcontext->__ss.__rsp);
#elif defined(__APPLE__) && defined(__aarch64__)
    return static_cast<uint64_t>(arm_thread_state64_get_sp(ucontext->uc_mcontext->__ss));
#elif defined(__x86_64__)
    return static_cast<uint64_t>(ucontext->uc_mcontext.gregs[REG_RSP]);
#elif defined(__aarch64__)
    return static_cast<uint64_t>(ucontext->uc_mcontext.sp);
#elif defined(__arm__)
    return static_cast<uint64_t>(ucontext->uc_mcontext.arm_sp);
#else
    return 0;
#endif
}

uint64_t
CrashReportHelpers::GetFramePointer(
    void* context)
{
    if (context == nullptr)
    {
        return 0;
    }

    ucontext_t* ucontext = reinterpret_cast<ucontext_t*>(context);
#if defined(__APPLE__) && defined(__x86_64__)
    return static_cast<uint64_t>(ucontext->uc_mcontext->__ss.__rbp);
#elif defined(__APPLE__) && defined(__aarch64__)
    return static_cast<uint64_t>(arm_thread_state64_get_fp(ucontext->uc_mcontext->__ss));
#elif defined(__x86_64__)
    return static_cast<uint64_t>(ucontext->uc_mcontext.gregs[REG_RBP]);
#elif defined(__aarch64__)
    return static_cast<uint64_t>(ucontext->uc_mcontext.regs[29]);
#elif defined(__arm__)
    return static_cast<uint64_t>(ucontext->uc_mcontext.arm_fp);
#else
    return 0;
#endif
}

void
CrashReportHelpers::WriteCrashSiteFrameToJson(
    SignalSafeJsonWriter* writer,
    void* context)
{
    uint64_t ipValue = GetInstructionPointer(context);
    uint64_t spValue = GetStackPointer(context);

    writer->OpenObject();
    // Crash-site frame: IP/SP captured directly from the signal's saved
    // ucontext_t. It is the instruction the OS interrupted (faulting user
    // code, libc abort(), the JIT, etc.) - not a frame inside this reporter.
    // Marked native because classifying an arbitrary IP as managed would
    // require a JIT lookup we deliberately avoid in the signal handler;
    // subsequent frames produced by the managed stack walker carry their
    // own is_managed classification.
    writer->WriteString("is_managed", "false");
    writer->WriteHexAsString("stack_pointer", spValue);
    writer->WriteHexAsString("native_address", ipValue);
    writer->CloseObject(); // frame
}

void
CrashReportHelpers::BuildMethodName(
    char* buffer,
    size_t bufferSize,
    const char* className,
    const char* methodName)
{
    if (buffer == nullptr || bufferSize == 0)
    {
        return;
    }

    if (className != nullptr && methodName != nullptr)
    {
        size_t pos = 0;
        AppendString(buffer, bufferSize, &pos, className);
        AppendString(buffer, bufferSize, &pos, ".");
        AppendString(buffer, bufferSize, &pos, methodName);
    }
    else if (className != nullptr)
    {
        CopyString(buffer, bufferSize, className);
    }
    else if (methodName != nullptr)
    {
        CopyString(buffer, bufferSize, methodName);
    }
    else
    {
        buffer[0] = '\0';
    }
}

// Returns the basename of a path (the substring after the last directory
// separator). The crash reporter is currently Unix-only via
// FEATURE_INPROC_CRASHREPORT gating, but a future Windows port would need
// a different separator; expose a platform-conditional constant so callers
// don't have to change.
#if defined(_WIN32)
static constexpr char CRASHREPORT_DIRECTORY_SEPARATOR = '\\';
#else
static constexpr char CRASHREPORT_DIRECTORY_SEPARATOR = '/';
#endif

const char*
CrashReportHelpers::GetFilename(
    const char* path)
{
    if (path == nullptr)
    {
        return nullptr;
    }

    const char* fileName = strrchr(path, CRASHREPORT_DIRECTORY_SEPARATOR);
    if (fileName != nullptr)
    {
        ++fileName;
        if (*fileName != '\0')
        {
            return fileName;
        }
    }

    return path;
}

void
CrashReportHelpers::CopyString(
    char* buffer,
    size_t bufferSize,
    const char* value)
{
    if (buffer == nullptr || bufferSize == 0)
    {
        return;
    }

    if (value == nullptr)
    {
        buffer[0] = '\0';
        return;
    }

    size_t toCopy = strnlen(value, bufferSize - 1);
    if (toCopy != 0)
    {
        memcpy(buffer, value, toCopy);
    }

    buffer[toCopy] = '\0';
}

void
CrashReportHelpers::JsonFrameCallback(
    uint64_t ip,
    uint64_t stackPointer,
    const char* methodName,
    const char* className,
    const char* moduleName,
    uint32_t nativeOffset,
    uint32_t token,
    uint32_t ilOffset,
    uint32_t moduleTimestamp,
    uint32_t moduleSize,
    const char* moduleGuid,
    void* ctx)
{
    SignalSafeJsonWriter* writer = reinterpret_cast<SignalSafeJsonWriter*>(ctx);
    if (writer == nullptr)
    {
        return;
    }

    WriteFrameToJson(writer, ip, stackPointer, methodName, className, moduleName,
        nativeOffset, token, ilOffset, moduleTimestamp, moduleSize, moduleGuid);
}

void
CrashReportHelpers::WriteFrameToJson(
    SignalSafeJsonWriter* writer,
    uint64_t ip,
    uint64_t stackPointer,
    const char* methodName,
    const char* className,
    const char* moduleName,
    uint32_t nativeOffset,
    uint32_t token,
    uint32_t ilOffset,
    uint32_t moduleTimestamp,
    uint32_t moduleSize,
    const char* moduleGuid)
{
    if (writer == nullptr)
    {
        return;
    }

    writer->OpenObject();
    writer->WriteHexAsString("stack_pointer", stackPointer);
    writer->WriteHexAsString("native_address", ip);
    writer->WriteHexAsString("native_offset", nativeOffset);

    if (methodName != nullptr)
    {
        char fullName[CRASHREPORT_STRING_BUFFER_SIZE];
        BuildMethodName(fullName, sizeof(fullName), className, methodName);
        writer->WriteString("method_name", fullName);
        writer->WriteString("is_managed", "true");
        writer->WriteHexAsString("token", token);
        writer->WriteHexAsString("il_offset", ilOffset);
        if (moduleName != nullptr)
        {
            writer->WriteString("filename", moduleName);
        }
        if (moduleTimestamp != 0)
        {
            writer->WriteHexAsString("timestamp", moduleTimestamp);
        }
        if (moduleSize != 0)
        {
            writer->WriteHexAsString("sizeofimage", moduleSize);
        }
        if (moduleGuid != nullptr && moduleGuid[0] != '\0')
        {
            writer->WriteString("guid", moduleGuid);
        }
    }
    else
    {
        writer->WriteString("is_managed", "false");
        if (moduleName != nullptr)
        {
            writer->WriteString("native_module", moduleName);
        }
    }

    writer->CloseObject(); // frame
}

void
CrashReportHelpers::WriteFrameToConsole(
    SignalSafeConsoleWriter* consoleWriter,
    uint32_t frameIndex,
    int moduleIndex,
    uint64_t ip,
    const char* methodName,
    const char* className,
    const char* moduleName,
    uint32_t nativeOffset,
    uint32_t token,
    uint32_t ilOffset)
{
    if (consoleWriter == nullptr)
    {
        return;
    }

    consoleWriter->AppendStr("  #");
    if (frameIndex < 10)
    {
        consoleWriter->AppendChar('0');
    }
    consoleWriter->AppendDecimal(static_cast<uint64_t>(frameIndex));
    consoleWriter->AppendChar(' ');

    if (moduleIndex >= 0)
    {
        consoleWriter->AppendChar('[');
        consoleWriter->AppendDecimal(static_cast<uint64_t>(moduleIndex));
        consoleWriter->AppendStr("] ");
    }
    else if (methodName != nullptr && moduleName != nullptr && moduleName[0] != '\0')
    {
        consoleWriter->AppendStr("(in ");
        consoleWriter->AppendStr(GetFilename(moduleName));
        consoleWriter->AppendStr(") ");
    }

    if (methodName != nullptr)
    {
        char fullName[CRASHREPORT_STRING_BUFFER_SIZE];
        BuildMethodName(fullName, sizeof(fullName), className, methodName);
        consoleWriter->AppendStr(fullName);
        consoleWriter->AppendStr(" + 0x");
        consoleWriter->AppendHex(static_cast<uint64_t>(ilOffset));
        consoleWriter->AppendStr(" (token=0x");
        consoleWriter->AppendHex(static_cast<uint64_t>(token));
        consoleWriter->AppendChar(')');
    }
    else
    {
        consoleWriter->AppendStr("0x");
        consoleWriter->AppendHex(ip);
        if (moduleName != nullptr && moduleName[0] != '\0')
        {
            consoleWriter->AppendStr(" (");
            consoleWriter->AppendStr(GetFilename(moduleName));
            consoleWriter->AppendStr(" + 0x");
            consoleWriter->AppendHex(static_cast<uint64_t>(nativeOffset));
            consoleWriter->AppendChar(')');
        }
    }
    consoleWriter->EndLine();
}

void
CrashReportHelpers::WriteThreadBlockHeaderToConsole(
    SignalSafeConsoleWriter* consoleWriter,
    uint64_t osThreadId,
    bool isCrashThread)
{
    if (consoleWriter == nullptr)
    {
        return;
    }

    consoleWriter->WriteBlank();
    consoleWriter->AppendStr("--- thread 0x");
    consoleWriter->AppendHex(osThreadId);
    if (isCrashThread)
    {
        consoleWriter->AppendStr(" (crashed)");
    }
    consoleWriter->AppendStr(" ---");
    consoleWriter->EndLine();
}

void
CrashReportHelpers::WriteThreadBlockCloserToConsole(
    SignalSafeConsoleWriter* consoleWriter,
    uint32_t frameCount)
{
    if (consoleWriter == nullptr)
    {
        return;
    }

    if (frameCount == 0)
    {
        consoleWriter->WriteLine("  (no managed frames)");
    }
}

void
CrashReportHelpers::FrameSinkCallback(
    uint64_t ip,
    uint64_t stackPointer,
    const char* methodName,
    const char* className,
    const char* moduleName,
    uint32_t nativeOffset,
    uint32_t token,
    uint32_t ilOffset,
    uint32_t moduleTimestamp,
    uint32_t moduleSize,
    const char* moduleGuid,
    void* ctx)
{
    FrameSinks* sinks = reinterpret_cast<FrameSinks*>(ctx);
    if (sinks == nullptr)
    {
        return;
    }

    uint32_t frameIndex = sinks->currentThreadFrameCount != nullptr
        ? *sinks->currentThreadFrameCount
        : 0;

    int moduleIndex = s_moduleTable.GetOrAddIndex(moduleName, moduleGuid);

    WriteFrameToJson(sinks->writer, ip, stackPointer, methodName, className, moduleName,
        nativeOffset, token, ilOffset, moduleTimestamp, moduleSize, moduleGuid);

    WriteFrameToConsole(sinks->consoleWriter, frameIndex, moduleIndex, ip, methodName, className, moduleName,
        nativeOffset, token, ilOffset);

    if (sinks->currentThreadFrameCount != nullptr)
    {
        ++*sinks->currentThreadFrameCount;
    }
}

void
ThreadEnumerationContext::OnFrame(
    uint64_t ip,
    uint64_t stackPointer,
    const char* methodName,
    const char* className,
    const char* moduleName,
    uint32_t nativeOffset,
    uint32_t token,
    uint32_t ilOffset,
    uint32_t moduleTimestamp,
    uint32_t moduleSize,
    const char* moduleGuid)
{
    CrashReportHelpers::FrameSinks sinks =
    {
        m_writer,
        m_consoleWriter,
        &m_currentThreadFrameCount,
    };
    CrashReportHelpers::FrameSinkCallback(ip, stackPointer, methodName, className, moduleName,
        nativeOffset, token, ilOffset, moduleTimestamp, moduleSize, moduleGuid, &sinks);
}

void
ThreadEnumerationContext::FrameCallback(
    uint64_t ip,
    uint64_t stackPointer,
    const char* methodName,
    const char* className,
    const char* moduleName,
    uint32_t nativeOffset,
    uint32_t token,
    uint32_t ilOffset,
    uint32_t moduleTimestamp,
    uint32_t moduleSize,
    const char* moduleGuid,
    void* ctx)
{
    if (ctx == nullptr)
    {
        return;
    }
    reinterpret_cast<ThreadEnumerationContext*>(ctx)->OnFrame(ip, stackPointer, methodName, className, moduleName, nativeOffset, token, ilOffset, moduleTimestamp, moduleSize, moduleGuid);
}

void
ThreadEnumerationContext::FinishCurrentThreadCompactBlock()
{
    if (m_threadCount == 0)
    {
        return;
    }

    CrashReportHelpers::WriteThreadBlockCloserToConsole(m_consoleWriter,
        m_currentThreadFrameCount);
}

void
ThreadEnumerationContext::OnThread(
    uint64_t osThreadId,
    bool isCrashThread,
    const char* exceptionType,
    uint32_t exceptionHResult)
{
    if (m_threadCount > 0)
    {
        FinishCurrentThreadCompactBlock();

        m_writer->CloseArray(); // stack_frames
        m_writer->CloseObject(); // thread

        (void)m_writer->Flush();
    }

    if (isCrashThread)
    {
        m_sawCrashThread = true;
    }
    m_threadCount++;
    m_currentThreadFrameCount = 0;

    m_writer->OpenObject();
    m_writer->WriteString("is_managed", "true");
    m_writer->WriteString("crashed", isCrashThread ? "true" : "false");
    m_writer->WriteHexAsString("native_thread_id", osThreadId);

    if (exceptionType != nullptr && exceptionType[0] != '\0')
    {
        m_writer->WriteString("managed_exception_type", exceptionType);
        m_writer->WriteHexAsString("managed_exception_hresult", exceptionHResult);
    }

    if (isCrashThread)
    {
        CrashReportHelpers::WriteRegistersToJson(m_writer, m_signalContext);
    }

    m_writer->OpenArray("stack_frames");
    if (isCrashThread)
    {
        CrashReportHelpers::WriteCrashSiteFrameToJson(m_writer, m_signalContext);
    }

    if (m_consoleWriter != nullptr)
    {
        CrashReportHelpers::WriteThreadBlockHeaderToConsole(m_consoleWriter, osThreadId, isCrashThread);

        if (exceptionType != nullptr && exceptionType[0] != '\0')
        {
            m_consoleWriter->AppendStr("  managed exception: ");
            m_consoleWriter->AppendStr(exceptionType);
            m_consoleWriter->AppendStr(" (0x");
            m_consoleWriter->AppendHex(static_cast<uint64_t>(exceptionHResult));
            m_consoleWriter->AppendChar(')');
            m_consoleWriter->EndLine();
        }
    }
}

void
ThreadEnumerationContext::ThreadCallback(
    uint64_t osThreadId,
    bool isCrashThread,
    const char* exceptionType,
    uint32_t exceptionHResult,
    void* ctx)
{
    if (ctx == nullptr)
    {
        return;
    }
    reinterpret_cast<ThreadEnumerationContext*>(ctx)->OnThread(osThreadId, isCrashThread, exceptionType, exceptionHResult);
}

void
ThreadEnumerationContext::EnumerateThreads(
    InProcCrashReportEnumerateThreadsCallback callback)
{
    if (callback == nullptr)
    {
        return;
    }

    callback(m_crashingTid, &ThreadCallback, &FrameCallback, this);

    if (m_threadCount == 0)
    {
        return;
    }

    FinishCurrentThreadCompactBlock();

    // Close the last thread's stack_frames + thread objects opened by OnThread.
    m_writer->CloseArray(); // stack_frames
    m_writer->CloseObject(); // thread

    // Flush the final thread so it reaches the crash report file even if any
    // later work (e.g. synthesizing a crash thread fallback) hangs or faults.
    (void)m_writer->Flush();
}

void
InProcCrashReporter::EmitSynthesizedCrashThread(
    void* context,
    bool walkStack)
{
    uint64_t crashingTid = static_cast<uint64_t>(minipal_get_current_thread_id());

    m_jsonWriter.OpenObject();
    m_jsonWriter.WriteString("is_managed",
        m_isManagedThreadCallback != nullptr && m_isManagedThreadCallback() ? "true" : "false");
    m_jsonWriter.WriteString("crashed", "true");
    m_jsonWriter.WriteHexAsString("native_thread_id", crashingTid);

    CrashReportHelpers::WriteRegistersToJson(&m_jsonWriter, context);
    m_jsonWriter.OpenArray("stack_frames");
    CrashReportHelpers::WriteCrashSiteFrameToJson(&m_jsonWriter, context);

    CrashReportHelpers::WriteThreadBlockHeaderToConsole(&s_consoleWriter, crashingTid, /*isCrashThread*/ true);

    uint32_t synthesizedFrameCount = 0;
    if (walkStack && m_walkStackCallback != nullptr)
    {
        CrashReportHelpers::FrameSinks sinks =
        {
            &m_jsonWriter,
            &s_consoleWriter,
            &synthesizedFrameCount,
        };
        m_walkStackCallback(&CrashReportHelpers::FrameSinkCallback, &sinks);
    }
    CrashReportHelpers::WriteThreadBlockCloserToConsole(&s_consoleWriter,
        synthesizedFrameCount);

    m_jsonWriter.CloseArray(); // stack_frames
    m_jsonWriter.CloseObject(); // thread
}

// --- InProcCrashReporter: console header and footer ------------------------

void
InProcCrashReporter::EmitConsoleHeader(int signal)
{
    s_consoleWriter.WriteSeparator();
    s_consoleWriter.AppendStr(".NET Crash Report v");
    s_consoleWriter.AppendStr(CRASHREPORT_PROTOCOL_VERSION);
    s_consoleWriter.EndLine();

    char version[sizeof(sccsid)];
    CrashReportHelpers::GetVersionString(version, sizeof(version));
    if (version[0] != '\0')
    {
        s_consoleWriter.WriteKeyValueStr("Build", version);
    }

    s_consoleWriter.WriteKeyValueStr("ABI", CRASHREPORT_ARCHITECTURE_NAME);

    if (m_processName[0] != '\0')
    {
        s_consoleWriter.WriteKeyValueStr("Cmdline", m_processName);
    }

    s_consoleWriter.WriteKeyValueDecimal("pid", static_cast<uint64_t>(GetCurrentProcessId()));

    s_consoleWriter.AppendStr("signal ");
    s_consoleWriter.AppendSignedDecimal(signal);
    s_consoleWriter.AppendStr(" (");
    s_consoleWriter.AppendStr(PROCGetSignalNameAscii(signal));
    s_consoleWriter.AppendChar(')');
    s_consoleWriter.EndLine();
}

void
InProcCrashReporter::EmitConsoleModulesAndFooter()
{
    if (s_moduleTable.Count() != 0)
    {
        s_consoleWriter.WriteBlank();
        s_consoleWriter.WriteLine("modules:");
        for (size_t i = 0; i < s_moduleTable.Count(); ++i)
        {
            s_consoleWriter.AppendStr("  [");
            s_consoleWriter.AppendDecimal(static_cast<uint64_t>(i));
            s_consoleWriter.AppendStr("] ");
            s_consoleWriter.AppendStr(CrashReportHelpers::GetFilename(s_moduleTable.Name(i)));
            s_consoleWriter.AppendChar(' ');
            s_consoleWriter.AppendStr(s_moduleTable.Guid(i));
            s_consoleWriter.EndLine();
        }
    }

    s_consoleWriter.WriteSeparator();
}

// --- InProcCrashReporter: JSON header and footer ---------------------------

void
InProcCrashReporter::EmitJsonHeader()
{
    m_jsonWriter.OpenObject();
    m_jsonWriter.OpenObject("payload");
    m_jsonWriter.WriteString("protocol_version", CRASHREPORT_PROTOCOL_VERSION);

    m_jsonWriter.OpenObject("configuration");
    m_jsonWriter.WriteString("architecture", CRASHREPORT_ARCHITECTURE_NAME);
    char version[sizeof(sccsid)];
    CrashReportHelpers::GetVersionString(version, sizeof(version));
    m_jsonWriter.WriteString("version", version);
    m_jsonWriter.CloseObject(); // configuration

    if (m_processName[0] != '\0')
    {
        m_jsonWriter.WriteString("process_name", m_processName);
    }

    m_jsonWriter.WriteDecimalAsString("pid", static_cast<uint64_t>(GetCurrentProcessId()));
}

void
InProcCrashReporter::EmitJsonFooter(int signal)
{
    m_jsonWriter.CloseObject(); // payload

    m_jsonWriter.OpenObject("parameters");
    m_jsonWriter.WriteSignedDecimalAsString("signal", static_cast<int64_t>(signal));
#ifdef __APPLE__
    if (m_osVersion[0] != '\0')
    {
        m_jsonWriter.WriteString("OSVersion", m_osVersion);
    }
    if (m_systemModel[0] != '\0')
    {
        m_jsonWriter.WriteString("SystemModel", m_systemModel);
    }
    m_jsonWriter.WriteString("SystemManufacturer", "apple");
#endif
    m_jsonWriter.CloseObject(); // parameters

    m_jsonWriter.CloseObject(); // root
}
