// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#ifndef _ASSEMBLYLOADCONTEXT_H
#define _ASSEMBLYLOADCONTEXT_H

#include "crst.h"

class NativeImage;
class Module;

//
// Unmanaged counter-part of System.Runtime.Loader.AssemblyLoadContext
//
class AssemblyLoadContext : public IUnknownCommon<ICLRPrivBinder, IID_ICLRPrivBinder>
{
public:
    AssemblyLoadContext();

    STDMETHOD(GetBinderID)(
        /* [retval][out] */ UINT_PTR* pBinderId);

    NativeImage *LoadNativeImage(Module *componentModule, LPCUTF8 nativeImageName, int nativeImageNameLength);

private:
    // Multi-thread safe access to the list of composite R2R images
    class NativeImageList
    {
    private:
        ArrayList m_array;

    public:
        NativeImageList();
    
        void Clear_Unlocked();

        bool IsEmpty_Unlocked();
        int32_t GetCount_Unlocked();
        NativeImage* Get_UnlockedNoReference(int32_t index);
        void Set_Unlocked(int32_t index, NativeImage* nativeImage);
        HRESULT Append_Unlocked(NativeImage* nativeImage);
    };

    // Conceptually a list of NativeImage structures, protected by lock AppDomain::GetAssemblyListLock
    NativeImageList m_nativeImages;
};

#endif
