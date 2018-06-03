// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

// ==++==
// 
 
// 
// ==--==

#include "strike.h"
#include "gcinfo.h"
#include "util.h"
#include <dbghelp.h>
#include <limits.h>

#include "sos_md.h"

#ifdef SOS_TARGET_X86
namespace X86GCDump
{
#include "gcdump.h"
#undef assert
#define assert(a)
#define CONTRACTL
#define DAC_ARG(x)
#define CONTRACTL_END
#define LIMITED_METHOD_CONTRACT
#define NOTHROW
#define GC_NOTRIGGER
#define SUPPORTS_DAC
#define LIMITED_METHOD_DAC_CONTRACT
#include "gcdecoder.cpp"
#undef CONTRACTL
#undef CONTRACTL_END
#undef LIMITED_METHOD_CONTRACT
#undef NOTHROW
#undef GC_NOTRIGGER
#undef _ASSERTE
#define _ASSERTE(a) do {} while (0)

#include "gcdump.cpp"
#include "i386/gcdumpx86.cpp"
}
#endif // SOS_TARGET_X86

#ifdef SOS_TARGET_AMD64 
#include "gcdump.h"
#define DAC_ARG(x)
#define SUPPORTS_DAC
#define LIMITED_METHOD_DAC_CONTRACT
#undef LIMITED_METHOD_CONTRACT
#undef PREGDISPLAY
    #ifdef LOG
    #undef LOG
    #endif
    #define LOG(x) ((void)0)
    #ifdef LOG_PIPTR
    #undef LOG_PIPTR
    #endif
    #define LOG_PIPTR(pObjRef, gcFlags, hCallBack) ((void)0)
#include "gcdumpnonx86.cpp"
#endif // SOS_TARGET_AMD64

#include "disasm.h"

#ifndef ERANGE
#define ERANGE 34
#endif

PVOID
GenOpenMapping(
    PCSTR FilePath,
    PULONG Size
    )
{
#ifndef FEATURE_PAL
    HANDLE hFile;
    HANDLE hMappedFile;
    PVOID MappedFile;

    hFile = CreateFileA(
                FilePath,
                GENERIC_READ,
                FILE_SHARE_READ | FILE_SHARE_WRITE,
                NULL,
                OPEN_EXISTING,
                0,
                NULL
                );
#if 0
    if ( hFile == NULL || hFile == INVALID_HANDLE_VALUE ) {

        if (GetLastError() == ERROR_CALL_NOT_IMPLEMENTED) {

            // We're on an OS that doesn't support Unicode
            // file operations.  Convert to ANSI and see if
            // that helps.
            
            CHAR FilePathA [ MAX_LONGPATH + 10 ];

            if (WideCharToMultiByte (CP_ACP,
                                     0,
                                     FilePath,
                                     -1,
                                     FilePathA,
                                     sizeof (FilePathA),
                                     0,
                                     0
                                     ) > 0) {

                hFile = CreateFileA(FilePathA,
                                    GENERIC_READ,
                                    FILE_SHARE_READ | FILE_SHARE_WRITE,
                                    NULL,
                                    OPEN_EXISTING,
                                    0,
                                    NULL
                                    );
            }
        }

        if ( hFile == NULL || hFile == INVALID_HANDLE_VALUE ) {
            return NULL;
        }
    }
#endif

    *Size = GetFileSize(hFile, NULL);
    if (*Size == ULONG_MAX) {
        CloseHandle( hFile );
        return NULL;
    }
    
    hMappedFile = CreateFileMapping (
                        hFile,
                        NULL,
                        PAGE_READONLY,
                        0,
                        0,
                        NULL
                        );

    if ( !hMappedFile ) {
        CloseHandle ( hFile );
        return NULL;
    }

    MappedFile = MapViewOfFile (
                        hMappedFile,
                        FILE_MAP_READ,
                        0,
                        0,
                        0
                        );

    CloseHandle (hMappedFile);
    CloseHandle (hFile);

    return MappedFile;
#else // FEATURE_PAL
    return NULL;
#endif // FEATURE_PAL
}

char* PrintOneLine (__in_z char *begin, __in_z char *limit)
{
    if (begin == NULL || begin >= limit) {
        return NULL;
    }
    char line[128];
    size_t length;
    char *end;
    while (1) {
        if (IsInterrupt())
            return NULL;
        length = strlen (begin);
        end = strstr (begin, "\r\xa");
        if (end == NULL) {
            ExtOut ("%s", begin);
            end = begin+length+1;
            if (end >= limit) {
                return NULL;
            }
        }
        else {
            end += 2;
            length = end-begin;
            while (length) {
                if (IsInterrupt())
                    return NULL;
                size_t n = length;
                if (n > 127) {
                    n = 127;
                }
                strncpy_s (line,_countof(line), begin, n);
                line[n] = '\0';
                ExtOut ("%s", line);
                begin += n;
                length -= n;
            }
            return end;
        }
    }
}

void UnassemblyUnmanaged(DWORD_PTR IP, BOOL bSuppressLines)
{
    char            filename[MAX_PATH_FNAME+1];
    char            line[256];
    int             lcount = 10;

    ULONG linenum = 0;
    ULONG64 Displacement = 0;
    BOOL fLineAvailable = FALSE;
    ULONG64 vIP = 0;
    
    if (!bSuppressLines)
    {
        ReloadSymbolWithLineInfo();
        fLineAvailable = SUCCEEDED (g_ExtSymbols->GetLineByOffset(TO_CDADDR(IP), 
                                                                  &linenum,
                                                                  filename,
                                                                  MAX_PATH_FNAME+1,
                                                                  NULL,
                                                                  &Displacement));
    }
    ULONG FileLines = 0;
    ArrayHolder<ULONG64> Buffer = NULL;

    if (fLineAvailable)
    {
        g_ExtSymbols->GetSourceFileLineOffsets(filename, NULL, 0, &FileLines);
        if (FileLines == 0xFFFFFFFF || FileLines == 0)
            fLineAvailable = FALSE;
    }

    if (fLineAvailable)
    {
        Buffer = new ULONG64[FileLines];
        if (Buffer == NULL)
            fLineAvailable = FALSE;
    }
    
    if (!fLineAvailable)
    {
        vIP = TO_CDADDR(IP);
        // There is no line info.  Just disasm the code.
        while (lcount-- > 0)
        {
            if (IsInterrupt())
                return;
            g_ExtControl->Disassemble (vIP, 0, line, 256, NULL, &vIP);
            ExtOut (line);
        }
        return;
    }

    g_ExtSymbols->GetSourceFileLineOffsets(filename, Buffer, FileLines, NULL);
    
    int beginLine = 0;
    int endLine = 0;
    int lastLine;
    linenum --;
    for (lastLine = linenum; lastLine >= 0; lastLine --) {
        if (IsInterrupt())
            return;
        if (Buffer[lastLine] != DEBUG_INVALID_OFFSET) {
            g_ExtSymbols->GetNameByOffset(Buffer[lastLine], NULL, 0, NULL, &Displacement);
            if (Displacement == 0) {
                beginLine = lastLine;
                break;
            }
        }
    }
    if (lastLine < 0) {
        int n = lcount / 2;
        lastLine = linenum-1;
        beginLine = lastLine;
        while (lastLine >= 0) {
            if (IsInterrupt())
                return;
            if (Buffer[lastLine] != DEBUG_INVALID_OFFSET) {
                beginLine = lastLine;
                n --;
                if (n == 0) {
                    break;
                }
            }
            lastLine --;
        }
    }
    while (beginLine > 0 && Buffer[beginLine-1] == DEBUG_INVALID_OFFSET) {
        if (IsInterrupt())
            return;
        beginLine --;
    }
    int endOfFunc = 0;
    for (lastLine = linenum+1; (ULONG)lastLine < FileLines; lastLine ++) {
        if (IsInterrupt())
            return;
        if (Buffer[lastLine] != DEBUG_INVALID_OFFSET) {
            g_ExtSymbols->GetNameByOffset(Buffer[lastLine], NULL, 0, NULL, &Displacement);
            if (Displacement == 0) {
                endLine = lastLine;
                break;
            }
            endOfFunc = lastLine;
        }
    }
    if ((ULONG)lastLine == FileLines) {
        int n = lcount / 2;
        lastLine = linenum+1;
        endLine = lastLine;
        while ((ULONG)lastLine < FileLines) {
            if (IsInterrupt())
                return;
            if (Buffer[lastLine] != DEBUG_INVALID_OFFSET) {
                endLine = lastLine;
                n --;
                if (n == 0) {
                    break;
                }
            }
            lastLine ++;
        }
    }

    PVOID MappedBase = NULL;
    ULONG MappedSize = 0;

    class ToUnmap
    {
        PVOID *m_Base;
    public:
        ToUnmap (PVOID *base)
        :m_Base(base)
        {}
        ~ToUnmap ()
        {
            if (*m_Base) {
                UnmapViewOfFile (*m_Base);
                *m_Base = NULL;
            }
        }
    };
    ToUnmap toUnmap(&MappedBase);

#define MAX_SOURCE_PATH 1024
    char Found[MAX_SOURCE_PATH];
    char *pFile;
    if (g_ExtSymbols->FindSourceFile(0,
                                     filename, 
                                     DEBUG_FIND_SOURCE_BEST_MATCH | DEBUG_FIND_SOURCE_FULL_PATH, 
                                     NULL,
                                     Found, 
                                     sizeof(Found), 
                                     NULL) != S_OK)
    {
        pFile = filename;
    }
    else
    {
        MappedBase = GenOpenMapping(Found, &MappedSize);
        pFile = Found;
    }
    
    lastLine = beginLine;
    char *pFileCh = (char*)MappedBase;
    if (MappedBase) {
        ExtOut ("%s\n", pFile);
        int n = beginLine;
        while (n > 0) {
            while (!(pFileCh[0] == '\r' && pFileCh[1] == 0xa)) {
                if (IsInterrupt())
                    return;
                pFileCh ++;
            }
            pFileCh += 2;
            n --;
        }
    }
    
    char filename1[MAX_PATH_FNAME+1];
    for (lastLine = beginLine; lastLine < endLine; lastLine ++) {
        if (IsInterrupt())
            return;
        if (MappedBase) {
            ExtOut("%4d ", lastLine+1);
            pFileCh = PrintOneLine(pFileCh, (char*)MappedBase+MappedSize);
        }
        if (Buffer[lastLine] != DEBUG_INVALID_OFFSET) {
            if (MappedBase == 0) {
                ExtOut (">>> %s:%d\n", pFile, lastLine+1);
            }
            vIP = Buffer[lastLine];
            ULONG64 vNextLineIP;
            int i;
            for (i = lastLine + 1; (ULONG)i < FileLines && Buffer[i] == DEBUG_INVALID_OFFSET; i ++) {
                if (IsInterrupt())
                    return;
            }
            if ((ULONG)i == FileLines) {
                vNextLineIP = 0;
            }
            else
                vNextLineIP = Buffer[i];
            while (1) {
                if (IsInterrupt())
                    return;
                g_ExtControl->Disassemble(vIP, 0, line, 256, NULL, &vIP);
                ExtOut (line);
                if (vIP > vNextLineIP || vNextLineIP - vIP > 40) {
                    if (FAILED (g_ExtSymbols->GetLineByOffset(vIP, &linenum,
                                                              filename1,
                                                              MAX_PATH_FNAME+1,
                                                              NULL,
                                                              &Displacement))) {
                        if (lastLine != endOfFunc) {
                            break;
                        }
                        if (strstr (line, "ret") || strstr (line, "jmp")) {
                            break;
                        }
                    }

                    if (linenum != (ULONG)lastLine+1 || strcmp (filename, filename1)) {
                        break;
                    }
                }
                else if (vIP == vNextLineIP) {
                    break;
                }
            }
        }
    }
}

void DisasmAndClean (DWORD_PTR &IP, __out_ecount_opt(length) char *line, ULONG length)
{
    ULONG64 vIP = TO_CDADDR(IP);
    g_ExtControl->Disassemble (vIP, 0, line, length, NULL, &vIP);
    IP = (DWORD_PTR)vIP;
    // remove the ending '\n'
    char *ptr = strrchr (line, '\n');
    if (ptr != NULL)
        ptr[0] = '\0';
}

// If byref, move to pass the byref prefix
BOOL IsByRef (__deref_inout_z char *& ptr)
{
    BOOL bByRef = FALSE;
    const char* qindirCh = "qword ptr [";
    const char* dindirCh = "dword ptr [";
    const char* qindirDsCh = "qword ptr ds:[";
    const char* dindirDsCh = "dword ptr ds:[";
    if (ptr[0] == '[')
    {
        bByRef = TRUE;
        ptr ++;
    }
    else if (!IsDbgTargetArm() && !strncmp (ptr, IsDbgTargetWin64() ? qindirCh : dindirCh, 11))
    {
        bByRef = TRUE;
        ptr += 11;
    }
    // The new disassembly engine for windbg formats indirect calls 
    // slightly differently:
    else if (!IsDbgTargetArm() && !strncmp (ptr, IsDbgTargetWin64() ? qindirDsCh : dindirDsCh, 14))
    {
        bByRef = TRUE;
        ptr += 14;
    }
    return bByRef;
}

BOOL IsTermSep (char ch)
{
    return (ch == '\0' || isspace (ch) || ch == ',' || ch == '\n');
}

// Find next term. A term is seperated by space or ,
void NextTerm (__deref_inout_z char *& ptr)
{
    // If we have a byref, skip to ']'
    if (IsByRef (ptr))
    {
        while (ptr[0] != ']' && ptr[0] != '\0')
        {
            if (IsInterrupt())
                return;
            ptr ++;
        }
        if (ptr[0] == ']')
            ptr ++;
    }
    
    while (!IsTermSep (ptr[0]))
    {
        if (IsInterrupt())
            return;
        ptr ++;
    }

    while (IsTermSep(ptr[0]) && (*ptr != '\0'))
    {
        if (IsInterrupt())
            return;
        ptr ++;
    }
}


// Parses something like 6e24d310, 0x6e24d310, or 6e24d310h.  
// On 64-bit, also parses things like 000006fb`f9b70f50 and 
// 000006fbf9b70f50 (as well as their 0x-prefix, -h suffix variations).
INT_PTR ParseHexNumber (__in_z char *ptr, ___out char **endptr)
{
    char *endptr1;
    INT_PTR value1 = strtoul(ptr, &endptr1, 16);

#ifdef _TARGET_WIN64_
    if ('`' == endptr1[0] && isxdigit(endptr1[1]))
    {
        char *endptr2;
        INT_PTR value2 = strtoul(endptr1+1, &endptr2, 16);

        value1 = (value1 << 32) | value2;
        endptr1 = endptr2;
    }
    // if the hex number was specified as 000006fbf9b70f50, an overflow occurred
    else if (ULONG_MAX == value1 && errno == ERANGE)
    {
        if (!strncmp(ptr, "0x", 2))
            ptr += 2;

        char savedigit = ptr[8];
        ptr[8] = '\0';

        value1 = strtoul(ptr, &endptr1, 16);

        ptr[8] = savedigit;

        char *endptr2;
        INT_PTR value2 = strtoul(ptr+8, &endptr2, 16);

        size_t ndigits2 = endptr2 - (ptr+8);

        value1 = (value1 << (ndigits2*4)) | value2;
        endptr1 = endptr2;
    }
#endif // _TARGET_WIN64_

    // account for the possible 'h' suffix
    if ((*endptr1 == 'h') || (*endptr1 == 'H'))
    {
        ++endptr1;
    }

    *endptr = endptr1;
    return value1;
}


// only handle pure value, or memory address
INT_PTR GetValueFromExpr(__in_z char *ptr, INT_PTR &value)
{
    BOOL bNegative = FALSE;
    value = 0;
    char *myPtr = ptr;
    BOOL bByRef = IsByRef (myPtr);

    // ARM disassembly contains '#' prefixes for hex constants
    if (*myPtr == '#')
        ++myPtr;

    if (myPtr[0] == '-')
    {
        myPtr ++;
        bNegative = TRUE;
    }
    if (!strncmp (myPtr, "0x", 2) || isxdigit (myPtr[0]))
    {
        char *endptr;
        value = ParseHexNumber(myPtr, &endptr);
        if ((!bByRef && IsTermSep(endptr[0])) || (bByRef && endptr[0] == ']'))
        {
            if (bNegative)
                value = -value;
            ptr = endptr;
            if (bByRef)
            {
                ptr += 1;
                SafeReadMemory (TO_TADDR(value), &value, 4, NULL);
            }
            return ptr - myPtr;
        }
    }

    // handle mscorlib+0xed310 (6e24d310)
    if (!bByRef)
    {
        ptr = myPtr;
        // handle 'offset ' before the expression:
        if (strncmp(ptr, "offset ", 7) == 0)
        {
            ptr += 7;
        }
        while (ptr[0] != ' ' && ptr[0] != '+' && ptr[0] != '\0')
        {
            if (IsInterrupt())
                return 0;
            ptr ++;
        }
        if (ptr[0] == '+')
        {
            NextTerm (ptr);
            if (ptr[0] == '(')
            {
                ptr ++;
                char *endptr;
                value = ParseHexNumber(ptr, &endptr);
                if (endptr[0] == ')')
                {
                    ptr ++;
                    return ptr - myPtr;
                }
            }
        }
    }
    if (bByRef)
    {
        // handle dword [mscorlib+0x2bd788 (02ead788)]
        ptr = myPtr;
        // handle 'offset ' before the expression:
        if (strncmp(ptr, "offset ", 7) == 0)
        {
            ptr += 7;
        }
        while (ptr[0] != '(' && ptr[0] != '\0')
        {
            if (IsInterrupt())
                return 0;
            ptr ++;
        }
        if (ptr[0] == '(')
        {
            ptr ++;
            char *endptr;
            value = ParseHexNumber(ptr, &endptr);
            if (endptr[0] == ')' && endptr[1] == ']')
            {
                ptr = endptr + 2;
                SafeReadMemory (TO_TADDR(value), &value, 4, NULL);
                return ptr - myPtr;
            }
        }
    }

#ifdef _TARGET_WIN64_
    // handle CLRStub@7fffc8601cc (000007fffc8601cc)
    if (!bByRef && !strncmp(myPtr, "CLRStub[", 8))
    {
        ptr = myPtr;
        while (ptr[0] != '(' && ptr[0] != '\0')
        {
            if (IsInterrupt())
                return 0;
            ptr ++;
        }
        if (ptr[0] == '(')
        {
            ptr ++;
            char *endptr;
            value = ParseHexNumber(ptr, &endptr);
            if (endptr[0] == ')')
            {
                ptr ++;
                return ptr - myPtr;
            }
        }
    }
#endif // _TARGET_WIN64_

    return 0;
}


const char * HelperFuncName (size_t IP)
{
    static char s_szHelperName[100];
    if (S_OK == g_sos->GetJitHelperFunctionName(IP, sizeof(s_szHelperName), &s_szHelperName[0], NULL))
        return &s_szHelperName[0];
    else
        return NULL;
}


// Returns:
//   NULL if the EHInfo passed in does not refer to a Typed clause
//   "..." if pEHInfo->isCatchAllHandler is TRUE
//   "TypeName" if pEHInfo is a DACEHInfo*.
// Note:
//   The return is a pointer to a global buffer, therefore this value must
//   be consumed as soon as possible after a call to this function.
LPCWSTR EHTypedClauseTypeName(___in const DACEHInfo* pEHInfo)
{
    _ASSERTE(pEHInfo != NULL);
    if ((pEHInfo->clauseType == EHTyped) && pEHInfo->isCatchAllHandler)
    {
        return W("...");
    }

    // is there a method table or a token to look at?
    if (pEHInfo->clauseType == EHTyped)
    {
        TADDR mt;
        if (pEHInfo->moduleAddr == 0)
        {
            mt = TO_TADDR(pEHInfo->mtCatch);
            NameForMT_s(mt, g_mdName, mdNameLen);
        } else {
            PrettyPrintClassFromToken(TO_TADDR(pEHInfo->moduleAddr), pEHInfo->tokCatch, g_mdName, mdNameLen, FormatCSharp);
        }
        return g_mdName;
    }

    return NULL;
}

BOOL IsClonedFinally(DACEHInfo *pEHInfo)
{
    // This maybe should be determined in the VM and passed in the DACEHInfo struct.
#if defined(_TARGET_AMD64_) || defined(_TARGET_ARM64_)
    return ((pEHInfo->tryStartOffset == pEHInfo->tryEndOffset) &&
            (pEHInfo->tryStartOffset == pEHInfo->handlerStartOffset) &&
            (pEHInfo->clauseType == EHFinally) &&
            pEHInfo->isDuplicateClause);
#else
    return FALSE;
#endif
}


void SOSEHInfo::FormatForDisassembly(CLRDATA_ADDRESS offSet)
{
    LPCWSTR typeName = NULL;
    // the order of printing and iterating will matter on the boundaries

    // Print END tags in forward order (most nested to least nested). However, cloned
    // finally clauses are always at the end, but they should be considered most nested,
    // so have a separate loop to output them first.
    for (UINT i=0; i < EHCount; i++)
    {
        DACEHInfo *pCur = &m_pInfos[i];

        if (IsClonedFinally(pCur) &&
            (offSet == pCur->handlerEndOffset))
        {
            ExtOut ("EHHandler %d: CLONED FINALLY END\n", i);
        }
    }

    for (UINT i=0; i < EHCount; i++)
    {
        DACEHInfo *pCur = &m_pInfos[i];

        if (pCur->isDuplicateClause)
        {
            // Don't print anything for duplicate clauses
            continue;
        }

        if (offSet == pCur->tryEndOffset)
        {
            ExtOut ("EHHandler %d: %s CLAUSE END\n", i, EHTypeName(pCur->clauseType));
        }

        if (offSet == pCur->handlerEndOffset)
        {
            ExtOut ("EHHandler %d: %s HANDLER END\n", i, EHTypeName(pCur->clauseType));
        }
    }

    // Print BEGIN tags in reverse order (least nested to most nested).
    for (UINT i=EHCount-1; i != (UINT)-1; --i)
    {
        DACEHInfo *pCur = &m_pInfos[i];

        // Must do this before the isDuplicatedClause check, since these are marked as duplicated clauses.
        if (IsClonedFinally(pCur) &&
            (offSet == pCur->handlerStartOffset))
        {
            ExtOut ("EHHandler %d: CLONED FINALLY BEGIN\n", i);
        }

        if (pCur->isDuplicateClause)
        {
            // Don't print anything for duplicate clauses
            continue;
        }
        
        if (offSet == pCur->tryStartOffset)
        {
            ExtOut ("EHHandler %d: %s CLAUSE BEGIN", i, EHTypeName(pCur->clauseType));
            typeName = EHTypedClauseTypeName(pCur);
            if (typeName != NULL)
            {
                ExtOut(" catch(%S) ", typeName);
            }
            ExtOut ("\n");
        }

        if (offSet == pCur->handlerStartOffset)
        {
            ExtOut ("EHHandler %d: %s HANDLER BEGIN", i, EHTypeName(pCur->clauseType));
            typeName = EHTypedClauseTypeName(pCur);
            if (typeName != NULL)
            {
                ExtOut(" catch(%S) ", typeName);
            }
            ExtOut ("\n");
        }

        if ((pCur->clauseType == EHFilter) &&
            (offSet == pCur->filterOffset))
        {
            ExtOut ("EHHandler %d: %s FILTER BEGIN\n",i, EHTypeName(pCur->clauseType));
        }
    }
}


//
// Implementation shared by X86, ARM, and X64
// Any cross platform code should resolve through g_targetMachine or should
// use the IS_DBG_TARGET_XYZ macro.
//

void PrintNativeStack(DWORD_PTR ip, BOOL bSuppressLines)
{
    char filename[MAX_PATH_FNAME + 1];
    char symbol[1024];
    ULONG64 displacement;

    HRESULT hr = g_ExtSymbols->GetNameByOffset(TO_CDADDR(ip), symbol, _countof(symbol), NULL, &displacement);
    if (SUCCEEDED(hr) && symbol[0] != '\0')
    {
        ExtOut("%s", symbol);

        if (displacement)
        {
            ExtOut(" + %#x", displacement);
        }

        if (!bSuppressLines)
        {
            ULONG line;
            hr = g_ExtSymbols->GetLineByOffset(TO_CDADDR(ip), &line, filename, _countof(filename), NULL, NULL);
            if (SUCCEEDED(hr))
            {
                ExtOut(" [%s:%d]", filename, line);
            }
        }
    }
    else
    {
        DMLOut(DMLIP(ip));
    }
}

// Return TRUE if we have printed something.
BOOL PrintCallInfo(DWORD_PTR vEBP, DWORD_PTR IP, DumpStackFlag& DSFlag, BOOL bSymbolOnly)
{
    ULONG64 Displacement;
    BOOL bOutput = FALSE;

    // degrade gracefully for debuggees that don't have a runtime loaded, or a DAC available
    DWORD_PTR methodDesc = 0;
    if (!g_bDacBroken)
    {
        methodDesc = FunctionType (IP);
    }

    if (methodDesc > 1)
    {
        bOutput = TRUE;
        if (!bSymbolOnly)
            DMLOut("%p %s ", SOS_PTR(vEBP), DMLIP(IP));
        DMLOut("(MethodDesc %s ", DMLMethodDesc(methodDesc));    
        
        // TODO: Microsoft, more checks to make sure method is not eeimpl, etc. Add this field to MethodDesc
        
        DacpCodeHeaderData codeHeaderData;
        if (codeHeaderData.Request(g_sos, TO_CDADDR(IP)) == S_OK)
        {
            DWORD_PTR IPBegin = (DWORD_PTR) codeHeaderData.MethodStart;        
            methodDesc = (DWORD_PTR) codeHeaderData.MethodDescPtr;
            Displacement = IP - IPBegin;        
            if (IP >= IPBegin && Displacement <= codeHeaderData.MethodSize)
                ExtOut ("+ %#x ", Displacement);    
        }            
        if (NameForMD_s(methodDesc, g_mdName, mdNameLen))
        {
            ExtOut("%S)", g_mdName);
        }
        else
        {
            ExtOut("%s)", DMLIP(IP));
        }
    }
    else
    {
        if (!DSFlag.fEEonly)
        {
            bOutput = TRUE;
            const char *name;
            if (!bSymbolOnly)
                DMLOut("%p %s ", SOS_PTR(vEBP), DMLIP(IP));

            // if AMD64 ever becomes a cross platform target this must be resolved through
            // virtual dispatch rather than conditional compilation
#if defined(_TARGET_AMD64_) || defined(_TARGET_X86_)
            // degrade gracefully for debuggees that don't have a runtime loaded, or a DAC available
            eTargetType ett = ettUnk;
            if (!g_bDacBroken)
            {
                DWORD_PTR finalMDorIP = 0;
                ett = GetFinalTarget(IP, &finalMDorIP);
                if (ett == ettNative || ett==ettJitHelp)
                {
                    methodDesc = 0;
                    IP = finalMDorIP;
                }
                else
                {
                    methodDesc = finalMDorIP;
                }
            }
#endif // _TARGET_AMD64_ || _TARGET_X86_
            if (methodDesc == 0) 
            {
                PrintNativeStack(IP, DSFlag.fSuppressSrcInfo);
            }
            else if (g_bDacBroken)
            {
                // degrade gracefully for debuggees that don't have a runtime loaded, or a DAC available
                DMLOut(DMLIP(IP));
            }
            else if (IsMethodDesc (IP))
            {
                NameForMD_s(IP, g_mdName, mdNameLen);
                ExtOut(" (stub for %S)", g_mdName);
            }
            else if (IsMethodDesc(IP+5)) {
                NameForMD_s((DWORD_PTR)(IP+5), g_mdName, mdNameLen);
                DMLOut("%s (MethodDesc %s %S)", DMLIP(IP), DMLMethodDesc(IP+5), g_mdName);
            }
            else if ((name = HelperFuncName(IP)) != NULL) {
                ExtOut(" (JitHelp: %s)", name);
            }
#if defined(_TARGET_AMD64_) || defined(_TARGET_X86_)
            else if (ett == ettMD || ett == ettStub)
            {
                NameForMD_s(methodDesc, g_mdName,mdNameLen);                    
                DMLOut("%s (stub for %S)", DMLIP(IP), g_mdName);
                // fallthrough to return
            }
#endif // _TARGET_AMD64_ || _TARGET_X86_
            else
            {
                DMLOut(DMLIP(IP));
            }
        }
    }
    return bOutput;
}

void DumpStackWorker (DumpStackFlag &DSFlag)
{
    DWORD_PTR eip;
    ULONG64 Offset;
    g_ExtRegisters->GetInstructionOffset(&Offset);
    eip = (DWORD_PTR)Offset;
    
    ExtOut("Current frame: ");
    PrintCallInfo (0, eip, DSFlag, TRUE);
    ExtOut ("\n");

    // make certain dword/qword aligned
    DWORD_PTR ptr = DSFlag.top & (~ALIGNCONST);
    
    ExtOut (g_targetMachine->GetDumpStackHeading());
    while (ptr < DSFlag.end)
    {
        if (IsInterrupt())
            return;
        DWORD_PTR retAddr;
        DWORD_PTR whereCalled;
        move_xp(retAddr, ptr);
        g_targetMachine->IsReturnAddress(retAddr, &whereCalled);
        if (whereCalled)
        {
            BOOL bOutput = PrintCallInfo(ptr-sizeof(TADDR), retAddr, DSFlag, FALSE);
            if (!DSFlag.fEEonly)
            {
                if (whereCalled != 0xFFFFFFFF)
                {
                    ExtOut (", calling ");
                    PrintCallInfo (0, whereCalled, DSFlag, TRUE);
                }
            }
            if (bOutput)
                ExtOut ("\n");
            
            DWORD_PTR cxrAddr;
            CROSS_PLATFORM_CONTEXT cxr;
            DWORD_PTR exrAddr;
            EXCEPTION_RECORD exr;

            if (g_targetMachine->GetExceptionContext(ptr,retAddr,&cxrAddr,&cxr,&exrAddr,&exr))
            {
                TADDR sp = g_targetMachine->GetSP(cxr);
                TADDR ip = g_targetMachine->GetIP(cxr);
                bOutput = PrintCallInfo(sp, ip, DSFlag, FALSE);
                if (bOutput)
                {
                    ExtOut(" ====> Exception ");
                    if (exrAddr)
                        ExtOut("Code %x ", exr.ExceptionCode);
                    ExtOut ("cxr@%p", SOS_PTR(cxrAddr));
                    if (exrAddr)
                        ExtOut(" exr@%p", SOS_PTR(exrAddr));
                    ExtOut("\n");
                }
            }
        }
        ptr += sizeof (DWORD_PTR);
    }
}

#ifdef SOS_TARGET_X86
///
/// X86Machine implementation
///
LPCSTR X86Machine::s_DumpStackHeading = "ChildEBP RetAddr  Caller, Callee\n";
LPCSTR X86Machine::s_DSOHeading       = "ESP/REG  Object   Name\n";
LPCSTR X86Machine::s_GCRegs[7]        = {"eax", "ebx", "ecx", "edx", "esi", "edi", "ebp"};
LPCSTR X86Machine::s_SPName           = "ESP";

void PrintNothing (const char *fmt, ...)
{
    // Do nothing.
}

///
/// Dump X86 GCInfo header and table
///
void X86Machine::DumpGCInfo(GCInfoToken gcInfoToken, unsigned methodSize, printfFtn gcPrintf, bool encBytes, bool bPrintHeader) const
{
    X86GCDump::InfoHdr header;
    X86GCDump::GCDump gcDump(gcInfoToken.Version, encBytes, 5, true);
    BYTE* pTable = dac_cast<PTR_BYTE>(gcInfoToken.Info);
    if (bPrintHeader)
    {
        gcDump.gcPrintf = gcPrintf;
        gcPrintf("Method info block:\n");
    }
    else
    {
        gcDump.gcPrintf = PrintNothing;
    }
    pTable += gcDump.DumpInfoHdr(pTable, &header, &methodSize, 0);
    if (bPrintHeader)
    {
        gcPrintf("\n");
        gcPrintf("Pointer table:\n");
    }
    gcDump.gcPrintf = gcPrintf;
    gcDump.DumpGCTable(pTable, header, methodSize, 0);
}
#endif // SOS_TARGET_X86

#ifdef SOS_TARGET_ARM
///
/// ARMMachine implementation
///
LPCSTR ARMMachine::s_DumpStackHeading = "ChildFP  RetAddr  Caller, Callee\n";
LPCSTR ARMMachine::s_DSOHeading       = "SP/REG  Object   Name\n";
LPCSTR ARMMachine::s_GCRegs[14]       = {"r0", "r1", "r2",  "r3",  "r4",  "r5",  "r6",
                                         "r7", "r8", "r9",  "r10", "r11", "r12", "lr"};
LPCSTR ARMMachine::s_SPName           = "sp";

#endif // SOS_TARGET_ARM

#ifdef SOS_TARGET_AMD64
///
/// AMD64Machine implementation
///
LPCSTR AMD64Machine::s_DumpStackHeading = "Child-SP         RetAddr          Caller, Callee\n";
LPCSTR AMD64Machine::s_DSOHeading       = "RSP/REG          Object           Name\n";
LPCSTR AMD64Machine::s_GCRegs[15]       = {"rax", "rbx", "rcx", "rdx", "rsi", "rdi", "rbp",
                                           "r8", "r9", "r10", "r11", "r12", "r13", "r14", "r15"};
LPCSTR AMD64Machine::s_SPName           = "RSP";

///
/// Dump AMD64 GCInfo table
///
void AMD64Machine::DumpGCInfo(GCInfoToken gcInfoToken, unsigned methodSize, printfFtn gcPrintf, bool encBytes, bool bPrintHeader) const
{
    if (bPrintHeader)
    {
        ExtOut("Pointer table:\n");
    }

    GCDump gcDump(gcInfoToken.Version, encBytes, 5, true);
    gcDump.gcPrintf = gcPrintf;

    gcDump.DumpGCTable(dac_cast<PTR_BYTE>(gcInfoToken.Info), methodSize, 0);
}

#endif // SOS_TARGET_AMD64

#ifdef SOS_TARGET_ARM64
///
/// ARM64Machine implementation
///
LPCSTR ARM64Machine::s_DumpStackHeading = "ChildFP          RetAddr          Caller, Callee\n";
LPCSTR ARM64Machine::s_DSOHeading       = "SP/REG           Object           Name\n";
// excluding x18, fp & lr as these will not contain object references
LPCSTR ARM64Machine::s_GCRegs[28]       = {"x0", "x1", "x2",  "x3",  "x4",  "x5",  "x6",
                                           "x7", "x8", "x9",  "x10", "x11", "x12", "x13",
                                           "x14", "x15", "x16", "x17", "x19", "x20","x21",
                                           "x22", "x23", "x24", "x25", "x26", "x27", "x28"};
LPCSTR ARM64Machine::s_SPName           = "sp";

#endif // SOS_TARGET_ARM64


