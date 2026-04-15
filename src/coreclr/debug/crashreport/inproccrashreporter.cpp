// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// In-proc crash report generator.
//
// Emits a createdump-shaped JSON skeleton to logcat / stderr.

#include "inproccrashreporter.h"
#include "crashjsonwriter.h"
#include "moduleenumerator.h"

#include <fcntl.h>
#include <errno.h>
#include <unistd.h>
#include <string.h>
#include <ucontext.h>

#ifdef __ANDROID__
#include <android/log.h>
#endif

#include <minipal/thread.h>

// Include the .NET version string instead of linking because it is "static".
#include "_version.c"

static CrashJsonWriter s_jsonWriter;
// These callbacks are published during runtime startup and then only read from
// the crash path; this minimal branch intentionally reuses the existing VM
// inspection hooks before the later strict-safety hardening slices.
static volatile InProcCrashReportIsManagedThreadCallback g_isManagedThreadCallback = NULL;
static volatile InProcCrashReportWalkStackCallback g_walkStackCallback = NULL;
static volatile InProcCrashReportGetExceptionCallback g_getExceptionCallback = NULL;
static volatile InProcCrashReportEnumerateThreadsCallback g_enumerateThreadsCallback = NULL;
static volatile int g_writeReportToFile = 0;
static char g_reportPath[256];
static char g_defaultReportDirectory[256];

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

static
void
WriteToLog(
    const char* msg,
    int len);

static
int
WriteAllToFile(
    int fd,
    const char* buffer,
    int len);

static
void
AppendChar(
    char* buffer,
    int bufferSize,
    int* pos,
    char value);

static
void
AppendString(
    char* buffer,
    int bufferSize,
    int* pos,
    const char* value);

static
void
AppendUnsignedDecimal(
    char* buffer,
    int bufferSize,
    int* pos,
    uint64_t value);

static
void
TerminateBuffer(
    char* buffer,
    int bufferSize,
    int* pos);

static
const char*
GetExceptionTypeCode(
    int signal);

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

    CrashJsonInit(&s_jsonWriter);

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
    if (CrashModulesTryGetProcessName(processName, sizeof(processName)))
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
    CrashJsonWriteString(&s_jsonWriter, "ExceptionType", hasException ? "0x05000000" : GetExceptionTypeCode(signal));
#ifdef __APPLE__
    CrashJsonWriteString(&s_jsonWriter, "OSVersion", "");
    CrashJsonWriteString(&s_jsonWriter, "SystemModel", "");
    CrashJsonWriteString(&s_jsonWriter, "SystemManufacturer", "apple");
#endif
    CrashJsonCloseObject(&s_jsonWriter);

    CrashJsonCloseObject(&s_jsonWriter);

    WriteToLog(CrashJsonGetBuffer(&s_jsonWriter), CrashJsonGetLength(&s_jsonWriter));

    if (g_writeReportToFile != 0)
    {
        char reportPath[256];
        int pathLen = 0;
        reportPath[0] = '\0';

        if (g_reportPath[0] != '\0')
        {
            AppendString(reportPath, sizeof(reportPath), &pathLen, g_reportPath);
            AppendString(reportPath, sizeof(reportPath), &pathLen, ".crashreport.json");
        }
        else
        {
            const char* directory = g_defaultReportDirectory[0] != '\0' ? g_defaultReportDirectory : "/tmp";

            AppendString(reportPath, sizeof(reportPath), &pathLen, directory);
            if (pathLen > 0 && reportPath[pathLen - 1] != '/')
            {
                AppendChar(reportPath, sizeof(reportPath), &pathLen, '/');
            }
            AppendString(reportPath, sizeof(reportPath), &pathLen, "dotnet_crash_");
            AppendUnsignedDecimal(reportPath, sizeof(reportPath), &pathLen, static_cast<uint64_t>(getpid()));
            AppendString(reportPath, sizeof(reportPath), &pathLen, ".crashreport.json");
        }

        TerminateBuffer(reportPath, sizeof(reportPath), &pathLen);

        int fd = open(reportPath, O_WRONLY | O_CREAT | O_TRUNC, 0644);
        if (fd != -1)
        {
            int writeSucceeded = WriteAllToFile(fd, CrashJsonGetBuffer(&s_jsonWriter), CrashJsonGetLength(&s_jsonWriter)) &&
                WriteAllToFile(fd, "\n", 1);

            if (close(fd) != 0 || !writeSucceeded)
            {
                unlink(reportPath);
            }
        }
    }
}

void
InProcCrashReportInitialize(
    int writeToFile,
    const char* dumpPath,
    const char* defaultDirectory)
{
    g_reportPath[0] = '\0';
    if (dumpPath != NULL)
    {
        int index = 0;
        while (dumpPath[index] != '\0' && index < static_cast<int>(sizeof(g_reportPath)) - 1)
        {
            g_reportPath[index] = dumpPath[index];
            index++;
        }
        g_reportPath[index] = '\0';
    }

    g_defaultReportDirectory[0] = '\0';
    if (defaultDirectory != NULL)
    {
        int index = 0;
        while (defaultDirectory[index] != '\0' && index < static_cast<int>(sizeof(g_defaultReportDirectory)) - 1)
        {
            g_defaultReportDirectory[index] = defaultDirectory[index];
            index++;
        }
        g_defaultReportDirectory[index] = '\0';
    }

    __sync_synchronize();
    g_writeReportToFile = writeToFile;
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

void
WriteToLog(
    const char* msg,
    int len)
{
#ifdef __ANDROID__
    if (msg == NULL)
    {
        return;
    }

    if (len < 0)
    {
        len = 0;
        while (msg[len] != '\0')
        {
            len++;
        }
    }

    // Emit long payloads in chunks so the JSON is not truncated by Android's
    // per-entry log size limit.
    int offset = 0;
    while (offset < len)
    {
        int chunk = len - offset;
        if (chunk > 3000)
        {
            chunk = 3000;
        }

        char buffer[3001];
        for (int i = 0; i < chunk; i++)
        {
            buffer[i] = msg[offset + i];
        }

        buffer[chunk] = '\0';
        // TODO-Async: Prefer Android's async_safe/log.h entrypoints here if they
        // become available through the supported NDK surface. __android_log_write
        // keeps the crash report visible in logcat, but it doesn't document an
        // async-signal-safe contract.
        __android_log_write(ANDROID_LOG_ERROR, "DOTNET", buffer);
        offset += chunk;
    }
#else
    write(STDERR_FILENO, msg, len);
#endif
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

void
AppendChar(
    char* buffer,
    int bufferSize,
    int* pos,
    char value)
{
    if (*pos < bufferSize - 1)
    {
        buffer[*pos] = value;
        (*pos)++;
    }
}

void
AppendString(
    char* buffer,
    int bufferSize,
    int* pos,
    const char* value)
{
    if (value == NULL)
    {
        return;
    }

    while (*value != '\0' && *pos < bufferSize - 1)
    {
        buffer[*pos] = *value;
        (*pos)++;
        value++;
    }
}

void
AppendUnsignedDecimal(
    char* buffer,
    int bufferSize,
    int* pos,
    uint64_t value)
{
    char reverse[32];
    int reversePos = 0;

    if (value == 0)
    {
        AppendChar(buffer, bufferSize, pos, '0');
        return;
    }

    while (value != 0 && reversePos < static_cast<int>(sizeof(reverse)))
    {
        reverse[reversePos++] = static_cast<char>('0' + (value % 10));
        value /= 10;
    }

    while (reversePos > 0)
    {
        AppendChar(buffer, bufferSize, pos, reverse[--reversePos]);
    }
}

void
TerminateBuffer(
    char* buffer,
    int bufferSize,
    int* pos)
{
    if (*pos >= bufferSize)
    {
        *pos = bufferSize - 1;
    }

    buffer[*pos] = '\0';
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

    int index = 0;
    while (version[index] != '\0' && index < bufferSize - 2)
    {
        buffer[index] = version[index];
        index++;
    }

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
    uint64_t moduleBase = 0;
    char moduleBaseBuffer[32];
    char moduleName[256];
    moduleName[0] = '\0';

    FormatHexValue(ip, sizeof(ip), ipValue);
    FormatHexValue(sp, sizeof(sp), spValue);

    CrashJsonOpenObject(writer, NULL);
    CrashJsonWriteString(writer, "is_managed", "false");
    CrashJsonWriteString(writer, "stack_pointer", sp);
    CrashJsonWriteString(writer, "native_address", ip);
    if (CrashModulesTryLookupModuleForAddress(ipValue, &moduleBase, moduleName, sizeof(moduleName)))
    {
        char imageOffset[32];
        FormatHexValue(moduleBaseBuffer, sizeof(moduleBaseBuffer), moduleBase);
        FormatHexValue(imageOffset, sizeof(imageOffset), ipValue - moduleBase);
        CrashJsonWriteString(writer, "module_address", moduleBaseBuffer);
        CrashJsonWriteString(writer, "native_image_offset", imageOffset);
        CrashJsonWriteString(writer, "native_module", moduleName);
    }
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

    int index = 0;
    if (className != NULL)
    {
        while (*className != '\0' && index < bufferSize - 1)
        {
            buffer[index++] = *className++;
        }
    }

    if (methodName != NULL)
    {
        if (index > 0 && index < bufferSize - 1)
        {
            buffer[index++] = '.';
        }

        while (*methodName != '\0' && index < bufferSize - 1)
        {
            buffer[index++] = *methodName++;
        }
    }

    buffer[index] = '\0';
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
    uint64_t moduleBase = 0;
    char nativeModuleName[256];
    nativeModuleName[0] = '\0';

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
    if (CrashModulesTryLookupModuleForAddress(ip, &moduleBase, nativeModuleName, sizeof(nativeModuleName)))
    {
        char moduleAddress[32];
        char nativeImageOffset[32];
        FormatHexValue(moduleAddress, sizeof(moduleAddress), moduleBase);
        FormatHexValue(nativeImageOffset, sizeof(nativeImageOffset), ip - moduleBase);
        CrashJsonWriteString(writer, "module_address", moduleAddress);
        CrashJsonWriteString(writer, "native_image_offset", nativeImageOffset);
    }

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
        if (nativeModuleName[0] != '\0')
        {
            CrashJsonWriteString(writer, "native_module", nativeModuleName);
        }
        else if (moduleName != NULL)
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

const char*
GetExceptionTypeCode(
    int signal)
{
    switch (signal)
    {
        case SIGSEGV:
            return "0x20000000";
        case SIGABRT:
            return "0x30000000";
        case SIGBUS:
            return "0x60000000";
        case SIGILL:
            return "0x50000000";
        case SIGFPE:
            return "0x70000000";
        case SIGTRAP:
            return "0x03000000";
        case SIGTERM:
            return "0x02000000";
        default:
            return "0x00000000";
    }
}
