// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// In-proc crash report generator.
//
// Emits a createdump-shaped JSON skeleton to logcat / stderr.

#include "inproccrashreporter.h"
#include "crashjsonwriter.h"

#include <unistd.h>

#ifdef __ANDROID__
#include <android/log.h>
#endif

static CrashJsonWriter s_jsonWriter;

static
void
WriteToLog(
    const char* msg,
    int len);

void
InProcCrashReportGenerate(
    int signal,
    siginfo_t* siginfo,
    void* context)
{
    (void)signal;
    (void)siginfo;
    (void)context;

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
    CrashJsonWriteString(&s_jsonWriter, "version", "");
    CrashJsonCloseObject(&s_jsonWriter);

    CrashJsonWriteString(&s_jsonWriter, "process_name", "");

    CrashJsonOpenArray(&s_jsonWriter, "threads");
    // TODO: Replace with actual thread enumeration.
    int threadCount = 0;
    for (int threadIndex = 0; threadIndex < 0; threadIndex++)
    {
        CrashJsonOpenObject(&s_jsonWriter, NULL);
        CrashJsonWriteString(&s_jsonWriter, "is_managed", "false");
        CrashJsonWriteString(&s_jsonWriter, "crashed", "false");
        CrashJsonWriteString(&s_jsonWriter, "native_thread_id", "0x0");

        CrashJsonOpenObject(&s_jsonWriter, "ctx");
        CrashJsonWriteString(&s_jsonWriter, "IP", "0x0");
        CrashJsonWriteString(&s_jsonWriter, "SP", "0x0");
        CrashJsonWriteString(&s_jsonWriter, "BP", "0x0");
        CrashJsonCloseObject(&s_jsonWriter);

        CrashJsonOpenArray(&s_jsonWriter, "stack_frames");
        // TODO: Replace with actual frame enumeration.
        int stackFrameCount = 0;
        for (int stackFrameIndex = 0; stackFrameIndex < stackFrameCount; stackFrameIndex++)
        {
            CrashJsonOpenObject(&s_jsonWriter, NULL);
            CrashJsonWriteString(&s_jsonWriter, "is_managed", "false");
            CrashJsonWriteString(&s_jsonWriter, "module_address", "0x0");
            CrashJsonWriteString(&s_jsonWriter, "stack_pointer", "0x0");
            CrashJsonWriteString(&s_jsonWriter, "native_address", "0x0");
            CrashJsonCloseObject(&s_jsonWriter);
        }
        CrashJsonCloseArray(&s_jsonWriter);

        CrashJsonCloseObject(&s_jsonWriter);
    }
    CrashJsonCloseArray(&s_jsonWriter);

    CrashJsonCloseObject(&s_jsonWriter);

    CrashJsonOpenObject(&s_jsonWriter, "parameters");
    CrashJsonWriteString(&s_jsonWriter, "ExceptionType", "0x00000000");
#ifdef __APPLE__
    CrashJsonWriteString(&s_jsonWriter, "OSVersion", "");
    CrashJsonWriteString(&s_jsonWriter, "SystemModel", "");
    CrashJsonWriteString(&s_jsonWriter, "SystemManufacturer", "apple");
#endif
    CrashJsonCloseObject(&s_jsonWriter);

    CrashJsonCloseObject(&s_jsonWriter);

    WriteToLog(CrashJsonGetBuffer(&s_jsonWriter), CrashJsonGetLength(&s_jsonWriter));
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
