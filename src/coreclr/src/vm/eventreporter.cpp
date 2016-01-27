// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
//

//
//*****************************************************************************
// EventReporter.cpp
//
// A utility to log an entry in event log.
//
//*****************************************************************************


#include "common.h"
#include "utilcode.h"
#include "eventreporter.h"
#include "typestring.h"
#include "debugdebugger.h"

#include "../dlls/mscorrc/resource.h"

#if defined(FEATURE_CORECLR)
#include "getproductversionnumber.h"
#endif // FEATURE_CORECLR

//---------------------------------------------------------------------------------------
//
// A constructor for EventReporter.  The header of the log is generated here.
//
// Arguments:
//    type - Event report type
//
// Assumptions:
//    The argument type must be valid.
//
EventReporter::EventReporter(EventReporterType type)
{
    CONTRACTL
    {
        THROWS;
        GC_NOTRIGGER;
        MODE_ANY;
    }
    CONTRACTL_END;

    m_eventType = type;

    HMODULE hModule = WszGetModuleHandle(NULL);
    WCHAR appPath[MAX_LONGPATH];
    DWORD ret = WszGetModuleFileName(hModule, appPath, NumItems(appPath));

    fBufferFull = FALSE;

    InlineSString<256> ssMessage;

    if(!ssMessage.LoadResource(CCompRC::Optional, IDS_ER_APPLICATION))
        m_Description.Append(W("Application: "));
    else
    {
        m_Description.Append(ssMessage);
    }

    // If we were able to get an app name.
    if (ret != 0)
    {
        // If app name has a '\', consider the part after that; otherwise consider whole name.
        WCHAR* appName =  wcsrchr(appPath, W('\\'));
        appName = appName ? appName+1 : appPath;
        m_Description.Append(appName);
        m_Description.Append(W("\n"));
    }
    else
    {
        ssMessage.Clear();
        if(!ssMessage.LoadResource(CCompRC::Optional, IDS_ER_UNKNOWN))
            m_Description.Append(W("unknown\n"));
        else
        {
            m_Description.Append(ssMessage);
            m_Description.Append(W("\n"));
        }
    }

    ssMessage.Clear();
    if(!ssMessage.LoadResource(CCompRC::Optional, IDS_ER_FRAMEWORK_VERSION))
#ifndef FEATURE_CORECLR
        m_Description.Append(W("Framework Version: "));
#else // FEATURE_CORECLR
        m_Description.Append(W("CoreCLR Version: "));
#endif // !FEATURE_CORECLR
    else
    {
        m_Description.Append(ssMessage);
    }

    BOOL fHasVersion = FALSE;
#ifndef FEATURE_CORECLR
    if (GetCLRModule() != NULL)
    {
        WCHAR buffer[80];
        DWORD length;
        if (SUCCEEDED(GetCORVersionInternal(buffer, 80, &length)))
        {
            m_Description.Append(buffer);
            m_Description.Append(W("\n"));
            fHasVersion = TRUE;
        }
    }
#else // FEATURE_CORECLR
    DWORD dwMajorVersion = 0;
    DWORD dwMinorVersion = 0;
    DWORD dwBuild = 0;
    DWORD dwRevision = 0;
    EventReporter::GetCoreCLRInstanceProductVersion(&dwMajorVersion, &dwMinorVersion, &dwBuild, &dwRevision);
    m_Description.AppendPrintf(W("%lu.%lu.%lu.%lu\n"),dwMajorVersion, dwMinorVersion, dwBuild, dwRevision);
    fHasVersion = TRUE;
#endif // !FEATURE_CORECLR

    if (!fHasVersion)
    {
        ssMessage.Clear();
        if(!ssMessage.LoadResource(CCompRC::Optional, IDS_ER_UNKNOWN))
            m_Description.Append(W("unknown\n"));
        else
        {
            m_Description.Append(ssMessage);
            m_Description.Append(W("\n"));
        }        
    }

    ssMessage.Clear();

    switch(m_eventType) {
    case ERT_UnhandledException:
        if(!ssMessage.LoadResource(CCompRC::Optional, IDS_ER_UNHANDLEDEXCEPTION))
            m_Description.Append(W("Description: The process was terminated due to an unhandled exception."));
        else
        {
            m_Description.Append(ssMessage);
        }
        m_Description.Append(W("\n"));
        break;

    case ERT_ManagedFailFast:
        if(!ssMessage.LoadResource(CCompRC::Optional, IDS_ER_MANAGEDFAILFAST))
            m_Description.Append(W("Description: The application requested process termination through System.Environment.FailFast(string message)."));
        else
        {
            m_Description.Append(ssMessage);
        }
        m_Description.Append(W("\n"));
        break;

    case ERT_UnmanagedFailFast:
        if(!ssMessage.LoadResource(CCompRC::Optional, IDS_ER_UNMANAGEDFAILFAST))
            m_Description.Append(W("Description: The process was terminated due to an internal error in the .NET Runtime "));
        else
        {
            m_Description.Append(ssMessage);
        }
        break;

    case ERT_StackOverflow:
        // Fetch the localized Stack Overflow Error text or fall back on a hardcoded variant if things get dire.
        if(!ssMessage.LoadResource(CCompRC::Optional, IDS_ER_STACK_OVERFLOW))
            m_Description.Append(W("Description: The process was terminated due to stack overflow."));
        else
        {
            m_Description.Append(ssMessage);
        }
        m_Description.Append(W("\n"));
        break;

    case ERT_CodeContractFailed:
        if(!ssMessage.LoadResource(CCompRC::Optional, IDS_ER_CODECONTRACT_FAILED))
            m_Description.Append(W("Description: The application encountered a bug.  A managed code contract (precondition, postcondition, object invariant, or assert) failed."));
        else
        {
            m_Description.Append(ssMessage);
        }
        m_Description.Append(W("\n"));
        break;

    default:
        _ASSERTE(!"Unknown EventReporterType.");
        break;
    }
}


//---------------------------------------------------------------------------------------
//
// Add extra description to the EventLog report.
//
// Arguments:
//    pString - The extra description to append to log
//
// Return Value:
//    None.
//
void EventReporter::AddDescription(__in WCHAR *pString)
{
    CONTRACTL
    {
        THROWS;
        GC_NOTRIGGER;
        SO_INTOLERANT;
        MODE_ANY;
    }
    CONTRACTL_END;
    StackSString s(pString);
    AddDescription(s);
}

//---------------------------------------------------------------------------------------
//
// Add extra description to the EventLog report.
//
// Arguments:
//    pString - The extra description to append to log
//
// Return Value:
//    None.
//
void EventReporter::AddDescription(SString& s)
{
    CONTRACTL
    {
        THROWS;
        GC_NOTRIGGER;
        SO_INTOLERANT;
        MODE_ANY;
    }
    CONTRACTL_END;
    _ASSERTE (m_eventType == ERT_UnhandledException || m_eventType == ERT_ManagedFailFast ||
              m_eventType == ERT_UnmanagedFailFast || m_eventType == ERT_CodeContractFailed);
    if (m_eventType == ERT_ManagedFailFast)
    {
        SmallStackSString ssMessage;
        if(!ssMessage.LoadResource(CCompRC::Optional, IDS_ER_MANAGEDFAILFASTMSG))
            m_Description.Append(W("Message: "));
        else
        {
            m_Description.Append(ssMessage);
        }
    }
    else if (m_eventType == ERT_UnhandledException)
    {
        SmallStackSString ssMessage;
        if (!ssMessage.LoadResource(CCompRC::Optional, IDS_ER_UNHANDLEDEXCEPTIONMSG))
        {
            m_Description.Append(W("Exception Info: "));
        }
        else
        {
            m_Description.Append(ssMessage);
        }
    }
    else if (m_eventType == ERT_CodeContractFailed)
    {
        SmallStackSString ssMessage;
        if (!ssMessage.LoadResource(CCompRC::Optional, IDS_ER_CODECONTRACT_DETAILMSG))
            m_Description.Append(W("Contract details: "));
        else
            m_Description.Append(ssMessage);
    }
    m_Description.Append(s);
    m_Description.Append(W("\n"));
}

//---------------------------------------------------------------------------------------
//
// Add a marker for stack trace section in the EventLog entry
//
// Arguments:
//    None.
//
// Return Value:
//    None.
//
void EventReporter::BeginStackTrace()
{
    CONTRACTL
    {
        THROWS;
        GC_NOTRIGGER;
        SO_INTOLERANT;
        MODE_ANY;
    }
    CONTRACTL_END;
    _ASSERTE (m_eventType == ERT_UnhandledException || m_eventType == ERT_ManagedFailFast || m_eventType == ERT_CodeContractFailed);
    InlineSString<80> ssMessage;
    if(!ssMessage.LoadResource(CCompRC::Optional, IDS_ER_STACK))
        m_Description.Append(W("Stack:\n"));
    else
    {
        m_Description.Append(ssMessage);
        m_Description.Append(W("\n"));
    }
}

//---------------------------------------------------------------------------------------
//
// Add the signature of one managed stack frame into EventLog entry.
//
// Arguments:
//    s       - The signature of managed function, including argument type
//
// Return Value:
//    None.
//
void EventReporter::AddStackTrace(SString& s)
{
    WRAPPER_NO_CONTRACT;
    _ASSERTE (m_eventType == ERT_UnhandledException || m_eventType == ERT_ManagedFailFast || m_eventType == ERT_CodeContractFailed);

    // Continue to append to the buffer until we are full
    if (fBufferFull == FALSE)
    {
        m_Description.Append(s);
        m_Description.Append(W("\n"));

        COUNT_T curSize = m_Description.GetCount();

        // Truncate the buffer if we have exceeded the limit based upon the OS we are on
        DWORD dwMaxSizeLimit = MAX_SIZE_EVENTLOG_ENTRY_STRING_WINVISTA;
        if (curSize >= dwMaxSizeLimit)
        {
            // Load the truncation message
            StackSString truncate;
            if (!truncate.LoadResource(CCompRC::Optional, IDS_ER_MESSAGE_TRUNCATE))
            {
                truncate.Set(W("The remainder of the message was truncated."));
            }
            truncate.Insert(truncate.Begin(), W("\n"));
            truncate.Insert(truncate.End(), W("\n"));

            SString::Iterator ext;
            COUNT_T truncCount = truncate.GetCount();

            // Go back "truncCount" characters from the end of the string.
            // The "-1" in end is to accomodate null termination.
            ext = m_Description.Begin() + dwMaxSizeLimit - truncCount - 1;

            // Now look for a "\n" from the last position we got
            BOOL fFoundMarker = m_Description.FindBack(ext, W("\n"));
            if (ext != m_Description.Begin())
            {
                // Move to the next character if we found the "\n"
                if (fFoundMarker)
                   ext++;
            }

            // Truncate the string till our current position and append
            // the truncation message
            m_Description.Truncate(ext);
            m_Description.Append(truncate);

            // Set the flag that we are full - no point appending more stack details
            fBufferFull = TRUE;
        }
    }
}

//---------------------------------------------------------------------------------------
//
// Generate an entry in EventLog.
//
// Arguments:
//    None.
//
// Return Value:
//    None.
//
void EventReporter::Report()
{
    CONTRACTL
    {
        NOTHROW; 
        GC_NOTRIGGER;
    }
    CONTRACTL_END;

    DWORD eventID;
    switch (m_eventType)
    {
    case ERT_UnhandledException:
        eventID = 1026;
        break;
    case ERT_ManagedFailFast:
        eventID = 1025;
        break;
    case ERT_UnmanagedFailFast:
        eventID = 1023;
        break;
    case ERT_StackOverflow:
        eventID = 1027;
        break;
    case ERT_CodeContractFailed:
        eventID = 1028;
        break;
    default:
        _ASSERTE(!"Invalid event type");
        eventID = 1023;
        break;
    }

    CONTRACT_VIOLATION(ThrowsViolation);
    
    COUNT_T ctSize = m_Description.GetCount();
    LOG((LF_EH, LL_INFO100, "EventReporter::Report - Writing %d bytes to event log.\n", ctSize));

    if (ctSize > 0)
    {
        DWORD dwRetVal = ClrReportEvent(W(".NET Runtime"),
                       EVENTLOG_ERROR_TYPE,
                       0,
                       eventID,
                       NULL,
                       m_Description.GetUnicode()
                       );

        if (dwRetVal != ERROR_SUCCESS)
        {
            LOG((LF_EH, LL_INFO100, "EventReporter::Report - Error (win32 code %d) while writing to event log.\n", dwRetVal));

            // We were unable to log the error to event log - now check why.
            if ((dwRetVal != ERROR_EVENTLOG_FILE_CORRUPT) && (dwRetVal != ERROR_LOG_FILE_FULL) && 
                (dwRetVal != ERROR_NOT_ENOUGH_MEMORY)) // Writing to the log can fail under OOM (observed on Vista)
            {
                // If the event log file was neither corrupt nor full, then assert,
                // since something is wrong!
#ifndef _TARGET_ARM_
                //ARMTODO: Event reporting is currently non-functional on winpe.
                _ASSERTE(!"EventReporter::Report - Unable to log details to event log!");
#endif
            }
            else
            {
                // Since the event log file was either corrupt or full, simply
                // write this status to our log. We cannot fix a corrupt file
                // and we cannot clear the log since we dont administer the machine.
                STRESS_LOG0(LF_CORDB, LL_INFO10000, "EventReporter::Report: Event log is full, corrupt or not enough memory to process.\n");
            }
        }
    }
}


//---------------------------------------------------------------------------------------
//
// Check if we should generate an EventLog entry.
//
// Arguments:
//    None
//
// Return Value:
//    TRUE  - We should generate one entry
//    FALSE - We should not generate one entry
//
BOOL ShouldLogInEventLog()
{
    CONTRACTL
    {
        NOTHROW; 
        GC_NOTRIGGER;
    }
    CONTRACTL_END;

#ifndef FEATURE_CORESYSTEM
    // If the process is being debugged, don't log
    if ((CORDebuggerAttached() || IsDebuggerPresent())
#ifdef _DEBUG
        // Allow debug to be able to break in
        &&
        CLRConfig::GetConfigValue(CLRConfig::INTERNAL_BreakOnUncaughtException) == 0
#endif
        )
    {
        return FALSE;
    }

    static LONG fOnce = 0;
    if (fOnce == 1 || FastInterlockExchange(&fOnce, 1) == 1)
    {
        return FALSE;
    }

    if (CLRConfig::GetConfigValue(CLRConfig::EXTERNAL_logFatalError) == 0)
        return FALSE;
    else
        return TRUE;
#else
    // no event log on Apollo
    return FALSE;
#endif //!FEATURE_CORESYSTEM
}

//---------------------------------------------------------------------------------------
//
// A callback function for stack walker to save signature of one managed frame.
//
// Arguments:
//    pCF      - The frame info passed by stack walker.
//    pData    - The data to pass info between stack walker and its caller
//
// Return Value:
//    SWA_CONTINUE  - Continue search for the next frame.
//
struct LogCallstackData
{
    EventReporter *pReporter;
    SmallStackSString *pWordAt;
};

StackWalkAction LogCallstackForEventReporterCallback(
    CrawlFrame       *pCF,      //
    VOID*             pData     // Caller's private data
)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        SO_INTOLERANT;
        MODE_ANY;
    }
    CONTRACTL_END;

    EventReporter* pReporter = ((LogCallstackData*)pData)->pReporter;
    SmallStackSString *pWordAt = ((LogCallstackData*)pData)->pWordAt;

    MethodDesc *pMD = pCF->GetFunction();
    _ASSERTE(pMD != NULL);

    StackSString str;
    str = *pWordAt;

    TypeString::AppendMethodInternal(str, pMD, TypeString::FormatNamespace|TypeString::FormatFullInst|TypeString::FormatSignature); 
    pReporter->AddStackTrace(str);

    return SWA_CONTINUE;
}

//---------------------------------------------------------------------------------------
//
// A woker to save managed stack trace.
//
// Arguments:
//    reporter - EventReporter object for EventLog
//
// Return Value:
//    None
//
void LogCallstackForEventReporterWorker(EventReporter& reporter)
{
    Thread* pThread = GetThread();
    _ASSERTE (pThread);

    SmallStackSString WordAt;

    if (!WordAt.LoadResource(CCompRC::Optional, IDS_ER_WORDAT))
    {
        WordAt.Set(W("   at"));
    }
    else
    {
        WordAt.Insert(WordAt.Begin(), W("   "));
    }
    WordAt += W(" ");

    LogCallstackData data = {
        &reporter, &WordAt
    };

    pThread->StackWalkFrames(&LogCallstackForEventReporterCallback, &data, QUICKUNWIND | FUNCTIONSONLY);
}

//---------------------------------------------------------------------------------------
//
// Generate stack trace info for those managed frames on the stack currently.
//
// Arguments:
//    reporter - EventReporter object for EventLog
//
// Return Value:
//    None
//
void LogCallstackForEventReporter(EventReporter& reporter)
{
    WRAPPER_NO_CONTRACT;

    reporter.BeginStackTrace();

    LogCallstackForEventReporterWorker(reporter);
}

void ReportExceptionStackHelper(OBJECTREF exObj, EventReporter& reporter, SmallStackSString& wordAt, int recursionLimit)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_COOPERATIVE;
    }
    CONTRACTL_END;

    if (exObj == NULL || recursionLimit == 0)
    {
        return;
    }

    struct
    {
        OBJECTREF exObj;
        EXCEPTIONREF ex;
        STRINGREF remoteStackTraceString;
    } gc;
    ZeroMemory(&gc, sizeof(gc));
    gc.exObj = exObj;
    gc.ex = (EXCEPTIONREF)exObj;

    GCPROTECT_BEGIN(gc);

    ReportExceptionStackHelper((gc.ex)->GetInnerException(), reporter, wordAt, recursionLimit - 1);

    StackSString exTypeStr;
    TypeString::AppendType(exTypeStr, TypeHandle((gc.ex)->GetMethodTable()), TypeString::FormatNamespace | TypeString::FormatFullInst);
    reporter.AddDescription(exTypeStr);

    gc.remoteStackTraceString = (gc.ex)->GetRemoteStackTraceString();
    if (gc.remoteStackTraceString != NULL && gc.remoteStackTraceString->GetStringLength())
    {
        SString remoteStackTrace;
        gc.remoteStackTraceString->GetSString(remoteStackTrace);

        // If source info is contained, trim it
        StripFileInfoFromStackTrace(remoteStackTrace);

        reporter.AddStackTrace(remoteStackTrace);
    }

    DebugStackTrace::GetStackFramesData stackFramesData;
    stackFramesData.pDomain = NULL;
    stackFramesData.skip = 0;
    stackFramesData.NumFramesRequested = 0;

    DebugStackTrace::GetStackFramesFromException(&(gc.exObj), &stackFramesData);

    for (int j = 0; j < stackFramesData.cElements; j++)
    {
        StackSString str;
        str = wordAt;
        TypeString::AppendMethodInternal(str, stackFramesData.pElements[j].pFunc, TypeString::FormatNamespace | TypeString::FormatFullInst | TypeString::FormatSignature);
        reporter.AddStackTrace(str);
    }

    StackSString separator(L""); // This will result in blank line
    reporter.AddStackTrace(separator);

    GCPROTECT_END();
}


//---------------------------------------------------------------------------------------
//
// Generate an EventLog entry for unhandled exception.
//
// Arguments:
//    pExceptionInfo - Exception information
//
// Return Value:
//    None
//
void DoReportForUnhandledException(PEXCEPTION_POINTERS pExceptionInfo)
{
    WRAPPER_NO_CONTRACT;
    
    if (ShouldLogInEventLog())
    {
        Thread *pThread = GetThread();
        EventReporter reporter(EventReporter::ERT_UnhandledException);
        EX_TRY
        {
            StackSString s;
            if (pThread && pThread->HasException() != NULL)
            {
                GCX_COOP();
                struct
                {
                    OBJECTREF throwable;
                    STRINGREF originalExceptionMessage;
                } gc;
                ZeroMemory(&gc, sizeof(gc));
                
                GCPROTECT_BEGIN(gc);

                gc.throwable = pThread->GetThrowable();
                _ASSERTE(gc.throwable != NULL);

#ifdef FEATURE_CORECLR
                // On CoreCLR, managed code execution happens in non-default AppDomains and all threads have an AD transition
                // at their base from DefaultDomain to the target Domain before they start executing managed code. Thus, when 
                // an exception goes unhandled in a non-default AppDomain on a reverse pinvoke thread, the original exception details are copied 
                // to the Message property of System.CrossAppDomainMarshaledException instance at the AD transition boundary,
                // and the exception is then thrown in the calling AppDomain. This is done since CoreCLR does not support marshaling of 
                // objects across AppDomains.
                //
                // On SL, exceptions dont go unhandled to the OS. But in WLC, they can. Thus, when the scenario above happens for WLC,
                // the OS will invoke CoreCLR's registered UEF and reach here to write the stacktrace from the 
                // exception object (which will be a CrossAppDomainMarshaledException instance) to the event log. At this point,
                // we shall be in DefaultDomain. 
                //
                // However, the original exception details are in the Message property of CrossAppDomainMarshaledException. So, we should
                // look that up and if it is not empty, add those details to the EventReporter so that they get written to the
                // event log as well.
                //
                // We can also be here when in non-DefaultDomain an exception goes unhandled on a pure managed thread. In such a case,
                // we wont have CrossAppDomainMarshaledException instance but the original exception object that will be used to extract
                //  the stack trace from.
                if (pThread->GetDomain()->IsDefaultDomain())
                {
                    if (IsExceptionOfType(kCrossAppDomainMarshaledException, &(gc.throwable)))
                    {
                        // This is a CrossAppDomainMarshaledException instance - check if it has
                        // something for us in the Message property.
                        gc.originalExceptionMessage = ((EXCEPTIONREF)gc.throwable)->GetMessage();
                        if (gc.originalExceptionMessage != NULL)
                        {
                            // Ok - so, we have details about the original exception. Add them to the
                            // EventReporter object so that they get written to the event log.
                            reporter.AddDescription(gc.originalExceptionMessage->GetBuffer());

                            LOG((LF_EH, LL_INFO100, "DoReportForUnhandledException - Added original exception details to EventReporter from CrossAppDomainMarshaledException object.\n"));
                        }
                        else
                        {
                            LOG((LF_EH, LL_INFO100, "DoReportForUnhandledException - Original exception details not present in CrossAppDomainMarshaledException object.\n"));
                        }
                    }
                }
                else
#endif // FEATURE_CORECLR
                {
                    if (IsException(gc.throwable->GetMethodTable()))
                    {
                        SmallStackSString wordAt;
                        if (!wordAt.LoadResource(CCompRC::Optional, IDS_ER_WORDAT))
                        {
                            wordAt.Set(W("   at"));
                        }
                        else
                        {
                            wordAt.Insert(wordAt.Begin(), W("   "));
                        }
                        wordAt += W(" ");

                        ReportExceptionStackHelper(gc.throwable, reporter, wordAt, /* recursionLimit = */10);
                    }
                    else
                    {
                        TypeString::AppendType(s, TypeHandle(gc.throwable->GetMethodTable()), TypeString::FormatNamespace | TypeString::FormatFullInst);
                        reporter.AddDescription(s);
                        reporter.BeginStackTrace();
                        LogCallstackForEventReporterWorker(reporter);
                    }
                }

                GCPROTECT_END();
            }
            else
            {
                InlineSString<80> ssErrorFormat;
                if(!ssErrorFormat.LoadResource(CCompRC::Optional, IDS_ER_UNHANDLEDEXCEPTIONINFO))
                    ssErrorFormat.Set(W("exception code %1, exception address %2"));
                SmallStackSString exceptionCodeString;
                exceptionCodeString.Printf(W("%x"), pExceptionInfo->ExceptionRecord->ExceptionCode);
                SmallStackSString addressString;
                addressString.Printf(W("%p"), (UINT_PTR)pExceptionInfo->ExceptionRecord->ExceptionAddress);
                s.FormatMessage(FORMAT_MESSAGE_FROM_STRING, (LPCWSTR)ssErrorFormat, 0, 0, exceptionCodeString, addressString);
                reporter.AddDescription(s);
                if (pThread)
                {
                    LogCallstackForEventReporter(reporter);
                }
            }
        }
        EX_CATCH
        {
            // We are reporting an exception.  If we throw while working on this, it is not fatal.
        }
        EX_END_CATCH(SwallowAllExceptions);

        reporter.Report();
    }
}

#if defined(FEATURE_CORECLR)
// This function will return the product version of CoreCLR
// instance we are executing in.
void EventReporter::GetCoreCLRInstanceProductVersion(DWORD * pdwMajor, DWORD * pdwMinor, DWORD * pdwBuild, DWORD * pdwRevision)
{
    STATIC_CONTRACT_THROWS;

    // Get the instance of the runtime
    HMODULE hModRuntime = GetCLRModule();
    _ASSERTE(hModRuntime != NULL);

    // Get the path to the runtime
    WCHAR runtimePath[MAX_LONGPATH];
    DWORD ret = WszGetModuleFileName(hModRuntime, runtimePath, NumItems(runtimePath));
    if (ret != 0)
    {
        // Got the path - get the file version from the path
        SString path;
        path.Clear();
        path.Append(runtimePath);
        DWORD dwVersionMS = 0;
        DWORD dwVersionLS = 0;
        GetProductVersionNumber(path, &dwVersionMS, &dwVersionLS);

        // Get the Major.Minor.Build.Revision details from the returned values
        *pdwMajor = HIWORD(dwVersionMS);
        *pdwMinor = LOWORD(dwVersionMS);
        *pdwBuild = HIWORD(dwVersionLS);
        *pdwRevision = LOWORD(dwVersionLS);
        LOG((LF_CORDB, LL_INFO100, "GetCoreCLRInstanceVersion: Got CoreCLR version: %lu.%lu.%lu.%lu\n",
            *pdwMajor, *pdwMinor, *pdwBuild, *pdwRevision));
    }
    else
    {
        // Failed to get the path
        LOG((LF_CORDB, LL_INFO100, "GetCoreCLRInstanceVersion: Unable to get CoreCLR version.\n"));
    }
}
#endif // FEATURE_CORECLR
