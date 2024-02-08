// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
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

// The (approx.) maximum size that EventLog appears to allow.
//
// An event entry comprises of string to be written and event header information.
// The total permissible length of the string and event header is 32K.
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

public:
    // Construct
    EventReporter(EventReporterType type);
    // Add extra info into description part of the log
    void AddDescription(_In_ WCHAR *pString);
    void AddDescription(SString& s);
    // Start callstack record
    void BeginStackTrace();
    // Add one frame to the callstack part
    void AddStackTrace(SString& s);
    // Add failfast stack trace
    void AddFailFastStackTrace(SString& s);
    // Report to the EventLog
    void Report();
};

// return TRUE if we need to log in EventLog.
BOOL ShouldLogInEventLog();
// Record managed callstack in EventReporter.
void LogCallstackForEventReporter(EventReporter& reporter);
// Record unhandled native exceptions.
void DoReportForUnhandledNativeException(PEXCEPTION_POINTERS pExceptionInfo);
// Helper method for logging stack trace in EventReporter
void ReportExceptionStackHelper(OBJECTREF exObj, EventReporter& reporter, SmallStackSString& wordAt, int recursionLimit);
#endif // _eventreporter_h_
