// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "standardpch.h"
#include "superpmi.h"
#include "simpletimer.h"
#include "mclist.h"
#include "lightweightmap.h"
#include "commandline.h"
#include "errorhandling.h"
#include "fileio.h"
#include <minipal/random.h>

#include <signal.h>

#ifdef TARGET_UNIX
#include <errno.h>
#include <fcntl.h>
#include <sys/wait.h>
#include <unistd.h>
#include <minipal/getexepath.h>
#endif // TARGET_UNIX

// Forward declare the conversion method. Including spmiutil.h pulls in other headers
// that cause build breaks.
std::string ConvertToUtf8(const WCHAR* str);

// Platform-specific process handle type.
#ifdef TARGET_UNIX
typedef pid_t SpmiProcessHandle;
#else
typedef HANDLE SpmiProcessHandle;
#endif // TARGET_UNIX

#define MAX_LOG_LINE_SIZE 0x1000 // 4 KB

volatile sig_atomic_t closeRequested = 0; // global variable to communicate CTRL+C between threads.

#ifndef TARGET_UNIX

bool StartProcess(char* commandLine, HANDLE hStdOutput, HANDLE hStdError, HANDLE* hProcess)
{
    LogDebug("StartProcess commandLine=%s", commandLine);

    STARTUPINFO         si;
    PROCESS_INFORMATION pi;

    ZeroMemory(&si, sizeof(si));
    si.cb         = sizeof(si);
    si.dwFlags    = STARTF_USESTDHANDLES;
    si.hStdInput  = GetStdHandle(STD_INPUT_HANDLE);
    si.hStdOutput = hStdOutput;
    si.hStdError  = hStdError;

    ZeroMemory(&pi, sizeof(pi));

    // Start the child process.
    if (!CreateProcess(NULL,        // No module name (use command line)
                       commandLine, // Command line
                       NULL,        // Process handle not inheritable
                       NULL,        // Thread handle not inheritable
                       TRUE,        // Set handle inheritance to TRUE (required to use STARTF_USESTDHANDLES)
                       0,           // No creation flags
                       NULL,        // Use parent's environment block
                       NULL,        // Use parent's starting directory
                       &si,         // Pointer to STARTUPINFO structure
                       &pi))        // Pointer to PROCESS_INFORMATION structure
    {
        LogError("CreateProcess failed (%d). CommandLine: %s", GetLastError(), commandLine);
        *hProcess = INVALID_HANDLE_VALUE;
        return false;
    }

    *hProcess = pi.hProcess;
    CloseHandle(pi.hThread);
    return true;
}

#else // TARGET_UNIX

// Parse a command line string into an argv array for execv. Supports single-quote,
// double-quote, and backslash escaping. The returned array and its strings are
// heap-allocated and must be freed by the caller.
static char** ParseCommandLineToArgv(char* commandLine, int* argc)
{
    // First pass: count tokens so we know how big to make argv.
    int  count        = 0;
    bool inArg        = false;
    bool inSingleQuote = false;
    bool inDoubleQuote = false;
    for (char* p = commandLine; *p != '\0'; ++p)
    {
        char c = *p;
        if (c == '\\' && !inSingleQuote)
        {
            if (*(p + 1) != '\0')
                ++p; // skip escaped character
            if (!inArg) { inArg = true; count++; }
        }
        else if (!inSingleQuote && c == '"')
        {
            inDoubleQuote = !inDoubleQuote;
            if (!inArg) { inArg = true; count++; }
        }
        else if (!inDoubleQuote && c == '\'')
        {
            inSingleQuote = !inSingleQuote;
            if (!inArg) { inArg = true; count++; }
        }
        else if (!inSingleQuote && !inDoubleQuote && (c == ' ' || c == '\t'))
        {
            inArg = false;
        }
        else
        {
            if (!inArg) { inArg = true; count++; }
        }
    }

    *argc          = count;
    char** argv    = new char*[count + 1];
    int    idx     = 0;

    // Second pass: extract tokens.
    inArg        = false;
    inSingleQuote = false;
    inDoubleQuote = false;
    // Temporary buffer (upper-bound: length of the entire commandLine).
    size_t cmdLen = strlen(commandLine);
    char*  buf    = new char[cmdLen + 1];
    int    bufLen = 0;

    for (char* p = commandLine; ; ++p)
    {
        char c = *p;

        if (c == '\0' || (!inSingleQuote && !inDoubleQuote && (c == ' ' || c == '\t')))
        {
            if (inArg)
            {
                buf[bufLen] = '\0';
                argv[idx]   = new char[bufLen + 1];
                memcpy(argv[idx], buf, bufLen + 1);
                ++idx;
                bufLen = 0;
                inArg  = false;
            }
            if (c == '\0')
                break;
            continue;
        }

        inArg = true;

        if (c == '\\' && !inSingleQuote)
        {
            char next = *(p + 1);
            if (next != '\0')
            {
                buf[bufLen++] = next;
                ++p;
            }
            else
            {
                buf[bufLen++] = c;
            }
        }
        else if (!inSingleQuote && c == '"')
        {
            inDoubleQuote = !inDoubleQuote;
        }
        else if (!inDoubleQuote && c == '\'')
        {
            inSingleQuote = !inSingleQuote;
        }
        else
        {
            buf[bufLen++] = c;
        }
    }

    delete[] buf;
    argv[idx] = nullptr;
    return argv;
}

bool StartProcess(char* commandLine, int hStdOutput, int hStdError, pid_t* pid)
{
    LogDebug("StartProcess commandLine=%s", commandLine);

    int    argc;
    char** argv = ParseCommandLineToArgv(commandLine, &argc);
    if (argc == 0)
    {
        LogError("StartProcess: empty command line");
        delete[] argv;
        return false;
    }

    *pid = fork();
    if (*pid == -1)
    {
        LogError("fork() failed: %s", strerror(errno));
        for (int i = 0; i < argc; i++)
            delete[] argv[i];
        delete[] argv;
        return false;
    }

    if (*pid == 0)
    {
        // Child process: redirect stdout and stderr then exec.
        if (dup2(hStdOutput, STDOUT_FILENO) == -1)
        {
            fprintf(stderr, "dup2(stdout) failed: %s\n", strerror(errno));
            _exit(1);
        }
        if (dup2(hStdError, STDERR_FILENO) == -1)
        {
            fprintf(stderr, "dup2(stderr) failed: %s\n", strerror(errno));
            _exit(1);
        }

        // Close the original fds now that they are duplicated to STDOUT/STDERR.
        // The O_CLOEXEC on the parent's open() handles the sibling worker fds.
        if (hStdOutput != STDOUT_FILENO)
            close(hStdOutput);
        if (hStdError != STDERR_FILENO)
            close(hStdError);

        execv(argv[0], argv);
        // If execv returns, it failed.
        fprintf(stderr, "execv(%s) failed: %s\n", argv[0], strerror(errno));
        _exit(1);
    }

    // Parent process: free argv and return.
    for (int i = 0; i < argc; i++)
        delete[] argv[i];
    delete[] argv;
    return true;
}

#endif // TARGET_UNIX

void ReadMCLToArray(char* mclFilename, int** arr, int* count)
{
    *count     = 0;
    *arr       = nullptr;
    char* buff = nullptr;

    HANDLE hFile = CreateFileA(mclFilename, GENERIC_READ, FILE_SHARE_READ, NULL, OPEN_EXISTING,
                               FILE_ATTRIBUTE_NORMAL | FILE_FLAG_SEQUENTIAL_SCAN, NULL);

    if (hFile == INVALID_HANDLE_VALUE)
    {
        LogError("Unable to open '%s'. GetLastError()=%u", mclFilename, GetLastError());
        goto Cleanup;
    }

    LARGE_INTEGER DataTemp;
    if (GetFileSizeEx(hFile, &DataTemp) == 0)
    {
        LogError("GetFileSizeEx failed. GetLastError()=%u", GetLastError());
        goto Cleanup;
    }

    if (DataTemp.QuadPart > MAXMCLFILESIZE)
    {
        LogError("Size %d exceeds max size of %d", DataTemp.QuadPart, MAXMCLFILESIZE);
        goto Cleanup;
    }

    int sz;
    sz = (int)(DataTemp.QuadPart);

    buff = new char[sz];
    DWORD bytesRead;
    if (ReadFile(hFile, buff, sz, &bytesRead, nullptr) == 0)
    {
        LogError("ReadFile failed. GetLastError()=%u", GetLastError());
        goto Cleanup;
    }

    for (int i = 0; i < sz; i++)
    {
        if (buff[i] == 0x0d)
            (*count)++;
    }

    if (*count <= 0)
        return;

    *arr = new int[*count];
    for (int j = 0, arrIndex = 0; j < sz;)
    {
        // seek the first number on the line
        while (!isdigit((unsigned char)buff[j]))
            j++;
        // read in the number
        (*arr)[arrIndex++] = atoi(&buff[j]);
        // seek to the start of next line
        while ((j < sz) && (buff[j] != 0x0a))
            j++;
        j++;
    }

Cleanup:
    if (buff != nullptr)
        delete[] buff;

    if (hFile != INVALID_HANDLE_VALUE)
        CloseHandle(hFile);
}

bool WriteArrayToMCL(char* mclFilename, int* arr, int count)
{
    HANDLE hMCLFile =
        CreateFileA(mclFilename, GENERIC_WRITE, FILE_SHARE_WRITE, NULL, CREATE_ALWAYS, FILE_ATTRIBUTE_NORMAL, NULL);
    bool result = true;

    if (hMCLFile == INVALID_HANDLE_VALUE)
    {
        LogError("Failed to open output file '%s'. GetLastError()=%u", mclFilename, GetLastError());
        result = false;
        goto Cleanup;
    }

    for (int i = 0; i < count; i++)
    {
        char  strMethodIndex[12];
        DWORD charCount    = 0;
        DWORD bytesWritten = 0;

        charCount = sprintf_s(strMethodIndex, sizeof(strMethodIndex), "%d\r\n", arr[i]);

        if (!WriteFile(hMCLFile, strMethodIndex, charCount, &bytesWritten, nullptr) || (bytesWritten != charCount))
        {
            LogError("Failed to write method index '%d'. GetLastError()=%u", strMethodIndex, GetLastError());
            result = false;
            goto Cleanup;
        }
    }

Cleanup:
    if (hMCLFile != INVALID_HANDLE_VALUE)
        CloseHandle(hMCLFile);

    return result;
}

void ProcessChildStdErr(char* stderrFilename)
{
    char buff[MAX_LOG_LINE_SIZE];

    FILE* fp = fopen(stderrFilename, "r");

    if (fp == NULL)
    {
        LogError("Unable to open '%s'.", stderrFilename);
        goto Cleanup;
    }

    while (fgets(buff, MAX_LOG_LINE_SIZE, fp) != NULL)
    {
        // get rid of the '\n' at the end of line
        size_t buffLen = strlen(buff);
        if (buff[buffLen - 1] == '\n')
            buff[buffLen - 1] = 0;

        LogPassThroughStderr("%s", buff);
    }

Cleanup:
    if (fp != NULL)
        fclose(fp);
}

void ProcessChildStdOut(const CommandLine::Options& o,
                        char*                       stdoutFilename,
                        int*                        loaded,
                        int*                        jitted,
                        int*                        failed,
                        int*                        excluded,
                        int*                        missing,
                        int*                        diffs,
                        bool*                       usageError)
{
    char buff[MAX_LOG_LINE_SIZE];

    FILE* fp = fopen(stdoutFilename, "r");

    if (fp == NULL)
    {
        LogError("Unable to open '%s'.", stdoutFilename);
        goto Cleanup;
    }

    while (fgets(buff, MAX_LOG_LINE_SIZE, fp) != NULL)
    {
        // get rid of the '\n' at the end of line
        size_t buffLen = strlen(buff);
        if (buff[buffLen - 1] == '\n')
            buff[buffLen - 1] = 0;

        if (strncmp(buff, g_SuperPMIUsageFirstLine, strlen(g_SuperPMIUsageFirstLine)) == 0)
        {
            *usageError = true; // Signals that we had a SuperPMI command line usage error
            LogPassThroughStdout("%s", buff);
        }
        else if (strncmp(buff, g_AllFormatStringFixedPrefix, strlen(g_AllFormatStringFixedPrefix)) == 0)
        {
            int childLoaded = 0, childJitted = 0, childFailed = 0, childExcluded = 0, childMissing = 0;
            if (o.applyDiff)
            {
                int childDiffs = 0;
                int converted  = sscanf_s(buff, g_AsmDiffsSummaryFormatString, &childLoaded, &childJitted, &childFailed,
                                         &childExcluded, &childMissing, &childDiffs);
                if (converted != 6)
                {
                    LogError("Couldn't parse status message: \"%s\"", buff);
                    continue;
                }
                *diffs += childDiffs;
            }
            else
            {
                int converted =
                    sscanf_s(buff, g_SummaryFormatString, &childLoaded, &childJitted, &childFailed, &childExcluded, &childMissing);
                if (converted != 5)
                {
                    LogError("Couldn't parse status message: \"%s\"", buff);
                    continue;
                }
                *diffs = -1;
            }
            *loaded += childLoaded;
            *jitted += childJitted;
            *failed += childFailed;
            *excluded += childExcluded;
            *missing += childMissing;
        }
        else
        {
            // Do output pass-through.
            // Note that the same logging verbosity level is passed to the child processes.
            LogPassThroughStdout("%s", buff);
        }
    }

Cleanup:
    if (fp != NULL)
    {
        fclose(fp);
    }
}

#ifndef TARGET_UNIX
BOOL WINAPI CtrlHandler(DWORD fdwCtrlType)
{
    // Since the child SuperPMI.exe processes share the same console
    // We don't need to kill them individually as they also receive the Ctrl-C

    closeRequested = 1; // set a flag to indicate we need to quit
    return TRUE;
}
#else // TARGET_UNIX
static void PosixCtrlHandler(int signum)
{
    (void)signum;
    closeRequested = 1;
}
#endif // TARGET_UNIX

int __cdecl compareInt(const void* arg1, const void* arg2)
{
    return (*(const int*)arg1) - (*(const int*)arg2);
}

struct PerWorkerData
{
#ifdef TARGET_UNIX
    int hStdOutput = -1;
    int hStdError  = -1;
#else
    HANDLE hStdOutput = INVALID_HANDLE_VALUE;
    HANDLE hStdError  = INVALID_HANDLE_VALUE;
#endif

    char* failingMCListPath = nullptr;
    char* detailsPath = nullptr;
    char* stdOutputPath = nullptr;
    char* stdErrorPath = nullptr;
    SpmiResult resultCode = SpmiResult::GeneralFailure;
};

static void MergeWorkerMCLs(char* mclFilename, PerWorkerData* workerData, int workerCount, char* PerWorkerData::*mclPath)
{
    int **MCL = new int *[workerCount], *MCLCount = new int[workerCount], totalCount = 0;

    for (int i = 0; i < workerCount; i++)
    {
        // Read the next partial MCL file
        ReadMCLToArray(workerData[i].*mclPath, &MCL[i], &MCLCount[i]);

        totalCount += MCLCount[i];
    }

    int* mergedMCL = new int[totalCount];
    int  index     = 0;

    for (int i = 0; i < workerCount; i++)
    {
        for (int j             = 0; j < MCLCount[i]; j++)
            mergedMCL[index++] = MCL[i][j];
    }

    qsort(mergedMCL, totalCount, sizeof(int), compareInt);

    // Write the merged MCL array back to disk
    if (!WriteArrayToMCL(mclFilename, mergedMCL, totalCount))
        LogError("Unable to write to MCL file %s.", mclFilename);

    delete[] MCL;
    delete[] MCLCount;
    delete[] mergedMCL;
}

static void MergeWorkerCsvs(char* csvFilename, PerWorkerData* workerData, int workerCount, char* PerWorkerData::* csvPath)
{
    FileWriter fw;
    if (!FileWriter::CreateNew(csvFilename, &fw))
    {
        LogError("Could not create file %s", csvFilename);
        return;
    }

    bool hasHeader = false;
    for (int i = 0; i < workerCount; i++)
    {
        switch (workerData[i].resultCode)
        {
            case SpmiResult::Success:
            case SpmiResult::Diffs:
            case SpmiResult::Misses:
            case SpmiResult::Error:
                break;

            default:
                LogWarning("Skipping merging CSV from child %d due to result code %d", i, (int)workerData[i].resultCode);
                continue;
        }

        FileLineReader reader;
        if (!FileLineReader::Open(workerData[i].*csvPath, &reader))
        {
            LogError("Could not open child CSV file %s", workerData[i].*csvPath);
            continue;
        }

        if (hasHeader && !reader.AdvanceLine())
        {
            continue;
        }

        while (reader.AdvanceLine())
        {
             fw.Printf("%s\n", reader.GetCurrentLine());
        }

        hasHeader = true;
    }
}

#define MAX_CMDLINE_SIZE 0x1000 // 4 KB

//-------------------------------------------------------------
// addJitOptionArgument: Writes jitOption arguments to the argument string for a child spmi process.
//
// Arguments:
//    jitOptions   -   map with options
//    bytesWritten -   size of the argument string in bytes
//    spmiArgs     -   pointer to the argument string
//    optionName   -   the jitOption name, can include [force] flag.
//
void addJitOptionArgument(LightWeightMap<DWORD, DWORD>* jitOptions,
                          int&        bytesWritten,
                          char*       spmiArgs,
                          const char* optionName)
{
    if (jitOptions != nullptr)
    {
        for (unsigned i = 0; i < jitOptions->GetCount(); i++)
        {
            const char* key   = (const char*)jitOptions->GetBuffer(jitOptions->GetKey(i));
            const char* value = (const char*)jitOptions->GetBuffer(jitOptions->GetItem(i));
            bytesWritten += sprintf_s(spmiArgs + bytesWritten, MAX_CMDLINE_SIZE - bytesWritten, " -%s %s=%s",
                                      optionName, key, value);
        }
    }
}

// From the arguments that we parsed, construct the arguments to pass to the child processes.
char* ConstructChildProcessArgs(const CommandLine::Options& o)
{
    int   bytesWritten = 0;
    char* spmiArgs     = new char[MAX_CMDLINE_SIZE];
    *spmiArgs          = '\0';

#define ADDSTRING(s)                                                                                                   \
    if (s != nullptr)                                                                                                  \
    {                                                                                                                  \
        bytesWritten += sprintf_s(spmiArgs + bytesWritten, MAX_CMDLINE_SIZE - bytesWritten, " %s", s);                 \
    }
#define ADDARG_BOOL(b, arg)                                                                                            \
    if (b)                                                                                                             \
    {                                                                                                                  \
        bytesWritten += sprintf_s(spmiArgs + bytesWritten, MAX_CMDLINE_SIZE - bytesWritten, " %s", arg);               \
    }
#define ADDARG_STRING(s, arg)                                                                                          \
    if (s != nullptr)                                                                                                  \
    {                                                                                                                  \
        bytesWritten += sprintf_s(spmiArgs + bytesWritten, MAX_CMDLINE_SIZE - bytesWritten, " %s %s", arg, s);         \
    }

    // Only pass through an integer argument if it is not the same as the default (which must be specified here).
    // (This is a proxy for "did the command-line parser actually parse something for this argument".)
#define ADDARG_INT(i, arg, defaultValue)                                                                               \
    if (i != defaultValue)                                                                                             \
    {                                                                                                                  \
        bytesWritten += sprintf_s(spmiArgs + bytesWritten, MAX_CMDLINE_SIZE - bytesWritten, " %s %d", arg, i);         \
    }

    // We don't pass through:
    //
    //    -parallel
    //    -writeLogFile (the parent process writes the log file based on the output of the child processes)
    //    -reproName
    //    -coredistools
    //    -skipCleanup
    //    -metricsSummary
    //    -baseMetricsSummary
    //    -diffMetricsSummary
    //    -failingMCList
    //    -diffMCList
    //
    // Everything else we need to reconstruct and pass through.
    //
    // Note that for -verbosity, if the level includes LOGLEVEL_INFO, the process will output a
    // "Loaded/Jitted/FailedCompile/Excluded/Missing[/Diffs]" line which is parsed and summarized by
    // the parent process. If it isn't output, the summary doesn't happen.

    ADDARG_BOOL(o.breakOnError, "-boe");
    ADDARG_BOOL(o.breakOnAssert, "-boa");
    ADDARG_BOOL(o.breakOnException, "-box");
    ADDARG_BOOL(o.ignoreStoredConfig, "-ignoreStoredConfig");
    ADDARG_BOOL(o.applyDiff, "-applyDiff");
    ADDARG_STRING(o.verbosity, "-verbosity");
    ADDARG_STRING(o.reproName, "-reproName");
    ADDARG_STRING(o.methodStatsTypes, "-emitMethodStats");
    ADDARG_STRING(o.hash, "-matchHash");
    ADDARG_STRING(o.targetArchitecture, "-target");
    ADDARG_STRING(o.compileList, "-compile");
    ADDARG_INT(o.failureLimit, "-failureLimit", -1);
    ADDARG_INT(o.repeatCount, "-repeatCount", 1);

    addJitOptionArgument(o.forceJitOptions, bytesWritten, spmiArgs, "jitoption force");
    addJitOptionArgument(o.forceJit2Options, bytesWritten, spmiArgs, "jit2option force");

    addJitOptionArgument(o.jitOptions, bytesWritten, spmiArgs, "jitoption");
    addJitOptionArgument(o.jit2Options, bytesWritten, spmiArgs, "jit2option");

    ADDSTRING(o.nameOfJit);
    ADDSTRING(o.nameOfJit2);
    ADDSTRING(o.nameOfInputMethodContextFile);

#undef ADDSTRING
#undef ADDARG_BOOL
#undef ADDARG_STRING
#undef ADDARG_INT

    return spmiArgs;
}

#ifdef TARGET_UNIX

static bool RegisterCtrlHandler()
{
    if (signal(SIGINT, PosixCtrlHandler) == SIG_ERR)
    {
        LogError("Failed to register SIGINT handler.");
        return false;
    }
    return true;
}

static bool GetTempFolderPath(char* tempPath)
{
    const char* tmpDir = getenv("TMPDIR");
    if (tmpDir == nullptr)
        tmpDir = "/tmp";
    snprintf(tempPath, MAX_PATH, "%s/", tmpDir);
    return true;
}

static int GetDefaultWorkerCount()
{
    long nprocs = sysconf(_SC_NPROCESSORS_ONLN);
    return (nprocs > 0) ? (int)nprocs : 1;
}

static bool GetCurrentExePath(char* path)
{
    char* exePath = minipal_getexepath();
    if (exePath == nullptr)
    {
        LogError("Failed to get current exe path.");
        return false;
    }
    strncpy(path, exePath, MAX_PATH - 1);
    path[MAX_PATH - 1] = '\0';
    free(exePath);
    return true;
}

static bool OpenWorkerOutputFiles(PerWorkerData& wd, int workerIndex)
{
    LogDebug("stdout %i=%s", workerIndex, wd.stdOutputPath);
    wd.hStdOutput = open(wd.stdOutputPath, O_WRONLY | O_CREAT | O_TRUNC | O_CLOEXEC, 0666);
    if (wd.hStdOutput == -1)
    {
        LogError("Unable to open '%s'. errno=%d", wd.stdOutputPath, errno);
        return false;
    }

    LogDebug("stderr %i=%s", workerIndex, wd.stdErrorPath);
    wd.hStdError = open(wd.stdErrorPath, O_WRONLY | O_CREAT | O_TRUNC | O_CLOEXEC, 0666);
    if (wd.hStdError == -1)
    {
        LogError("Unable to open '%s'. errno=%d", wd.stdErrorPath, errno);
        close(wd.hStdOutput);
        wd.hStdOutput = -1;
        return false;
    }
    return true;
}

static void CloseWorkerOutputFiles(PerWorkerData& wd)
{
    close(wd.hStdOutput);
    close(wd.hStdError);
}

// Returns a heap-allocated array of per-worker exit codes; caller must free with FreeWorkerExitCodes.
static int* WaitForWorkerProcesses(SpmiProcessHandle* handles, int count)
{
    int* exitCodes = new int[count];
    for (int i = 0; i < count; i++)
    {
        int status;
        pid_t result;
        do
        {
            result = waitpid(handles[i], &status, 0);
        } while (result == -1 && errno == EINTR);

        if (result == -1)
        {
            LogError("waitpid failed for child %d: %s", i, strerror(errno));
            exitCodes[i] = -1;
        }
        else if (WIFEXITED(status))
        {
            exitCodes[i] = WEXITSTATUS(status);
        }
        else
        {
            // Terminated by a signal (e.g., OOM killer).
            exitCodes[i] = -1;
        }
    }
    return exitCodes;
}

static bool GetWorkerExitCode(SpmiProcessHandle* handles, int* exitCodes, int workerIndex, unsigned& exitCode)
{
    if (exitCodes[workerIndex] == -1)
        return false;
    exitCode = (unsigned)exitCodes[workerIndex];
    return true;
}

static void FreeWorkerExitCodes(int* exitCodes)
{
    delete[] exitCodes;
}

#else // !TARGET_UNIX

static bool RegisterCtrlHandler()
{
    if (!SetConsoleCtrlHandler(CtrlHandler, TRUE))
    {
        LogError("Failed to set control handler.");
        return false;
    }
    return true;
}

static bool GetTempFolderPath(char* tempPath)
{
    if (!GetTempPath(MAX_PATH, tempPath))
    {
        LogError("Failed to get path to temp folder.");
        return false;
    }
    return true;
}

static int GetDefaultWorkerCount()
{
    SYSTEM_INFO sysinfo;
    GetSystemInfo(&sysinfo);

    int count = (int)sysinfo.dwNumberOfProcessors;

    // If we ever execute on a machine which has more than MAXIMUM_WAIT_OBJECTS(64) CPU cores
    // we still can't spawn more than the max supported by WaitForMultipleObjects()
    if (count > MAXIMUM_WAIT_OBJECTS)
        count = MAXIMUM_WAIT_OBJECTS;

    return count;
}

static bool GetCurrentExePath(char* path)
{
    if (!GetModuleFileName(NULL, path, MAX_PATH))
    {
        LogError("Failed to get current exe path.");
        return false;
    }
    return true;
}

static bool OpenWorkerOutputFiles(PerWorkerData& wd, int workerIndex)
{
    SECURITY_ATTRIBUTES sa;
    sa.nLength              = sizeof(sa);
    sa.lpSecurityDescriptor = NULL;
    sa.bInheritHandle       = TRUE; // Let newly created stdout/stderr handles be inherited.

    LogDebug("stdout %i=%s", workerIndex, wd.stdOutputPath);
    wd.hStdOutput = CreateFileA(wd.stdOutputPath, GENERIC_WRITE, FILE_SHARE_READ, &sa, CREATE_ALWAYS,
                                FILE_ATTRIBUTE_NORMAL, NULL);
    if (wd.hStdOutput == INVALID_HANDLE_VALUE)
    {
        LogError("Unable to open '%s'. GetLastError()=%u", wd.stdOutputPath, GetLastError());
        return false;
    }

    LogDebug("stderr %i=%s", workerIndex, wd.stdErrorPath);
    wd.hStdError = CreateFileA(wd.stdErrorPath, GENERIC_WRITE, FILE_SHARE_READ, &sa, CREATE_ALWAYS,
                               FILE_ATTRIBUTE_NORMAL, NULL);
    if (wd.hStdError == INVALID_HANDLE_VALUE)
    {
        LogError("Unable to open '%s'. GetLastError()=%u", wd.stdErrorPath, GetLastError());
        CloseHandle(wd.hStdOutput);
        wd.hStdOutput = INVALID_HANDLE_VALUE;
        return false;
    }
    return true;
}

static void CloseWorkerOutputFiles(PerWorkerData& wd)
{
    CloseHandle(wd.hStdOutput);
    CloseHandle(wd.hStdError);
}

// Returns nullptr; exit codes are retrieved lazily via GetWorkerExitCode / GetExitCodeProcess.
static int* WaitForWorkerProcesses(SpmiProcessHandle* handles, int count)
{
    DWORD waitResult = WaitForMultipleObjects((DWORD)count, handles, TRUE, INFINITE);
    if (waitResult == WAIT_FAILED)
    {
        LogError("WaitForMultipleObjects failed. GetLastError()=%u", GetLastError());
        return nullptr;
    }
    return nullptr;
}

static bool GetWorkerExitCode(SpmiProcessHandle* handles, int* exitCodes, int workerIndex, unsigned& exitCode)
{
    DWORD code;
    if (!GetExitCodeProcess(handles[workerIndex], &code))
        return false;
    exitCode = (unsigned)code;
    return true;
}

static void FreeWorkerExitCodes(int* exitCodes)
{
    // No-op on Windows; exit codes are retrieved via GetExitCodeProcess.
}

#endif // TARGET_UNIX

int doParallelSuperPMI(CommandLine::Options& o)
{
    HRESULT     hr = E_FAIL;
    SimpleTimer st;
    st.Start();

    if (!RegisterCtrlHandler())
        return 1;

    char tempPath[MAX_PATH];
    if (!GetTempFolderPath(tempPath))
        return 1;

    if (o.workerCount <= 0)
        o.workerCount = GetDefaultWorkerCount();

    // Obtain the path of the current executable, which we will use to spawn ourself.
    char* spmiFilename = new char[MAX_PATH];
    if (!GetCurrentExePath(spmiFilename))
        return 1;

    char* spmiArgs = ConstructChildProcessArgs(o);

    // TODO: merge all this output to a single call to LogVerbose to avoid all the newlines.
    LogVerbose("Using child (%s) with args (%s)", spmiFilename, spmiArgs);
    if (o.mclFilename != nullptr)
        LogVerbose(" failingMCList=%s", o.mclFilename);
    if (o.details != nullptr)
        LogVerbose(" details=%s", o.details);
    LogVerbose(" workerCount=%d, skipCleanup=%d.", o.workerCount, o.skipCleanup);

    PerWorkerData* perWorkerData = new PerWorkerData[o.workerCount];

    // Add a random number to the temporary file names to allow multiple parallel SuperPMI to happen at once.
    unsigned int randNumber = 0;
    minipal_get_non_cryptographically_secure_random_bytes((uint8_t*)&randNumber, sizeof(randNumber));

    for (int i = 0; i < o.workerCount; i++)
    {
        PerWorkerData& wd = perWorkerData[i];
        if (o.mclFilename != nullptr)
        {
            wd.failingMCListPath = new char[MAX_PATH];
            sprintf_s(wd.failingMCListPath, MAX_PATH, "%sParallelSuperPMI-%u-%d.mcl", tempPath, randNumber, i);
        }

        if (o.details != nullptr)
        {
            wd.detailsPath = new char[MAX_PATH];
            sprintf_s(wd.detailsPath, MAX_PATH, "%sParallelSuperPMI-Details-%u-%d.csv", tempPath, randNumber, i);
        }

        wd.stdOutputPath = new char[MAX_PATH];
        wd.stdErrorPath  = new char[MAX_PATH];

        sprintf_s(wd.stdOutputPath, MAX_PATH, "%sParallelSuperPMI-stdout-%u-%d.txt", tempPath, randNumber, i);
        sprintf_s(wd.stdErrorPath, MAX_PATH, "%sParallelSuperPMI-stderr-%u-%d.txt", tempPath, randNumber, i);
    }

    char cmdLine[MAX_CMDLINE_SIZE];
    cmdLine[0] = '\0';
    int bytesWritten;

    SpmiProcessHandle* hProcesses = new SpmiProcessHandle[o.workerCount];
    for (int i = 0; i < o.workerCount; i++)
    {
        bytesWritten = sprintf_s(cmdLine, MAX_CMDLINE_SIZE, "%s -stride %d %d", spmiFilename, i + 1, o.workerCount);

        PerWorkerData& wd = perWorkerData[i];

        if (wd.failingMCListPath != nullptr)
        {
            bytesWritten += sprintf_s(cmdLine + bytesWritten, MAX_CMDLINE_SIZE - bytesWritten, " -failingMCList %s",
                                      wd.failingMCListPath);
        }

        if (wd.detailsPath != nullptr)
        {
            bytesWritten += sprintf_s(cmdLine + bytesWritten, MAX_CMDLINE_SIZE - bytesWritten, " -details %s",
                                      wd.detailsPath);
        }

        bytesWritten += sprintf_s(cmdLine + bytesWritten, MAX_CMDLINE_SIZE - bytesWritten, " -v ewmin %s", spmiArgs);

        if (!OpenWorkerOutputFiles(wd, i))
            return -1;

        // Create a SuperPMI worker process and redirect its output to file
        if (!StartProcess(cmdLine, wd.hStdOutput, wd.hStdError, &hProcesses[i]))
            return -1;
    }

    int* exitCodes = WaitForWorkerProcesses(hProcesses, o.workerCount);

    // Close stdout/stderr
    for (int i = 0; i < o.workerCount; i++)
        CloseWorkerOutputFiles(perWorkerData[i]);

    SpmiResult result = SpmiResult::Success;

    if (!closeRequested)
    {
        // Figure out the error code to use.
        // Mainly, if any child returns non-zero, we want to return non-zero, to indicate failure.
        for (int i = 0; i < o.workerCount; i++)
        {
            unsigned exitCodeTmp;
            bool     ok = GetWorkerExitCode(hProcesses, exitCodes, i, exitCodeTmp);
            if (ok)
            {
                SpmiResult childResult = (SpmiResult)exitCodeTmp;

                switch (childResult)
                {
                case SpmiResult::Success:
                case SpmiResult::Diffs:
                case SpmiResult::Misses:
                case SpmiResult::Error:
                case SpmiResult::JitFailedToInit:
                    break;

                default:
                    // We may get here for OOM-killed, for example.
                    LogError("Child process %d exited with code %u", i, exitCodeTmp);
                    childResult = SpmiResult::GeneralFailure;
                    break;
                }

                perWorkerData[i].resultCode = childResult;

                if (childResult != result)
                {
                    // In priority: first failures are more important to propagate
                    if (result == SpmiResult::GeneralFailure || childResult == SpmiResult::GeneralFailure)
                    {
                        result = SpmiResult::GeneralFailure;
                    }
                    else if (result == SpmiResult::JitFailedToInit || childResult == SpmiResult::JitFailedToInit)
                    {
                        result = SpmiResult::JitFailedToInit;
                    }
                    else if (result == SpmiResult::Error || childResult == SpmiResult::Error)
                    {
                        result = SpmiResult::Error;
                    }
                    else if (result == SpmiResult::Diffs || childResult == SpmiResult::Diffs)
                    {
                        result = SpmiResult::Diffs;
                    }
                    else if (result == SpmiResult::Misses || childResult == SpmiResult::Misses)
                    {
                        result = SpmiResult::Misses;
                    }
                    // Keep success result
                }
            }
            else
            {
                LogError("Could not get exit code for child process %d\n", i);
                result = SpmiResult::GeneralFailure;
            }
        }

        bool usageError = false; // variable to flag if we hit a usage error in SuperPMI

        int loaded = 0, jitted = 0, failed = 0, excluded = 0, missing = 0, diffs = 0;

        // Read the stderr files and log them as errors
        // Read the stdout files and parse them for counts and log any MISSING or ISSUE errors
        for (int i = 0; i < o.workerCount; i++)
        {
            PerWorkerData& wd = perWorkerData[i];
            ProcessChildStdErr(wd.stdErrorPath);
            ProcessChildStdOut(o, wd.stdOutputPath, &loaded, &jitted, &failed, &excluded, &missing, &diffs, &usageError);

            if (usageError)
                break;
        }

        if (o.mclFilename != nullptr && !usageError)
        {
            // Concat the resulting .mcl files
            MergeWorkerMCLs(o.mclFilename, perWorkerData, o.workerCount, &PerWorkerData::failingMCListPath);
        }

        if (o.details != nullptr && !usageError)
        {
            // Concat the resulting diff .mcl files
            MergeWorkerCsvs(o.details, perWorkerData, o.workerCount, &PerWorkerData::detailsPath);
        }

        if (!usageError)
        {
            if (o.applyDiff)
            {
                LogInfo(g_AsmDiffsSummaryFormatString, loaded, jitted, failed, excluded, missing, diffs);
            }
            else
            {
                LogInfo(g_SummaryFormatString, loaded, jitted, failed, excluded, missing);
            }
        }

        st.Stop();
        LogVerbose("Total time: %fms", st.GetMilliseconds());
    }

    FreeWorkerExitCodes(exitCodes);

    if (!o.skipCleanup)
    {
        // Delete all temporary files generated
        for (int i = 0; i < o.workerCount; i++)
        {
            PerWorkerData& wd = perWorkerData[i];
            if (wd.failingMCListPath != nullptr)
            {
                remove(wd.failingMCListPath);
            }
            if (wd.detailsPath != nullptr)
            {
                remove(wd.detailsPath);
            }
            remove(wd.stdOutputPath);
            remove(wd.stdErrorPath);
        }
    }

    return (int)result;
}
