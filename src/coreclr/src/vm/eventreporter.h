//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//
//

//
//*****************************************************************************
// EventReporter.h:
// A utility to log an entry in event log.
//*****************************************************************************


#ifndef _eventreporter_h_
#define _eventreporter_h_

#include "contract.h"
#include "sstring.h"

// Maximum size for a string in event log entry
#define MAX_SIZE_EVENTLOG_ENTRY_STRING 0x8000 // decimal 32768

// The (approx.) maximum size that Vista appears to allow. Post discussion with the OS event log team,
// it has been identified that Vista has taken a breaking change in ReportEventW API implementation
// without getting it publicly documented.
//
// An event entry comprises of string to be written and event header information. Prior to Vista,
// 32K length strings were allowed and event header size was over it. Vista onwards, the total
// permissible length of the string and event header became 32K, resulting in strings becoming
// shorter in length. Hence, the change in size.
#define MAX_SIZE_EVENTLOG_ENTRY_STRING_WINVISTA 0x7C62 // decimal 31842 

class EventReporter
{
public:
    enum EventReporterType
    {
        ERT_UnhandledException,
        ERT_ManagedFailFast,
        ERT_UnmanagedFailFast,
        ERT_StackOverflow,
        ERT_CodeContractFailed,
    };
private:
    EventReporterType m_eventType;
    // We use 2048 which is large enough for most task.  This allows us to avoid
    // unnecessary memory allocation.
    InlineSString<2048> m_Description;

    // Flag to indicate if the buffer is full
    BOOL fBufferFull;

#ifdef FEATURE_CORECLR
    static void GetCoreCLRInstanceProductVersion(DWORD * pdwMajor, DWORD * pdwMinor, DWORD * pdwBuild, DWORD * pdwRevision);
#endif // FEATURE_CORECLR

public:
    // Construct 
    EventReporter(EventReporterType type);
    // Add extra info into description part of the log
    void AddDescription(__in WCHAR *pString);
    void AddDescription(SString& s);
    // Start callstack record
    void BeginStackTrace();
    // Add one frame to the callstack part
    void AddStackTrace(SString& s);
    // Report to the EventLog
    void Report();
};

// return TRUE if we need to log in EventLog.
BOOL ShouldLogInEventLog();
// Record managed callstack in EventReporter.
void LogCallstackForEventReporter(EventReporter& reporter);
// Generate a report in EventLog for unhandled exception for both managed and unmanaged.
void DoReportForUnhandledException(PEXCEPTION_POINTERS pExceptionInfo);

#endif // _eventreporter_h_
