// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//
// FILE: dwreport.h
//
// This file contains declarations for functions used to report errors occurring
// in a process running managed code.
//

//

//
// ============================================================================

#ifndef __DWREPORT_H__
#define __DWREPORT_H__

// return values for DoFaultReport
enum FaultReportResult
{
    FaultReportResultAbort,
    FaultReportResultDebug,
    FaultReportResultQuit
};

void* GetBucketParametersForManagedException(UINT_PTR ip, TypeOfReportedError tore, Thread * pThread, OBJECTREF * exception);
void FreeBucketParametersForManagedException(void *pgmb);

HRESULT GetBucketParametersForCurrentException(BucketParameters *pParams);

//------------------------------------------------------------------------------
// DoFaultReport
//
// Description
//
// Parameters
//   pExceptionInfo -- information about the exception that caused the error.
//           If the error is not the result of an exception, pass NULL for this
//           parameter
// Returns
//   FaultReportResult -- enumeration indicating the
//   FaultReportResultAbort -- if Watson could not execute normally
//   FaultReportResultDebug -- if Watson executed normally, and the user
//              chose to debug the process
//   FaultReportResultQuit  -- if Watson executed normally, and the user
//              chose to end the process (e.g. pressed "Send Error Report" or
//              "Don't Send").
//
//------------------------------------------------------------------------------
FaultReportResult DoFaultReport(            // Was Watson attempted, successful?  Run debugger?
    EXCEPTION_POINTERS *pExceptionInfo,     // Information about the fault.
    TypeOfReportedError tore);              // What sort of error is reported.

BOOL IsWatsonEnabled();
BOOL RegisterOutOfProcessWatsonCallbacks();

int DwGetAssemblyVersion(               // Number of characters written.
    _In_z_ LPCWSTR  wszFilePath,         // Path to the executable.
    _Inout_updates_(cchBuf) WCHAR *pBuf, // Put description here.
    int cchBuf);

HRESULT DwGetFileVersionInfo(               // S_OK or error
    _In_z_ LPCWSTR wszFilePath,             // Path to the executable.
    USHORT& major,
    USHORT& minor,
    USHORT& build,
    USHORT& revision);

BOOL ContainsUnicodeChars(_In_z_ LPCWSTR wsz);

// Proxy parameters for Resetting Watson buckets
struct ResetWatsonBucketsParams
{
    Thread * m_pThread;
    EXCEPTION_RECORD * pExceptionRecord;
};
void ResetWatsonBucketsFavorWorker(void * pParam);

extern LONG g_watsonAlreadyLaunched;

//----------------------------------------------------------------------------
// Passes data between DoFaultReport and DoFaultReportCallback
//----------------------------------------------------------------------------
typedef enum tagEFaultRepRetVal EFaultRepRetVal;
struct FaultReportInfo
{
    EXCEPTION_POINTERS  /*in*/  *m_pExceptionInfo;
    EFaultRepRetVal     /*out*/  m_faultRepRetValResult;
};

VOID WINAPI DoFaultReportDoFavorCallback(LPVOID pFaultReportInfoAsVoid);

ContractFailureKind GetContractFailureKind(OBJECTREF obj);

#endif // __DWREPORT_H__
