// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// In-proc crash report generator.
//
// Streams a createdump-shaped JSON skeleton to a crashreport.json file.

#include "inproccrashreporter.h"
#include "signalsafejsonwriter.h"

#include "pal.h"

#include <fcntl.h>
#include <errno.h>
#include <unistd.h>
#include <string.h>
#include <time.h>
#include <ucontext.h>
#include <minipal/getexepath.h>
#include <minipal/thread.h>

// Include the .NET version string instead of linking because it is "static".
#if __has_include("_version.c")
#include "_version.c"
#else
static char sccsid[] = "@(#)Version N/A";
#endif

class ThreadEnumerationContext
{
public:
    ThreadEnumerationContext(
        SignalSafeJsonWriter* writer,
        void* signalContext)
        : m_writer(writer),
          m_signalContext(signalContext),
          m_threadCount(0),
          m_sawCrashThread(false)
    {
    }

    ThreadEnumerationContext(const ThreadEnumerationContext&) = delete;
    ThreadEnumerationContext& operator=(const ThreadEnumerationContext&) = delete;

    size_t ThreadCount() const { return m_threadCount; }
    bool SawCrashThread() const { return m_sawCrashThread; }
    SignalSafeJsonWriter* Writer() const { return m_writer; }

    void EnumerateThreads(InProcCrashReportEnumerateThreadsCallback callback, uint64_t crashingTid);

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

    SignalSafeJsonWriter* m_writer;
    void* m_signalContext;
    size_t m_threadCount;
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

    static bool WriteToFile(
        int fd,
        const char* buffer,
        size_t len);

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

    if (m_reportPath[0] == '\0' || !CrashReportHelpers::BuildReportPath(reportPath, sizeof(reportPath), m_reportPath, m_processName, m_hostName))
    {
        return;
    }

    int fd = open(reportPath, O_WRONLY | O_CREAT | O_TRUNC, 0600);
    if (fd == -1)
    {
        return;
    }

    (void)siginfo;

    CrashReportOutputContext outputContext(fd);

    m_jsonWriter.Init(&CrashReportOutputContext::ChunkCallback, &outputContext);

    m_jsonWriter.OpenObject();
    m_jsonWriter.OpenObject("payload");
    m_jsonWriter.WriteString("protocol_version", "1.0.0");

    m_jsonWriter.OpenObject("configuration");
#if defined(__x86_64__)
    m_jsonWriter.WriteString("architecture", "amd64");
#elif defined(__aarch64__)
    m_jsonWriter.WriteString("architecture", "arm64");
#elif defined(__arm__)
    m_jsonWriter.WriteString("architecture", "arm");
#endif
    char version[sizeof(sccsid)];
    CrashReportHelpers::GetVersionString(version, sizeof(version));
    m_jsonWriter.WriteString("version", version);
    m_jsonWriter.CloseObject(); // configuration

    if (m_processName[0] != '\0')
    {
        m_jsonWriter.WriteString("process_name", m_processName);
    }

    m_jsonWriter.WriteDecimalAsString("pid", static_cast<uint64_t>(GetCurrentProcessId()));

    m_jsonWriter.OpenArray("threads");
    if (m_enumerateThreadsCallback != nullptr)
    {
        ThreadEnumerationContext threadContext(&m_jsonWriter, context);
        uint64_t crashingTid = static_cast<uint64_t>(minipal_get_current_thread_id());

        threadContext.EnumerateThreads(m_enumerateThreadsCallback, crashingTid);

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

    m_jsonWriter.CloseObject(); // payload

    m_jsonWriter.OpenObject("parameters");
    m_jsonWriter.WriteSignedDecimalAsString("signal", static_cast<int64_t>(signal));
    m_jsonWriter.CloseObject(); // parameters

    m_jsonWriter.CloseObject(); // root

    if (fd != -1)
    {
        bool writeSucceeded = m_jsonWriter.Finish() &&
            !outputContext.WriteFailed() &&
            CrashReportHelpers::WriteToFile(fd, "\n", 1);

        if (close(fd) != 0 || !writeSucceeded)
        {
            unlink(reportPath);
        }
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
                if (SignalSafeJsonWriter::FormatUnsignedDecimal(numberBuf, sizeof(numberBuf), pid) == 0)
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
                if (SignalSafeJsonWriter::FormatUnsignedDecimal(
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
#if defined(__x86_64__)
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
#if defined(__x86_64__)
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
#if defined(__x86_64__)
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
    CrashReportHelpers::JsonFrameCallback(ip, stackPointer, methodName, className, moduleName, nativeOffset, token, ilOffset, moduleTimestamp, moduleSize, moduleGuid, m_writer);
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
ThreadEnumerationContext::OnThread(
    uint64_t osThreadId,
    bool isCrashThread,
    const char* exceptionType,
    uint32_t exceptionHResult)
{
    if (m_threadCount > 0)
    {
        m_writer->CloseArray(); // stack_frames
        m_writer->CloseObject(); // thread

        (void)m_writer->Flush();
    }

    if (isCrashThread)
    {
        m_sawCrashThread = true;
    }
    m_threadCount++;

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
    InProcCrashReportEnumerateThreadsCallback callback,
    uint64_t crashingTid)
{
    if (callback == nullptr)
    {
        return;
    }

    callback(crashingTid, &ThreadCallback, &FrameCallback, this);

    if (m_threadCount == 0)
    {
        return;
    }

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
    if (walkStack && m_walkStackCallback != nullptr)
    {
        m_walkStackCallback(&CrashReportHelpers::JsonFrameCallback, &m_jsonWriter);
    }
    m_jsonWriter.CloseArray(); // stack_frames
    m_jsonWriter.CloseObject(); // thread
}
