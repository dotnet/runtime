// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// In-proc crash report generator.
//
// Streams a createdump-shaped JSON skeleton to a crashreport.json file.

#include "inproccrashreporter.h"
#include "inproccrashreportlifecycle.h"
#include "crashreportstringutils.h"
#include "inproccrashreportwatchdog.h"
#include "signalsafeconsolewriter.h"
#include "signalsafejsonwriter.h"
#include "signalsafeformatter.h"

#include "pal.h"
#include "volatile.h"

#include <fcntl.h>
#include <errno.h>
#include <stdlib.h>
#include <new>
#include <unistd.h>
#include <string.h>
#include <time.h>
#include <ucontext.h>
#include <minipal/getexepath.h>
#include <minipal/guid.h>
#include <minipal/log.h>
#include <minipal/thread.h>
#if defined(__ANDROID__)
#include <android/log.h>
#elif defined(TARGET_IOS) || defined(TARGET_TVOS) || defined(TARGET_MACCATALYST)
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
#elif defined(__i386__)
static const char CRASHREPORT_ARCHITECTURE_NAME[] = "x86";
#else
static const char CRASHREPORT_ARCHITECTURE_NAME[] = "unknown";
#endif

// Prescribed compact crash report log format. One logical line == one
// __android_log_write entry under CRASHREPORT_LOG_TAG on Android, one
// '\n'-terminated stderr write on Apple mobile platforms.
//
//   *** *** *** *** *** *** *** *** *** *** *** *** *** *** *** ***   (BeginConsoleReport)
//   .NET Crash Report v<protocol>
//   Build: <sccsid>                                                    (omitted if empty)
//   ABI: <architecture>
//   Cmdline: <process name>                                            (omitted if empty)
//   pid: <pid>
//   signal <N> (<NAME>)
//                                                                      (blank between sections)
//   --- thread 0xTID [(crashed)] ---                                   (BeginConsoleThreadBlock)
//     managed exception: <Type> (0x<HRESULT>)                          (only if EE provided one)
//     #NN [<moduleIndex>] Class.Method + 0xILOFFSET (token=0xTOKEN)    (managed frame; WriteFrameToConsole)
//     #NN (in <name>) Class.Method + 0xILOFFSET (token=0xTOKEN)        (overflow form: module didn't fit the table)
//     #NN [<moduleIndex>] 0xIP (module + 0xOFFSET)                     (native frame; WriteFrameToConsole)
//     #NN 0xIP (module + 0xOFFSET)                                     (native frame not in module table)
//     (no managed frames) | ... +N more frames                         (EndConsoleThreadBlock)
//                                                                      (blank between threads)
//   modules:                                                           (EndConsoleReport)
//     [N] <name> {<MVID>}                                              (one per ModuleTable entry)
//   *** *** *** *** *** *** *** *** *** *** *** *** *** *** *** ***   (closing separator)

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

// Include the .NET version string instead of linking because it is "static".
#if __has_include("_version.c")
#include "_version.c"
#else
static char sccsid[] = "@(#)Version N/A";
#endif

#if defined(TARGET_IOS) || defined(TARGET_TVOS) || defined(TARGET_MACCATALYST)
// Query a sysctl by name into a caller-supplied buffer. Called from Initialize, NOT from the
// signal handler -- sysctl/sysctlbyname is not on POSIX's async-signal-safe list, so the
// queried values are cached for use during crash reporting.
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
#endif // defined(TARGET_IOS) || defined(TARGET_TVOS) || defined(TARGET_MACCATALYST)

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

static constexpr int MAX_MODULES_IN_TABLE = 256;

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

        for (int i = 0; i < m_count; ++i)
        {
            if (m_moduleHandles[i] == moduleHandle)
            {
                return i;
            }
        }

        if (m_count >= MAX_MODULES_IN_TABLE)
        {
            return -1;
        }

        m_moduleHandles[m_count] = moduleHandle;
        return m_count++;
    }

    int Count() const { return m_count; }
    const void* ModuleHandle(int i) const { return m_moduleHandles[i]; }
private:
    const void* m_moduleHandles[MAX_MODULES_IN_TABLE];
    int m_count = 0;
};

struct FrameContext
{
    SignalSafeJsonWriter* jsonWriter;
    SignalSafeConsoleWriter* consoleWriter;
    InProcCrashReportModuleInfoCallback moduleInfoCallback;
    ModuleTable* moduleTable;
    SignalSafeFormatter* formatter;
    uint32_t* currentThreadFrameCount;
    uint32_t* currentThreadDroppedCount;
    uint32_t frameLimitPerThread;
    char* methodNameBuffer;
    size_t methodNameBufferSize;
};

class ThreadEnumerationContext
{
public:
    ThreadEnumerationContext()
    {
        Init(nullptr, nullptr, nullptr, nullptr, nullptr, nullptr, 0, 0, 0, nullptr);
    }

    ThreadEnumerationContext(
        SignalSafeJsonWriter* writer,
        SignalSafeConsoleWriter* consoleWriter,
        InProcCrashReportModuleInfoCallback moduleInfoCallback,
        ModuleTable* moduleTable,
        SignalSafeFormatter* formatter,
        char* methodNameScratch,
        size_t methodNameScratchSize,
        uint64_t crashingTid,
        uint32_t frameLimitPerThread,
        void* signalContext)
    {
        Init(writer, consoleWriter, moduleInfoCallback, moduleTable, formatter, methodNameScratch, methodNameScratchSize, crashingTid, frameLimitPerThread, signalContext);
    }

    ThreadEnumerationContext(const ThreadEnumerationContext&) = delete;
    ThreadEnumerationContext& operator=(const ThreadEnumerationContext&) = delete;

    size_t ThreadCount() const { return m_threadCount; }
    bool SawCrashThread() const { return m_sawCrashThread; }
    SignalSafeJsonWriter* JsonWriter() const { return m_frameContext.jsonWriter; }
    SignalSafeConsoleWriter* ConsoleWriter() const { return m_frameContext.consoleWriter; }

    void Init(
        SignalSafeJsonWriter* writer,
        SignalSafeConsoleWriter* consoleWriter,
        InProcCrashReportModuleInfoCallback moduleInfoCallback,
        ModuleTable* moduleTable,
        SignalSafeFormatter* formatter,
        char* methodNameScratch,
        size_t methodNameScratchSize,
        uint64_t crashingTid,
        uint32_t frameLimitPerThread,
        void* signalContext)
    {
        m_frameContext.jsonWriter = writer;
        m_frameContext.consoleWriter = consoleWriter;
        m_frameContext.moduleInfoCallback = moduleInfoCallback;
        m_frameContext.moduleTable = moduleTable;
        m_frameContext.formatter = formatter;
        m_frameContext.methodNameBuffer = methodNameScratch;
        m_frameContext.methodNameBufferSize = methodNameScratchSize;
        m_frameContext.currentThreadFrameCount = &m_currentThreadFrameCount;
        m_frameContext.currentThreadDroppedCount = &m_currentThreadDroppedCount;
        m_frameContext.frameLimitPerThread = frameLimitPerThread;
        m_signalContext = signalContext;
        m_threadCount = 0;
        m_crashingTid = crashingTid;
        m_currentThreadFrameCount = 0;
        m_currentThreadDroppedCount = 0;
        m_sawCrashThread = false;
        if (methodNameScratch != nullptr && methodNameScratchSize != 0)
        {
            methodNameScratch[0] = '\0';
        }
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
        uint32_t moduleTimestamp,
        uint32_t moduleSize,
        const GUID* moduleGuid,
        uint32_t nativeOffset,
        uint32_t token,
        uint32_t ilOffset,
        const char* genericArgs,
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
        uint32_t moduleTimestamp,
        uint32_t moduleSize,
        const GUID* moduleGuid,
        uint32_t nativeOffset,
        uint32_t token,
        uint32_t ilOffset,
        const char* genericArgs);

    void EndCurrentConsoleThreadBlock();
    void EndCurrentJsonThreadBlock();

    FrameContext m_frameContext;
    void* m_signalContext;
    size_t m_threadCount;
    uint64_t m_crashingTid;
    uint32_t m_currentThreadFrameCount;
    uint32_t m_currentThreadDroppedCount;
    bool m_sawCrashThread;
};

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

class InProcCrashReporter
{
public:
    static InProcCrashReporter* GetInstance();
    static bool InitializeInstance(const InProcCrashReporterSettings& settings);

    // Capture configuration and the crash-report template path. Must run before
    // the instance is published to the PAL signal-handler path.
    void Initialize(const InProcCrashReporterSettings& settings);

    void CreateReport(
        int signal,
        void* context);

    void SetCrashKind(InProcCrashReportCrashKind crashKind);
    void BeginStackOverflowTrace(uint64_t crashingTid, uint32_t totalFrameCount);
    void AddStackOverflowTraceFrame(
        const char* methodName,
        uint32_t repeatCount,
        uint32_t repeatSequenceLength);
    void EndStackOverflowTrace();

private:
    InProcCrashReporter() = default;
    InProcCrashReporter(const InProcCrashReporter&) = delete;
    InProcCrashReporter& operator=(const InProcCrashReporter&) = delete;

    void EmitSynthesizedCrashThread(
        void* context,
        bool walkStack);

    void EmitStackOverflowCrashThread();

    void EmitThreads(
        InProcCrashReportCrashKind crashKind,
        void* context);

    void BeginConsoleReport(int signal);
    void EndConsoleReport();

    void BeginJsonReport();
    void EndJsonReport(
        int signal,
        bool jsonEnabled,
        int fd);

    static const char* GetSignalNameAscii(int signal);

    SignalSafeJsonWriter m_jsonWriter;
    SignalSafeConsoleWriter m_consoleWriter;
    StackOverflowTraceSnapshot m_stackOverflowTrace;
    ModuleTable m_moduleTable;
    ThreadEnumerationContext m_threadContext;
    CrashReportOutputContext m_outputContext;
    SignalSafeFormatter m_formatter;
    InProcCrashReportIsManagedThreadCallback m_isManagedThreadCallback = nullptr;
    InProcCrashReportWalkStackCallback m_walkStackCallback = nullptr;
    InProcCrashReportEnumerateThreadsCallback m_enumerateThreadsCallback = nullptr;
    InProcCrashReportModuleInfoCallback m_moduleInfoCallback = nullptr;
    volatile LONG m_crashKind = static_cast<LONG>(InProcCrashReportCrashKind::Unknown);
    uint32_t m_frameLimitPerThread = 0;
    InProcCrashReportLifecycle m_lifecycle;
    char m_reportFilePath[CRASHREPORT_PATH_BUFFER_SIZE];
    char m_processName[CRASHREPORT_STRING_BUFFER_SIZE];
    char m_stringScratch[CRASHREPORT_STRING_BUFFER_SIZE];
#if defined(TARGET_IOS) || defined(TARGET_TVOS) || defined(TARGET_MACCATALYST)
    char m_osVersion[CRASHREPORT_STRING_BUFFER_SIZE];
    char m_systemModel[CRASHREPORT_STRING_BUFFER_SIZE];
#endif
};

static InProcCrashReporter* s_reporter = nullptr;

class CrashReportHelpers
{
public:
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

    static void WriteFrameToJson(
        SignalSafeJsonWriter* writer,
        SignalSafeFormatter* formatter,
        char* methodNameBuffer,
        size_t methodNameBufferSize,
        uint64_t ip,
        uint64_t stackPointer,
        const char* methodName,
        const char* className,
        const char* moduleName,
        uint32_t moduleTimestamp,
        uint32_t moduleSize,
        const GUID* moduleGuid,
        uint32_t nativeOffset,
        uint32_t token,
        uint32_t ilOffset,
        const char* genericArgs);

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
        uint32_t ilOffset,
        const char* genericArgs);

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
        uint32_t moduleTimestamp,
        uint32_t moduleSize,
        const GUID* moduleGuid,
        uint32_t nativeOffset,
        uint32_t token,
        uint32_t ilOffset,
        const char* genericArgs,
        void* ctx);

    static void WriteFrameToReport(
        FrameContext* frameContext,
        uint64_t ip,
        uint64_t stackPointer,
        const char* methodName,
        const char* className,
        const char* moduleName,
        const void* moduleHandle,
        uint32_t moduleTimestamp,
        uint32_t moduleSize,
        const GUID* moduleGuid,
        uint32_t nativeOffset,
        uint32_t token,
        uint32_t ilOffset,
        const char* genericArgs);

    static bool WriteToFile(
        int fd,
        const char* buffer,
        size_t len);

    // SignalSafeJsonWriter callback that drops everything: used when the
    // crash report is running in compact-log-only mode (no managed report
    // directory configured) so the JSON formatter still keeps its bookkeeping
    // consistent without emitting bytes anywhere.
    static bool DiscardOutputCallback(const char* buffer, size_t len, void* ctx);

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
    CrashReportWatchdogScope watchdogScope;

    m_reportFilePath[0] = '\0';
    // The JSON file sink is enabled only by lifecycle-managed output. Otherwise
    // the crash report runs in compact-log-only mode: the JSON emitter still
    // executes (so it can keep its bookkeeping consistent) but writes go to a
    // no-op DiscardOutputCallback instead of an open fd.
    int fd = -1;
    bool jsonEnabled = m_lifecycle.IsReportFileOutputEnabled() &&
        m_lifecycle.PrepareReportFile(&m_formatter, m_reportFilePath, sizeof(m_reportFilePath), &fd);

    if (jsonEnabled && fd == -1)
    {
        jsonEnabled = false;
    }

    InProcCrashReportCrashKind crashKind = static_cast<InProcCrashReportCrashKind>(
        InterlockedExchange(&m_crashKind, static_cast<LONG>(InProcCrashReportCrashKind::Unknown)));

    m_outputContext.Init(fd);
    if (jsonEnabled)
    {
        m_jsonWriter.Init(&CrashReportOutputContext::ChunkCallback, &m_outputContext);
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
        m_threadContext.Init(
            &m_jsonWriter,
            &m_consoleWriter,
            m_moduleInfoCallback,
            &m_moduleTable,
            &m_formatter,
            m_stringScratch,
            sizeof(m_stringScratch),
            crashingTid,
            m_frameLimitPerThread,
            context);

        m_threadContext.EnumerateThreads(m_enumerateThreadsCallback);

        if (m_threadContext.ThreadCount() == 0 || !m_threadContext.SawCrashThread())
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

InProcCrashReporter*
InProcCrashReporter::GetInstance()
{
    return VolatileLoad(&s_reporter);
}

bool
InProcCrashReporter::InitializeInstance(
    const InProcCrashReporterSettings& settings)
{
    if (VolatileLoad(&s_reporter) != nullptr)
    {
        return true;
    }

    InProcCrashReporter* reporter = new (std::nothrow) InProcCrashReporter();
    if (reporter == nullptr)
    {
        InProcCrashReportLogInitializationFailure(".NET crash report disabled: failed to allocate reporter storage");
        return false;
    }

    reporter->Initialize(settings);
    if (InterlockedCompareExchangePointer(&s_reporter, reporter, nullptr) != nullptr)
    {
        delete reporter;
    }

    return true;
}

const char*
InProcCrashReporter::GetSignalNameAscii(int signal)
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

void
InProcCrashReporter::Initialize(
    const InProcCrashReporterSettings& settings)
{
    m_isManagedThreadCallback = settings.isManagedThreadCallback;
    m_walkStackCallback = settings.walkStackCallback;
    m_enumerateThreadsCallback = settings.enumerateThreadsCallback;
    m_moduleInfoCallback = settings.moduleInfoCallback;
    m_frameLimitPerThread = settings.frameLimitPerThread;
    m_crashKind = static_cast<LONG>(InProcCrashReportCrashKind::Unknown);
    m_stackOverflowTrace.available = 0;
    m_reportFilePath[0] = '\0';

    (void)CrashReportWatchdog::TryInitialize(settings.timeoutSeconds);

    m_processName[0] = '\0';
#if defined(__ANDROID__)
    // On Android every app forks from the Zygote, so /proc/self/exe always
    // resolves to /system/bin/app_process64. /proc/self/cmdline holds the
    // package name (set by ActivityThread via PR_SET_NAME / setproctitle),
    // which is what crash diagnostics actually want.
    int cmdlineFd = open("/proc/self/cmdline", O_RDONLY | O_CLOEXEC);
    if (cmdlineFd >= 0)
    {
        ssize_t n = read(cmdlineFd, m_stringScratch, sizeof(m_stringScratch) - 1);
        close(cmdlineFd);
        if (n > 0)
        {
            m_stringScratch[n] = '\0';
            CrashReportStringUtils::CopyString(m_processName, sizeof(m_processName), CrashReportHelpers::GetFilename(m_stringScratch));
        }
    }
#endif
    if (m_processName[0] == '\0')
    {
        if (char* exePath = minipal_getexepath())
        {
            CrashReportStringUtils::CopyString(m_processName, sizeof(m_processName), CrashReportHelpers::GetFilename(exePath));
            free(exePath);
        }
    }

    // File output is produced only through the lifecycle-managed report
    // directory. When no root is configured the reporter still runs, emitting
    // compact console logs without writing a JSON report file.
    if (settings.reportRootPath != nullptr && settings.reportRootPath[0] != '\0')
    {
        m_lifecycle.Initialize(settings.reportRootPath, settings.maxFileCount);
    }

#if defined(TARGET_IOS) || defined(TARGET_TVOS) || defined(TARGET_MACCATALYST)
    // Cache sysctl values at Initialize because sysctl/sysctlbyname is not on POSIX's
    // async-signal-safe list; CreateReport reads these from the signal-handler path.
    CacheSysctlString("kern.osproductversion", m_osVersion, sizeof(m_osVersion));
    CacheSysctlString("hw.model", m_systemModel, sizeof(m_systemModel));
#endif
}

void
InProcCrashReporter::SetCrashKind(InProcCrashReportCrashKind crashKind)
{
    InterlockedExchange(&m_crashKind, static_cast<LONG>(crashKind));
}

void
InProcCrashReporter::BeginStackOverflowTrace(
    uint64_t crashingTid,
    uint32_t totalFrameCount)
{
    StackOverflowTraceSnapshot& trace = m_stackOverflowTrace;
    InterlockedExchange(&trace.available, 0);
    trace.crashingTid = crashingTid;
    trace.totalFrameCount = totalFrameCount;
    trace.frameCount = 0;
    trace.truncatedFrameCount = 0;
}

void
InProcCrashReporter::AddStackOverflowTraceFrame(
    const char* methodName,
    uint32_t repeatCount,
    uint32_t repeatSequenceLength)
{
    StackOverflowTraceSnapshot& trace = m_stackOverflowTrace;
    if (trace.frameCount >= CRASHREPORT_STACK_OVERFLOW_MAX_TRACE_FRAMES)
    {
        trace.truncatedFrameCount++;
        return;
    }

    StackOverflowTraceFrame& frame = trace.frames[trace.frameCount++];
    CrashReportStringUtils::CopyString(frame.methodName, sizeof(frame.methodName), methodName);
    frame.repeatCount = repeatCount;
    frame.repeatSequenceLength = repeatSequenceLength;
}

void
InProcCrashReporter::EndStackOverflowTrace()
{
    InterlockedExchange(&m_stackOverflowTrace.available, 1);
}

void
InProcCrashReportSignalDispatcher(int signal, void* siginfo, void* context)
{
    (void)siginfo;

    InProcCrashReporter* reporter = InProcCrashReporter::GetInstance();
    if (reporter == nullptr)
    {
        return;
    }

    // Preserve the interrupted context's errno before the crash reporter uses syscalls.
    int savedErrno = errno;
    reporter->CreateReport(signal, context);
    errno = savedErrno;
}

void
InProcCrashReportInitialize(const InProcCrashReporterSettings& settings)
{
    if (!InProcCrashReporter::InitializeInstance(settings))
    {
        return;
    }

    // Register last so PAL only observes the dispatcher after the reporter
    // singleton is fully populated (mirrors the publication ordering used by
    // PAL_SetLogManagedCallstackForSignalCallback).
    PAL_SetInProcCrashReportCallback(&InProcCrashReportSignalDispatcher);
}

void
InProcCrashReportLogInitializationFailure(const char* message)
{
    if (message == nullptr)
    {
        return;
    }

#if defined(__ANDROID__)
    __android_log_write(ANDROID_LOG_ERROR, CRASHREPORT_LOG_TAG, message);
#else
    minipal_log_write_error(message);
    minipal_log_write_error("\n");
#endif
}

void
InProcCrashReportSetCrashKind(InProcCrashReportCrashKind crashKind)
{
    InProcCrashReporter* reporter = InProcCrashReporter::GetInstance();
    if (reporter == nullptr)
    {
        return;
    }

    reporter->SetCrashKind(crashKind);
}

void
InProcCrashReportBeginStackOverflowTrace(
    uint64_t crashingTid,
    uint32_t totalFrameCount)
{
    InProcCrashReporter* reporter = InProcCrashReporter::GetInstance();
    if (reporter == nullptr)
    {
        return;
    }

    reporter->BeginStackOverflowTrace(crashingTid, totalFrameCount);
}

void
InProcCrashReportAddStackOverflowTraceFrame(
    const char* methodName,
    uint32_t repeatCount,
    uint32_t repeatSequenceLength)
{
    InProcCrashReporter* reporter = InProcCrashReporter::GetInstance();
    if (reporter == nullptr)
    {
        return;
    }

    reporter->AddStackOverflowTraceFrame(methodName, repeatCount, repeatSequenceLength);
}

void
InProcCrashReportEndStackOverflowTrace()
{
    InProcCrashReporter* reporter = InProcCrashReporter::GetInstance();
    if (reporter == nullptr)
    {
        return;
    }

    reporter->EndStackOverflowTrace();
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
    return CrashReportStringUtils::AppendString(buffer, bufferSize, pos, value);
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
#if (defined(TARGET_IOS) || defined(TARGET_TVOS) || defined(TARGET_MACCATALYST)) && defined(__x86_64__)
    return static_cast<uint64_t>(ucontext->uc_mcontext->__ss.__rip);
#elif (defined(TARGET_IOS) || defined(TARGET_TVOS) || defined(TARGET_MACCATALYST)) && defined(__aarch64__)
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
#if (defined(TARGET_IOS) || defined(TARGET_TVOS) || defined(TARGET_MACCATALYST)) && defined(__x86_64__)
    return static_cast<uint64_t>(ucontext->uc_mcontext->__ss.__rsp);
#elif (defined(TARGET_IOS) || defined(TARGET_TVOS) || defined(TARGET_MACCATALYST)) && defined(__aarch64__)
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
#if (defined(TARGET_IOS) || defined(TARGET_TVOS) || defined(TARGET_MACCATALYST)) && defined(__x86_64__)
    return static_cast<uint64_t>(ucontext->uc_mcontext->__ss.__rbp);
#elif (defined(TARGET_IOS) || defined(TARGET_TVOS) || defined(TARGET_MACCATALYST)) && defined(__aarch64__)
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
        CrashReportStringUtils::CopyString(buffer, bufferSize, className);
    }
    else if (methodName != nullptr)
    {
        CrashReportStringUtils::CopyString(buffer, bufferSize, methodName);
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
CrashReportHelpers::WriteFrameToJson(
    SignalSafeJsonWriter* writer,
    SignalSafeFormatter* formatter,
    char* methodNameBuffer,
    size_t methodNameBufferSize,
    uint64_t ip,
    uint64_t stackPointer,
    const char* methodName,
    const char* className,
    const char* moduleName,
    uint32_t moduleTimestamp,
    uint32_t moduleSize,
    const GUID* moduleGuid,
    uint32_t nativeOffset,
    uint32_t token,
    uint32_t ilOffset,
    const char* genericArgs)
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
        // Generic instantiation arguments cannot be recovered off-device from the
        // token + PDB (instantiations are a runtime construct, not metadata), so
        // record them here alongside the deferred keys to keep the JSON report a
        // complete, self-describing store.
        if (genericArgs != nullptr && genericArgs[0] != '\0')
        {
            writer->WriteString("generic_args", genericArgs);
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
            if (formatter != nullptr)
            {
                writer->WriteString("guid", formatter->FormatGuid(*moduleGuid));
            }
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
    uint32_t ilOffset,
    const char* genericArgs)
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
        // Generic instantiation arguments travel inline because they cannot be
        // recovered off-device from the token + PDB. Format: "<classArgs;methodArgs>"
        // (value-type args exact; reference-type args show as System.__Canon).
        if (genericArgs != nullptr && genericArgs[0] != '\0')
        {
            consoleWriter->AppendChar(' ');
            consoleWriter->AppendStr(genericArgs);
        }
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
    uint32_t moduleTimestamp,
    uint32_t moduleSize,
    const GUID* moduleGuid,
    uint32_t nativeOffset,
    uint32_t token,
    uint32_t ilOffset,
    const char* genericArgs,
    void* ctx)
{
    FrameContext* frameContext = reinterpret_cast<FrameContext*>(ctx);
    if (frameContext == nullptr)
    {
        return;
    }

    WriteFrameToReport(
        frameContext,
        ip,
        stackPointer,
        methodName,
        className,
        moduleName,
        moduleHandle,
        moduleTimestamp,
        moduleSize,
        moduleGuid,
        nativeOffset,
        token,
        ilOffset,
        genericArgs);
}

void
CrashReportHelpers::WriteFrameToReport(
    FrameContext* frameContext,
    uint64_t ip,
    uint64_t stackPointer,
    const char* methodName,
    const char* className,
    const char* moduleName,
    const void* moduleHandle,
    uint32_t moduleTimestamp,
    uint32_t moduleSize,
    const GUID* moduleGuid,
    uint32_t nativeOffset,
    uint32_t token,
    uint32_t ilOffset,
    const char* genericArgs)
{
    if (frameContext == nullptr)
    {
        return;
    }

    SignalSafeJsonWriter* jsonWriter = frameContext->jsonWriter;
    SignalSafeConsoleWriter* consoleWriter = frameContext->consoleWriter;
    InProcCrashReportModuleInfoCallback moduleInfoCallback = frameContext->moduleInfoCallback;
    ModuleTable* moduleTable = frameContext->moduleTable;
    SignalSafeFormatter* formatter = frameContext->formatter;
    char* methodNameBuffer = frameContext->methodNameBuffer;
    size_t methodNameBufferSize = frameContext->methodNameBufferSize;
    uint32_t* currentThreadFrameCount = frameContext->currentThreadFrameCount;
    uint32_t* currentThreadDroppedCount = frameContext->currentThreadDroppedCount;
    uint32_t frameLimitPerThread = frameContext->frameLimitPerThread;
    uint32_t frameIndex = currentThreadFrameCount != nullptr
        ? *currentThreadFrameCount
        : 0;

    // Always feed the JSON sink: the file output is the authoritative,
    // post-mortem data store and the cap is a compact-log triage knob.
    WriteFrameToJson(jsonWriter,
        formatter,
        methodNameBuffer,
        methodNameBufferSize,
        ip, stackPointer, methodName, className, moduleName,
        moduleTimestamp, moduleSize, moduleGuid, nativeOffset, token, ilOffset,
        genericArgs);

    bool consoleCapped = frameLimitPerThread != 0 &&
        frameIndex >= frameLimitPerThread;
    if (!consoleCapped)
    {
        int moduleIndex = -1;
        if (moduleTable != nullptr && moduleInfoCallback != nullptr && moduleHandle != nullptr)
        {
            const char* resolvedModuleName = nullptr;
            GUID resolvedModuleGuid;
            if (moduleInfoCallback(moduleHandle, &resolvedModuleName, &resolvedModuleGuid) &&
                HasModuleName(resolvedModuleName))
            {
                moduleIndex = moduleTable->GetOrAddIndex(moduleHandle);
            }
        }
        WriteFrameToConsole(consoleWriter,
            methodNameBuffer,
            methodNameBufferSize,
            frameIndex, moduleIndex, ip, methodName, className, moduleName,
            nativeOffset, token, ilOffset, genericArgs);
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
    uint32_t moduleTimestamp,
    uint32_t moduleSize,
    const GUID* moduleGuid,
    uint32_t nativeOffset,
    uint32_t token,
    uint32_t ilOffset,
    const char* genericArgs)
{
    CrashReportHelpers::WriteFrameToReport(
        &m_frameContext,
        ip,
        stackPointer,
        methodName,
        className,
        moduleName,
        moduleHandle,
        moduleTimestamp,
        moduleSize,
        moduleGuid,
        nativeOffset,
        token,
        ilOffset,
        genericArgs);
}

void
ThreadEnumerationContext::FrameCallback(
    uint64_t ip,
    uint64_t stackPointer,
    const char* methodName,
    const char* className,
    const char* moduleName,
    const void* moduleHandle,
    uint32_t moduleTimestamp,
    uint32_t moduleSize,
    const GUID* moduleGuid,
    uint32_t nativeOffset,
    uint32_t token,
    uint32_t ilOffset,
    const char* genericArgs,
    void* ctx)
{
    if (ctx == nullptr)
    {
        return;
    }
    reinterpret_cast<ThreadEnumerationContext*>(ctx)->OnFrame(
        ip,
        stackPointer,
        methodName,
        className,
        moduleName,
        moduleHandle,
        moduleTimestamp,
        moduleSize,
        moduleGuid,
        nativeOffset,
        token,
        ilOffset,
        genericArgs);
}

void
ThreadEnumerationContext::EndCurrentConsoleThreadBlock()
{
    if (m_threadCount == 0)
    {
        return;
    }

    CrashReportHelpers::EndConsoleThreadBlock(m_frameContext.consoleWriter,
        m_currentThreadFrameCount, m_currentThreadDroppedCount);
}

void
ThreadEnumerationContext::EndCurrentJsonThreadBlock()
{
    if (m_threadCount == 0)
    {
        return;
    }

    CrashReportHelpers::EndJsonStackFrames(m_frameContext.jsonWriter);
    CrashReportHelpers::EndJsonThreadBlock(m_frameContext.jsonWriter);

    (void)m_frameContext.jsonWriter->Flush();
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

    CrashReportHelpers::BeginJsonThreadBlock(m_frameContext.jsonWriter,
        osThreadId, /*isManagedThread*/ true, isCrashThread, exceptionType, exceptionHResult);

    if (isCrashThread)
    {
        CrashReportHelpers::WriteRegistersToJson(m_frameContext.jsonWriter, m_signalContext);
    }

    CrashReportHelpers::BeginJsonStackFrames(m_frameContext.jsonWriter, isCrashThread, m_signalContext);

    if (m_frameContext.consoleWriter != nullptr)
    {
        CrashReportHelpers::BeginConsoleThreadBlock(m_frameContext.consoleWriter, osThreadId, isCrashThread);

        if (exceptionType != nullptr && exceptionType[0] != '\0')
        {
            m_frameContext.consoleWriter->AppendStr("  managed exception: ");
            m_frameContext.consoleWriter->AppendStr(exceptionType);
            m_frameContext.consoleWriter->AppendStr(" (0x");
            m_frameContext.consoleWriter->AppendHex(static_cast<uint64_t>(exceptionHResult));
            m_frameContext.consoleWriter->AppendChar(')');
            m_frameContext.consoleWriter->EndLine();
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

    CrashReportHelpers::BeginConsoleThreadBlock(&m_consoleWriter, crashingTid, /*isCrashThread*/ true);

    uint32_t synthesizedFrameCount = 0;
    uint32_t synthesizedDroppedCount = 0;
    if (walkStack && m_walkStackCallback != nullptr)
    {
        FrameContext frameContext =
        {
            &m_jsonWriter,
            &m_consoleWriter,
            m_moduleInfoCallback,
            &m_moduleTable,
            &m_formatter,
            &synthesizedFrameCount,
            &synthesizedDroppedCount,
            m_frameLimitPerThread,
            m_stringScratch,
            sizeof(m_stringScratch),
        };
        m_walkStackCallback(&CrashReportHelpers::WriteFrame, &frameContext);
    }
    CrashReportHelpers::EndConsoleThreadBlock(&m_consoleWriter,
        synthesizedFrameCount, synthesizedDroppedCount);

    CrashReportHelpers::EndJsonStackFrames(&m_jsonWriter);
    CrashReportHelpers::EndJsonThreadBlock(&m_jsonWriter);
}

void
InProcCrashReporter::EmitStackOverflowCrashThread()
{
    StackOverflowTraceSnapshot& trace = m_stackOverflowTrace;
    bool stackOverflowTraceAvailable = InterlockedCompareExchange(&trace.available, 0, 0) != 0;
    uint64_t crashingTid = stackOverflowTraceAvailable && trace.crashingTid != 0
        ? trace.crashingTid
        : static_cast<uint64_t>(minipal_get_current_thread_id());

    CrashReportHelpers::BeginJsonThreadBlock(&m_jsonWriter,
        crashingTid,
        /*isManagedThread*/ true,
        /*isCrashThread*/ true,
        CRASHREPORT_STACK_OVERFLOW_EXCEPTION_TYPE,
        CRASHREPORT_COR_E_STACKOVERFLOW);
    if (stackOverflowTraceAvailable)
    {
        m_jsonWriter.WriteDecimalAsString("stack_overflow_total_frames", trace.totalFrameCount);
        if (trace.truncatedFrameCount != 0)
        {
            m_jsonWriter.WriteDecimalAsString("stack_overflow_trace_truncated_frames", trace.truncatedFrameCount);
        }
    }
    else
    {
        m_jsonWriter.WriteString("stack_frames_unavailable_reason", CRASHREPORT_STACK_OVERFLOW_TRACE_UNAVAILABLE_REASON);
    }

    CrashReportHelpers::BeginJsonStackFrames(&m_jsonWriter, /*writeCrashSiteFrame*/ false, nullptr);
    if (stackOverflowTraceAvailable)
    {
        for (uint32_t i = 0; i < trace.frameCount;)
        {
            StackOverflowTraceFrame& frame = trace.frames[i];
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
            if (sequenceEnd > trace.frameCount)
            {
                sequenceEnd = trace.frameCount;
            }

            for (; i < sequenceEnd; ++i)
            {
                CrashReportHelpers::WriteStackOverflowFrameToJson(
                    &m_jsonWriter, trace.frames[i], false);
            }
        }
    }
    CrashReportHelpers::EndJsonStackFrames(&m_jsonWriter);
    CrashReportHelpers::EndJsonThreadBlock(&m_jsonWriter);

    CrashReportHelpers::BeginConsoleThreadBlock(&m_consoleWriter, crashingTid, /*isCrashThread*/ true);
    m_consoleWriter.AppendStr("  managed exception: ");
    m_consoleWriter.AppendStr(CRASHREPORT_STACK_OVERFLOW_EXCEPTION_TYPE);
    m_consoleWriter.AppendStr(" (0x");
    m_consoleWriter.AppendHex(static_cast<uint64_t>(CRASHREPORT_COR_E_STACKOVERFLOW));
    m_consoleWriter.AppendChar(')');
    m_consoleWriter.EndLine();

    if (!stackOverflowTraceAvailable)
    {
        m_consoleWriter.WriteLine("  stack overflow trace unavailable");
        CrashReportHelpers::EndConsoleThreadBlock(&m_consoleWriter, 0, 0);
        return;
    }

    m_consoleWriter.AppendStr("  stack overflow frames: ");
    m_consoleWriter.AppendDecimal(static_cast<uint64_t>(trace.totalFrameCount));
    m_consoleWriter.EndLine();

    uint32_t consoleFrameCount = 0;
    uint32_t consoleDroppedCount = trace.truncatedFrameCount;
    for (uint32_t i = 0; i < trace.frameCount;)
    {
        StackOverflowTraceFrame& frame = trace.frames[i];
        uint32_t repeatSequenceLength = frame.repeatSequenceLength;
        if (frame.repeatCount > 1 && repeatSequenceLength != 0)
        {
            uint32_t sequenceEnd = i + repeatSequenceLength;
            if (sequenceEnd > trace.frameCount)
            {
                sequenceEnd = trace.frameCount;
            }

            if (m_frameLimitPerThread != 0 && consoleFrameCount >= m_frameLimitPerThread)
            {
                consoleDroppedCount += sequenceEnd - i;
                i = sequenceEnd;
                continue;
            }

            m_consoleWriter.AppendStr("  repeated ");
            m_consoleWriter.AppendDecimal(static_cast<uint64_t>(frame.repeatCount));
            m_consoleWriter.AppendStr(" times:");
            m_consoleWriter.EndLine();

            for (; i < sequenceEnd; ++i)
            {
                if (m_frameLimitPerThread != 0 && consoleFrameCount >= m_frameLimitPerThread)
                {
                    consoleDroppedCount++;
                    continue;
                }

                CrashReportHelpers::WriteStackOverflowFrameToConsole(
                    &m_consoleWriter, consoleFrameCount, trace.frames[i]);
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
            CrashReportHelpers::WriteStackOverflowFrameToConsole(&m_consoleWriter, consoleFrameCount, frame);
            consoleFrameCount++;
        }
        ++i;
    }

    CrashReportHelpers::EndConsoleThreadBlock(&m_consoleWriter,
        consoleFrameCount, consoleDroppedCount);
}

// --- InProcCrashReporter: console report lifecycle -------------------------

void
InProcCrashReporter::BeginConsoleReport(int signal)
{
    m_consoleWriter.WriteSeparator();
    m_consoleWriter.AppendStr(".NET Crash Report v");
    m_consoleWriter.AppendStr(CRASHREPORT_PROTOCOL_VERSION);
    m_consoleWriter.EndLine();

    CrashReportHelpers::GetVersionString(m_stringScratch, sizeof(m_stringScratch));
    if (m_stringScratch[0] != '\0')
    {
        m_consoleWriter.WriteKeyValueStr("Build", m_stringScratch);
    }

    m_consoleWriter.WriteKeyValueStr("ABI", CRASHREPORT_ARCHITECTURE_NAME);

    if (m_processName[0] != '\0')
    {
        m_consoleWriter.WriteKeyValueStr("Cmdline", m_processName);
    }

    m_consoleWriter.WriteKeyValueDecimal("pid", static_cast<uint64_t>(GetCurrentProcessId()));

    m_consoleWriter.AppendStr("signal ");
    m_consoleWriter.AppendSignedDecimal(signal);
    m_consoleWriter.AppendStr(" (");
    m_consoleWriter.AppendStr(GetSignalNameAscii(signal));
    m_consoleWriter.AppendChar(')');
    m_consoleWriter.EndLine();
}

void
InProcCrashReporter::EndConsoleReport()
{
    if (m_moduleTable.Count() != 0)
    {
        m_consoleWriter.WriteBlank();
        m_consoleWriter.WriteLine("modules:");
        for (int i = 0; i < m_moduleTable.Count(); ++i)
        {
            m_consoleWriter.AppendStr("  [");
            m_consoleWriter.AppendDecimal(static_cast<uint64_t>(i));
            m_consoleWriter.AppendStr("] ");
            const char* moduleName = nullptr;
            GUID moduleGuid;
            if (m_moduleInfoCallback != nullptr &&
                m_moduleInfoCallback(m_moduleTable.ModuleHandle(i), &moduleName, &moduleGuid) &&
                HasModuleName(moduleName))
            {
                m_consoleWriter.AppendStr(CrashReportHelpers::GetFilename(moduleName));
                m_consoleWriter.AppendChar(' ');
                m_consoleWriter.AppendStr(m_formatter.FormatGuid(moduleGuid));
            }
            else
            {
                m_consoleWriter.AppendStr("<unknown>");
            }
            m_consoleWriter.EndLine();
        }
    }

    m_consoleWriter.WriteSeparator();
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
    CrashReportHelpers::GetVersionString(m_stringScratch, sizeof(m_stringScratch));
    m_jsonWriter.WriteString("version", m_stringScratch);
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
#if defined(TARGET_IOS) || defined(TARGET_TVOS) || defined(TARGET_MACCATALYST)
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
        bool writeFailed = m_outputContext.WriteFailed();
        if (!CrashReportHelpers::WriteToFile(fd, "\n", 1))
        {
            writeFailed = true;
        }

        bool closeSucceeded = close(fd) == 0;
        bool reportSucceeded = finishSucceeded && !writeFailed && closeSucceeded;
        m_lifecycle.FinishReportFile(reportSucceeded, m_reportFilePath);
    }
    else
    {
        (void)m_jsonWriter.Finish();
    }
}
