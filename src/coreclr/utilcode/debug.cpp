// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//*****************************************************************************
// Debug.cpp
//
// Helper code for debugging.
//*****************************************************************************
//


#include "stdafx.h"
#include "utilcode.h"
#include "ex.h"
#include "corexcep.h"

#ifdef _DEBUG
#define LOGGING
#endif


#include "log.h"

#ifdef HOST_WINDOWS
extern "C" _CRTIMP int __cdecl _flushall(void);
void CreateCrashDumpIfEnabled(bool stackoverflow = false);
#endif

// Global state counter to implement SUPPRESS_ALLOCATION_ASSERTS_IN_THIS_SCOPE.
Volatile<LONG> g_DbgSuppressAllocationAsserts = 0;

static void GetExecutableFileNameUtf8(SString& value)
{
    CONTRACTL
    {
        THROWS;
        GC_NOTRIGGER;
    }
    CONTRACTL_END;

    SString tmp;
    WCHAR * pCharBuf = tmp.OpenUnicodeBuffer(_MAX_PATH);
    DWORD numChars = GetModuleFileNameW(0 /* Get current executable */, pCharBuf, _MAX_PATH);
    tmp.CloseBuffer(numChars);

    tmp.ConvertToUTF8(value);
}

static void DECLSPEC_NORETURN FailFastOnAssert()
{
    WRAPPER_NO_CONTRACT; // If we're calling this, we're well past caring about contract consistency!

    FlushLogging(); // make certain we get the last part of the log
#ifdef HOST_WINDOWS
    _flushall();
#else
    fflush(NULL);
#endif

    ShutdownLogging();
#ifdef HOST_WINDOWS
    CreateCrashDumpIfEnabled();
#endif
    RaiseFailFastException(NULL, NULL, 0);
}

#ifdef _DEBUG

// Continue the app on an assert. Still output the assert, but
// Don't throw up a GUI. This is useful for testing fatal error
// paths (like FEEE) where the runtime asserts.
BOOL ContinueOnAssert()
{
    STATIC_CONTRACT_NOTHROW;
    STATIC_CONTRACT_GC_NOTRIGGER;
    STATIC_CONTRACT_DEBUG_ONLY;

    static ConfigDWORD fNoGui;
    return fNoGui.val(CLRConfig::INTERNAL_ContinueOnAssert);
}

void DoRaiseExceptionOnAssert(DWORD chance)
{
    STATIC_CONTRACT_NOTHROW;
    STATIC_CONTRACT_GC_NOTRIGGER;
    STATIC_CONTRACT_DEBUG_ONLY;
    STATIC_CONTRACT_FORBID_FAULT;
    STATIC_CONTRACT_SUPPORTS_DAC;

#if !defined(DACCESS_COMPILE)
    if (chance)
    {
#ifndef TARGET_UNIX
        PAL_TRY_NAKED
        {
            RaiseException(EXCEPTION_INTERNAL_ASSERT, 0, 0, NULL);
        }
        PAL_EXCEPT_NAKED((chance == 1) ? EXCEPTION_EXECUTE_HANDLER : EXCEPTION_CONTINUE_SEARCH)
        {
        }
        PAL_ENDTRY_NAKED
#else // TARGET_UNIX
        // For PAL always raise the exception.
        RaiseException(EXCEPTION_INTERNAL_ASSERT, 0, 0, NULL);
#endif // TARGET_UNIX
    }
#endif // !DACCESS_COMPILE
}

enum RaiseOnAssertOptions { rTestAndRaise, rTestOnly };

BOOL RaiseExceptionOnAssert(RaiseOnAssertOptions option = rTestAndRaise)
{
    STATIC_CONTRACT_NOTHROW;
    STATIC_CONTRACT_GC_NOTRIGGER;
    STATIC_CONTRACT_DEBUG_ONLY;
    STATIC_CONTRACT_FORBID_FAULT;
    STATIC_CONTRACT_SUPPORTS_DAC;

    // ok for debug-only code to take locks
    CONTRACT_VIOLATION(TakesLockViolation);

    DWORD fRet = 0;

#if !defined(DACCESS_COMPILE)
    static ConfigDWORD fRaiseExceptionOnAssert;
    //
    // we don't want this config key to affect mscordacwks as well!
    //
    EX_TRY
    {
        fRet = fRaiseExceptionOnAssert.val(CLRConfig::INTERNAL_RaiseExceptionOnAssert);
    }
    EX_CATCH
    {
    }
    EX_END_CATCH(SwallowAllExceptions);

    if (option == rTestAndRaise && fRet != 0)
    {
        DoRaiseExceptionOnAssert(fRet);
    }
#endif // !DACCESS_COMPILE

    return fRet != 0;
}

VOID LogAssert(
    LPCSTR      szFile,
    int         iLine,
    LPCSTR      szExpr
)
{
    STATIC_CONTRACT_NOTHROW;
    STATIC_CONTRACT_GC_NOTRIGGER;
    STATIC_CONTRACT_DEBUG_ONLY;

    // Log asserts to the stress log. Note that we can't include the szExpr b/c that
    // may not be a string literal (particularly for formatt-able asserts).
    STRESS_LOG2(LF_ASSERT, LL_ALWAYS, "ASSERT:%s, line:%d\n", szFile, iLine);

    SYSTEMTIME st;
#ifndef TARGET_UNIX
    GetLocalTime(&st);
#else
    GetSystemTime(&st);
#endif

    SString exename;
    GetExecutableFileNameUtf8(exename);

    LOG((LF_ASSERT,
         LL_FATALERROR,
         "FAILED ASSERT(PID %d [0x%08x], Thread: %d [0x%x]) (%lu/%lu/%lu: %02lu:%02lu:%02lu %s): File: %s, Line %d : %s\n",
         GetCurrentProcessId(),
         GetCurrentProcessId(),
         GetCurrentThreadId(),
         GetCurrentThreadId(),
         (ULONG)st.wMonth,
         (ULONG)st.wDay,
         (ULONG)st.wYear,
         1 + (( (ULONG)st.wHour + 11 ) % 12),
         (ULONG)st.wMinute,
         (ULONG)st.wSecond,
         (st.wHour < 12) ? "am" : "pm",
         szFile,
         iLine,
         szExpr));
    LOG((LF_ASSERT, LL_FATALERROR, "RUNNING EXE: %s\n", exename.GetUTF8()));
}

//*****************************************************************************
// This function is called in order to ultimately return an out of memory
// failed hresult.  But this code will check what environment you are running
// in and give an assert for running in a debug build environment.  Usually
// out of memory on a dev machine is a bogus allocation, and this allows you
// to catch such errors.  But when run in a stress environment where you are
// trying to get out of memory, assert behavior stops the tests.
//*****************************************************************************
HRESULT _OutOfMemory(LPCSTR szFile, int iLine)
{
    STATIC_CONTRACT_NOTHROW;
    STATIC_CONTRACT_GC_NOTRIGGER;
    STATIC_CONTRACT_DEBUG_ONLY;

    printf("WARNING: Out of memory condition being issued from: %s, line %d\n", szFile, iLine);
    return (E_OUTOFMEMORY);
}

static const char * szLowMemoryAssertMessage = "Assert failure (unable to format)";

//*****************************************************************************
// This function will handle ignore codes and tell the user what is happening.
//*****************************************************************************
bool _DbgBreakCheck(
    LPCSTR      szFile,
    int         iLine,
    LPCUTF8     szExpr,
    BOOL        fConstrained)
{
    STATIC_CONTRACT_THROWS;
    STATIC_CONTRACT_GC_NOTRIGGER;
    STATIC_CONTRACT_FORBID_FAULT;
    STATIC_CONTRACT_DEBUG_ONLY;

    CONTRACT_VIOLATION(FaultNotFatal | GCViolation | TakesLockViolation);

    char formatBuffer[4096];

    SString modulePath;
    BOOL formattedMessages = FALSE;

    // If we are low on memory we cannot even format a message. If this happens we want to
    // contain the exception here but display as much information as we can about the exception.
    if (!fConstrained)
    {
        EX_TRY
        {
            GetExecutableFileNameUtf8(modulePath);

            sprintf_s(formatBuffer, sizeof(formatBuffer),
                "\nAssert failure(PID %d [0x%08x], Thread: %d [0x%04x]): %s\n"
                "    File: %s:%d\n"
                "    Image: %s\n\n",
                GetCurrentProcessId(), GetCurrentProcessId(),
                GetCurrentThreadId(), GetCurrentThreadId(),
                szExpr, szFile, iLine, modulePath.GetUTF8());

            formattedMessages = TRUE;
        }
        EX_CATCH
        {
        }
        EX_END_CATCH(SwallowAllExceptions);
    }

    // Emit assert in debug output and console for easy access.
    if (formattedMessages)
    {
        OutputDebugStringUtf8(formatBuffer);
        fprintf(stderr, "%s", formatBuffer);
    }
    else
    {
        // Note: we cannot convert to unicode or concatenate in this situation.
        OutputDebugStringUtf8(szLowMemoryAssertMessage);
        OutputDebugStringUtf8("\n");
        OutputDebugStringUtf8(szFile);
        OutputDebugStringUtf8("\n");
        OutputDebugStringUtf8(szExpr);
        OutputDebugStringUtf8("\n");
        printf("%s", szLowMemoryAssertMessage);
        printf("\n");
        printf("%s", szFile);
        printf("\n");
        printf("%s", szExpr);
        printf("\n");
    }

    LogAssert(szFile, iLine, szExpr);

    if (ContinueOnAssert())
    {
        return false;       // don't stop debugger. No gui.
    }

    if (IsDebuggerPresent())
    {
        return true;       // like a retry
    }

    FailFastOnAssert();
    UNREACHABLE();
}

bool _DbgBreakCheckNoThrow(
    LPCSTR      szFile,
    int         iLine,
    LPCSTR      szExpr,
    BOOL        fConstrained)
{
    STATIC_CONTRACT_NOTHROW;
    STATIC_CONTRACT_GC_NOTRIGGER;
    STATIC_CONTRACT_FORBID_FAULT;
    STATIC_CONTRACT_DEBUG_ONLY;

    bool failed = false;
    bool result = false;
    EX_TRY
    {
        result = _DbgBreakCheck(szFile, iLine, szExpr, fConstrained);
    }
    EX_CATCH
    {
        failed = true;
    }
    EX_END_CATCH(SwallowAllExceptions);

    if (failed)
    {
        return true;
    }
    return result;
}

#ifndef TARGET_UNIX
// Get the timestamp from the PE file header.  This is useful
unsigned DbgGetEXETimeStamp()
{
    STATIC_CONTRACT_NOTHROW;
    STATIC_CONTRACT_GC_NOTRIGGER;
    STATIC_CONTRACT_DEBUG_ONLY;

    static ULONG cache = 0;
    if (cache == 0) {
        // Use GetModuleHandleA to avoid contracts - this results in a recursive loop initializing the
        // debug allocator.
        BYTE* imageBase = (BYTE*) GetModuleHandleA(NULL);
        if (imageBase == 0)
            return(0);
        IMAGE_DOS_HEADER *pDOS = (IMAGE_DOS_HEADER*) imageBase;
        if ((pDOS->e_magic != VAL16(IMAGE_DOS_SIGNATURE)) || (pDOS->e_lfanew == 0))
            return(0);

        IMAGE_NT_HEADERS *pNT = (IMAGE_NT_HEADERS*) (VAL32(pDOS->e_lfanew) + imageBase);
        cache = VAL32(pNT->FileHeader.TimeDateStamp);
    }

    return cache;
}
#endif // TARGET_UNIX

VOID DebBreakHr(HRESULT hr)
{
  STATIC_CONTRACT_LEAF;
  static int i = 0;  // add some code here so that we'll be able to set a BP
  _ASSERTE(hr != (HRESULT) 0xcccccccc);
  i++;

  // @CONSIDER: We maybe want this on retail builds.
  #ifdef _DEBUG
  static DWORD dwBreakHR = 99;

  if (dwBreakHR == 99)
         dwBreakHR = CLRConfig::GetConfigValue(CLRConfig::INTERNAL_BreakOnHR);
  if (dwBreakHR == (DWORD)hr)
  {
    _DbgBreak();
  }
  #endif
}

#ifndef TARGET_UNIX
CHAR g_szExprWithStack2[10480];
#endif
void *dbgForceToMemory;     // dummy pointer that pessimises enregistration

int g_BufferLock = -1;

VOID DbgAssertDialog(const char *szFile, int iLine, const char *szExpr)
{
    STATIC_CONTRACT_NOTHROW;
    STATIC_CONTRACT_GC_NOTRIGGER;
    STATIC_CONTRACT_FORBID_FAULT;
    STATIC_CONTRACT_SUPPORTS_DAC_HOST_ONLY;

    DEBUG_ONLY_FUNCTION;

#ifdef DACCESS_COMPILE
    // In the DAC case, asserts can mean one of two things.
    // Either there is a bug in the DAC infrastructure itself (a real assert), or just
    // that the target is corrupt or being accessed at an inconsistent state (a "target
    // consistency failure").  For target consistency failures, we need a mechanism to disable them
    // (without affecting other asserts) so that we can test corrupt / inconsistent targets.

    // @dbgtodo  DAC: For now we're treating all asserts as if they are target consistency checks.
    // In the future we should differentiate the two so that real asserts continue to fire, even when
    // we expect the target to be inconsistent.  See DevDiv Bugs 31674.
    if( !DacTargetConsistencyAssertsEnabled() )
    {
        return;
    }
#endif // #ifndef DACCESS_COMPILE

    // We increment this every time we use the SUPPRESS_ALLOCATION_ASSERTS_IN_THIS_SCOPE
    // macro below.  If it is a big number it means either a lot of threads are asserting
    // or we have a recursion in the Assert logic (usually the latter).  At least with this
    // code in place, we don't get stack overflow (and the process torn down).
    // the correct fix is to avoid calling asserting when allocating memory with an assert.
    if (g_DbgSuppressAllocationAsserts > 16)
        DebugBreak();

    SUPPRESS_ALLOCATION_ASSERTS_IN_THIS_SCOPE;

    // Raising the assert dialog can cause us to re-enter the host when allocating
    // memory for the string.  Since this is debug-only code, we can safely skip
    // violation asserts here, particularly since they can also cause infinite
    // recursion.
    PERMANENT_CONTRACT_VIOLATION(HostViolation, ReasonDebugOnly);

    dbgForceToMemory = &szFile;     //make certain these args are available in the debugger
    dbgForceToMemory = &iLine;
    dbgForceToMemory = &szExpr;

    RaiseExceptionOnAssert(rTestAndRaise);

    BOOL fConstrained = FALSE;

    DWORD dwAssertStacktrace = CLRConfig::GetConfigValue(CLRConfig::INTERNAL_AssertStacktrace);

    LONG lAlreadyOwned = InterlockedExchange((LPLONG)&g_BufferLock, 1);
    if (fConstrained || dwAssertStacktrace == 0 || lAlreadyOwned == 1)
    {
        if (_DbgBreakCheckNoThrow(szFile, iLine, szExpr, fConstrained))
        {
            _DbgBreak();
        }
    }
    else
    {
        char *szExprToDisplay = (char*)szExpr;
#ifdef TARGET_UNIX
        BOOL fGotStackTrace = TRUE;
#else
        BOOL fGotStackTrace = FALSE;
#ifndef DACCESS_COMPILE
        EX_TRY
        {
            FAULT_NOT_FATAL();
            szExprToDisplay = &g_szExprWithStack2[0];
            strcpy(szExprToDisplay, szExpr);
            strcat_s(szExprToDisplay, ARRAY_SIZE(g_szExprWithStack2), "\n\n");
            GetStringFromStackLevels(1, 10, szExprToDisplay + strlen(szExprToDisplay));
            fGotStackTrace = TRUE;
        }
        EX_CATCH
        {
        }
        EX_END_CATCH(SwallowAllExceptions);
#endif  // DACCESS_COMPILE
#endif  // TARGET_UNIX

        if (_DbgBreakCheckNoThrow(szFile, iLine, szExprToDisplay, !fGotStackTrace))
        {
            _DbgBreak();
        }

        g_BufferLock = 0;
    }
} // DbgAssertDialog

#if !defined(DACCESS_COMPILE)
//-----------------------------------------------------------------------------
// Returns an a stacktrace for a given context.
// Very useful inside exception filters.
// Returns true if successful, false on failure (such as OOM).
// This never throws.
//-----------------------------------------------------------------------------
bool GetStackTraceAtContext(SString & s, CONTEXT * pContext)
{
    SUPPRESS_ALLOCATION_ASSERTS_IN_THIS_SCOPE;
    STATIC_CONTRACT_DEBUG_ONLY;

     // NULL means use the current context.
    bool fSuccess = false;

    FAULT_NOT_FATAL();

#ifndef TARGET_UNIX
    EX_TRY
    {
        const int cTotal = cfrMaxAssertStackLevels - 1;
        // If we have a supplied context, then don't skip any frames. Else we'll
        // be using the current context, so skip this frame.
        const int cSkip = (pContext == NULL) ? 1 : 0;
        char * szString = s.OpenUTF8Buffer(cchMaxAssertStackLevelStringLen * cTotal);
        GetStringFromStackLevels(cSkip, cTotal, szString, pContext);
        s.CloseBuffer((COUNT_T) strlen(szString));

        // If we made it this far w/o throwing, we succeeded.
        fSuccess = true;
    }
    EX_CATCH
    {
        // Nothing to do here.
    }
    EX_END_CATCH(SwallowAllExceptions);
#endif // TARGET_UNIX

    return fSuccess;
} // GetStackTraceAtContext
#endif // !defined(DACCESS_COMPILE)
#endif // _DEBUG

void DECLSPEC_NORETURN __FreeBuildAssertFail(const char *szFile, int iLine, const char *szExpr)
{
    WRAPPER_NO_CONTRACT; // If we're calling this, we're well past caring about contract consistency!

    SString modulePath;
    GetExecutableFileNameUtf8(modulePath);

    SString buffer;
    buffer.Printf("CLR: Assert failure(PID %d [0x%08x], Thread: %d [0x%x]): %s\n"
                "    File: %s:%d Image:\n%s\n",
                GetCurrentProcessId(), GetCurrentProcessId(),
                GetCurrentThreadId(), GetCurrentThreadId(),
                szExpr, szFile, iLine, modulePath.GetUTF8());
    OutputDebugStringUtf8(buffer.GetUTF8());

    // Write out the error to the console
    printf("%s", buffer.GetUTF8());

    // Log to the stress log. Note that we can't include the szExpr b/c that
    // may not be a string literal (particularly for formatt-able asserts).
    STRESS_LOG2(LF_ASSERT, LL_ALWAYS, "ASSERT:%s:%d\n", szFile, iLine);

    FailFastOnAssert();
    UNREACHABLE();
}
