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
void *DumpEnvironmentBlock(void)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
    }
    CONTRACTL_END;

    LPTSTR lpszVariable;
    lpszVariable = (LPTSTR)GetEnvironmentStringsW();

    while (*lpszVariable)
    {
        fprintf(stderr, "%c", *lpszVariable++);
    }

    fprintf(stderr, "\n");

    return GetEnvironmentStringsW();
}

#if defined(TARGET_X86) && !defined(TARGET_UNIX)
/*******************************************************************/
// Dump the SEH chain to stderr
void PrintSEHChain(void)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
    }
    CONTRACTL_END;

    EXCEPTION_REGISTRATION_RECORD* pEHR = GetCurrentSEHRecord();

    while (pEHR != NULL && pEHR != EXCEPTION_CHAIN_END)
    {
        fprintf(stderr, "pEHR:0x%x  Handler:0x%x\n", (size_t)pEHR, (size_t)pEHR->Handler);
        pEHR = pEHR->Next;
    }
}
#endif // TARGET_X86

/*******************************************************************/
MethodDesc* IP2MD(ULONG_PTR IP)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
    }
    CONTRACTL_END;

    return ExecutionManager::GetCodeMethodDesc((PCODE)IP);
}

/*******************************************************************/
/* if addr is a valid method table, return a pointer to it */
MethodTable* AsMethodTable(size_t addr)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        DEBUG_ONLY;
    }
    CONTRACTL_END;

    MethodTable* pValidMT = NULL;

    EX_TRY
    {
        MethodTable* pMT = (MethodTable*) addr;

        if (isMemoryReadable((TADDR)pMT, sizeof(MethodTable)))
        {
            EEClass* cls = pMT->GetClass_NoLogging();

            if (isMemoryReadable((TADDR)cls, sizeof(EEClass)) &&
                (cls->GetMethodTable() == pMT))
            {
                pValidMT = pMT;
            }
        }
    }
    EX_CATCH
    {
    }
    EX_END_CATCH(SwallowAllExceptions)

    return(pValidMT);
}

/*******************************************************************/
/* if addr is a valid method table, return a pointer to it */
MethodDesc* AsMethodDesc(size_t addr)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        DEBUG_ONLY;
    }
    CONTRACTL_END;

    if (!IS_ALIGNED(addr, sizeof(void*)))
        return(0);

    MethodDesc* pValidMD = NULL;

    // We try to avoid the most AVs by explicitit calls to isMemoryReadable below, but rare cases can still get through
    // if we are unlucky.
    AVInRuntimeImplOkayHolder AVOkay;

    EX_TRY
    {
        MethodDesc* pMD = (MethodDesc*) addr;

        if (isMemoryReadable((TADDR)pMD, sizeof(MethodDesc)))
        {
            MethodDescChunk *chunk = pMD->GetMethodDescChunk();

            if (isMemoryReadable((TADDR)chunk, sizeof(MethodDescChunk)))
            {
                RelativeFixupPointer<PTR_MethodTable> * ppMT = chunk->GetMethodTablePtr();

                // The MethodTable is stored as a RelativeFixupPointer which does an
                // extra indirection if the address is tagged (the low bit is set).
                // That could AV if we don't check it first.

                if (!ppMT->IsTagged((TADDR)ppMT) || isMemoryReadable((TADDR)ppMT->GetValuePtr(), sizeof(MethodTable*)))
                {
                    if (AsMethodTable((size_t)RelativeFixupPointer<PTR_MethodTable>::GetValueAtPtr((TADDR)ppMT)) != 0)
                    {
                        pValidMD = pMD;
                    }
                }
            }
        }
    }
    EX_CATCH
    {
    }
    EX_END_CATCH(SwallowAllExceptions)


    return(pValidMD);
}


//  This function will return NULL if the buffer is not large enough.
/*******************************************************************/

WCHAR* formatMethodTable(MethodTable* pMT,
                           __out_z __inout_ecount(bufSize) WCHAR* buff,
                           DWORD bufSize)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
    }
    CONTRACTL_END;

    if(bufSize == 0)
    {
        return NULL;
    }

    buff[ bufSize - 1] = W('\0');

    DefineFullyQualifiedNameForClass();

    LPCUTF8 clsName = GetFullyQualifiedNameForClass(pMT);

    if (clsName != 0)
    {
        if(_snwprintf_s(buff, bufSize - 1, _TRUNCATE, W("%S"), clsName) < 0)
        {
            return NULL;
        }

        buff[ bufSize - 1] = W('\0');

    }
    return(buff);
}

/*******************************************************************/
//  This function will return NULL if the buffer is not large enough, otherwise it will
//  return the buffer position for next write.
/*******************************************************************/

WCHAR* formatMethodDesc(MethodDesc* pMD,
                          __out_z __inout_ecount(bufSize) WCHAR* buff,
                          DWORD bufSize)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
    }
    CONTRACTL_END;

    if(bufSize == 0)
    {
        return NULL;
    }

    buff = formatMethodTable(pMD->GetMethodTable(), buff, bufSize);
    if(buff == NULL)
    {
        return NULL;
    }

    buff[bufSize - 1] = W('\0');    // this will guarantee the buffer is also NULL-terminated
    if(_snwprintf_s( &buff[wcslen(buff)] , bufSize - wcslen(buff) - 1, _TRUNCATE, W("::%S"), pMD->GetName()) < 0)
    {
        return NULL;
    }

#ifdef _DEBUG
    if (pMD->m_pszDebugMethodSignature)
    {
        if(_snwprintf_s(&buff[wcslen(buff)],
                      bufSize - wcslen(buff) - 1,
                      _TRUNCATE,
                      W(" %S"),
                      pMD->m_pszDebugMethodSignature) < 0)
        {
            return NULL;
        }

    }
#endif

    if(_snwprintf_s(&buff[wcslen(buff)], bufSize - wcslen(buff) - 1, _TRUNCATE, W("(%zx)"), (size_t)pMD) < 0)
    {
        return NULL;
    }

    return(buff);
}




/*******************************************************************/
/* dump the stack, pretty printing IL methods if possible. This
   routine is very robust.  It will never cause an access violation
   and it always find return addresses if they are on the stack
   (it may find some spurious ones however).  */

int dumpStack(BYTE* topOfStack, unsigned len)
{
    CONTRACTL
    {
        THROWS;
        GC_NOTRIGGER;
    }
    CONTRACTL_END;

    size_t* top = (size_t*) topOfStack;
    size_t* end = (size_t*) &topOfStack[len];

    size_t* ptr = (size_t*) (((size_t) top) & ~3);    // make certain dword aligned.
    TADDR whereCalled;

    WszOutputDebugString(W("***************************************************\n"));

    CQuickBytes qb;

    int nLen = MAX_CLASSNAME_LENGTH * 4 + 400;  // this should be enough

    WCHAR *buff = (WCHAR *) qb.AllocThrows(nLen * sizeof(WCHAR));
    WCHAR *buffEnd = buff + nLen;

    while (ptr < end)
    {
        buff[nLen - 1] = W('\0');

        WCHAR* buffPtr = buff;

        // stop if we hit unmapped pages
        if (!isMemoryReadable((TADDR)ptr, sizeof(TADDR)))
        {
            break;
        }

        if (isRetAddr((TADDR)*ptr, &whereCalled))
        {
            if (_snwprintf_s(buffPtr, buffEnd - buffPtr, _TRUNCATE,  W("STK[%08X] = %08X "), (DWORD)(size_t)ptr, (DWORD)*ptr) < 0)
            {
                return(0);
            }

            buffPtr += wcslen(buffPtr);

            const WCHAR* kind = W("RETADDR ");

            // Is this a stub (is the return address a MethodDesc?
            MethodDesc* ftn = AsMethodDesc(*ptr);

            if (ftn != 0)
            {

                kind = W("     MD PARAM");

                // If another true return address is not directly before it, it is just
                // a methodDesc param.
                TADDR prevRetAddr = ptr[1];

                if (isRetAddr(prevRetAddr, &whereCalled) && AsMethodDesc(prevRetAddr) == 0)
                {
                    kind = W("STUBCALL");
                }
                else
                {
                    // Is it the magic sequence used by CallDescr?
                    if (isMemoryReadable(prevRetAddr - sizeof(short),
                                         sizeof(short)) &&
                        ((short*) prevRetAddr)[-1] == 0x5A59)   // Pop ECX POP EDX
                    {
                        kind = W("STUBCALL");
                    }

                }

            }
            else    // Is it some other code the EE knows about?
            {
                ftn = ExecutionManager::GetCodeMethodDesc((PCODE)(*ptr));
            }

            if (_snwprintf_s(buffPtr, buffEnd - buffPtr, _TRUNCATE, W("%s "), kind) < 0)
            {
                return(0);
            }

            buffPtr += wcslen(buffPtr);

            if (ftn != 0)
            {
                // buffer is not large enough
                if (formatMethodDesc(ftn, buffPtr, static_cast<DWORD>(buffEnd - buffPtr)) == NULL)
                {
                    return(0);
                }

                buffPtr += wcslen(buffPtr);
            }
            else
            {
                wcsncpy_s(buffPtr, buffEnd - buffPtr, W("<UNKNOWN FTN>"), _TRUNCATE);
                buffPtr += wcslen(buffPtr);
            }

            if (whereCalled != 0)
            {
                if (_snwprintf_s(buffPtr, buffEnd - buffPtr, _TRUNCATE, W(" Caller called Entry %zX"), (size_t)whereCalled) < 0)
                {
                    return(0);
                }

                buffPtr += wcslen(buffPtr);
            }

            wcsncpy_s(buffPtr, buffEnd - buffPtr, W("\n"), _TRUNCATE);
            buffPtr += wcslen(buffPtr);
            WszOutputDebugString(buff);
        }

        MethodTable* pMT = AsMethodTable(*ptr);
        if (pMT != 0)
        {
            buffPtr = buff;
            if ( _snwprintf_s(buffPtr, buffEnd - buffPtr, _TRUNCATE, W("STK[%08X] = %08X          MT PARAM "), (DWORD)(size_t)ptr, (DWORD)*ptr ) < 0)
            {
                return(0);
            }

            buffPtr += wcslen(buffPtr);

            if (formatMethodTable(pMT, buffPtr, static_cast<DWORD>(buffEnd - buffPtr)) == NULL)
            {
                return(0);
            }

            buffPtr += wcslen(buffPtr);

            wcsncpy_s(buffPtr, buffEnd - buffPtr, W("\n"), _TRUNCATE);
            WszOutputDebugString(buff);

        }

        ptr++;

    } // while

    return(0);
}

/*******************************************************************/
/* dump the stack from the current ESP.  Stop when we reach a 64K
   boundary */
int DumpCurrentStack()
{
    CONTRACTL
    {
        THROWS;
        GC_NOTRIGGER;
    }
    CONTRACTL_END;

#ifdef TARGET_X86
    BYTE* top = (BYTE *)GetCurrentSP();

        // go back at most 64K, it will stop if we go off the
        // top to unmapped memory
    return(dumpStack(top, 0xFFFF));
#else
    _ASSERTE(!"@NYI - DumpCurrentStack(DebugHelp.cpp)");
    return 0;
#endif // TARGET_X86
}

/*******************************************************************/
WCHAR* StringVal(STRINGREF objref)
{
    CONTRACTL
    {
        THROWS;
        GC_NOTRIGGER;
    }
    CONTRACTL_END;

    return(objref->GetBuffer());
}

LPCUTF8 NameForMethodTable(UINT_PTR pMT)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
    }
    CONTRACTL_END;

    DefineFullyQualifiedNameForClass();
    LPCUTF8 clsName = GetFullyQualifiedNameForClass(((MethodTable*)pMT));
    // Note we're returning local stack space - this should be OK for using in the debugger though
    return clsName;
}

LPCUTF8 ClassNameForObject(UINT_PTR obj)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
    }
    CONTRACTL_END;

    return(NameForMethodTable((UINT_PTR)(((Object*)obj)->GetMethodTable())));
}

LPCUTF8 ClassNameForOBJECTREF(OBJECTREF obj)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
    }
    CONTRACTL_END;

    return(ClassNameForObject((UINT_PTR)(OBJECTREFToObject(obj))));
}

LPCUTF8 NameForMethodDesc(UINT_PTR pMD)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
    }
    CONTRACTL_END;

    return(((MethodDesc*)pMD)->GetName());
}

LPCUTF8 ClassNameForMethodDesc(UINT_PTR pMD)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
    }
    CONTRACTL_END;

    DefineFullyQualifiedNameForClass ();
    return GetFullyQualifiedNameForClass(((MethodDesc*)pMD)->GetMethodTable());
}

PCCOR_SIGNATURE RawSigForMethodDesc(MethodDesc* pMD)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
    }
    CONTRACTL_END;

    return(pMD->GetSig());
}

SyncBlock *GetSyncBlockForObject(UINT_PTR obj)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
    }
    CONTRACTL_END;

    return ((Object*)obj)->GetHeader()->PassiveGetSyncBlock();
}

/*******************************************************************/
void PrintMethodTable(UINT_PTR pMT)
{
    CONTRACTL
    {
        NOTHROW;
        GC_TRIGGERS;
    }
    CONTRACTL_END;

    MethodTable * p = (MethodTable *)pMT;

    DefineFullyQualifiedNameForClass();
    LPCUTF8 name = GetFullyQualifiedNameForClass(p);
    p->DebugDumpVtable(name, true);
    p->DebugDumpFieldLayout(name, true);
    p->DebugDumpGCDesc(name, true);
}

void PrintTableForMethodDesc(UINT_PTR pMD)
{
    CONTRACTL
    {
        NOTHROW;
        GC_TRIGGERS;
    }
    CONTRACTL_END;

    PrintMethodTable((UINT_PTR) ((MethodDesc *)pMD)->GetMethodTable() );
}

void PrintException(OBJECTREF pObjectRef)
{
    CONTRACTL
    {
        THROWS;
        GC_NOTRIGGER;
    }
    CONTRACTL_END;


    if(pObjectRef == NULL)
    {
        return;
    }

    GCPROTECT_BEGIN(pObjectRef);

    MethodDescCallSite toString(METHOD__OBJECT__TO_STRING, &pObjectRef);

    ARG_SLOT arg[1] = {
        ObjToArgSlot(pObjectRef)
    };

    STRINGREF str = toString.Call_RetSTRINGREF(arg);

    if(str->GetBuffer() != NULL)
    {
        WszOutputDebugString(str->GetBuffer());
    }

    GCPROTECT_END();
}

/*******************************************************************/
/* sends a current stack trace to the debug window */

const char* FormatSig(MethodDesc* pMD, AllocMemTracker *pamTracker);

struct PrintCallbackData {
    BOOL toStdout;
#ifdef _DEBUG
    BOOL toLOG;
#endif
};

StackWalkAction PrintStackTraceCallback(CrawlFrame* pCF, VOID* pData)
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
    WCHAR *buff = (WCHAR*)alloca((nLen + 1) * sizeof(WCHAR));
    buff[0] = 0;
    buff[nLen-1] = W('\0');                    // make sure the buffer is always NULL-terminated

    PrintCallbackData *pCBD = (PrintCallbackData *)pData;

    if (pMD != 0)
    {
        MethodTable * pMT = pMD->GetMethodTable();

        DefineFullyQualifiedNameForClass();

        LPCUTF8 clsName = GetFullyQualifiedNameForClass(pMT);

        if (clsName != 0)
        {
            if(_snwprintf_s(&buff[wcslen(buff)], nLen - wcslen(buff) - 1, _TRUNCATE, W("%S::"), clsName) < 0)
            {
                return SWA_CONTINUE;
            }
        }

        // This prematurely suppressrelease'd AmTracker will leak any memory allocated by FormatSig.
        // But this routine is diagnostic aid, not customer-reachable so we won't bother to plug.
        AllocMemTracker dummyAmTracker;

        int buffLen = _snwprintf_s(&buff[wcslen(buff)],
                      nLen - wcslen(buff) - 1,
                      _TRUNCATE,
                      W("%S %S  "),
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

            if(_snwprintf_s(&buff[wcslen(buff)],
                          nLen - wcslen(buff) - 1,
                          _TRUNCATE,
                          W("JIT ESP:%zX MethStart:%zX EIP:%zX(rel %X)"),
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

            if(_snwprintf_s(&buff[wcslen(buff)], nLen - wcslen(buff) - 1, _TRUNCATE, W("EE implemented")) < 0)
            {
                return SWA_CONTINUE;
            }
        }

    }
    else
    {
        Frame* frame = pCF->GetFrame();

        if(_snwprintf_s(&buff[wcslen(buff)],
                      nLen - wcslen(buff) - 1,
                      _TRUNCATE,
                      W("EE Frame is") LFMT_ADDR,
                      DBG_ADDR(frame)) < 0)
        {
            return SWA_CONTINUE;
        }
    }

    if (pCBD->toStdout)
    {
        wcscat_s(buff, nLen + 1, W("\n"));
        PrintToStdOutW(buff);
    }
#ifdef _DEBUG
    else if (pCBD->toLOG)
    {
        MAKE_ANSIPTR_FROMWIDE(sbuff, buff);
        // For LogSpewAlways to work rightr the "\n" (newline)
        // must be in the fmt string not part of the args
        LogSpewAlways("    %s\n", sbuff);
    }
#endif
    else
    {
        wcscat_s(buff, nLen + 1, W("\n"));
        WszOutputDebugString(buff);
    }

    return SWA_CONTINUE;
}

void PrintStackTrace()
{
    CONTRACTL
    {
        DISABLED(NOTHROW);
        DISABLED(GC_TRIGGERS);
    }
    CONTRACTL_END;

    WszOutputDebugString(W("***************************************************\n"));
    PrintCallbackData cbd = {0};
    GetThread()->StackWalkFrames(PrintStackTraceCallback, &cbd, ALLOW_ASYNC_STACK_WALK, 0);
}

void PrintStackTraceToStdout()
{
    CONTRACTL
    {
        DISABLED(NOTHROW);
        DISABLED(GC_TRIGGERS);
    }
    CONTRACTL_END;

    PrintCallbackData cbd = {1};
    GetThread()->StackWalkFrames(PrintStackTraceCallback, &cbd, ALLOW_ASYNC_STACK_WALK, 0);
}

#ifdef _DEBUG
void PrintStackTraceToLog()
{
    CONTRACTL
    {
        DISABLED(NOTHROW);
        DISABLED(GC_TRIGGERS);
    }
    CONTRACTL_END;

    PrintCallbackData cbd = {0, 1};
    GetThread()->StackWalkFrames(PrintStackTraceCallback, &cbd, ALLOW_ASYNC_STACK_WALK, 0);
}
#endif

/*******************************************************************/
// Get the system or current domain from the thread.
BaseDomain* GetSystemDomain()
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
    }
    CONTRACTL_END;

    return SystemDomain::System();
}

AppDomain* GetCurrentDomain()
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
    }
    CONTRACTL_END;

    return SystemDomain::GetCurrentDomain();
}

void PrintDomainName(size_t ob)
{
    CONTRACTL
    {
        THROWS;
        GC_NOTRIGGER;
    }
    CONTRACTL_END;

    AppDomain* dm = (AppDomain*) ob;
    LPCWSTR st = dm->GetFriendlyName(FALSE);

    if(st != NULL)
    {
        WszOutputDebugString(st);
    }
    else
    {
        WszOutputDebugString(W("<Domain with no Name>"));
    }
}

#if defined(TARGET_X86)

#include "gcdump.h"

/*********************************************************************/
void printfToDbgOut(const char* fmt, ...)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
    }
    CONTRACTL_END;

    va_list args;
    va_start(args, fmt);

    char buffer[4096];
    _vsnprintf_s(buffer, COUNTOF(buffer), _TRUNCATE, fmt, args);

    va_end(args);
    OutputDebugStringA( buffer );
}

void DumpGCInfo(MethodDesc* method)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
    }
    CONTRACTL_END;

    PCODE methodStart = method->GetNativeCode();

    if (methodStart == 0)
    {
        return;
    }

    EECodeInfo codeInfo(methodStart);
    _ASSERTE(codeInfo.GetRelOffset() == 0);

    ICodeManager* codeMan = codeInfo.GetCodeManager();
    GCInfoToken gcInfoToken = codeInfo.GetGCInfoToken();

    unsigned methodSize = (unsigned)codeMan->GetFunctionSize(gcInfoToken);

    GCDump gcDump(gcInfoToken.Version);
    PTR_CBYTE gcInfo = PTR_CBYTE(gcInfoToken.Info);

    gcDump.gcPrintf = printfToDbgOut;

    InfoHdr header;

    printfToDbgOut ("Method info block:\n");
    gcInfo += gcDump.DumpInfoHdr(gcInfo, &header, &methodSize, 0);

    printfToDbgOut ("\n");
    printfToDbgOut ("Pointer table:\n");

    gcInfo += gcDump.DumpGCTable(gcInfo, header, methodSize, 0);
}

void DumpGCInfoMD(size_t method)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
    }
    CONTRACTL_END;

    DumpGCInfo((MethodDesc*) method);
}
#endif


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
