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
    const DebuggerModuleEntry *pDME;

    pDME = m_table.FindFirstEntry(&hf);

    while (pDME)
    {
        DebuggerModule *pDM = pDME->module;
        TRACE_FREE(pDME->module);
        m_table.Remove(pDM->GetRuntimeModule());
        DeleteInteropSafe(pDM);

        pDME = m_table.FindNextEntry(&hf);
    }

    m_table.RemoveAll();
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

    LOG((LF_CORDB, LL_EVERYTHING, "DMT::AM: DebuggerMod:%p Module:%p\n",
        pModule, pModule->GetRuntimeModule()));

    DebuggerModuleEntry entry = {};
    entry.module = pModule;
    m_table.Add(entry);
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

    LOG((LF_CORDB, LL_INFO1000, "DMT::RM Attempting to remove Module:%p\n", pModule));

    _ASSERTE(ThreadHoldsLock());
    _ASSERTE(pModule != NULL);

    const DebuggerModuleEntry *entry = m_table.LookupPtr(pModule);
    if (entry != nullptr)
    {
        DebuggerModule *pDM = entry->module;
        LOG((LF_CORDB, LL_INFO1000, "DMT::RM Removing DebuggerMod:%p - Module:%p Asm:%p\n",
            pDM, pModule, pDM->GetAssembly()));
        TRACE_FREE(pDM);
        m_table.Remove(pModule);
        DeleteInteropSafe(pDM);
        _ASSERTE(GetModule(pModule) == NULL);
        return;
    }

    LOG((LF_CORDB, LL_INFO1000, "DMT::RM  No debugger module found for Module:%p\n", pModule));
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

    const DebuggerModuleEntry *entry = m_table.LookupPtr(dac_cast<PTR_Module>(module));
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

    const DebuggerModuleEntry *entry = m_table.FindFirstEntry(info);
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

    const DebuggerModuleEntry *entry = m_table.FindNextEntry(info);
    if (entry == NULL)
        return NULL;
    else
        return entry->module;
}
