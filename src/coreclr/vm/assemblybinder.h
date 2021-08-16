// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#ifndef _ASSEMBLYBINDER_H
#define _ASSEMBLYBINDER_H

#include "../binder/inc/applicationcontext.hpp"

class PEImage;
class AssemblyLoaderAllocator;

class AssemblyBinder
{
public:
    HRESULT BindAssemblyByName(AssemblyNameData* pAssemblyNameData,
        BINDER_SPACE::Assembly** ppAssembly);

    virtual HRESULT BindUsingPEImage(PEImage* pPEImage,
        BOOL fIsNativeImage,
        BINDER_SPACE::Assembly** ppAssembly) = 0;

    virtual HRESULT BindUsingAssemblyName(BINDER_SPACE::AssemblyName* pAssemblyName,
        BINDER_SPACE::Assembly** ppAssembly) = 0;

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

private:
    BINDER_SPACE::ApplicationContext m_appContext;
};

#endif
