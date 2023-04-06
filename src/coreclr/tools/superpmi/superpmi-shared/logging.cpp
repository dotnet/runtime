// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

//----------------------------------------------------------
// Logging.cpp - Common logging and console output infrastructure
//----------------------------------------------------------

#include "standardpch.h"
#include "logging.h"
#include "errorhandling.h"
#include <time.h>
#include <stdio.h>

//
// NOTE: Since the logging system is at the core of the error handling infrastructure, any errors
// that occur while logging will print a message to the console. Fatal errors trigger a debugbreak.
//

bool             Logger::s_initialized = false;
UINT32           Logger::s_logLevel    = LOGMASK_DEFAULT;
HANDLE           Logger::s_logFile     = INVALID_HANDLE_VALUE;
char*            Logger::s_logFilePath = nullptr;
CRITICAL_SECTION Logger::s_critSec;

//
// Initializes the logging subsystem. This must be called before invoking any of the logging functionality.
//
/* static */
void Logger::Initialize()
{
    if (!s_initialized)
    {
        InitializeCriticalSection(&s_critSec);
        s_initialized = true;
    }
}

//
// Shuts down the logging subsystem, freeing resources, closing handles, and such.
//
/* static */
void Logger::Shutdown()
{
    if (s_initialized)
    {
        DeleteCriticalSection(&s_critSec);
        CloseLogFile();
        s_initialized = false;
    }
}

//
// Opens a log file at the given path and enables file-based logging, if the given path is valid.
//
/* static */
void Logger::OpenLogFile(char* logFilePath)
{
    if (s_logFile == INVALID_HANDLE_VALUE && logFilePath != nullptr)
    {
        s_logFile = CreateFileA(logFilePath, GENERIC_WRITE, FILE_SHARE_READ | FILE_SHARE_DELETE, NULL, CREATE_ALWAYS,
                                FILE_ATTRIBUTE_NORMAL | FILE_FLAG_SEQUENTIAL_SCAN, NULL);

        if (s_logFile != INVALID_HANDLE_VALUE)
        {
            // We may need the file path later in order to delete the log file
            s_logFilePath = _strdup(logFilePath);
        }
        else
        {
            fprintf(stderr, "WARNING: [Logger::OpenLogFile] Failed to open log file '%s'. GetLastError()=%u\n",
                    logFilePath, GetLastError());
        }
    }
}

//
// Closes the currently open log file, if one is open.
//
/* static */
void Logger::CloseLogFile()
{
    if (s_logFile != INVALID_HANDLE_VALUE)
    {
        // Avoid polluting the file system with empty log files
        if (GetFileSize(s_logFile, nullptr) == 0 && s_logFilePath != nullptr)
        {
            // We can call this before closing the handle because remove just marks the file
            // for deletion, i.e. it does not actually get deleted until its last handle is closed.
            if (remove(s_logFilePath) == -1)
                fprintf(stderr, "WARNING: [Logger::CloseLogFile] remove failed. GetLastError()=%u\n",
                        GetLastError());
        }

        if (!CloseHandle(s_logFile))
            fprintf(stderr, "WARNING: [Logger::CloseLogFile] CloseHandle failed. GetLastError()=%u\n", GetLastError());

        s_logFile = INVALID_HANDLE_VALUE;

        free(s_logFilePath);
        s_logFilePath = nullptr;
    }
}

//
// Returns a bitmask representing the logging levels that are specified by the given string. The string
// format is described explicitly in the command-line usage documentation for SuperPMI and MCS.
//
// In essence, each log level has a corresponding character representing it, and the presence of that
// character in the specifier string indicates that the log mask should include that log level. The
// "quiet" level will override any other level specified: that is, if a "q" is present in the string,
// all other levels specified will be disregarded.
//
// If "q" is not specified, and "a" is specified, then all log levels are enabled. This is a shorthand
// to avoid looking up all the log levels and enabling them all by specifying all the individual characters.
//
/* static */
UINT32 Logger::ParseLogLevelString(const char* specifierStr)
{
    UINT32 logLevelMask = LOGMASK_NONE;

    if (strchr(specifierStr, 'q') == nullptr) // "Quiet" overrides all other specifiers
    {
        if (strchr(specifierStr, 'a') != nullptr) // "All" overrides the other specifiers
        {
            logLevelMask |= LOGMASK_ALL;
        }
        else
        {
            if (strchr(specifierStr, 'e') != nullptr)
                logLevelMask |= LOGLEVEL_ERROR;

            if (strchr(specifierStr, 'w') != nullptr)
                logLevelMask |= LOGLEVEL_WARNING;

            if (strchr(specifierStr, 'm') != nullptr)
                logLevelMask |= LOGLEVEL_MISSING;

            if (strchr(specifierStr, 'i') != nullptr)
                logLevelMask |= LOGLEVEL_ISSUE;

            if (strchr(specifierStr, 'n') != nullptr)
                logLevelMask |= LOGLEVEL_INFO;

            if (strchr(specifierStr, 'v') != nullptr)
                logLevelMask |= LOGLEVEL_VERBOSE;

            if (strchr(specifierStr, 'd') != nullptr)
                logLevelMask |= LOGLEVEL_DEBUG;
        }
    }

    return logLevelMask;
}

/* static */
void Logger::LogPrintf(const char* function, const char* file, int line, LogLevel level, const char* msg, ...)
{
    va_list argList;
    va_start(argList, msg);
    LogVprintf(function, file, line, level, argList, msg);
}

//
// Logs a message, if the given log level is enabled, to both the console and the log file. This is the
// main logging function that all other logging functions eventually funnel into.
//
/* static */
void Logger::LogVprintf(
    const char* function, const char* file, int line, LogLevel level, va_list argList, const char* msg)
{
    if (!s_initialized)
    {
        fprintf(stderr, "ERROR: [Logger::LogVprintf] Invoked the logging system before initializing it.\n");
        __debugbreak();
    }

    // Early out if we're not logging at this level.
    if (!IsPassThrough(level) && ((level & GetLogLevel()) == 0))
    {
        return;
    }

    // Capture this first to make the timestamp more accurately reflect the actual time of logging
    time_t timestamp = time(nullptr);

    int   fullMsgLen = _vscprintf(msg, argList) + 1; // This doesn't count the null terminator
    char* fullMsg    = new char[fullMsgLen];

    _vsnprintf_s(fullMsg, fullMsgLen, fullMsgLen, msg, argList);
    va_end(argList);

    // Where to write messages? Default to stdout.
    FILE* dest = stdout;

    const char* logLevelStr = "";
    switch (level)
    {
        case LOGLEVEL_ERROR:
            logLevelStr = "ERROR";
            dest = stderr;
            break;

        case LOGLEVEL_WARNING:
            logLevelStr = "WARNING";
            dest = stderr;
            break;

        case LOGLEVEL_MISSING:
            logLevelStr = "MISSING";
            break;

        case LOGLEVEL_ISSUE:
            logLevelStr = "ISSUE";
            break;

        case LOGLEVEL_INFO:
            logLevelStr = "INFO";
            break;

        case LOGLEVEL_VERBOSE:
            logLevelStr = "VERBOSE";
            break;

        case LOGLEVEL_DEBUG:
            logLevelStr = "DEBUG";
            break;

        case LOGLEVEL_PASSTHROUGH_STDOUT:
            logLevelStr = "STDOUT";
            break;

        case LOGLEVEL_PASSTHROUGH_STDERR:
            logLevelStr = "STDERR";
            dest = stderr;
            break;

        default:
            logLevelStr = "INVALID_LOGLEVEL";
            break;
    }

    // NOTE: This implementation doesn't guarantee that log messages will be written in chronological
    // order, since Windows doesn't guarantee FIFO behavior when a thread relinquishes a lock. If
    // maintaining chronological order is crucial, then we can implement a priority queueing system
    // for log messages.

    EnterCriticalSection(&s_critSec);

    if (level < LOGLEVEL_INFO)
        fprintf(dest, "%s: ", logLevelStr);

    fprintf(dest, "%s\n", fullMsg);

    if (s_logFile != INVALID_HANDLE_VALUE)
    {
#ifndef TARGET_UNIX // TODO: no localtime_s() or strftime() in PAL
        tm      timeInfo;
        errno_t err = localtime_s(&timeInfo, &timestamp);
        if (err != 0)
        {
            fprintf(stderr, "WARNING: [Logger::LogVprintf] localtime failed with error %d.\n", err);
            goto CleanUp;
        }

        size_t timeStrBuffSize = 20 * sizeof(char);
        char*  timeStr         = (char*)malloc(timeStrBuffSize); // Use malloc so we can realloc if necessary

        // This particular format string should always generate strings of the same size, but
        // for the sake of robustness, we shouldn't rely on that assumption.
        while (strftime(timeStr, timeStrBuffSize, "%Y-%m-%d %H:%M:%S", &timeInfo) == 0)
        {
            timeStrBuffSize *= 2;
            timeStr = (char*)realloc(timeStr, timeStrBuffSize);
        }
#else  // TARGET_UNIX
        const char* timeStr = "";
#endif // TARGET_UNIX

        const char logEntryFmtStr[] = "%s - %s [%s:%d] - %s - %s\r\n";
        size_t logEntryBuffSize = sizeof(logEntryFmtStr) + strlen(timeStr) + strlen(function) + strlen(file) + /* line number */ 10 +
                                  strlen(logLevelStr) + strlen(fullMsg);

        char* logEntry = new char[logEntryBuffSize];
        sprintf_s(logEntry, logEntryBuffSize, logEntryFmtStr, timeStr, function, file, line, logLevelStr, fullMsg);
        size_t logEntryLen = strlen(logEntry);

        DWORD bytesWritten;

        if (!WriteFile(s_logFile, logEntry, (DWORD)logEntryLen, &bytesWritten, nullptr))
            fprintf(stderr, "WARNING: [Logger::LogVprintf] Failed to write to log file. GetLastError()=%u\n",
                    GetLastError());

        if (!FlushFileBuffers(s_logFile))
            fprintf(stderr, "WARNING: [Logger::LogVprintf] Failed to flush log file. GetLastError()=%u\n",
                    GetLastError());

        delete[] logEntry;

#ifndef TARGET_UNIX
        free((void*)timeStr);
#endif // !TARGET_UNIX
    }

#ifndef TARGET_UNIX
CleanUp:
#endif // !TARGET_UNIX

    LeaveCriticalSection(&s_critSec);
    delete[] fullMsg;
}

//
// Special helper for logging exceptions. This logs the exception message given as a debug message.
//
/* static */
void Logger::LogExceptionMessage(
    const char* function, const char* file, int line, DWORD exceptionCode, const char* msg, ...)
{
    std::string fullMsg = "Exception thrown: ";
    fullMsg += msg;

    va_list argList;
    va_start(argList, msg);
    LogVprintf(function, file, line, LOGLEVEL_DEBUG, argList, fullMsg.c_str());
}

//
// Logger for JIT issues. Identifies the issue type and logs the given message normally.
//
/* static */
void IssueLogger::LogIssueHelper(
    const char* function, const char* file, int line, IssueType issue, const char* msg, ...)
{
    std::string fullMsg;

    switch (issue)
    {
        case ISSUE_ASSERT:
            fullMsg += "<ASSERT>";
            break;

        case ISSUE_ASM_DIFF:
            fullMsg += "<ASM_DIFF>";
            break;

        default:
            fullMsg += "<UNKNOWN_ISSUE_TYPE>";
            break;
    }

    fullMsg += " ";
    fullMsg += msg;

    va_list argList;
    va_start(argList, msg);
    Logger::LogVprintf(function, file, line, LOGLEVEL_ISSUE, argList, fullMsg.c_str());
}
