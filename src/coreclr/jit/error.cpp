// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

/*XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX
XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX
XX                                                                           XX
XX                           error.cpp                                       XX
XX                                                                           XX
XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX
XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX
*/

#include "jitpch.h"
#ifdef _MSC_VER
#pragma hdrstop
#endif
#include "compiler.h"

#if MEASURE_FATAL
unsigned fatal_badCode;
unsigned fatal_noWay;
unsigned fatal_implLimitation;
unsigned fatal_NOMEM;
unsigned fatal_noWayAssertBody;
#ifdef DEBUG
unsigned fatal_noWayAssertBodyArgs;
#endif // DEBUG
unsigned fatal_NYI;
#endif // MEASURE_FATAL

/*****************************************************************************/
void DECLSPEC_NORETURN fatal(int errCode)
{
#ifdef DEBUG
    if (errCode != CORJIT_SKIPPED) // Don't stop on NYI: use COMPlus_AltJitAssertOnNYI for that.
    {
        if (JitConfig.DebugBreakOnVerificationFailure())
        {
            DebugBreak();
        }
    }
#endif // DEBUG

    ULONG_PTR exceptArg = errCode;
    RaiseException(FATAL_JIT_EXCEPTION, EXCEPTION_NONCONTINUABLE, 1, &exceptArg);
    UNREACHABLE();
}

/*****************************************************************************/
void DECLSPEC_NORETURN badCode()
{
#if MEASURE_FATAL
    fatal_badCode += 1;
#endif // MEASURE_FATAL

    fatal(CORJIT_BADCODE);
}

/*****************************************************************************/
void DECLSPEC_NORETURN noWay()
{
#if MEASURE_FATAL
    fatal_noWay += 1;
#endif // MEASURE_FATAL

    fatal(CORJIT_INTERNALERROR);
}

/*****************************************************************************/
void DECLSPEC_NORETURN implLimitation()
{
#if MEASURE_FATAL
    fatal_implLimitation += 1;
#endif // MEASURE_FATAL

    fatal(CORJIT_IMPLLIMITATION);
}

/*****************************************************************************/
void DECLSPEC_NORETURN NOMEM()
{
#if MEASURE_FATAL
    fatal_NOMEM += 1;
#endif // MEASURE_FATAL

    fatal(CORJIT_OUTOFMEM);
}

/*****************************************************************************/
void DECLSPEC_NORETURN noWayAssertBody()
{
#if MEASURE_FATAL
    fatal_noWayAssertBody += 1;
#endif // MEASURE_FATAL

#ifndef DEBUG
    // Even in retail, if we hit a noway, and we have this variable set, we don't want to fall back
    // to MinOpts, which might hide a regression. Instead, hit a breakpoint (and crash). We don't
    // have the assert code to fall back on here.
    // The debug path goes through this function also, to do the call to 'fatal'.
    // This kind of noway is hit for unreached().
    if (JitConfig.JitEnableNoWayAssert())
    {
        DebugBreak();
    }
#endif // !DEBUG

    fatal(CORJIT_RECOVERABLEERROR);
}

inline static bool ShouldThrowOnNoway(
#ifdef FEATURE_TRACELOGGING
    const char* filename, unsigned line
#endif
    )
{
    return JitTls::GetCompiler() == nullptr ||
           JitTls::GetCompiler()->compShouldThrowOnNoway(
#ifdef FEATURE_TRACELOGGING
               filename, line
#endif
               );
}

/*****************************************************************************/
void noWayAssertBodyConditional(
#ifdef FEATURE_TRACELOGGING
    const char* filename, unsigned line
#endif
    )
{
#ifdef FEATURE_TRACELOGGING
    if (ShouldThrowOnNoway(filename, line))
#else
    if (ShouldThrowOnNoway())
#endif // FEATURE_TRACELOGGING
    {
        noWayAssertBody();
    }
}

/*****************************************************************************/
void notYetImplemented(const char* msg, const char* filename, unsigned line)
{
    Compiler* pCompiler = JitTls::GetCompiler();
    if ((pCompiler == nullptr) || (pCompiler->opts.jitFlags->IsSet(JitFlags::JIT_FLAG_ALT_JIT)))
    {
        NOWAY_MSG_FILE_AND_LINE(msg, filename, line);
        return;
    }
#if FUNC_INFO_LOGGING
#ifdef DEBUG
    LogEnv* env = JitTls::GetLogEnv();
    if (env != nullptr)
    {
        const Compiler* const pCompiler = env->compiler;
        if (pCompiler->verbose)
        {
            printf("\n\n%s - NYI (%s:%d - %s)\n", pCompiler->info.compFullName, filename, line, msg);
        }
    }
    if (Compiler::compJitFuncInfoFile != nullptr)
    {
        fprintf(Compiler::compJitFuncInfoFile, "%s - NYI (%s:%d - %s)\n",
                (env == nullptr) ? "UNKNOWN" : env->compiler->info.compFullName, filename, line, msg);
        fflush(Compiler::compJitFuncInfoFile);
    }
#else  // !DEBUG
    if (Compiler::compJitFuncInfoFile != nullptr)
    {
        fprintf(Compiler::compJitFuncInfoFile, "NYI (%s:%d - %s)\n", filename, line, msg);
        fflush(Compiler::compJitFuncInfoFile);
    }
#endif // !DEBUG
#endif // FUNC_INFO_LOGGING

#ifdef DEBUG
    // Assume we're within a compFunctionTrace boundary, which might not be true.
    pCompiler->compFunctionTraceEnd(nullptr, 0, true);
#endif // DEBUG

    DWORD value = JitConfig.AltJitAssertOnNYI();

    // 0 means just silently skip
    // If we are in retail builds, assume ignore
    // 1 means popup the assert (abort=abort, retry=debugger, ignore=skip)
    // 2 means silently don't skip (same as 3 for retail)
    // 3 means popup the assert (abort=abort, retry=debugger, ignore=don't skip)
    if (value & 1)
    {
#ifdef DEBUG
        assertAbort(msg, filename, line);
#endif
    }

    if ((value & 2) == 0)
    {
#if MEASURE_FATAL
        fatal_NYI += 1;
#endif // MEASURE_FATAL

        fatal(CORJIT_SKIPPED);
    }
}

/*****************************************************************************/
LONG __JITfilter(PEXCEPTION_POINTERS pExceptionPointers, LPVOID lpvParam)
{
    DWORD exceptCode = pExceptionPointers->ExceptionRecord->ExceptionCode;

    if (exceptCode == FATAL_JIT_EXCEPTION)
    {
        ErrorTrapParam* pParam = (ErrorTrapParam*)lpvParam;

        assert(pExceptionPointers->ExceptionRecord->NumberParameters == 1);
        pParam->errc = (int)pExceptionPointers->ExceptionRecord->ExceptionInformation[0];

        ICorJitInfo* jitInfo = pParam->jitInfo;

        if (jitInfo != nullptr)
        {
            jitInfo->reportFatalError((CorJitResult)pParam->errc);
        }

        return EXCEPTION_EXECUTE_HANDLER;
    }

    return EXCEPTION_CONTINUE_SEARCH;
}

/*****************************************************************************/
#ifdef DEBUG

DWORD getBreakOnBadCode()
{
    return JitConfig.JitBreakOnBadCode();
}

/*****************************************************************************/
void debugError(const char* msg, const char* file, unsigned line)
{
    const char* tail = strrchr(file, '\\');
    if (tail != nullptr)
    {
        tail = tail + 1;
    }
    else
    {
        tail = file;
    }

    LogEnv* env = JitTls::GetLogEnv();

    logf(LL_ERROR, "COMPILATION FAILED: file: %s:%d compiling method %s reason %s\n", tail, line,
         env->compiler->info.compFullName, msg);

    // We now only assert when user explicitly set ComPlus_JitRequired=1
    // If ComPlus_JitRequired is 0 or is not set, we will not assert.
    if (JitConfig.JitRequired() == 1 || getBreakOnBadCode())
    {
        assertAbort(msg, file, line);
    }

    BreakIfDebuggerPresent();
}

/*****************************************************************************/
LogEnv::LogEnv(ICorJitInfo* aCompHnd) : compHnd(aCompHnd), compiler(nullptr)
{
}

/*****************************************************************************/
extern "C" void __cdecl assertAbort(const char* why, const char* file, unsigned line)
{
    const char* msg       = why;
    LogEnv*     env       = JitTls::GetLogEnv();
    const int   BUFF_SIZE = 8192;
    char*       buff      = (char*)_alloca(BUFF_SIZE);
    const char* phaseName = "unknown phase";
    if (env->compiler)
    {
        phaseName = PhaseNames[env->compiler->mostRecentlyActivePhase];
        _snprintf_s(buff, BUFF_SIZE, _TRUNCATE,
                    "Assertion failed '%s' in '%s' during '%s' (IL size %d; hash 0x%08x; %s)\n", why,
                    env->compiler->info.compFullName, phaseName, env->compiler->info.compILCodeSize,
                    env->compiler->info.compMethodHash(), env->compiler->compGetTieringName(/* short name */ true));
        msg = buff;
    }
    printf(""); // null string means flush

#if FUNC_INFO_LOGGING
    if (Compiler::compJitFuncInfoFile != nullptr)
    {
        fprintf(Compiler::compJitFuncInfoFile, "%s - Assertion failed (%s:%d - %s) during %s\n",
                (env == nullptr) ? "UNKNOWN" : env->compiler->info.compFullName, file, line, why, phaseName);
    }
#endif // FUNC_INFO_LOGGING

    if (env->compHnd->doAssert(file, line, msg))
    {
        DebugBreak();
    }

    Compiler* comp = JitTls::GetCompiler();

    if (comp != nullptr && comp->opts.jitFlags->IsSet(JitFlags::JIT_FLAG_ALT_JIT))
    {
        // If we hit an assert, and we got here, it's either because the user hit "ignore" on the
        // dialog pop-up, or they set COMPlus_ContinueOnAssert=1 to not emit a pop-up, but just continue.
        // If we're an altjit, we have two options: (1) silently continue, as a normal JIT would, probably
        // leading to additional asserts, or (2) tell the VM that the AltJit wants to skip this function,
        // thus falling back to the fallback JIT. Setting COMPlus_AltJitSkipOnAssert=1 chooses this "skip"
        // to the fallback JIT behavior. This is useful when doing ASM diffs, where we only want to see
        // the first assert for any function, but we don't want to kill the whole ngen process on the
        // first assert (which would happen if you used COMPlus_NoGuiOnAssert=1 for example).
        if (JitConfig.AltJitSkipOnAssert() != 0)
        {
            fatal(CORJIT_SKIPPED);
        }
    }
}

/*********************************************************************/
bool vlogf(unsigned level, const char* fmt, va_list args)
{
    return JitTls::GetLogEnv()->compHnd->logMsg(level, fmt, args);
}

int vflogf(FILE* file, const char* fmt, va_list args)
{
    // 0-length string means flush
    if (fmt[0] == '\0')
    {
        fflush(file);
        return 0;
    }

    const int BUFF_SIZE = 8192;
    char      buffer[BUFF_SIZE];
    int       written = _vsnprintf_s(&buffer[0], BUFF_SIZE, _TRUNCATE, fmt, args);

    if (JitConfig.JitDumpToDebugger())
    {
        OutputDebugStringA(buffer);
    }

    // We use fputs here so that this executes as fast a possible
    fputs(&buffer[0], file);
    return written;
}

int flogf(FILE* file, const char* fmt, ...)
{
    va_list args;
    va_start(args, fmt);
    int written = vflogf(file, fmt, args);
    va_end(args);
    return written;
}

/*********************************************************************/
int logf(const char* fmt, ...)
{
    va_list     args;
    static bool logToEEfailed = false;
    int         written       = 0;
    //
    // We remember when the EE failed to log, because vlogf()
    // is very slow in a checked build.
    //
    // If it fails to log an LL_INFO1000 message once
    // it will always fail when logging an LL_INFO1000 message.
    //
    if (!logToEEfailed)
    {
        va_start(args, fmt);
        if (!vlogf(LL_INFO1000, fmt, args))
        {
            logToEEfailed = true;
        }
        va_end(args);
    }

    if (logToEEfailed)
    {
        // if the EE refuses to log it, we try to send it to stdout
        va_start(args, fmt);
        written = vflogf(jitstdout, fmt, args);
        va_end(args);
    }
#if 0  // Enable this only when you need it
    else
    {
        //
        // The EE just successfully logged our message
        //
        static ConfigDWORD fJitBreakOnDumpToken;
        DWORD breakOnDumpToken = fJitBreakOnDumpToken.val(CLRConfig::INTERNAL_BreakOnDumpToken);
        static DWORD forbidEntry = 0;

        if ((breakOnDumpToken != 0xffffffff) && (forbidEntry == 0))
        {
            forbidEntry = 1;

            // Use value of 0 to get the dump
            static DWORD currentLine = 1;

            if (currentLine == breakOnDumpToken)
            {
                assert(!"Dump token reached");
            }

            printf("(Token=0x%x) ", currentLine++);
            forbidEntry = 0;
        }
    }
#endif // 0
    va_end(args);

    return written;
}

/*********************************************************************/
void gcDump_logf(const char* fmt, ...)
{
    va_list     args;
    static bool logToEEfailed = false;
    //
    // We remember when the EE failed to log, because vlogf()
    // is very slow in a checked build.
    //
    // If it fails to log an LL_INFO1000 message once
    // it will always fail when logging an LL_INFO1000 message.
    //
    if (!logToEEfailed)
    {
        va_start(args, fmt);
        if (!vlogf(LL_INFO1000, fmt, args))
        {
            logToEEfailed = true;
        }
        va_end(args);
    }

    if (logToEEfailed)
    {
        // if the EE refuses to log it, we try to send it to stdout
        va_start(args, fmt);
        vflogf(jitstdout, fmt, args);
        va_end(args);
    }
#if 0  // Enable this only when you need it
    else
    {
        //
        // The EE just successfully logged our message
        //
        static ConfigDWORD fJitBreakOnDumpToken;
        DWORD breakOnDumpToken = fJitBreakOnDumpToken.val(CLRConfig::INTERNAL_BreakOnDumpToken);
        static DWORD forbidEntry = 0;

        if ((breakOnDumpToken != 0xffffffff) && (forbidEntry == 0))
        {
            forbidEntry = 1;

            // Use value of 0 to get the dump
            static DWORD currentLine = 1;

            if (currentLine == breakOnDumpToken)
            {
                assert(!"Dump token reached");
            }

            printf("(Token=0x%x) ", currentLine++);
            forbidEntry = 0;
        }
    }
#endif // 0
    va_end(args);
}

/*********************************************************************/
void logf(unsigned level, const char* fmt, ...)
{
    va_list args;
    va_start(args, fmt);
    vlogf(level, fmt, args);
    va_end(args);
}

void DECLSPEC_NORETURN badCode3(const char* msg, const char* msg2, int arg, _In_z_ const char* file, unsigned line)
{
    const int BUFF_SIZE = 512;
    char      buf1[BUFF_SIZE];
    char      buf2[BUFF_SIZE];
    sprintf_s(buf1, BUFF_SIZE, "%s%s", msg, msg2);
    sprintf_s(buf2, BUFF_SIZE, buf1, arg);

    debugError(buf2, file, line);
    badCode();
}

void noWayAssertAbortHelper(const char* cond, const char* file, unsigned line)
{
    // Show the assert UI.
    if (JitConfig.JitEnableNoWayAssert())
    {
        assertAbort(cond, file, line);
    }
}

void noWayAssertBodyConditional(const char* cond, const char* file, unsigned line)
{
#ifdef FEATURE_TRACELOGGING
    if (ShouldThrowOnNoway(file, line))
#else
    if (ShouldThrowOnNoway())
#endif
    {
        noWayAssertBody(cond, file, line);
    }
    // In CHK we want the assert UI to show up in min-opts.
    else
    {
        noWayAssertAbortHelper(cond, file, line);
    }
}

void DECLSPEC_NORETURN noWayAssertBody(const char* cond, const char* file, unsigned line)
{
#if MEASURE_FATAL
    fatal_noWayAssertBodyArgs += 1;
#endif // MEASURE_FATAL

    noWayAssertAbortHelper(cond, file, line);
    noWayAssertBody();
}

#endif // DEBUG
