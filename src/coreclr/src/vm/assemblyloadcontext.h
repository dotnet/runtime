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

    NativeImage *LoadNativeImage(Module *componentModule, LPCUTF8 nativeImageName);

private:
    SArray<NativeImage *> m_nativeImages;
};

#endif
