// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//*****************************************************************************
// File: DebuggerModule.cpp
//

//
// Stuff for tracking DebuggerModules.
//
//*****************************************************************************

#include "stdafx.h"
#include "../inc/common.h"
#include "eeconfig.h" // This is here even for retail & free builds...
#include "vars.hpp"
#include <limits.h>
#include "ilformatter.h"
#include "debuginfostore.h"


/* ------------------------------------------------------------------------ *
 * Debugger Module routines
 * ------------------------------------------------------------------------ */

void DebuggerModule::SetCanChangeJitFlags(bool fCanChangeJitFlags)
{
    m_fCanChangeJitFlags = fCanChangeJitFlags;
}

#ifndef DACCESS_COMPILE


DebuggerModuleTable::DebuggerModuleTable() : CHashTableAndData<CNewZeroData>(101)
{
    WRAPPER_NO_CONTRACT;

    SUPPRESS_ALLOCATION_ASSERTS_IN_THIS_SCOPE;
    NewInit(101, sizeof(DebuggerModuleEntry), 101);
}

DebuggerModuleTable::~DebuggerModuleTable()
{
    WRAPPER_NO_CONTRACT;

    _ASSERTE(ThreadHoldsLock());
    Clear();
}


#ifdef _DEBUG
bool DebuggerModuleTable::ThreadHoldsLock()
{
    // In shutdown (IsAtProcessExit()), the shutdown thread implicitly holds all locks.
    return IsAtProcessExit() || g_pDebugger->HasDebuggerDataLock();
}
#endif

void DebuggerModuleTable::Clear()
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
    }
    CONTRACTL_END;

    _ASSERTE(ThreadHoldsLock());

    HASHFIND hf;
    DebuggerModuleEntry *pDME;

    pDME = (DebuggerModuleEntry *) FindFirstEntry(&hf);

    while (pDME)
    {
        DebuggerModule *pDM = pDME->module;
        Module         *pEEM = pDM->GetRuntimeModule();

        TRACE_FREE(pDME->module);
        DeleteInteropSafe(pDM);
        Delete(HASH(pEEM), (HASHENTRY *) pDME);

        pDME = (DebuggerModuleEntry *) FindFirstEntry(&hf);
    }

    CHashTableAndData<CNewZeroData>::Clear();
}

void DebuggerModuleTable::AddModule(DebuggerModule *pModule)
{
    CONTRACTL
    {
        THROWS;
        GC_NOTRIGGER;
    }
    CONTRACTL_END;

    _ASSERTE(ThreadHoldsLock());

    _ASSERTE(pModule != NULL);

    LOG((LF_CORDB, LL_EVERYTHING, "DMT::AM: DebuggerMod:0x%x Module:0x%x\n",
        pModule, pModule->GetRuntimeModule()));

    DebuggerModuleEntry * pEntry = (DebuggerModuleEntry *) Add(HASH(pModule->GetRuntimeModule()));
    if (pEntry == NULL)
    {
        ThrowOutOfMemory();
    }

    pEntry->module = pModule;
}

//-----------------------------------------------------------------------------
// Remove a DebuggerModule from the module table when it gets notified.
// This occurs in response to the finalization of an unloaded AssemblyLoadContext.
//-----------------------------------------------------------------------------
void DebuggerModuleTable::RemoveModule(Module* pModule)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
    }
    CONTRACTL_END;

    LOG((LF_CORDB, LL_INFO1000, "DMT::RM Attempting to remove Module:0x%x\n", pModule));

    _ASSERTE(ThreadHoldsLock());
    _ASSERTE(pModule != NULL);

    HASHFIND hf;

    for (DebuggerModuleEntry *pDME = (DebuggerModuleEntry *) FindFirstEntry(&hf);
         pDME != NULL;
         pDME = (DebuggerModuleEntry*) FindNextEntry(&hf))
    {
        DebuggerModule *pDM = pDME->module;
        Module* pRuntimeModule = pDM->GetRuntimeModule();

        if (pRuntimeModule == pModule)
        {
            LOG((LF_CORDB, LL_INFO1000, "DMT::RM Removing DebuggerMod:0x%x - Module:0x%x DF:0x%x\n",
                pDM, pModule, pDM->GetDomainAssembly()));
            TRACE_FREE(pDM);
            DeleteInteropSafe(pDM);
            Delete(HASH(pRuntimeModule), (HASHENTRY *) pDME);
            _ASSERTE(GetModule(pModule) == NULL);
            return;
        }
    }

    LOG((LF_CORDB, LL_INFO1000, "DMT::RM  No debugger module found for Module:0x%x\n", pModule));
}


#endif // DACCESS_COMPILE

DebuggerModule *DebuggerModuleTable::GetModule(Module* module)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
    }
    CONTRACTL_END;

    _ASSERTE(module != NULL);
    _ASSERTE(ThreadHoldsLock());

    DebuggerModuleEntry *entry
      = (DebuggerModuleEntry *) Find(HASH(module), KEY(module));
    if (entry == NULL)
        return NULL;
    else
        return entry->module;
}

DebuggerModule *DebuggerModuleTable::GetFirstModule(HASHFIND *info)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
    }
    CONTRACTL_END;

    _ASSERTE(ThreadHoldsLock());

    DebuggerModuleEntry *entry = (DebuggerModuleEntry *) FindFirstEntry(info);
    if (entry == NULL)
        return NULL;
    else
        return entry->module;
}

DebuggerModule *DebuggerModuleTable::GetNextModule(HASHFIND *info)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
    }
    CONTRACTL_END;

    _ASSERTE(ThreadHoldsLock());

    DebuggerModuleEntry *entry = (DebuggerModuleEntry *) FindNextEntry(info);
    if (entry == NULL)
        return NULL;
    else
        return entry->module;
}
