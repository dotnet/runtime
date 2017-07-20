// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
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
#include "newapis.h"

#include <shlobj.h>

#include "dlwrap.h"

#ifndef DACCESS_COMPILE

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


// Function to parse apart a command line and return the 
// arguments just like argv and argc
// This function is a little funky because of the pointer work
// but it is neat because it allows the recipient of the char**
// to only have to do a single delete []
LPWSTR* CommandLineToArgvW(__in LPWSTR lpCmdLine, DWORD *pNumArgs)
{

    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        INJECT_FAULT(return NULL;); 
    }
    CONTRACTL_END

    DWORD argcount = 0;
    LPWSTR retval = NULL;
    LPWSTR *pslot;
    // First we need to find out how many strings there are in the command line
    _ASSERTE(lpCmdLine);
    _ASSERTE(pNumArgs);

    LPWSTR pdst = NULL;
    argcount = ParseCommandLine(lpCmdLine, &pdst);

    // This check is because on WinCE the Application Name is not passed in as an argument to the app!
    if (argcount == 0)
    {
        *pNumArgs = 0;
        return NULL;
    }

    // Now we need alloc a buffer the size of the command line + the number of strings * DWORD
    retval = new (nothrow) WCHAR[(argcount*sizeof(WCHAR*))/sizeof(WCHAR) + (pdst - (LPWSTR)NULL)];
    if(!retval)
        return NULL;

    pdst = (LPWSTR)( argcount*sizeof(LPWSTR*) + (BYTE*)retval );
    ParseCommandLine(lpCmdLine, &pdst);
    pdst = (LPWSTR)( argcount*sizeof(LPWSTR*) + (BYTE*)retval );
    pslot = (LPWSTR*)retval;
    for (DWORD i = 0; i < argcount; i++)
    {
        *(pslot++) = pdst;
        while (*pdst != W('\0'))
        {
            pdst++;
        }
        pdst++;
    }

    

    *pNumArgs = argcount;
    return (LPWSTR*)retval;

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
        SO_TOLERANT;    // So long as we cleanup the heap when we're done, all the memory goes with it
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
        // No CharNextExA on CoreSystem, we just assume no multi-byte characters (this code path shouldn't be
        // used in the production codepath for currently supported CoreSystem based products anyway).
#ifndef FEATURE_CORESYSTEM
        if (dwChunkToWrite < BytesToWrite) {
            break;
            // must go by char to find biggest string that will fit, taking DBCS chars into account
            //dwChunkToWrite = 0;
            //const char *charNext = pszString;
            //while (dwChunkToWrite < maxWriteFileSize-2 && charNext) {
            //    charNext = CharNextExA(0, pszString+dwChunkToWrite, 0);
            //    dwChunkToWrite = (DWORD)(charNext - pszString);
            //}
            //if (dwChunkToWrite == 0)
            //    break;
        }
#endif // !FEATURE_CORESYSTEM

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





//+--------------------------------------------------------------------------
//
//  Function:   VMDebugOutputA( . . . . )
//              VMDebugOutputW( . . . . )
//  
//  Synopsis:   Output a message formatted in printf fashion to the debugger.
//              ANSI and wide character versions are both provided.  Only 
//              present in debug builds (i.e. when _DEBUG is defined).
//
//  Arguments:  [format]     ---   ANSI or Wide character format string
//                                 in printf/OutputDebugString-style format.
// 
//              [ ... ]      ---   Variable length argument list compatible
//                                 with the format string.
//
//  Returns:    Nothing.
// 
//  Notes:      Has internal static sized character buffer of 
//              width specified by the preprocessor constant DEBUGOUT_BUFSIZE.
//
//---------------------------------------------------------------------------
#ifdef _DEBUG

#define DEBUGOUT_BUFSIZE 1024

void __cdecl VMDebugOutputA(__in LPSTR format, ...)
{
    STATIC_CONTRACT_NOTHROW;
    STATIC_CONTRACT_GC_NOTRIGGER;
    STATIC_CONTRACT_FORBID_FAULT;

    va_list     argPtr;
    va_start(argPtr, format);

    char szBuffer[DEBUGOUT_BUFSIZE];

    if(vsprintf_s(szBuffer, DEBUGOUT_BUFSIZE-1, format, argPtr) > 0)
        OutputDebugStringA(szBuffer);
    va_end(argPtr);
}

void __cdecl VMDebugOutputW(__in LPWSTR format, ...)
{
    STATIC_CONTRACT_NOTHROW;
    STATIC_CONTRACT_GC_NOTRIGGER;
    STATIC_CONTRACT_FORBID_FAULT;
    STATIC_CONTRACT_DEBUG_ONLY;

    va_list     argPtr;
    va_start(argPtr, format);
    
    WCHAR wszBuffer[DEBUGOUT_BUFSIZE];

    if(vswprintf_s(wszBuffer, DEBUGOUT_BUFSIZE-2, format, argPtr) > 0)
        WszOutputDebugString(wszBuffer);
    va_end(argPtr);
}

#endif   // #ifdef DACCESS_COMPILE

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

#ifdef _TARGET_X86_
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
#elif defined(_TARGET_AMD64_)
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
#elif defined(_TARGET_ARM_)

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
#elif defined(_TARGET_ARM64_)

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
#endif  // _TARGET_X86_
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


BOOL CompareFiles(HANDLE hFile1,HANDLE hFile2)
{

    STATIC_CONTRACT_THROWS;
    STATIC_CONTRACT_GC_NOTRIGGER;
    BY_HANDLE_FILE_INFORMATION fileinfo1;
    BY_HANDLE_FILE_INFORMATION fileinfo2;    
    if (!GetFileInformationByHandle(hFile1,&fileinfo1) ||
        !GetFileInformationByHandle(hFile2,&fileinfo2))
        ThrowLastError();
    return fileinfo1.nFileIndexLow == fileinfo2.nFileIndexLow &&
               fileinfo1.nFileIndexHigh == fileinfo2.nFileIndexHigh &&
               fileinfo1.dwVolumeSerialNumber==fileinfo2.dwVolumeSerialNumber;
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


#if defined(_WIN64)
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
#endif // _WIN64


bool    GetNativeVarVal(const ICorDebugInfo::VarLoc &   varLoc,
                        PCONTEXT                        pCtx,
                        SIZE_T                      *   pVal1,
                        SIZE_T                      *   pVal2
                        WIN64_ARG(SIZE_T                cbSize))
{

    STATIC_CONTRACT_NOTHROW;
    STATIC_CONTRACT_GC_NOTRIGGER;
    STATIC_CONTRACT_FORBID_FAULT;

    switch(varLoc.vlType)
    {
#if !defined(_WIN64)
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

#else  // _WIN64
    case ICorDebugInfo::VLT_REG:
    case ICorDebugInfo::VLT_REG_FP:
    case ICorDebugInfo::VLT_STK:
        GetNativeVarValHelper(pVal1, pVal2, NativeVarStackAddr(varLoc, pCtx), cbSize);
        break;

    case ICorDebugInfo::VLT_REG_BYREF:      // fall through
    case ICorDebugInfo::VLT_STK_BYREF:
        _ASSERTE(!"GNVV: This function should not be called for value types");
        break;

#endif // _WIN64

    default:
         _ASSERTE(!"Bad locType"); break;
    }

    return true;
}


#if defined(_WIN64)
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
#endif // _WIN64


bool    SetNativeVarVal(const ICorDebugInfo::VarLoc &   varLoc,
                        PCONTEXT                        pCtx,
                        SIZE_T                          val1,
                        SIZE_T                          val2
                        WIN64_ARG(SIZE_T                cbSize))
{
    STATIC_CONTRACT_NOTHROW;
    STATIC_CONTRACT_GC_NOTRIGGER;
    STATIC_CONTRACT_FORBID_FAULT;

    switch(varLoc.vlType)
    {
#if !defined(_WIN64)
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

#else  // _WIN64
    case ICorDebugInfo::VLT_REG:
    case ICorDebugInfo::VLT_REG_FP:
    case ICorDebugInfo::VLT_STK:
        SetNativeVarValHelper(NativeVarStackAddr(varLoc, pCtx), val1, val2, cbSize);
        break;

    case ICorDebugInfo::VLT_REG_BYREF:      // fall through
    case ICorDebugInfo::VLT_STK_BYREF:
        _ASSERTE(!"GNVV: This function should not be called for value types");
        break;

#endif // _WIN64

    default:
         _ASSERTE(!"Bad locType"); break;
    }

    return true;
}

HRESULT VMPostError(                    // Returned error.
    HRESULT     hrRpt,                  // Reported error.
    ...)                                // Error arguments.
{
    CONTRACTL
    {
        NOTHROW;
        GC_TRIGGERS;
        MODE_ANY;
    }
    CONTRACTL_END;

    GCX_PREEMP();
   
    va_list     marker;                 // User text.
    va_start(marker, hrRpt);
    hrRpt = PostErrorVA(hrRpt, marker);
    va_end(marker);
    
    return hrRpt;
}

#ifndef CROSSGEN_COMPILE
void VMDumpCOMErrors(HRESULT hrErr)
{
    CONTRACTL
    {
        NOTHROW;
        GC_TRIGGERS;
        MODE_PREEMPTIVE;
        PRECONDITION(FAILED(hrErr));
    }
    CONTRACTL_END;

    SafeComHolderPreemp<IErrorInfo> pIErr(NULL);// Error interface.
    BSTRHolder bstrDesc(NULL);                  // Description text.

    // Try to get an error info object and display the message.
    if (SafeGetErrorInfo(&pIErr) == S_OK && pIErr->GetDescription(&bstrDesc) == S_OK)
    {
        EEMessageBoxCatastrophic(IDS_EE_GENERIC, IDS_FATAL_ERROR, (BSTR)bstrDesc);
    }
    else
    {
        // Just give out the failed hr return code.
        EEMessageBoxCatastrophic(IDS_COMPLUS_ERROR, IDS_FATAL_ERROR, hrErr);
    }
}

//-----------------------------------------------------------------------------
#ifndef FEATURE_PAL

// Wrap registry functions to use CQuickWSTR to allocate space. This does it
// in a stack friendly manner.
//-----------------------------------------------------------------------------
LONG UtilRegEnumKey(HKEY hKey,            // handle to key to query
                    DWORD dwIndex,        // index of subkey to query
                    CQuickWSTR* lpName) // buffer for subkey name
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        INJECT_FAULT(return ERROR_NOT_ENOUGH_MEMORY;);
    }
    CONTRACTL_END;

    DWORD size = (DWORD)lpName->MaxSize();
    LONG result = WszRegEnumKeyEx(hKey,
                                  dwIndex,
                                  lpName->Ptr(),
                                  &size,
                                  NULL,
                                  NULL,
                                  NULL,
                                  NULL);

    if (result == ERROR_SUCCESS || result == ERROR_MORE_DATA) {

        // Grow or shrink buffer to correct size
        if (lpName->ReSizeNoThrow(size+1) != NOERROR)
            result = ERROR_NOT_ENOUGH_MEMORY;

        if (result == ERROR_MORE_DATA) {
            size = (DWORD)lpName->MaxSize();
            result = WszRegEnumKeyEx(hKey,
                                     dwIndex,
                                     lpName->Ptr(),
                                     &size,
                                     NULL,
                                     NULL,
                                     NULL,
                                     NULL);
        }
    }

    return result;
}

LONG UtilRegQueryStringValueEx(HKEY hKey,           // handle to key to query
                               LPCWSTR lpValueName, // address of name of value to query
                               LPDWORD lpReserved,  // reserved
                               LPDWORD lpType,      // address of buffer for value type
                               CQuickWSTR* lpData)// data buffer
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        INJECT_FAULT(return ERROR_NOT_ENOUGH_MEMORY;);
    }
    CONTRACTL_END;

    DWORD size = (DWORD)lpData->MaxSize();
    LONG result = WszRegQueryValueEx(hKey,
                                     lpValueName,
                                     lpReserved,
                                     lpType,
                                     (LPBYTE) lpData->Ptr(),
                                     &size);

    if (result == ERROR_SUCCESS || result == ERROR_MORE_DATA) {

        // Grow or shrink buffer to correct size
        if (lpData->ReSizeNoThrow(size+1) != NOERROR)
            result = ERROR_NOT_ENOUGH_MEMORY;

        if (result == ERROR_MORE_DATA) {
            size = (DWORD)lpData->MaxSize();
            result = WszRegQueryValueEx(hKey,
                                        lpValueName,
                                        lpReserved,
                                        lpType,
                                        (LPBYTE) lpData->Ptr(),
                                        &size);
        }
    }
    
    return result;
}

BOOL ReportEventCLR(
     WORD       wType,
     WORD       wCategory,
     DWORD      dwEventID,
     PSID       lpUserSid,
     SString  * message)
{
    CONTRACTL {
        THROWS;
        GC_TRIGGERS;
        MODE_ANY;
    } CONTRACTL_END;

    GCX_PREEMP();

    SString buff;
    buff.Printf(W(".NET Runtime version %s - %s"), VER_FILEVERSION_STR_L, message->GetUnicode());

    DWORD dwRetVal = ClrReportEvent(W(".NET Runtime"),
                        wType,          // event type 
                        wCategory,      // category
                        dwEventID,      // event identifier 
                        lpUserSid,      // user security identifier
                        buff.GetUnicode()); // one substitution string 

    // Return BOOLEAN based upon return code
    return (dwRetVal == ERROR_SUCCESS)?TRUE:FALSE;
}

// This function checks to see if GetLogicalProcessorInformation API is supported. 
// On success, this function allocates a SLPI array, sets nEntries to number 
// of elements in the SLPI array and returns a pointer to the SLPI array after filling it with information. 
//
// Note: If successful, IsGLPISupported allocates memory for the SLPI array and expects the caller to
// free the memory once the caller is done using the information in the SLPI array.
//
// If the API is not supported or any failure, returns NULL
//
SYSTEM_LOGICAL_PROCESSOR_INFORMATION *IsGLPISupported( PDWORD nEntries ) 
{
    DWORD cbslpi = 0;
    DWORD dwNumElements = 0;
    SYSTEM_LOGICAL_PROCESSOR_INFORMATION *pslpi = NULL;

    // We setup the first call to GetLogicalProcessorInformation to fail so that we can obtain
    // the size of the buffer required to allocate for the SLPI array that is returned

    if (!GetLogicalProcessorInformation(pslpi, &cbslpi) &&
    GetLastError() != ERROR_INSUFFICIENT_BUFFER)
    {
        // If we fail with anything other than an ERROR_INSUFFICIENT_BUFFER here, we punt with failure.
        return NULL;
    }

    _ASSERTE(cbslpi);

    // compute the number of SLPI entries required to hold the information returned from GLPI

    dwNumElements = cbslpi / sizeof(SYSTEM_LOGICAL_PROCESSOR_INFORMATION);

    // allocate a buffer in the free heap to hold an array of SLPI entries from GLPI, number of elements in the array is dwNumElements 

    pslpi = new (nothrow) SYSTEM_LOGICAL_PROCESSOR_INFORMATION[ dwNumElements ];

    if(pslpi == NULL)
    {
        // the memory allocation failed
        return NULL;
    }      

    // Make call to GetLogicalProcessorInformation. Returns array of SLPI structures

    if (!GetLogicalProcessorInformation(pslpi, &cbslpi))
    {
        // GetLogicalProcessorInformation failed
        delete[] pslpi ; //Allocation was fine but the API call itself failed and so we are releasing the memory before the return NULL.
        return NULL ;
    } 

    // GetLogicalProcessorInformation successful, set nEntries to number of entries in the SLPI array
    *nEntries  = dwNumElements;

    return pslpi;    // return pointer to SLPI array

}//IsGLPISupported

// This function returns the size of highest level cache on the physical chip.   If it cannot
// determine the cachesize this function returns 0.
size_t GetLogicalProcessorCacheSizeFromOS()
{
    size_t cache_size = 0;
    DWORD nEntries = 0;

    // Try to use GetLogicalProcessorInformation API and get a valid pointer to the SLPI array if successful.  Returns NULL
    // if API not present or on failure.

    SYSTEM_LOGICAL_PROCESSOR_INFORMATION *pslpi = IsGLPISupported(&nEntries) ;   

    if (pslpi == NULL)
    {
        // GetLogicalProcessorInformation not supported or failed.  
        goto Exit;
    }

    // Crack the information. Iterate through all the SLPI array entries for all processors in system.
    // Will return the greatest of all the processor cache sizes or zero
    {
        size_t last_cache_size = 0;

        for (DWORD i=0; i < nEntries; i++)
        {
            if (pslpi[i].Relationship == RelationCache)
            {
                last_cache_size = max(last_cache_size, pslpi[i].Cache.Size);
            }             
        }  
        cache_size = last_cache_size;
    }
Exit:

    if(pslpi)
        delete[] pslpi;  // release the memory allocated for the SLPI array.    

    return cache_size;
}

#endif // !FEATURE_PAL

// This function returns the number of logical processors on a given physical chip.  If it cannot
// determine the number of logical cpus, or the machine is not populated uniformly with the same
// type of processors, this function returns 0. 

DWORD GetLogicalCpuCountFromOS()
{
    // No CONTRACT possible because GetLogicalCpuCount uses SEH

    STATIC_CONTRACT_THROWS;
    STATIC_CONTRACT_GC_NOTRIGGER;

    static DWORD val = 0;
    DWORD retVal = 0;

#ifdef FEATURE_PAL
    retVal = PAL_GetLogicalCpuCountFromOS();
#else // FEATURE_PAL    
    
    DWORD nEntries = 0;

    DWORD prevcount = 0;
    DWORD count = 1;

    // Try to use GetLogicalProcessorInformation API and get a valid pointer to the SLPI array if successful.  Returns NULL
    // if API not present or on failure.
    SYSTEM_LOGICAL_PROCESSOR_INFORMATION *pslpi = IsGLPISupported(&nEntries) ;

    if (pslpi == NULL)
    {
        // GetLogicalProcessorInformation no supported
        goto lDone;
    }

    for (DWORD j = 0; j < nEntries; j++)
    {
        if (pslpi[j].Relationship == RelationProcessorCore)
        {
            // LTP_PC_SMT indicates HT or SMT
            if (pslpi[j].ProcessorCore.Flags == LTP_PC_SMT)
            {
                SIZE_T pmask = pslpi[j].ProcessorMask;

                // Count the processors in the mask
                //
                // These are not the fastest bit counters. There may be processor intrinsics
                // (which would be best), but there are variants faster than these:
                // See http://en.wikipedia.org/wiki/Hamming_weight.
                // This is the naive implementation.
#if !_WIN64
                count = (pmask & 0x55555555) + ((pmask >> 1) &  0x55555555);
                count = (count & 0x33333333) + ((count >> 2) &  0x33333333);
                count = (count & 0x0F0F0F0F) + ((count >> 4) &  0x0F0F0F0F);
                count = (count & 0x00FF00FF) + ((count >> 8) &  0x00FF00FF);
                count = (count & 0x0000FFFF) + ((count >> 16)&  0x0000FFFF);
#else
                pmask = (pmask & 0x5555555555555555ull) + ((pmask >> 1) & 0x5555555555555555ull);
                pmask = (pmask & 0x3333333333333333ull) + ((pmask >> 2) & 0x3333333333333333ull);
                pmask = (pmask & 0x0f0f0f0f0f0f0f0full) + ((pmask >> 4) & 0x0f0f0f0f0f0f0f0full);
                pmask = (pmask & 0x00ff00ff00ff00ffull) + ((pmask >> 8) & 0x00ff00ff00ff00ffull);
                pmask = (pmask & 0x0000ffff0000ffffull) + ((pmask >> 16) & 0x0000ffff0000ffffull);
                pmask = (pmask & 0x00000000ffffffffull) + ((pmask >> 32) & 0x00000000ffffffffull);
                count = static_cast<DWORD>(pmask);
#endif // !_WIN64 else
                assert (count > 0);

                if (prevcount)
                {
                    if (count != prevcount)
                    {
                        retVal = 1;       // masks are not symmetric
                        goto lDone;
                    }
                }

                prevcount = count;
            }
        }
    }

    retVal = count;

lDone: 

    if(pslpi)
    {
        delete[] pslpi;                        // release the memory allocated for the SLPI array    
    }
#endif // FEATURE_PAL

    return retVal;
}   

#if defined(_TARGET_X86_) || defined(_TARGET_AMD64_)

#define CACHE_WAY_BITS          0xFFC00000      // number of cache WAYS-Associativity is returned in EBX[31:22] (10 bits) using cpuid function 4
#define CACHE_PARTITION_BITS    0x003FF000      // number of cache Physical Partitions is returned in EBX[21:12] (10 bits) using cpuid function 4
#define CACHE_LINESIZE_BITS     0x00000FFF      // Linesize returned in EBX[11:0] (12 bits) using cpuid function 4

// these are defined in src\VM\AMD64\asmhelpers.asm / cgenx86.cpp
extern "C" DWORD __stdcall getcpuid(DWORD arg1, unsigned char result[16]);
extern "C" DWORD __stdcall getextcpuid(DWORD arg1, DWORD arg2, unsigned char result[16]);

// The following function uses a deterministic mechanism for enumerating/calculating the details of the cache hierarychy at runtime
// by using deterministic cache parameter leafs on Prescott and higher processors. 
// If successful, this function returns the cache size in bytes of the highest level on-die cache. Returns 0 on failure.

size_t GetIntelDeterministicCacheEnum()
{
    LIMITED_METHOD_CONTRACT;
    size_t retVal = 0;
    unsigned char buffer[16];
    size_t buflen = ARRAYSIZE(buffer);

    DWORD maxCpuid = getextcpuid(0,0,buffer);
    DWORD dwBuffer[4];
    memcpy(dwBuffer, buffer, buflen);

    if( (maxCpuid > 3) && (maxCpuid < 0x80000000) ) // Deterministic Cache Enum is Supported
    {
        DWORD dwCacheWays, dwCachePartitions, dwLineSize, dwSets;
        DWORD retEAX = 0;
        DWORD loopECX = 0;
        size_t maxSize = 0;
        size_t curSize = 0;

        // Make First call  to getextcpuid with loopECX=0. loopECX provides an index indicating which level to return information about.
        // The second parameter is input EAX=4, to specify we want deterministic cache parameter leaf information. 
        // getextcpuid with EAX=4 should be executed with loopECX = 0,1, ... until retEAX [4:0] contains 00000b, indicating no more
        // cache levels are supported.

        getextcpuid(loopECX, 4, buffer);       
        memcpy(dwBuffer, buffer, buflen);
        retEAX = dwBuffer[0];       // get EAX

        int i = 0;
        while(retEAX & 0x1f)       // Crack cache enums and loop while EAX > 0
        {

            dwCacheWays = (dwBuffer[1] & CACHE_WAY_BITS) >> 22;
            dwCachePartitions = (dwBuffer[1] & CACHE_PARTITION_BITS) >> 12;
            dwLineSize = dwBuffer[1] & CACHE_LINESIZE_BITS;
            dwSets = dwBuffer[2];    // ECX

            curSize = (dwCacheWays+1)*(dwCachePartitions+1)*(dwLineSize+1)*(dwSets+1);

            if (maxSize < curSize)
                maxSize = curSize;

            loopECX++;
            getextcpuid(loopECX, 4, buffer);  
            memcpy(dwBuffer, buffer, buflen);
            retEAX = dwBuffer[0] ;      // get EAX[4:0];        
            i++;
            if (i > 16) {               // prevent infinite looping
              return 0;
            }
        }
        retVal = maxSize;
    }
    return retVal ;
}

// The following function uses CPUID function 2 with descriptor values to determine the cache size.  This requires a-priori 
// knowledge of the descriptor values. This works on gallatin and prior processors (already released processors).
// If successful, this function returns the cache size in bytes of the highest level on-die cache. Returns 0 on failure.

size_t GetIntelDescriptorValuesCache()
{
    LIMITED_METHOD_CONTRACT;
    size_t size = 0;
    size_t maxSize = 0;    
    unsigned char buffer[16];

    getextcpuid(0,2, buffer);         // call CPUID with EAX function 2H to obtain cache descriptor values 

    for (int i = buffer[0]; --i >= 0; )
    {
        int j;
        for (j = 3; j < 16; j += 4)
        {
            // if the information in a register is marked invalid, set to null descriptors
            if  (buffer[j] & 0x80)
            {
                buffer[j-3] = 0;
                buffer[j-2] = 0;
                buffer[j-1] = 0;
                buffer[j-0] = 0;
            }
        }

        for (j = 1; j < 16; j++)
        {
            switch  (buffer[j])    // need to add descriptor values for 8M and 12M when they become known
            {
                case    0x41:
                case    0x79:
                    size = 128*1024;
                    break;

                case    0x42:
                case    0x7A:
                case    0x82:
                    size = 256*1024;
                    break;

                case    0x22:
                case    0x43:
                case    0x7B:
                case    0x83:
                case    0x86:                    
                    size = 512*1024;
                    break;

                case    0x23:
                case    0x44:
                case    0x7C:
                case    0x84:
                case    0x87:                    
                    size = 1024*1024;
                    break;

                case    0x25:
                case    0x45:
                case    0x85:
                    size = 2*1024*1024;
                    break;

                case    0x29:
                    size = 4*1024*1024;
                    break;
            }
            if (maxSize < size)
                maxSize = size;
        }

        if  (i > 0)
            getextcpuid(0,2, buffer);
    }
    return     maxSize;
}



#define NUM_LOGICAL_BITS 0x00FF0000         // EBX[23:16] Bit 16-23 in ebx contains the number of logical
                                                                        // processors per physical processor (using cpuid function 1)
#define INITIAL_APIC_ID_BITS  0xFF000000                 // EBX[31:24] Bits 24-31 (8 bits) return the 8-bit unique
                                                                                      // initial APIC ID for the processor this code is running on.
                                                                                      // Default value = 0xff if HT is not supported

// This function uses CPUID function 1 to return the number of logical processors on a given physical chip.  
// It returns the number of logicals processors on a physical chip. 

DWORD GetLogicalCpuCountFallback()
{   
    BYTE LogicalNum   = 0;
    BYTE PhysicalNum  = 0;
    DWORD lProcCounter = 0;
    unsigned char buffer[16];

    DWORD* dwBuffer = (DWORD*)buffer;
    DWORD retVal = 1;

    getextcpuid(0,1, buffer);  //call CPUID with EAX=1

    if (dwBuffer[3] & (1<<28))  // edx:bit 28 is HT bit
    {
        PhysicalNum = (BYTE) g_SystemInfo.dwNumberOfProcessors ; // total # of processors
        LogicalNum  = (BYTE) ((dwBuffer[1] & NUM_LOGICAL_BITS) >> 16); // # of logical per physical

        if(LogicalNum > 1) 
        {
#ifdef FEATURE_CORESYSTEM
            // CoreSystem doesn't expose GetProcessAffinityMask or SetProcessAffinityMask or anything
            // functionally equivalent. Just assume 1:1 mapping if we get here (in reality we shouldn't since
            // all CoreSystems support GetLogicalProcessorInformation so GetLogicalCpuCountFromOS should have
            // taken care of everything.
            goto fDone;
#else // FEATURE_CORESYSTEM
            HANDLE hCurrentProcessHandle;
            DWORD_PTR  dwProcessAffinity;
            DWORD_PTR  dwSystemAffinity;
            DWORD_PTR  dwAffinityMask;

            // Calculate the appropriate  shifts and mask based on the
            // number of logical processors.

            BYTE i = 1, PHY_ID_MASK  = 0xFF, PHY_ID_SHIFT = 0;
            while (i < LogicalNum)
            {
                i *= 2;
                PHY_ID_MASK  <<= 1;
                PHY_ID_SHIFT++;
            }
            hCurrentProcessHandle = GetCurrentProcess();  

            GetProcessAffinityMask(hCurrentProcessHandle, &dwProcessAffinity, &dwSystemAffinity);

            // Check if available process affinity mask is equal to the available system affinity mask
            // If the masks are equal, then all the processors the OS utilizes are available to the 
            // application.

            if (dwProcessAffinity != dwSystemAffinity)
            {
                retVal = 0;          
                goto fDone;
            }

            dwAffinityMask = 1;

            // loop over all processors, running APIC ID retrieval code starting
            // with the first one by setting process affinity.
            while (dwAffinityMask != 0 && dwAffinityMask <= dwProcessAffinity)
            {
                // Check if this CPU is available
                if (dwAffinityMask & dwProcessAffinity)
                {
                    if (SetProcessAffinityMask(hCurrentProcessHandle, dwAffinityMask))  
                    {
                        BYTE APIC_ID, LOG_ID, PHY_ID;
                        __SwitchToThread(0, CALLER_LIMITS_SPINNING); // Give OS time to switch CPU

                        getextcpuid(0,1, buffer);  //call cpuid with EAX=1

                        APIC_ID = (dwBuffer[1] & INITIAL_APIC_ID_BITS) >> 24;
                        LOG_ID  = APIC_ID & ~PHY_ID_MASK;
                        PHY_ID  = APIC_ID >> PHY_ID_SHIFT;
                        if (LOG_ID != 0)   
                        lProcCounter++;
                    }
                }
                dwAffinityMask = dwAffinityMask << 1;
            }
            // Reset the processor affinity

            SetProcessAffinityMask(hCurrentProcessHandle, dwProcessAffinity);

            // Check if HT is enabled on all the processors
            if(lProcCounter > 0 && (lProcCounter == (DWORD)(PhysicalNum / LogicalNum)))
            {
                retVal = lProcCounter;
                goto fDone;
            }
#endif // FEATURE_CORESYSTEM
        }
    }   
fDone:

    return retVal;
}

#endif // _TARGET_X86_ || _TARGET_AMD64_

size_t GetLargestOnDieCacheSize(BOOL bTrueSize)
{
    // No CONTRACT possible because GetLargestOnDieCacheSize uses SEH

    STATIC_CONTRACT_NOTHROW;
    STATIC_CONTRACT_GC_NOTRIGGER;

#if defined(_TARGET_AMD64_) || defined (_TARGET_X86_)

    static size_t maxSize;
    static size_t maxTrueSize;

    if (maxSize)
    {
        // maxSize and maxTrueSize cached
        if (bTrueSize)
        {
            return maxTrueSize;
        }
        else
        {
            return maxSize;
        }
    }

    DefaultCatchFilterParam param;
    param.pv = COMPLUS_EXCEPTION_EXECUTE_HANDLER;

    PAL_TRY(DefaultCatchFilterParam *, pParam, &param)
    {
        unsigned char buffer[16];
        DWORD* dwBuffer = (DWORD*)buffer;

        DWORD maxCpuId = getcpuid(0, buffer);

        if (dwBuffer[1] == 'uneG') 
        {
            if (dwBuffer[3] == 'Ieni') 
            {
                if (dwBuffer[2] == 'letn') 
                {
                    /*
                    //The following lines are commented because the OS API  on Windows 2003 SP1 is not returning the Cache Relation information on x86. 
                    //Once the OS API (LH and above) is updated with this information, we should start using the OS API to get the cache enumeration by
                    //uncommenting the lines below.

                    tempSize = GetLogicalProcessorCacheSizeFromOS(); //use OS API for cache enumeration on LH and above
                    */
                    size_t tempSize = 0;
                    if (maxCpuId >= 2)         // cpuid support for cache size determination is available
                    {
                        tempSize = GetIntelDeterministicCacheEnum();          // try to use use deterministic cache size enumeration
                        if (!tempSize)
                        {                    // deterministic enumeration failed, fallback to legacy enumeration using descriptor values            
                            tempSize = GetIntelDescriptorValuesCache();   
                        }   
                    }

                    // update maxSize once with final value
                    maxTrueSize = tempSize;

#ifdef _WIN64
                    if (maxCpuId >= 2)
                    {
                        // If we're running on a Prescott or greater core, EM64T tests
                        // show that starting with a gen0 larger than LLC improves performance.
                        // Thus, start with a gen0 size that is larger than the cache.  The value of
                        // 3 is a reasonable tradeoff between workingset and performance.
                        maxSize = maxTrueSize * 3;
                    }
                    else
#endif
                    {
                        maxSize = maxTrueSize;
                    }
                }
            }
        }

        if (dwBuffer[1] == 'htuA') {
            if (dwBuffer[3] == 'itne') {
                if (dwBuffer[2] == 'DMAc') {

                    if (getcpuid(0x80000000, buffer) >= 0x80000006)
                    {
                        getcpuid(0x80000006, buffer);

                        DWORD dwL2CacheBits = dwBuffer[2];
                        DWORD dwL3CacheBits = dwBuffer[3];

                        maxTrueSize = (size_t)((dwL2CacheBits >> 16) * 1024);    // L2 cache size in ECX bits 31-16
								
                        getcpuid(0x1, buffer);
                        DWORD dwBaseFamily = (dwBuffer[0] & (0xF << 8)) >> 8;
                        DWORD dwExtFamily  = (dwBuffer[0] & (0xFF << 20)) >> 20;
                        DWORD dwFamily = dwBaseFamily >= 0xF ? dwBaseFamily + dwExtFamily : dwBaseFamily;

                        if (dwFamily >= 0x10)
                        {
                            BOOL bSkipAMDL3 = FALSE;

                            if (dwFamily == 0x10)   // are we running on a Barcelona (Family 10h) processor?
                            {
                                // check model
                                DWORD dwBaseModel = (dwBuffer[0] & (0xF << 4)) >> 4 ;
                                DWORD dwExtModel  = (dwBuffer[0] & (0xF << 16)) >> 16;
                                DWORD dwModel = dwBaseFamily >= 0xF ? (dwExtModel << 4) | dwBaseModel : dwBaseModel;

                                switch (dwModel)
                                {
                                    case 0x2:
                                        // 65nm parts do not benefit from larger Gen0
                                        bSkipAMDL3 = TRUE;
                                        break;

                                    case 0x4:
                                    default:
                                        bSkipAMDL3 = FALSE;
                                }
                            }

                            if (!bSkipAMDL3)
                            {
                                // 45nm Greyhound parts (and future parts based on newer northbridge) benefit
                                // from increased gen0 size, taking L3 into account
                                getcpuid(0x80000008, buffer);
                                DWORD dwNumberOfCores = (dwBuffer[2] & (0xFF)) + 1;	    // NC is in ECX bits 7-0

                                DWORD dwL3CacheSize = (size_t)((dwL3CacheBits >> 18) * 512 * 1024);  // L3 size in EDX bits 31-18 * 512KB
                                // L3 is shared between cores
                                dwL3CacheSize = dwL3CacheSize / dwNumberOfCores;
                                maxTrueSize += dwL3CacheSize;       // due to exclusive caches, add L3 size (possibly zero) to L2
                                                                    // L1 is too small to worry about, so ignore it
                            }
                        }


                        maxSize = maxTrueSize;
                    }
                }
            }
        }
    }
    PAL_EXCEPT_FILTER(DefaultCatchFilter)
    {
    }
    PAL_ENDTRY

    //    printf("GetLargestOnDieCacheSize returns %d, adjusted size %d\n", maxSize, maxTrueSize);
    if (bTrueSize)
        return maxTrueSize;
    else
        return maxSize;

#else
    size_t cache_size = GetLogicalProcessorCacheSizeFromOS() ; // Returns the size of the highest level processor cache
    return cache_size;

#endif
}

//---------------------------------------------------------------------

#ifndef FEATURE_PAL
ThreadLocaleHolder::~ThreadLocaleHolder()
{
#ifdef FEATURE_USE_LCID
#endif // FEATURE_USE_LCID
    {
        SetThreadLocale(m_locale);
    }
}

HMODULE CLRGetModuleHandle(LPCWSTR lpModuleFileName)
{
    // Don't use dynamic contract: will override GetLastError value
    STATIC_CONTRACT_NOTHROW;
    STATIC_CONTRACT_GC_NOTRIGGER;
    STATIC_CONTRACT_FORBID_FAULT;
    STATIC_CONTRACT_SO_TOLERANT;

    HMODULE hMod = WszGetModuleHandle(lpModuleFileName);
    return hMod;
}


HMODULE CLRGetCurrentModuleHandle()
{
    // Don't use dynamic contract: will override GetLastError value
    STATIC_CONTRACT_NOTHROW;
    STATIC_CONTRACT_GC_NOTRIGGER;
    STATIC_CONTRACT_FORBID_FAULT;
    STATIC_CONTRACT_SO_TOLERANT;

    HMODULE hMod = WszGetModuleHandle(NULL);
    return hMod;
}


#endif // !FEATURE_PAL

LPVOID EEHeapAllocInProcessHeap(DWORD dwFlags, SIZE_T dwBytes);
BOOL EEHeapFreeInProcessHeap(DWORD dwFlags, LPVOID lpMem);
void ShutdownRuntimeWithoutExiting(int exitCode);
BOOL IsRuntimeStarted(DWORD *pdwStartupFlags);

void *GetCLRFunction(LPCSTR FunctionName)
{

    void* func = NULL;
    BEGIN_ENTRYPOINT_VOIDRET;

    LIMITED_METHOD_CONTRACT;

    if (strcmp(FunctionName, "EEHeapAllocInProcessHeap") == 0)
    {
        func = (void*)EEHeapAllocInProcessHeap;
    }
    else if (strcmp(FunctionName, "EEHeapFreeInProcessHeap") == 0)
    {
        func = (void*)EEHeapFreeInProcessHeap;
    }
    else if (strcmp(FunctionName, "ShutdownRuntimeWithoutExiting") == 0)
    {
        func = (void*)ShutdownRuntimeWithoutExiting;
    }
    else if (strcmp(FunctionName, "IsRuntimeStarted") == 0)
    {
        func = (void*)IsRuntimeStarted;
    }
    else {
        _ASSERTE ("Unknown function name");
        func = NULL;
    }
    END_ENTRYPOINT_VOIDRET;

    return func;
}

#endif // CROSSGEN_COMPILE

LPVOID
CLRMapViewOfFileEx(
    IN HANDLE hFileMappingObject,
    IN DWORD dwDesiredAccess,
    IN DWORD dwFileOffsetHigh,
    IN DWORD dwFileOffsetLow,
    IN SIZE_T dwNumberOfBytesToMap,
    IN LPVOID lpBaseAddress
    )
{
#ifdef _DEBUG
#ifdef _TARGET_X86_

    char *tmp = new (nothrow) char;
    if (!tmp)
    {
        SetLastError(ERROR_OUTOFMEMORY);
        return NULL;
    }
    delete tmp;

#endif // _TARGET_X86_
#endif // _DEBUG

    LPVOID pv = MapViewOfFileEx(hFileMappingObject,dwDesiredAccess,dwFileOffsetHigh,dwFileOffsetLow,dwNumberOfBytesToMap,lpBaseAddress);


    if (!pv)
    {
        if(GetLastError()==ERROR_SUCCESS)
            SetLastError(ERROR_OUTOFMEMORY);
        return NULL;
    }

#ifdef _DEBUG
#ifdef _TARGET_X86_
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
#endif // _TARGET_X86_
#endif // _DEBUG
    {
    }

    if (!pv && GetLastError()==ERROR_SUCCESS)
        SetLastError(ERROR_OUTOFMEMORY);

    return pv;
}

LPVOID
CLRMapViewOfFile(
    IN HANDLE hFileMappingObject,
    IN DWORD dwDesiredAccess,
    IN DWORD dwFileOffsetHigh,
    IN DWORD dwFileOffsetLow,
    IN SIZE_T dwNumberOfBytesToMap
    )
{
    WRAPPER_NO_CONTRACT;
    return CLRMapViewOfFileEx(hFileMappingObject,dwDesiredAccess,dwFileOffsetHigh,dwFileOffsetLow,dwNumberOfBytesToMap,NULL);
}


BOOL
CLRUnmapViewOfFile(
    IN LPVOID lpBaseAddress
    )
{
    STATIC_CONTRACT_ENTRY_POINT;

#ifdef _DEBUG
#ifdef _TARGET_X86_
    if (g_pConfig && g_pConfig->ShouldInjectFault(INJECTFAULT_MAPVIEWOFFILE))
    {
        return ClrVirtualFree((LPVOID)lpBaseAddress, 0, MEM_RELEASE);
    }
    else
#endif // _TARGET_X86_
#endif // _DEBUG
    {
        BOOL result = UnmapViewOfFile(lpBaseAddress);
        if (result)
        {
        }
        return result;
    }
}


#ifndef CROSSGEN_COMPILE

static HMODULE CLRLoadLibraryWorker(LPCWSTR lpLibFileName, DWORD *pLastError)
{
    // Don't use dynamic contract: will override GetLastError value
    STATIC_CONTRACT_NOTHROW;
    STATIC_CONTRACT_GC_TRIGGERS;
    STATIC_CONTRACT_FAULT;
    STATIC_CONTRACT_SO_TOLERANT;

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

    // This method should be marked "throws" due to the probe here.
    STATIC_CONTRACT_VIOLATION(ThrowsViolation);

    BEGIN_SO_TOLERANT_CODE(GetThread());
    hmod = CLRLoadLibraryWorker(lpLibFileName, &dwLastError);
    END_SO_TOLERANT_CODE;

    SetLastError(dwLastError);
    return hmod;
}

#ifndef FEATURE_PAL

static HMODULE CLRLoadLibraryExWorker(LPCWSTR lpLibFileName, HANDLE hFile, DWORD dwFlags, DWORD *pLastError)

{
    // Don't use dynamic contract: will override GetLastError value
    STATIC_CONTRACT_NOTHROW;
    STATIC_CONTRACT_GC_TRIGGERS;
    STATIC_CONTRACT_FAULT;
    STATIC_CONTRACT_SO_TOLERANT;

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

    BEGIN_SO_TOLERANT_CODE(GetThread());
    hmod = CLRLoadLibraryExWorker(lpLibFileName, hFile, dwFlags, &lastError);
    END_SO_TOLERANT_CODE;
   
    SetLastError(lastError);
    return hmod;
}

#endif // !FEATURE_PAL

BOOL CLRFreeLibrary(HMODULE hModule)
{
    // Don't use dynamic contract: will override GetLastError value
    STATIC_CONTRACT_NOTHROW;
    STATIC_CONTRACT_GC_TRIGGERS;
    STATIC_CONTRACT_FORBID_FAULT;
    STATIC_CONTRACT_SO_TOLERANT;

    return FreeLibrary(hModule);
}

VOID CLRFreeLibraryAndExitThread(HMODULE hModule,DWORD dwExitCode)
{
    // Don't use dynamic contract: will override GetLastError value
    STATIC_CONTRACT_NOTHROW;
    STATIC_CONTRACT_GC_TRIGGERS;
    STATIC_CONTRACT_FORBID_FAULT;
    STATIC_CONTRACT_SO_TOLERANT;

    // This is no-return
    FreeLibraryAndExitThread(hModule,dwExitCode);
}

#endif // CROSSGEN_COMPILE

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
    STATIC_CONTRACT_SO_TOLERANT;

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
        SO_INTOLERANT;
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
__declspec(thread) bool tls_isSymReaderInProgress = false;
#endif // FEATURE_GDBJIT

// called from the runtime
void DACNotify::DoJITNotification(MethodDesc *MethodDescPtr)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        SO_INTOLERANT;
        MODE_PREEMPTIVE;
    }
    CONTRACTL_END;
#if defined(FEATURE_GDBJIT) && defined(FEATURE_PAL) && !defined(CROSSGEN_COMPILE)
    if(!tls_isSymReaderInProgress)
    {
        tls_isSymReaderInProgress = true;
        NotifyGdb::MethodCompiled(MethodDescPtr);
        tls_isSymReaderInProgress = false;
    }
#endif    
    TADDR Args[2] = { JIT_NOTIFICATION, (TADDR) MethodDescPtr };
    DACNotifyExceptionHelper(Args, 2);
}

void DACNotify::DoJITPitchingNotification(MethodDesc *MethodDescPtr)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        SO_INTOLERANT;
        MODE_PREEMPTIVE;
    }
    CONTRACTL_END;

#if defined(FEATURE_GDBJIT) && defined(FEATURE_PAL) && !defined(CROSSGEN_COMPILE)
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
        SO_INTOLERANT;
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
        SO_INTOLERANT;
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
        SO_INTOLERANT;
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
        SO_INTOLERANT;
        MODE_PREEMPTIVE;
    }
    CONTRACTL_END;

    if (args.typ == GC_MARK_END)
    {
        TADDR Args[3] = { GC_NOTIFICATION, (TADDR) args.typ, args.condemnedGeneration };
        DACNotifyExceptionHelper(Args, 3);
    }
}

void DACNotify::DoExceptionCatcherEnterNotification(MethodDesc *MethodDescPtr, DWORD nativeOffset)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        SO_INTOLERANT;
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
    
BOOL DACNotify::ParseJITNotification(TADDR Args[], TADDR& MethodDescPtr)
{
    _ASSERTE(Args[0] == JIT_NOTIFICATION);
    if (Args[0] != JIT_NOTIFICATION)
    {
        return FALSE;
    }

    MethodDescPtr = Args[1];

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


#if !defined(DACCESS_COMPILE) && !defined(CROSSGEN_COMPILE)


#if defined(_DEBUG) && !defined(FEATURE_PAL)

typedef USHORT
(__stdcall *PFNRtlCaptureStackBackTrace)(
    IN ULONG FramesToSkip,
    IN ULONG FramesToCapture,
    OUT PVOID * BackTrace,
    OUT PULONG BackTraceHash);

static PFNRtlCaptureStackBackTrace s_RtlCaptureStackBackTrace = NULL;

WORD UtilCaptureStackBackTrace(
    ULONG FramesToSkip,
    ULONG FramesToCapture,
    PVOID * BackTrace,
    OUT PULONG BackTraceHash)
{
    WRAPPER_NO_CONTRACT;

#ifdef _DEBUG
    Thread* t = GetThread();
    if (t != NULL) {
        // the thread should not have a hijack set up or we can't walk the stack. 
        _ASSERTE(!(t->m_State & Thread::TS_Hijacked));    
    }
#endif

    if(!s_RtlCaptureStackBackTrace)
    {
        // Don't need to worry about race conditions here since it will be the same value
        HMODULE hModNtdll = GetModuleHandleA("ntdll.dll");
        s_RtlCaptureStackBackTrace = reinterpret_cast<PFNRtlCaptureStackBackTrace>(
            GetProcAddress(hModNtdll, "RtlCaptureStackBackTrace"));
    }
    if (!s_RtlCaptureStackBackTrace) {
        return 0;
    }
    ULONG hash;
    if (BackTraceHash == NULL) {
        BackTraceHash = &hash;
    }
    return s_RtlCaptureStackBackTrace(FramesToSkip, FramesToCapture, BackTrace, BackTraceHash);
}

#endif // #if _DEBUG && !FEATURE_PAL


#ifdef _DEBUG
DisableDelayLoadCheckForOleaut32::DisableDelayLoadCheckForOleaut32()
{
    GetThread()->SetThreadStateNC(Thread::TSNC_DisableOleaut32Check);
}

DisableDelayLoadCheckForOleaut32::~DisableDelayLoadCheckForOleaut32()
{
    GetThread()->ResetThreadStateNC(Thread::TSNC_DisableOleaut32Check);
}

BOOL DelayLoadOleaut32CheckDisabled()
{
    Thread *pThread = GetThread();
    if (pThread && pThread->HasThreadStateNC(Thread::TSNC_DisableOleaut32Check))
    {
        return TRUE;
    }

    return FALSE;
}
#endif

BOOL EnableARM()
{
#ifdef FEATURE_APPDOMAIN_RESOURCE_MONITORING
    CONTRACTL
    {
        NOTHROW;
        // TODO: this should really be GC_TRIGGERS so we wouldn't need the 
        // CONTRACT_VIOLATION below but the hosting API that calls this
        // can be called on a COOP thread and it has a GC_NOTRIGGER contract. 
        // We should use the AD unload thread to call this function on.
        GC_NOTRIGGER;
        SO_TOLERANT;
    }
    CONTRACTL_END;

    BOOL fARMEnabled = g_fEnableARM;

    if (!fARMEnabled)
    {
        if (ThreadStore::s_pThreadStore)
        {
            // We need to establish the baselines for the CPU usage counting.
            Thread *pThread = NULL;
            CONTRACT_VIOLATION(GCViolation);

            // I am returning TRUE here so the caller will NOT enable
            // ARM - if we can't take the thread store lock, something
            // is already kind of messed up so no need to proceed with
            // enabling ARM.
            BEGIN_SO_INTOLERANT_CODE_NOTHROW(GetThread(), return TRUE);
            // Take the thread store lock while we enumerate threads.
            ThreadStoreLockHolder tsl ;

            while ((pThread = ThreadStore::GetThreadList(pThread)) != NULL)
            {
                if (pThread->IsUnstarted() || pThread->IsDead())
                    continue;
                pThread->QueryThreadProcessorUsage();
            }

            END_SO_INTOLERANT_CODE;
        }
        g_fEnableARM = TRUE;
    }

    return fARMEnabled;
#else // FEATURE_APPDOMAIN_RESOURCE_MONITORING
    return FALSE;
#endif // FEATURE_APPDOMAIN_RESOURCE_MONITORING
}

#endif // !DACCESS_COMPILE && !CROSSGEN_COMPILE


static BOOL TrustMeIAmSafe(void *pLock) 
{
    LIMITED_METHOD_CONTRACT;
    return TRUE;
}

LockOwner g_lockTrustMeIAmThreadSafe = { NULL, TrustMeIAmSafe };


DangerousNonHostedSpinLock g_randomLock;
CLRRandom g_random;


int GetRandomInt(int maxVal)
{
#ifndef CROSSGEN_COMPILE
    // Use the thread-local Random instance if possible
    Thread* pThread = GetThread();
    if (pThread)
        return pThread->GetRandom()->Next(maxVal);
#endif

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
// Casing Table Helpers for use in the EE.
//

// // Convert szIn to lower case in the Invariant locale.
INT32 InternalCasingHelper::InvariantToLower(__out_bcount_opt(cMaxBytes) LPUTF8 szOut, int cMaxBytes, __in_z LPCUTF8 szIn)
{
    CONTRACTL {
        THROWS;
        GC_TRIGGERS;
        MODE_ANY;
        INJECT_FAULT(COMPlusThrowOM());
    } CONTRACTL_END

    return InvariantToLowerHelper(szOut, cMaxBytes, szIn, TRUE /*fAllowThrow*/);
}

// Convert szIn to lower case in the Invariant locale.
INT32 InternalCasingHelper::InvariantToLowerNoThrow(__out_bcount_opt(cMaxBytes) LPUTF8 szOut, int cMaxBytes, __in_z LPCUTF8 szIn)
{
    CONTRACTL {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_ANY;
        INJECT_FAULT(return 0;);
    } CONTRACTL_END


    return InvariantToLowerHelper(szOut, cMaxBytes, szIn, FALSE /*fAllowThrow*/);
}

// Convert szIn to lower case in the Invariant locale.
INT32 InternalCasingHelper::InvariantToLowerHelper(__out_bcount_opt(cMaxBytes) LPUTF8 szOut, int cMaxBytes, __in_z LPCUTF8 szIn, BOOL fAllowThrow)
{

    CONTRACTL {
        // This fcn can trigger a lazy load of the TextInfo class.
        if (fAllowThrow) THROWS; else NOTHROW;
        if (fAllowThrow) GC_TRIGGERS; else GC_NOTRIGGER;
        if (fAllowThrow) {INJECT_FAULT(COMPlusThrowOM());} else {INJECT_FAULT(return 0);}
        MODE_ANY;

        PRECONDITION((cMaxBytes == 0) || CheckPointer(szOut));
        PRECONDITION(CheckPointer(szIn));
    } CONTRACTL_END

    int inLength = (int)(strlen(szIn)+1);
    INT32 result = 0;

    LPCUTF8 szInSave = szIn;
    LPUTF8 szOutSave = szOut;
    BOOL bFoundHighChars=FALSE;
    //Compute our end point.
    LPCUTF8 szEnd;
    INT32 wideCopyLen;

    CQuickBytes qbOut;
    LPWSTR szWideOut;

    if (cMaxBytes != 0 && szOut == NULL) {
        if (fAllowThrow) {
            COMPlusThrowHR(ERROR_INVALID_PARAMETER);
        }
        SetLastError(ERROR_INVALID_PARAMETER);
        result = 0;
        goto Exit;
    }

    if (cMaxBytes) {
        szEnd = szOut + min(inLength, cMaxBytes);
        //Walk the string copying the characters.  Change the case on
        //any character between A-Z.
        for (; szOut<szEnd; szOut++, szIn++) {
            if (*szIn>='A' && *szIn<='Z') {
                *szOut = *szIn | 0x20;
            }
            else {
                if (((UINT32)(*szIn))>((UINT32)0x80)) {
                    bFoundHighChars = TRUE;
                    break;
                }
                *szOut = *szIn;
            }
        }

        if (!bFoundHighChars) {
            //If we copied everything, tell them how many bytes we copied,
            //and arrange it so that the original position of the string + the returned
            //length gives us the position of the null (useful if we're appending).
            if (--inLength > cMaxBytes) {
                if (fAllowThrow) {
                    COMPlusThrowHR(HRESULT_FROM_WIN32(ERROR_INSUFFICIENT_BUFFER));
                }
                SetLastError(ERROR_INSUFFICIENT_BUFFER);
                result = 0;
                goto Exit;
            }

            result = inLength;
            goto Exit;
        }
    }
    else {
        szEnd = szIn + inLength;
        for (; szIn<szEnd; szIn++) {
            if (((UINT32)(*szIn))>((UINT32)0x80)) {
                bFoundHighChars = TRUE;
                break;
            }
        }

        if (!bFoundHighChars) {
            result = inLength;
            goto Exit;
        }
    }

    szOut = szOutSave;

#ifndef FEATURE_PAL
   
    //convert the UTF8 to Unicode
    //MAKE_WIDEPTR_FROMUTF8(szInWide, szInSave);

    int __lszInWide;
    LPWSTR szInWide;
    __lszInWide = WszMultiByteToWideChar(CP_UTF8, 0, szInSave, -1, 0, 0);
    if (__lszInWide > MAKE_MAX_LENGTH)
         RaiseException(EXCEPTION_INT_OVERFLOW, EXCEPTION_NONCONTINUABLE, 0, 0);
    szInWide = (LPWSTR) alloca(__lszInWide*sizeof(WCHAR));
    if (szInWide == NULL) {
        if (fAllowThrow) {
            COMPlusThrowOM();
        } else {
            SetLastError(ERROR_NOT_ENOUGH_MEMORY);
            result = 0;
            goto Exit;
        }
    }
    if (0==WszMultiByteToWideChar(CP_UTF8, 0, szInSave, -1, szInWide, __lszInWide)) {
        RaiseException(ERROR_NO_UNICODE_TRANSLATION, EXCEPTION_NONCONTINUABLE, 0, 0);
    }


    wideCopyLen = (INT32)wcslen(szInWide)+1;
    if (fAllowThrow) {
        szWideOut = (LPWSTR)qbOut.AllocThrows(wideCopyLen * sizeof(WCHAR));
    }
    else {
        szWideOut = (LPWSTR)qbOut.AllocNoThrow(wideCopyLen * sizeof(WCHAR));
        if (!szWideOut) {
            SetLastError(ERROR_NOT_ENOUGH_MEMORY);
            result = 0;
            goto Exit;
        }
    }

    //Do the casing operation
    NewApis::LCMapStringEx(W(""), LCMAP_LOWERCASE, szInWide, wideCopyLen, szWideOut, wideCopyLen, NULL, NULL, 0);

    //Convert the Unicode back to UTF8
    result = WszWideCharToMultiByte(CP_UTF8, 0, szWideOut, wideCopyLen, szOut, cMaxBytes, NULL, NULL);

    if ((result == 0) && fAllowThrow) {
        COMPlusThrowWin32();
    }

#endif // !FEATURE_PAL
    
Exit:
    return result;
}

//
//
// COMCharacter and Helper functions
//
//

#ifndef FEATURE_PAL
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
#endif // !FEATURE_PAL

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

#ifndef FEATURE_PAL
    if (c <= (WCHAR) 0x7F) // common case
    {
        BOOL result = (c == ' ') || (c == '\r') || (c == '\n') || (c == '\t') || (c == '\f') || (c == (WCHAR) 0x0B);

        ASSERT(result == ((GetCharacterInfoHelper(c, CT_CTYPE1) & C1_SPACE)!=0));

        return result;
    }

    // GetCharacterInfoHelper costs around 160 instructions
    return((GetCharacterInfoHelper(c, CT_CTYPE1) & C1_SPACE)!=0);
#else // !FEATURE_PAL
    return iswspace(c);
#endif // !FEATURE_PAL
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
#ifndef FEATURE_PAL
    return((GetCharacterInfoHelper(c, CT_CTYPE1) & C1_DIGIT)!=0);
#else // !FEATURE_PAL
    return iswdigit(c);
#endif // !FEATURE_PAL
}

BOOL RuntimeFileNotFound(HRESULT hr)
{
    LIMITED_METHOD_CONTRACT;
    return Assembly::FileNotFound(hr);
}

#ifndef FEATURE_PAL
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
#endif // !FEATURE_PAL

#endif // !DACCESS_COMPILE
