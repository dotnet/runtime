// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// In-proc crash report generator.
//
// Streams a createdump-shaped JSON skeleton to a crashreport.json file.

#include "inproccrashreporter.h"
#include "crashjsonwriter.h"

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

static CrashJsonWriter s_jsonWriter;
// These callbacks are published during runtime startup and then only read from
// the crash path; this minimal branch intentionally reuses the existing VM
// inspection hooks before the later strict-safety hardening slices.
static volatile InProcCrashReportIsManagedThreadCallback g_isManagedThreadCallback = NULL;
static volatile InProcCrashReportWalkStackCallback g_walkStackCallback = NULL;
static volatile InProcCrashReportGetExceptionCallback g_getExceptionCallback = NULL;
static volatile InProcCrashReportEnumerateThreadsCallback g_enumerateThreadsCallback = NULL;
static char g_reportPath[256];

struct MultiThreadJsonContext
{
    CrashJsonWriter* writer;
    void* signalContext;
    int threadCount;
    int sawCrashThread;
    int hasCrashException;
    const char* crashExceptionType;
    uint32_t crashExceptionHResult;
};

struct CrashReportOutputContext
{
    int fd;
    int writeFailed;
};

static
void
GetVersionString(
    char* buffer,
    int bufferSize);

static
void
FormatHexValue(
    char* buffer,
    int bufferSize,
    uint64_t value);

static
void
WriteRegistersToJson(
    CrashJsonWriter* writer,
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
    CrashJsonWriter* writer,
    void* context);

static
void
BuildMethodName(
    char* buffer,
    int bufferSize,
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
    int bufferSize,
    const char* value);

static
int
TryGetProcessName(
    char* filename,
    int filenameLen);

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

static
void
JsonThreadFrameCallback(
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

static
void
JsonThreadCallback(
    uint64_t osThreadId,
    int isCrashThread,
    const char* exceptionType,
    uint32_t exceptionHResult,
    void* ctx);

int
WriteAllToFile(
    int fd,
    const char* buffer,
    int len);

static
int
WriteCrashReportChunk(
    const char* buffer,
    int len,
    void* ctx);

static
int
BuildReportPath(
    char* buffer,
    int bufferSize,
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
    char exMsgBuf[512];
    uint32_t exHresult = 0;
    exTypeBuf[0] = '\0';
    exMsgBuf[0] = '\0';

    int hasException = 0;
    if (g_getExceptionCallback != NULL && signal != SIGSEGV && signal != SIGBUS)
    {
        hasException = g_getExceptionCallback(exTypeBuf, sizeof(exTypeBuf), exMsgBuf, sizeof(exMsgBuf), &exHresult);
    }

    CrashReportOutputContext outputContext =
    {
        fd,
        0
    };

    CrashJsonInit(&s_jsonWriter, WriteCrashReportChunk, &outputContext);

    CrashJsonOpenObject(&s_jsonWriter, NULL);
    CrashJsonOpenObject(&s_jsonWriter, "payload");
    CrashJsonWriteString(&s_jsonWriter, "protocol_version", "1.0.0");

    CrashJsonOpenObject(&s_jsonWriter, "configuration");
#if defined(__x86_64__)
    CrashJsonWriteString(&s_jsonWriter, "architecture", "amd64");
#elif defined(__aarch64__)
    CrashJsonWriteString(&s_jsonWriter, "architecture", "arm64");
#elif defined(__arm__)
    CrashJsonWriteString(&s_jsonWriter, "architecture", "arm");
#endif
    char version[sizeof(sccsid) + 1];
    GetVersionString(version, sizeof(version));
    CrashJsonWriteString(&s_jsonWriter, "version", version);
    CrashJsonCloseObject(&s_jsonWriter);

    char processName[256];
    if (TryGetProcessName(processName, sizeof(processName)))
    {
        CrashJsonWriteString(&s_jsonWriter, "process_name", processName);
    }

    CrashJsonOpenArray(&s_jsonWriter, "threads");
    if (g_enumerateThreadsCallback != NULL)
    {
        MultiThreadJsonContext threadContext = { &s_jsonWriter, context, 0, 0, hasException, exTypeBuf, exHresult };
        uint64_t crashingTid = static_cast<uint64_t>(minipal_get_current_thread_id());

        g_enumerateThreadsCallback(crashingTid, JsonThreadCallback, JsonThreadFrameCallback, &threadContext);

        if (threadContext.threadCount > 0)
        {
            // Close the last thread's stack_frames + object opened by the
            // enumeration callback.
            CrashJsonCloseArray(&s_jsonWriter);
            CrashJsonCloseObject(&s_jsonWriter);

            // Flush the final thread so it reaches the crash report file
            // even if any later work (e.g. synthesizing a crash thread
            // fallback) hangs or faults.
            (void)CrashJsonFlush(&s_jsonWriter);
        }

        if (threadContext.threadCount == 0 || !threadContext.sawCrashThread)
        {
            CrashJsonOpenObject(&s_jsonWriter, NULL);
            CrashJsonWriteString(&s_jsonWriter, "is_managed",
                g_isManagedThreadCallback != NULL && g_isManagedThreadCallback() ? "true" : "false");
            CrashJsonWriteString(&s_jsonWriter, "crashed", "true");

            char nativeThreadId[32];
            FormatHexValue(nativeThreadId, sizeof(nativeThreadId), crashingTid);
            CrashJsonWriteString(&s_jsonWriter, "native_thread_id", nativeThreadId);

            if (hasException)
            {
                char hresultBuffer[32];
                FormatHexValue(hresultBuffer, sizeof(hresultBuffer), exHresult);

                CrashJsonWriteString(&s_jsonWriter, "managed_exception_type", exTypeBuf);
                CrashJsonWriteString(&s_jsonWriter, "managed_exception_hresult", hresultBuffer);
            }

            WriteRegistersToJson(&s_jsonWriter, context);
            CrashJsonOpenArray(&s_jsonWriter, "stack_frames");
            WriteCrashSiteFrameToJson(&s_jsonWriter, context);
            CrashJsonCloseArray(&s_jsonWriter);
            CrashJsonCloseObject(&s_jsonWriter);
        }
    }
    else
    {
        uint64_t crashingTid = static_cast<uint64_t>(minipal_get_current_thread_id());

        CrashJsonOpenObject(&s_jsonWriter, NULL);
        CrashJsonWriteString(&s_jsonWriter, "is_managed",
            g_isManagedThreadCallback != NULL && g_isManagedThreadCallback() ? "true" : "false");
        CrashJsonWriteString(&s_jsonWriter, "crashed", "true");

        char nativeThreadId[32];
        FormatHexValue(nativeThreadId, sizeof(nativeThreadId), crashingTid);
        CrashJsonWriteString(&s_jsonWriter, "native_thread_id", nativeThreadId);

        if (hasException)
        {
            char hresultBuffer[32];
            FormatHexValue(hresultBuffer, sizeof(hresultBuffer), exHresult);

            CrashJsonWriteString(&s_jsonWriter, "managed_exception_type", exTypeBuf);
            CrashJsonWriteString(&s_jsonWriter, "managed_exception_hresult", hresultBuffer);
        }

        WriteRegistersToJson(&s_jsonWriter, context);
        CrashJsonOpenArray(&s_jsonWriter, "stack_frames");
        WriteCrashSiteFrameToJson(&s_jsonWriter, context);
        if (g_walkStackCallback != NULL)
        {
            g_walkStackCallback(JsonFrameCallback, &s_jsonWriter);
        }
        CrashJsonCloseArray(&s_jsonWriter);
        CrashJsonCloseObject(&s_jsonWriter);
    }
    CrashJsonCloseArray(&s_jsonWriter);

    CrashJsonCloseObject(&s_jsonWriter);

    CrashJsonOpenObject(&s_jsonWriter, "parameters");
    char signalBuf[16];
    (void)snprintf(signalBuf, sizeof(signalBuf), "%d", signal);
    CrashJsonWriteString(&s_jsonWriter, "signal", signalBuf);
#ifdef __APPLE__
    CrashJsonWriteString(&s_jsonWriter, "OSVersion", "");
    CrashJsonWriteString(&s_jsonWriter, "SystemModel", "");
    CrashJsonWriteString(&s_jsonWriter, "SystemManufacturer", "apple");
#endif
    CrashJsonCloseObject(&s_jsonWriter);

    CrashJsonCloseObject(&s_jsonWriter);
    CrashJsonFinish(&s_jsonWriter);

    if (fd != -1)
    {
        int writeSucceeded = !CrashJsonHasFailed(&s_jsonWriter) &&
            outputContext.writeFailed == 0 &&
            WriteAllToFile(fd, "\n", 1);

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

int
WriteAllToFile(
    int fd,
    const char* buffer,
    int len)
{
    int totalWritten = 0;
    while (totalWritten < len)
    {
        ssize_t written = write(fd, buffer + totalWritten, len - totalWritten);
        if (written > 0)
        {
            totalWritten += static_cast<int>(written);
            continue;
        }

        if (written == -1 && errno == EINTR)
        {
            continue;
        }

        return 0;
    }

    return 1;
}

int
WriteCrashReportChunk(
    const char* buffer,
    int len,
    void* ctx)
{
    CrashReportOutputContext* outputContext = reinterpret_cast<CrashReportOutputContext*>(ctx);
    if (outputContext == NULL || outputContext->fd == -1)
    {
        return 0;
    }

    if (!WriteAllToFile(outputContext->fd, buffer, len))
    {
        outputContext->writeFailed = 1;
        return 0;
    }

    return 1;
}

// Expand a subset of the coredump template patterns used by createdump's
// FormatDumpName: %%  %p  %d (PID).  Other specifiers are passed through
// literally since the remaining createdump patterns (%e, %h, %t) are not
// meaningful for in-proc crash reports.
static
int
ExpandDumpTemplate(
    char* buffer,
    int bufferSize,
    const char* pattern)
{
    if (buffer == NULL || bufferSize <= 0 || pattern == NULL)
    {
        return 0;
    }

    int pos = 0;
    unsigned pid = static_cast<unsigned>(getpid());

    while (*pattern != '\0' && pos < bufferSize - 1)
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
                if (pidLen > 0 && pos + pidLen < bufferSize)
                {
                    memcpy(buffer + pos, pidBuf, static_cast<size_t>(pidLen));
                    pos += pidLen;
                }
            }
            else
            {
                // Unknown specifier — pass through literally.
                if (pos < bufferSize - 1)
                {
                    buffer[pos++] = '%';
                }
                if (*pattern != '\0' && pos < bufferSize - 1)
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

int
BuildReportPath(
    char* buffer,
    int bufferSize,
    const char* dumpPath)
{
    if (buffer == NULL || bufferSize <= 0 || dumpPath == NULL || dumpPath[0] == '\0')
    {
        return 0;
    }

    char expanded[256];
    int expandedLen = ExpandDumpTemplate(expanded, sizeof(expanded), dumpPath);
    if (expandedLen <= 0)
    {
        return 0;
    }

    int written = snprintf(buffer, static_cast<size_t>(bufferSize), "%s.crashreport.json", expanded);
    return written > 0 && written < bufferSize;
}

void
GetVersionString(
    char* buffer,
    int bufferSize)
{
    if (buffer == NULL || bufferSize <= 0)
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

    size_t copied = strnlen(version, static_cast<size_t>(bufferSize - 2));
    if (copied != 0)
    {
        memcpy(buffer, version, copied);
    }

    int index = static_cast<int>(copied);
    buffer[index++] = ' ';
    buffer[index] = '\0';
}

void
FormatHexValue(
    char* buffer,
    int bufferSize,
    uint64_t value)
{
    if (buffer == NULL || bufferSize <= 0)
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
    int reverseLength = 0;
    do
    {
        int digit = static_cast<int>(value & 0xf);
        reverse[reverseLength++] = static_cast<char>(digit < 10 ? ('0' + digit) : ('a' + digit - 10));
        value >>= 4;
    } while (value != 0 && reverseLength < static_cast<int>(sizeof(reverse)));

    int index = 2;
    while (reverseLength > 0 && index < bufferSize - 1)
    {
        buffer[index++] = reverse[--reverseLength];
    }
    buffer[index] = '\0';
}

void
WriteRegistersToJson(
    CrashJsonWriter* writer,
    void* context)
{
    // Only the crashing thread has a reliable signal context in this slice.
    uint64_t ipValue = GetInstructionPointer(context);
    uint64_t spValue = GetStackPointer(context);
    char ip[32] = "0x0";
    char sp[32] = "0x0";
    char bp[32] = "0x0";

    FormatHexValue(ip, sizeof(ip), ipValue);
    FormatHexValue(sp, sizeof(sp), spValue);

    if (context != NULL)
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

    CrashJsonOpenObject(writer, "ctx");
    CrashJsonWriteString(writer, "IP", ip);
    CrashJsonWriteString(writer, "SP", sp);
    CrashJsonWriteString(writer, "BP", bp);
    CrashJsonCloseObject(writer);
}

uint64_t
GetInstructionPointer(
    void* context)
{
    if (context == NULL)
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
    if (context == NULL)
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
    CrashJsonWriter* writer,
    void* context)
{
    uint64_t ipValue = GetInstructionPointer(context);
    uint64_t spValue = GetStackPointer(context);
    char ip[32] = "0x0";
    char sp[32] = "0x0";

    FormatHexValue(ip, sizeof(ip), ipValue);
    FormatHexValue(sp, sizeof(sp), spValue);

    CrashJsonOpenObject(writer, NULL);
    CrashJsonWriteString(writer, "is_managed", "false");
    CrashJsonWriteString(writer, "stack_pointer", sp);
    CrashJsonWriteString(writer, "native_address", ip);
    CrashJsonCloseObject(writer);
}

void
BuildMethodName(
    char* buffer,
    int bufferSize,
    const char* className,
    const char* methodName)
{
    if (buffer == NULL || bufferSize <= 0)
    {
        return;
    }

    if (className != NULL && methodName != NULL)
    {
        (void)snprintf(buffer, static_cast<size_t>(bufferSize), "%s.%s", className, methodName);
    }
    else if (className != NULL)
    {
        (void)snprintf(buffer, static_cast<size_t>(bufferSize), "%s", className);
    }
    else if (methodName != NULL)
    {
        (void)snprintf(buffer, static_cast<size_t>(bufferSize), "%s", methodName);
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
    int bufferSize,
    const char* value)
{
    if (buffer == NULL || bufferSize <= 0)
    {
        return;
    }

    if (value == NULL)
    {
        buffer[0] = '\0';
        return;
    }

    size_t copied = strnlen(value, static_cast<size_t>(bufferSize - 1));
    if (copied != 0)
    {
        memcpy(buffer, value, copied);
    }

    buffer[copied] = '\0';
}

int
TryGetProcessName(
    char* filename,
    int filenameLen)
{
    if (filename == NULL || filenameLen <= 0)
    {
        return 0;
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
                return 1;
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

    return 0;
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
    CrashJsonWriter* writer = reinterpret_cast<CrashJsonWriter*>(ctx);
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

    CrashJsonOpenObject(writer, NULL);
    CrashJsonWriteString(writer, "stack_pointer", stackPointerBuffer);
    CrashJsonWriteString(writer, "native_address", ipBuffer);
    CrashJsonWriteString(writer, "native_offset", nativeOffsetBuffer);

    if (methodName != NULL)
    {
        char fullName[256];
        BuildMethodName(fullName, sizeof(fullName), className, methodName);
        CrashJsonWriteString(writer, "method_name", fullName);
        CrashJsonWriteString(writer, "is_managed", "true");
        CrashJsonWriteString(writer, "token", tokenBuffer);
        CrashJsonWriteString(writer, "il_offset", ilOffsetBuffer);
        if (moduleName != NULL)
        {
            CrashJsonWriteString(writer, "filename", moduleName);
        }
        if (moduleTimestamp != 0)
        {
            CrashJsonWriteString(writer, "timestamp", moduleTimestampBuffer);
        }
        if (moduleSize != 0)
        {
            CrashJsonWriteString(writer, "sizeofimage", moduleSizeBuffer);
        }
        if (moduleGuid != NULL && moduleGuid[0] != '\0')
        {
            CrashJsonWriteString(writer, "guid", moduleGuid);
        }
    }
    else
    {
        CrashJsonWriteString(writer, "is_managed", "false");
        if (moduleName != NULL)
        {
            CrashJsonWriteString(writer, "native_module", moduleName);
        }
    }

    CrashJsonCloseObject(writer);
}

void
JsonThreadFrameCallback(
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
    MultiThreadJsonContext* threadContext = reinterpret_cast<MultiThreadJsonContext*>(ctx);
    JsonFrameCallback(ip, stackPointer, methodName, className, moduleName, nativeOffset, token, ilOffset, moduleTimestamp, moduleSize, moduleGuid, threadContext->writer);
}

void
JsonThreadCallback(
    uint64_t osThreadId,
    int isCrashThread,
    const char* exceptionType,
    uint32_t exceptionHResult,
    void* ctx)
{
    MultiThreadJsonContext* threadContext = reinterpret_cast<MultiThreadJsonContext*>(ctx);
    if (threadContext->threadCount > 0)
    {
        CrashJsonCloseArray(threadContext->writer);
        CrashJsonCloseObject(threadContext->writer);

        (void)CrashJsonFlush(threadContext->writer);
    }

    if (isCrashThread)
    {
        threadContext->sawCrashThread = 1;
    }
    threadContext->threadCount++;

    CrashJsonOpenObject(threadContext->writer, NULL);
    CrashJsonWriteString(threadContext->writer, "is_managed", "true");
    CrashJsonWriteString(threadContext->writer, "crashed", isCrashThread ? "true" : "false");

    char nativeThreadId[32];
    FormatHexValue(nativeThreadId, sizeof(nativeThreadId), osThreadId);
    CrashJsonWriteString(threadContext->writer, "native_thread_id", nativeThreadId);

    if (isCrashThread && threadContext->hasCrashException)
    {
        char hresultBuffer[32];
        FormatHexValue(hresultBuffer, sizeof(hresultBuffer), threadContext->crashExceptionHResult);

        CrashJsonWriteString(threadContext->writer, "managed_exception_type", threadContext->crashExceptionType);
        CrashJsonWriteString(threadContext->writer, "managed_exception_hresult", hresultBuffer);
    }
    else if (exceptionType != NULL && exceptionType[0] != '\0')
    {
        char hresultBuffer[32];
        FormatHexValue(hresultBuffer, sizeof(hresultBuffer), exceptionHResult);

        CrashJsonWriteString(threadContext->writer, "managed_exception_type", exceptionType);
        CrashJsonWriteString(threadContext->writer, "managed_exception_hresult", hresultBuffer);
    }

    if (isCrashThread)
    {
        WriteRegistersToJson(threadContext->writer, threadContext->signalContext);
    }

    CrashJsonOpenArray(threadContext->writer, "stack_frames");
    if (isCrashThread)
    {
        WriteCrashSiteFrameToJson(threadContext->writer, threadContext->signalContext);
    }
}
