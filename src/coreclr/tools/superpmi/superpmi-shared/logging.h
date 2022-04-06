// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

//----------------------------------------------------------
// Logging.h - Common logging and console output infrastructure
//----------------------------------------------------------
#ifndef _Logging
#define _Logging

//
// General purpose logging macros
//

#define LogMessage(level, ...) Logger::LogPrintf(__func__, __FILE__, __LINE__, level, __VA_ARGS__)

#define LogError(...) LogMessage(LOGLEVEL_ERROR, __VA_ARGS__)
#define LogWarning(...) LogMessage(LOGLEVEL_WARNING, __VA_ARGS__)
#define LogMissing(...) LogMessage(LOGLEVEL_MISSING, __VA_ARGS__)
#define LogInfo(...) LogMessage(LOGLEVEL_INFO, __VA_ARGS__)
#define LogVerbose(...) LogMessage(LOGLEVEL_VERBOSE, __VA_ARGS__)
#define LogDebug(...) LogMessage(LOGLEVEL_DEBUG, __VA_ARGS__)

#define LogIssue(issue, msg, ...) IssueLogger::LogIssueHelper(__FUNCTION__, __FILE__, __LINE__, issue, msg, __VA_ARGS__)

// Captures the exception message before throwing so we can log it at the point of occurrence
#define LogException(exCode, msg, ...)                                                                                 \
    do                                                                                                                 \
    {                                                                                                                  \
        Logger::LogExceptionMessage(__FUNCTION__, __FILE__, __LINE__, exCode, msg, __VA_ARGS__);                       \
        ThrowException(exCode, msg, __VA_ARGS__);                                                                      \
    } while (0)

// These are specified as flags so subsets of the logging functionality can be enabled/disabled at once
enum LogLevel : UINT32
{
    LOGLEVEL_ERROR   = 0x00000001, // Internal fatal errors that are non-recoverable
    LOGLEVEL_WARNING = 0x00000002, // Internal conditions that are unusual, but not serious
    LOGLEVEL_MISSING = 0x00000004, // Failures to due to missing JIT-EE details
    LOGLEVEL_ISSUE   = 0x00000008, // Issues found with the JIT, e.g. asm diffs, asserts
    LOGLEVEL_INFO    = 0x00000010, // Notifications/summaries, e.g. 'Loaded 5  Jitted 4  FailedCompile 1'
    LOGLEVEL_VERBOSE = 0x00000020, // Status messages, e.g. 'Jit startup took 151.12ms'
    LOGLEVEL_DEBUG   = 0x00000040  // Detailed output that's only useful for SuperPMI debugging
};

// Preset log level combinations
enum LogLevelMask : UINT32
{
    LOGMASK_NONE    = 0x00000000,
    LOGMASK_DEFAULT = (LOGLEVEL_DEBUG - 1), // Default is essentially "enable everything except debug"
    LOGMASK_ALL     = 0xffffffff
};

//
// Manages the SuperPMI logging subsystem, including both file-based logging and logging to the console.
//
class Logger
{
private:
    static bool             s_initialized;
    static UINT32           s_logLevel;
    static HANDLE           s_logFile;
    static char*            s_logFilePath;
    static CRITICAL_SECTION s_critSec;

public:
    static void Initialize();
    static void Shutdown();

    static void OpenLogFile(char* logFilePath);
    static void CloseLogFile();

    static UINT32 ParseLogLevelString(const char* specifierStr);
    static void SetLogLevel(UINT32 logLevelMask)
    {
        s_logLevel = logLevelMask;
    }
    static UINT32 GetLogLevel()
    {
        return s_logLevel;
    }

    // Return true if all specified log levels are enabled.
    static bool IsLogLevelEnabled(UINT32 logLevelMask)
    {
        return (logLevelMask & GetLogLevel()) == logLevelMask;
    }

    static void LogPrintf(const char* function, const char* file, int line, LogLevel level, const char* msg, ...);
    static void LogVprintf(
        const char* function, const char* file, int line, LogLevel level, va_list argList, const char* msg);
    static void LogExceptionMessage(
        const char* function, const char* file, int line, DWORD exceptionCode, const char* msg, ...);
};

enum IssueType
{
    ISSUE_ASSERT,
    ISSUE_ASM_DIFF
};

//
// JIT issues have more granularity than other types of log messages. The logging of issues is abstracted
// from the normal logger to reflect this. It also will enable us to track things specific to JIT issues,
// like statistics on issues that were found during a run.
//
class IssueLogger
{
public:
    static void LogIssueHelper(const char* function, const char* file, int line, IssueType issue, const char* msg, ...);
};

#endif
