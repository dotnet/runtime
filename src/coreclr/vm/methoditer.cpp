// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//*****************************************************************************
// File: MethodIter.cpp

// Iterate through jitted instances of a method.
//*****************************************************************************


#include "common.h"
#include "methoditer.h"


//---------------------------------------------------------------------------------------
//
// Iterates next MethodDesc. Updates the holder only if the assembly differs from the previous one.
// Caller should not release (i.e. change) the holder explicitly between calls, otherwise collectible
// assembly might be without a reference and get deallocated (even the native part).
//
BOOL LoadedMethodDescIterator::Next(
    CollectibleAssemblyHolder<DomainAssembly *> * pDomainAssemblyHolder)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_ANY;
    }
    CONTRACTL_END

    if (!m_fFirstTime)
    {
        // This is the 2nd or more time we called Next().

        // If the method + type is not generic, then nothing more to iterate.
        if (!m_mainMD->HasClassOrMethodInstantiation())
        {
            *pDomainAssemblyHolder = NULL;
            return FALSE;
        }
        goto ADVANCE_METHOD;
    }

    m_fFirstTime = FALSE;

    // This is the 1st time we've called Next(). must Initialize iterator
    if (m_mainMD == NULL)
    {
        m_mainMD = m_module->LookupMethodDef(m_md);
    }

    // note m_mainMD should be sufficiently restored to allow us to get
    // at the method table, flags and token etc.
    if (m_mainMD == NULL)
    {
        *pDomainAssemblyHolder = NULL;
        return FALSE;
    }

    // Needs to work w/ non-generic methods too.
    if (!m_mainMD->HasClassOrMethodInstantiation())
    {
        *pDomainAssemblyHolder = NULL;
        return TRUE;
    }

    m_assemIterator = m_pAppDomain->IterateAssembliesEx(m_assemIterationFlags);

ADVANCE_ASSEMBLY:
    if  (!m_assemIterator.Next(pDomainAssemblyHolder))
    {
        _ASSERTE(*pDomainAssemblyHolder == NULL);
        return FALSE;
    }

#ifdef _DEBUG
    dbg_m_pDomainAssembly = *pDomainAssemblyHolder;
#endif //_DEBUG

    m_currentModule = (*pDomainAssemblyHolder)->GetModule();

    if (m_mainMD->HasClassInstantiation())
    {
        m_typeIterator.Reset();
    }
    else
    {
        m_startedNonGenericType = FALSE;
    }

ADVANCE_TYPE:
    if (m_mainMD->HasClassInstantiation())
    {
        if (!GetCurrentModule()->GetAvailableParamTypes()->FindNext(&m_typeIterator, &m_typeIteratorEntry))
            goto ADVANCE_ASSEMBLY;

        //if (m_typeIteratorEntry->data != TypeHandle(m_mainMD->GetMethodTable()))
        //    goto ADVANCE_TYPE;

        // When looking up the AvailableParamTypes table we have to be really careful since
        // the entries may be unrestored, and may have all sorts of encoded tokens in them.
        // Similar logic occurs in the Lookup function for that table.  We will clean this
        // up in Whidbey Beta2.
        TypeHandle th = m_typeIteratorEntry->GetTypeHandle();

        if (th.IsTypeDesc())
            goto ADVANCE_TYPE;

        MethodTable *pMT = th.AsMethodTable();

        if (!pMT->IsRestored())
            goto ADVANCE_TYPE;

        // Check the class token
        if (pMT->GetTypeDefRid() != m_mainMD->GetMethodTable()->GetTypeDefRid())
            goto ADVANCE_TYPE;

        // Check the module is correct
        if (pMT->GetModule() != m_module)
            goto ADVANCE_TYPE;
    }
    else if (m_startedNonGenericType)
    {
        goto ADVANCE_ASSEMBLY;
    }
    else
    {
        m_startedNonGenericType = TRUE;
    }

    if (m_mainMD->HasMethodInstantiation())
    {
        m_methodIterator.Reset();
    }
    else
    {
        m_startedNonGenericMethod = FALSE;
    }

ADVANCE_METHOD:
    if (m_mainMD->HasMethodInstantiation())
    {
        if (!GetCurrentModule()->GetInstMethodHashTable()->FindNext(&m_methodIterator, &m_methodIteratorEntry))
            goto ADVANCE_TYPE;
        if (m_methodIteratorEntry->GetMethod()->GetModule() != m_module)
            goto ADVANCE_METHOD;
        if (m_methodIteratorEntry->GetMethod()->GetMemberDef() != m_md)
            goto ADVANCE_METHOD;
    }
    else if (m_startedNonGenericMethod)
    {
        goto ADVANCE_TYPE;
    }
    else
    {
        m_startedNonGenericMethod = TRUE;
    }

    // Note: We don't need to keep the assembly alive in DAC - see code:CollectibleAssemblyHolder#CAH_DAC
#ifndef DACCESS_COMPILE
    _ASSERTE_MSG(
        *pDomainAssemblyHolder == dbg_m_pDomainAssembly,
        "Caller probably modified the assembly holder, which they shouldn't - see method comment.");
#endif //DACCESS_COMPILE

    return TRUE;
} // LoadedMethodDescIterator::Next


Module * LoadedMethodDescIterator::GetCurrentModule()
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_ANY;
    }
    CONTRACTL_END

    return m_currentModule;
}

MethodDesc *LoadedMethodDescIterator::Current()
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        PRECONDITION(CheckPointer(m_mainMD));
    }
    CONTRACTL_END


    if (m_mainMD->HasMethodInstantiation())
    {
        _ASSERTE(m_methodIteratorEntry);
        return m_methodIteratorEntry->GetMethod();
    }

    if (!m_mainMD->HasClassInstantiation())
    {
        // No Method or Class instantiation,then it's not generic.
        return m_mainMD;
    }

    MethodTable *pMT = m_typeIteratorEntry->GetTypeHandle().GetMethodTable()->GetCanonicalMethodTable();
    PREFIX_ASSUME(pMT != NULL);
    return pMT->GetParallelMethodDesc(m_mainMD);
}

void
LoadedMethodDescIterator::Start(
    AppDomain * pAppDomain,
    Module *pModule,
    mdMethodDef md,
    AssemblyIterationFlags assemblyIterationFlags)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        PRECONDITION(CheckPointer(pModule));
        PRECONDITION(CheckPointer(pAppDomain, NULL_OK));
    }
    CONTRACTL_END;

    m_assemIterationFlags = assemblyIterationFlags;
    m_mainMD = NULL;
    m_module = pModule;
    m_md = md;
    m_pAppDomain = pAppDomain;
    m_fFirstTime = TRUE;

    _ASSERTE(pAppDomain != NULL);
    _ASSERTE(TypeFromToken(m_md) == mdtMethodDef);
}

// This is special init for DAC only
// @TODO:: change it to dac compile only.
void
LoadedMethodDescIterator::Start(
    AppDomain     *pAppDomain,
    Module          *pModule,
    mdMethodDef     md,
    MethodDesc      *pMethodDesc)
{
    Start(pAppDomain, pModule, md);
    m_mainMD = pMethodDesc;
}

LoadedMethodDescIterator::LoadedMethodDescIterator(void)
{
    LIMITED_METHOD_CONTRACT;
    m_mainMD = NULL;
    m_module = NULL;
    m_md = mdTokenNil;
    m_pAppDomain = NULL;
}
