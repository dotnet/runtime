// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#ifndef _ASSEMBLYBINDER_H
#define _ASSEMBLYBINDER_H

#include <sarray.h>
#include "../binder/inc/applicationcontext.hpp"

class PEImage;
class NativeImage;
class Assembly;
class Module;
class AssemblyLoaderAllocator;

class AssemblyBinder
{
public:

    HRESULT BindAssemblyByName(AssemblyNameData* pAssemblyNameData, BINDER_SPACE::Assembly** ppAssembly);
    virtual HRESULT BindUsingPEImage(PEImage* pPEImage, BINDER_SPACE::Assembly** ppAssembly) = 0;
    virtual HRESULT BindUsingAssemblyName(BINDER_SPACE::AssemblyName* pAssemblyName, BINDER_SPACE::Assembly** ppAssembly) = 0;

    /// <summary>
    /// Get LoaderAllocator for binders that contain it. For other binders, return NULL.
    /// </summary>
    virtual AssemblyLoaderAllocator* GetLoaderAllocator() = 0;

    /// <summary>
    /// Tells if the binder is a default binder (not a custom one)
    /// </summary>
    virtual bool IsDefault() = 0;

    inline BINDER_SPACE::ApplicationContext* GetAppContext()
    {
        return &m_appContext;
    }

    INT_PTR GetManagedAssemblyLoadContext()
    {
        return m_ptrManagedAssemblyLoadContext;
    }

    void SetManagedAssemblyLoadContext(INT_PTR ptrManagedDefaultBinderInstance)
    {
        m_ptrManagedAssemblyLoadContext = ptrManagedDefaultBinderInstance;
    }

    NativeImage* LoadNativeImage(Module* componentModule, LPCUTF8 nativeImageName);
    void AddLoadedAssembly(Assembly* loadedAssembly);

private:
    BINDER_SPACE::ApplicationContext m_appContext;

    // A GC handle to the managed AssemblyLoadContext.
    // It is a long weak handle for collectible AssemblyLoadContexts and strong handle for non-collectible ones.
    INT_PTR m_ptrManagedAssemblyLoadContext;

    SArray<NativeImage*> m_nativeImages;
    SArray<Assembly*> m_loadedAssemblies;
};

#endif
