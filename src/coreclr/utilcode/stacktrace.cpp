// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//-----------------------------------------------------------------------------

//-----------------------------------------------------------------------------

#include "stdafx.h"

#include "stacktrace.h"
#include <imagehlp.h>
#include "corhlpr.h"
#include "utilcode.h"
#include "pedecoder.h" // for IMAGE_FILE_MACHINE_NATIVE
#include <minipal/utils.h>

//This is a workaround. We need to work with the debugger team to figure
//out how the module handle of the CLR can be found in a SxS safe way.
HMODULE GetCLRModuleHack()
{
    static HMODULE s_hModCLR = 0;
    if (!s_hModCLR)
    {
        s_hModCLR = GetModuleHandleA(MAIN_CLR_DLL_NAME_A);
    }
    return s_hModCLR;
}

HINSTANCE LoadImageHlp()
{
    STATIC_CONTRACT_NOTHROW;
    STATIC_CONTRACT_GC_NOTRIGGER;
    STATIC_CONTRACT_CANNOT_TAKE_LOCK;
    SCAN_IGNORE_FAULT; // Faults from Wsz funcs are handled.

    return LoadLibraryExA("imagehlp.dll", NULL, 0);
}

HINSTANCE LoadDbgHelp()
{
    STATIC_CONTRACT_NOTHROW;
    STATIC_CONTRACT_GC_NOTRIGGER;
    SCAN_IGNORE_FAULT; // Faults from Wsz funcs are handled.

    return LoadLibraryExA("dbghelp.dll", NULL, 0);
}

/****************************************************************************
* SymCallback *
*---------------------*
*   Description:
*       Callback for imghelp.
****************************************************************************/
BOOL __stdcall SymCallback
(
HANDLE hProcess,
ULONG ActionCode,
PVOID CallbackData,
PVOID UserContext
)
{
    WRAPPER_NO_CONTRACT;

    switch (ActionCode)
    {
    case CBA_DEBUG_INFO:
        OutputDebugStringA("IMGHLP: ");
        OutputDebugStringA((LPCSTR) CallbackData);
        OutputDebugStringA("\n");
        break;

    case CBA_DEFERRED_SYMBOL_LOAD_START:
        OutputDebugStringA("IMGHLP: Deferred symbol load start ");
        OutputDebugStringA(((IMAGEHLP_DEFERRED_SYMBOL_LOAD*)CallbackData)->FileName);
        OutputDebugStringA("\n");
        break;

    case CBA_DEFERRED_SYMBOL_LOAD_COMPLETE:
        OutputDebugStringA("IMGHLP: Deferred symbol load complete ");
        OutputDebugStringA(((IMAGEHLP_DEFERRED_SYMBOL_LOAD*)CallbackData)->FileName);
        OutputDebugStringA("\n");
        break;

    case CBA_DEFERRED_SYMBOL_LOAD_FAILURE:
        OutputDebugStringA("IMGHLP: Deferred symbol load failure ");
        OutputDebugStringA(((IMAGEHLP_DEFERRED_SYMBOL_LOAD*)CallbackData)->FileName);
        OutputDebugStringA("\n");
        break;

    case CBA_DEFERRED_SYMBOL_LOAD_PARTIAL:
        OutputDebugStringA("IMGHLP: Deferred symbol load partial ");
        OutputDebugStringA(((IMAGEHLP_DEFERRED_SYMBOL_LOAD*)CallbackData)->FileName);
        OutputDebugStringA("\n");
        break;
    }

    return FALSE;
}

// @TODO_IA64: all of this stack trace stuff is pretty much broken on 64-bit
// right now because this code doesn't use the new SymXxxx64 functions.

#define LOCAL_ASSERT(x)
//
//--- Macros ------------------------------------------------------------------
//

//
// Types and Constants --------------------------------------------------------
//

struct SYM_INFO
{
    DWORD_PTR   dwOffset;
    char        achModule[cchMaxAssertModuleLen];
    char        achSymbol[cchMaxAssertSymbolLen];
};

//--- Function Pointers to APIs in IMAGEHLP.DLL. Loaded dynamically. ---------

typedef LPAPI_VERSION (__stdcall *pfnImgHlp_ImagehlpApiVersionEx)(
    LPAPI_VERSION AppVersion
    );

typedef BOOL (__stdcall *pfnImgHlp_StackWalk)(
    DWORD                             MachineType,
    HANDLE                            hProcess,
    HANDLE                            hThread,
    LPSTACKFRAME                      StackFrame,
    LPVOID                            ContextRecord,
    PREAD_PROCESS_MEMORY_ROUTINE      ReadMemoryRoutine,
    PFUNCTION_TABLE_ACCESS_ROUTINE    FunctionTableAccessRoutine,
    PGET_MODULE_BASE_ROUTINE          GetModuleBaseRoutine,
    PTRANSLATE_ADDRESS_ROUTINE        TranslateAddress
    );

#ifdef HOST_64BIT
typedef DWORD64 (__stdcall *pfnImgHlp_SymGetModuleBase64)(
    IN  HANDLE          hProcess,
    IN  DWORD64         dwAddr
    );

typedef IMAGEHLP_SYMBOL64 PLAT_IMAGEHLP_SYMBOL;
typedef IMAGEHLP_MODULE64 PLAT_IMAGEHLP_MODULE;

#else
typedef IMAGEHLP_SYMBOL PLAT_IMAGEHLP_SYMBOL;
typedef IMAGEHLP_MODULE PLAT_IMAGEHLP_MODULE;
#endif

#undef IMAGEHLP_SYMBOL
#undef IMAGEHLP_MODULE


typedef BOOL (__stdcall *pfnImgHlp_SymGetModuleInfo)(
    IN  HANDLE                  hProcess,
    IN  DWORD_PTR               dwAddr,
    OUT PLAT_IMAGEHLP_MODULE*   ModuleInfo
    );

typedef LPVOID (__stdcall *pfnImgHlp_SymFunctionTableAccess)(
    HANDLE                  hProcess,
    DWORD_PTR               AddrBase
    );

typedef BOOL (__stdcall *pfnImgHlp_SymGetSymFromAddr)(
    IN  HANDLE                  hProcess,
    IN  DWORD_PTR               dwAddr,
    OUT DWORD_PTR*              pdwDisplacement,
    OUT PLAT_IMAGEHLP_SYMBOL*   Symbol
    );

typedef BOOL (__stdcall *pfnImgHlp_SymInitialize)(
    IN HANDLE   hProcess,
    IN LPSTR    UserSearchPath,
    IN BOOL     fInvadeProcess
    );

typedef BOOL (__stdcall *pfnImgHlp_SymUnDName)(
    IN  PLAT_IMAGEHLP_SYMBOL*   sym,               // Symbol to undecorate
    OUT LPSTR                   UnDecName,         // Buffer to store undecorated name in
    IN  DWORD                   UnDecNameLength    // Size of the buffer
    );

typedef BOOL (__stdcall *pfnImgHlp_SymLoadModule)(
    IN  HANDLE          hProcess,
    IN  HANDLE          hFile,
    IN  PSTR            ImageName,
    IN  PSTR            ModuleName,
    IN  DWORD_PTR       BaseOfDll,
    IN  DWORD           SizeOfDll
    );

typedef BOOL (_stdcall *pfnImgHlp_SymRegisterCallback)(
    IN  HANDLE                          hProcess,
    IN  PSYMBOL_REGISTERED_CALLBACK     CallbackFunction,
    IN  PVOID                           UserContext
    );

typedef DWORD (_stdcall *pfnImgHlp_SymSetOptions)(
    IN  DWORD           SymOptions
    );

typedef DWORD (_stdcall *pfnImgHlp_SymGetOptions)(
    );


struct IMGHLPFN_LOAD
{
    LPCSTR   pszFnName;
    LPVOID * ppvfn;
};


#if defined(HOST_64BIT)
typedef void (*pfn_GetRuntimeStackWalkInfo)(
    IN  ULONG64   ControlPc,
    OUT UINT_PTR* pModuleBase,
    OUT UINT_PTR* pFuncEntry
    );
#endif // HOST_64BIT


//
// Globals --------------------------------------------------------------------
//

static BOOL      g_fLoadedImageHlp = FALSE;          // set to true on success
static BOOL      g_fLoadedImageHlpFailed = FALSE;    // set to true on failure
static HINSTANCE g_hinstImageHlp   = NULL;
static HINSTANCE g_hinstDbgHelp    = NULL;
static HANDLE    g_hProcess = NULL;

pfnImgHlp_ImagehlpApiVersionEx    _ImagehlpApiVersionEx;
pfnImgHlp_StackWalk               _StackWalk;
pfnImgHlp_SymGetModuleInfo        _SymGetModuleInfo;
pfnImgHlp_SymFunctionTableAccess  _SymFunctionTableAccess;
pfnImgHlp_SymGetSymFromAddr       _SymGetSymFromAddr;
pfnImgHlp_SymInitialize           _SymInitialize;
pfnImgHlp_SymUnDName              _SymUnDName;
pfnImgHlp_SymLoadModule           _SymLoadModule;
pfnImgHlp_SymRegisterCallback     _SymRegisterCallback;
pfnImgHlp_SymSetOptions           _SymSetOptions;
pfnImgHlp_SymGetOptions           _SymGetOptions;
#if defined(HOST_64BIT)
pfn_GetRuntimeStackWalkInfo       _GetRuntimeStackWalkInfo;
#endif // HOST_64BIT

IMGHLPFN_LOAD ailFuncList[] =
{
    { "ImagehlpApiVersionEx",   (LPVOID*)&_ImagehlpApiVersionEx },
    { "StackWalk",              (LPVOID*)&_StackWalk },
    { "SymGetModuleInfo",       (LPVOID*)&_SymGetModuleInfo },
    { "SymFunctionTableAccess", (LPVOID*)&_SymFunctionTableAccess },
    { "SymGetSymFromAddr",      (LPVOID*)&_SymGetSymFromAddr },
    { "SymInitialize",          (LPVOID*)&_SymInitialize },
    { "SymUnDName",             (LPVOID*)&_SymUnDName },
    { "SymLoadModule",          (LPVOID*)&_SymLoadModule },
    { "SymRegisterCallback",    (LPVOID*)&_SymRegisterCallback },
    { "SymSetOptions",          (LPVOID*)&_SymSetOptions },
    { "SymGetOptions",          (LPVOID*)&_SymGetOptions },
};


/****************************************************************************
* FillSymbolSearchPath *
*----------------------*
*   Description:
*       Manually pick out all the symbol path information we need for a real
*       stack trace to work.  This includes the default NT symbol paths and
*       places on a VBL build machine where they should live.
****************************************************************************/
#define MAX_SYM_PATH        (1024*8)
#define DEFAULT_SYM_PATH    W("symsrv*symsrv.dll*\\\\symbols\\symbols;")
#define STR_ENGINE_NAME     MAIN_CLR_DLL_NAME_W
LPSTR FillSymbolSearchPathThrows(CQuickBytes &qb)
{
    STATIC_CONTRACT_GC_NOTRIGGER;
    STATIC_CONTRACT_CANNOT_TAKE_LOCK;
    SCAN_IGNORE_FAULT; // Faults from Wsz funcs are handled.

#ifndef DACCESS_COMPILE
    // not allowed to do allocation if current thread suspends EE.
    if (IsSuspendEEThread ())
        return NULL;
#endif

   InlineSString<MAX_SYM_PATH> rcBuff ; // Working buffer
    int         chTotal = 0;                // How full is working buffer.
    int         ch;

    // If the NT symbol server path vars are there, then use those.
    chTotal = WszGetEnvironmentVariable(W("_NT_SYMBOL_PATH"), rcBuff);
    if (chTotal + 1 < MAX_SYM_PATH)
        rcBuff.Append(W(';'));

    // Copy the defacto NT symbol path as well.
    size_t sympathLength = chTotal + ARRAY_SIZE(DEFAULT_SYM_PATH) + 1;
		// integer overflow occurred
	if (sympathLength < (size_t)chTotal || sympathLength < ARRAY_SIZE(DEFAULT_SYM_PATH))
	{
		return NULL;
	}

    if (sympathLength < MAX_SYM_PATH)
    {
        rcBuff.Append(DEFAULT_SYM_PATH);
        chTotal = rcBuff.GetCount();
    }

    // Next, if there is a URTTARGET, add that since that is where ndpsetup places
    // your symobls on an install.
    PathString rcBuffTemp;
    ch = WszGetEnvironmentVariable(W("URTTARGET"), rcBuffTemp);
    rcBuff.Append(rcBuffTemp);
    if (ch != 0 && (chTotal + ch + 1 < MAX_SYM_PATH))
    {
    	size_t chNewTotal = chTotal + ch;
		if (chNewTotal < (size_t)chTotal || chNewTotal < (size_t)ch)
		{ // integer overflow occurred
			return NULL;
		}
        chTotal += ch;
        rcBuff.Append(W(';'));
    }

#ifndef SELF_NO_HOST
    // Fetch the path location of the engine dll and add that path as well, just
    // in case URTARGET didn't cut it either.
    // For no-host builds of utilcode, we don't necessarily have an engine DLL in the
    // process, so skip this part.

    ch = WszGetModuleFileName(GetCLRModuleHack(), rcBuffTemp);


	size_t pathLocationLength = chTotal + ch + 1;
		// integer overflow occurred
	if (pathLocationLength < (size_t)chTotal || pathLocationLength < (size_t)ch)
	{
		return NULL;
	}

    if (ch != 0 && (pathLocationLength < MAX_SYM_PATH))
    {
        chTotal = chTotal + ch - ARRAY_SIZE(STR_ENGINE_NAME);
        rcBuff.Append(W(';'));
    }
#endif

    // Now we have a working buffer with a bunch of interesting stuff.  Time
    // to convert it back to ansi for the imagehlp api's.  Allocate the buffer
    // 2x bigger to handle worst case for MBCS.
    ch = ::WszWideCharToMultiByte(CP_ACP, WC_NO_BEST_FIT_CHARS, rcBuff, -1, 0, 0, 0, 0);
    LPSTR szRtn = (LPSTR) qb.AllocNoThrow(ch + 1);
    if (!szRtn)
        return NULL;
    WszWideCharToMultiByte(CP_ACP, WC_NO_BEST_FIT_CHARS, rcBuff, -1, szRtn, ch+1, 0, 0);
    return (szRtn);
}
LPSTR FillSymbolSearchPath(CQuickBytes &qb)
{
    STATIC_CONTRACT_NOTHROW;
    STATIC_CONTRACT_GC_NOTRIGGER;
    STATIC_CONTRACT_CANNOT_TAKE_LOCK;
    SCAN_IGNORE_FAULT; // Faults from Wsz funcs are handled.
    LPSTR retval = NULL;
    HRESULT hr = S_OK;

    EX_TRY
    {
        retval = FillSymbolSearchPathThrows(qb);
    }
    EX_CATCH_HRESULT(hr);

    if (hr != S_OK)
    {
        SetLastError(hr);
        retval = NULL;
    }

    return retval;
}

/****************************************************************************
* MagicInit *
*-----------*
*   Description:
*       Initializes the symbol loading code. Currently called (if necessary)
*       at the beginning of each method that might need ImageHelp to be
*       loaded.
****************************************************************************/
void MagicInit()
{
    STATIC_CONTRACT_NOTHROW;
    STATIC_CONTRACT_GC_NOTRIGGER;
    STATIC_CONTRACT_CANNOT_TAKE_LOCK;

    if (g_fLoadedImageHlp || g_fLoadedImageHlpFailed)
    {
        return;
    }

    g_hProcess = GetCurrentProcess();

    if (g_hinstDbgHelp == NULL)
    {
        g_hinstDbgHelp = LoadDbgHelp();
    }
    if (NULL == g_hinstDbgHelp)
    {
        // Imagehlp.dll has dependency on dbghelp.dll through delay load.
        // If dbghelp.dll is not available, Imagehlp.dll initializes API's like ImageApiVersionEx to
        // some dummy function.  Then we AV when we use data from _ImagehlpApiVersionEx
        g_fLoadedImageHlpFailed = TRUE;
        return;
    }

    //
    // Try to load imagehlp.dll
    //
    if (g_hinstImageHlp == NULL) {
        g_hinstImageHlp = LoadImageHlp();
    }
    LOCAL_ASSERT(g_hinstImageHlp);

    if (NULL == g_hinstImageHlp)
    {
        g_fLoadedImageHlpFailed = TRUE;
        return;
    }

    //
    // Try to get the API entrypoints in imagehlp.dll
    //
    for (int i = 0; i < ARRAY_SIZE(ailFuncList); i++)
    {
        *(ailFuncList[i].ppvfn) = GetProcAddress(
                g_hinstImageHlp,
                ailFuncList[i].pszFnName);
        LOCAL_ASSERT(*(ailFuncList[i].ppvfn));

        if (!*(ailFuncList[i].ppvfn))
        {
            g_fLoadedImageHlpFailed = TRUE;
            return;
        }
    }

    API_VERSION AppVersion = { 4, 0, API_VERSION_NUMBER, 0 };
    LPAPI_VERSION papiver = _ImagehlpApiVersionEx(&AppVersion);

    //
    // We assume any version 4 or greater is OK.
    //
    LOCAL_ASSERT(papiver->Revision >= 4);
    if (papiver->Revision < 4)
    {
        g_fLoadedImageHlpFailed = TRUE;
        return;
    }

    g_fLoadedImageHlp = TRUE;

    //
    // Initialize imagehlp.dll.  A NULL search path is supposed to resolve
    // symbols but never works.  So pull in everything and put some additional
    // hints that might help out a dev box.
    //

    _SymSetOptions(_SymGetOptions() | SYMOPT_DEFERRED_LOADS|SYMOPT_DEBUG);
#ifndef HOST_64BIT
    _SymRegisterCallback(g_hProcess, SymCallback, 0);
#endif

    CQuickBytes qbSearchPath;
    LPSTR szSearchPath = FillSymbolSearchPath(qbSearchPath);
    _SymInitialize(g_hProcess, szSearchPath, TRUE);

    return;
}


/****************************************************************************
* FillSymbolInfo *
*----------------*
*   Description:
*       Fills in a SYM_INFO structure
****************************************************************************/
void FillSymbolInfo
(
SYM_INFO *psi,
DWORD_PTR dwAddr
)
{
    STATIC_CONTRACT_NOTHROW;
    STATIC_CONTRACT_GC_NOTRIGGER;
    STATIC_CONTRACT_CANNOT_TAKE_LOCK;

    if (!g_fLoadedImageHlp)
    {
        return;
    }

    LOCAL_ASSERT(psi);
    memset(psi, 0, sizeof(SYM_INFO));

    PLAT_IMAGEHLP_MODULE  mi;
    mi.SizeOfStruct = sizeof(mi);

    if (!_SymGetModuleInfo(g_hProcess, dwAddr, &mi))
    {
        strcpy_s(psi->achModule, ARRAY_SIZE(psi->achModule), "<no module>");
    }
    else
    {
        strcpy_s(psi->achModule, ARRAY_SIZE(psi->achModule), mi.ModuleName);
        _strupr_s(psi->achModule, ARRAY_SIZE(psi->achModule));
    }

    CHAR rgchUndec[256];
    const CHAR * pszSymbol = NULL;

    // Name field of IMAGEHLP_SYMBOL is dynamically sized.
    // Pad with space for 255 characters.
    union
    {
        CHAR rgchSymbol[sizeof(PLAT_IMAGEHLP_SYMBOL) + 255];
        PLAT_IMAGEHLP_SYMBOL  sym;
    };

    __try
    {
        sym.SizeOfStruct = sizeof(PLAT_IMAGEHLP_SYMBOL);
        sym.Address = dwAddr;
        sym.MaxNameLength = 255;

        if (_SymGetSymFromAddr(g_hProcess, dwAddr, &psi->dwOffset, &sym))
        {
            pszSymbol = sym.Name;

            if (_SymUnDName(&sym, rgchUndec, STRING_LENGTH(rgchUndec)))
            {
                pszSymbol = rgchUndec;
            }
        }
        else
        {
            pszSymbol = "<no symbol>";
        }
    }
    __except (EXCEPTION_EXECUTE_HANDLER)
    {
        pszSymbol = "<EX: no symbol>";
        psi->dwOffset = dwAddr - mi.BaseOfImage;
    }

    strcpy_s(psi->achSymbol, ARRAY_SIZE(psi->achSymbol), pszSymbol);
}

/****************************************************************************
* FunctionTableAccess *
*---------------------*
*   Description:
*       Helper for imagehlp's StackWalk API.
****************************************************************************/
LPVOID __stdcall FunctionTableAccess
(
HANDLE hProcess,
DWORD_PTR dwPCAddr
)
{
    WRAPPER_NO_CONTRACT;

    HANDLE hFuncEntry = _SymFunctionTableAccess( hProcess, dwPCAddr );

#if defined(HOST_64BIT)
    if (hFuncEntry == NULL)
    {
        if (_GetRuntimeStackWalkInfo == NULL)
        {
            _GetRuntimeStackWalkInfo = (pfn_GetRuntimeStackWalkInfo)
                                       GetProcAddress(GetCLRModuleHack(), "GetRuntimeStackWalkInfo");
            if (_GetRuntimeStackWalkInfo == NULL)
                return NULL;
        }

        _GetRuntimeStackWalkInfo((ULONG64)dwPCAddr, NULL, (UINT_PTR*)(&hFuncEntry));
    }
#endif // HOST_64BIT

    return hFuncEntry;
}

/****************************************************************************
* GetModuleBase *
*---------------*
*   Description:
*       Helper for imagehlp's StackWalk API. Retrieves the base address of
*       the module containing the giving virtual address.
*
*       NOTE: If the module information for the given module hasnot yet been
*       loaded, then it is loaded on this call.
*
*   Return:
*       Base virtual address where the module containing ReturnAddress is
*       loaded, or 0 if the address cannot be determined.
****************************************************************************/
DWORD_PTR __stdcall GetModuleBase
(
HANDLE hProcess,
DWORD_PTR dwAddr
)
{
    STATIC_CONTRACT_NOTHROW;
    STATIC_CONTRACT_GC_NOTRIGGER;

    PLAT_IMAGEHLP_MODULE ModuleInfo;
    ModuleInfo.SizeOfStruct = sizeof(ModuleInfo);

    if (_SymGetModuleInfo(hProcess, dwAddr, &ModuleInfo))
    {
        return ModuleInfo.BaseOfImage;
    }
    else
    {
        MEMORY_BASIC_INFORMATION mbi;

        if (VirtualQueryEx(hProcess, (LPVOID)dwAddr, &mbi, sizeof(mbi)))
        {
            if (mbi.Type & MEM_IMAGE)
            {
                char achFile[MAX_LONGPATH] = {0};
                DWORD cch;

                cch = GetModuleFileNameA(
                        (HINSTANCE)mbi.AllocationBase,
                        achFile,
                        MAX_LONGPATH);

                // Ignore the return code since we can't do anything with it.
                _SymLoadModule(
                    hProcess,
                    NULL,
                    ((cch) ? achFile : NULL),
                    NULL,
                    (DWORD_PTR)mbi.AllocationBase,
                    0);

                return (DWORD_PTR)mbi.AllocationBase;
            }
        }
    }

#if defined(HOST_64BIT)
    if (_GetRuntimeStackWalkInfo == NULL)
    {
        _GetRuntimeStackWalkInfo = (pfn_GetRuntimeStackWalkInfo)
                                   GetProcAddress(GetCLRModuleHack(), "GetRuntimeStackWalkInfo");
        if (_GetRuntimeStackWalkInfo == NULL)
            return NULL;
    }

    DWORD_PTR moduleBase;
    _GetRuntimeStackWalkInfo((ULONG64)dwAddr, (UINT_PTR*)&moduleBase, NULL);
    if (moduleBase != NULL)
        return moduleBase;
#endif // HOST_64BIT

    return 0;
}

#if !defined(DACCESS_COMPILE)
/****************************************************************************
* GetStackBacktrace *
*-------------------*
*   Description:
*       Gets a stacktrace of the current stack, including symbols.
*
*   Return:
*       The number of elements actually retrieved.
****************************************************************************/

UINT GetStackBacktrace
(
UINT ifrStart,          // How many stack elements to skip before starting.
UINT cfrTotal,          // How many elements to trace after starting.
DWORD_PTR* pdwEip,      // Array to be filled with stack addresses.
SYM_INFO* psiSymbols,   // This array is filled with symbol information.
                        // It should be big enough to hold cfrTotal elts.
                        // If NULL, no symbol information is stored.
CONTEXT * pContext      // Context to use (or NULL to use current)
)
{
    STATIC_CONTRACT_NOTHROW;
    STATIC_CONTRACT_GC_NOTRIGGER;
    STATIC_CONTRACT_CANNOT_TAKE_LOCK;

    UINT        nElements   = 0;
    DWORD_PTR*  pdw         = pdwEip;
    SYM_INFO*   psi         = psiSymbols;

    MagicInit();

    memset(pdwEip, 0, cfrTotal*sizeof(DWORD_PTR));

    if (psiSymbols)
    {
        memset(psiSymbols, 0, cfrTotal * sizeof(SYM_INFO));
    }

    if (!g_fLoadedImageHlp)
    {
        return 0;
    }

    CONTEXT context;
    if (pContext == NULL)
    {
        ClrCaptureContext(&context);
    }
    else
    {
        memcpy(&context, pContext, sizeof(CONTEXT));
    }

#ifdef HOST_64BIT
    STACKFRAME64 stkfrm;
    memset(&stkfrm, 0, sizeof(STACKFRAME64));
#else
    STACKFRAME stkfrm;
    memset(&stkfrm, 0, sizeof(STACKFRAME));
#endif

    stkfrm.AddrPC.Mode      = AddrModeFlat;
    stkfrm.AddrStack.Mode   = AddrModeFlat;
    stkfrm.AddrFrame.Mode   = AddrModeFlat;
#if defined(_M_IX86)
    stkfrm.AddrPC.Offset    = context.Eip;
    stkfrm.AddrStack.Offset = context.Esp;
    stkfrm.AddrFrame.Offset = context.Ebp;  // Frame Pointer
#endif

#ifndef HOST_X86
    // If we don't have a user-supplied context, then don't skip any frames.
    // So ignore this function (GetStackBackTrace)
    // ClrCaptureContext on x86 gives us the ESP/EBP/EIP of its caller's caller
    // so we don't need to do this.
    if (pContext == NULL)
    {
        ifrStart += 1;
    }
#endif // !HOST_X86

    for (UINT i = 0; i < ifrStart + cfrTotal; i++)
    {
        if (!_StackWalk(IMAGE_FILE_MACHINE_NATIVE,
                        g_hProcess,
                        GetCurrentThread(),
                        &stkfrm,
                        &context,
                        NULL,
                        (PFUNCTION_TABLE_ACCESS_ROUTINE)FunctionTableAccess,
                        (PGET_MODULE_BASE_ROUTINE)GetModuleBase,
                        NULL))
        {
            break;
        }

        if (i >= ifrStart)
        {
            *pdw++ = stkfrm.AddrPC.Offset;
            nElements++;

            if (psi)
            {
                FillSymbolInfo(psi++, stkfrm.AddrPC.Offset);
            }
        }
    }

    LOCAL_ASSERT(nElements == (UINT)(pdw - pdwEip));
    return nElements;
}
#endif // !defined(DACCESS_COMPILE)

/****************************************************************************
* GetStringFromSymbolInfo *
*-------------------------*
*   Description:
*       Actually prints the info into the string for the symbol.
****************************************************************************/

#ifdef HOST_64BIT
    #define FMT_ADDR_BARE      "%08x`%08x"
    #define FMT_ADDR_OFFSET    "%llX"
#else
    #define FMT_ADDR_BARE      "%08x"
    #define FMT_ADDR_OFFSET    "%lX"
#endif

void GetStringFromSymbolInfo
(
DWORD_PTR dwAddr,
SYM_INFO *psi,   // @parm Pointer to SYMBOL_INFO. Can be NULL.
__out_ecount (cchMaxAssertStackLevelStringLen) CHAR *pszString     // @parm Place to put string.
)
{
    STATIC_CONTRACT_NOTHROW;
    STATIC_CONTRACT_GC_NOTRIGGER;
    STATIC_CONTRACT_CANNOT_TAKE_LOCK;

    LOCAL_ASSERT(pszString);

    // <module>! <symbol> + 0x<offset> 0x<addr>\n

    if (psi)
    {
        sprintf_s(pszString,
                  cchMaxAssertStackLevelStringLen,
                  "%s! %s + 0x" FMT_ADDR_OFFSET " (0x" FMT_ADDR_BARE ")",
                  (psi->achModule[0]) ? psi->achModule : "<no module>",
                  (psi->achSymbol[0]) ? psi->achSymbol : "<no symbol>",
                  psi->dwOffset,
                  DBG_ADDR(dwAddr));
    }
    else
    {
        sprintf_s(pszString, cchMaxAssertStackLevelStringLen, "<symbols not available> (0x%p)", (void *)dwAddr);
    }

    LOCAL_ASSERT(strlen(pszString) < cchMaxAssertStackLevelStringLen);
}

#if !defined(DACCESS_COMPILE)

/****************************************************************************
* GetStringFromStackLevels *
*--------------------------*
*   Description:
*       Retrieves a string from the stack frame. If more than one frame, they
*       are separated by newlines
****************************************************************************/
void GetStringFromStackLevels
(
UINT ifrStart,      // @parm How many stack elements to skip before starting.
UINT cfrTotal,      // @parm How many elements to trace after starting.
                    //  Can't be more than cfrMaxAssertStackLevels.
__out_ecount(cchMaxAssertStackLevelStringLen * cfrTotal) CHAR *pszString,    // @parm Place to put string.
                    //  Max size will be cchMaxAssertStackLevelStringLen * cfrTotal.
CONTEXT * pContext  // @parm Context to start the stack trace at; null for current context.
)
{
    STATIC_CONTRACT_NOTHROW;
    STATIC_CONTRACT_GC_NOTRIGGER;
    STATIC_CONTRACT_CANNOT_TAKE_LOCK;

    LOCAL_ASSERT(pszString);
    LOCAL_ASSERT(cfrTotal < cfrMaxAssertStackLevels);

    *pszString = '\0';

    if (cfrTotal == 0)
    {
        return;
    }

    DWORD_PTR rgdwStackAddrs[cfrMaxAssertStackLevels];
    SYM_INFO rgsi[cfrMaxAssertStackLevels];

    // Ignore this function (GetStringFromStackLevels) if we don't have a user-supplied context.
    if (pContext == NULL)
    {
        ifrStart += 1;
    }

    UINT uiRetrieved =
            GetStackBacktrace(ifrStart, cfrTotal, rgdwStackAddrs, rgsi, pContext);

    // First level
    CHAR aszLevel[cchMaxAssertStackLevelStringLen];
    GetStringFromSymbolInfo(rgdwStackAddrs[0], &rgsi[0], aszLevel);

    size_t bufSize = cchMaxAssertStackLevelStringLen * cfrTotal;

    strcpy_s(pszString, bufSize, aszLevel);

    // Additional levels
    for (UINT i = 1; i < uiRetrieved; ++i)
    {
        strcat_s(pszString, bufSize, "\n");
        GetStringFromSymbolInfo(rgdwStackAddrs[i],
                        &rgsi[i], aszLevel);
        strcat_s(pszString, bufSize, aszLevel);
    }

    LOCAL_ASSERT(strlen(pszString) <= cchMaxAssertStackLevelStringLen * cfrTotal);
}
#endif // !defined(DACCESS_COMPILE)

/****************************************************************************
* GetStringFromAddr *
*-------------------*
*   Description:
*       Returns a string from an address.
****************************************************************************/
void GetStringFromAddr
(
DWORD_PTR dwAddr,
_Out_writes_(cchMaxAssertStackLevelStringLen) LPSTR szString // Place to put string.
                // Buffer must hold at least cchMaxAssertStackLevelStringLen.
)
{
    STATIC_CONTRACT_NOTHROW;
    STATIC_CONTRACT_GC_NOTRIGGER;

    LOCAL_ASSERT(szString);

    SYM_INFO si;
    FillSymbolInfo(&si, dwAddr);

    sprintf_s(szString,
              cchMaxAssertStackLevelStringLen,
              "%s! %s + 0x%p (0x%p)",
              (si.achModule[0]) ? si.achModule : "<no module>",
              (si.achSymbol[0]) ? si.achSymbol : "<no symbol>",
              (void*)si.dwOffset,
              (void*)dwAddr);
}

/****************************************************************************
* MagicDeinit *
*-------------*
*   Description:
*       Cleans up for the symbol loading code. Should be called before exit
*       to free the dynamically loaded imagehlp.dll.
****************************************************************************/
void MagicDeinit(void)
{
    STATIC_CONTRACT_NOTHROW;
    STATIC_CONTRACT_GC_NOTRIGGER;

    if (g_hinstImageHlp)
    {
        FreeLibrary(g_hinstImageHlp);

        g_hinstImageHlp   = NULL;
        g_fLoadedImageHlp = FALSE;
    }
}

#if defined(HOST_X86)
/****************************************************************************
* ClrCaptureContext *
*-------------------*
*   Description:
*       Exactly the contents of RtlCaptureContext for Win7 - Win2K doesn't
*       support this, so we need it for CoreCLR 4, if we require Win2K support
****************************************************************************/
extern "C" __declspec(naked) void __stdcall
ClrCaptureContext(_Out_ PCONTEXT ctx)
{
    __asm {
        push ebx;
        mov  ebx,dword ptr [esp+8]
        mov  dword ptr [ebx+0B0h],eax
        mov  dword ptr [ebx+0ACh],ecx
        mov  dword ptr [ebx+0A8h],edx
        mov  eax,dword ptr [esp]
        mov  dword ptr [ebx+0A4h],eax
        mov  dword ptr [ebx+0A0h],esi
        mov  dword ptr [ebx+09Ch],edi
        mov  word ptr [ebx+0BCh],cs
        mov  word ptr [ebx+098h],ds
        mov  word ptr [ebx+094h],es
        mov  word ptr [ebx+090h],fs
        mov  word ptr [ebx+08Ch],gs
        mov  word ptr [ebx+0C8h],ss
        pushfd
        pop  dword ptr [ebx+0C0h]
        mov  eax,dword ptr [ebp+4]
        mov  dword ptr [ebx+0B8h],eax
        mov  eax,dword ptr [ebp]
        mov  dword ptr [ebx+0B4h],eax
        lea  eax,[ebp+8]
        mov  dword ptr [ebx+0C4h],eax
        mov  dword ptr [ebx],10007h
        pop  ebx
        ret  4
    }
}
#endif // HOST_X86
