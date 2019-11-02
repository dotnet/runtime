// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#include "stdafx.h"
#include "perflog.h"
#include "jitperf.h"
#include <limits.h>
#include "sstring.h"

//=============================================================================
// ALL THE PERF LOG CODE IS COMPILED ONLY IF ENABLE_PERF_LOG IS DEFINED.
#if defined (ENABLE_PERF_LOG)
//=============================================================================

//-----------------------------------------------------------------------------
// Widechar strings representing the units in UnitOfMeasure. *** Keep in sync  ***
// with the array defined in PerfLog.cpp
const WCHAR * const wszUnitOfMeasureDescr[MAX_UNITS_OF_MEASURE] =
{
    W(""),
    W("sec"),
    W("Bytes"),
    W("KBytes"),
    W("KBytes/sec"),
    W("cycles")
};

//-----------------------------------------------------------------------------
// Widechar strings representing the "direction" property of above units.
// *** Keep in sync  *** with the array defined in PerfLog.cpp
// "Direction" property is false if an increase in the value of the counter indicates
// a degrade.
// "Direction" property is true if an increase in the value of the counter indicates
// an improvement.
const WCHAR * const wszIDirection[MAX_UNITS_OF_MEASURE] =
{
    W("false"),
    W("false"),
    W("false"),
    W("false"),
    W("true"),
    W("false")
};

//-----------------------------------------------------------------------------
// Initialize static variables of the PerfLog class.
bool PerfLog::m_perfLogInit = false;
WCHAR PerfLog::m_wszOutStr_1[];
DWORD PerfLog::m_dwWriteByte = 0;
int PerfLog::m_fLogPerfData = 0;
HANDLE PerfLog::m_hPerfLogFileHandle = 0;
bool PerfLog::m_perfAutomationFormat = false;
bool PerfLog::m_commaSeparatedFormat = false;

//-----------------------------------------------------------------------------
// Initliaze perf logging. Must be called before calling PERFLOG (x)...
void PerfLog::PerfLogInitialize()
{
    LIMITED_METHOD_CONTRACT;

    // Make sure we are called only once.
    if (m_perfLogInit)
    {
        return;
    }

    // First check for special cases:

#if defined(ENABLE_JIT_PERF)
    // Checks the JIT_PERF_OUTPUT env var and sets g_fJitPerfOn.
    InitJitPerf();
#endif

#ifdef WS_PERF
    // Private working set perf stats
    InitWSPerf();
#endif // WS_PERF

    // Put other special cases here.

    // <TODO>@TODO agk: clean this logic a bit</TODO>
    // Special cases considered. Now turn on loggin if any of above want logging
    // or if PERF_OUTPUT says so.

    InlineSString<4> lpszValue;
    // Read the env var PERF_OUTPUT and if set continue.
    m_fLogPerfData = WszGetEnvironmentVariable (W("PERF_OUTPUT"), lpszValue);

#if defined(ENABLE_JIT_PERF)
    if (!m_fLogPerfData)
    {
        // Make sure that JIT perf was not requested.
        if (!g_fJitPerfOn)
            return;

        // JIT perf stats are needed so set the flags also.
        m_fLogPerfData = 1;
    }
#endif

    // See if we want to output to the database
    PathString _lpszValue;
    DWORD _cchValue = 10; // 11 - 1
    _cchValue = WszGetEnvironmentVariable (W("PerfOutput"), _lpszValue);
    if (_cchValue && (wcscmp (_lpszValue, W("DBase")) == 0))
        m_perfAutomationFormat = true;
    if (_cchValue && (wcscmp (_lpszValue, W("CSV")) == 0))
        m_commaSeparatedFormat = true;

    if (PerfAutomationFormat() || CommaSeparatedFormat())
    {
        // Hardcoded file name for spitting the perf auotmation formatted perf data. Open
        // the file here for writing and close in PerfLogDone().
        m_hPerfLogFileHandle = WszCreateFile (
#ifdef PLATFORM_UNIX
                                              W("/tmp/PerfData.dat"),
#else
                                              W("C:\\PerfData.dat"),
#endif
                                              GENERIC_WRITE,
                                              FILE_SHARE_WRITE,
                                              0,
                                              OPEN_ALWAYS,
                                              FILE_ATTRIBUTE_NORMAL,
                                              0);

        // check return value
        if(m_hPerfLogFileHandle == INVALID_HANDLE_VALUE)
        {
            m_fLogPerfData = 0;
            goto ErrExit;
        }

        // Make sure we append to the file.  <TODO>@TODO agk: Is this necessary?</TODO>
        if(SetFilePointer (m_hPerfLogFileHandle, 0, NULL, FILE_END) == INVALID_SET_FILE_POINTER )
        {
            CloseHandle (m_hPerfLogFileHandle);
            m_fLogPerfData = 0;
            goto ErrExit;
        }
    }

    m_perfLogInit = true;

ErrExit:
    return;
}

// Wrap up...
void PerfLog::PerfLogDone()
{
    LIMITED_METHOD_CONTRACT;

#if defined(ENABLE_JIT_PERF)
    DoneJitPerfStats();
#endif

#ifdef WS_PERF
    // Private working set perf
    OutputWSPerfStats();
#endif // WS_PERF

    if (CommaSeparatedFormat())
    {
        if (0 == WriteFile (m_hPerfLogFileHandle, "\n", (DWORD)strlen("\n"), &m_dwWriteByte, NULL))
            printf("ERROR: Could not write to perf log.\n");
    }

    if (PerfLoggingEnabled())
        CloseHandle (m_hPerfLogFileHandle);
}

void PerfLog::OutToStdout(__in_z const WCHAR *wszName, UnitOfMeasure unit, __in_opt const WCHAR *wszDescr)
{
    LIMITED_METHOD_CONTRACT;

    WCHAR wszOutStr_2[PRINT_STR_LEN];

    if (wszDescr)
        _snwprintf_s(wszOutStr_2, PRINT_STR_LEN, PRINT_STR_LEN - 1, W(" (%s)\n"), wszDescr);
    else
        _snwprintf_s(wszOutStr_2, PRINT_STR_LEN, PRINT_STR_LEN - 1, W("\n"));

    printf("%S", m_wszOutStr_1);
    printf("%S", wszOutStr_2);
}

void PerfLog::OutToPerfFile(__in_z const WCHAR *wszName, UnitOfMeasure unit, __in_opt const WCHAR *wszDescr)
{
    LIMITED_METHOD_CONTRACT;

    char szPrintStr[PRINT_STR_LEN];

    if (CommaSeparatedFormat())
    {
        if (WszWideCharToMultiByte (CP_ACP, 0, m_wszOutStr_1, -1, szPrintStr, PRINT_STR_LEN-1, 0, 0) ) {
            if (0 == WriteFile (m_hPerfLogFileHandle, szPrintStr, (DWORD)strlen(szPrintStr), &m_dwWriteByte, NULL))
                printf("ERROR: Could not write to perf log.\n");
        }
        else
            wprintf(W("ERROR: Could not do string conversion.\n"));
    }
    else
    {
        WCHAR wszOutStr_2[PRINT_STR_LEN];

        // workaround. The formats for ExecTime is slightly different from a custom value.
        if (wcscmp(wszName, W("ExecTime")) == 0)
            _snwprintf_s(wszOutStr_2, PRINT_STR_LEN, PRINT_STR_LEN - 1, W("ExecUnitDescr=%s\nExecIDirection=%s\n"), wszDescr, wszIDirection[unit]);
        else
        {
            if (wszDescr)
                _snwprintf_s(wszOutStr_2, PRINT_STR_LEN, PRINT_STR_LEN - 1, W("%s Descr=%s\n%s Unit Descr=None\n%s IDirection=%s\n"), wszName, wszDescr, wszName, wszName, wszIDirection[unit]);
            else
                _snwprintf_s(wszOutStr_2, PRINT_STR_LEN, PRINT_STR_LEN - 1, W("%s Descr=None\n%s Unit Descr=None\n%s IDirection=%s\n"), wszName, wszName, wszName, wszIDirection[unit]);
        }

        // Write both pieces to the file.
        if(WszWideCharToMultiByte (CP_ACP, 0, m_wszOutStr_1, -1, szPrintStr, PRINT_STR_LEN-1, 0, 0) ) {
            if (0 == WriteFile (m_hPerfLogFileHandle, szPrintStr, (DWORD)strlen(szPrintStr), &m_dwWriteByte, NULL))
                printf("ERROR: Could not write to perf log.\n");
        }
        else
            wprintf(W("ERROR: Could not do string conversion.\n"));

        if(WszWideCharToMultiByte (CP_ACP, 0, wszOutStr_2, -1, szPrintStr, PRINT_STR_LEN-1, 0, 0)) {
            if (0 == WriteFile (m_hPerfLogFileHandle, szPrintStr, (DWORD)strlen(szPrintStr), &m_dwWriteByte, NULL))
                printf("ERROR: Could not write to perf log.\n");
        }
        else
            wprintf(W("ERROR: Could not do string conversion.\n"));
    }
}

// Output stats in pretty print to stdout and perf automation format to file
// handle m_hPerfLogFileHandle
void PerfLog::Log(__in_z const WCHAR *wszName, UINT64 val, UnitOfMeasure unit, __in_opt const WCHAR *wszDescr)
{
    LIMITED_METHOD_CONTRACT;

    // Format the output into two pieces: The first piece is formatted here, rest in OutToStdout.
    _snwprintf_s(m_wszOutStr_1, PRINT_STR_LEN, PRINT_STR_LEN - 1, W("%-30s%12.3I64u %s"), wszName, val, wszUnitOfMeasureDescr[unit]);
    OutToStdout (wszName, unit, wszDescr);

    // Format the output into two pieces: The first piece is formatted here, rest in OutToPerfFile
    if (CommaSeparatedFormat())
    {
        _snwprintf_s(m_wszOutStr_1, PRINT_STR_LEN, PRINT_STR_LEN - 1, W("%s;%0.3I64u;"), wszName, val);
        OutToPerfFile (wszName, unit, wszDescr);
    }

    if (PerfAutomationFormat())
    {
        // workaround, Special case for ExecTime. since the format is slightly different than for custom value.
        if (wcscmp(wszName, W("ExecTime")) == 0)
            _snwprintf_s(m_wszOutStr_1, PRINT_STR_LEN, PRINT_STR_LEN - 1, W("%s=%0.3I64u\nExecUnit=%s\n"), wszName, val, wszUnitOfMeasureDescr[unit]);
        else
            _snwprintf_s(m_wszOutStr_1, PRINT_STR_LEN, PRINT_STR_LEN - 1, W("%s=%0.3I64u\n%s Unit=%s\n"), wszName, val, wszName, wszUnitOfMeasureDescr[unit]);
        OutToPerfFile (wszName, unit, wszDescr);
    }
}

// Output stats in pretty print to stdout and perf automation format to file
// handle m_hPerfLogFileHandle
void PerfLog::Log(__in_z const WCHAR *wszName, double val, UnitOfMeasure unit, __in_opt const WCHAR *wszDescr)
{
    LIMITED_METHOD_CONTRACT;

    // Format the output into two pieces: The first piece is formatted here, rest in OutToStdout.
    _snwprintf_s(m_wszOutStr_1, PRINT_STR_LEN, PRINT_STR_LEN - 1, W("%-30s%12.3g %s"), wszName, val, wszUnitOfMeasureDescr[unit]);
    OutToStdout (wszName, unit, wszDescr);

    // Format the output into two pieces: The first piece is formatted here, rest in OutToPerfFile
    if (CommaSeparatedFormat())
    {
        _snwprintf_s(m_wszOutStr_1, PRINT_STR_LEN, PRINT_STR_LEN - 1, W("%s;%0.3g;"), wszName, val);
        OutToPerfFile (wszName, unit, wszDescr);
    }

    if (PerfAutomationFormat())
    {
        // workaround, Special case for ExecTime. since the format is slightly different than for custom value.
        if (wcscmp(wszName, W("ExecTime")) == 0)
            _snwprintf_s(m_wszOutStr_1, PRINT_STR_LEN, PRINT_STR_LEN - 1,  W("%s=%0.3g\nExecUnit=%s\n"), wszName, val, wszUnitOfMeasureDescr[unit]);
        else
            _snwprintf_s(m_wszOutStr_1, PRINT_STR_LEN, PRINT_STR_LEN - 1, W("%s=%0.3g\n%s Unit=%s\n"), wszName, val, wszName, wszUnitOfMeasureDescr[unit]);
        OutToPerfFile (wszName, unit, wszDescr);
    }
}

// Output stats in pretty print to stdout and perf automation format to file
// handle m_hPerfLogFileHandle
void PerfLog::Log(__in_z const WCHAR *wszName, UINT32 val, UnitOfMeasure unit, __in_opt const WCHAR *wszDescr)
{
    LIMITED_METHOD_CONTRACT;

    // Format the output into two pieces: The first piece is formatted here, rest in OutToStdout.

    _snwprintf_s(m_wszOutStr_1, PRINT_STR_LEN, PRINT_STR_LEN - 1, W("%-30s%12d %s"), wszName, val, wszUnitOfMeasureDescr[unit]);
    OutToStdout (wszName, unit, wszDescr);

    // Format the output into two pieces: The first piece is formatted here, rest in OutToPerfFile
    if (CommaSeparatedFormat())
    {
        _snwprintf_s(m_wszOutStr_1, PRINT_STR_LEN, PRINT_STR_LEN - 1, W("%s;%d;"), wszName, val);
        OutToPerfFile (wszName, unit, wszDescr);
    }

    if (PerfAutomationFormat())
    {
        // workaround, Special case for ExecTime. since the format is slightly different than for custom value.
        if (wcscmp(wszName, W("ExecTime")) == 0)
            _snwprintf_s(m_wszOutStr_1, PRINT_STR_LEN, PRINT_STR_LEN - 1, W("%s=%0.3d\nExecUnit=%s\n"), wszName, val, wszUnitOfMeasureDescr[unit]);
        else
            _snwprintf_s(m_wszOutStr_1, PRINT_STR_LEN, PRINT_STR_LEN - 1, W("%s=%0.3d\n%s Unit=%s\n"), wszName, val, wszName, wszUnitOfMeasureDescr[unit]);
        OutToPerfFile (wszName, unit, wszDescr);
    }
}


//=============================================================================
// ALL THE PERF LOG CODE IS COMPILED ONLY IF THE ENABLE_PERF_LOG WAS DEFINED.
#endif // ENABLE_PERF_LOG
//=============================================================================

