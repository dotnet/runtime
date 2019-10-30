// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
// ============================================================
//
// FailureCache.hpp
//


//
// Defines the FailureCache class
//
// ============================================================


#ifndef __BINDER__FAILURE_CACHE_HPP__
#define __BINDER__FAILURE_CACHE_HPP__

#include "failurecachehashtraits.hpp"

namespace BINDER_SPACE
{
    class FailureCache : protected SHash<FailureCacheHashTraits>
    {
    private:
        typedef SHash<FailureCacheHashTraits> Hash;
    public:
        FailureCache();
        ~FailureCache();

        HRESULT Add(/* in */ SString  &assemblyNameorPath,
                    /* in */ HRESULT  hrBindResult);
        HRESULT Lookup(/* in */ SString &assemblyNameorPath);
        void Remove(/* in */ SString &assemblyName);
    };
};

#endif
