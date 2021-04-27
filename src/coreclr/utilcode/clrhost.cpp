// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//

//

#include "stdafx.h"

#include "clrhost.h"
#include "utilcode.h"
#include "ex.h"
#include "clrnt.h"
#include "contract.h"

#if HOST_WINDOWS
extern "C" IMAGE_DOS_HEADER __ImageBase;
#else
static void* pImageBase = NULL;
#endif

void* GetClrModuleBase()
{
    LIMITED_METHOD_CONTRACT;

#if HOST_WINDOWS
    return (void*)&__ImageBase;
#else // HOST_WINDOWS
    // PAL_GetSymbolModuleBase defers to dladdr, which is typically a hash lookup through symbols.
    // It should be fairly fast, however it may take a loader lock, so we will cache the result.
    void* pRet = VolatileLoadWithoutBarrier(&pImageBase);
    if (!pRet)
    {
        pImageBase = pRet = (void*)PAL_GetSymbolModuleBase((void*)GetClrModuleBase);
    }

    return pRet;
#endif // HOST_WINDOWS
}

thread_local int t_CantAllocCount;

#ifdef FAILPOINTS_ENABLED
typedef int (*FHashStack) ();

static FHashStack fHashStack = 0;
static _TEB *HashStackSetupThread = NULL;
static _TEB *RFSCustomDataSetupThread = NULL;

static void SetupHashStack ()
{
    CANNOT_HAVE_CONTRACT;

    FHashStack oldValue = InterlockedCompareExchangeT(&fHashStack,
        reinterpret_cast<FHashStack>(1), reinterpret_cast<FHashStack>(0));
    if ((size_t) oldValue >= 2) {
        return;
    }
    else if ((size_t) oldValue == 0) {
        // We are the first thread to initialize
        HashStackSetupThread = NtCurrentTeb();

        if (CLRConfig::GetConfigValue(CLRConfig::INTERNAL_HashStack) == 0) {
            fHashStack = (FHashStack) 2;
            return;
        }

        PAL_TRY(void *, unused, NULL) {
            FHashStack func;
            HMODULE hmod = LoadLibraryExA ("mscorrfs.dll", NULL, 0);
            if (hmod) {
                func = (FHashStack)GetProcAddress (hmod, "HashStack");
                if (func == 0) {
                    func = (FHashStack)2;
                }
            }
            else
                func = (FHashStack)2;
            fHashStack = func;
        }
        PAL_EXCEPT(EXCEPTION_EXECUTE_HANDLER)
        {
            fHashStack = (FHashStack) 2;
        }
        PAL_ENDTRY;
    }
    else if (NtCurrentTeb() == HashStackSetupThread) {
        // We get here while initializing
        return;
    }
    else {
        // All other threads will wait
        while (fHashStack == (FHashStack) 1) {
            ClrSleepEx (100, FALSE);
        }
    }
}

int RFS_HashStack ()
{
    CANNOT_HAVE_CONTRACT;

    if ((size_t)fHashStack < 2) {
        SetupHashStack ();
    }

    if ((size_t)fHashStack <= 2) {
        return 0;
    }
    else
        return fHashStack ();
}

#endif // FAILPOINTS_ENABLED

DWORD GetClrModulePathName(SString& buffer)
{
#ifdef HOST_WINDOWS
    return WszGetModuleFileName((HINSTANCE)GetClrModuleBase(), buffer);
#else
    return WszGetModuleFileName(PAL_GetPalHostModule(), buffer);
#endif
}

#if defined(SELF_NO_HOST)

HMODULE CLRLoadLibrary(LPCWSTR lpLibFileName)
{
    WRAPPER_NO_CONTRACT;
    return CLRLoadLibraryEx(lpLibFileName, NULL, 0);
}

HMODULE CLRLoadLibraryEx(LPCWSTR lpLibFileName, HANDLE hFile, DWORD dwFlags)
{
    WRAPPER_NO_CONTRACT;
    return WszLoadLibraryEx(lpLibFileName, hFile, dwFlags);
}

BOOL CLRFreeLibrary(HMODULE hModule)
{
    WRAPPER_NO_CONTRACT;
    return FreeLibrary(hModule);
}

#endif // defined(SELF_NO_HOST)


#if defined(_DEBUG_IMPL) && defined(ENABLE_CONTRACTS_IMPL)

//-----------------------------------------------------------------------------------------------
// Imposes a new typeload level limit for the scope of the holder. Any attempt to load a type
// past that limit generates a contract violation assert.
//
// Do not invoke this directly. Invoke it through TRIGGERS_TYPE_LOAD or OVERRIDE_TYPE_LOAD_LEVEL_LIMIT.
//
// Arguments:
//     fConditional   - if FALSE, this holder is a nop - supports the MAYBE_* macros.
//     newLevel       - a value from classloadlevel.h - specifies the new max limit.
//     fEnforceLevelChangeDirection
//                    - if true,  implements TRIGGERS_TYPE_LOAD (level cap only allowed to decrease.)
//                      if false, implements OVERRIDE (level allowed to increase - may only be used
//                                                     by loader and only when recursion is structurally
//                                                     impossible.)
//     szFunction,
//     szFile,
//     lineNum        - records location of holder so we can print it in assertion boxes
//
// Assumptions:
//     ClrDebugState must have been set up (executing any contract will do this.)
//     Thread need *not* have a Thread* structure set up.
//
// Notes:
//     The holder withholds the assert if a LoadsTypeViolation suppress is in effect (but
//     still sets up the new limit.)
//
//     As with other contract annotations, however, the violation suppression is *lifted*
//     within the scope guarded by the holder itself.
//-----------------------------------------------------------------------------------------------
LoadsTypeHolder::LoadsTypeHolder(BOOL       fConditional,
                                 UINT       newLevel,
                                 BOOL       fEnforceLevelChangeDirection,
                                 const char *szFunction,
                                 const char *szFile,
                                 int        lineNum
                               )
{
    // This fcn makes non-scoped changes to ClrDebugState so we cannot use a runtime CONTRACT here.
    STATIC_CONTRACT_NOTHROW;
    STATIC_CONTRACT_GC_NOTRIGGER;
    STATIC_CONTRACT_FORBID_FAULT;


    m_fConditional = fConditional;
    if (m_fConditional)
    {
        m_pClrDebugState = CheckClrDebugState();
        _ASSERTE(m_pClrDebugState);

        m_oldClrDebugState = *m_pClrDebugState;

        if (fEnforceLevelChangeDirection)
        {
            if (newLevel > m_pClrDebugState->GetMaxLoadTypeLevel())
            {
                if (!( (LoadsTypeViolation|BadDebugState) & m_pClrDebugState->ViolationMask()))
                {
                    CONTRACT_ASSERT("Illegal attempt to load a type beyond the current level limit.",
                                    (m_pClrDebugState->GetMaxLoadTypeLevel() + 1) << Contract::LOADS_TYPE_Shift,
                                    Contract::LOADS_TYPE_Mask,
                                    szFunction,
                                    szFile,
                                    lineNum
                                    );
                }
            }
        }

        m_pClrDebugState->ViolationMaskReset(LoadsTypeViolation);
        m_pClrDebugState->SetMaxLoadTypeLevel(newLevel);

        m_contractStackRecord.m_szFunction      = szFunction;
        m_contractStackRecord.m_szFile          = szFile;
        m_contractStackRecord.m_lineNum         = lineNum;
        m_contractStackRecord.m_testmask        = (Contract::ALL_Disabled & ~((UINT)(Contract::LOADS_TYPE_Mask))) | (((newLevel) + 1) << Contract::LOADS_TYPE_Shift);
        m_contractStackRecord.m_construct       = fEnforceLevelChangeDirection ? "TRIGGERS_TYPE_LOAD" : "OVERRIDE_TYPE_LOAD_LEVEL_LIMIT";
        m_contractStackRecord.m_pNext           = m_pClrDebugState->GetContractStackTrace();
        m_pClrDebugState->SetContractStackTrace(&m_contractStackRecord);


    }
} // LoadsTypeHolder::LoadsTypeHolder

//-----------------------------------------------------------------------------------------------
// Restores prior typeload level limit.
//-----------------------------------------------------------------------------------------------
LoadsTypeHolder::~LoadsTypeHolder()
{
    // This fcn makes non-scoped changes to ClrDebugState so we cannot use a runtime CONTRACT here.
    STATIC_CONTRACT_NOTHROW;
    STATIC_CONTRACT_GC_NOTRIGGER;
    STATIC_CONTRACT_FORBID_FAULT;


    if (m_fConditional)
    {
        *m_pClrDebugState = m_oldClrDebugState;
    }
}

#endif //defined(_DEBUG_IMPL) && defined(ENABLE_CONTRACTS_IMPL)
