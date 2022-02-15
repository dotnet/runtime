// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.



#ifndef _METHODDESCITER_H_
#define _METHODDESCITER_H_

#include "instmethhash.h"
#include "method.hpp"
#include "appdomain.hpp"
#include "domainassembly.h"
#include "typehash.h"


// Iterate all the currently loaded instantiations of a mdMethodDef
// in a given AppDomain.  Can be used for both generic + nongeneric methods.
// This may give back duplicate entries; and it may give back some extra
// MethodDescs for which HasNativeCode() returns false.
// Regarding EnC: MethodDescs only match the latest version in an EnC case.
// Thus this iterator does not go through all previous EnC versions.

// This iterator is almost a nop for the non-generic case.
// This is currently not an efficient implementation for the generic case
// as we search every entry in every item in the ParamTypes and/or InstMeth tables.
// It is possible we may have
// to make this more efficient, but it should not be used very often (only
// when debugging prejitted generic code, and then only when updating
// methodInfos after the load of a new module, and also when fetching
// the native code ranges for generic code).
class LoadedMethodDescIterator
{
    Module *     m_module;
    mdMethodDef  m_md;
    MethodDesc * m_mainMD;
    AppDomain *  m_pAppDomain;

    // The following hold the state of the iteration....
    // Yes we iterate everything for the moment - we need
    // to get every single module.  Ideally when finding debugging information
    // we should only iterate freshly added modules.  We would also like to only
    // iterate the relevant entries of the hash tables but that means changing the
    // hash functions.

    // These are used when iterating over an AppDomain
    AppDomain::AssemblyIterator             m_assemIterator;
    Module*                                 m_currentModule;
    AssemblyIterationFlags                  m_assemIterationFlags;

    EETypeHashTable::Iterator               m_typeIterator;
    EETypeHashEntry *                       m_typeIteratorEntry;
    BOOL                                    m_startedNonGenericType;
    InstMethodHashTable::Iterator           m_methodIterator;
    InstMethodHashEntry *                   m_methodIteratorEntry;
    BOOL                                    m_startedNonGenericMethod;
    BOOL                                    m_fFirstTime;

#ifdef _DEBUG
    DomainAssembly * dbg_m_pDomainAssembly;
#endif //_DEBUG

public:
    // Iterates next MethodDesc. Updates the holder only if the assembly differs from the previous one.
    // Caller should not release (i.e. change) the holder explicitly between calls, otherwise collectible
    // assembly might be without a reference and get deallocated (even the native part).
    BOOL Next(CollectibleAssemblyHolder<DomainAssembly *> * pDomainAssemblyHolder);
    MethodDesc *Current();
    void Start(AppDomain * pAppDomain,
               Module *pModule,
               mdMethodDef md,
               AssemblyIterationFlags assemIterationFlags = (AssemblyIterationFlags)(kIncludeLoaded | kIncludeExecution));
    void Start(AppDomain * pAppDomain, Module *pModule, mdMethodDef md, MethodDesc *pDesc);

    LoadedMethodDescIterator(
        AppDomain * pAppDomain,
        Module *pModule,
        mdMethodDef md,
        AssemblyIterationFlags assemblyIterationFlags = (AssemblyIterationFlags)(kIncludeLoaded | kIncludeExecution))
    {
        LIMITED_METHOD_CONTRACT;
        Start(pAppDomain, pModule, md, assemblyIterationFlags);
    }
    LoadedMethodDescIterator(void);

protected:
    Module * GetCurrentModule();

};  // class LoadedMethodDescIterator


#endif // _METHODDESCITER_H_
