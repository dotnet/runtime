// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
//

//

#include "stdafx.h"

#include "clrhost.h"
#include "utilcode.h"
#include "ex.h"
#include "hostimpl.h"
#include "clrnt.h"
#include "contract.h"

CoreClrCallbacks g_CoreClrCallbacks;


// In some cirumstance (e.g, the thread suspecd another thread), allocation on heap
// could cause dead lock. We use a counter in TLS to indicate the current thread is not allowed
// to do heap allocation.
//In cases where CLRTlsInfo doesn't exist and we still want to track CantAlloc info (this is important to 
//stress log), We use a global counter. This introduces the problem where one thread could disable allocation 
//for another thread, but the cases should be rare (we limit the use to stress log for now) and the period 
//should (MUST) be short
//only stress log check this counter

struct CantAllocThread
{
    PVOID m_fiberId;
    LONG  m_CantCount;
};

#define MaxCantAllocThreadNum 100
static CantAllocThread g_CantAllocThreads[MaxCantAllocThreadNum] = {};
static Volatile<LONG> g_CantAllocStressLogCount = 0;

void IncCantAllocCount()
{
    size_t count = 0;
    if (ClrFlsCheckValue(TlsIdx_CantAllocCount, (LPVOID *)&count))
    {
        _ASSERTE (count >= 0);
        ClrFlsSetValue(TlsIdx_CantAllocCount,  (LPVOID)(count+1));
        return;
    }
    PVOID fiberId = ClrTeb::GetFiberPtrId();
    for (int i = 0; i < MaxCantAllocThreadNum; i ++)
    {
        if (g_CantAllocThreads[i].m_fiberId == fiberId)
        {
            g_CantAllocThreads[i].m_CantCount ++;
            return;
        }
    }
    for (int i = 0; i < MaxCantAllocThreadNum; i ++)
    {
        if (g_CantAllocThreads[i].m_fiberId == NULL)
        {
            if (InterlockedCompareExchangeT(&g_CantAllocThreads[i].m_fiberId, fiberId, NULL) == NULL)
            {
                _ASSERTE(g_CantAllocThreads[i].m_CantCount == 0);
                g_CantAllocThreads[i].m_CantCount = 1;
                return;
            }
        }
    }
    count = InterlockedIncrement (&g_CantAllocStressLogCount);
    _ASSERTE (count >= 1);
    return;
}

void DecCantAllocCount()
{
    size_t count = 0;
    if (ClrFlsCheckValue(TlsIdx_CantAllocCount, (LPVOID *)&count))
    {
        if (count > 0)
        {
            ClrFlsSetValue(TlsIdx_CantAllocCount,  (LPVOID)(count-1));
            return;
        }
    }
    PVOID fiberId = ClrTeb::GetFiberPtrId();
    for (int i = 0; i < MaxCantAllocThreadNum; i ++)
    {
        if (g_CantAllocThreads[i].m_fiberId == fiberId)
        {
            _ASSERTE (g_CantAllocThreads[i].m_CantCount > 0);
            g_CantAllocThreads[i].m_CantCount --;
            if (g_CantAllocThreads[i].m_CantCount == 0)
            {
                g_CantAllocThreads[i].m_fiberId = NULL;
            }
            return;
        }
    }
    _ASSERTE (g_CantAllocStressLogCount > 0);
    InterlockedDecrement (&g_CantAllocStressLogCount);
    return;

}

// for stress log the rule is more restrict, we have to check the global counter too
BOOL IsInCantAllocStressLogRegion()
{
    size_t count = 0;
    if (ClrFlsCheckValue(TlsIdx_CantAllocCount, (LPVOID *)&count))
    {
        if (count > 0)
        {
            return true;
        }
    }
    PVOID fiberId = ClrTeb::GetFiberPtrId();
    for (int i = 0; i < MaxCantAllocThreadNum; i ++)
    {
        if (g_CantAllocThreads[i].m_fiberId == fiberId)
        {
            _ASSERTE (g_CantAllocThreads[i].m_CantCount > 0);
            return true;
        }
    }

    return g_CantAllocStressLogCount > 0;

}


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



//-----------------------------------------------------------------------------------
// This is the approved way to get a module handle to mscorwks.dll (or coreclr.dll).
// Never call GetModuleHandle(mscorwks) yourself as this will break side-by-side inproc.
//
// This function is safe to call before or during CRT initialization. It can not
// legally return NULL (it only does so in the case of a broken build invariant.)
// 
// TODO puCLR SxS utilcode work: Since this is never supposed to return NULL, it should
// not be present in SELF_NO_HOST builds of utilcode where there isn't necessarily a
// CLR in the process.  We should also ASSERT that GetModuleHandleA isn't returning
// NULL below - we've probably been getting away with this in SELF_NO_HOST cases like
// mscordbi.dll.
//-----------------------------------------------------------------------------------
HMODULE GetCLRModule ()
{
    //! WARNING: At the time this function is invoked, the C Runtime has NOT been fully initialized, let alone the CLR.
    //! So don't put in a runtime contract and don't invoke other functions in the CLR (not even _ASSERTE!) 

    STATIC_CONTRACT_NOTHROW;
    STATIC_CONTRACT_SUPPORTS_DAC; // DAC can call in here since we initialize the SxS callbacks in ClrDataAccess::Initialize.

#ifdef DACCESS_COMPILE
    // For DAC, "g_CoreClrCallbacks" is populated in InitUtilCode when the latter is invoked
    // from ClrDataAccess::Initialize alongwith a reference to a structure allocated in the 
    // host-process address space.
    //
    // This function will be invoked in the host when DAC uses SEHException::GetHr that calls into
    // IsComplusException, which calls into WasThrownByUs that calls GetCLRModule when EH SxS is enabled.
    // However, this function can also be executed within the target space as well.
    //
    // Since DACCop gives the warning to DACize this global that, actually, would be used only
    // in the respective address spaces and does not require marshalling, we need to ignore this
    // warning.
    DACCOP_IGNORE(UndacizedGlobalVariable, "g_CoreClrCallbacks has the dual mode DAC issue.");
#endif // DACCESS_COMPILE
    VALIDATECORECLRCALLBACKS();

    // This is the normal coreclr case - we return the module handle that was captured in our DllMain.
#ifdef DACCESS_COMPILE
    // For DAC, "g_CoreClrCallbacks" is populated in InitUtilCode when the latter is invoked
    // from ClrDataAccess::Initialize alongwith a reference to a structure allocated in the 
    // host-process address space.
    //
    // This function will be invoked in the host when DAC uses SEHException::GetHr that calls into
    // IsComplusException, which calls into WasThrownByUs that calls GetCLRModule when EH SxS is enabled.
    // However, this function can also be executed within the target space as well.
    //
    // Since DACCop gives the warning to DACize this global that, actually, would be used only
    // in the respective address spaces and does not require marshalling, we need to ignore this
    // warning.
    DACCOP_IGNORE(UndacizedGlobalVariable, "g_CoreClrCallbacks has the dual mode DAC issue.");
#endif // DACCESS_COMPILE
    return g_CoreClrCallbacks.m_hmodCoreCLR;
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


//--------------------------------------------------------------------------
// Side by side inproc support
//
// These are new abstractions designed to support loading multiple CLR
// versions in the same process.
//--------------------------------------------------------------------------


//--------------------------------------------------------------------------
// One-time initialized called by coreclr.dll in its dllmain.
//--------------------------------------------------------------------------
VOID InitUtilcode(CoreClrCallbacks const & cccallbacks)
{
    //! WARNING: At the time this function is invoked, the C Runtime has NOT been fully initialized, let alone the CLR.
    //! So don't put in a runtime contract and don't invoke other functions in the CLR (not even _ASSERTE!) 

    LIMITED_METHOD_CONTRACT;

    g_CoreClrCallbacks = cccallbacks;
}

CoreClrCallbacks const & GetClrCallbacks()
{
    LIMITED_METHOD_CONTRACT;

    VALIDATECORECLRCALLBACKS();
    return g_CoreClrCallbacks;
}

#ifdef _DEBUG
void OnUninitializedCoreClrCallbacks()
{
    // Supports DAC since it can be called from GetCLRModule which supports DAC as well.
    LIMITED_METHOD_DAC_CONTRACT;
    
    // If you got here, the most likely cause of the failure is that you're loading some DLL
    // (other than coreclr.dll) that links to utilcode.lib, or that you're using a nohost
    // variant of utilcode.lib but hitting code that assumes there is a CLR in the process.
    //
    // It is expected that coreclr.dll 
    // is the ONLY dll that links to utilcode libraries.
    //
    // If you must introduce a new dll that links to utilcode.lib, it is your responsibility
    // to ensure that that dll invoke InitUtilcode() and forward it the right data from the *correct*
    // loaded instance of coreclr. And you'll have to do without the CRT being initialized.
    //
    // Can't use an _ASSERTE here because even that's broken if we get to this point.
    MessageBoxW(0, 
                W("g_CoreClrCallbacks not initialized."),
                W("\n\n")
                W("You got here because the dll that included this copy of utilcode.lib ")
                W("did not call InitUtilcode() The most likely cause is that you're running ")
                W("a dll (other than coreclr.dll) that links to utilcode.lib.") 
                ,
                0);
    _ASSERTE(FALSE);
    DebugBreak();
}
#endif // _DEBUG

