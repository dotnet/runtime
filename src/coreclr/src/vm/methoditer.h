// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.



#ifndef _METHODDESCITER_H_
#define _METHODDESCITER_H_

#include "instmethhash.h"
#include "method.hpp"
#include "appdomain.hpp"
#include "domainfile.h"
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
    DomainModuleIterator                    m_moduleIterator;
    AssemblyIterationFlags                  m_assemIterationFlags;
    ModuleIterationOption                   m_moduleIterationFlags;

    // These are used when iterating over the SharedDomain
    SharedDomain::SharedAssemblyIterator    m_sharedAssemblyIterator;
    Assembly::ModuleIterator                m_sharedModuleIterator;

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
    // Defines the set of assemblies that LoadedMethodDescIterator should consider. 
    // Typical usages:
    //     * Debugger controller (for setting breakpoint) just uses kModeAllADAssemblies.  
    //     * RejitManager uses the iterator once with kModeSharedDomainAssemblies, and
    //         then a bunch of times (once per AD) with kModeUnsharedADAssemblies to
    //         ensure all assemblies in all ADs are considered, and to avoid unnecessary
    //         dupes for domain-neutral assemblies.
    enum AssemblyIterationMode
    {
        // Default, used by debugger's breakpoint controller.  Iterates through all
        // Assemblies associated with the specified AppDomain
        kModeAllADAssemblies,

        // Iterate through only the *unshared* assemblies associated with the specified
        // AppDomain.
        kModeUnsharedADAssemblies,

        // Rather than iterating through Assemblies associated with an AppDomain, just
        // iterate over all Assemblies associated with the SharedDomain
        kModeSharedDomainAssemblies,
    };

    // Iterates next MethodDesc. Updates the holder only if the assembly differs from the previous one.
    // Caller should not release (i.e. change) the holder explicitly between calls, otherwise collectible 
    // assembly might be without a reference and get deallocated (even the native part).
    BOOL Next(CollectibleAssemblyHolder<DomainAssembly *> * pDomainAssemblyHolder);
    MethodDesc *Current();
    void Start(AppDomain * pAppDomain,
               Module *pModule,
               mdMethodDef md,
               AssemblyIterationMode assemblyIterationMode,
               AssemblyIterationFlags assemIterationFlags = (AssemblyIterationFlags)(kIncludeLoaded | kIncludeExecution),
               ModuleIterationOption moduleIterationFlags = kModIterIncludeLoaded);
    void Start(AppDomain * pAppDomain, Module *pModule, mdMethodDef md, MethodDesc *pDesc);
    
    LoadedMethodDescIterator(
        AppDomain * pAppDomain,
        Module *pModule,
        mdMethodDef md,
        AssemblyIterationMode assemblyIterationMode = kModeAllADAssemblies,
        AssemblyIterationFlags assemblyIterationFlags = (AssemblyIterationFlags)(kIncludeLoaded | kIncludeExecution),
        ModuleIterationOption moduleIterationFlags = kModIterIncludeLoaded)
    {
        LIMITED_METHOD_CONTRACT;
        Start(pAppDomain, pModule, md, assemblyIterationMode, assemblyIterationFlags, moduleIterationFlags);
    }
    LoadedMethodDescIterator(void);

protected:
    AssemblyIterationMode m_assemblyIterationMode;
    BOOL m_fSharedDomain;

    Module * GetCurrentModule();
    BOOL NextSharedModule();

};  // class LoadedMethodDescIterator


#endif // _METHODDESCITER_H_
