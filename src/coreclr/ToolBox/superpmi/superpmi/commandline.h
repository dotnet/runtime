// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

//----------------------------------------------------------
// CommandLine.h - tiny very specific command line parser
//----------------------------------------------------------
#ifndef _CommandLine
#define _CommandLine

class CommandLine
{
public:
    class Options
    {
    public:
        Options()
            : nameOfJit(nullptr)
            , nameOfJit2(nullptr)
            , nameOfInputMethodContextFile(nullptr)
            , writeLogFile(nullptr)
            , reproName(nullptr)
            , breakOnError(false)
            , breakOnAssert(false)
            , applyDiff(false)
            , parallel(false)
#if !defined(USE_MSVCDIS) && defined(USE_COREDISTOOLS)
            , useCoreDisTools(true) // if CoreDisTools is available (but MSVCDIS is not), use it.
#else
            , useCoreDisTools(false) // Otherwise, use MSVCDIS if that is available (else no diffs are available).
#endif
            , skipCleanup(false)
            , workerCount(-1)
            , indexCount(-1)
            , failureLimit(-1)
            , indexes(nullptr)
            , hash(nullptr)
            , methodStatsTypes(nullptr)
            , mclFilename(nullptr)
            , diffMCLFilename(nullptr)
            , targetArchitecture(nullptr)
            , compileList(nullptr)
            , offset(-1)
            , increment(-1)
            , forceJitOptions(nullptr)
            , forceJit2Options(nullptr)
            , jitOptions(nullptr)
            , jit2Options(nullptr)
        {
        }

        char* nameOfJit;
        char* nameOfJit2;
        char* nameOfInputMethodContextFile;
        char* writeLogFile;
        char* reproName;
        bool  breakOnError;
        bool  breakOnAssert;
        bool  applyDiff;
        bool  parallel;        // User specified to use /parallel mode.
        bool  useCoreDisTools; // Use CoreDisTools library instead of Msvcdis
        bool  skipCleanup; // In /parallel mode, do we skip cleanup of temporary files? Used for debugging /parallel.
        int   workerCount; // Number of workers to use for /parallel mode. -1 (or 1) means don't use parallel mode.
        int   indexCount;  // If indexCount is -1 and hash points to nullptr it means compile all.
        int   failureLimit; // Number of failures after which bail out the replay/asmdiffs.
        int*  indexes;
        char* hash;
        char* methodStatsTypes;
        char* mclFilename;
        char* diffMCLFilename;
        char* targetArchitecture;
        char* compileList;
        int   offset;
        int   increment;
        LightWeightMap<DWORD, DWORD>* forceJitOptions;
        LightWeightMap<DWORD, DWORD>* forceJit2Options;
        LightWeightMap<DWORD, DWORD>* jitOptions;
        LightWeightMap<DWORD, DWORD>* jit2Options;
    };

    static bool Parse(int argc, char* argv[], /* OUT */ Options* o);

    static bool AddJitOption(int&  currArgument,
                             int   argc,
                             char* argv[],
                             LightWeightMap<DWORD, DWORD>** pJitOptions,
                             LightWeightMap<DWORD, DWORD>** pForceJitOptions);

private:
    static void DumpHelp(const char* program);
};
#endif
