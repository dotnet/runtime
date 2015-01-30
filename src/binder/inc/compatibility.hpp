//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//
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
