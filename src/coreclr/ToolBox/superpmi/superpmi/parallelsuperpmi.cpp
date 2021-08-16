// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "standardpch.h"
#include "superpmi.h"
#include "simpletimer.h"
#include "mclist.h"
#include "lightweightmap.h"
#include "commandline.h"
#include "errorhandling.h"

#define MAX_LOG_LINE_SIZE 0x1000 // 4 KB

bool closeRequested = false; // global variable to communicate CTRL+C between threads.

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

#if TARGET_UNIX
    const unsigned cmdLen = (unsigned)strlen(commandLine) + 1;
    WCHAR* cmdLineW = new WCHAR[cmdLen];
    MultiByteToWideChar(CP_UTF8, 0, commandLine, cmdLen, cmdLineW, cmdLen);
#endif
    // Start the child process.
    if (!CreateProcess(NULL,        // No module name (use command line)
#if TARGET_UNIX
                       cmdLineW,    // Command line
#else
                       commandLine, // Command line
#endif
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
#if TARGET_UNIX
        delete[] cmdLineW;
#endif
        return false;
    }

    *hProcess = pi.hProcess;

#if TARGET_UNIX
    delete[] cmdLineW;
#endif
    return true;
}

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

        if (strncmp(buff, "ERROR: ", 7) == 0)
            LogError("%s", &buff[7]); // log as Error and remove the "ERROR: " in front
        else if (strncmp(buff, "WARNING: ", 9) == 0)
            LogWarning("%s", &buff[9]); // log as Warning and remove the "WARNING: " in front
        else if (strlen(buff) > 0)
            LogWarning("%s", buff); // unknown output, log it as a warning
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

        if (strncmp(buff, "MISSING: ", 9) == 0)
            LogMissing("%s", &buff[9]); // log as Missing and remove the "MISSING: " in front
        else if (strncmp(buff, "ISSUE: ", 7) == 0)
        {
            if (strncmp(&buff[7], "<ASM_DIFF> ", 11) == 0)
                LogIssue(ISSUE_ASM_DIFF, "%s", &buff[18]); // log as Issue and remove the "ISSUE: <ASM_DIFF>" in front
            else if (strncmp(&buff[7], "<ASSERT> ", 9) == 0)
                LogIssue(ISSUE_ASSERT, "%s", &buff[16]); // log as Issue and remove the "ISSUE: <ASSERT>" in front
        }
        else if (strncmp(buff, g_SuperPMIUsageFirstLine, strlen(g_SuperPMIUsageFirstLine)) == 0)
        {
            *usageError = true; // Signals that we had a SuperPMI command line usage error

            // Read the entire stdout file and printf it
            printf("%s", buff);
            while (fgets(buff, MAX_LOG_LINE_SIZE, fp) != NULL)
            {
                printf("%s", buff);
            }
            break;
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
    }

Cleanup:
    if (fp != NULL)
    {
        fclose(fp);
    }
}

#ifndef TARGET_UNIX // TODO-Porting: handle Ctrl-C signals gracefully on Unix
BOOL WINAPI CtrlHandler(DWORD fdwCtrlType)
{
    // Since the child SuperPMI.exe processes share the same console
    // We don't need to kill them individually as they also receive the Ctrl-C

    closeRequested = true; // set a flag to indicate we need to quit
    return TRUE;
}
#endif // !TARGET_UNIX

int __cdecl compareInt(const void* arg1, const void* arg2)
{
    return (*(const int*)arg1) - (*(const int*)arg2);
}

// 'arrWorkerMCLPath' is an array of strings of size 'workerCount'.
void MergeWorkerMCLs(char* mclFilename, char** arrWorkerMCLPath, int workerCount)
{
    int **MCL = new int *[workerCount], *MCLCount = new int[workerCount], totalCount = 0;

    for (int i = 0; i < workerCount; i++)
    {
        // Read the next partial MCL file
        ReadMCLToArray(arrWorkerMCLPath[i], &MCL[i], &MCLCount[i]);

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
            WCHAR* key   = (WCHAR*)jitOptions->GetBuffer(jitOptions->GetKey(i));
            WCHAR* value = (WCHAR*)jitOptions->GetBuffer(jitOptions->GetItem(i));
            bytesWritten += sprintf_s(spmiArgs + bytesWritten, MAX_CMDLINE_SIZE - bytesWritten, " -%s %S=%S",
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

// We don't pass through /parallel, /skipCleanup, /verbosity, /failingMCList, or /diffMCList. Everything else we need to
// reconstruct and pass through.

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

    ADDARG_BOOL(o.breakOnError, "-boe");
    ADDARG_BOOL(o.breakOnAssert, "-boa");
    ADDARG_BOOL(o.applyDiff, "-applyDiff");
    ADDARG_STRING(o.reproName, "-reproName");
    ADDARG_STRING(o.writeLogFile, "-writeLogFile");
    ADDARG_STRING(o.methodStatsTypes, "-emitMethodStats");
    ADDARG_STRING(o.hash, "-matchHash");
    ADDARG_STRING(o.targetArchitecture, "-target");
    ADDARG_STRING(o.compileList, "-compile");

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

    return spmiArgs;
}

int doParallelSuperPMI(CommandLine::Options& o)
{
    HRESULT     hr = E_FAIL;
    SimpleTimer st;
    st.Start();

#ifndef TARGET_UNIX // TODO-Porting: handle Ctrl-C signals gracefully on Unix
    // Register a ConsoleCtrlHandler
    if (!SetConsoleCtrlHandler(CtrlHandler, TRUE))
    {
        LogError("Failed to set control handler.");
        return 1;
    }
#endif // !TARGET_UNIX

    char tempPath[MAX_PATH];
    if (!GetTempPath(MAX_PATH, tempPath))
    {
        LogError("Failed to get path to temp folder.");
        return 1;
    }

    if (o.workerCount <= 0)
    {
        // Use the default value which is the number of processors on the machine.
        SYSTEM_INFO sysinfo;
        GetSystemInfo(&sysinfo);

        o.workerCount = sysinfo.dwNumberOfProcessors;

        // If we ever execute on a machine which has more than MAXIMUM_WAIT_OBJECTS(64) CPU cores
        // we still can't spawn more than the max supported by WaitForMultipleObjects()
        if (o.workerCount > MAXIMUM_WAIT_OBJECTS)
            o.workerCount = MAXIMUM_WAIT_OBJECTS;
    }

    // Obtain the folder path of the current executable, which we will use to spawn ourself.
    char* spmiFilename = new char[MAX_PATH];
    if (!GetModuleFileName(NULL, spmiFilename, MAX_PATH))
    {
        LogError("Failed to get current exe path.");
        return 1;
    }

    char* spmiArgs = ConstructChildProcessArgs(o);

    // TODO: merge all this output to a single call to LogVerbose to avoid all the newlines.
    LogVerbose("Using child (%s) with args (%s)", spmiFilename, spmiArgs);
    if (o.mclFilename != nullptr)
        LogVerbose(" failingMCList=%s", o.mclFilename);
    if (o.diffMCLFilename != nullptr)
        LogVerbose(" diffMCLFilename=%s", o.diffMCLFilename);
    LogVerbose(" workerCount=%d, skipCleanup=%d.", o.workerCount, o.skipCleanup);

    HANDLE* hProcesses = new HANDLE[o.workerCount];
    HANDLE* hStdOutput = new HANDLE[o.workerCount];
    HANDLE* hStdError  = new HANDLE[o.workerCount];

    char** arrFailingMCListPath = new char*[o.workerCount];
    char** arrDiffMCListPath    = new char*[o.workerCount];
    char** arrStdOutputPath     = new char*[o.workerCount];
    char** arrStdErrorPath      = new char*[o.workerCount];

    // Add a random number to the temporary file names to allow multiple parallel SuperPMI to happen at once.
    unsigned int randNumber = 0;
#ifdef TARGET_UNIX
    PAL_Random(&randNumber, sizeof(randNumber));
#else  // !TARGET_UNIX
    rand_s(&randNumber);
#endif // !TARGET_UNIX

    for (int i = 0; i < o.workerCount; i++)
    {
        if (o.mclFilename != nullptr)
        {
            arrFailingMCListPath[i] = new char[MAX_PATH];
            sprintf_s(arrFailingMCListPath[i], MAX_PATH, "%sParallelSuperPMI-%u-%d.mcl", tempPath, randNumber, i);
        }
        else
        {
            arrFailingMCListPath[i] = nullptr;
        }

        if (o.diffMCLFilename != nullptr)
        {
            arrDiffMCListPath[i] = new char[MAX_PATH];
            sprintf_s(arrDiffMCListPath[i], MAX_PATH, "%sParallelSuperPMI-Diff-%u-%d.mcl", tempPath, randNumber, i);
        }
        else
        {
            arrDiffMCListPath[i] = nullptr;
        }

        arrStdOutputPath[i] = new char[MAX_PATH];
        arrStdErrorPath[i]  = new char[MAX_PATH];

        sprintf_s(arrStdOutputPath[i], MAX_PATH, "%sParallelSuperPMI-stdout-%u-%d.txt", tempPath, randNumber, i);
        sprintf_s(arrStdErrorPath[i], MAX_PATH, "%sParallelSuperPMI-stderr-%u-%d.txt", tempPath, randNumber, i);
    }

    char cmdLine[MAX_CMDLINE_SIZE];
    cmdLine[0] = '\0';
    int bytesWritten;

    for (int i = 0; i < o.workerCount; i++)
    {
        bytesWritten = sprintf_s(cmdLine, MAX_CMDLINE_SIZE, "%s -stride %d %d", spmiFilename, i + 1, o.workerCount);

        if (o.mclFilename != nullptr)
        {
            bytesWritten += sprintf_s(cmdLine + bytesWritten, MAX_CMDLINE_SIZE - bytesWritten, " -failingMCList %s",
                                      arrFailingMCListPath[i]);
        }

        if (o.diffMCLFilename != nullptr)
        {
            bytesWritten += sprintf_s(cmdLine + bytesWritten, MAX_CMDLINE_SIZE - bytesWritten, " -diffMCList %s",
                                      arrDiffMCListPath[i]);
        }

        if (o.failureLimit > 0)
        {
            bytesWritten += sprintf_s(cmdLine + bytesWritten, MAX_CMDLINE_SIZE - bytesWritten, " -failureLimit %d",
                                      o.failureLimit);
        }

        bytesWritten += sprintf_s(cmdLine + bytesWritten, MAX_CMDLINE_SIZE - bytesWritten, " -v ewmin %s", spmiArgs);

        SECURITY_ATTRIBUTES sa;
        sa.nLength              = sizeof(sa);
        sa.lpSecurityDescriptor = NULL;
        sa.bInheritHandle       = TRUE; // Let newly created stdout/stderr handles be inherited.

        LogDebug("stdout %i=%s", i, arrStdOutputPath[i]);
        hStdOutput[i] = CreateFileA(arrStdOutputPath[i], GENERIC_WRITE, FILE_SHARE_READ, &sa, CREATE_ALWAYS,
                                    FILE_ATTRIBUTE_NORMAL, NULL);
        if (hStdOutput[i] == INVALID_HANDLE_VALUE)
        {
            LogError("Unable to open '%s'. GetLastError()=%u", arrStdOutputPath[i], GetLastError());
            return -1;
        }

        LogDebug("stderr %i=%s", i, arrStdErrorPath[i]);
        hStdError[i] = CreateFileA(arrStdErrorPath[i], GENERIC_WRITE, FILE_SHARE_READ, &sa, CREATE_ALWAYS,
                                   FILE_ATTRIBUTE_NORMAL, NULL);
        if (hStdError[i] == INVALID_HANDLE_VALUE)
        {
            LogError("Unable to open '%s'. GetLastError()=%u", arrStdErrorPath[i], GetLastError());
            return -1;
        }

        // Create a SuperPMI worker process and redirect its output to file
        if (!StartProcess(cmdLine, hStdOutput[i], hStdError[i], &hProcesses[i]))
        {
            return -1;
        }
    }

    WaitForMultipleObjects(o.workerCount, hProcesses, true, INFINITE);

    // Close stdout/stderr
    for (int i = 0; i < o.workerCount; i++)
    {
        CloseHandle(hStdOutput[i]);
        CloseHandle(hStdError[i]);
    }

    SpmiResult result = SpmiResult::Success;

    if (!closeRequested)
    {
        // Figure out the error code to use.
        // Mainly, if any child returns non-zero, we want to return non-zero, to indicate failure.
        for (int i = 0; i < o.workerCount; i++)
        {
            DWORD      exitCodeTmp;
            BOOL       ok          = GetExitCodeProcess(hProcesses[i], &exitCodeTmp);
            SpmiResult childResult = (SpmiResult)exitCodeTmp;
            if (ok && (childResult != result))
            {
                if (result == SpmiResult::Error || childResult == SpmiResult::Error)
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
                else if (result == SpmiResult::JitFailedToInit || childResult == SpmiResult::JitFailedToInit)
                {
                    result = SpmiResult::JitFailedToInit;
                }
                else
                {
                    result = SpmiResult::GeneralFailure;
                }
            }
        }

        bool usageError = false; // variable to flag if we hit a usage error in SuperPMI

        int loaded = 0, jitted = 0, failed = 0, excluded = 0, missing = 0, diffs = 0;

        // Read the stderr files and log them as errors
        // Read the stdout files and parse them for counts and log any MISSING or ISSUE errors
        for (int i = 0; i < o.workerCount; i++)
        {
            ProcessChildStdErr(arrStdErrorPath[i]);
            ProcessChildStdOut(o, arrStdOutputPath[i], &loaded, &jitted, &failed, &excluded, &missing, &diffs, &usageError);
            if (usageError)
                break;
        }

        if (o.mclFilename != nullptr && !usageError)
        {
            // Concat the resulting .mcl files
            MergeWorkerMCLs(o.mclFilename, arrFailingMCListPath, o.workerCount);
        }

        if (o.diffMCLFilename != nullptr && !usageError)
        {
            // Concat the resulting diff .mcl files
            MergeWorkerMCLs(o.diffMCLFilename, arrDiffMCListPath, o.workerCount);
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

    if (!o.skipCleanup)
    {
        // Delete all temporary files generated
        for (int i = 0; i < o.workerCount; i++)
        {
            if (arrFailingMCListPath[i] != nullptr)
            {
                DeleteFile(arrFailingMCListPath[i]);
            }
            if (arrDiffMCListPath[i] != nullptr)
            {
                DeleteFile(arrDiffMCListPath[i]);
            }
            DeleteFile(arrStdOutputPath[i]);
            DeleteFile(arrStdErrorPath[i]);
        }
    }

    return (int)result;
}
