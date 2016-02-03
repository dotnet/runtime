// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
// ===========================================================================
// File: MIXEDMODE.CPP
// 

//

// MIXEDMODE deals with mixed-mode binaries support
// ===========================================================================



#include "common.h"

#include "mixedmode.hpp"

#include "dllimportcallback.h"

#ifdef FEATURE_MIXEDMODE


IJWNOADThunk::IJWNOADThunk(HMODULE pModulebase, DWORD dwIndex, mdToken Token)
{
    LIMITED_METHOD_CONTRACT;
    m_pModulebase=pModulebase;
    m_dwIndex=dwIndex;
    m_Token=Token;
    m_fAccessingCache = 0;

    for (int i=0; i < IJWNOADThunkStubCacheSize; i++)
    {
        m_cache[i].m_AppDomainID = (ADID)-1;
        m_cache[i].m_CodeAddr = 0;
    }

#ifdef _TARGET_X86_ 
    m_code.Encode((BYTE*)GetEEFuncEntryPoint(IJWNOADThunkJumpTarget), this);
#else // !_TARGET_X86_
    m_code.Encode((BYTE*)GetEEFuncEntryPoint(MakeCall), this);
#endif // !_TARGET_X86_
};

#define E_PROCESS_SHUTDOWN_REENTRY    HRESULT_FROM_WIN32(ERROR_PROCESS_ABORTED)


#ifdef _TARGET_X86_ 
// Slow path lookup...called from stub
extern "C" LPCVOID __stdcall IJWNOADThunkJumpTargetHelper(IJWNOADThunk* pThunk)
{
    WRAPPER_NO_CONTRACT;

    return pThunk->FindThunkTarget();
}
#endif // _TARGET_X86_

LPCVOID IJWNOADThunk::FindThunkTarget()
{
    CONTRACT(LPCVOID)
    {
        INSTANCE_CHECK;
        NOTHROW;
        GC_TRIGGERS;
        MODE_ANY;
        SO_TOLERANT;
        POSTCONDITION(CheckPointer(RETVAL));
    }
    CONTRACT_END;

    // We don't plan on fixing this in Whidbey...the IJW scenario has always assumed throwing is "ok" here.
    CONTRACT_VIOLATION(ThrowsViolation);

    LPCVOID      pvTargetCode = NULL;

    AppDomain*  pDomain;

    Thread* pThread = SetupThread();

    // Ensure that we're in preemptive mode.
    //  We only need this check for a newly created
    //  CLR thread - it defaults to COOP mode from here.
    GCX_PREEMP_NO_DTOR();

    pDomain = GetAppDomain();

    if (NULL == pDomain)
    {
        _ASSERTE(!"Appdomain should've been set up by SetupThread");
        pDomain = SystemDomain::System()->DefaultDomain();
    }


    if (NULL != pDomain)
    {
        // Get a local copy so we don't have to deal with a race condition.
        LPCVOID pCacheTarget = NULL;
        GetCachedInfo(pDomain->GetId(), &pvTargetCode);

        // Cache miss.
        if (pvTargetCode==NULL)
        {
            INSTALL_UNWIND_AND_CONTINUE_HANDLER;
            BEGIN_SO_INTOLERANT_CODE(pThread);
            {
                Module* pModule;

                pModule = pDomain->GetIJWModule(m_pModulebase);
                if (NULL == pModule)
                {
                    // New for Whidbey: In V1.1, we just gave up and raised an exception if the target assembly wasn't already loaded
                    // into the current appdomain. We now force-inject the assembly.

                    PEAssemblyHolder pFile(pDomain->BindExplicitAssembly(m_pModulebase, FALSE));
                    pDomain->LoadAssembly(NULL, pFile, FILE_ACTIVE);

                    // Now, try the lookup again. The LoadAssembly() either worked or it didn't. If it didn't, it is probably
                    // due to lack of memory and all we can do is raise an exception and hope the IJW caller does something reasonable.
                    // Otherwise, we should now succeed in finding the current domain's instantiation of the target module.
                    pModule = pDomain->GetIJWModule(m_pModulebase);
                }

                if (NULL != pModule)
                {
                    pModule->EnsureActive();

                    UMEntryThunk* pThunkTable;

                    pThunkTable  = pModule->GetADThunkTable();
                    pvTargetCode = (LPVOID)GetEEFuncEntryPoint((LPVOID)pThunkTable[m_dwIndex].GetCode());

                    // Populate the cache with our latest info.
                    SetCachedInfo(pDomain->GetId(), pvTargetCode);
                }
            }
            END_SO_INTOLERANT_CODE;
            UNINSTALL_UNWIND_AND_CONTINUE_HANDLER;
        }
    }

    if(pvTargetCode==NULL)
        pvTargetCode=(LPVOID)GetEEFuncEntryPoint(SafeNoModule);

    RETURN (LPCVOID)pvTargetCode;
}

#ifdef _TARGET_X86_ 

#ifdef _MSC_VER 
#pragma warning(push)
#pragma warning (disable : 4740) // There is inline asm code in this function, which disables
                                 // global optimizations.
#endif // _MSC_VER

__declspec(naked)  void _cdecl IJWNOADThunk::MakeCall()
{
    WRAPPER_NO_CONTRACT;
    struct
    {
        LPVOID This;
        LPCVOID RetAddr;
    } Vars;
    #define LocalsSize 8

    _asm enter LocalsSize+4,0;
    _asm push ebx;
    _asm push ecx;
    _asm push edx;
    _asm push esi;
    _asm push edi;

    _asm mov Vars.This, eax;

    //careful above this point
    _ASSERTE(sizeof(Vars)<=LocalsSize);

    Vars.RetAddr = ((IJWNOADThunk*)Vars.This)->FindThunkTarget();

    _ASSERTE(NULL != Vars.RetAddr);

    _asm pop edi;
    _asm pop esi;
    _asm pop edx;
    _asm pop ecx;
    _asm pop ebx;
    _asm mov eax,Vars.RetAddr;
    _asm leave;
    _asm jmp eax;
};

#if defined(_MSC_VER) 
#pragma warning(pop)
#endif

#elif defined(_TARGET_AMD64_)
// Implemented in AMD64\UMThunkStub.asm
#elif defined(_TARGET_ARM_)
// Implemented in Arm\asmhelpers.asm
#else
void __cdecl IJWNOADThunk::MakeCall()
{
    LIMITED_METHOD_CONTRACT;
    PORTABILITY_ASSERT("IJWNOADThunk::MakeCall");
}
#endif

void IJWNOADThunk::SafeNoModule()
{
    STATIC_CONTRACT_THROWS;
    STATIC_CONTRACT_GC_TRIGGERS;

    if (!CanRunManagedCode())
    {
        Thread* pThread=GetThread();

        // DO NOT IMPROVE THIS EXCEPTION!  It cannot be a managed exception.  It
        // cannot be a real exception object because we cannot execute any managed
        // code here.
        if(pThread)
            pThread->m_fPreemptiveGCDisabled = 0;
        COMPlusThrowBoot(E_PROCESS_SHUTDOWN_REENTRY);
    }
    NoModule();
}

void IJWNOADThunk::NoModule()
{
    WRAPPER_NO_CONTRACT;
    INSTALL_UNWIND_AND_CONTINUE_HANDLER;
    //<TODO>This should give the file name as part of the exception message!</TODO>
    COMPlusThrowHR(COR_E_DLLNOTFOUND);
    UNINSTALL_UNWIND_AND_CONTINUE_HANDLER;
}

#endif // FEATURE_MIXEDMODE

