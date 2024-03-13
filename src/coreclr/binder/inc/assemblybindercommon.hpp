// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// ============================================================
//
// AssemblyBinder.hpp
//


//
// Defines the AssemblyBinder class
//
// ============================================================

#ifndef __BINDER__ASSEMBLY_BINDER_COMMON_HPP__
#define __BINDER__ASSEMBLY_BINDER_COMMON_HPP__

#include "bindertypes.hpp"
#include "bundle.h"

class AssemblyBinder;
class DefaultAssemblyBinder;
class PEAssembly;
class PEImage;

namespace BINDER_SPACE
{
    class AssemblyBinderCommon
    {
    public:
        static HRESULT BindToSystem(/* in */ SString    &systemDirectory,
                                    /* out */ PEImage   **ppPEImage);

        static HRESULT TranslatePEToArchitectureType(DWORD  *pdwPAFlags, PEKIND *PeKind);

        static HRESULT CreateDefaultBinder(DefaultAssemblyBinder** ppDefaultBinder);

        static BOOL IsValidArchitecture(PEKIND kArchitecture);
    };
};

#endif
