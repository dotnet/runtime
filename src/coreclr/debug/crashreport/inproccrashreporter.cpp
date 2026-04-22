// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// In-proc crash report generator.
//
// Streams a createdump-shaped JSON skeleton to a crashreport.json file.

#include "inproccrashreporter.h"
#include "signalsafejsonwriter.h"

#include <fcntl.h>
#include <errno.h>
#include <stdio.h>
#include <unistd.h>
#include <string.h>
#include <ucontext.h>
#include <minipal/thread.h>

// Include the .NET version string instead of linking because it is "static".
#if __has_include("_version.c")
#include "_version.c"
#else
static char sccsid[] = "@(#)Version N/A";
#endif

static SignalSafeJsonWriter s_jsonWriter;
static volatile InProcCrashReportIsManagedThreadCallback g_isManagedThreadCallback = nullptr;
static volatile InProcCrashReportWalkStackCallback g_walkStackCallback = nullptr;
static volatile InProcCrashReportGetExceptionCallback g_getExceptionCallback = nullptr;
static volatile InProcCrashReportEnumerateThreadsCallback g_enumerateThreadsCallback = nullptr;
static char g_reportPath[256];

class ThreadEnumerationContext
{
public:
    ThreadEnumerationContext(
        SignalSafeJsonWriter* writer,
        void* signalContext,
        bool hasCrashException,
        const char* crashExceptionType,
        uint32_t crashExceptionHResult)
        : m_writer(writer),
          m_signalContext(signalContext),
          m_threadCount(0),
          m_sawCrashThread(false),
          m_hasCrashException(hasCrashException),
          m_crashExceptionType(crashExceptionType),
          m_crashExceptionHResult(crashExceptionHResult)
    {
    }

    ThreadEnumerationContext(const ThreadEnumerationContext&) = delete;
    ThreadEnumerationContext& operator=(const ThreadEnumerationContext&) = delete;

    size_t ThreadCount() const { return m_threadCount; }
    bool SawCrashThread() const { return m_sawCrashThread; }
    SignalSafeJsonWriter* Writer() const { return m_writer; }

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
    SignalSafeJsonWriter* m_writer;
    void* m_signalContext;
    size_t m_threadCount;
    bool m_sawCrashThread;
    bool m_hasCrashException;
    const char* m_crashExceptionType;
    uint32_t m_crashExceptionHResult;
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

    bool HandleChunk(const char* buffer, size_t len);

    static bool ChunkCallback(const char* buffer, size_t len, void* ctx);

private:
    int m_fd;
    bool m_writeFailed;
};

static
void
GetVersionString(
    char* buffer,
    size_t bufferSize);

static
void
FormatHexValue(
    char* buffer,
    size_t bufferSize,
    uint64_t value);

static
void
WriteRegistersToJson(
    SignalSafeJsonWriter* writer,
    void* context);

static
uint64_t
GetInstructionPointer(
    void* context);

static
uint64_t
GetStackPointer(
    void* context);

static
void
WriteCrashSiteFrameToJson(
    SignalSafeJsonWriter* writer,
    void* context);

static
void
BuildMethodName(
    char* buffer,
    size_t bufferSize,
    const char* className,
    const char* methodName);

static
const char*
GetFilename(
    const char* path);

static
void
CopyString(
    char* buffer,
    size_t bufferSize,
    const char* value);

static
bool
TryGetProcessName(
    char* filename,
    size_t filenameLen);

static
void
JsonFrameCallback(
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

bool
WriteToFile(
    int fd,
    const char* buffer,
    size_t len);

static
bool
BuildReportPath(
    char* buffer,
    size_t bufferSize,
    const char* dumpPath);

void
InProcCrashReportGenerate(
    int signal,
    siginfo_t* siginfo,
    void* context)
{
    static volatile int s_generating = 0;
    if (__sync_val_compare_and_swap(&s_generating, 0, 1) != 0)
    {
        return;
    }

    char reportPath[256];
    reportPath[0] = '\0';

    if (g_reportPath[0] == '\0' || !BuildReportPath(reportPath, sizeof(reportPath), g_reportPath))
    {
        return;
    }

    int fd = open(reportPath, O_WRONLY | O_CREAT | O_TRUNC, 0600);
    if (fd == -1)
    {
        return;
    }

    (void)siginfo;

    char exTypeBuf[256];
    uint32_t exHresult = 0;
    exTypeBuf[0] = '\0';

    bool hasException = false;
    if (g_getExceptionCallback != nullptr && signal != SIGSEGV && signal != SIGBUS)
    {
        hasException = g_getExceptionCallback(exTypeBuf, sizeof(exTypeBuf), &exHresult);
    }

    CrashReportOutputContext outputContext(fd);

    s_jsonWriter.Init(&CrashReportOutputContext::ChunkCallback, &outputContext);

    s_jsonWriter.OpenObject(nullptr);
    s_jsonWriter.OpenObject("payload");
    s_jsonWriter.WriteString("protocol_version", "1.0.0");

    s_jsonWriter.OpenObject("configuration");
#if defined(__x86_64__)
    s_jsonWriter.WriteString("architecture", "amd64");
#elif defined(__aarch64__)
    s_jsonWriter.WriteString("architecture", "arm64");
#elif defined(__arm__)
    s_jsonWriter.WriteString("architecture", "arm");
#endif
    char version[sizeof(sccsid) + 1];
    GetVersionString(version, sizeof(version));
    s_jsonWriter.WriteString("version", version);
    s_jsonWriter.CloseObject(); // configuration

    char processName[256];
    if (TryGetProcessName(processName, sizeof(processName)))
    {
        s_jsonWriter.WriteString("process_name", processName);
    }

    char pidBuf[16];
    (void)snprintf(pidBuf, sizeof(pidBuf), "%u", static_cast<unsigned>(getpid()));
    s_jsonWriter.WriteString("pid", pidBuf);

    s_jsonWriter.OpenArray("threads");
    if (g_enumerateThreadsCallback != nullptr)
    {
        ThreadEnumerationContext threadContext(&s_jsonWriter, context, hasException, exTypeBuf, exHresult);
        uint64_t crashingTid = static_cast<uint64_t>(minipal_get_current_thread_id());

        g_enumerateThreadsCallback(crashingTid, &ThreadEnumerationContext::ThreadCallback, &ThreadEnumerationContext::FrameCallback, &threadContext);

        if (threadContext.ThreadCount() > 0)
        {
            // Close the last thread's stack_frames + object opened by the
            // enumeration callback.
            s_jsonWriter.CloseArray(); // stack_frames
            s_jsonWriter.CloseObject(); // thread

            // Flush the final thread so it reaches the crash report file
            // even if any later work (e.g. synthesizing a crash thread
            // fallback) hangs or faults.
            (void)s_jsonWriter.Flush();
        }

        if (threadContext.ThreadCount() == 0 || !threadContext.SawCrashThread())
        {
            s_jsonWriter.OpenObject(nullptr);
            s_jsonWriter.WriteString("is_managed",
                g_isManagedThreadCallback != nullptr && g_isManagedThreadCallback() ? "true" : "false");
            s_jsonWriter.WriteString("crashed", "true");

            char nativeThreadId[32];
            FormatHexValue(nativeThreadId, sizeof(nativeThreadId), crashingTid);
            s_jsonWriter.WriteString("native_thread_id", nativeThreadId);

            if (hasException)
            {
                char hresultBuffer[32];
                FormatHexValue(hresultBuffer, sizeof(hresultBuffer), exHresult);

                s_jsonWriter.WriteString("managed_exception_type", exTypeBuf);
                s_jsonWriter.WriteString("managed_exception_hresult", hresultBuffer);
            }

            WriteRegistersToJson(&s_jsonWriter, context);
            s_jsonWriter.OpenArray("stack_frames");
            WriteCrashSiteFrameToJson(&s_jsonWriter, context);
            s_jsonWriter.CloseArray(); // stack_frames
            s_jsonWriter.CloseObject(); // thread
        }
    }
    else
    {
        uint64_t crashingTid = static_cast<uint64_t>(minipal_get_current_thread_id());

        s_jsonWriter.OpenObject(nullptr);
        s_jsonWriter.WriteString("is_managed",
            g_isManagedThreadCallback != nullptr && g_isManagedThreadCallback() ? "true" : "false");
        s_jsonWriter.WriteString("crashed", "true");

        char nativeThreadId[32];
        FormatHexValue(nativeThreadId, sizeof(nativeThreadId), crashingTid);
        s_jsonWriter.WriteString("native_thread_id", nativeThreadId);

        if (hasException)
        {
            char hresultBuffer[32];
            FormatHexValue(hresultBuffer, sizeof(hresultBuffer), exHresult);

            s_jsonWriter.WriteString("managed_exception_type", exTypeBuf);
            s_jsonWriter.WriteString("managed_exception_hresult", hresultBuffer);
        }

        WriteRegistersToJson(&s_jsonWriter, context);
        s_jsonWriter.OpenArray("stack_frames");
        WriteCrashSiteFrameToJson(&s_jsonWriter, context);
        if (g_walkStackCallback != nullptr)
        {
            g_walkStackCallback(JsonFrameCallback, &s_jsonWriter);
        }
        s_jsonWriter.CloseArray(); // stack_frames
        s_jsonWriter.CloseObject(); // thread
    }
    s_jsonWriter.CloseArray(); // threads

    s_jsonWriter.CloseObject(); // payload

    s_jsonWriter.OpenObject("parameters");
    char signalBuf[16];
    (void)snprintf(signalBuf, sizeof(signalBuf), "%d", signal);
    s_jsonWriter.WriteString("signal", signalBuf);
#ifdef __APPLE__
    s_jsonWriter.WriteString("OSVersion", "");
    s_jsonWriter.WriteString("SystemModel", "");
    s_jsonWriter.WriteString("SystemManufacturer", "apple");
#endif
    s_jsonWriter.CloseObject(); // parameters

    s_jsonWriter.CloseObject(); // root
    s_jsonWriter.Finish();

    if (fd != -1)
    {
        bool writeSucceeded = !s_jsonWriter.HasError() &&
            !outputContext.WriteFailed() &&
            WriteToFile(fd, "\n", 1);

        if (close(fd) != 0 || !writeSucceeded)
        {
            unlink(reportPath);
        }
    }
}

void
InProcCrashReportInitialize(
    const char* dumpPath)
{
    CopyString(g_reportPath, sizeof(g_reportPath), dumpPath);
    __sync_synchronize();
}

void
InProcCrashReportSetCurrentThreadManagedResolver(
    InProcCrashReportIsManagedThreadCallback callback)
{
    g_isManagedThreadCallback = callback;
}

void
InProcCrashReportSetStackWalker(
    InProcCrashReportWalkStackCallback callback)
{
    g_walkStackCallback = callback;
}

void
InProcCrashReportSetExceptionResolver(
    InProcCrashReportGetExceptionCallback callback)
{
    g_getExceptionCallback = callback;
}

void
InProcCrashReportSetThreadEnumerator(
    InProcCrashReportEnumerateThreadsCallback callback)
{
    g_enumerateThreadsCallback = callback;
}

bool
WriteToFile(
    int fd,
    const char* buffer,
    size_t len)
{
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

    if (!WriteToFile(m_fd, buffer, len))
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

// Expand a subset of the coredump template patterns used by createdump's
// FormatDumpName: %%  %p  %d (PID).  Other specifiers are passed through
// literally since the remaining createdump patterns (%e, %h, %t) are not
// meaningful for in-proc crash reports.
static
size_t
ExpandDumpTemplate(
    char* buffer,
    size_t bufferSize,
    const char* pattern)
{
    if (buffer == nullptr || bufferSize == 0 || pattern == nullptr)
    {
        return 0;
    }

    size_t pos = 0;
    unsigned pid = static_cast<unsigned>(getpid());

    while (*pattern != '\0' && pos + 1 < bufferSize)
    {
        if (*pattern == '%')
        {
            pattern++;
            if (*pattern == '%')
            {
                buffer[pos++] = '%';
            }
            else if (*pattern == 'p' || *pattern == 'd')
            {
                char pidBuf[16];
                int pidLen = snprintf(pidBuf, sizeof(pidBuf), "%u", pid);
                if (pidLen > 0 && pos + static_cast<size_t>(pidLen) < bufferSize)
                {
                    memcpy(buffer + pos, pidBuf, static_cast<size_t>(pidLen));
                    pos += static_cast<size_t>(pidLen);
                }
            }
            else
            {
                // Unknown specifier — pass through literally.
                if (pos + 1 < bufferSize)
                {
                    buffer[pos++] = '%';
                }
                if (*pattern != '\0' && pos + 1 < bufferSize)
                {
                    buffer[pos++] = *pattern;
                }
            }

            if (*pattern != '\0')
            {
                pattern++;
            }
        }
        else
        {
            buffer[pos++] = *pattern++;
        }
    }

    buffer[pos] = '\0';
    return pos;
}

bool
BuildReportPath(
    char* buffer,
    size_t bufferSize,
    const char* dumpPath)
{
    if (buffer == nullptr || bufferSize == 0 || dumpPath == nullptr || dumpPath[0] == '\0')
    {
        return false;
    }

    char expanded[256];
    size_t expandedLen = ExpandDumpTemplate(expanded, sizeof(expanded), dumpPath);
    if (expandedLen == 0)
    {
        return false;
    }

    int written = snprintf(buffer, bufferSize, "%s.crashreport.json", expanded);
    return written > 0 && static_cast<size_t>(written) < bufferSize;
}

void
GetVersionString(
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

    size_t copied = strnlen(version, bufferSize - 2);
    if (copied != 0)
    {
        memcpy(buffer, version, copied);
    }

    buffer[copied] = ' ';
    buffer[copied + 1] = '\0';
}

void
FormatHexValue(
    char* buffer,
    size_t bufferSize,
    uint64_t value)
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

    buffer[0] = '0';
    if (bufferSize == 2)
    {
        buffer[1] = '\0';
        return;
    }

    buffer[1] = 'x';

    char reverse[16];
    size_t reverseLength = 0;
    do
    {
        unsigned digit = static_cast<unsigned>(value & 0xf);
        reverse[reverseLength++] = static_cast<char>(digit < 10 ? ('0' + digit) : ('a' + digit - 10));
        value >>= 4;
    } while (value != 0 && reverseLength < sizeof(reverse));

    size_t index = 2;
    while (reverseLength > 0 && index + 1 < bufferSize)
    {
        buffer[index++] = reverse[--reverseLength];
    }
    buffer[index] = '\0';
}

void
WriteRegistersToJson(
    SignalSafeJsonWriter* writer,
    void* context)
{
    uint64_t ipValue = GetInstructionPointer(context);
    uint64_t spValue = GetStackPointer(context);
    char ip[32] = "0x0";
    char sp[32] = "0x0";
    char bp[32] = "0x0";

    FormatHexValue(ip, sizeof(ip), ipValue);
    FormatHexValue(sp, sizeof(sp), spValue);

    if (context != nullptr)
    {
        ucontext_t* ucontext = reinterpret_cast<ucontext_t*>(context);
#if defined(__x86_64__)
        FormatHexValue(bp, sizeof(bp), static_cast<uint64_t>(ucontext->uc_mcontext.gregs[REG_RBP]));
#elif defined(__aarch64__)
        FormatHexValue(bp, sizeof(bp), static_cast<uint64_t>(ucontext->uc_mcontext.regs[29]));
#elif defined(__arm__)
        FormatHexValue(bp, sizeof(bp), static_cast<uint64_t>(ucontext->uc_mcontext.arm_fp));
#endif
    }

    writer->OpenObject("ctx");
    writer->WriteString("IP", ip);
    writer->WriteString("SP", sp);
    writer->WriteString("BP", bp);
    writer->CloseObject(); // ctx
}

uint64_t
GetInstructionPointer(
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
GetStackPointer(
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

void
WriteCrashSiteFrameToJson(
    SignalSafeJsonWriter* writer,
    void* context)
{
    uint64_t ipValue = GetInstructionPointer(context);
    uint64_t spValue = GetStackPointer(context);
    char ip[32] = "0x0";
    char sp[32] = "0x0";

    FormatHexValue(ip, sizeof(ip), ipValue);
    FormatHexValue(sp, sizeof(sp), spValue);

    writer->OpenObject(nullptr);
    writer->WriteString("is_managed", "false");
    writer->WriteString("stack_pointer", sp);
    writer->WriteString("native_address", ip);
    writer->CloseObject(); // frame
}

void
BuildMethodName(
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
        (void)snprintf(buffer, bufferSize, "%s.%s", className, methodName);
    }
    else if (className != nullptr)
    {
        (void)snprintf(buffer, bufferSize, "%s", className);
    }
    else if (methodName != nullptr)
    {
        (void)snprintf(buffer, bufferSize, "%s", methodName);
    }
    else
    {
        buffer[0] = '\0';
    }
}

const char*
GetFilename(
    const char* path)
{
    const char* last = path;
    for (const char* p = path; *p != '\0'; p++)
    {
        if (*p == '/')
        {
            last = p + 1;
        }
    }

    return last;
}

void
CopyString(
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

    size_t copied = strnlen(value, bufferSize - 1);
    if (copied != 0)
    {
        memcpy(buffer, value, copied);
    }

    buffer[copied] = '\0';
}

bool
TryGetProcessName(
    char* filename,
    size_t filenameLen)
{
    if (filename == nullptr || filenameLen == 0)
    {
        return false;
    }

    filename[0] = '\0';

    int fd = open("/proc/self/cmdline", O_RDONLY);
    if (fd != -1)
    {
        char cmdline[256];
        ssize_t bytesRead = read(fd, cmdline, sizeof(cmdline) - 1);
        close(fd);

        if (bytesRead > 0)
        {
            cmdline[bytesRead] = '\0';
            CopyString(filename, filenameLen, GetFilename(cmdline));
            if (filename[0] != '\0')
            {
                return true;
            }
        }
    }

    char exePath[256];
    ssize_t pathLength = readlink("/proc/self/exe", exePath, sizeof(exePath) - 1);
    if (pathLength > 0)
    {
        exePath[pathLength] = '\0';
        CopyString(filename, filenameLen, GetFilename(exePath));
        return filename[0] != '\0';
    }

    return false;
}

void
JsonFrameCallback(
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
    char ipBuffer[32];
    char stackPointerBuffer[32];
    char nativeOffsetBuffer[32];
    char tokenBuffer[32];
    char ilOffsetBuffer[32];
    char moduleTimestampBuffer[32];
    char moduleSizeBuffer[32];

    FormatHexValue(ipBuffer, sizeof(ipBuffer), ip);
    FormatHexValue(stackPointerBuffer, sizeof(stackPointerBuffer), stackPointer);
    FormatHexValue(nativeOffsetBuffer, sizeof(nativeOffsetBuffer), nativeOffset);
    FormatHexValue(tokenBuffer, sizeof(tokenBuffer), token);
    FormatHexValue(ilOffsetBuffer, sizeof(ilOffsetBuffer), ilOffset);
    FormatHexValue(moduleTimestampBuffer, sizeof(moduleTimestampBuffer), moduleTimestamp);
    FormatHexValue(moduleSizeBuffer, sizeof(moduleSizeBuffer), moduleSize);

    writer->OpenObject(nullptr);
    writer->WriteString("stack_pointer", stackPointerBuffer);
    writer->WriteString("native_address", ipBuffer);
    writer->WriteString("native_offset", nativeOffsetBuffer);

    if (methodName != nullptr)
    {
        char fullName[256];
        BuildMethodName(fullName, sizeof(fullName), className, methodName);
        writer->WriteString("method_name", fullName);
        writer->WriteString("is_managed", "true");
        writer->WriteString("token", tokenBuffer);
        writer->WriteString("il_offset", ilOffsetBuffer);
        if (moduleName != nullptr)
        {
            writer->WriteString("filename", moduleName);
        }
        if (moduleTimestamp != 0)
        {
            writer->WriteString("timestamp", moduleTimestampBuffer);
        }
        if (moduleSize != 0)
        {
            writer->WriteString("sizeofimage", moduleSizeBuffer);
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
    JsonFrameCallback(ip, stackPointer, methodName, className, moduleName, nativeOffset, token, ilOffset, moduleTimestamp, moduleSize, moduleGuid, m_writer);
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

    m_writer->OpenObject(nullptr);
    m_writer->WriteString("is_managed", "true");
    m_writer->WriteString("crashed", isCrashThread ? "true" : "false");

    char nativeThreadId[32];
    FormatHexValue(nativeThreadId, sizeof(nativeThreadId), osThreadId);
    m_writer->WriteString("native_thread_id", nativeThreadId);

    if (isCrashThread && m_hasCrashException)
    {
        char hresultBuffer[32];
        FormatHexValue(hresultBuffer, sizeof(hresultBuffer), m_crashExceptionHResult);

        m_writer->WriteString("managed_exception_type", m_crashExceptionType);
        m_writer->WriteString("managed_exception_hresult", hresultBuffer);
    }
    else if (exceptionType != nullptr && exceptionType[0] != '\0')
    {
        char hresultBuffer[32];
        FormatHexValue(hresultBuffer, sizeof(hresultBuffer), exceptionHResult);

        m_writer->WriteString("managed_exception_type", exceptionType);
        m_writer->WriteString("managed_exception_hresult", hresultBuffer);
    }

    if (isCrashThread)
    {
        WriteRegistersToJson(m_writer, m_signalContext);
    }

    m_writer->OpenArray("stack_frames");
    if (isCrashThread)
    {
        WriteCrashSiteFrameToJson(m_writer, m_signalContext);
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
    reinterpret_cast<ThreadEnumerationContext*>(ctx)->OnThread(osThreadId, isCrashThread, exceptionType, exceptionHResult);
}
