// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
// ============================================================
//
// Compatibility.hpp
//


//
// Defines the V2 Compatibility class
//
// ============================================================

#ifndef __BINDER__COMPATIBLITY_HPP__
#define __BINDER__COMPATIBLITY_HPP__

#include "bindertypes.hpp"

namespace BINDER_SPACE
{
    class Compatibility
    {
    public:
        static HRESULT Retarget(/* in */ AssemblyName   *pAssemblyName,
                                /* out */ AssemblyName **ppRetargetedAssemblyName,
                                /* out */ BOOL          *pFIsRetargeted);
    };
};

#endif
