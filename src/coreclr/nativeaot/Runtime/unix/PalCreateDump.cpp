// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include <stdio.h>
#include <stdlib.h>
#include <stdint.h>
#include <errno.h>
#include <sal.h>
#include "config.h"
#include <pthread.h>
#include <string.h>
#include <assert.h>
#include <unistd.h>
#define __STDC_FORMAT_MACROS
#include <inttypes.h>
#include <sys/mman.h>
#include <sys/types.h>
#include <sys/stat.h>
#include <signal.h>
#if HAVE_PRCTL_H
#include <sys/prctl.h>
#include <sys/syscall.h>
#endif
#include <sys/wait.h>
#include <sys/time.h>
#include <sys/resource.h>
#include <sys/stat.h>
#include <dlfcn.h>

#ifdef __APPLE__
#include <sys/sysctl.h>
#include <mach/task.h>
#endif

#ifdef __NetBSD__
#include <sys/cdefs.h>
#include <sys/param.h>
#include <sys/sysctl.h>
#include <kvm.h>
#endif

#ifdef __FreeBSD__
#include <sys/sysctl.h>
#include <sys/user.h>
#endif

#define _T(s) s
#include "RhConfig.h"

#include <minipal/utils.h>
#include <minipal/thread.h>
#include <generatedumpflags.h>

#if !defined(HOST_MACCATALYST) && !defined(HOST_IOS) && !defined(HOST_TVOS)

// Crash dump generating program arguments. MAX_ARGV_ENTRIES is the max number
// of entries if every createdump option/argument is passed.
#define MAX_ARGV_ENTRIES 32 
const char* g_argvCreateDump[MAX_ARGV_ENTRIES] = { nullptr };
char* g_szCreateDumpPath = nullptr;
char* g_ppidarg  = nullptr;

const size_t MaxUnsigned32BitDecString = STRING_LENGTH("4294967295");
const size_t MaxUnsigned64BitDecString = STRING_LENGTH("18446744073709551615");

/*++
Function:
    FormatInt

    Helper function to format an uint32 as a string.

--*/
static
char*
FormatInt(uint32_t value)
{
    char* buffer = (char*)malloc(MaxUnsigned32BitDecString + 1);
    if (buffer != nullptr)
    {
        if (snprintf(buffer, MaxUnsigned32BitDecString, "%" PRIu32, value) < 0)
        {
            free(buffer);
            buffer = nullptr;
        }
    }
    return buffer;
}

/*++
Function:
    FormatInt64

    Helper function to format an uint64 as a string.

--*/
static
char*
FormatInt64(uint64_t value)
{
    char* buffer = (char*)malloc(MaxUnsigned64BitDecString + 1);
    if (buffer != nullptr)
    {
        if (snprintf(buffer, MaxUnsigned64BitDecString, "%" PRIu64, value) < 0)
        {
            free(buffer);
            buffer = nullptr;
        }
    }
    return buffer;
}

/*++
Function
  BuildCreateDumpCommandLine

Abstract
  Builds the createdump command line from the arguments.

Return
  true - succeeds, false - fails

--*/
static
bool
BuildCreateDumpCommandLine(
    const char** argv,
    const char* dumpName,
    const char* logFileName,
    int dumpType,
    uint32_t flags)
{
    if (g_szCreateDumpPath == nullptr || g_ppidarg == nullptr)
    {
        return false;
    }

    int argc = 0;
    argv[argc++] = g_szCreateDumpPath;

    if (dumpName != nullptr)
    {
        argv[argc++] = "--name";
        argv[argc++] = dumpName;
    }

    switch (dumpType)
    {
        case DumpTypeNormal:
            argv[argc++] = "--normal";
            break;
        case DumpTypeWithHeap:
            argv[argc++] = "--withheap";
            break;
        case DumpTypeTriage:
            argv[argc++] = "--triage";
            break;
        case DumpTypeFull:
            argv[argc++] = "--full";
            break;
        default:
            break;
    }

    if (flags & GenerateDumpFlagsLoggingEnabled)
    {
        argv[argc++] = "--diag";
    }

    if (flags & GenerateDumpFlagsVerboseLoggingEnabled)
    {
        argv[argc++] = "--verbose";
    }

    if (flags & GenerateDumpFlagsCrashReportEnabled)
    {
        argv[argc++] = "--crashreport";
    }

    if (flags & GenerateDumpFlagsCrashReportOnlyEnabled)
    {
        argv[argc++] = "--crashreportonly";
    }

    if (logFileName != nullptr)
    {
        argv[argc++] = "--logtofile";
        argv[argc++] = logFileName;
    }

    argv[argc++] = "--nativeaot";
    argv[argc++] = g_ppidarg;
    argv[argc++] = nullptr;

    if (argc >= MAX_ARGV_ENTRIES)
    {
        return false;
    }
    return true;
}

/*++
Function:
  CreateCrashDump

  Creates crash dump of the process. Can be called from the
  unhandled native exception handler.

(no return value)
--*/
static bool
CreateCrashDump(
    const char* argv[],
    char* errorMessageBuffer,
    int cbErrorMessageBuffer)
{
    int pipe_descs[2];
    if (pipe(pipe_descs) == -1)
    {
        if (errorMessageBuffer != nullptr)
        {
            snprintf(errorMessageBuffer, cbErrorMessageBuffer, "Problem launching createdump: pipe() FAILED %s (%d)\n", strerror(errno), errno);
        }
        return false;
    }
    // [0] is read end, [1] is write end
    int parent_pipe = pipe_descs[0];
    int child_pipe = pipe_descs[1];

    // Fork the core dump child process.
    pid_t childpid = fork();

    // If error, write an error to trace log and abort
    if (childpid == -1)
    {
        if (errorMessageBuffer != nullptr)
        {
            snprintf(errorMessageBuffer, cbErrorMessageBuffer, "Problem launching createdump: fork() FAILED %s (%d)\n", strerror(errno), errno);
        }
        close(pipe_descs[0]);
        close(pipe_descs[1]);
        return false;
    }
    else if (childpid == 0)
    {
        // Close the read end of the pipe, the child doesn't need it
        close(parent_pipe);

        // Only dup the child's stderr if there is error buffer
        if (errorMessageBuffer != nullptr)
        {
            dup2(child_pipe, STDERR_FILENO);
        }
        // Execute the createdump program
        if (execv(argv[0], (char* const *)argv) == -1)
        {
            fprintf(stderr, "Problem launching createdump (may not have execute permissions): execv(%s) FAILED %s (%d)\n", argv[0], strerror(errno), errno);
            exit(-1);
        }
    }
    else
    {
#if HAVE_PRCTL_H && HAVE_PR_SET_PTRACER
        // Gives the child process permission to use /proc/<pid>/mem and ptrace
        if (prctl(PR_SET_PTRACER, childpid, 0, 0, 0) == -1)
        {
            // Ignore any error because on some CentOS and OpenSUSE distros, it isn't
            // supported but createdump works just fine.
#ifdef _DEBUG
            fprintf(stderr, "CreateCrashDump: prctl() FAILED %s (%d)\n", strerror(errno), errno);
#endif
        }
#endif // HAVE_PRCTL_H && HAVE_PR_SET_PTRACER
        close(child_pipe);

        // Read createdump's stderr messages (if any)
        if (errorMessageBuffer != nullptr)
        {
            // Read createdump's stderr
            int bytesRead = 0;
            int count = 0;
            while ((count = read(parent_pipe, errorMessageBuffer + bytesRead, cbErrorMessageBuffer - bytesRead)) > 0)
            {
                bytesRead += count;
            }
            errorMessageBuffer[bytesRead] = 0;
            if (bytesRead > 0)
            {
                fputs(errorMessageBuffer, stderr);
            }
        }
        close(parent_pipe);

        // Parent waits until the child process is done
        int wstatus = 0;
        int result = waitpid(childpid, &wstatus, 0);
        if (result != childpid)
        {
            fprintf(stderr, "Problem waiting for createdump: waitpid() FAILED result %d wstatus %08x errno %s (%d)\n",
                result, wstatus, strerror(errno), errno);
            return false;
        }
        else
        {
#ifdef _DEBUG
            fprintf(stderr, "waitpid() returned successfully (wstatus %08x) WEXITSTATUS %x WTERMSIG %x\n", wstatus, WEXITSTATUS(wstatus), WTERMSIG(wstatus));
#endif
            return !WIFEXITED(wstatus) || WEXITSTATUS(wstatus) == 0;
        }
    }
    return true;
}

#endif // !defined(HOST_MACCATALYST) && !defined(HOST_IOS) && !defined(HOST_TVOS)

/*++
Function:
  PalCreateCrashDumpIfEnabled

  Creates crash dump of the process (if enabled). Can be called from the unhandled native exception handler.

Parameters:
    signal - POSIX signal number or 0
    siginfo - signal info or nullptr
    exceptionRecord - address of exception record or nullptr

(no return value)
--*/
void
PalCreateCrashDumpIfEnabled(int signal, siginfo_t* siginfo, void* exceptionRecord)
{
#if !defined(HOST_MACCATALYST) && !defined(HOST_IOS) && !defined(HOST_TVOS)
    // If enabled, launch the create minidump utility and wait until it completes
    if (g_argvCreateDump[0] != nullptr)
    {
        const char* argv[MAX_ARGV_ENTRIES];
        char* signalArg = nullptr;
        char* crashThreadArg = nullptr;
        char* signalCodeArg = nullptr;
        char* signalErrnoArg = nullptr;
        char* signalAddressArg = nullptr;
        char* exceptionRecordArg = nullptr;

        // Copy the createdump argv
        int argc = 0;
        for (; argc < MAX_ARGV_ENTRIES; argc++)
        {
            argv[argc] = g_argvCreateDump[argc];
            if (g_argvCreateDump[argc] == nullptr)
            {
                break;
            }
        }

        if (signal != 0 && argc < MAX_ARGV_ENTRIES)
        {
            // Add the signal number to the command line
            signalArg = FormatInt(signal);
            if (signalArg != nullptr)
            {
                argv[argc++] = "--signal";
                argv[argc++] = signalArg;
            }

            // Add the current thread id to the command line. This function is always called on the crashing thread.
            crashThreadArg = FormatInt(minipal_get_current_thread_id());
            if (crashThreadArg != nullptr)
            {
                argv[argc++] = "--crashthread";
                argv[argc++] = crashThreadArg;
            }

            if (siginfo != nullptr && argc < MAX_ARGV_ENTRIES)
            {
                signalCodeArg = FormatInt(siginfo->si_code);
                if (signalCodeArg != nullptr)
                {
                    argv[argc++] = "--code";
                    argv[argc++] = signalCodeArg;
                }
                signalErrnoArg = FormatInt(siginfo->si_errno);
                if (signalErrnoArg != nullptr)
                {
                    argv[argc++] = "--errno";
                    argv[argc++] = signalErrnoArg;
                }
                signalAddressArg = FormatInt64((uint64_t)siginfo->si_addr);
                if (signalAddressArg != nullptr)
                {
                    argv[argc++] = "--address";
                    argv[argc++] = signalAddressArg;
                }
            }

            if (exceptionRecord != nullptr && argc < MAX_ARGV_ENTRIES)
            {
                exceptionRecordArg = FormatInt64((uint64_t)exceptionRecord);
                if (exceptionRecordArg != nullptr)
                {
                    argv[argc++] = "--exception-record";
                    argv[argc++] = exceptionRecordArg;
                }
            }

            argv[argc++] = nullptr;
            assert(argc < MAX_ARGV_ENTRIES);
        }

        CreateCrashDump(argv, nullptr, 0);

        free(signalArg);
        free(crashThreadArg);
        free(signalCodeArg);
        free(signalErrnoArg);
        free(signalAddressArg);
        free(exceptionRecordArg);
    }
#endif // !defined(HOST_MACCATALYST) && !defined(HOST_IOS) && !defined(HOST_TVOS)
}

void
PalCreateCrashDumpIfEnabled()
{
    PalCreateCrashDumpIfEnabled(SIGABRT, nullptr, nullptr);
}

void
PalCreateCrashDumpIfEnabled(void* pExceptionRecord)
{
    PalCreateCrashDumpIfEnabled(SIGABRT, nullptr, pExceptionRecord);
}

/*++
Function:
  PalGenerateCoreDump

Abstract:
  Public entry point to create a crash dump of the process.

Parameters:
    dumpName
    dumpType:
        Normal = 1,
        WithHeap = 2,
        Triage = 3,
        Full = 4
    flags
        See enum

Return:
    true success
    false failed
--*/
bool
PalGenerateCoreDump(
    const char* dumpName,
    int dumpType,
    uint32_t flags,
    char* errorMessageBuffer,
    int cbErrorMessageBuffer)
{
#if !defined(HOST_MACCATALYST) && !defined(HOST_IOS) && !defined(HOST_TVOS)
    const char* argvCreateDump[MAX_ARGV_ENTRIES];
    if (dumpType <= DumpTypeUnknown || dumpType > DumpTypeMax)
    {
        return false;
    }
    if (dumpName != nullptr && dumpName[0] == '\0')
    {
        dumpName = nullptr;
    }
    bool result = BuildCreateDumpCommandLine(argvCreateDump, dumpName, nullptr, dumpType, flags);
    if (result)
    {
        result = CreateCrashDump(argvCreateDump, errorMessageBuffer, cbErrorMessageBuffer);
    }
    return result;
#else
    return false;
#endif // !defined(HOST_MACCATALYST) && !defined(HOST_IOS) && !defined(HOST_TVOS)
}

/*++
Function
  PalCreateDumpInitialize()

Abstract
  Initialize the process abort crash dump program file path and
  name. Doing all of this ahead of time so nothing is allocated
  or copied in abort/signal handler.

Return
  true - succeeds, false - fails

--*/
bool
PalCreateDumpInitialize()
{
#if !defined(HOST_MACCATALYST) && !defined(HOST_IOS) && !defined(HOST_TVOS)
    bool enabled = false;
    RhConfig::Environment::TryGetBooleanValue("DbgEnableMiniDump", &enabled);
    if (enabled)
    {
        char* dumpName = nullptr;
        RhConfig::Environment::TryGetStringValue("DbgMiniDumpName", &dumpName);

        char* logFilePath = nullptr;
        RhConfig::Environment::TryGetStringValue("CreateDumpLogToFile", &logFilePath);

        uint64_t dumpType = DumpTypeUnknown;
        if (RhConfig::Environment::TryGetIntegerValue("DbgMiniDumpType", &dumpType, true))
        {
            if (dumpType <= DumpTypeUnknown || dumpType > DumpTypeMax)
            {
                dumpType = DumpTypeUnknown;
            }
        }
        uint32_t flags = GenerateDumpFlagsNone;
        bool value = false;
        if (RhConfig::Environment::TryGetBooleanValue("CreateDumpDiagnostics", &value))
        {
            if (value)
            {
                flags |= GenerateDumpFlagsLoggingEnabled;
            }
        }
        if (RhConfig::Environment::TryGetBooleanValue("CreateDumpVerboseDiagnostics", &value))
        {
            if (value)
            {
                flags |= GenerateDumpFlagsVerboseLoggingEnabled;
            }
        }
        if (RhConfig::Environment::TryGetBooleanValue("EnableCrashReport", &value))
        {
            if (value)
            {
                flags |= GenerateDumpFlagsCrashReportEnabled;
            }
        }
        if (RhConfig::Environment::TryGetBooleanValue("EnableCrashReportOnly", &value))
        {
            if (value)
            {
                flags |= GenerateDumpFlagsCrashReportOnlyEnabled;
            }
        }

        // Build the createdump program path for the command line
        Dl_info info;
        if (dladdr((void*)&PalCreateDumpInitialize, &info) == 0)
        {
            return false;
        }
        const char* DumpGeneratorName = "createdump";
        int programLen = strlen(info.dli_fname) + strlen(DumpGeneratorName) + 1;
        char* program = (char*)malloc(programLen);
        if (program == nullptr)
        {
            return false;
        }
        strncpy(program, info.dli_fname, programLen);
        char *last = strrchr(program, '/');
        if (last != nullptr)
        {
            *(last + 1) = '\0';
        }
        else
        {
            program[0] = '\0';
        }
        strncat(program, DumpGeneratorName, programLen);

        struct stat fileData;
        if (stat(program, &fileData) == -1 || !S_ISREG(fileData.st_mode))
        {
            fprintf(stderr, "DOTNET_DbgEnableMiniDump is set and the createdump binary does not exist: %s\n", program);
            return true;
        }
        g_szCreateDumpPath = program;

        // Format the app pid for the createdump command line
        g_ppidarg = FormatInt(getpid());
        if (g_ppidarg == nullptr)
        {
            return false;
        }

        if (!BuildCreateDumpCommandLine(g_argvCreateDump, dumpName, logFilePath, dumpType, flags))
        {
            return false;
        }
    }
#endif // !defined(HOST_MACCATALYST) && !defined(HOST_IOS) && !defined(HOST_TVOS)

    return true;
}
