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
#endif

// Prescribed compact crash report log format. One logical line == one
// __android_log_write entry under CRASHREPORT_LOG_TAG on Android, one
// '\n'-terminated stderr write on Apple mobile platforms.
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

#if defined(TARGET_IOS) || defined(TARGET_TVOS) || defined(TARGET_MACCATALYST)
// Query a sysctl by name into a caller-supplied buffer. Called from Initialize, NOT from the
// signal handler -- sysctl/sysctlbyname is not on POSIX's async-signal-safe list, so the
// queried values are cached for use during crash reporting (mirrors the hostName /
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
        uint32_t moduleTimestamp,
        uint32_t moduleSize,
        const GUID* moduleGuid,
        uint32_t nativeOffset,
        uint32_t token,
        uint32_t ilOffset,
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
        uint32_t ilOffset);

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

// Holds the reporter's preallocated mutable state. Keeping this separate from
// InProcCrashReporter lets disabled processes avoid the large buffers entirely,
// while Initialize can allocate and publish the state before registering the PAL
// signal callback.
struct InProcCrashReporterStorage
{
    SignalSafeJsonWriter jsonWriter;
    SignalSafeConsoleWriter consoleWriter;
    StackOverflowTraceSnapshot stackOverflowTrace;
    ModuleTable moduleTable;
    ThreadEnumerationContext threadContext;
    CrashReportOutputContext outputContext;
    SignalSafeFormatter formatter;
    InProcCrashReportIsManagedThreadCallback isManagedThreadCallback = nullptr;
    InProcCrashReportWalkStackCallback walkStackCallback = nullptr;
    InProcCrashReportEnumerateThreadsCallback enumerateThreadsCallback = nullptr;
    InProcCrashReportModuleInfoCallback moduleInfoCallback = nullptr;
    volatile LONG crashKind = static_cast<LONG>(InProcCrashReportCrashKind::Unknown);
    uint32_t frameLimitPerThread = 0;
    char reportPath[CRASHREPORT_PATH_BUFFER_SIZE];
    char reportFilePathScratch[CRASHREPORT_PATH_BUFFER_SIZE];
    char expandedReportPathScratch[CRASHREPORT_PATH_BUFFER_SIZE];
    char methodNameScratch[CRASHREPORT_STRING_BUFFER_SIZE];
    char processName[CRASHREPORT_STRING_BUFFER_SIZE];
    char processNameScratch[CRASHREPORT_STRING_BUFFER_SIZE];
    char hostName[CRASHREPORT_STRING_BUFFER_SIZE];
    char versionScratch[sizeof(sccsid)];
    char moduleGuidScratch[MINIPAL_GUID_BUFFER_LEN];
#if defined(TARGET_IOS) || defined(TARGET_TVOS) || defined(TARGET_MACCATALYST)
    char osVersion[CRASHREPORT_STRING_BUFFER_SIZE];
    char systemModel[CRASHREPORT_STRING_BUFFER_SIZE];
#endif
};

static InProcCrashReporterStorage* volatile s_storage = nullptr;

static bool EnsureCrashReportStorage()
{
    if (s_storage != nullptr)
    {
        return true;
    }

    InProcCrashReporterStorage* storage = new (std::nothrow) InProcCrashReporterStorage();
    if (storage == nullptr)
    {
        return false;
    }

    if (InterlockedCompareExchangePointer(&s_storage, storage, nullptr) != nullptr)
    {
        delete storage;
    }

    return true;
}

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
        uint32_t moduleTimestamp,
        uint32_t moduleSize,
        const GUID* moduleGuid,
        uint32_t nativeOffset,
        uint32_t token,
        uint32_t ilOffset);

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
        uint32_t moduleTimestamp,
        uint32_t moduleSize,
        const GUID* moduleGuid,
        uint32_t nativeOffset,
        uint32_t token,
        uint32_t ilOffset,
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
        uint32_t moduleTimestamp,
        uint32_t moduleSize,
        const GUID* moduleGuid,
        uint32_t nativeOffset,
        uint32_t token,
        uint32_t ilOffset);

    static bool WriteToFile(
        int fd,
        const char* buffer,
        size_t len);

    // SignalSafeJsonWriter callback that drops everything: used when the
    // crash report is running in compact-log-only mode (no DbgMiniDumpName)
    // so the JSON formatter still keeps its bookkeeping consistent without
    // emitting bytes anywhere.
    static bool DiscardOutputCallback(const char* buffer, size_t len, void* ctx);

};

void
InProcCrashReporter::CreateReport(
    int signal,
    void* context)
{
    InProcCrashReporterStorage* storage = s_storage;
    if (storage == nullptr)
    {
        return;
    }

    static LONG s_generating = 0;
    if (InterlockedCompareExchange(&s_generating, 1, 0) != 0)
    {
        return;
    }

    storage->reportFilePathScratch[0] = '\0';
    // The JSON file sink is only enabled when DbgMiniDumpName supplied a
    // template AND the template expanded to a valid path. Otherwise the
    // crash report runs in compact-log-only mode: the JSON emitter still
    // executes (so it can keep its bookkeeping consistent) but writes go
    // to a no-op DiscardOutputCallback instead of an open fd.
    bool jsonEnabled = storage->reportPath[0] != '\0' && BuildReportPath();

    int fd = -1;
    if (jsonEnabled)
    {
        fd = open(storage->reportFilePathScratch, O_WRONLY | O_CREAT | O_TRUNC, 0600);
        if (fd == -1)
        {
            jsonEnabled = false;
        }
    }

    InProcCrashReportCrashKind crashKind = static_cast<InProcCrashReportCrashKind>(
        InterlockedExchange(&storage->crashKind, static_cast<LONG>(InProcCrashReportCrashKind::Unknown)));

    storage->outputContext.Init(fd);
    if (jsonEnabled)
    {
        storage->jsonWriter.Init(&CrashReportOutputContext::ChunkCallback, &storage->outputContext);
    }
    else
    {
        storage->jsonWriter.Init(&CrashReportHelpers::DiscardOutputCallback, nullptr);
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
    InProcCrashReporterStorage* storage = s_storage;
    if (storage == nullptr)
    {
        return;
    }

    storage->jsonWriter.OpenArray("threads");
    if (crashKind == InProcCrashReportCrashKind::StackOverflow)
    {
        EmitStackOverflowCrashThread();
    }
    else if (storage->enumerateThreadsCallback != nullptr)
    {
        uint64_t crashingTid = static_cast<uint64_t>(minipal_get_current_thread_id());
        storage->threadContext.Init(&storage->jsonWriter, &storage->consoleWriter, storage->moduleInfoCallback, crashingTid, storage->frameLimitPerThread, context);

        storage->threadContext.EnumerateThreads(storage->enumerateThreadsCallback);

        if (storage->threadContext.ThreadCount() == 0 || !storage->threadContext.SawCrashThread())
        {
            EmitSynthesizedCrashThread(context, /*walkStack*/ false);
        }
    }
    else
    {
        EmitSynthesizedCrashThread(context, /*walkStack*/ true);
    }
    storage->jsonWriter.CloseArray(); // threads
}

InProcCrashReporter&
InProcCrashReporter::GetInstance()
{
    static InProcCrashReporter s_instance;
    return s_instance;
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

bool
InProcCrashReporter::Initialize(
    const InProcCrashReporterSettings& settings)
{
    if (!EnsureCrashReportStorage())
    {
        InProcCrashReportLogInitializationFailure(".NET crash report disabled: failed to allocate reporter storage");
        return false;
    }

    InProcCrashReporterStorage* storage = s_storage;
    storage->isManagedThreadCallback = settings.isManagedThreadCallback;
    storage->walkStackCallback = settings.walkStackCallback;
    storage->enumerateThreadsCallback = settings.enumerateThreadsCallback;
    storage->moduleInfoCallback = settings.moduleInfoCallback;
    storage->frameLimitPerThread = settings.frameLimitPerThread;
    storage->crashKind = static_cast<LONG>(InProcCrashReportCrashKind::Unknown);
    storage->stackOverflowTrace.available = 0;
    CrashReportHelpers::CopyString(storage->reportPath, sizeof(storage->reportPath), settings.reportPath);

    storage->processName[0] = '\0';
#if defined(__ANDROID__)
    // On Android every app forks from the Zygote, so /proc/self/exe always
    // resolves to /system/bin/app_process64. /proc/self/cmdline holds the
    // package name (set by ActivityThread via PR_SET_NAME / setproctitle),
    // which is what crash diagnostics actually want.
    int cmdlineFd = open("/proc/self/cmdline", O_RDONLY | O_CLOEXEC);
    if (cmdlineFd >= 0)
    {
        ssize_t n = read(cmdlineFd, storage->processNameScratch, sizeof(storage->processNameScratch) - 1);
        close(cmdlineFd);
        if (n > 0)
        {
            storage->processNameScratch[n] = '\0';
            CrashReportHelpers::CopyString(storage->processName, sizeof(storage->processName), CrashReportHelpers::GetFilename(storage->processNameScratch));
        }
    }
#endif
    if (storage->processName[0] == '\0')
    {
        if (char* exePath = minipal_getexepath())
        {
            CrashReportHelpers::CopyString(storage->processName, sizeof(storage->processName), CrashReportHelpers::GetFilename(exePath));
            free(exePath);
        }
    }

    // Cache hostname here because gethostname is not on the POSIX
    // async-signal-safe list; the dump-template expander needs it for %h
    // expansion at crash time.
    storage->hostName[0] = '\0';
    if (gethostname(storage->hostName, sizeof(storage->hostName) - 1) == 0)
    {
        storage->hostName[sizeof(storage->hostName) - 1] = '\0';
    }
    else
    {
        storage->hostName[0] = '\0';
    }

#if defined(TARGET_IOS) || defined(TARGET_TVOS) || defined(TARGET_MACCATALYST)
    // Cache sysctl values at Initialize because sysctl/sysctlbyname is not on POSIX's
    // async-signal-safe list; CreateReport reads these from the signal-handler path.
    CacheSysctlString("kern.osproductversion", storage->osVersion, sizeof(storage->osVersion));
    CacheSysctlString("hw.model", storage->systemModel, sizeof(storage->systemModel));
#endif
    return true;
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
    if (!InProcCrashReporter::GetInstance().Initialize(settings))
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
#elif defined(TARGET_IOS) || defined(TARGET_TVOS) || defined(TARGET_MACCATALYST)
    minipal_log_write_error(message);
    minipal_log_write_error("\n");
#else
    (void)message;
#endif
}

void
InProcCrashReportSetCrashKind(InProcCrashReportCrashKind crashKind)
{
    InProcCrashReporterStorage* storage = s_storage;
    if (storage == nullptr)
    {
        return;
    }

    InterlockedExchange(&storage->crashKind, static_cast<LONG>(crashKind));
}

void
InProcCrashReportBeginStackOverflowTrace(
    uint64_t crashingTid,
    uint32_t totalFrameCount)
{
    InProcCrashReporterStorage* storage = s_storage;
    if (storage == nullptr)
    {
        return;
    }

    StackOverflowTraceSnapshot& trace = storage->stackOverflowTrace;
    InterlockedExchange(&trace.available, 0);
    trace.crashingTid = crashingTid;
    trace.totalFrameCount = totalFrameCount;
    trace.frameCount = 0;
    trace.truncatedFrameCount = 0;
}

void
InProcCrashReportAddStackOverflowTraceFrame(
    const char* methodName,
    uint32_t repeatCount,
    uint32_t repeatSequenceLength)
{
    InProcCrashReporterStorage* storage = s_storage;
    if (storage == nullptr)
    {
        return;
    }

    StackOverflowTraceSnapshot& trace = storage->stackOverflowTrace;
    if (trace.frameCount >= CRASHREPORT_STACK_OVERFLOW_MAX_TRACE_FRAMES)
    {
        trace.truncatedFrameCount++;
        return;
    }

    StackOverflowTraceFrame& frame = trace.frames[trace.frameCount++];
    CopyStringToBuffer(frame.methodName, sizeof(frame.methodName), methodName);
    frame.repeatCount = repeatCount;
    frame.repeatSequenceLength = repeatSequenceLength;
}

void
InProcCrashReportEndStackOverflowTrace()
{
    InProcCrashReporterStorage* storage = s_storage;
    if (storage == nullptr)
    {
        return;
    }

    InterlockedExchange(&storage->stackOverflowTrace.available, 1);
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
InProcCrashReporter::ExpandDumpTemplate(
    char* buffer,
    size_t bufferSize,
    const char* pattern)
{
    InProcCrashReporterStorage* storage = s_storage;
    if (storage == nullptr ||
        buffer == nullptr || bufferSize == 0 ||
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
                substitution = storage->formatter.FormatUnsignedDecimal(pid);
                break;

            case 'e':
                substitution = (storage->processName[0] != '\0') ? storage->processName : nullptr;
                break;

            case 'h':
                substitution = (storage->hostName[0] != '\0') ? storage->hostName : nullptr;
                break;

            case 't':
                substitution = storage->formatter.FormatUnsignedDecimal(static_cast<uint64_t>(time(nullptr)));
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
InProcCrashReporter::BuildReportPath()
{
    InProcCrashReporterStorage* storage = s_storage;
    if (storage == nullptr || storage->reportPath[0] == '\0')
    {
        return false;
    }

    size_t expandedLen = ExpandDumpTemplate(
        storage->expandedReportPathScratch,
        sizeof(storage->expandedReportPathScratch),
        storage->reportPath);
    if (expandedLen == 0)
    {
        return false;
    }

    size_t pos = 0;
    if (!CrashReportHelpers::AppendString(storage->reportFilePathScratch, sizeof(storage->reportFilePathScratch), &pos, storage->expandedReportPathScratch))
    {
        return false;
    }
    if (!CrashReportHelpers::AppendString(storage->reportFilePathScratch, sizeof(storage->reportFilePathScratch), &pos, ".crashreport.json"))
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
    uint32_t moduleTimestamp,
    uint32_t moduleSize,
    const GUID* moduleGuid,
    uint32_t nativeOffset,
    uint32_t token,
    uint32_t ilOffset)
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
            InProcCrashReporterStorage* storage = s_storage;
            if (storage != nullptr)
            {
                minipal_guid_as_string(*moduleGuid, storage->moduleGuidScratch, sizeof(storage->moduleGuidScratch));
                writer->WriteString("guid", storage->moduleGuidScratch);
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
    uint32_t moduleTimestamp,
    uint32_t moduleSize,
    const GUID* moduleGuid,
    uint32_t nativeOffset,
    uint32_t token,
    uint32_t ilOffset,
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
        moduleTimestamp,
        moduleSize,
        moduleGuid,
        nativeOffset,
        token,
        ilOffset);
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
    uint32_t moduleTimestamp,
    uint32_t moduleSize,
    const GUID* moduleGuid,
    uint32_t nativeOffset,
    uint32_t token,
    uint32_t ilOffset)
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
        moduleTimestamp, moduleSize, moduleGuid, nativeOffset, token, ilOffset);

    bool consoleCapped = frameLimitPerThread != 0 &&
        frameIndex >= frameLimitPerThread;
    if (!consoleCapped)
    {
        InProcCrashReporterStorage* storage = s_storage;
        int moduleIndex = storage != nullptr && moduleInfoCallback != nullptr && moduleHandle != nullptr
            ? storage->moduleTable.GetOrAddIndex(moduleHandle)
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
    uint32_t moduleTimestamp,
    uint32_t moduleSize,
    const GUID* moduleGuid,
    uint32_t nativeOffset,
    uint32_t token,
    uint32_t ilOffset)
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
        moduleTimestamp,
        moduleSize,
        moduleGuid,
        nativeOffset,
        token,
        ilOffset);
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
        ilOffset);
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
    InProcCrashReporterStorage* storage = s_storage;
    if (storage == nullptr)
    {
        return;
    }

    uint64_t crashingTid = static_cast<uint64_t>(minipal_get_current_thread_id());

    bool isManagedThread = storage->isManagedThreadCallback != nullptr && storage->isManagedThreadCallback();
    CrashReportHelpers::BeginJsonThreadBlock(&storage->jsonWriter,
        crashingTid, isManagedThread, /*isCrashThread*/ true, nullptr, 0);

    CrashReportHelpers::WriteRegistersToJson(&storage->jsonWriter, context);
    CrashReportHelpers::BeginJsonStackFrames(&storage->jsonWriter, /*writeCrashSiteFrame*/ true, context);

    CrashReportHelpers::BeginConsoleThreadBlock(&storage->consoleWriter, crashingTid, /*isCrashThread*/ true);

    uint32_t synthesizedFrameCount = 0;
    uint32_t synthesizedDroppedCount = 0;
    if (walkStack && storage->walkStackCallback != nullptr)
    {
        CrashReportHelpers::FrameContext frameContext =
        {
            &storage->jsonWriter,
            &storage->consoleWriter,
            storage->moduleInfoCallback,
            &synthesizedFrameCount,
            &synthesizedDroppedCount,
            storage->frameLimitPerThread,
            storage->methodNameScratch,
            sizeof(storage->methodNameScratch),
        };
        storage->walkStackCallback(&CrashReportHelpers::WriteFrame, &frameContext);
    }
    CrashReportHelpers::EndConsoleThreadBlock(&storage->consoleWriter,
        synthesizedFrameCount, synthesizedDroppedCount);

    CrashReportHelpers::EndJsonStackFrames(&storage->jsonWriter);
    CrashReportHelpers::EndJsonThreadBlock(&storage->jsonWriter);
}

void
InProcCrashReporter::EmitStackOverflowCrashThread()
{
    InProcCrashReporterStorage* storage = s_storage;
    if (storage == nullptr)
    {
        return;
    }

    StackOverflowTraceSnapshot& trace = storage->stackOverflowTrace;
    bool stackOverflowTraceAvailable = trace.available != 0;
    uint64_t crashingTid = stackOverflowTraceAvailable && trace.crashingTid != 0
        ? trace.crashingTid
        : static_cast<uint64_t>(minipal_get_current_thread_id());

    CrashReportHelpers::BeginJsonThreadBlock(&storage->jsonWriter,
        crashingTid,
        /*isManagedThread*/ true,
        /*isCrashThread*/ true,
        CRASHREPORT_STACK_OVERFLOW_EXCEPTION_TYPE,
        CRASHREPORT_COR_E_STACKOVERFLOW);
    if (stackOverflowTraceAvailable)
    {
        storage->jsonWriter.WriteDecimalAsString("stack_overflow_total_frames", trace.totalFrameCount);
        if (trace.truncatedFrameCount != 0)
        {
            storage->jsonWriter.WriteDecimalAsString("stack_overflow_trace_truncated_frames", trace.truncatedFrameCount);
        }
    }
    else
    {
        storage->jsonWriter.WriteString("stack_frames_unavailable_reason", CRASHREPORT_STACK_OVERFLOW_TRACE_UNAVAILABLE_REASON);
    }

    CrashReportHelpers::BeginJsonStackFrames(&storage->jsonWriter, /*writeCrashSiteFrame*/ false, nullptr);
    if (stackOverflowTraceAvailable)
    {
        for (uint32_t i = 0; i < trace.frameCount;)
        {
            StackOverflowTraceFrame& frame = trace.frames[i];
            uint32_t repeatSequenceLength = frame.repeatSequenceLength;
            bool isRepeatSequence = frame.repeatCount > 1 && repeatSequenceLength != 0;
            CrashReportHelpers::WriteStackOverflowFrameToJson(
                &storage->jsonWriter, frame, isRepeatSequence);
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
                    &storage->jsonWriter, trace.frames[i], false);
            }
        }
    }
    CrashReportHelpers::EndJsonStackFrames(&storage->jsonWriter);
    CrashReportHelpers::EndJsonThreadBlock(&storage->jsonWriter);

    CrashReportHelpers::BeginConsoleThreadBlock(&storage->consoleWriter, crashingTid, /*isCrashThread*/ true);
    storage->consoleWriter.AppendStr("  managed exception: ");
    storage->consoleWriter.AppendStr(CRASHREPORT_STACK_OVERFLOW_EXCEPTION_TYPE);
    storage->consoleWriter.AppendStr(" (0x");
    storage->consoleWriter.AppendHex(static_cast<uint64_t>(CRASHREPORT_COR_E_STACKOVERFLOW));
    storage->consoleWriter.AppendChar(')');
    storage->consoleWriter.EndLine();

    if (!stackOverflowTraceAvailable)
    {
        storage->consoleWriter.WriteLine("  stack overflow trace unavailable");
        CrashReportHelpers::EndConsoleThreadBlock(&storage->consoleWriter, 0, 0);
        return;
    }

    storage->consoleWriter.AppendStr("  stack overflow frames: ");
    storage->consoleWriter.AppendDecimal(static_cast<uint64_t>(trace.totalFrameCount));
    storage->consoleWriter.EndLine();

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

            if (storage->frameLimitPerThread != 0 && consoleFrameCount >= storage->frameLimitPerThread)
            {
                consoleDroppedCount += sequenceEnd - i;
                i = sequenceEnd;
                continue;
            }

            storage->consoleWriter.AppendStr("  repeated ");
            storage->consoleWriter.AppendDecimal(static_cast<uint64_t>(frame.repeatCount));
            storage->consoleWriter.AppendStr(" times:");
            storage->consoleWriter.EndLine();

            for (; i < sequenceEnd; ++i)
            {
                if (storage->frameLimitPerThread != 0 && consoleFrameCount >= storage->frameLimitPerThread)
                {
                    consoleDroppedCount++;
                    continue;
                }

                CrashReportHelpers::WriteStackOverflowFrameToConsole(
                    &storage->consoleWriter, consoleFrameCount, trace.frames[i]);
                consoleFrameCount++;
            }

            continue;
        }

        if (storage->frameLimitPerThread != 0 && consoleFrameCount >= storage->frameLimitPerThread)
        {
            consoleDroppedCount++;
        }
        else
        {
            CrashReportHelpers::WriteStackOverflowFrameToConsole(&storage->consoleWriter, consoleFrameCount, frame);
            consoleFrameCount++;
        }
        ++i;
    }

    CrashReportHelpers::EndConsoleThreadBlock(&storage->consoleWriter,
        consoleFrameCount, consoleDroppedCount);
}

// --- InProcCrashReporter: console report lifecycle -------------------------

void
InProcCrashReporter::BeginConsoleReport(int signal)
{
    InProcCrashReporterStorage* storage = s_storage;
    if (storage == nullptr)
    {
        return;
    }

    storage->consoleWriter.WriteSeparator();
    storage->consoleWriter.AppendStr(".NET Crash Report v");
    storage->consoleWriter.AppendStr(CRASHREPORT_PROTOCOL_VERSION);
    storage->consoleWriter.EndLine();

    CrashReportHelpers::GetVersionString(storage->versionScratch, sizeof(storage->versionScratch));
    if (storage->versionScratch[0] != '\0')
    {
        storage->consoleWriter.WriteKeyValueStr("Build", storage->versionScratch);
    }

    storage->consoleWriter.WriteKeyValueStr("ABI", CRASHREPORT_ARCHITECTURE_NAME);

    if (storage->processName[0] != '\0')
    {
        storage->consoleWriter.WriteKeyValueStr("Cmdline", storage->processName);
    }

    storage->consoleWriter.WriteKeyValueDecimal("pid", static_cast<uint64_t>(GetCurrentProcessId()));

    storage->consoleWriter.AppendStr("signal ");
    storage->consoleWriter.AppendSignedDecimal(signal);
    storage->consoleWriter.AppendStr(" (");
    storage->consoleWriter.AppendStr(GetSignalNameAscii(signal));
    storage->consoleWriter.AppendChar(')');
    storage->consoleWriter.EndLine();
}

void
InProcCrashReporter::EndConsoleReport()
{
    InProcCrashReporterStorage* storage = s_storage;
    if (storage == nullptr)
    {
        return;
    }

    if (storage->moduleTable.Count() != 0)
    {
        storage->consoleWriter.WriteBlank();
        storage->consoleWriter.WriteLine("modules:");
        for (size_t i = 0; i < storage->moduleTable.Count(); ++i)
        {
            storage->consoleWriter.AppendStr("  [");
            storage->consoleWriter.AppendDecimal(static_cast<uint64_t>(i));
            storage->consoleWriter.AppendStr("] ");
            const char* moduleName = nullptr;
            GUID moduleGuid;
            if (storage->moduleInfoCallback != nullptr &&
                storage->moduleInfoCallback(storage->moduleTable.ModuleHandle(i), &moduleName, &moduleGuid) &&
                HasModuleName(moduleName))
            {
                storage->consoleWriter.AppendStr(CrashReportHelpers::GetFilename(moduleName));
                storage->consoleWriter.AppendChar(' ');
                minipal_guid_as_string(moduleGuid, storage->moduleGuidScratch, sizeof(storage->moduleGuidScratch));
                storage->consoleWriter.AppendStr(storage->moduleGuidScratch);
            }
            else
            {
                storage->consoleWriter.AppendStr("<unknown>");
            }
            storage->consoleWriter.EndLine();
        }
    }

    storage->consoleWriter.WriteSeparator();
}

// --- InProcCrashReporter: JSON report lifecycle ----------------------------

void
InProcCrashReporter::BeginJsonReport()
{
    InProcCrashReporterStorage* storage = s_storage;
    if (storage == nullptr)
    {
        return;
    }

    storage->jsonWriter.OpenObject();
    storage->jsonWriter.OpenObject("payload");
    storage->jsonWriter.WriteString("protocol_version", CRASHREPORT_PROTOCOL_VERSION);

    storage->jsonWriter.OpenObject("configuration");
    storage->jsonWriter.WriteString("architecture", CRASHREPORT_ARCHITECTURE_NAME);
    CrashReportHelpers::GetVersionString(storage->versionScratch, sizeof(storage->versionScratch));
    storage->jsonWriter.WriteString("version", storage->versionScratch);
    storage->jsonWriter.CloseObject(); // configuration

    if (storage->processName[0] != '\0')
    {
        storage->jsonWriter.WriteString("process_name", storage->processName);
    }

    storage->jsonWriter.WriteDecimalAsString("pid", static_cast<uint64_t>(GetCurrentProcessId()));
}

void
InProcCrashReporter::EndJsonReport(
    int signal,
    bool jsonEnabled,
    int fd)
{
    InProcCrashReporterStorage* storage = s_storage;
    if (storage == nullptr)
    {
        return;
    }

    storage->jsonWriter.CloseObject(); // payload

    storage->jsonWriter.OpenObject("parameters");
    storage->jsonWriter.WriteSignedDecimalAsString("signal", static_cast<int64_t>(signal));
#if defined(TARGET_IOS) || defined(TARGET_TVOS) || defined(TARGET_MACCATALYST)
    if (storage->osVersion[0] != '\0')
    {
        storage->jsonWriter.WriteString("OSVersion", storage->osVersion);
    }
    if (storage->systemModel[0] != '\0')
    {
        storage->jsonWriter.WriteString("SystemModel", storage->systemModel);
    }
    storage->jsonWriter.WriteString("SystemManufacturer", "apple");
#endif
    storage->jsonWriter.CloseObject(); // parameters

    storage->jsonWriter.CloseObject(); // root

    if (jsonEnabled)
    {
        bool finishSucceeded = storage->jsonWriter.Finish();
        bool writeFailed = storage->outputContext.WriteFailed();
        if (!CrashReportHelpers::WriteToFile(fd, "\n", 1))
        {
            writeFailed = true;
        }

        if (close(fd) != 0 || !finishSucceeded || writeFailed)
        {
            unlink(storage->reportFilePathScratch);
        }
    }
    else
    {
        (void)storage->jsonWriter.Finish();
    }
}
