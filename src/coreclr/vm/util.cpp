// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// ===========================================================================
// File: UTIL.CPP
//

// ===========================================================================


#include "common.h"
#include "excep.h"
#include "corhost.h"
#include "eventtrace.h"
#include "posterror.h"
#include "eemessagebox.h"

#include <shlobj.h>

#include "dlwrap.h"

#ifndef DACCESS_COMPILE


thread_local size_t t_ThreadType;

void ClrFlsSetThreadType(TlsThreadTypeFlag flag)
{
    LIMITED_METHOD_CONTRACT;

    t_ThreadType |= flag;

    // The historic location of ThreadType slot kept for compatibility with SOS
    // TODO: Introduce DAC API to make this hack unnecessary
#if defined(_MSC_VER) && defined(HOST_X86)
    // Workaround for https://developercommunity.visualstudio.com/content/problem/949233/tls-relative-fixup-overflow-tls-section-is-too-lar.html
    gCurrentThreadInfo.m_EETlsData = (void**)(((size_t)&t_ThreadType ^ 1) - (4 * TlsIdx_ThreadType + 1));
#else
    gCurrentThreadInfo.m_EETlsData = (void**)&t_ThreadType - TlsIdx_ThreadType;
#endif
}

void ClrFlsClearThreadType(TlsThreadTypeFlag flag)
{
    LIMITED_METHOD_CONTRACT;
    t_ThreadType &= ~flag;
}

thread_local size_t t_CantStopCount;

// Helper function that encapsulates the parsing rules.
//
// Called first with *pdstout == NULL to figure out how many args there are
// and the size of the required destination buffer.
//
// Called again with a nonnull *pdstout to fill in the actual buffer.
//
// Returns the # of arguments.
static UINT ParseCommandLine(LPCWSTR psrc, __inout LPWSTR *pdstout)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        FORBID_FAULT;
    }
    CONTRACTL_END

    UINT    argcount = 1;       // discovery of arg0 is unconditional, below
    LPWSTR  pdst     = *pdstout;
    BOOL    fDoWrite = (pdst != NULL);

    BOOL    fInQuotes;
    int     iSlash;

    /* A quoted program name is handled here. The handling is much
       simpler than for other arguments. Basically, whatever lies
       between the leading double-quote and next one, or a terminal null
       character is simply accepted. Fancier handling is not required
       because the program name must be a legal NTFS/HPFS file name.
       Note that the double-quote characters are not copied, nor do they
       contribute to numchars.

       This "simplification" is necessary for compatibility reasons even
       though it leads to mishandling of certain cases.  For example,
       "c:\tests\"test.exe will result in an arg0 of c:\tests\ and an
       arg1 of test.exe.  In any rational world this is incorrect, but
       we need to preserve compatibility.
    */

    LPCWSTR pStart = psrc;
    BOOL    skipQuote = FALSE;

    if (*psrc == W('\"'))
    {
        // scan from just past the first double-quote through the next
        // double-quote, or up to a null, whichever comes first
        while ((*(++psrc) != W('\"')) && (*psrc != W('\0')))
            continue;

        skipQuote = TRUE;
    }
    else
    {
        /* Not a quoted program name */

        while (!ISWWHITE(*psrc) && *psrc != W('\0'))
            psrc++;
    }

    // We have now identified arg0 as pStart (or pStart+1 if we have a leading
    // quote) through psrc-1 inclusive
    if (skipQuote)
        pStart++;
    while (pStart < psrc)
    {
        if (fDoWrite)
            *pdst = *pStart;

        pStart++;
        pdst++;
    }

    // And terminate it.
    if (fDoWrite)
        *pdst = W('\0');

    pdst++;

    // if we stopped on a double-quote when arg0 is quoted, skip over it
    if (skipQuote && *psrc == W('\"'))
        psrc++;

    while ( *psrc != W('\0'))
    {
LEADINGWHITE:

        // The outofarg state.
        while (ISWWHITE(*psrc))
            psrc++;

        if (*psrc == W('\0'))
            break;
        else
        if (*psrc == W('#'))
        {
            while (*psrc != W('\0') && *psrc != W('\n'))
                psrc++;     // skip to end of line

            goto LEADINGWHITE;
        }

        argcount++;
        fInQuotes = FALSE;

        while ((!ISWWHITE(*psrc) || fInQuotes) && *psrc != W('\0'))
        {
            switch (*psrc)
            {
            case W('\\'):
                iSlash = 0;
                while (*psrc == W('\\'))
                {
                    iSlash++;
                    psrc++;
                }

                if (*psrc == W('\"'))
                {
                    for ( ; iSlash >= 2; iSlash -= 2)
                    {
                        if (fDoWrite)
                            *pdst = W('\\');

                        pdst++;
                    }

                    if (iSlash & 1)
                    {
                        if (fDoWrite)
                            *pdst = *psrc;

                        psrc++;
                        pdst++;
                    }
                    else
                    {
                        fInQuotes = !fInQuotes;
                        psrc++;
                    }
                }
                else
                    for ( ; iSlash > 0; iSlash--)
                    {
                        if (fDoWrite)
                            *pdst = W('\\');

                        pdst++;
                    }

                break;

            case W('\"'):
                fInQuotes = !fInQuotes;
                psrc++;
                break;

            default:
                if (fDoWrite)
                    *pdst = *psrc;

                psrc++;
                pdst++;
            }
        }

        if (fDoWrite)
            *pdst = W('\0');

        pdst++;
    }


    _ASSERTE(*psrc == W('\0'));
    *pdstout = pdst;
    return argcount;
}


//************************************************************************
// CQuickHeap
//
// A fast non-multithread-safe heap for short term use.
// Destroying the heap frees all blocks allocated from the heap.
// Blocks cannot be freed individually.
//
// The heap uses COM+ exceptions to report errors.
//
// The heap does not use any internal synchronization so it is not
// multithreadsafe.
//************************************************************************
CQuickHeap::CQuickHeap()
{
    LIMITED_METHOD_CONTRACT;

    m_pFirstQuickBlock    = NULL;
    m_pFirstBigQuickBlock = NULL;
    m_pNextFree           = NULL;
}

CQuickHeap::~CQuickHeap()
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        FORBID_FAULT;
    }
    CONTRACTL_END

    QuickBlock *pQuickBlock = m_pFirstQuickBlock;
    while (pQuickBlock) {
        QuickBlock *ptmp = pQuickBlock;
        pQuickBlock = pQuickBlock->m_next;
        delete [] (BYTE*)ptmp;
    }

    pQuickBlock = m_pFirstBigQuickBlock;
    while (pQuickBlock) {
        QuickBlock *ptmp = pQuickBlock;
        pQuickBlock = pQuickBlock->m_next;
        delete [] (BYTE*)ptmp;
    }
}

LPVOID CQuickHeap::Alloc(UINT sz)
{
    CONTRACTL
    {
        THROWS;
        GC_NOTRIGGER;
        INJECT_FAULT(COMPlusThrowOM(););
    } CONTRACTL_END;

    sz = (sz+7) & ~7;

    if ( sz > kBlockSize ) {

        QuickBlock *pQuickBigBlock = (QuickBlock*) new BYTE[sz + sizeof(QuickBlock) - 1];
        pQuickBigBlock->m_next = m_pFirstBigQuickBlock;
        m_pFirstBigQuickBlock = pQuickBigBlock;

        return pQuickBigBlock->m_bytes;


    } else {
        if (m_pNextFree == NULL || sz > (UINT)( &(m_pFirstQuickBlock->m_bytes[kBlockSize]) - m_pNextFree )) {
            QuickBlock *pQuickBlock = (QuickBlock*) new BYTE[kBlockSize + sizeof(QuickBlock) - 1];
            pQuickBlock->m_next = m_pFirstQuickBlock;
            m_pFirstQuickBlock = pQuickBlock;
            m_pNextFree = pQuickBlock->m_bytes;
        }
        LPVOID pv = m_pNextFree;
        m_pNextFree += sz;
        return pv;
    }
}

//----------------------------------------------------------------------------
// Output functions that avoid the crt's.
//----------------------------------------------------------------------------

static
void NPrintToHandleA(HANDLE Handle, const char *pszString, size_t BytesToWrite)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        FORBID_FAULT;
    }
    CONTRACTL_END

    if (Handle == INVALID_HANDLE_VALUE || Handle == NULL)
        return;

    BOOL success;
    DWORD   dwBytesWritten;
    const size_t maxWriteFileSize = 32767; // This is somewhat arbitrary limit, but 2**16-1 doesn't work

    while (BytesToWrite > 0) {
        DWORD dwChunkToWrite = (DWORD) min(BytesToWrite, maxWriteFileSize);

        // Try to write to handle.  If this is not a CUI app, then this is probably
        // not going to work unless the dev took special pains to set their own console
        // handle during CreateProcess.  So try it, but don't yell if it doesn't work in
        // that case.  Also, if we redirect stdout to a pipe then the pipe breaks (ie, we
        // write to something like the UNIX head command), don't complain.
        success = WriteFile(Handle, pszString, dwChunkToWrite, &dwBytesWritten, NULL);
        if (!success)
        {
#if defined(_DEBUG)
            // This can happen if stdout is a closed pipe.  This might not help
            // much, but we'll have half a chance of seeing this.
            OutputDebugStringA("CLR: Writing out an unhandled exception to stdout failed!\n");
            OutputDebugStringA(pszString);
#endif //_DEBUG

            break;
        }
        else {
            _ASSERTE(dwBytesWritten == dwChunkToWrite);
        }
        pszString = pszString + dwChunkToWrite;
        BytesToWrite -= dwChunkToWrite;
    }

}

static
void PrintToHandleA(HANDLE Handle, const char *pszString)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        FORBID_FAULT;
    }
    CONTRACTL_END

    size_t len = strlen(pszString);
    NPrintToHandleA(Handle, pszString, len);
}

void PrintToStdOutA(const char *pszString) {
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        FORBID_FAULT;
    }
    CONTRACTL_END

    HANDLE  Handle = GetStdHandle(STD_OUTPUT_HANDLE);
    PrintToHandleA(Handle, pszString);
}


void PrintToStdOutW(const WCHAR *pwzString)
{
    CONTRACTL
    {
        THROWS;
        GC_NOTRIGGER;
        INJECT_FAULT(COMPlusThrowOM(););
    }
    CONTRACTL_END

    MAKE_MULTIBYTE_FROMWIDE_BESTFIT(pStr, pwzString, GetConsoleOutputCP());

    PrintToStdOutA(pStr);
}

void PrintToStdErrA(const char *pszString) {
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        FORBID_FAULT;
    }
    CONTRACTL_END

    HANDLE  Handle = GetStdHandle(STD_ERROR_HANDLE);
    PrintToHandleA(Handle, pszString);
}


void PrintToStdErrW(const WCHAR *pwzString)
{
    CONTRACTL
    {
        THROWS;
        GC_NOTRIGGER;
        INJECT_FAULT(COMPlusThrowOM(););
    }
    CONTRACTL_END

    MAKE_MULTIBYTE_FROMWIDE_BESTFIT(pStr, pwzString, GetConsoleOutputCP());

    PrintToStdErrA(pStr);
}



void NPrintToStdOutA(const char *pszString, size_t nbytes) {
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        FORBID_FAULT;
    }
    CONTRACTL_END

    HANDLE  Handle = GetStdHandle(STD_OUTPUT_HANDLE);
    NPrintToHandleA(Handle, pszString, nbytes);
}


void NPrintToStdOutW(const WCHAR *pwzString, size_t nchars)
{
    CONTRACTL
    {
        THROWS;
        GC_NOTRIGGER;
        INJECT_FAULT(COMPlusThrowOM(););
    }
    CONTRACTL_END

    LPSTR pStr;
    MAKE_MULTIBYTE_FROMWIDEN_BESTFIT(pStr, pwzString, (int)nchars, nbytes, GetConsoleOutputCP());

    NPrintToStdOutA(pStr, nbytes);
}

void NPrintToStdErrA(const char *pszString, size_t nbytes) {
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        FORBID_FAULT;
    }
    CONTRACTL_END

    HANDLE  Handle = GetStdHandle(STD_ERROR_HANDLE);
    NPrintToHandleA(Handle, pszString, nbytes);
}


void NPrintToStdErrW(const WCHAR *pwzString, size_t nchars)
{
    CONTRACTL
    {
        THROWS;
        GC_NOTRIGGER;
        INJECT_FAULT(COMPlusThrowOM(););
    }
    CONTRACTL_END

    LPSTR pStr;

    MAKE_MULTIBYTE_FROMWIDEN_BESTFIT(pStr, pwzString, (int)nchars, nbytes, GetConsoleOutputCP());

    NPrintToStdErrA(pStr, nbytes);
}
//----------------------------------------------------------------------------

//*****************************************************************************
// Compare VarLoc's
//*****************************************************************************

bool operator ==(const ICorDebugInfo::VarLoc &varLoc1,
                 const ICorDebugInfo::VarLoc &varLoc2)
{
    STATIC_CONTRACT_NOTHROW;
    STATIC_CONTRACT_GC_NOTRIGGER;
    STATIC_CONTRACT_FORBID_FAULT;

    if (varLoc1.vlType != varLoc2.vlType)
        return false;

    switch(varLoc1.vlType)
    {
    case ICorDebugInfo::VLT_REG:
    case ICorDebugInfo::VLT_REG_BYREF:
        return varLoc1.vlReg.vlrReg == varLoc2.vlReg.vlrReg;

    case ICorDebugInfo::VLT_STK:
    case ICorDebugInfo::VLT_STK_BYREF:
        return varLoc1.vlStk.vlsBaseReg == varLoc2.vlStk.vlsBaseReg &&
               varLoc1.vlStk.vlsOffset  == varLoc2.vlStk.vlsOffset;

    case ICorDebugInfo::VLT_REG_REG:
        return varLoc1.vlRegReg.vlrrReg1 == varLoc2.vlRegReg.vlrrReg1 &&
               varLoc1.vlRegReg.vlrrReg2 == varLoc2.vlRegReg.vlrrReg2;

    case ICorDebugInfo::VLT_REG_STK:
        return varLoc1.vlRegStk.vlrsReg == varLoc2.vlRegStk.vlrsReg &&
               varLoc1.vlRegStk.vlrsStk.vlrssBaseReg == varLoc2.vlRegStk.vlrsStk.vlrssBaseReg &&
               varLoc1.vlRegStk.vlrsStk.vlrssOffset == varLoc2.vlRegStk.vlrsStk.vlrssOffset;

    case ICorDebugInfo::VLT_STK_REG:
        return varLoc1.vlStkReg.vlsrStk.vlsrsBaseReg == varLoc2.vlStkReg.vlsrStk.vlsrsBaseReg &&
               varLoc1.vlStkReg.vlsrStk.vlsrsOffset == varLoc2.vlStkReg.vlsrStk.vlsrsBaseReg &&
               varLoc1.vlStkReg.vlsrReg == varLoc2.vlStkReg.vlsrReg;

    case ICorDebugInfo::VLT_STK2:
        return varLoc1.vlStk2.vls2BaseReg == varLoc2.vlStk2.vls2BaseReg &&
               varLoc1.vlStk2.vls2Offset == varLoc2.vlStk2.vls2Offset;

    case ICorDebugInfo::VLT_FPSTK:
        return varLoc1.vlFPstk.vlfReg == varLoc2.vlFPstk.vlfReg;

    default:
        _ASSERTE(!"Bad vlType"); return false;
    }
}

#endif // #ifndef DACCESS_COMPILE

//*****************************************************************************
// The following are used to read and write data given NativeVarInfo
// for primitive types. For ValueClasses, FALSE will be returned.
//*****************************************************************************

SIZE_T GetRegOffsInCONTEXT(ICorDebugInfo::RegNum regNum)
{
    STATIC_CONTRACT_NOTHROW;
    STATIC_CONTRACT_GC_NOTRIGGER;
    STATIC_CONTRACT_FORBID_FAULT;

#ifdef TARGET_X86
    switch(regNum)
    {
    case ICorDebugInfo::REGNUM_EAX: return offsetof(T_CONTEXT,Eax);
    case ICorDebugInfo::REGNUM_ECX: return offsetof(T_CONTEXT,Ecx);
    case ICorDebugInfo::REGNUM_EDX: return offsetof(T_CONTEXT,Edx);
    case ICorDebugInfo::REGNUM_EBX: return offsetof(T_CONTEXT,Ebx);
    // TODO: Fix AMBIENT_SP handling.
    // AMBIENT_SP It isn't necessarily the same value as ESP.  We probably shouldn't try
    // and handle REGNUM_AMBIENT_SP here, and instead update our callers (eg.
    // GetNativeVarVal) to handle this case explicitly.  This logic should also be
    // merged with the parallel (but correct in this case) logic in mscordbi.
    case ICorDebugInfo::REGNUM_ESP:
    case ICorDebugInfo::REGNUM_AMBIENT_SP:
                                    return offsetof(T_CONTEXT,Esp);
    case ICorDebugInfo::REGNUM_EBP: return offsetof(T_CONTEXT,Ebp);
    case ICorDebugInfo::REGNUM_ESI: return offsetof(T_CONTEXT,Esi);
    case ICorDebugInfo::REGNUM_EDI: return offsetof(T_CONTEXT,Edi);
    default: _ASSERTE(!"Bad regNum"); return (SIZE_T) -1;
    }
#elif defined(TARGET_AMD64)
    switch(regNum)
    {
    case ICorDebugInfo::REGNUM_RAX: return offsetof(CONTEXT, Rax);
    case ICorDebugInfo::REGNUM_RCX: return offsetof(CONTEXT, Rcx);
    case ICorDebugInfo::REGNUM_RDX: return offsetof(CONTEXT, Rdx);
    case ICorDebugInfo::REGNUM_RBX: return offsetof(CONTEXT, Rbx);
    case ICorDebugInfo::REGNUM_RSP: return offsetof(CONTEXT, Rsp);
    case ICorDebugInfo::REGNUM_RBP: return offsetof(CONTEXT, Rbp);
    case ICorDebugInfo::REGNUM_RSI: return offsetof(CONTEXT, Rsi);
    case ICorDebugInfo::REGNUM_RDI: return offsetof(CONTEXT, Rdi);
    case ICorDebugInfo::REGNUM_R8:  return offsetof(CONTEXT, R8);
    case ICorDebugInfo::REGNUM_R9:  return offsetof(CONTEXT, R9);
    case ICorDebugInfo::REGNUM_R10: return offsetof(CONTEXT, R10);
    case ICorDebugInfo::REGNUM_R11: return offsetof(CONTEXT, R11);
    case ICorDebugInfo::REGNUM_R12: return offsetof(CONTEXT, R12);
    case ICorDebugInfo::REGNUM_R13: return offsetof(CONTEXT, R13);
    case ICorDebugInfo::REGNUM_R14: return offsetof(CONTEXT, R14);
    case ICorDebugInfo::REGNUM_R15: return offsetof(CONTEXT, R15);
    default: _ASSERTE(!"Bad regNum"); return (SIZE_T)(-1);
    }
#elif defined(TARGET_ARM)

    switch(regNum)
    {
    case ICorDebugInfo::REGNUM_R0: return offsetof(T_CONTEXT, R0);
    case ICorDebugInfo::REGNUM_R1: return offsetof(T_CONTEXT, R1);
    case ICorDebugInfo::REGNUM_R2: return offsetof(T_CONTEXT, R2);
    case ICorDebugInfo::REGNUM_R3: return offsetof(T_CONTEXT, R3);
    case ICorDebugInfo::REGNUM_R4: return offsetof(T_CONTEXT, R4);
    case ICorDebugInfo::REGNUM_R5: return offsetof(T_CONTEXT, R5);
    case ICorDebugInfo::REGNUM_R6: return offsetof(T_CONTEXT, R6);
    case ICorDebugInfo::REGNUM_R7: return offsetof(T_CONTEXT, R7);
    case ICorDebugInfo::REGNUM_R8: return offsetof(T_CONTEXT, R8);
    case ICorDebugInfo::REGNUM_R9: return offsetof(T_CONTEXT, R9);
    case ICorDebugInfo::REGNUM_R10: return offsetof(T_CONTEXT, R10);
    case ICorDebugInfo::REGNUM_R11: return offsetof(T_CONTEXT, R11);
    case ICorDebugInfo::REGNUM_R12: return offsetof(T_CONTEXT, R12);
    case ICorDebugInfo::REGNUM_SP: return offsetof(T_CONTEXT, Sp);
    case ICorDebugInfo::REGNUM_PC: return offsetof(T_CONTEXT, Pc);
    case ICorDebugInfo::REGNUM_LR: return offsetof(T_CONTEXT, Lr);
    case ICorDebugInfo::REGNUM_AMBIENT_SP: return offsetof(T_CONTEXT, Sp);
    default: _ASSERTE(!"Bad regNum"); return (SIZE_T)(-1);
    }
#elif defined(TARGET_ARM64)

    switch(regNum)
    {
    case ICorDebugInfo::REGNUM_X0: return offsetof(T_CONTEXT, X0);
    case ICorDebugInfo::REGNUM_X1: return offsetof(T_CONTEXT, X1);
    case ICorDebugInfo::REGNUM_X2: return offsetof(T_CONTEXT, X2);
    case ICorDebugInfo::REGNUM_X3: return offsetof(T_CONTEXT, X3);
    case ICorDebugInfo::REGNUM_X4: return offsetof(T_CONTEXT, X4);
    case ICorDebugInfo::REGNUM_X5: return offsetof(T_CONTEXT, X5);
    case ICorDebugInfo::REGNUM_X6: return offsetof(T_CONTEXT, X6);
    case ICorDebugInfo::REGNUM_X7: return offsetof(T_CONTEXT, X7);
    case ICorDebugInfo::REGNUM_X8: return offsetof(T_CONTEXT, X8);
    case ICorDebugInfo::REGNUM_X9: return offsetof(T_CONTEXT, X9);
    case ICorDebugInfo::REGNUM_X10: return offsetof(T_CONTEXT, X10);
    case ICorDebugInfo::REGNUM_X11: return offsetof(T_CONTEXT, X11);
    case ICorDebugInfo::REGNUM_X12: return offsetof(T_CONTEXT, X12);
    case ICorDebugInfo::REGNUM_X13: return offsetof(T_CONTEXT, X13);
    case ICorDebugInfo::REGNUM_X14: return offsetof(T_CONTEXT, X14);
    case ICorDebugInfo::REGNUM_X15: return offsetof(T_CONTEXT, X15);
    case ICorDebugInfo::REGNUM_X16: return offsetof(T_CONTEXT, X16);
    case ICorDebugInfo::REGNUM_X17: return offsetof(T_CONTEXT, X17);
    case ICorDebugInfo::REGNUM_X18: return offsetof(T_CONTEXT, X18);
    case ICorDebugInfo::REGNUM_X19: return offsetof(T_CONTEXT, X19);
    case ICorDebugInfo::REGNUM_X20: return offsetof(T_CONTEXT, X20);
    case ICorDebugInfo::REGNUM_X21: return offsetof(T_CONTEXT, X21);
    case ICorDebugInfo::REGNUM_X22: return offsetof(T_CONTEXT, X22);
    case ICorDebugInfo::REGNUM_X23: return offsetof(T_CONTEXT, X23);
    case ICorDebugInfo::REGNUM_X24: return offsetof(T_CONTEXT, X24);
    case ICorDebugInfo::REGNUM_X25: return offsetof(T_CONTEXT, X25);
    case ICorDebugInfo::REGNUM_X26: return offsetof(T_CONTEXT, X26);
    case ICorDebugInfo::REGNUM_X27: return offsetof(T_CONTEXT, X27);
    case ICorDebugInfo::REGNUM_X28: return offsetof(T_CONTEXT, X28);
    case ICorDebugInfo::REGNUM_FP: return offsetof(T_CONTEXT, Fp);
    case ICorDebugInfo::REGNUM_LR: return offsetof(T_CONTEXT, Lr);
    case ICorDebugInfo::REGNUM_SP: return offsetof(T_CONTEXT, Sp);
    case ICorDebugInfo::REGNUM_PC: return offsetof(T_CONTEXT, Pc);
    case ICorDebugInfo::REGNUM_AMBIENT_SP: return offsetof(T_CONTEXT, Sp);
    default: _ASSERTE(!"Bad regNum"); return (SIZE_T)(-1);
    }
#else
    PORTABILITY_ASSERT("GetRegOffsInCONTEXT is not implemented on this platform.");
    return (SIZE_T) -1;
#endif  // TARGET_X86
}

SIZE_T DereferenceByRefVar(SIZE_T addr)
{
    STATIC_CONTRACT_WRAPPER;

    SIZE_T result = NULL;

#if defined(DACCESS_COMPILE)
    HRESULT hr = DacReadAll(addr, &result, sizeof(result), false);
    if (FAILED(hr))
    {
        result = NULL;
    }

#else  // !DACCESS_COMPILE
    EX_TRY
    {
        AVInRuntimeImplOkayHolder AVOkay;

        result = *(SIZE_T*)addr;
    }
    EX_CATCH
    {
    }
    EX_END_CATCH(SwallowAllExceptions);

#endif // !DACCESS_COMPILE

    return result;
}

// How are errors communicated to the caller?
ULONG NativeVarLocations(const ICorDebugInfo::VarLoc &   varLoc,
                         PT_CONTEXT                      pCtx,
                         ULONG                           numLocs,
                         NativeVarLocation*              locs)
{
    STATIC_CONTRACT_NOTHROW;
    STATIC_CONTRACT_GC_NOTRIGGER;
    STATIC_CONTRACT_FORBID_FAULT;

    _ASSERTE(numLocs >= MAX_NATIVE_VAR_LOCS);

    bool fByRef = false;
    switch(varLoc.vlType)
    {
        SIZE_T regOffs;
        TADDR  baseReg;

    case ICorDebugInfo::VLT_REG_BYREF:
        fByRef = true;                  // fall through
        FALLTHROUGH;
    case ICorDebugInfo::VLT_REG:
        regOffs = GetRegOffsInCONTEXT(varLoc.vlReg.vlrReg);
        locs->addr = (ULONG64)(ULONG_PTR)pCtx + regOffs;
        if (fByRef)
        {
            locs->addr = (ULONG64)DereferenceByRefVar((SIZE_T)locs->addr);
        }
        locs->size = sizeof(SIZE_T);
        {
            locs->contextReg = true;
        }
        return 1;

    case ICorDebugInfo::VLT_STK_BYREF:
        fByRef = true;                      // fall through
        FALLTHROUGH;
    case ICorDebugInfo::VLT_STK:
        regOffs = GetRegOffsInCONTEXT(varLoc.vlStk.vlsBaseReg);
        baseReg = *(TADDR *)(regOffs + (BYTE*)pCtx);
        locs->addr = baseReg + varLoc.vlStk.vlsOffset;
        if (fByRef)
        {
            locs->addr = (ULONG64)DereferenceByRefVar((SIZE_T)locs->addr);
        }
        locs->size = sizeof(SIZE_T);
        locs->contextReg = false;
        return 1;

    case ICorDebugInfo::VLT_REG_REG:
        regOffs = GetRegOffsInCONTEXT(varLoc.vlRegReg.vlrrReg1);
        locs->addr = (ULONG64)(ULONG_PTR)pCtx + regOffs;
        locs->size = sizeof(SIZE_T);
        locs->contextReg = true;
        locs++;

        regOffs = GetRegOffsInCONTEXT(varLoc.vlRegReg.vlrrReg2);
        locs->addr = (ULONG64)(ULONG_PTR)pCtx + regOffs;
        locs->size = sizeof(SIZE_T);
        locs->contextReg = true;
        return 2;

    case ICorDebugInfo::VLT_REG_STK:
        regOffs = GetRegOffsInCONTEXT(varLoc.vlRegStk.vlrsReg);
        locs->addr = (ULONG64)(ULONG_PTR)pCtx + regOffs;
        locs->size = sizeof(SIZE_T);
        locs->contextReg = true;
        locs++;

        regOffs = GetRegOffsInCONTEXT(varLoc.vlRegStk.vlrsStk.vlrssBaseReg);
        baseReg = *(TADDR *)(regOffs + (BYTE*)pCtx);
        locs->addr = baseReg + varLoc.vlRegStk.vlrsStk.vlrssOffset;
        locs->size = sizeof(SIZE_T);
        locs->contextReg = false;
        return 2;

    case ICorDebugInfo::VLT_STK_REG:
        regOffs = GetRegOffsInCONTEXT(varLoc.vlStkReg.vlsrStk.vlsrsBaseReg);
        baseReg = *(TADDR *)(regOffs + (BYTE*)pCtx);
        locs->addr = baseReg + varLoc.vlStkReg.vlsrStk.vlsrsOffset;
        locs->size = sizeof(SIZE_T);
        locs->contextReg = false;
        locs++;

        regOffs = GetRegOffsInCONTEXT(varLoc.vlStkReg.vlsrReg);
        locs->addr = (ULONG64)(ULONG_PTR)pCtx + regOffs;
        locs->size = sizeof(SIZE_T);
        locs->contextReg = true;
        return 2;

    case ICorDebugInfo::VLT_STK2:
        regOffs = GetRegOffsInCONTEXT(varLoc.vlStk2.vls2BaseReg);
        baseReg = *(TADDR *)(regOffs + (BYTE*)pCtx);
        locs->addr = baseReg + varLoc.vlStk2.vls2Offset;
        locs->size = 2 * sizeof(SIZE_T);
        locs->contextReg = false;
        return 1;

    case ICorDebugInfo::VLT_FPSTK:
         _ASSERTE(!"NYI");
         return 0;

    default:
         _ASSERTE(!"Bad locType");
         return 0;
    }
}


#ifndef DACCESS_COMPILE

// Returns the location at which the variable
// begins.  Returns NULL for register vars.  For reg-stack
// split, it'll return the addr of the stack part.
// This also works for VLT_REG (a single register).
SIZE_T *NativeVarStackAddr(const ICorDebugInfo::VarLoc &   varLoc,
                           PCONTEXT                        pCtx)
{
    STATIC_CONTRACT_NOTHROW;
    STATIC_CONTRACT_GC_NOTRIGGER;
    STATIC_CONTRACT_FORBID_FAULT;

    SIZE_T *dwAddr = NULL;

    bool fByRef = false;
    switch(varLoc.vlType)
    {
        SIZE_T          regOffs;
        const BYTE *    baseReg;

    case ICorDebugInfo::VLT_REG_BYREF:
        fByRef = true;                      // fall through
        FALLTHROUGH;
    case ICorDebugInfo::VLT_REG:
        regOffs = GetRegOffsInCONTEXT(varLoc.vlReg.vlrReg);
        dwAddr = (SIZE_T *)(regOffs + (BYTE*)pCtx);
        if (fByRef)
        {
            dwAddr = (SIZE_T*)(*dwAddr);
        }
        LOG((LF_CORDB, LL_INFO100, "NVSA: VLT_REG @ 0x%x (by ref = %d)\n", dwAddr, fByRef));
        break;

    case ICorDebugInfo::VLT_STK_BYREF:
        fByRef = true;                      // fall through
        FALLTHROUGH;
    case ICorDebugInfo::VLT_STK:
        regOffs = GetRegOffsInCONTEXT(varLoc.vlStk.vlsBaseReg);
        baseReg = (const BYTE *)*(SIZE_T *)(regOffs + (BYTE*)pCtx);
        dwAddr  = (SIZE_T *)(baseReg + varLoc.vlStk.vlsOffset);
        if (fByRef)
        {
            dwAddr = (SIZE_T*)(*dwAddr);
        }
        LOG((LF_CORDB, LL_INFO100, "NVSA: VLT_STK @ 0x%x (by ref = %d)\n", dwAddr, fByRef));
        break;

    case ICorDebugInfo::VLT_STK2:
        // <TODO>@TODO : VLT_STK2 is overloaded to also mean VLT_STK_n.
        // return FALSE if n > 2;</TODO>

        regOffs = GetRegOffsInCONTEXT(varLoc.vlStk2.vls2BaseReg);
        baseReg = (const BYTE *)*(SIZE_T *)(regOffs + (BYTE*)pCtx);
        dwAddr = (SIZE_T *)(baseReg + varLoc.vlStk2.vls2Offset);
        LOG((LF_CORDB, LL_INFO100, "NVSA: VLT_STK_2 @ 0x%x\n",dwAddr));
        break;

    case ICorDebugInfo::VLT_REG_STK:
        regOffs = GetRegOffsInCONTEXT(varLoc.vlRegStk.vlrsStk.vlrssBaseReg);
        baseReg = (const BYTE *)*(SIZE_T *)(regOffs + (BYTE*)pCtx);
        dwAddr = (SIZE_T *)(baseReg + varLoc.vlRegStk.vlrsStk.vlrssOffset);
        LOG((LF_CORDB, LL_INFO100, "NVSA: REG_STK @ 0x%x\n",dwAddr));
        break;

    case ICorDebugInfo::VLT_STK_REG:
        regOffs = GetRegOffsInCONTEXT(varLoc.vlStkReg.vlsrStk.vlsrsBaseReg);
        baseReg = (const BYTE *)*(SIZE_T *)(regOffs + (BYTE*)pCtx);
        dwAddr = (SIZE_T *)(baseReg + varLoc.vlStkReg.vlsrStk.vlsrsOffset);
        LOG((LF_CORDB, LL_INFO100, "NVSA: STK_REG @ 0x%x\n",dwAddr));
        break;

    case ICorDebugInfo::VLT_REG_REG:
    case ICorDebugInfo::VLT_FPSTK:
         _ASSERTE(!"NYI"); break;

    default:
         _ASSERTE(!"Bad locType"); break;
    }

    return dwAddr;

}


#if defined(HOST_64BIT)
void GetNativeVarValHelper(SIZE_T* dstAddrLow, SIZE_T* dstAddrHigh, SIZE_T* srcAddr, SIZE_T size)
{
    if (size == 1)
        *(BYTE*)dstAddrLow   = *(BYTE*)srcAddr;
    else if (size == 2)
        *(USHORT*)dstAddrLow = *(USHORT*)srcAddr;
    else if (size == 4)
        *(ULONG*)dstAddrLow  = *(ULONG*)srcAddr;
    else if (size == 8)
        *dstAddrLow          = *srcAddr;
    else if (size == 16)
    {
        *dstAddrLow  = *srcAddr;
        *dstAddrHigh = *(srcAddr+1);
    }
    else
    {
        _ASSERTE(!"util.cpp - unreachable code.\n");
        UNREACHABLE();
    }
}
#endif // HOST_64BIT


bool    GetNativeVarVal(const ICorDebugInfo::VarLoc &   varLoc,
                        PCONTEXT                        pCtx,
                        SIZE_T                      *   pVal1,
                        SIZE_T                      *   pVal2
                        BIT64_ARG(SIZE_T                cbSize))
{

    STATIC_CONTRACT_NOTHROW;
    STATIC_CONTRACT_GC_NOTRIGGER;
    STATIC_CONTRACT_FORBID_FAULT;

    switch(varLoc.vlType)
    {
#if !defined(HOST_64BIT)
        SIZE_T          regOffs;

    case ICorDebugInfo::VLT_REG:
        *pVal1  = *NativeVarStackAddr(varLoc,pCtx);
        break;

    case ICorDebugInfo::VLT_STK:
        *pVal1  = *NativeVarStackAddr(varLoc,pCtx);
        break;

    case ICorDebugInfo::VLT_STK2:
        *pVal1  = *NativeVarStackAddr(varLoc,pCtx);
        *pVal2  = *(NativeVarStackAddr(varLoc,pCtx)+ 1);
        break;

    case ICorDebugInfo::VLT_REG_REG:
        regOffs = GetRegOffsInCONTEXT(varLoc.vlRegReg.vlrrReg1);
        *pVal1 = *(SIZE_T *)(regOffs + (BYTE*)pCtx);
        LOG((LF_CORDB, LL_INFO100, "GNVV: STK_REG_REG 1 @ 0x%x\n",
            (SIZE_T *)(regOffs + (BYTE*)pCtx)));

        regOffs = GetRegOffsInCONTEXT(varLoc.vlRegReg.vlrrReg2);
        *pVal2 = *(SIZE_T *)(regOffs + (BYTE*)pCtx);
        LOG((LF_CORDB, LL_INFO100, "GNVV: STK_REG_REG 2 @ 0x%x\n",
            (SIZE_T *)(regOffs + (BYTE*)pCtx)));
        break;

    case ICorDebugInfo::VLT_REG_STK:
        regOffs = GetRegOffsInCONTEXT(varLoc.vlRegStk.vlrsReg);
        *pVal1 = *(SIZE_T *)(regOffs + (BYTE*)pCtx);
        LOG((LF_CORDB, LL_INFO100, "GNVV: STK_REG_STK reg @ 0x%x\n",
            (SIZE_T *)(regOffs + (BYTE*)pCtx)));
        *pVal2 = *NativeVarStackAddr(varLoc,pCtx);
        break;

    case ICorDebugInfo::VLT_STK_REG:
        *pVal1 = *NativeVarStackAddr(varLoc,pCtx);
        regOffs = GetRegOffsInCONTEXT(varLoc.vlStkReg.vlsrReg);
        *pVal2 = *(SIZE_T *)(regOffs + (BYTE*)pCtx);
        LOG((LF_CORDB, LL_INFO100, "GNVV: STK_STK_REG reg @ 0x%x\n",
            (SIZE_T *)(regOffs + (BYTE*)pCtx)));
        break;

    case ICorDebugInfo::VLT_FPSTK:
         _ASSERTE(!"NYI"); break;

#else  // HOST_64BIT
    case ICorDebugInfo::VLT_REG:
    case ICorDebugInfo::VLT_REG_FP:
    case ICorDebugInfo::VLT_STK:
        GetNativeVarValHelper(pVal1, pVal2, NativeVarStackAddr(varLoc, pCtx), cbSize);
        break;

    case ICorDebugInfo::VLT_REG_BYREF:      // fall through
    case ICorDebugInfo::VLT_STK_BYREF:
        _ASSERTE(!"GNVV: This function should not be called for value types");
        break;

#endif // HOST_64BIT

    default:
         _ASSERTE(!"Bad locType"); break;
    }

    return true;
}


#if defined(HOST_64BIT)
void SetNativeVarValHelper(SIZE_T* dstAddr, SIZE_T valueLow, SIZE_T valueHigh, SIZE_T size)
{
    if (size == 1)
        *(BYTE*)dstAddr   = (BYTE)valueLow;
    else if (size == 2)
        *(USHORT*)dstAddr = (USHORT)valueLow;
    else if (size == 4)
        *(ULONG*)dstAddr  = (ULONG)valueLow;
    else if (size == 8)
        *dstAddr          = valueLow;
    else if (size == 16)
    {
        *dstAddr          = valueLow;
        *(dstAddr+1)      = valueHigh;
    }
    else
    {
        _ASSERTE(!"util.cpp - unreachable code.\n");
        UNREACHABLE();
    }
}
#endif // HOST_64BIT


bool    SetNativeVarVal(const ICorDebugInfo::VarLoc &   varLoc,
                        PCONTEXT                        pCtx,
                        SIZE_T                          val1,
                        SIZE_T                          val2
                        BIT64_ARG(SIZE_T                cbSize))
{
    STATIC_CONTRACT_NOTHROW;
    STATIC_CONTRACT_GC_NOTRIGGER;
    STATIC_CONTRACT_FORBID_FAULT;

    switch(varLoc.vlType)
    {
#if !defined(HOST_64BIT)
        SIZE_T          regOffs;

    case ICorDebugInfo::VLT_REG:
        *NativeVarStackAddr(varLoc,pCtx) = val1;
        break;

    case ICorDebugInfo::VLT_STK:
        *NativeVarStackAddr(varLoc,pCtx) = val1;
        break;

    case ICorDebugInfo::VLT_STK2:
        *NativeVarStackAddr(varLoc,pCtx) = val1;
        *(NativeVarStackAddr(varLoc,pCtx)+ 1) = val2;
        break;

    case ICorDebugInfo::VLT_REG_REG:
        regOffs = GetRegOffsInCONTEXT(varLoc.vlRegReg.vlrrReg1);
        *(SIZE_T *)(regOffs + (BYTE*)pCtx) = val1;
        LOG((LF_CORDB, LL_INFO100, "SNVV: STK_REG_REG 1 @ 0x%x\n",
            (SIZE_T *)(regOffs + (BYTE*)pCtx)));

        regOffs = GetRegOffsInCONTEXT(varLoc.vlRegReg.vlrrReg2);
        *(SIZE_T *)(regOffs + (BYTE*)pCtx) = val2;
        LOG((LF_CORDB, LL_INFO100, "SNVV: STK_REG_REG 2 @ 0x%x\n",
            (SIZE_T *)(regOffs + (BYTE*)pCtx)));
        break;

    case ICorDebugInfo::VLT_REG_STK:
        regOffs = GetRegOffsInCONTEXT(varLoc.vlRegStk.vlrsReg);
        *(SIZE_T *)(regOffs + (BYTE*)pCtx) = val1;
        LOG((LF_CORDB, LL_INFO100, "SNVV: STK_REG_STK reg @ 0x%x\n",
            (SIZE_T *)(regOffs + (BYTE*)pCtx)));
        *NativeVarStackAddr(varLoc,pCtx) = val2;
        break;

    case ICorDebugInfo::VLT_STK_REG:
        *NativeVarStackAddr(varLoc,pCtx) = val1;
        regOffs = GetRegOffsInCONTEXT(varLoc.vlStkReg.vlsrReg);
        *(SIZE_T *)(regOffs + (BYTE*)pCtx) = val2;
        LOG((LF_CORDB, LL_INFO100, "SNVV: STK_STK_REG reg @ 0x%x\n",
            (SIZE_T *)(regOffs + (BYTE*)pCtx)));
        break;

    case ICorDebugInfo::VLT_FPSTK:
         _ASSERTE(!"NYI"); break;

#else  // HOST_64BIT
    case ICorDebugInfo::VLT_REG:
    case ICorDebugInfo::VLT_REG_FP:
    case ICorDebugInfo::VLT_STK:
        SetNativeVarValHelper(NativeVarStackAddr(varLoc, pCtx), val1, val2, cbSize);
        break;

    case ICorDebugInfo::VLT_REG_BYREF:      // fall through
    case ICorDebugInfo::VLT_STK_BYREF:
        _ASSERTE(!"GNVV: This function should not be called for value types");
        break;

#endif // HOST_64BIT

    default:
         _ASSERTE(!"Bad locType"); break;
    }

    return true;
}

LPVOID
CLRMapViewOfFile(
    IN HANDLE hFileMappingObject,
    IN DWORD dwDesiredAccess,
    IN DWORD dwFileOffsetHigh,
    IN DWORD dwFileOffsetLow,
    IN SIZE_T dwNumberOfBytesToMap,
    IN LPVOID lpBaseAddress
    )
{
#ifdef _DEBUG
#ifdef TARGET_X86

    char *tmp = new (nothrow) char;
    if (!tmp)
    {
        SetLastError(ERROR_OUTOFMEMORY);
        return NULL;
    }
    delete tmp;

#endif // TARGET_X86
#endif // _DEBUG

    LPVOID pv = MapViewOfFileEx(hFileMappingObject,dwDesiredAccess,dwFileOffsetHigh,dwFileOffsetLow,dwNumberOfBytesToMap,lpBaseAddress);


    if (!pv)
    {
        if(GetLastError()==ERROR_SUCCESS)
            SetLastError(ERROR_OUTOFMEMORY);
        return NULL;
    }

#ifdef _DEBUG
#ifdef TARGET_X86
    if (pv && g_pConfig && g_pConfig->ShouldInjectFault(INJECTFAULT_MAPVIEWOFFILE))
    {
        MEMORY_BASIC_INFORMATION mbi;
        memset(&mbi, 0, sizeof(mbi));
        if (!ClrVirtualQuery(pv, &mbi, sizeof(mbi)))
        {
            if(GetLastError()==ERROR_SUCCESS)
                SetLastError(ERROR_OUTOFMEMORY);
            return NULL;
        }
        UnmapViewOfFile(pv);
        pv = ClrVirtualAlloc(lpBaseAddress, mbi.RegionSize, MEM_RESERVE, PAGE_NOACCESS);
    }
    else
#endif // TARGET_X86
#endif // _DEBUG
    {
    }

    if (!pv && GetLastError()==ERROR_SUCCESS)
        SetLastError(ERROR_OUTOFMEMORY);

    return pv;
}

BOOL
CLRUnmapViewOfFile(
    IN LPVOID lpBaseAddress
    )
{
    STATIC_CONTRACT_ENTRY_POINT;

#ifdef _DEBUG
#ifdef TARGET_X86
    if (g_pConfig && g_pConfig->ShouldInjectFault(INJECTFAULT_MAPVIEWOFFILE))
    {
        return ClrVirtualFree((LPVOID)lpBaseAddress, 0, MEM_RELEASE);
    }
    else
#endif // TARGET_X86
#endif // _DEBUG
    {
        BOOL result = UnmapViewOfFile(lpBaseAddress);
        if (result)
        {
        }
        return result;
    }
}



static HMODULE CLRLoadLibraryWorker(LPCWSTR lpLibFileName, DWORD *pLastError)
{
    // Don't use dynamic contract: will override GetLastError value
    STATIC_CONTRACT_NOTHROW;
    STATIC_CONTRACT_GC_TRIGGERS;
    STATIC_CONTRACT_FAULT;

    HMODULE hMod;
    UINT last = SetErrorMode(SEM_NOOPENFILEERRORBOX|SEM_FAILCRITICALERRORS);
    {
        INDEBUG(PEDecoder::ForceRelocForDLL(lpLibFileName));
        hMod = WszLoadLibrary(lpLibFileName);
        *pLastError = GetLastError();
    }
    SetErrorMode(last);
    return hMod;
}

HMODULE CLRLoadLibrary(LPCWSTR lpLibFileName)
{
    // Don't use dynamic contract: will override GetLastError value
    STATIC_CONTRACT_NOTHROW;
    STATIC_CONTRACT_GC_TRIGGERS;
    STATIC_CONTRACT_FAULT;

    DWORD dwLastError = 0;
    HMODULE hmod = 0;

    hmod = CLRLoadLibraryWorker(lpLibFileName, &dwLastError);

    SetLastError(dwLastError);
    return hmod;
}

#ifndef TARGET_UNIX

static HMODULE CLRLoadLibraryExWorker(LPCWSTR lpLibFileName, HANDLE hFile, DWORD dwFlags, DWORD *pLastError)

{
    // Don't use dynamic contract: will override GetLastError value
    STATIC_CONTRACT_NOTHROW;
    STATIC_CONTRACT_GC_TRIGGERS;
    STATIC_CONTRACT_FAULT;

    HMODULE hMod;
    UINT last = SetErrorMode(SEM_NOOPENFILEERRORBOX|SEM_FAILCRITICALERRORS);
    {
        INDEBUG(PEDecoder::ForceRelocForDLL(lpLibFileName));
        hMod = WszLoadLibraryEx(lpLibFileName, hFile, dwFlags);
        *pLastError = GetLastError();
    }
    SetErrorMode(last);
    return hMod;
}

HMODULE CLRLoadLibraryEx(LPCWSTR lpLibFileName, HANDLE hFile, DWORD dwFlags)
{
    // Don't use dynamic contract: will override GetLastError value

    // This will throw in the case of SO
    //STATIC_CONTRACT_NOTHROW;
    STATIC_CONTRACT_GC_TRIGGERS;
    STATIC_CONTRACT_FAULT;

    DWORD lastError = ERROR_SUCCESS;
    HMODULE hmod = NULL;

    hmod = CLRLoadLibraryExWorker(lpLibFileName, hFile, dwFlags, &lastError);

    SetLastError(lastError);
    return hmod;
}

#endif // !TARGET_UNIX

BOOL CLRFreeLibrary(HMODULE hModule)
{
    // Don't use dynamic contract: will override GetLastError value
    STATIC_CONTRACT_NOTHROW;
    STATIC_CONTRACT_GC_TRIGGERS;
    STATIC_CONTRACT_FORBID_FAULT;

    return FreeLibrary(hModule);
}


#endif // #ifndef DACCESS_COMPILE

GPTR_IMPL(JITNotification, g_pNotificationTable);
GVAL_IMPL(ULONG32, g_dacNotificationFlags);

BOOL IsValidMethodCodeNotification(USHORT Notification)
{
    // If any bit is on other than that given by a valid combination of flags, no good.
    if (Notification & ~(
        CLRDATA_METHNOTIFY_NONE |
        CLRDATA_METHNOTIFY_GENERATED |
        CLRDATA_METHNOTIFY_DISCARDED))
    {
        return FALSE;
    }

    return TRUE;
}

JITNotifications::JITNotifications(JITNotification *jitTable)
{
    LIMITED_METHOD_CONTRACT;
    if (jitTable)
    {
        // Bookkeeping info is held in the first slot
        m_jitTable = jitTable + 1;
    }
    else
    {
        m_jitTable = NULL;
    }
}

BOOL JITNotifications::FindItem(TADDR clrModule, mdToken token, UINT *indexOut)
{
    LIMITED_METHOD_CONTRACT;
    if (m_jitTable == NULL)
    {
        return FALSE;
    }

    if (indexOut == NULL)
    {
        return FALSE;
    }

    UINT Length = GetLength();
    for(UINT i=0; i < Length; i++)
    {
        JITNotification *pCurrent = m_jitTable + i;
        if (!pCurrent->IsFree() &&
            pCurrent->clrModule == clrModule &&
            pCurrent->methodToken == token)
        {
            *indexOut = i;
            return TRUE;
        }
    }

    return FALSE;
}

// if clrModule is NULL, all active notifications are changed to NType
BOOL JITNotifications::SetAllNotifications(TADDR clrModule,USHORT NType,BOOL *changedOut)
{
    if (m_jitTable == NULL)
    {
        return FALSE;
    }

    if (changedOut == NULL)
    {
        return FALSE;
    }

    *changedOut = FALSE;

    UINT Length = GetLength();
    for(UINT i=0; i < Length; i++)
    {
        JITNotification *pCurrent = m_jitTable + i;
        if (!pCurrent->IsFree() &&
            ((clrModule == NULL) || (pCurrent->clrModule == clrModule))&&
            pCurrent->state != NType)
        {
            pCurrent->state = NType;
            *changedOut = TRUE;
        }
    }

    if (*changedOut && NType == CLRDATA_METHNOTIFY_NONE)
    {
        // Need to recompute length if we removed notifications
        for (UINT iCurrent=Length; iCurrent > 0; iCurrent--)
        {
            JITNotification *pCurrent = m_jitTable + (iCurrent - 1);
            if (pCurrent->IsFree())
            {
                DecrementLength();
            }
        }
    }
    return TRUE;
}

BOOL JITNotifications::SetNotification(TADDR clrModule, mdToken token, USHORT NType)
{
    UINT iIndex;

    if (!IsActive())
    {
        return FALSE;
    }

    if (clrModule == NULL)
    {
        return FALSE;
    }

    if (NType == CLRDATA_METHNOTIFY_NONE)
    {
        // Remove an item if it exists
        if (FindItem(clrModule, token, &iIndex))
        {
            JITNotification *pItem = m_jitTable + iIndex;
            pItem->SetFree();
            _ASSERTE(iIndex < GetLength());
            // Update highest?
            if (iIndex == (GetLength()-1))
            {
                DecrementLength();
            }
        }
        return TRUE;
    }

    if (FindItem(clrModule, token, &iIndex))
    {
        JITNotification *pItem = m_jitTable + iIndex;
        _ASSERTE(pItem->IsFree() == FALSE);
        pItem->state =  NType;
        return TRUE;
    }

    // Find first free item
    UINT iFirstFree = GetLength();
    for (UINT i = 0; i < iFirstFree; i++)
    {
        JITNotification *pCurrent = m_jitTable + i;
        if (pCurrent->state == CLRDATA_METHNOTIFY_NONE)
        {
            iFirstFree = i;
            break;
        }
    }

    if (iFirstFree == GetLength() &&
        iFirstFree == GetTableSize())
    {
        // No more room
        return FALSE;
    }

    JITNotification *pCurrent = m_jitTable + iFirstFree;
    pCurrent->SetState(clrModule, token, NType);
    if (iFirstFree == GetLength())
    {
        IncrementLength();
    }

    return TRUE;
}

UINT JITNotifications::GetLength()
{
    LIMITED_METHOD_CONTRACT;
    _ASSERTE(IsActive());

    if (!IsActive())
    {
        return 0;
    }

    return (UINT) (m_jitTable - 1)->methodToken;
}

void JITNotifications::IncrementLength()
{
    _ASSERTE(IsActive());

    if (!IsActive())
    {
        return;
    }

    UINT *pShort = (UINT *) &((m_jitTable - 1)->methodToken);
    (*pShort)++;
}

void JITNotifications::DecrementLength()
{
    _ASSERTE(IsActive());

    if (!IsActive())
    {
        return;
    }

    UINT *pShort = (UINT *) &((m_jitTable - 1)->methodToken);
    (*pShort)--;
}

UINT JITNotifications::GetTableSize()
{
    _ASSERTE(IsActive());

    if (!IsActive())
    {
        return 0;
    }

    return ((UINT) (m_jitTable - 1)->clrModule);
}

USHORT JITNotifications::Requested(TADDR clrModule, mdToken token)
{
    LIMITED_METHOD_CONTRACT;
    UINT iIndex;
    if (FindItem(clrModule, token, &iIndex))
    {
        JITNotification *pItem = m_jitTable + iIndex;
        _ASSERTE(pItem->IsFree() == FALSE);
        return pItem->state;
    }

    return CLRDATA_METHNOTIFY_NONE;
}

#ifdef DACCESS_COMPILE

JITNotification *JITNotifications::InitializeNotificationTable(UINT TableSize)
{
    // We use the first entry in the table for recordkeeping info.

    JITNotification *retTable = new (nothrow) JITNotification[TableSize+1];
    if (retTable)
    {
        // Set the length
        UINT *pUint = (UINT *) &(retTable->methodToken);
        *pUint = 0;
        // Set the table size
        pUint = (UINT *) &(retTable->clrModule);
        *pUint = TableSize;
    }
    return retTable;
}

template <class NotificationClass>
BOOL UpdateOutOfProcTable(__GlobalPtr<NotificationClass*, DPTR(NotificationClass)> pHostTable, NotificationClass* copyFrom, UINT tableSize)
{

    ClrSafeInt<ULONG32> allocSize = S_SIZE_T(sizeof(NotificationClass)) * ClrSafeInt<UINT>(tableSize);
    if (allocSize.IsOverflow())
    {
        return FALSE;
    }

    if (dac_cast<TADDR>(pHostTable) == NULL)
    {
        // The table has not been initialized in the target.  Allocate space for it and update the pointer
        // in the target so that we'll use this allocated memory from now on.  Note that we never free this
        // memory, but this isn't a big deal because it's only a single allocation.
        TADDR Location;

        if (DacAllocVirtual(0, allocSize.Value(),
                                MEM_COMMIT, PAGE_READWRITE, false,
                                &Location) != S_OK)
        {
            return FALSE;
        }

        DPTR(DPTR(NotificationClass)) ppTable = &pHostTable;
        *ppTable = DPTR(NotificationClass)(Location);
        if (DacWriteHostInstance(ppTable,false) != S_OK)
        {
            return FALSE;
        }
    }

    // We store recordkeeping info right before the m_jitTable pointer, that must be written as well.
    if (DacWriteAll(dac_cast<TADDR>(pHostTable), copyFrom,
        allocSize.Value(), false) != S_OK)
    {
        return FALSE;
    }

    return TRUE;
}

BOOL JITNotifications::UpdateOutOfProcTable()
{
    return ::UpdateOutOfProcTable<JITNotification>(g_pNotificationTable, m_jitTable - 1, GetTableSize() + 1);
}
#endif // DACCESS_COMPILE

GPTR_IMPL(GcNotification, g_pGcNotificationTable);

GcNotifications::GcNotifications(GcNotification *gcTable)
{
    LIMITED_METHOD_CONTRACT;
    if (gcTable)
    {
        // Bookkeeping info is held in the first slot
        m_gcTable = gcTable + 1;
    }
    else
    {
        m_gcTable = NULL;
    }
}

BOOL GcNotifications::FindItem(GcEvtArgs ev_, UINT *indexOut)
{
    LIMITED_METHOD_CONTRACT;
    if (m_gcTable == NULL)
    {
        return FALSE;
    }

    if (indexOut == NULL)
    {
        return FALSE;
    }

    UINT length = Length();
    for (UINT i = 0; i < length; i++)
    {
        if (m_gcTable[i].IsMatch(ev_))
        {
            *indexOut = i;
            return TRUE;
        }
    }

    return FALSE;
}


BOOL GcNotifications::SetNotification(GcEvtArgs ev)
{
    if (!IsActive())
    {
        return FALSE;
    }

    if (ev.typ < 0 || ev.typ >= GC_EVENT_TYPE_MAX)
    {
        return FALSE;
    }

    // build the "match" event
    GcEvtArgs evStar = { ev.typ };
    switch (ev.typ)
    {
        case GC_MARK_END:
            // specify mark event matching all generations
            evStar.condemnedGeneration = -1;
            break;
        default:
            break;
    }

    // look for the entry that matches the evStar argument
    UINT idx;
    if (!FindItem(evStar, &idx))
    {
        // Find first free item
        UINT iFirstFree = Length();
        for (UINT i = 0; i < iFirstFree; i++)
        {
            GcNotification *pCurrent = m_gcTable + i;
            if (pCurrent->IsFree())
            {
                iFirstFree = i;
                break;
            }
        }

        if (iFirstFree == Length() &&
            iFirstFree == GetTableSize())
    {
            // No more room
        return FALSE;
    }

        // guarantee the free cell is zeroed out
        m_gcTable[iFirstFree].SetFree();
        idx = iFirstFree;
    }

    // Now update the state
    m_gcTable[idx].ev.typ = ev.typ;
    switch (ev.typ)
    {
        case GC_MARK_END:
            if (ev.condemnedGeneration == 0)
            {
                m_gcTable[idx].SetFree();
            }
            else
            {
                m_gcTable[idx].ev.condemnedGeneration |= ev.condemnedGeneration;
            }
            break;
        default:
            break;
    }

    // and if needed, update the array's length
    if (idx == Length())
    {
        IncrementLength();
    }

    return TRUE;
}

GARY_IMPL(size_t, g_clrNotificationArguments, MAX_CLR_NOTIFICATION_ARGS);

#ifdef DACCESS_COMPILE

GcNotification *GcNotifications::InitializeNotificationTable(UINT TableSize)
{
    // We use the first entry in the table for recordkeeping info.

    GcNotification *retTable = new (nothrow) GcNotification[TableSize+1];
    if (retTable)
    {
        // Set the length
        UINT *pUint = (UINT *) &(retTable[0].ev.typ);
        *pUint = 0;
        // Set the table size
        ++pUint;
        *pUint = TableSize;
    }
    return retTable;
}

BOOL GcNotifications::UpdateOutOfProcTable()
{
    return ::UpdateOutOfProcTable<GcNotification>(g_pGcNotificationTable, m_gcTable - 1, GetTableSize() + 1);
}

#else // DACCESS_COMPILE

static CrstStatic g_clrNotificationCrst;

void DACRaiseException(TADDR *args, UINT argCount)
{
    STATIC_CONTRACT_NOTHROW;
    STATIC_CONTRACT_GC_NOTRIGGER;
    STATIC_CONTRACT_MODE_ANY;

    struct Param
    {
        TADDR *args;
        UINT argCount;
    } param;
    param.args = args;
    param.argCount = argCount;

    PAL_TRY(Param *, pParam, &param)
    {
        RaiseException(CLRDATA_NOTIFY_EXCEPTION, 0, pParam->argCount, (ULONG_PTR *)pParam->args);
    }
    PAL_EXCEPT(EXCEPTION_EXECUTE_HANDLER)
    {
    }
    PAL_ENDTRY
}

void DACNotifyExceptionHelper(TADDR *args, UINT argCount)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_ANY;
    }
    CONTRACTL_END;

    _ASSERTE(argCount <= MAX_CLR_NOTIFICATION_ARGS);

    if (IsDebuggerPresent() && !CORDebuggerAttached())
    {
        CrstHolder lh(&g_clrNotificationCrst);

        for (UINT i = 0; i < argCount; i++)
        {
            g_clrNotificationArguments[i] = args[i];
        }

        DACRaiseException(args, argCount);

        g_clrNotificationArguments[0] = NULL;
    }
}

void InitializeClrNotifications()
{
    g_clrNotificationCrst.Init(CrstClrNotification, CRST_UNSAFE_ANYMODE);
    g_clrNotificationArguments[0] = NULL;
}

// <TODO> FIX IN BETA 2
//
// g_dacNotificationFlags is only modified by the DAC and therefore the
// optmizer can assume that it will always be its default value and has
// been seen to eliminate the code in DoModuleLoadNotification,
// etc... such that DAC notifications are no longer sent.
//
// TODO: fix this in Beta 2
// the RIGHT fix is to make g_dacNotificationFlags volatile, but currently
// we don't have DAC macros to do that. Additionally, there are a number
// of other places we should look at DAC definitions to determine if they
// should be also declared volatile.
//
// for now we just turn off optimization for these guys
#ifdef _MSC_VER
#pragma warning(push)
#pragma warning(disable: 4748)
#pragma optimize("", off)
#endif  // _MSC_VER

#if defined(FEATURE_GDBJIT)
#include "gdbjit.h"
#endif // FEATURE_GDBJIT

// called from the runtime
void DACNotify::DoJITNotification(MethodDesc *MethodDescPtr, TADDR NativeCodeLocation)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_PREEMPTIVE;
    }
    CONTRACTL_END;

    TADDR Args[3] = { JIT_NOTIFICATION2, (TADDR) MethodDescPtr, NativeCodeLocation };
    DACNotifyExceptionHelper(Args, 3);
}

void DACNotify::DoJITPitchingNotification(MethodDesc *MethodDescPtr)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_PREEMPTIVE;
    }
    CONTRACTL_END;

#if defined(FEATURE_GDBJIT) && defined(TARGET_UNIX)
    NotifyGdb::MethodPitched(MethodDescPtr);
#endif
    TADDR Args[2] = { JIT_PITCHING_NOTIFICATION, (TADDR) MethodDescPtr };
    DACNotifyExceptionHelper(Args, 2);
}

void DACNotify::DoModuleLoadNotification(Module *ModulePtr)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_PREEMPTIVE;
    }
    CONTRACTL_END;

    if ((g_dacNotificationFlags & CLRDATA_NOTIFY_ON_MODULE_LOAD) != 0)
    {
        TADDR Args[2] = { MODULE_LOAD_NOTIFICATION, (TADDR) ModulePtr};
        DACNotifyExceptionHelper(Args, 2);
    }
}

void DACNotify::DoModuleUnloadNotification(Module *ModulePtr)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_PREEMPTIVE;
    }
    CONTRACTL_END;

    if ((g_dacNotificationFlags & CLRDATA_NOTIFY_ON_MODULE_UNLOAD) != 0)
    {
        TADDR Args[2] = { MODULE_UNLOAD_NOTIFICATION, (TADDR) ModulePtr};
        DACNotifyExceptionHelper(Args, 2);
    }
}

void DACNotify::DoExceptionNotification(Thread* ThreadPtr)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_PREEMPTIVE;
    }
    CONTRACTL_END;

    if ((g_dacNotificationFlags & CLRDATA_NOTIFY_ON_EXCEPTION) != 0)
    {
        TADDR Args[2] = { EXCEPTION_NOTIFICATION, (TADDR) ThreadPtr};
        DACNotifyExceptionHelper(Args, 2);
    }
}

void DACNotify::DoGCNotification(const GcEvtArgs& args)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_COOPERATIVE;
    }
    CONTRACTL_END;

    if (args.typ == GC_MARK_END)
    {
        TADDR Args[3] = { GC_NOTIFICATION, (TADDR) args.typ, (TADDR) args.condemnedGeneration };
        DACNotifyExceptionHelper(Args, 3);
    }
}

void DACNotify::DoExceptionCatcherEnterNotification(MethodDesc *MethodDescPtr, DWORD nativeOffset)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_COOPERATIVE;
    }
    CONTRACTL_END;

    if ((g_dacNotificationFlags & CLRDATA_NOTIFY_ON_EXCEPTION_CATCH_ENTER) != 0)
    {
        TADDR Args[3] = { CATCH_ENTER_NOTIFICATION, (TADDR) MethodDescPtr, (TADDR)nativeOffset };
        DACNotifyExceptionHelper(Args, 3);
    }
}

#ifdef _MSC_VER
#pragma optimize("", on)
#pragma warning(pop)
#endif  // _MSC_VER
// </TODO>

#endif // DACCESS_COMPILE

// called from the DAC
int DACNotify::GetType(TADDR Args[])
{
    // Type is an enum, and will thus fit into an int.
    return static_cast<int>(Args[0]);
}

BOOL DACNotify::ParseJITNotification(TADDR Args[], TADDR& MethodDescPtr, TADDR& NativeCodeLocation)
{
    _ASSERTE(Args[0] == JIT_NOTIFICATION2);
    if (Args[0] != JIT_NOTIFICATION2)
    {
        return FALSE;
    }

    MethodDescPtr = Args[1];
    NativeCodeLocation = Args[2];

    return TRUE;
}

BOOL DACNotify::ParseJITPitchingNotification(TADDR Args[], TADDR& MethodDescPtr)
{
    _ASSERTE(Args[0] == JIT_PITCHING_NOTIFICATION);
    if (Args[0] != JIT_PITCHING_NOTIFICATION)
    {
        return FALSE;
    }

    MethodDescPtr = Args[1];

    return TRUE;
}

BOOL DACNotify::ParseModuleLoadNotification(TADDR Args[], TADDR& Module)
{
    _ASSERTE(Args[0] == MODULE_LOAD_NOTIFICATION);
    if (Args[0] != MODULE_LOAD_NOTIFICATION)
    {
        return FALSE;
    }

    Module = Args[1];

    return TRUE;
}

BOOL DACNotify::ParseModuleUnloadNotification(TADDR Args[], TADDR& Module)
{
    _ASSERTE(Args[0] == MODULE_UNLOAD_NOTIFICATION);
    if (Args[0] != MODULE_UNLOAD_NOTIFICATION)
    {
        return FALSE;
    }

    Module = Args[1];

    return TRUE;
}

BOOL DACNotify::ParseExceptionNotification(TADDR Args[], TADDR& ThreadPtr)
{
    _ASSERTE(Args[0] == EXCEPTION_NOTIFICATION);
    if (Args[0] != EXCEPTION_NOTIFICATION)
    {
        return FALSE;
    }

    ThreadPtr = Args[1];

    return TRUE;
}


BOOL DACNotify::ParseGCNotification(TADDR Args[], GcEvtArgs& args)
{
    _ASSERTE(Args[0] == GC_NOTIFICATION);
    if (Args[0] != GC_NOTIFICATION)
    {
        return FALSE;
    }

    BOOL bRet = FALSE;

    args.typ = (GcEvt_t) Args[1];
    switch (args.typ)
    {
        case GC_MARK_END:
        {
            // The condemnedGeneration is an int.
            args.condemnedGeneration = static_cast<int>(Args[2]);
            bRet = TRUE;
            break;
        }
        default:
            bRet = FALSE;
            break;
    }

    return bRet;
}

BOOL DACNotify::ParseExceptionCatcherEnterNotification(TADDR Args[], TADDR& MethodDescPtr, DWORD& nativeOffset)
{
    _ASSERTE(Args[0] == CATCH_ENTER_NOTIFICATION);
    if (Args[0] != CATCH_ENTER_NOTIFICATION)
    {
        return FALSE;
    }

    MethodDescPtr = Args[1];
    nativeOffset = (DWORD) Args[2];
    return TRUE;
}

static BOOL TrustMeIAmSafe(void *pLock)
{
    LIMITED_METHOD_CONTRACT;
    return TRUE;
}

LockOwner g_lockTrustMeIAmThreadSafe = { NULL, TrustMeIAmSafe };

static DangerousNonHostedSpinLock g_randomLock;
static CLRRandom g_random;

int GetRandomInt(int maxVal)
{
    // Use the thread-local Random instance if possible
    Thread* pThread = GetThreadNULLOk();
    if (pThread)
        return pThread->GetRandom()->Next(maxVal);

    // No Thread object - need to fall back to the global generator.
    // In DAC builds we don't need the lock (DAC is single-threaded) and can't get it anyway (DNHSL isn't supported)
#ifndef DACCESS_COMPILE
    DangerousNonHostedSpinLockHolder lh(&g_randomLock);
#endif
    if (!g_random.IsInitialized())
        g_random.Init();
    return g_random.Next(maxVal);
}

// These wrap the SString:L:CompareCaseInsenstive function in a way that makes it
// easy to fix code that uses _stricmp. _stricmp should be avoided as it uses the current
// C-runtime locale rather than the invariance culture.
//
// Note that unlike the real _stricmp, these functions unavoidably have a throws/gc_triggers/inject_fault
// contract. So if need a case-insensitive comparison in a place where you can't tolerate this contract,
// you've got a problem.
int __cdecl stricmpUTF8(const char* szStr1, const char* szStr2)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        INJECT_FAULT(COMPlusThrowOM());
    }
    CONTRACTL_END

    SString sStr1 (SString::Utf8, szStr1);
    SString sStr2 (SString::Utf8, szStr2);
    return sStr1.CompareCaseInsensitive(sStr2);

}

#ifndef DACCESS_COMPILE
//
//
// COMCharacter and Helper functions
//
//

#ifndef TARGET_UNIX
/*============================GetCharacterInfoHelper============================
**Determines character type info (digit, whitespace, etc) for the given char.
**Args:   c is the character on which to operate.
**        CharInfoType is one of CT_CTYPE1, CT_CTYPE2, CT_CTYPE3 and specifies the type
**        of information being requested.
**Returns: The bitmask returned by GetStringTypeEx.  The caller needs to know
**         how to interpret this.
**Exceptions: ArgumentException if GetStringTypeEx fails.
==============================================================================*/
INT32 GetCharacterInfoHelper(WCHAR c, INT32 CharInfoType)
{
    WRAPPER_NO_CONTRACT;

    unsigned short result=0;
    if (!GetStringTypeEx(LOCALE_USER_DEFAULT, CharInfoType, &(c), 1, &result)) {
        _ASSERTE(!"This should not happen, verify the arguments passed to GetStringTypeEx()");
    }
    return(INT32)result;
}
#endif // !TARGET_UNIX

/*==============================nativeIsWhiteSpace==============================
**The locally available version of IsWhiteSpace.  Designed to be called by other
**native methods.  The work is mostly done by GetCharacterInfoHelper
**Args:  c -- the character to check.
**Returns: true if c is whitespace, false otherwise.
**Exceptions:  Only those thrown by GetCharacterInfoHelper.
==============================================================================*/
BOOL COMCharacter::nativeIsWhiteSpace(WCHAR c)
{
    WRAPPER_NO_CONTRACT;

#ifndef TARGET_UNIX
    if (c <= (WCHAR) 0x7F) // common case
    {
        BOOL result = (c == ' ') || (c == '\r') || (c == '\n') || (c == '\t') || (c == '\f') || (c == (WCHAR) 0x0B);

        ASSERT(result == ((GetCharacterInfoHelper(c, CT_CTYPE1) & C1_SPACE)!=0));

        return result;
    }

    // GetCharacterInfoHelper costs around 160 instructions
    return((GetCharacterInfoHelper(c, CT_CTYPE1) & C1_SPACE)!=0);
#else // !TARGET_UNIX
    return iswspace(c);
#endif // !TARGET_UNIX
}

/*================================nativeIsDigit=================================
**The locally available version of IsDigit.  Designed to be called by other
**native methods.  The work is mostly done by GetCharacterInfoHelper
**Args:  c -- the character to check.
**Returns: true if c is whitespace, false otherwise.
**Exceptions:  Only those thrown by GetCharacterInfoHelper.
==============================================================================*/
BOOL COMCharacter::nativeIsDigit(WCHAR c)
{
    WRAPPER_NO_CONTRACT;
#ifndef TARGET_UNIX
    return((GetCharacterInfoHelper(c, CT_CTYPE1) & C1_DIGIT)!=0);
#else // !TARGET_UNIX
    return iswdigit(c);
#endif // !TARGET_UNIX
}

BOOL RuntimeFileNotFound(HRESULT hr)
{
    LIMITED_METHOD_CONTRACT;
    return Assembly::FileNotFound(hr);
}

#ifndef TARGET_UNIX
HRESULT GetFileVersion(                     // S_OK or error
    LPCWSTR wszFilePath,                    // Path to the executable.
    ULARGE_INTEGER* pFileVersion)           // Put file version here.
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_ANY;
    }
    CONTRACTL_END;

    //
    // Note that this code is equivalent to FusionGetFileVersionInfo, found in fusion\asmcache\asmcache.cpp
    //

    // Avoid confusion.
    pFileVersion->QuadPart = 0;

    DWORD ret;

    DWORD dwHandle = 0;
    DWORD bufSize = GetFileVersionInfoSizeW(wszFilePath, &dwHandle);
    if (!bufSize)
    {
        return HRESULT_FROM_GetLastErrorNA();
    }

    // Allocate the buffer for the version info structure
    // _alloca() can't return NULL -- raises STATUS_STACK_OVERFLOW.
    BYTE* pVersionInfoBuffer = reinterpret_cast< BYTE* >(_alloca(bufSize));

    ret = GetFileVersionInfoW(wszFilePath, dwHandle, bufSize, pVersionInfoBuffer);
    if (!ret)
    {
        return HRESULT_FROM_GetLastErrorNA();
    }

    // Extract the actual File Version number that we care about.
    UINT versionInfoSize = 0;
    VS_FIXEDFILEINFO* pVSFileInfo;
    ret = VerQueryValueW(pVersionInfoBuffer, W("\\"),
                            reinterpret_cast< void **>(&pVSFileInfo), &versionInfoSize);
    if (!ret || versionInfoSize == 0)
    {
        return HRESULT_FROM_GetLastErrorNA();
    }

    pFileVersion->HighPart = pVSFileInfo->dwFileVersionMS;
    pFileVersion->LowPart = pVSFileInfo->dwFileVersionLS;

    return S_OK;
}
#endif // !TARGET_UNIX

Volatile<double> NormalizedTimer::s_frequency = -1.0;

void FillStubCodePage(BYTE* pageBase, const void* code, int codeSize, int pageSize)
{
    int totalCodeSize = (pageSize / codeSize) * codeSize;

    memcpy(pageBase, code, codeSize);

    int i;
    for (i = codeSize; i < pageSize / 2; i *= 2)
    {
        memcpy(pageBase + i, pageBase, i);
    }

    if (i != totalCodeSize)
    {
        memcpy(pageBase + i, pageBase, totalCodeSize - i);
    }
}

#endif // !DACCESS_COMPILE
