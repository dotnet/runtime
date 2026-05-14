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

static const char CRASHREPORT_PROTOCOL_VERSION[] = "1.0.0";
static constexpr uint32_t CRASHREPORT_COR_E_STACKOVERFLOW = 0x800703E9;
static const char CRASHREPORT_STACK_OVERFLOW_EXCEPTION_TYPE[] = "System.StackOverflowException";
static const char CRASHREPORT_STACK_OVERFLOW_TRACE_UNAVAILABLE_REASON[] = "stack_overflow_trace_unavailable";
static constexpr uint32_t CRASHREPORT_STACK_OVERFLOW_MAX_TRACE_FRAMES = 128;

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
//   *** *** *** *** *** *** *** *** *** *** *** *** *** *** *** ***   (BeginConsoleReport)
//   .NET Crash Report v<protocol>
//   Build: <sccsid>                                                    (omitted if empty)
//   ABI: amd64|arm64|arm
//   Cmdline: <process name>                                            (omitted if empty)
//   pid: <pid>
//   signal <N> (<NAME>)
//                                                                      (blank between sections)
//   --- thread 0xTID [(crashed)] ---                                   (BeginConsoleThreadBlock)
//     managed exception: <Type> (0x<HRESULT>)                          (only if EE provided one)
//     #NN [M] Class.Method + 0xILOFFSET (token=0xTOKEN)                (managed frame; WriteFrameToConsole)
//     #NN (in <name>) Class.Method + 0xILOFFSET (token=0xTOKEN)        (overflow form: module didn't fit the table)
//     #NN [M] 0xIP (module + 0xOFFSET)                                 (native frame; WriteFrameToConsole)
//     #NN 0xIP (module + 0xOFFSET)                                     (native frame not in module table)
//     (no managed frames) | ... +N more frames                         (EndConsoleThreadBlock)
//                                                                      (blank between threads)
//   modules:                                                           (EndConsoleReport)
//     [N] <name> {<MVID>}                                              (one per ModuleTable entry)
//   *** *** *** *** *** *** *** *** *** *** *** *** *** *** *** ***   (closing separator)

static SignalSafeConsoleWriter s_consoleWriter;
static volatile LONG s_crashKind = static_cast<LONG>(InProcCrashReportCrashKind::Unknown);

struct StackOverflowTraceFrame
{
    char methodName[CRASHREPORT_STRING_BUFFER_SIZE];
    uint32_t repeatCount;
    uint32_t repeatSequenceLength;
};

struct StackOverflowTraceSnapshot
{
    uint64_t crashingTid;
    uint32_t totalFrameCount;
    uint32_t frameCount;
    uint32_t truncatedFrameCount;
    StackOverflowTraceFrame frames[CRASHREPORT_STACK_OVERFLOW_MAX_TRACE_FRAMES];
    volatile LONG available;
};

static StackOverflowTraceSnapshot s_stackOverflowTrace;

// Include the .NET version string instead of linking because it is "static".
#if __has_include("_version.c")
#include "_version.c"
#else
static char sccsid[] = "@(#)Version N/A";
#endif

static char s_versionScratch[sizeof(sccsid)];
static char s_moduleGuidScratch[MINIPAL_GUID_BUFFER_LEN];

static const char*
GetSignalNameAscii(int signal)
{
    switch (signal)
    {
        case SIGSEGV: return "SIGSEGV";
        case SIGBUS:  return "SIGBUS";
        case SIGFPE:  return "SIGFPE";
        case SIGILL:  return "SIGILL";
        case SIGABRT: return "SIGABRT";
        case SIGTRAP: return "SIGTRAP";
        case SIGTERM: return "SIGTERM";
        default:      return "Unknown signal";
    }
}

static void CopyStringToBuffer(char* buffer, size_t bufferSize, const char* value)
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

// Bounded module table that deduplicates each unique module observed during a
// single crash report. Frames in the compact log refer to modules by short
// ``[N]`` indices instead of repeating module identity on every line; the
// matching ``modules:`` block resolves the module handles back to full data.
//
// Capacity is fixed at MAX_MODULES_IN_TABLE (no heap on the fatal-signal
// path). A managed frame whose module didn't fit (table full, missing handle,
// or unresolved module identity) renders the module identity inline as
// ``(in <name>) `` so the frame stays self-describing — overflow is lossless,
// just less compact for that frame.
//
// Single-instance because CreateReport is one-shot per process (guarded by
// the ``s_generating`` InterlockedCompareExchange in CreateReport).

static constexpr size_t MAX_MODULES_IN_TABLE = 256;

class ModuleTable
{
public:
    int GetOrAddIndex(
        const void* moduleHandle)
    {
        if (moduleHandle == nullptr)
        {
            return -1;
        }

        for (size_t i = 0; i < m_count; ++i)
        {
            if (m_moduleHandles[i] == moduleHandle)
            {
                return static_cast<int>(i);
            }
        }

        if (m_count >= MAX_MODULES_IN_TABLE)
        {
            return -1;
        }

        m_moduleHandles[m_count] = moduleHandle;
        return static_cast<int>(m_count++);
    }

    size_t Count() const { return m_count; }
    const void* ModuleHandle(size_t i) const { return m_moduleHandles[i]; }

private:
    const void* m_moduleHandles[MAX_MODULES_IN_TABLE];
    size_t m_count = 0;
};

static ModuleTable s_moduleTable;

class ThreadEnumerationContext
{
public:
    ThreadEnumerationContext()
    {
        Init(nullptr, nullptr, nullptr, 0, 0, nullptr);
    }

    ThreadEnumerationContext(
        SignalSafeJsonWriter* writer,
        SignalSafeConsoleWriter* consoleWriter,
        InProcCrashReportModuleInfoCallback moduleInfoCallback,
        uint64_t crashingTid,
        uint32_t frameLimitPerThread,
        void* signalContext)
    {
        Init(writer, consoleWriter, moduleInfoCallback, crashingTid, frameLimitPerThread, signalContext);
    }

    ThreadEnumerationContext(const ThreadEnumerationContext&) = delete;
    ThreadEnumerationContext& operator=(const ThreadEnumerationContext&) = delete;

    size_t ThreadCount() const { return m_threadCount; }
    bool SawCrashThread() const { return m_sawCrashThread; }
    SignalSafeJsonWriter* JsonWriter() const { return m_jsonWriter; }
    SignalSafeConsoleWriter* ConsoleWriter() const { return m_consoleWriter; }

    void Init(
        SignalSafeJsonWriter* writer,
        SignalSafeConsoleWriter* consoleWriter,
        InProcCrashReportModuleInfoCallback moduleInfoCallback,
        uint64_t crashingTid,
        uint32_t frameLimitPerThread,
        void* signalContext)
    {
        m_jsonWriter = writer;
        m_consoleWriter = consoleWriter;
        m_moduleInfoCallback = moduleInfoCallback;
        m_signalContext = signalContext;
        m_threadCount = 0;
        m_crashingTid = crashingTid;
        m_currentThreadFrameCount = 0;
        m_currentThreadDroppedCount = 0;
        m_frameLimitPerThread = frameLimitPerThread;
        m_sawCrashThread = false;
        m_methodNameScratch[0] = '\0';
    }

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
        const void* moduleHandle,
        uint32_t nativeOffset,
        uint32_t token,
        uint32_t ilOffset,
        uint32_t moduleTimestamp,
        uint32_t moduleSize,
        const GUID* moduleGuid,
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
        const void* moduleHandle,
        uint32_t nativeOffset,
        uint32_t token,
        uint32_t ilOffset,
        uint32_t moduleTimestamp,
        uint32_t moduleSize,
        const GUID* moduleGuid);

    void EndCurrentConsoleThreadBlock();
    void EndCurrentJsonThreadBlock();

    SignalSafeJsonWriter* m_jsonWriter;
    SignalSafeConsoleWriter* m_consoleWriter;
    InProcCrashReportModuleInfoCallback m_moduleInfoCallback;
    void* m_signalContext;
    size_t m_threadCount;
    uint64_t m_crashingTid;
    uint32_t m_currentThreadFrameCount;
    uint32_t m_currentThreadDroppedCount;
    uint32_t m_frameLimitPerThread;
    bool m_sawCrashThread;
    char m_methodNameScratch[CRASHREPORT_STRING_BUFFER_SIZE];
};

static ThreadEnumerationContext s_threadContext;

class CrashReportOutputContext
{
public:
    CrashReportOutputContext()
        : m_fd(-1),
          m_writeFailed(false)
    {
    }

    explicit CrashReportOutputContext(int fd)
    {
        Init(fd);
    }

    CrashReportOutputContext(const CrashReportOutputContext&) = delete;
    CrashReportOutputContext& operator=(const CrashReportOutputContext&) = delete;

    int Fd() const { return m_fd; }
    bool WriteFailed() const { return m_writeFailed; }

    void Init(int fd)
    {
        m_fd = fd;
        m_writeFailed = false;
    }

    static bool ChunkCallback(const char* buffer, size_t len, void* ctx);

private:
    bool HandleChunk(const char* buffer, size_t len);

    int m_fd;
    bool m_writeFailed;
};

static CrashReportOutputContext s_outputContext;

class CrashReportHelpers
{
public:
    struct FrameContext
    {
        SignalSafeJsonWriter* jsonWriter;
        SignalSafeConsoleWriter* consoleWriter;
        InProcCrashReportModuleInfoCallback moduleInfoCallback;
        uint32_t* currentThreadFrameCount;
        uint32_t* currentThreadDroppedCount;
        uint32_t frameLimitPerThread;
        char* methodNameBuffer;
        size_t methodNameBufferSize;
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

    static void WriteFrameToJson(
        SignalSafeJsonWriter* writer,
        char* methodNameBuffer,
        size_t methodNameBufferSize,
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
        const GUID* moduleGuid);

    static void WriteFrameToConsole(
        SignalSafeConsoleWriter* consoleWriter,
        char* methodNameBuffer,
        size_t methodNameBufferSize,
        uint32_t frameIndex,
        int moduleIndex,
        uint64_t ip,
        const char* methodName,
        const char* className,
        const char* fallbackModuleName,
        uint32_t nativeOffset,
        uint32_t token,
        uint32_t ilOffset);

    static void WriteStackOverflowFrameToJson(
        SignalSafeJsonWriter* writer,
        const StackOverflowTraceFrame& frame,
        bool includeRepeatMetadata);

    static void WriteStackOverflowFrameToConsole(
        SignalSafeConsoleWriter* consoleWriter,
        uint32_t frameIndex,
        const StackOverflowTraceFrame& frame);

    static void BeginJsonThreadBlock(
        SignalSafeJsonWriter* jsonWriter,
        uint64_t osThreadId,
        bool isManagedThread,
        bool isCrashThread,
        const char* exceptionType,
        uint32_t exceptionHResult);

    static void BeginJsonStackFrames(
        SignalSafeJsonWriter* jsonWriter,
        bool writeCrashSiteFrame,
        void* signalContext);

    static void EndJsonStackFrames(
        SignalSafeJsonWriter* jsonWriter);

    static void EndJsonThreadBlock(
        SignalSafeJsonWriter* jsonWriter);

    static void BeginConsoleThreadBlock(
        SignalSafeConsoleWriter* consoleWriter,
        uint64_t osThreadId,
        bool isCrashThread);

    static void EndConsoleThreadBlock(
        SignalSafeConsoleWriter* consoleWriter,
        uint32_t frameCount,
        uint32_t droppedCount);

    static void WriteFrame(
        uint64_t ip,
        uint64_t stackPointer,
        const char* methodName,
        const char* className,
        const char* moduleName,
        const void* moduleHandle,
        uint32_t nativeOffset,
        uint32_t token,
        uint32_t ilOffset,
        uint32_t moduleTimestamp,
        uint32_t moduleSize,
        const GUID* moduleGuid,
        void* ctx);

    static void WriteFrameToReport(
        SignalSafeJsonWriter* jsonWriter,
        SignalSafeConsoleWriter* consoleWriter,
        InProcCrashReportModuleInfoCallback moduleInfoCallback,
        char* methodNameBuffer,
        size_t methodNameBufferSize,
        uint32_t* currentThreadFrameCount,
        uint32_t* currentThreadDroppedCount,
        uint32_t frameLimitPerThread,
        uint64_t ip,
        uint64_t stackPointer,
        const char* methodName,
        const char* className,
        const char* moduleName,
        const void* moduleHandle,
        uint32_t nativeOffset,
        uint32_t token,
        uint32_t ilOffset,
        uint32_t moduleTimestamp,
        uint32_t moduleSize,
        const GUID* moduleGuid);

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
        char* expandedBuffer,
        size_t expandedBufferSize,
        char* numberBuffer,
        size_t numberBufferSize,
        const char* dumpPath,
        const char* processName,
        const char* hostName);

    static size_t ExpandDumpTemplate(
        char* buffer,
        size_t bufferSize,
        char* numberBuffer,
        size_t numberBufferSize,
        const char* pattern,
        const char* processName,
        const char* hostName);
};

void
InProcCrashReporter::CreateReport(
    int signal,
    void* context)
{
    static LONG s_generating = 0;
    if (InterlockedCompareExchange(&s_generating, 1, 0) != 0)
    {
        return;
    }

    m_reportFilePathScratch[0] = '\0';

    // The JSON file sink is only enabled when DbgMiniDumpName supplied a
    // template AND the template expanded to a valid path. Otherwise the
    // crash report runs in compact-log-only mode: the JSON emitter still
    // executes (so it can keep its bookkeeping consistent) but writes go
    // to a no-op DiscardOutputCallback instead of an open fd.
    bool jsonEnabled = m_reportPath[0] != '\0' &&
        CrashReportHelpers::BuildReportPath(
            m_reportFilePathScratch,
            sizeof(m_reportFilePathScratch),
            m_expandedReportPathScratch,
            sizeof(m_expandedReportPathScratch),
            m_numberScratch,
            sizeof(m_numberScratch),
            m_reportPath,
            m_processName,
            m_hostName);

    int fd = -1;
    if (jsonEnabled)
    {
        fd = open(m_reportFilePathScratch, O_WRONLY | O_CREAT | O_TRUNC, 0600);
        if (fd == -1)
        {
            jsonEnabled = false;
        }
    }

    InProcCrashReportCrashKind crashKind = static_cast<InProcCrashReportCrashKind>(
        InterlockedExchange(&s_crashKind, static_cast<LONG>(InProcCrashReportCrashKind::Unknown)));

    s_outputContext.Init(fd);
    if (jsonEnabled)
    {
        m_jsonWriter.Init(&CrashReportOutputContext::ChunkCallback, &s_outputContext);
    }
    else
    {
        m_jsonWriter.Init(&CrashReportHelpers::DiscardOutputCallback, nullptr);
    }

    BeginConsoleReport(signal);
    BeginJsonReport();
    EmitThreads(crashKind, context);
    EndJsonReport(signal, jsonEnabled, fd);
    EndConsoleReport();
}

void
InProcCrashReporter::EmitThreads(
    InProcCrashReportCrashKind crashKind,
    void* context)
{
    m_jsonWriter.OpenArray("threads");
    if (crashKind == InProcCrashReportCrashKind::StackOverflow)
    {
        EmitStackOverflowCrashThread();
    }
    else if (m_enumerateThreadsCallback != nullptr)
    {
        uint64_t crashingTid = static_cast<uint64_t>(minipal_get_current_thread_id());
        s_threadContext.Init(&m_jsonWriter, &s_consoleWriter, m_moduleInfoCallback, crashingTid, m_frameLimitPerThread, context);

        s_threadContext.EnumerateThreads(m_enumerateThreadsCallback);

        if (s_threadContext.ThreadCount() == 0 || !s_threadContext.SawCrashThread())
        {
            EmitSynthesizedCrashThread(context, /*walkStack*/ false);
        }
    }
    else
    {
        EmitSynthesizedCrashThread(context, /*walkStack*/ true);
    }
    m_jsonWriter.CloseArray(); // threads
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
    m_moduleInfoCallback = settings.moduleInfoCallback;
    m_frameLimitPerThread = settings.frameLimitPerThread;
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
        ssize_t n = read(cmdlineFd, m_processNameScratch, sizeof(m_processNameScratch) - 1);
        close(cmdlineFd);
        if (n > 0)
        {
            m_processNameScratch[n] = '\0';
            CrashReportHelpers::CopyString(m_processName, sizeof(m_processName), CrashReportHelpers::GetFilename(m_processNameScratch));
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
    (void)siginfo;

    InProcCrashReporter& reporter = InProcCrashReporter::GetInstance();
    reporter.CreateReport(signal, context);
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

void
InProcCrashReportSetCrashKind(InProcCrashReportCrashKind crashKind)
{
    InterlockedExchange(&s_crashKind, static_cast<LONG>(crashKind));
}

void
InProcCrashReportBeginStackOverflowTrace(
    uint64_t crashingTid,
    uint32_t totalFrameCount)
{
    InterlockedExchange(&s_stackOverflowTrace.available, 0);
    s_stackOverflowTrace.crashingTid = crashingTid;
    s_stackOverflowTrace.totalFrameCount = totalFrameCount;
    s_stackOverflowTrace.frameCount = 0;
    s_stackOverflowTrace.truncatedFrameCount = 0;
}

void
InProcCrashReportAddStackOverflowTraceFrame(
    const char* methodName,
    uint32_t repeatCount,
    uint32_t repeatSequenceLength)
{
    if (s_stackOverflowTrace.frameCount >= CRASHREPORT_STACK_OVERFLOW_MAX_TRACE_FRAMES)
    {
        ++s_stackOverflowTrace.truncatedFrameCount;
        return;
    }

    StackOverflowTraceFrame& frame = s_stackOverflowTrace.frames[s_stackOverflowTrace.frameCount++];
    CopyStringToBuffer(frame.methodName, sizeof(frame.methodName), methodName);
    frame.repeatCount = repeatCount;
    frame.repeatSequenceLength = repeatSequenceLength;
}

void
InProcCrashReportCompleteStackOverflowTrace(uint32_t truncatedFrameCount)
{
    s_stackOverflowTrace.truncatedFrameCount += truncatedFrameCount;
    InterlockedExchange(&s_stackOverflowTrace.available, 1);
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
    char* numberBuffer,
    size_t numberBufferSize,
    const char* pattern,
    const char* processName,
    const char* hostName)
{
    if (buffer == nullptr || bufferSize == 0 ||
        numberBuffer == nullptr || numberBufferSize == 0 ||
        pattern == nullptr)
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
        numberBuffer[0] = '\0';

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
                if (SignalSafeFormat::FormatUnsignedDecimal(numberBuffer, numberBufferSize, pid) == 0)
                {
                    return 0;
                }
                substitution = numberBuffer;
                break;

            case 'e':
                substitution = (processName != nullptr && processName[0] != '\0') ? processName : nullptr;
                break;

            case 'h':
                substitution = (hostName != nullptr && hostName[0] != '\0') ? hostName : nullptr;
                break;

            case 't':
                if (SignalSafeFormat::FormatUnsignedDecimal(
                        numberBuffer, numberBufferSize, static_cast<uint64_t>(time(nullptr))) == 0)
                {
                    return 0;
                }
                substitution = numberBuffer;
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
    char* expandedBuffer,
    size_t expandedBufferSize,
    char* numberBuffer,
    size_t numberBufferSize,
    const char* dumpPath,
    const char* processName,
    const char* hostName)
{
    if (buffer == nullptr || bufferSize == 0 ||
        expandedBuffer == nullptr || expandedBufferSize == 0 ||
        numberBuffer == nullptr || numberBufferSize == 0 ||
        dumpPath == nullptr || dumpPath[0] == '\0')
    {
        return false;
    }

    size_t expandedLen = ExpandDumpTemplate(
        expandedBuffer,
        expandedBufferSize,
        numberBuffer,
        numberBufferSize,
        dumpPath,
        processName,
        hostName);
    if (expandedLen == 0)
    {
        return false;
    }

    size_t pos = 0;
    if (!AppendString(buffer, bufferSize, &pos, expandedBuffer))
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

static bool
HasModuleName(const char* moduleName)
{
    return moduleName != nullptr && moduleName[0] != '\0';
}

static bool
HasManagedIdentity(
    const char* methodName,
    const char* moduleName,
    uint32_t token)
{
    return methodName != nullptr ||
        (token != 0 && HasModuleName(moduleName));
}

void
CrashReportHelpers::CopyString(
    char* buffer,
    size_t bufferSize,
    const char* value)
{
    CopyStringToBuffer(buffer, bufferSize, value);
}

void
CrashReportHelpers::WriteFrameToJson(
    SignalSafeJsonWriter* writer,
    char* methodNameBuffer,
    size_t methodNameBufferSize,
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
    const GUID* moduleGuid)
{
    if (writer == nullptr)
    {
        return;
    }

    writer->OpenObject();
    writer->WriteHexAsString("stack_pointer", stackPointer);
    writer->WriteHexAsString("native_address", ip);
    writer->WriteHexAsString("native_offset", nativeOffset);

    if (HasManagedIdentity(methodName, moduleName, token))
    {
        writer->WriteString("is_managed", "true");
        if (methodName != nullptr)
        {
            const char* fullMethodName = methodName;
            if (methodNameBuffer != nullptr && methodNameBufferSize != 0)
            {
                BuildMethodName(methodNameBuffer, methodNameBufferSize, className, methodName);
                fullMethodName = methodNameBuffer;
            }
            writer->WriteString("method_name", fullMethodName);
        }
        if (methodName != nullptr || token != 0)
        {
            writer->WriteHexAsString("token", token);
            writer->WriteHexAsString("il_offset", ilOffset);
        }
        if (HasModuleName(moduleName))
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
        if (moduleGuid != nullptr)
        {
            minipal_guid_as_string(*moduleGuid, s_moduleGuidScratch, sizeof(s_moduleGuidScratch));
            writer->WriteString("guid", s_moduleGuidScratch);
        }
    }
    else
    {
        writer->WriteString("is_managed", "false");
        if (HasModuleName(moduleName))
        {
            writer->WriteString("native_module", moduleName);
        }
    }

    writer->CloseObject(); // frame
}

void
CrashReportHelpers::WriteFrameToConsole(
    SignalSafeConsoleWriter* consoleWriter,
    char* methodNameBuffer,
    size_t methodNameBufferSize,
    uint32_t frameIndex,
    int moduleIndex,
    uint64_t ip,
    const char* methodName,
    const char* className,
    const char* fallbackModuleName,
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
    else if ((methodName != nullptr || (token != 0 && HasModuleName(fallbackModuleName))) && HasModuleName(fallbackModuleName))
    {
        consoleWriter->AppendStr("(in ");
        consoleWriter->AppendStr(GetFilename(fallbackModuleName));
        consoleWriter->AppendStr(") ");
    }

    if (methodName != nullptr)
    {
        const char* fullMethodName = methodName;
        if (methodNameBuffer != nullptr && methodNameBufferSize != 0)
        {
            BuildMethodName(methodNameBuffer, methodNameBufferSize, className, methodName);
            fullMethodName = methodNameBuffer;
        }
        consoleWriter->AppendStr(fullMethodName);
        consoleWriter->AppendStr(" + 0x");
        consoleWriter->AppendHex(static_cast<uint64_t>(ilOffset));
        consoleWriter->AppendStr(" (token=0x");
        consoleWriter->AppendHex(static_cast<uint64_t>(token));
        consoleWriter->AppendChar(')');
    }
    else if (token != 0 && HasModuleName(fallbackModuleName))
    {
        consoleWriter->AppendStr("token=0x");
        consoleWriter->AppendHex(static_cast<uint64_t>(token));
        consoleWriter->AppendStr(" + 0x");
        consoleWriter->AppendHex(static_cast<uint64_t>(ilOffset));
    }
    else
    {
        consoleWriter->AppendStr("0x");
        consoleWriter->AppendHex(ip);
        if (HasModuleName(fallbackModuleName))
        {
            consoleWriter->AppendStr(" (");
            consoleWriter->AppendStr(GetFilename(fallbackModuleName));
            consoleWriter->AppendStr(" + 0x");
            consoleWriter->AppendHex(static_cast<uint64_t>(nativeOffset));
            consoleWriter->AppendChar(')');
        }
    }
    consoleWriter->EndLine();
}

void
CrashReportHelpers::WriteStackOverflowFrameToJson(
    SignalSafeJsonWriter* writer,
    const StackOverflowTraceFrame& frame,
    bool includeRepeatMetadata)
{
    if (writer == nullptr)
    {
        return;
    }

    writer->OpenObject();
    writer->WriteString("method_name", frame.methodName);
    writer->WriteString("is_managed", "true");
    if (includeRepeatMetadata)
    {
        writer->WriteDecimalAsString("stack_overflow_repeat_count", frame.repeatCount);
        writer->WriteDecimalAsString("stack_overflow_repeat_sequence_length", frame.repeatSequenceLength);
    }
    writer->CloseObject(); // frame
}

void
CrashReportHelpers::WriteStackOverflowFrameToConsole(
    SignalSafeConsoleWriter* consoleWriter,
    uint32_t frameIndex,
    const StackOverflowTraceFrame& frame)
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
    consoleWriter->AppendStr(frame.methodName);
    consoleWriter->EndLine();
}

void
CrashReportHelpers::BeginJsonThreadBlock(
    SignalSafeJsonWriter* jsonWriter,
    uint64_t osThreadId,
    bool isManagedThread,
    bool isCrashThread,
    const char* exceptionType,
    uint32_t exceptionHResult)
{
    if (jsonWriter == nullptr)
    {
        return;
    }

    jsonWriter->OpenObject();
    jsonWriter->WriteString("is_managed", isManagedThread ? "true" : "false");
    jsonWriter->WriteString("crashed", isCrashThread ? "true" : "false");
    jsonWriter->WriteHexAsString("native_thread_id", osThreadId);

    if (exceptionType != nullptr && exceptionType[0] != '\0')
    {
        jsonWriter->WriteString("managed_exception_type", exceptionType);
        jsonWriter->WriteHexAsString("managed_exception_hresult", exceptionHResult);
    }
}

void
CrashReportHelpers::BeginJsonStackFrames(
    SignalSafeJsonWriter* jsonWriter,
    bool writeCrashSiteFrame,
    void* signalContext)
{
    if (jsonWriter == nullptr)
    {
        return;
    }

    jsonWriter->OpenArray("stack_frames");
    if (writeCrashSiteFrame)
    {
        WriteCrashSiteFrameToJson(jsonWriter, signalContext);
    }
}

void
CrashReportHelpers::EndJsonStackFrames(
    SignalSafeJsonWriter* jsonWriter)
{
    if (jsonWriter == nullptr)
    {
        return;
    }

    jsonWriter->CloseArray(); // stack_frames
}

void
CrashReportHelpers::EndJsonThreadBlock(
    SignalSafeJsonWriter* jsonWriter)
{
    if (jsonWriter == nullptr)
    {
        return;
    }

    jsonWriter->CloseObject(); // thread
}

void
CrashReportHelpers::BeginConsoleThreadBlock(
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
CrashReportHelpers::EndConsoleThreadBlock(
    SignalSafeConsoleWriter* consoleWriter,
    uint32_t frameCount,
    uint32_t droppedCount)
{
    if (consoleWriter == nullptr)
    {
        return;
    }

    if (frameCount == 0)
    {
        consoleWriter->WriteLine("  (no managed frames)");
    }
    else if (droppedCount != 0)
    {
        consoleWriter->AppendStr("  ... +");
        consoleWriter->AppendDecimal(static_cast<uint64_t>(droppedCount));
        consoleWriter->AppendStr(" more frames");
        consoleWriter->EndLine();
    }
}

void
CrashReportHelpers::WriteFrame(
    uint64_t ip,
    uint64_t stackPointer,
    const char* methodName,
    const char* className,
    const char* moduleName,
    const void* moduleHandle,
    uint32_t nativeOffset,
    uint32_t token,
    uint32_t ilOffset,
    uint32_t moduleTimestamp,
    uint32_t moduleSize,
    const GUID* moduleGuid,
    void* ctx)
{
    FrameContext* frameContext = reinterpret_cast<FrameContext*>(ctx);
    if (frameContext == nullptr)
    {
        return;
    }

    WriteFrameToReport(
        frameContext->jsonWriter,
        frameContext->consoleWriter,
        frameContext->moduleInfoCallback,
        frameContext->methodNameBuffer,
        frameContext->methodNameBufferSize,
        frameContext->currentThreadFrameCount,
        frameContext->currentThreadDroppedCount,
        frameContext->frameLimitPerThread,
        ip,
        stackPointer,
        methodName,
        className,
        moduleName,
        moduleHandle,
        nativeOffset,
        token,
        ilOffset,
        moduleTimestamp,
        moduleSize,
        moduleGuid);
}

void
CrashReportHelpers::WriteFrameToReport(
    SignalSafeJsonWriter* jsonWriter,
    SignalSafeConsoleWriter* consoleWriter,
    InProcCrashReportModuleInfoCallback moduleInfoCallback,
    char* methodNameBuffer,
    size_t methodNameBufferSize,
    uint32_t* currentThreadFrameCount,
    uint32_t* currentThreadDroppedCount,
    uint32_t frameLimitPerThread,
    uint64_t ip,
    uint64_t stackPointer,
    const char* methodName,
    const char* className,
    const char* moduleName,
    const void* moduleHandle,
    uint32_t nativeOffset,
    uint32_t token,
    uint32_t ilOffset,
    uint32_t moduleTimestamp,
    uint32_t moduleSize,
    const GUID* moduleGuid)
{
    uint32_t frameIndex = currentThreadFrameCount != nullptr
        ? *currentThreadFrameCount
        : 0;

    // Always feed the JSON sink: the file output is the authoritative,
    // post-mortem data store and the cap is a compact-log triage knob.
    WriteFrameToJson(jsonWriter,
        methodNameBuffer,
        methodNameBufferSize,
        ip, stackPointer, methodName, className, moduleName,
        nativeOffset, token, ilOffset, moduleTimestamp, moduleSize, moduleGuid);

    bool consoleCapped = frameLimitPerThread != 0 &&
        frameIndex >= frameLimitPerThread;
    if (!consoleCapped)
    {
        int moduleIndex = moduleInfoCallback != nullptr && moduleHandle != nullptr
            ? s_moduleTable.GetOrAddIndex(moduleHandle)
            : -1;
        WriteFrameToConsole(consoleWriter,
            methodNameBuffer,
            methodNameBufferSize,
            frameIndex, moduleIndex, ip, methodName, className, moduleName,
            nativeOffset, token, ilOffset);
    }
    else if (currentThreadDroppedCount != nullptr)
    {
        (*currentThreadDroppedCount)++;
    }

    if (currentThreadFrameCount != nullptr)
    {
        (*currentThreadFrameCount)++;
    }
}

void
ThreadEnumerationContext::OnFrame(
    uint64_t ip,
    uint64_t stackPointer,
    const char* methodName,
    const char* className,
    const char* moduleName,
    const void* moduleHandle,
    uint32_t nativeOffset,
    uint32_t token,
    uint32_t ilOffset,
    uint32_t moduleTimestamp,
    uint32_t moduleSize,
    const GUID* moduleGuid)
{
    CrashReportHelpers::WriteFrameToReport(
        m_jsonWriter,
        m_consoleWriter,
        m_moduleInfoCallback,
        m_methodNameScratch,
        sizeof(m_methodNameScratch),
        &m_currentThreadFrameCount,
        &m_currentThreadDroppedCount,
        m_frameLimitPerThread,
        ip,
        stackPointer,
        methodName,
        className,
        moduleName,
        moduleHandle,
        nativeOffset,
        token,
        ilOffset,
        moduleTimestamp,
        moduleSize,
        moduleGuid);
}

void
ThreadEnumerationContext::FrameCallback(
    uint64_t ip,
    uint64_t stackPointer,
    const char* methodName,
    const char* className,
    const char* moduleName,
    const void* moduleHandle,
    uint32_t nativeOffset,
    uint32_t token,
    uint32_t ilOffset,
    uint32_t moduleTimestamp,
    uint32_t moduleSize,
    const GUID* moduleGuid,
    void* ctx)
{
    if (ctx == nullptr)
    {
        return;
    }
    reinterpret_cast<ThreadEnumerationContext*>(ctx)->OnFrame(ip, stackPointer, methodName, className, moduleName, moduleHandle, nativeOffset, token, ilOffset, moduleTimestamp, moduleSize, moduleGuid);
}

void
ThreadEnumerationContext::EndCurrentConsoleThreadBlock()
{
    if (m_threadCount == 0)
    {
        return;
    }

    CrashReportHelpers::EndConsoleThreadBlock(m_consoleWriter,
        m_currentThreadFrameCount, m_currentThreadDroppedCount);
}

void
ThreadEnumerationContext::EndCurrentJsonThreadBlock()
{
    if (m_threadCount == 0)
    {
        return;
    }

    CrashReportHelpers::EndJsonStackFrames(m_jsonWriter);
    CrashReportHelpers::EndJsonThreadBlock(m_jsonWriter);

    (void)m_jsonWriter->Flush();
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
        EndCurrentConsoleThreadBlock();
        EndCurrentJsonThreadBlock();
    }

    if (isCrashThread)
    {
        m_sawCrashThread = true;
    }
    m_threadCount++;
    m_currentThreadFrameCount = 0;
    m_currentThreadDroppedCount = 0;

    CrashReportHelpers::BeginJsonThreadBlock(m_jsonWriter,
        osThreadId, /*isManagedThread*/ true, isCrashThread, exceptionType, exceptionHResult);

    if (isCrashThread)
    {
        CrashReportHelpers::WriteRegistersToJson(m_jsonWriter, m_signalContext);
    }

    CrashReportHelpers::BeginJsonStackFrames(m_jsonWriter, isCrashThread, m_signalContext);

    if (m_consoleWriter != nullptr)
    {
        CrashReportHelpers::BeginConsoleThreadBlock(m_consoleWriter, osThreadId, isCrashThread);

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

    EndCurrentConsoleThreadBlock();
    EndCurrentJsonThreadBlock();
}

void
InProcCrashReporter::EmitSynthesizedCrashThread(
    void* context,
    bool walkStack)
{
    uint64_t crashingTid = static_cast<uint64_t>(minipal_get_current_thread_id());

    bool isManagedThread = m_isManagedThreadCallback != nullptr && m_isManagedThreadCallback();
    CrashReportHelpers::BeginJsonThreadBlock(&m_jsonWriter,
        crashingTid, isManagedThread, /*isCrashThread*/ true, nullptr, 0);

    CrashReportHelpers::WriteRegistersToJson(&m_jsonWriter, context);
    CrashReportHelpers::BeginJsonStackFrames(&m_jsonWriter, /*writeCrashSiteFrame*/ true, context);

    CrashReportHelpers::BeginConsoleThreadBlock(&s_consoleWriter, crashingTid, /*isCrashThread*/ true);

    uint32_t synthesizedFrameCount = 0;
    uint32_t synthesizedDroppedCount = 0;
    if (walkStack && m_walkStackCallback != nullptr)
    {
        CrashReportHelpers::FrameContext frameContext =
        {
            &m_jsonWriter,
            &s_consoleWriter,
            m_moduleInfoCallback,
            &synthesizedFrameCount,
            &synthesizedDroppedCount,
            m_frameLimitPerThread,
            m_methodNameScratch,
            sizeof(m_methodNameScratch),
        };
        m_walkStackCallback(&CrashReportHelpers::WriteFrame, &frameContext);
    }
    CrashReportHelpers::EndConsoleThreadBlock(&s_consoleWriter,
        synthesizedFrameCount, synthesizedDroppedCount);

    CrashReportHelpers::EndJsonStackFrames(&m_jsonWriter);
    CrashReportHelpers::EndJsonThreadBlock(&m_jsonWriter);
}

void
InProcCrashReporter::EmitStackOverflowCrashThread()
{
    bool stackOverflowTraceAvailable = s_stackOverflowTrace.available != 0;
    uint64_t crashingTid = stackOverflowTraceAvailable && s_stackOverflowTrace.crashingTid != 0
        ? s_stackOverflowTrace.crashingTid
        : static_cast<uint64_t>(minipal_get_current_thread_id());

    CrashReportHelpers::BeginJsonThreadBlock(&m_jsonWriter,
        crashingTid,
        /*isManagedThread*/ true,
        /*isCrashThread*/ true,
        CRASHREPORT_STACK_OVERFLOW_EXCEPTION_TYPE,
        CRASHREPORT_COR_E_STACKOVERFLOW);
    if (stackOverflowTraceAvailable)
    {
        m_jsonWriter.WriteDecimalAsString("stack_overflow_total_frames", s_stackOverflowTrace.totalFrameCount);
        if (s_stackOverflowTrace.truncatedFrameCount != 0)
        {
            m_jsonWriter.WriteDecimalAsString("stack_overflow_trace_truncated_frames", s_stackOverflowTrace.truncatedFrameCount);
        }
    }
    else
    {
        m_jsonWriter.WriteString("stack_frames_unavailable_reason", CRASHREPORT_STACK_OVERFLOW_TRACE_UNAVAILABLE_REASON);
    }

    CrashReportHelpers::BeginJsonStackFrames(&m_jsonWriter, /*writeCrashSiteFrame*/ false, nullptr);
    if (stackOverflowTraceAvailable)
    {
        for (uint32_t i = 0; i < s_stackOverflowTrace.frameCount;)
        {
            StackOverflowTraceFrame& frame = s_stackOverflowTrace.frames[i];
            uint32_t repeatSequenceLength = frame.repeatSequenceLength;
            bool isRepeatSequence = frame.repeatCount > 1 && repeatSequenceLength != 0;
            CrashReportHelpers::WriteStackOverflowFrameToJson(
                &m_jsonWriter, frame, isRepeatSequence);
            ++i;

            if (!isRepeatSequence)
            {
                continue;
            }

            uint32_t sequenceEnd = i + repeatSequenceLength - 1;
            if (sequenceEnd > s_stackOverflowTrace.frameCount)
            {
                sequenceEnd = s_stackOverflowTrace.frameCount;
            }

            for (; i < sequenceEnd; ++i)
            {
                CrashReportHelpers::WriteStackOverflowFrameToJson(
                    &m_jsonWriter, s_stackOverflowTrace.frames[i], false);
            }
        }
    }
    CrashReportHelpers::EndJsonStackFrames(&m_jsonWriter);
    CrashReportHelpers::EndJsonThreadBlock(&m_jsonWriter);

    CrashReportHelpers::BeginConsoleThreadBlock(&s_consoleWriter, crashingTid, /*isCrashThread*/ true);
    s_consoleWriter.AppendStr("  managed exception: ");
    s_consoleWriter.AppendStr(CRASHREPORT_STACK_OVERFLOW_EXCEPTION_TYPE);
    s_consoleWriter.AppendStr(" (0x");
    s_consoleWriter.AppendHex(static_cast<uint64_t>(CRASHREPORT_COR_E_STACKOVERFLOW));
    s_consoleWriter.AppendChar(')');
    s_consoleWriter.EndLine();

    if (!stackOverflowTraceAvailable)
    {
        s_consoleWriter.WriteLine("  stack overflow trace unavailable");
        CrashReportHelpers::EndConsoleThreadBlock(&s_consoleWriter, 0, 0);
        return;
    }

    s_consoleWriter.AppendStr("  stack overflow frames: ");
    s_consoleWriter.AppendDecimal(static_cast<uint64_t>(s_stackOverflowTrace.totalFrameCount));
    s_consoleWriter.EndLine();

    uint32_t consoleFrameCount = 0;
    uint32_t consoleDroppedCount = s_stackOverflowTrace.truncatedFrameCount;
    for (uint32_t i = 0; i < s_stackOverflowTrace.frameCount;)
    {
        StackOverflowTraceFrame& frame = s_stackOverflowTrace.frames[i];
        uint32_t repeatSequenceLength = frame.repeatSequenceLength;
        if (frame.repeatCount > 1 && repeatSequenceLength != 0)
        {
            uint32_t sequenceEnd = i + repeatSequenceLength;
            if (sequenceEnd > s_stackOverflowTrace.frameCount)
            {
                sequenceEnd = s_stackOverflowTrace.frameCount;
            }

            if (m_frameLimitPerThread != 0 && consoleFrameCount >= m_frameLimitPerThread)
            {
                consoleDroppedCount += sequenceEnd - i;
                i = sequenceEnd;
                continue;
            }

            s_consoleWriter.AppendStr("  repeated ");
            s_consoleWriter.AppendDecimal(static_cast<uint64_t>(frame.repeatCount));
            s_consoleWriter.AppendStr(" times:");
            s_consoleWriter.EndLine();

            for (; i < sequenceEnd; ++i)
            {
                if (m_frameLimitPerThread != 0 && consoleFrameCount >= m_frameLimitPerThread)
                {
                    consoleDroppedCount++;
                    continue;
                }

                CrashReportHelpers::WriteStackOverflowFrameToConsole(
                    &s_consoleWriter, consoleFrameCount, s_stackOverflowTrace.frames[i]);
                consoleFrameCount++;
            }

            continue;
        }

        if (m_frameLimitPerThread != 0 && consoleFrameCount >= m_frameLimitPerThread)
        {
            consoleDroppedCount++;
        }
        else
        {
            CrashReportHelpers::WriteStackOverflowFrameToConsole(&s_consoleWriter, consoleFrameCount, frame);
            consoleFrameCount++;
        }
        ++i;
    }

    CrashReportHelpers::EndConsoleThreadBlock(&s_consoleWriter,
        consoleFrameCount, consoleDroppedCount);
}

// --- InProcCrashReporter: console report lifecycle -------------------------

void
InProcCrashReporter::BeginConsoleReport(int signal)
{
    s_consoleWriter.WriteSeparator();
    s_consoleWriter.AppendStr(".NET Crash Report v");
    s_consoleWriter.AppendStr(CRASHREPORT_PROTOCOL_VERSION);
    s_consoleWriter.EndLine();

    CrashReportHelpers::GetVersionString(s_versionScratch, sizeof(s_versionScratch));
    if (s_versionScratch[0] != '\0')
    {
        s_consoleWriter.WriteKeyValueStr("Build", s_versionScratch);
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
    s_consoleWriter.AppendStr(GetSignalNameAscii(signal));
    s_consoleWriter.AppendChar(')');
    s_consoleWriter.EndLine();
}

void
InProcCrashReporter::EndConsoleReport()
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
            const char* moduleName = nullptr;
            GUID moduleGuid;
            if (m_moduleInfoCallback != nullptr &&
                m_moduleInfoCallback(s_moduleTable.ModuleHandle(i), &moduleName, &moduleGuid) &&
                HasModuleName(moduleName))
            {
                s_consoleWriter.AppendStr(CrashReportHelpers::GetFilename(moduleName));
                s_consoleWriter.AppendChar(' ');
                minipal_guid_as_string(moduleGuid, s_moduleGuidScratch, sizeof(s_moduleGuidScratch));
                s_consoleWriter.AppendStr(s_moduleGuidScratch);
            }
            else
            {
                s_consoleWriter.AppendStr("<unknown>");
            }
            s_consoleWriter.EndLine();
        }
    }

    s_consoleWriter.WriteSeparator();
}

// --- InProcCrashReporter: JSON report lifecycle ----------------------------

void
InProcCrashReporter::BeginJsonReport()
{
    m_jsonWriter.OpenObject();
    m_jsonWriter.OpenObject("payload");
    m_jsonWriter.WriteString("protocol_version", CRASHREPORT_PROTOCOL_VERSION);

    m_jsonWriter.OpenObject("configuration");
    m_jsonWriter.WriteString("architecture", CRASHREPORT_ARCHITECTURE_NAME);
    CrashReportHelpers::GetVersionString(s_versionScratch, sizeof(s_versionScratch));
    m_jsonWriter.WriteString("version", s_versionScratch);
    m_jsonWriter.CloseObject(); // configuration

    if (m_processName[0] != '\0')
    {
        m_jsonWriter.WriteString("process_name", m_processName);
    }

    m_jsonWriter.WriteDecimalAsString("pid", static_cast<uint64_t>(GetCurrentProcessId()));
}

void
InProcCrashReporter::EndJsonReport(
    int signal,
    bool jsonEnabled,
    int fd)
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

    if (jsonEnabled)
    {
        bool finishSucceeded = m_jsonWriter.Finish();
        bool writeFailed = s_outputContext.WriteFailed();
        if (!CrashReportHelpers::WriteToFile(fd, "\n", 1))
        {
            writeFailed = true;
        }

        if (close(fd) != 0 || !finishSucceeded || writeFailed)
        {
            unlink(m_reportFilePathScratch);
        }
    }
    else
    {
        (void)m_jsonWriter.Finish();
    }
}
