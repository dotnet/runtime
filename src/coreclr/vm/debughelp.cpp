// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.


#include "common.h"

/*******************************************************************/
/* The following routines used to exist in all builds so they could called from the
 * debugger before we had strike.
 * Now most of them are only included in debug builds for diagnostics purposes.
*/
/*******************************************************************/

#include "stdlib.h"

BOOL isMemoryReadable(const TADDR start, unsigned len)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
    }
    CONTRACTL_END;

#if !defined(DACCESS_COMPILE) && defined(TARGET_UNIX)

    return PAL_ProbeMemory((PVOID)start, len, FALSE);

#else // !DACCESS_COMPILE && TARGET_UNIX

    //
    // To accomplish this in a no-throw way, we have to touch each and every page
    // and see if it is in memory or not.
    //

    //
    // Touch the first and last bytes.
    //
    char buff;

#ifdef DACCESS_COMPILE
    if (DacReadAll(start, &buff, 1, false) != S_OK)
    {
        return 0;
    }
#else
    if (ReadProcessMemory(GetCurrentProcess(), (PVOID)start, &buff, 1, 0) == 0)
    {
        return 0;
    }
#endif

    TADDR location;

    location = start + (len - 1);

#ifdef DACCESS_COMPILE
    if (DacReadAll(location, &buff, 1, false) != S_OK)
    {
        return 0;
    }
#else
    if (ReadProcessMemory(GetCurrentProcess(), (PVOID)location,
                          &buff, 1, 0) == 0)
    {
        return 0;
    }
#endif

    //
    // Now we have to loop thru each and every page in between and touch them.
    //
    location = start;
    while (len > GetOsPageSize())
    {
        location += GetOsPageSize();
        len -= GetOsPageSize();

#ifdef DACCESS_COMPILE
        if (DacReadAll(location, &buff, 1, false) != S_OK)
        {
            return 0;
        }
#else
        if (ReadProcessMemory(GetCurrentProcess(), (PVOID)location,
                              &buff, 1, 0) == 0)
        {
            return 0;
        }
#endif
    }

    return 1;
#endif // !DACCESS_COMPILE && TARGET_UNIX
}


/*******************************************************************/
/* check to see if 'retAddr' is a valid return address (it points to
   someplace that has a 'call' right before it), If possible it is
   it returns the address that was called in whereCalled */

bool isRetAddr(TADDR retAddr, TADDR* whereCalled)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
    }
    CONTRACTL_END;

    // don't waste time values clearly out of range
    if (retAddr < (TADDR)BOT_MEMORY || retAddr > (TADDR)TOP_MEMORY)
    {
        return false;
    }

    PTR_BYTE spot = PTR_BYTE(retAddr);
    if (!isMemoryReadable(dac_cast<TADDR>(spot) - 7, 7))
    {
        return(false);
    }

    // Note this is possible to be spoofed, but pretty unlikely
    *whereCalled = 0;
    // call XXXXXXXX
    if (spot[-5] == 0xE8)
    {
        *whereCalled = *(PTR_DWORD(retAddr - 4)) + retAddr;
        return(true);
    }

    // call [XXXXXXXX]
    if (spot[-6] == 0xFF && (spot[-5] == 025))
    {
        if (isMemoryReadable(*(PTR_TADDR(retAddr - 4)), 4))
        {
            *whereCalled = *(PTR_TADDR(*(PTR_TADDR(retAddr - 4))));
            return(true);
        }
    }

    // call [REG+XX]
    if (spot[-3] == 0xFF && (spot[-2] & ~7) == 0120 && (spot[-2] & 7) != 4)
    {
        return(true);
    }

    if (spot[-4] == 0xFF && spot[-3] == 0124)       // call [ESP+XX]
    {
        return(true);
    }

    // call [REG+XXXX]
    if (spot[-6] == 0xFF && (spot[-5] & ~7) == 0220 && (spot[-5] & 7) != 4)
    {
        return(true);
    }

    if (spot[-7] == 0xFF && spot[-6] == 0224)       // call [ESP+XXXX]
    {
        return(true);
    }

    // call [REG]
    if (spot[-2] == 0xFF && (spot[-1] & ~7) == 0020 && (spot[-1] & 7) != 4 && (spot[-1] & 7) != 5)
    {
        return(true);
    }

    // call REG
    if (spot[-2] == 0xFF && (spot[-1] & ~7) == 0320 && (spot[-1] & 7) != 4)
    {
        return(true);
    }

    // There are other cases, but I don't believe they are used.
    return(false);
}

/*
 * The remaining methods are included in debug builds only
 */
#ifdef _DEBUG
#ifndef DACCESS_COMPILE

/*******************************************************************/
/* sends a current stack trace to the debug window */

const char* FormatSig(MethodDesc* pMD, AllocMemTracker *pamTracker);

struct PrintCallbackData {
    BOOL toStdout;
#ifdef _DEBUG
    BOOL toLOG;
#endif
};

static StackWalkAction PrintStackTraceCallback(CrawlFrame* pCF, VOID* pData)
{
    CONTRACTL
    {
        DISABLED(NOTHROW);
        DISABLED(GC_TRIGGERS);
    }
    CONTRACTL_END;

    CONTRACT_VIOLATION(ThrowsViolation);

    MethodDesc* pMD = pCF->GetFunction();
    const int nLen = 2048 - 1;    // keep one character for "\n"
    CHAR *buff = (CHAR*)alloca((nLen + 1) * sizeof(CHAR));
    buff[0] = 0;
    buff[nLen-1] = '\0';                    // make sure the buffer is always NULL-terminated

    PrintCallbackData *pCBD = (PrintCallbackData *)pData;

    if (pMD != 0)
    {
        MethodTable * pMT = pMD->GetMethodTable();

        DefineFullyQualifiedNameForClass();

        LPCUTF8 clsName = GetFullyQualifiedNameForClass(pMT);

        if (clsName != 0)
        {
            if(_snprintf_s(&buff[strlen(buff)], nLen - strlen(buff) - 1, _TRUNCATE, "%s::", clsName) < 0)
            {
                return SWA_CONTINUE;
            }
        }

        // This prematurely suppressrelease'd AmTracker will leak any memory allocated by FormatSig.
        // But this routine is diagnostic aid, not customer-reachable so we won't bother to plug.
        AllocMemTracker dummyAmTracker;

        int buffLen = _snprintf_s(&buff[strlen(buff)],
                      nLen - strlen(buff) - 1,
                      _TRUNCATE,
                      "%s %s  ",
                      pMD->GetName(),
                      FormatSig(pMD, &dummyAmTracker));

        dummyAmTracker.SuppressRelease();
        if (buffLen < 0 )
        {
            return SWA_CONTINUE;
        }

        if (pCF->IsFrameless() && pCF->GetJitManager() != 0) {

            PREGDISPLAY regs = pCF->GetRegisterSet();

            DWORD offset = pCF->GetRelOffset();

            TADDR start = pCF->GetCodeInfo()->GetStartAddress();

            if(_snprintf_s(&buff[strlen(buff)],
                          nLen - strlen(buff) - 1,
                          _TRUNCATE,
                          "JIT ESP:%zX MethStart:%zX EIP:%zX(rel %X)",
                          (size_t)GetRegdisplaySP(regs),
                          (size_t)start,
                          (size_t)GetControlPC(regs),
                          offset) < 0)
            {
                return SWA_CONTINUE;
            }

        }
        else
        {

            if(_snprintf_s(&buff[strlen(buff)], nLen - strlen(buff) - 1, _TRUNCATE, "EE implemented") < 0)
            {
                return SWA_CONTINUE;
            }
        }

    }
    else
    {
        Frame* frame = pCF->GetFrame();

        if(_snprintf_s(&buff[strlen(buff)],
                      nLen - strlen(buff) - 1,
                      _TRUNCATE,
                      "EE Frame is" FMT_ADDR,
                      DBG_ADDR(frame)) < 0)
        {
            return SWA_CONTINUE;
        }
    }

    if (pCBD->toStdout)
    {
        strcat_s(buff, nLen + 1, "\n");
        PrintToStdOutA(buff);
    }
#ifdef _DEBUG
    else if (pCBD->toLOG)
    {
        // For LogSpewAlways to work right the "\n" (newline)
        // must be in the fmt string not part of the args
        LogSpewAlways("    %s\n", buff);
    }
#endif
    else
    {
        strcat_s(buff, nLen + 1, "\n");
        OutputDebugStringUtf8(buff);
    }

    return SWA_CONTINUE;
}

#ifdef LOGGING
void LogStackTrace()
{
    WRAPPER_NO_CONTRACT;

    PrintCallbackData cbd = {0, 1};
    GetThread()->StackWalkFrames(PrintStackTraceCallback, &cbd,ALLOW_ASYNC_STACK_WALK, 0);
}
#endif

#endif // #ifndef DACCESS_COMPILE
#endif //_DEBUG
