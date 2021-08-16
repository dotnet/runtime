// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#ifndef _ASSEMBLYLOADCONTEXT_H
#define _ASSEMBLYLOADCONTEXT_H

#include "crst.h"
#include <sarray.h>
#include "assemblybinder.h"

class NativeImage;
class PEImage;
class Module;
class Assembly;
class AssemblyLoaderAllocator;

//
// Unmanaged counter-part of System.Runtime.Loader.AssemblyLoadContext
//
class AssemblyLoadContext : public AssemblyBinder
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
