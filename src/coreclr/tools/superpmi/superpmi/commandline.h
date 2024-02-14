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
        char* nameOfJit = nullptr;
        char* nameOfJit2 = nullptr;
        char* nameOfInputMethodContextFile = nullptr;
        char* verbosity = nullptr;
        char* writeLogFile = nullptr;
        char* reproName = nullptr;
        bool  breakOnError = false;
        bool  breakOnAssert = false;
        bool  breakOnException = false;
        bool  ignoreStoredConfig = false;
        bool  applyDiff = false;
        bool  parallel = false;        // User specified to use /parallel mode.
        char* streamFile = nullptr;
#if !defined(USE_MSVCDIS) && defined(USE_COREDISTOOLS)
        bool  useCoreDisTools = true; // Use CoreDisTools library instead of Msvcdis
#else
        bool  useCoreDisTools = false; // Use CoreDisTools library instead of Msvcdis
#endif
        bool  skipCleanup = false; // In /parallel mode, do we skip cleanup of temporary files? Used for debugging /parallel.
        int   workerCount = -1; // Number of workers to use for /parallel mode. -1 (or 1) means don't use parallel mode.
        int   indexCount = -1;  // If indexCount is -1 and hash points to nullptr it means compile all.
        int   failureLimit = -1; // Number of failures after which bail out the replay/asmdiffs.
        int   repeatCount = 1;   // Number of times given methods should be compiled.
        int*  indexes = nullptr;
        char* hash = nullptr;
        char* methodStatsTypes = nullptr;
        char* details = nullptr;
        char* mclFilename = nullptr;
        char* targetArchitecture = nullptr;
        char* compileList = nullptr;
        int   offset = -1;
        int   increment = -1;
        LightWeightMap<DWORD, DWORD>* forceJitOptions = nullptr;
        LightWeightMap<DWORD, DWORD>* forceJit2Options = nullptr;
        LightWeightMap<DWORD, DWORD>* jitOptions = nullptr;
        LightWeightMap<DWORD, DWORD>* jit2Options = nullptr;
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
