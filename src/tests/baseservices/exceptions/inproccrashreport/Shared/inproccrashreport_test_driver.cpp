// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// Invokes the production reporter with fixed callback data. This tests report
// fidelity, not real signal dispatch, VM stack walking, or crash-time safety.

#ifndef PLATFORM_UNIX
#include "config.h"
#endif
#include "pal.h"
#include "inproccrashreporter.h"
#if defined(TARGET_ANDROID)
#include "android_log_interpose.h"
#endif

#include <minipal/guid.h>

#include <errno.h>
#include <signal.h>
#include <stdint.h>
#include <stdio.h>
#include <string.h>
#include <ucontext.h>
#if !defined(TARGET_ANDROID)
#include <fcntl.h>
#include <unistd.h>
#endif

// Normally published to PAL via its callback setter, not the reporter header.
void InProcCrashReportSignalDispatcher(int signal, void* siginfo, void* context);

#define INPROC_TEST_EXPORT __attribute__((visibility("default")))

namespace
{
    // Scenario ids -- must match the managed harness (Program.cs).
    const int kScenarioRichSigsegv = 0;
    const int kScenarioAbort = 1;
    const int kScenarioStackOverflow = 2;
    const int kScenarioConsoleOnly = 3;

    // Synthetic module handles, resolved by ModuleInfoCallback below.
    const void* const kManagedModule = reinterpret_cast<const void*>(0x1000);
    const void* const kNativeModule = reinterpret_cast<const void*>(0x2000);
    const void* const kNativeModule2 = reinterpret_cast<const void*>(0x3000);

    const GUID kSyntheticGuid =
        { 0x11111111, 0x2222, 0x3333, { 0x44, 0x55, 0x66, 0x77, 0x88, 0x99, 0xaa, 0xbb } };

    bool Check(bool condition, const char* message)
    {
        if (!condition)
        {
            printf("FAIL: %s\n", message);
            fflush(stdout);
        }
        return condition;
    }

    bool CheckIo(bool succeeded, const char* operation, const char* path)
    {
        if (!succeeded)
        {
            int error = errno;
            printf("FAIL: %s('%s'): errno %d (%s)\n", operation, path, error, strerror(error));
            fflush(stdout);
        }
        return succeeded;
    }

    bool InitializePal()
    {
        // PAL and reporter state live until the isolated test process exits.
        int result = PAL_InitializeDLL();
        if (result != 0)
        {
            printf("FAIL: PAL_InitializeDLL returned %d\n", result);
            fflush(stdout);
            return false;
        }

        return Check(GetCurrentProcessId() == static_cast<uint32_t>(getpid()),
            "PAL process ID does not match the host process");
    }

    void InitializeServices(const char* reportRootPath, bool enableLifecycle)
    {
        InProcCrashReporterServicesSettings services = {};
        services.enableCreateCrashDump = true;
        services.enableLifecycle = enableLifecycle;
        services.reportRootPath = reportRootPath;
        services.maxFileCount = CRASHREPORT_DEFAULT_MAX_FILE_COUNT;
        InProcCrashReportInitializeServices(services);
    }

    struct SyntheticContext
    {
        ucontext_t context;
#if defined(TARGET_APPLE)
        struct __darwin_mcontext64 machineContext;
#endif
    };

    struct SignalReportContext
    {
        int (*beforeEnumerateCallback)();
        bool callbackSucceeded;
    };

    // Coordination belongs to this test call, not to other callers of the reporter.
    thread_local SignalReportContext* t_signalReportContext = nullptr;

    bool IsManagedThreadCallback()
    {
        return true;
    }

    bool ModuleInfoCallback(const void* moduleHandle, const char** moduleName, GUID* moduleGuid)
    {
        if (moduleGuid != nullptr)
        {
            *moduleGuid = kSyntheticGuid;
        }
        if (moduleHandle == kManagedModule)
        {
            if (moduleName != nullptr)
            {
                *moduleName = "synthetic.managed.dll";
            }
            return true;
        }
        if (moduleHandle == kNativeModule)
        {
            if (moduleName != nullptr)
            {
                *moduleName = "libsynthetic.so";
            }
            return true;
        }
        if (moduleHandle == kNativeModule2)
        {
            if (moduleName != nullptr)
            {
                *moduleName = "libnative2.so";
            }
            return true;
        }
        return false;
    }

    // Appends a managed frame (HasManagedIdentity true: methodName + token present).
    void EmitManagedFrame(
        InProcCrashReportFrameCallback frameCallback,
        uint64_t ip,
        const char* methodName,
        const char* className,
        uint32_t token,
        void* ctx)
    {
        frameCallback(
            ip, /*stackPointer*/ ip + 0x1000,
            methodName, className,
            /*moduleName*/ "synthetic.managed.dll", /*moduleHandle*/ kManagedModule,
            /*moduleTimestamp*/ 0x600dcafe, /*moduleSize*/ 0x00010000, /*moduleGuid*/ &kSyntheticGuid,
            /*nativeOffset*/ 0x20, token, /*ilOffset*/ 0x10, ctx);
    }

    // Appends a native frame (no managed identity; native_module set).
    void EmitNativeFrame(
        InProcCrashReportFrameCallback frameCallback,
        uint64_t ip,
        const char* moduleName,
        const void* moduleHandle,
        void* ctx)
    {
        frameCallback(
            ip, /*stackPointer*/ ip + 0x1000,
            /*methodName*/ nullptr, /*className*/ nullptr,
            moduleName, moduleHandle,
            /*moduleTimestamp*/ 0x12345678, /*moduleSize*/ 0x00020000, /*moduleGuid*/ &kSyntheticGuid,
            /*nativeOffset*/ 0x40, /*token*/ 0, /*ilOffset*/ 0, ctx);
    }

    // Three thread records: a mixed crash stack with generic names, a managed-only
    // stack, and a native-only stack. No real background threads are created.
    void EnumerateThreadsRichSigsegv(
        uint64_t crashingTid,
        InProcCrashReportThreadCallback threadCallback,
        InProcCrashReportFrameCallback frameCallback,
        void* ctx)
    {
        if (t_signalReportContext != nullptr && t_signalReportContext->beforeEnumerateCallback != nullptr)
        {
            t_signalReportContext->callbackSucceeded = t_signalReportContext->beforeEnumerateCallback() != 0;
        }

        threadCallback(crashingTid, /*isCrashThread*/ true, "System.NullReferenceException", 0x80004003, ctx);
        EmitManagedFrame(frameCallback, 0x000000000040aaaa,
            "DoWork", "Synthetic.App.Worker`1[System.Int32]", 0x06000001, ctx);
        EmitNativeFrame(frameCallback, 0x000000000040bbbb, "libsynthetic.so", kNativeModule, ctx);
        EmitManagedFrame(frameCallback, 0x000000000040cccc,
            "Insert", "Synthetic.App.Dictionary`2[System.String,System.Int32]", 0x06000002, ctx);
        EmitNativeFrame(frameCallback, 0x000000000040dddd, "libnative2.so", kNativeModule2, ctx);

        threadCallback(crashingTid + 1, /*isCrashThread*/ false, nullptr, 0, ctx);
        EmitManagedFrame(frameCallback, 0x000000000040eeee,
            "Listen", "Synthetic.App.Server", 0x06000003, ctx);

        threadCallback(crashingTid + 2, /*isCrashThread*/ false, nullptr, 0, ctx);
        EmitNativeFrame(frameCallback, 0x000000000040ffff, "libsynthetic.so", kNativeModule, ctx);
    }

    // A native-only crash stack without a managed exception, plus a second record
    // with a managed frame, exercises SIGABRT and the null-exception path.
    void EnumerateThreadsAbort(
        uint64_t crashingTid,
        InProcCrashReportThreadCallback threadCallback,
        InProcCrashReportFrameCallback frameCallback,
        void* ctx)
    {
        threadCallback(crashingTid, /*isCrashThread*/ true, /*exceptionType*/ nullptr, 0, ctx);
        EmitNativeFrame(frameCallback, 0x000000000040aaaa, "libsynthetic.so", kNativeModule, ctx);
        EmitNativeFrame(frameCallback, 0x000000000040bbbb, "libnative2.so", kNativeModule2, ctx);

        threadCallback(crashingTid + 1, /*isCrashThread*/ false, nullptr, 0, ctx);
        EmitManagedFrame(frameCallback, 0x000000000040cccc,
            "Listen", "Synthetic.App.Server", 0x06000001, ctx);
    }

    // Deterministic synthetic register state for the crash thread.
    void FillSyntheticContext(SyntheticContext* syntheticContext)
    {
        memset(syntheticContext, 0, sizeof(*syntheticContext));
        ucontext_t* uc = &syntheticContext->context;
#if defined(TARGET_APPLE)
        uc->uc_mcontext = &syntheticContext->machineContext;
#endif
#if defined(TARGET_AMD64)
    #if defined(TARGET_APPLE)
        uc->uc_mcontext->__ss.__rip = 0x000000000040aaaa;
        uc->uc_mcontext->__ss.__rsp = 0x00007fff0000aaaa;
        uc->uc_mcontext->__ss.__rbp = 0x00007fff0000aab0;
    #elif defined(TARGET_HAIKU)
        uc->uc_mcontext.rip = 0x000000000040aaaa;
        uc->uc_mcontext.rsp = 0x00007fff0000aaaa;
        uc->uc_mcontext.rbp = 0x00007fff0000aab0;
    #elif defined(TARGET_OPENBSD)
        uc->sc_rip = 0x000000000040aaaa;
        uc->sc_rsp = 0x00007fff0000aaaa;
        uc->sc_rbp = 0x00007fff0000aab0;
    #elif defined(TARGET_FREEBSD)
        uc->uc_mcontext.mc_rip = 0x000000000040aaaa;
        uc->uc_mcontext.mc_rsp = 0x00007fff0000aaaa;
        uc->uc_mcontext.mc_rbp = 0x00007fff0000aab0;
    #else
        uc->uc_mcontext.gregs[REG_RIP] = 0x000000000040aaaa;
        uc->uc_mcontext.gregs[REG_RSP] = 0x00007fff0000aaaa;
        uc->uc_mcontext.gregs[REG_RBP] = 0x00007fff0000aab0;
    #endif
#elif defined(TARGET_ARM64)
    #if defined(TARGET_APPLE)
        uc->uc_mcontext->__ss.__pc = 0x000000000040aaaa;
        uc->uc_mcontext->__ss.__sp = 0x00007fff0000aaaa;
        uc->uc_mcontext->__ss.__fp = 0x00007fff0000aab0;
    #elif defined(TARGET_FREEBSD)
        uc->uc_mcontext.mc_gpregs.gp_elr = 0x000000000040aaaa;
        uc->uc_mcontext.mc_gpregs.gp_sp = 0x00007fff0000aaaa;
        uc->uc_mcontext.mc_gpregs.gp_x[29] = 0x00007fff0000aab0;
    #else
        uc->uc_mcontext.pc = 0x000000000040aaaa;
        uc->uc_mcontext.sp = 0x00007fff0000aaaa;
        uc->uc_mcontext.regs[29] = 0x00007fff0000aab0;
    #endif
#elif defined(TARGET_ARM)
        uc->uc_mcontext.arm_pc = 0x0040aaaa;
        uc->uc_mcontext.arm_sp = 0x7fffaaaa;
        uc->uc_mcontext.arm_fp = 0x7fffaab0;
#elif defined(TARGET_X86)
        uc->uc_mcontext.gregs[REG_EIP] = 0x0040aaaa;
        uc->uc_mcontext.gregs[REG_ESP] = 0x7fffaaaa;
        uc->uc_mcontext.gregs[REG_EBP] = 0x7fffaab0;
#elif defined(TARGET_LOONGARCH64)
        uc->uc_mcontext.__pc = 0x000000000040aaaa;
        uc->uc_mcontext.__gregs[3] = 0x00007fff0000aaaa;
        uc->uc_mcontext.__gregs[22] = 0x00007fff0000aab0;
#elif defined(TARGET_RISCV64)
        uc->uc_mcontext.__gregs[0] = 0x000000000040aaaa;
        uc->uc_mcontext.__gregs[2] = 0x00007fff0000aaaa;
        uc->uc_mcontext.__gregs[8] = 0x00007fff0000aab0;
#elif defined(TARGET_S390X)
        uc->uc_mcontext.psw.addr = 0x000000000040aaaa;
        uc->uc_mcontext.gregs[15] = 0x00007fff0000aaaa;
        uc->uc_mcontext.gregs[11] = 0x00007fff0000aab0;
#elif defined(TARGET_POWERPC64)
        uc->uc_mcontext.gp_regs[32] = 0x000000000040aaaa;
        uc->uc_mcontext.gp_regs[1] = 0x00007fff0000aaaa;
        uc->uc_mcontext.gp_regs[31] = 0x00007fff0000aab0;
#else
#error Unsupported architecture
#endif
    }

#if defined(TARGET_ANDROID)
    bool WriteConsoleCapture(const char* consoleCapturePath)
    {
        const char* console = InProcCrashReportTest_GetConsoleCapture();
        if (!Check(console != nullptr, "Android console capture overflowed"))
        {
            return false;
        }

        FILE* file = fopen(consoleCapturePath, "w");
        if (!CheckIo(file != nullptr, "fopen", consoleCapturePath))
        {
            return false;
        }
        size_t length = strlen(console);
        bool written = CheckIo(fwrite(console, 1, length, file) == length, "fwrite", consoleCapturePath);
        bool closed = CheckIo(fclose(file) == 0, "fclose", consoleCapturePath);
        return written && closed;
    }
#else
    bool BeginConsoleCapture(const char* consoleCapturePath, int* savedStderr)
    {
        int capture = open(consoleCapturePath, O_WRONLY | O_CREAT | O_TRUNC, 0600);
        if (!CheckIo(capture != -1, "open", consoleCapturePath))
        {
            return false;
        }

        *savedStderr = dup(STDERR_FILENO);
        bool redirected = CheckIo(*savedStderr != -1, "dup", "stderr") &&
            CheckIo(dup2(capture, STDERR_FILENO) != -1, "dup2", consoleCapturePath);
        close(capture);
        if (!redirected && *savedStderr != -1)
        {
            close(*savedStderr);
        }

        return redirected;
    }

    bool EndConsoleCapture(int savedStderr)
    {
        bool restored = CheckIo(dup2(savedStderr, STDERR_FILENO) != -1, "dup2", "stderr");
        close(savedStderr);
        return restored;
    }
#endif

    bool WriteSignalReport(int signalNumber, const char* consoleCapturePath, int (*beforeEnumerateCallback)())
    {
        SyntheticContext syntheticContext;
        FillSyntheticContext(&syntheticContext);

        siginfo_t si = {};
        si.si_signo = signalNumber;

#if defined(TARGET_ANDROID)
        InProcCrashReportTest_ResetConsoleCapture();
#else
        int savedStderr;
        if (!BeginConsoleCapture(consoleCapturePath, &savedStderr))
        {
            return false;
        }
#endif

        SignalReportContext signalReportContext = { beforeEnumerateCallback, true };
        t_signalReportContext = &signalReportContext;
        errno = EDOM;
        InProcCrashReportSignalDispatcher(signalNumber, &si, &syntheticContext.context);
        bool errnoPreserved = Check(errno == EDOM, "signal dispatcher changed errno");
        t_signalReportContext = nullptr;

#if defined(TARGET_ANDROID)
        bool captured = WriteConsoleCapture(consoleCapturePath);
#else
        bool captured = EndConsoleCapture(savedStderr);
#endif
        return errnoPreserved && captured &&
            Check(signalReportContext.callbackSucceeded, "signal enumeration callback failed");
    }

    struct OnDemandOutputContext
    {
        FILE* file;
        const char* path;
        ucontext_t* signalContext;
        int (*beforeWriteCallback)();
        bool attemptReentrantReport;
        bool reentrantAttempted;
        bool reentrantResult;
    };

    bool WriteOnDemandOutput(const char* buffer, size_t length, void* context)
    {
        OnDemandOutputContext* output = static_cast<OnDemandOutputContext*>(context);
        if (output->beforeWriteCallback != nullptr && output->beforeWriteCallback() == 0)
        {
            return false;
        }

        if (output->attemptReentrantReport && !output->reentrantAttempted)
        {
            output->reentrantAttempted = true;
            output->reentrantResult = InProcCrashReportCreateReport(
                InProcCrashReportOutputFormat::Json,
                SIGSEGV,
                output->signalContext,
                &WriteOnDemandOutput,
                output);
        }

        return CheckIo(fwrite(buffer, 1, length, output->file) == length, "fwrite", output->path);
    }

    bool RejectOutput(const char* /*buffer*/, size_t /*length*/, void* context)
    {
        (*static_cast<int*>(context))++;
        return false;
    }

    int CreateOnDemandReport(
        InProcCrashReportOutputFormat outputFormat,
        int signal,
        const char* outputPath,
        ucontext_t* signalContext,
        bool attemptReentrantReport,
        int (*beforeWriteCallback)())
    {
        printf("Generating on-demand format %u, signal %d: %s\n", static_cast<unsigned>(outputFormat), signal, outputPath);
        fflush(stdout);
        FILE* file = fopen(outputPath, "wb");
        if (!CheckIo(file != nullptr, "fopen", outputPath))
        {
            return -1;
        }

        OnDemandOutputContext output = {};
        output.file = file;
        output.path = outputPath;
        output.signalContext = signalContext;
        output.beforeWriteCallback = beforeWriteCallback;
        output.attemptReentrantReport = attemptReentrantReport;

        bool generated = InProcCrashReportCreateReport(
            outputFormat,
            signal,
            signalContext,
            &WriteOnDemandOutput,
            &output);

        bool closed = CheckIo(fclose(file) == 0, "fclose", outputPath);
        if (!closed ||
            !Check(!attemptReentrantReport || (output.reentrantAttempted && !output.reentrantResult),
                "nested request was not attempted or was incorrectly accepted"))
        {
            return -1;
        }

        return generated ? 1 : 0;
    }

    bool WriteOnDemandReport(
        InProcCrashReportOutputFormat outputFormat,
        int signal,
        const char* outputPath,
        ucontext_t* signalContext,
        bool attemptReentrantReport = false)
    {
        return Check(CreateOnDemandReport(outputFormat, signal, outputPath, signalContext, attemptReentrantReport, nullptr) == 1,
            "on-demand generation failed");
    }
}

// One fatal-shaped scenario per process: the reporter retains its in-flight guard.
extern "C" INPROC_TEST_EXPORT int InProcCrashReportTest_DriveScenario(
    int scenario,
    const char* reporterRootPath,
    const char* consoleCapturePath)
{
    if (!InitializePal())
    {
        return -1;
    }

    InProcCrashReporterSettings settings = {};
    settings.isManagedThreadCallback = &IsManagedThreadCallback;
    settings.walkStackCallback = nullptr;
    settings.moduleInfoCallback = &ModuleInfoCallback;
    settings.frameLimitPerThread = 0;

    int signalNumber = SIGSEGV;
    switch (scenario)
    {
        case kScenarioRichSigsegv:
        case kScenarioConsoleOnly:
            signalNumber = SIGSEGV;
            settings.enumerateThreadsCallback = &EnumerateThreadsRichSigsegv;
            break;
        case kScenarioAbort:
            signalNumber = SIGABRT;
            settings.enumerateThreadsCallback = &EnumerateThreadsAbort;
            break;
        case kScenarioStackOverflow:
            signalNumber = SIGSEGV;
            settings.enumerateThreadsCallback = nullptr; // SO path does not enumerate threads
            break;
        default:
            Check(false, "unknown scenario ID");
            return -1;
    }

    InProcCrashReportInitialize(settings);

    InitializeServices(reporterRootPath, scenario != kScenarioConsoleOnly);

    if (scenario == kScenarioStackOverflow)
    {
        // Drive the captured-stack-overflow-trace path: the runtime SO helper
        // would have recorded a compressed managed stack (with a repeated
        // recursive sequence) for the reporter to emit later.
        InProcCrashReportSetCrashKind(InProcCrashReportCrashKind::StackOverflow);
        InProcCrashReportBeginStackOverflowTrace(/*crashingTid*/ 0, /*totalFrameCount*/ 42);
        InProcCrashReportAddStackOverflowTraceFrame("Synthetic.App.Program.Main", 1, 0);
        InProcCrashReportAddStackOverflowTraceFrame("Synthetic.App.Recurse.Down", 40, 1);
        InProcCrashReportAddStackOverflowTraceFrame("Synthetic.App.Recurse.Bottom", 1, 0);
        InProcCrashReportEndStackOverflowTrace();
    }

    return WriteSignalReport(signalNumber, consoleCapturePath, nullptr) ? 0 : -1;
}

// First generate without services; subsequent requests must not use the enabled
// lifecycle file sink, even after a caller-sink failure.
extern "C" INPROC_TEST_EXPORT int InProcCrashReportTest_DriveOnDemand(
    const char* reportRootPath,
    const char* firstJsonPath,
    const char* secondJsonPath,
    const char* firstLogPath,
    const char* secondLogPath)
{
    if (!InitializePal())
    {
        return -1;
    }

    InProcCrashReporterSettings settings = {};
    settings.isManagedThreadCallback = &IsManagedThreadCallback;
    settings.walkStackCallback = nullptr;
    settings.enumerateThreadsCallback = &EnumerateThreadsRichSigsegv;
    settings.moduleInfoCallback = &ModuleInfoCallback;
    settings.frameLimitPerThread = 0;
    InProcCrashReportInitialize(settings);

    SyntheticContext syntheticContext;
    FillSyntheticContext(&syntheticContext);
    ucontext_t* signalContext = &syntheticContext.context;

    if (!Check(!InProcCrashReportCreateReport(
            InProcCrashReportOutputFormat::Json,
            SIGSEGV,
            signalContext,
            nullptr,
            nullptr), "null output callback was accepted"))
    {
        return -1;
    }

    if (!WriteOnDemandReport(
            InProcCrashReportOutputFormat::Json,
            SIGSEGV,
            firstJsonPath,
            signalContext,
            /*attemptReentrantReport*/ true))
    {
        return -1;
    }

    InitializeServices(reportRootPath, /*enableLifecycle*/ true);
    const InProcCrashReportOutputFormat formats[] = { InProcCrashReportOutputFormat::Json, InProcCrashReportOutputFormat::Log };
    for (InProcCrashReportOutputFormat format : formats)
    {
        int calls = 0;
        bool generated = InProcCrashReportCreateReport(format, SIGSEGV, signalContext, &RejectOutput, &calls);
        if (!Check(!generated && calls == 1, "failing output callback was ignored or invoked again after failure"))
        {
            return -1;
        }
    }

    return WriteOnDemandReport(InProcCrashReportOutputFormat::Json, SIGABRT, secondJsonPath, signalContext) &&
        WriteOnDemandReport(InProcCrashReportOutputFormat::Log, SIGSEGV, firstLogPath, signalContext) &&
        WriteOnDemandReport(InProcCrashReportOutputFormat::Log, SIGABRT, secondLogPath, signalContext) ? 0 : -1;
}

// DriveOnDemand initializes the reporter before concurrent requests use this entry point.
extern "C" INPROC_TEST_EXPORT int InProcCrashReportTest_CreateOnDemandReport(
    InProcCrashReportOutputFormat outputFormat,
    int signal,
    const char* outputPath,
    int (*beforeWriteCallback)())
{
    SyntheticContext syntheticContext;
    FillSyntheticContext(&syntheticContext);
    return CreateOnDemandReport(outputFormat, signal, outputPath, &syntheticContext.context,
        /*attemptReentrantReport*/ false, beforeWriteCallback);
}

// Uses the same initialized reporter as on-demand calls; bypasses PAL signal handling.
// A successful return confirms capture, not admission: the dispatcher has no return value.
extern "C" INPROC_TEST_EXPORT int InProcCrashReportTest_CreateSignalReport(
    const char* consoleCapturePath,
    int (*beforeEnumerateCallback)())
{
    return WriteSignalReport(SIGSEGV, consoleCapturePath, beforeEnumerateCallback) ? 1 : -1;
}
