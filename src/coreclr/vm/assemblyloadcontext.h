// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#ifndef _ASSEMBLYLOADCONTEXT_H
#define _ASSEMBLYLOADCONTEXT_H

#include "crst.h"
#include <internalunknownimpl.h>
#include <sarray.h>


class NativeImage;
class Module;
class Assembly;
class AssemblyLoaderAllocator;

/**************************************************************************************
 ** Some things to keep in mind:
 **    - Equality is determined by pointer equality: two interface instances
 **      should be considered equal if and only if their pointer values are equal.
 **    - All operations are idempotent: when a method is called more than once with
 **      the same input values, it is required to return identical results. The only
 **      possible exceptions center around transient errors such as E_OUTOFMEMORY.
**************************************************************************************/
class ICLRPrivBinder
{
public:
    /**********************************************************************************
     ** BindAssemblyByName -- Binds an assembly by name.
     **     NOTE: This method is required to be idempotent. See general comment above.
     **
     ** pAssemblyFullName - name of the assembly for which a bind is being requested.
     ** ppAssembly - upon success, receives the bound assembly.
     **********************************************************************************/
    virtual HRESULT BindAssemblyByName(
        AssemblyNameData* pAssemblyNameData,
        BINDER_SPACE::Assembly **ppAssembly) = 0;

    /**********************************************************************************
     ** GetLoaderAllocator
     ** Get LoaderAllocator for binders that contain it. For other binders, return NULL.
     **
     **********************************************************************************/
    virtual AssemblyLoaderAllocator* GetLoaderAllocator() = 0;

    inline BINDER_SPACE::ApplicationContext* GetAppContext()
    {
        return &m_appContext;
    }

    // Add a virtual destructor to force derived types to also have virtual destructors.
    virtual ~ICLRPrivBinder()
    {
    }

private:
    LONG m_cRef = 0;

    BINDER_SPACE::ApplicationContext m_appContext;
};

//
// Unmanaged counter-part of System.Runtime.Loader.AssemblyLoadContext
//
class AssemblyLoadContext : public ICLRPrivBinder
{
public:
    AssemblyLoadContext();

    NativeImage *LoadNativeImage(Module *componentModule, LPCUTF8 nativeImageName);

    void AddLoadedAssembly(Assembly *loadedAssembly);

    INT_PTR GetManagedAssemblyLoadContext()
    {
        return m_ptrManagedAssemblyLoadContext;
    }

    void SetManagedAssemblyLoadContext(INT_PTR ptrManagedTPABinderInstance)
    {
        m_ptrManagedAssemblyLoadContext = ptrManagedTPABinderInstance;
    }

protected:
    // A GC handle to the managed AssemblyLoadContext.
    // It is a long weak handle for collectible AssemblyLoadContexts and strong handle for non-collectible ones.
    INT_PTR m_ptrManagedAssemblyLoadContext;

private:
    SArray<NativeImage *> m_nativeImages;
    SArray<Assembly *> m_loadedAssemblies;
};

#endif
